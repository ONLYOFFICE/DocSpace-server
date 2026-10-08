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
/// Covers who may change what. The templates and their fields belong to the DocSpace admins, so a room admin or a user
/// is refused on their update and deletion the way they are on the creation. The values and the assignments follow the
/// right to edit the entry: a viewer is refused on every write of the file, an editor writes the files but not the room.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataRightsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int StringType = 0;
    private const string ClientField = "Client";

    #region Templates and fields

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task UpdateTemplate_ByANonDocSpaceAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Locked " + Suffix(), [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(member);

        using var response = await api.UpdateTemplateResponseAsync(template.Id, new { name = "Renamed " + Suffix() }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await _filesClient.Authenticate(Owner);

        (await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken)).Name.Should().Be(template.Name, "a refused update changes nothing");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task DeleteTemplate_ByANonDocSpaceAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Kept " + Suffix(), [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(member);

        using var response = await api.DeleteTemplateResponseAsync(template.Id, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await _filesClient.Authenticate(Owner);

        using var reread = await api.GetTemplateResponseAsync(template.Id, TestContext.Current.CancellationToken);
        reread.StatusCode.Should().Be(HttpStatusCode.OK, "a refused deletion leaves the template in place");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task FieldManagement_ByANonDocSpaceAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Fields " + Suffix(), [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var field = template.Field(ClientField);
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(member);

        using var created = await api.CreateFieldResponseAsync(template.Id, new MetadataFieldPayload { Name = "Department", Type = StringType }, TestContext.Current.CancellationToken);
        created.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the fields are part of the template, an admin's object");

        using var updated = await api.UpdateFieldResponseAsync(template.Id, field.Id, new { name = "Customer" }, TestContext.Current.CancellationToken);
        updated.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var deleted = await api.DeleteFieldResponseAsync(template.Id, field.Id, TestContext.Current.CancellationToken);
        deleted.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await _filesClient.Authenticate(Owner);

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Fields.Should().ContainSingle().Which.Name.Should().Be(ClientField, "none of the refused requests must have changed the template");
    }

    #endregion

    #region Values and assignments

    [Fact]
    public async Task ValuesAndAssignments_ByAViewer_ReturnForbidden()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Viewer " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var other = await api.CreateTemplateAsync("Viewer other " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Viewer {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);
        var value = new MetadataValuePayload { FieldId = template.Field(ClientField).Id, StringValue = "ACME" };

        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(file.Id, [value], TestContext.Current.CancellationToken);

        var viewer = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, viewer, FileShare.Read);
        await _filesClient.Authenticate(viewer);

        // reading is what the viewer is there for
        var visible = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        visible.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("ACME");

        using var fileValues = await api.SetFileValuesResponseAsync(file.Id, [new MetadataValuePayload { FieldId = value.FieldId, StringValue = "Globex" }], TestContext.Current.CancellationToken);
        fileValues.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the values follow the right to edit the file");

        using var roomValues = await api.SetFolderValuesResponseAsync(room.Id, [value], TestContext.Current.CancellationToken);
        roomValues.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var assigned = await api.AssignFileTemplatesResponseAsync(file.Id, [other.Id], TestContext.Current.CancellationToken);
        assigned.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var unassigned = await api.UnassignFileTemplateResponseAsync(file.Id, template.Id, TestContext.Current.CancellationToken);
        unassigned.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await _filesClient.Authenticate(Owner);

        var untouched = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        untouched.Should().ContainSingle(t => t.Id == template.Id, "the refused assignment and removal changed nothing")
            .Which.Field(ClientField).Value!.StringValue.Should().Be("ACME", "the refused write changed nothing");
    }

    [Fact]
    public async Task ValuesAndAssignments_ByAnEditor_AreAcceptedOnTheFile_AndRefusedOnTheFolderAndTheRoom()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Editor " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Editor {suffix}");
        var folder = await CreateFolder($"Sub {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);
        var value = new MetadataValuePayload { FieldId = template.Field(ClientField).Id, StringValue = "ACME" };

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.AssignFolderTemplatesAsync(folder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);

        var editor = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, editor, FileShare.Editing);
        await _filesClient.Authenticate(editor);

        using var assigned = await api.AssignFileTemplatesResponseAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        assigned.StatusCode.Should().Be(HttpStatusCode.OK, "an editing member may assign a template to the files they edit");

        await api.SetFileValuesAsync(file.Id, [value], TestContext.Current.CancellationToken);

        using var roomValues = await api.SetFolderValuesResponseAsync(room.Id, [value], TestContext.Current.CancellationToken);
        roomValues.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the room itself is edited by its manager only");

        // the metadata of a folder is a property of the folder, like its name: it follows the right to create in the
        // folder, which the editing role does not have, the same rule the client applies to show the editor
        using var folderValues = await api.SetFolderValuesResponseAsync(folder.Id, [value], TestContext.Current.CancellationToken);
        folderValues.StatusCode.Should().Be(HttpStatusCode.Forbidden, "an editing member edits the documents, not the folders");

        using var folderCustomFields = await api.SetFolderCustomFieldsResponseAsync(folder.Id, [new CustomFieldPayload("Project code", "A-42")], TestContext.Current.CancellationToken);
        folderCustomFields.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var folderCascade = await api.UpdateFolderTemplateResponseAsync(folder.Id, template.Id, cascade: true, TestContext.Current.CancellationToken);
        folderCascade.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the cascade writes the whole subtree");

        var written = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        written.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("ACME");
    }

    [Fact]
    public async Task ValuesAndAssignments_ByAContentCreator_AreAcceptedOnTheFolder_AndRefusedOnTheRoom()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Creator " + suffix, [new MetadataFieldPayload { Name = ClientField, Type = StringType }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Creator {suffix}");
        var folder = await CreateFolder($"Sub {suffix}", room.Id);
        var value = new MetadataValuePayload { FieldId = template.Field(ClientField).Id, StringValue = "ACME" };

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);

        var creator = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);
        await _filesClient.Authenticate(creator);

        // a content creator may create in the folder, so the folder's metadata is theirs to write, cascade included
        await api.AssignFolderTemplatesAsync(folder.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(folder.Id, [value], TestContext.Current.CancellationToken);

        var cascade = await api.UpdateFolderTemplateAsync(folder.Id, template.Id, cascade: true, TestContext.Current.CancellationToken);
        cascade.Id.Should().NotBeNullOrEmpty();

        using var roomValues = await api.SetFolderValuesResponseAsync(room.Id, [value], TestContext.Current.CancellationToken);
        roomValues.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the room itself is edited by its manager only");

        var written = await api.GetFolderMetadataAsync(folder.Id, TestContext.Current.CancellationToken);
        written.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("ACME");
    }

    [Fact]
    public async Task FolderMetadata_OnTheSectionRoots_IsRefused()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Root " + Suffix(), [], TestContext.Current.CancellationToken);
        var value = new CustomFieldPayload("Project code", "A-42");

        // the roots answer the right to create with true for their owner and for every room admin, while nobody may
        // edit them: a cascade queued from the rooms root would walk every room of the portal
        var myDocuments = await GetSectionRootIdAsync("api/2.0/files/@my");

        using var myAssign = await api.AssignFolderTemplatesResponseAsync(myDocuments, [template.Id], cascade: true, TestContext.Current.CancellationToken);
        myAssign.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the owner creates in My documents but does not write its metadata");

        using var myFields = await api.SetFolderCustomFieldsResponseAsync(myDocuments, [value], TestContext.Current.CancellationToken);
        myFields.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        var rooms = await GetSectionRootIdAsync("api/2.0/files/rooms");

        using var roomsAssign = await api.AssignFolderTemplatesResponseAsync(rooms, [template.Id], cascade: true, TestContext.Current.CancellationToken);
        roomsAssign.StatusCode.Should().Be(HttpStatusCode.Forbidden, "a room admin creates rooms but does not write the metadata of the rooms root");

        using var roomsFields = await api.SetFolderCustomFieldsResponseAsync(rooms, [value], TestContext.Current.CancellationToken);
        roomsFields.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    private async Task<MetadataApiClient> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        return new MetadataApiClient(_filesClient);
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
