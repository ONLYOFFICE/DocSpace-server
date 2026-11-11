// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// design elements, icons, logos, and text, are the property of
// Ascensio System SIA and are protected by copyright and trademark laws.
// These elements may not be used in derivative works or modified in any way
// without prior written permission from Ascensio System SIA.
//
// Pursuant to Section 7 § 3(b) of the License you must retain the original
// Product logo when distributing the program. Pursuant to Section 7 § 3(e) we
// decline to grant you any rights under trademark law for use of our trademarks.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers what a metadata change tells the connected clients: the ordinary update event of every entry it changed, in
/// the room of the entry's folder, so an open listing re-reads the row. A direct write announces its entry; a cascade
/// pass, a hidden template, a deleted template and a deleted field announce the entries they touched, which nobody
/// named in the request.
/// </summary>
/// <remarks>
/// The socket is connected before the arrangement writes, and the events those writes send are awaited first: the
/// action under test has to add an event of its own, an arrangement event still in flight cannot pass for it.
/// </remarks>
[Trait("Category", "Metadata")]
[Trait("Category", "Socket")]
public class MetadataSocketEventsTests(AspireAppFixture fixture) : _05_Features.Socket.SocketTestBase(fixture)
{
    [Fact]
    public async Task AssignValuesAndUnassignOnAFile_EachSendTheFileUpdate_ToItsFolderRoom()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom($"Assign {Suffix()}");
        var file = await CreateFile("Assigned.docx", room.Id);
        await using var socket = await ConnectAndWatch(Owner, room.Id);

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await WaitForUpdates(socket, "file", file.Id, 1);

        await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues([new MetadataValueRequest(template.Fields.Single().Id, stringValue: "ACME")]), TestContext.Current.CancellationToken);
        await WaitForUpdates(socket, "file", file.Id, 2);

        await _metadataApi.UnassignFileTemplateAsync(file.Id, template.Id, TestContext.Current.CancellationToken);
        await WaitForUpdates(socket, "file", file.Id, 3);
    }

    [Fact]
    public async Task SetFolderCustomFields_SendsTheFolderUpdate_ToItsParentRoom()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Custom {Suffix()}");
        var folder = await CreateFolder("Custom fields", room.Id);
        await using var socket = await ConnectAndWatch(Owner, room.Id);

        await _metadataApi.SetFolderCustomFieldsAsync(folder.Id, new SetCustomFields([new CustomFieldRequest("Client", "ACME")]), TestContext.Current.CancellationToken);

        await WaitForUpdates(socket, "folder", folder.Id, 1);
    }

    [Fact]
    public async Task HidingATemplate_SendsTheUpdateOfEveryEntryItIsAssignedTo()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom($"Hide {Suffix()}");
        var file = await CreateFile("Hidden.docx", room.Id);
        var folder = await CreateFolder("Hidden folder", room.Id);
        await using var socket = await ConnectAndWatch(Owner, room.Id);

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFolderTemplatesAsync(folder.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await WaitForUpdates(socket, "file", file.Id, 1);
        await WaitForUpdates(socket, "folder", folder.Id, 1);

        await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate(visible: false), TestContext.Current.CancellationToken);

        // the template endpoint names no entry: hasMetadataFields of both entries changed behind the clients
        await WaitForUpdates(socket, "file", file.Id, 2);
        await WaitForUpdates(socket, "folder", folder.Id, 2);
    }

    [Fact]
    public async Task DeletingATemplate_SendsTheUpdateOfTheEntriesThatCarriedIt()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom($"Delete {Suffix()}");
        var file = await CreateFile("Deleted template.docx", room.Id);
        await using var socket = await ConnectAndWatch(Owner, room.Id);

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await WaitForUpdates(socket, "file", file.Id, 1);

        await _metadataApi.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken);

        await WaitForUpdates(socket, "file", file.Id, 2);
    }

    [Fact]
    public async Task DeletingAField_SendsTheUpdateOfTheEntriesThatHeldAValue()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync();
        var dropped = (await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("Dropped", MetadataFieldType.String), TestContext.Current.CancellationToken)).Response;
        var room = await CreateCustomRoom($"Field {Suffix()}");
        var file = await CreateFile("Deleted field.docx", room.Id);
        await using var socket = await ConnectAndWatch(Owner, room.Id);

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues([new MetadataValueRequest(dropped.Id, stringValue: "gone")]), TestContext.Current.CancellationToken);
        await WaitForUpdates(socket, "file", file.Id, 2);

        await _metadataApi.DeleteFieldAsync(template.Id, dropped.Id, TestContext.Current.CancellationToken);

        await WaitForUpdates(socket, "file", file.Id, 3);
    }

    [Fact]
    public async Task CascadeSwitchedOn_SendsAnUpdatePerChangedFile_AndOneForTheRoom()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync();
        var room = await CreateCustomRoom($"Cascade {Suffix()}");
        var first = await CreateFile("First.docx", room.Id);
        var second = await CreateFile("Second.docx", room.Id);
        var roomsRootId = await GetFolderIdAsync(FolderType.VirtualRooms, Owner);
        await using var socket = await ConnectAndWatch(Owner, room.Id, roomsRootId);

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(room.Id, new SetMetadataValues([new MetadataValueRequest(template.Fields.Single().Id, stringValue: "Cascaded")]), TestContext.Current.CancellationToken);
        await WaitForUpdates(socket, "folder", room.Id, 2);

        await _metadataApi.UpdateFolderTemplateAsync(room.Id, template.Id, new UpdateFolderMetadataTemplateRequest(cascade: true), TestContext.Current.CancellationToken);

        // the files are named by nobody: the pass announces each file it wrote, and the room once more
        await WaitForUpdates(socket, "file", first.Id, 1);
        await WaitForUpdates(socket, "file", second.Id, 1);
        await WaitForUpdates(socket, "folder", room.Id, 3);
    }

    private static Task<SocketEvent> WaitForUpdates(PortalSocket socket, string type, int id, int count)
    {
        return socket.WaitForCountAsync(ModifyFolder, Modified("update", type, id), count, cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task<MetadataTemplateDto> CreateTemplateAsync()
    {
        return (await _metadataApi.CreateTemplateAsync(
            new CreateMetadataTemplateRequestDto("Socket " + Suffix(), true, [new MetadataFieldRequest("Client", MetadataFieldType.String)]),
            TestContext.Current.CancellationToken)).Response;
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
