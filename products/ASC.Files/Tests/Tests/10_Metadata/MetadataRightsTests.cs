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
    private const string ClientField = "Client";

    #region Templates and fields

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task UpdateTemplate_ByANonDocSpaceAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Locked " + Suffix(), [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate("Renamed " + Suffix()), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);

        (await GetTemplateAsync(template.Id)).Name.Should().Be(template.Name, "a refused update changes nothing");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task DeleteTemplate_ByANonDocSpaceAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Kept " + Suffix(), [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.DeleteTemplateAsync(template.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);

        (await GetTemplateAsync(template.Id)).Id.Should().Be(template.Id, "a refused deletion leaves the template in place");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task FieldManagement_ByANonDocSpaceAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Fields " + Suffix(), [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var field = template.Field(ClientField);
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(member);

        var created = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("Department", MetadataFieldType.String), TestContext.Current.CancellationToken));
        created.ErrorCode.Should().Be(403, "the fields are part of the template, an admin's object");

        var updated = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateFieldAsync(template.Id, field.Id, new UpdateMetadataFieldRequest(name: "Customer"), TestContext.Current.CancellationToken));
        updated.ErrorCode.Should().Be(403);

        var deleted = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.DeleteFieldAsync(template.Id, field.Id, TestContext.Current.CancellationToken));
        deleted.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);

        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Fields.Should().ContainSingle().Which.Name.Should().Be(ClientField, "none of the refused requests must have changed the template");
    }

    #endregion

    #region Values and assignments

    [Fact]
    public async Task ValuesAndAssignments_ByAViewer_ReturnForbidden()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Viewer " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var other = await CreateTemplateAsync("Viewer other " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var room = await CreateCustomRoom($"Viewer {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);
        var value = new MetadataValueRequest(template.Field(ClientField).Id, stringValue: "ACME");

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues([value]), TestContext.Current.CancellationToken);

        var viewer = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, viewer, FileShare.Read);
        await _filesClient.Authenticate(viewer);

        // reading is what the viewer is there for
        var visible = await GetFileTemplatesAsync(file.Id);
        visible.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("ACME");

        var fileValues = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues([new MetadataValueRequest(value.FieldId, stringValue: "Globex")]), TestContext.Current.CancellationToken));
        fileValues.ErrorCode.Should().Be(403, "the values follow the right to edit the file");

        var roomValues = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderValuesAsync(room.Id, new SetMetadataValues([value]), TestContext.Current.CancellationToken));
        roomValues.ErrorCode.Should().Be(403);

        var assigned = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([other.Id]), TestContext.Current.CancellationToken));
        assigned.ErrorCode.Should().Be(403);

        var unassigned = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UnassignFileTemplateAsync(file.Id, template.Id, TestContext.Current.CancellationToken));
        unassigned.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);

        var untouched = await GetFileTemplatesAsync(file.Id);
        untouched.Should().ContainSingle(t => t.Id == template.Id, "the refused assignment and removal changed nothing")
            .Which.Field(ClientField).Value!.StringValue.Should().Be("ACME", "the refused write changed nothing");
    }

    [Fact]
    public async Task ValuesAndAssignments_ByAnEditor_AreAcceptedOnTheFile_AndRefusedOnTheFolderAndTheRoom()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Editor " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var room = await CreateCustomRoom($"Editor {suffix}");
        var folder = await CreateFolder($"Sub {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);
        var value = new MetadataValueRequest(template.Field(ClientField).Id, stringValue: "ACME");

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.AssignFolderTemplatesAsync(folder.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);

        var editor = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, editor, FileShare.Editing);
        await _filesClient.Authenticate(editor);

        // an editing member may assign a template to the files they edit
        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);

        await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues([value]), TestContext.Current.CancellationToken);

        var roomValues = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderValuesAsync(room.Id, new SetMetadataValues([value]), TestContext.Current.CancellationToken));
        roomValues.ErrorCode.Should().Be(403, "the room itself is edited by its manager only");

        // the metadata of a folder is a property of the folder, like its name: it follows the right to create in the
        // folder, which the editing role does not have, the same rule the client applies to show the editor
        var folderValues = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderValuesAsync(folder.Id, new SetMetadataValues([value]), TestContext.Current.CancellationToken));
        folderValues.ErrorCode.Should().Be(403, "an editing member edits the documents, not the folders");

        var folderCustomFields = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderCustomFieldsAsync(folder.Id, new SetCustomFields([new CustomFieldRequest("Project code", "A-42")]), TestContext.Current.CancellationToken));
        folderCustomFields.ErrorCode.Should().Be(403);

        var folderCascade = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateFolderTemplateAsync(folder.Id, template.Id, new UpdateFolderMetadataTemplateRequest(cascade: true), TestContext.Current.CancellationToken));
        folderCascade.ErrorCode.Should().Be(403, "the cascade writes the whole subtree");

        var written = await GetFileTemplatesAsync(file.Id);
        written.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("ACME");
    }

    [Fact]
    public async Task ValuesAndAssignments_ByAContentCreator_AreAcceptedOnTheFolder_AndRefusedOnTheRoom()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Creator " + suffix, [new MetadataFieldRequest(ClientField, MetadataFieldType.String)]);
        var room = await CreateCustomRoom($"Creator {suffix}");
        var folder = await CreateFolder($"Sub {suffix}", room.Id);
        var value = new MetadataValueRequest(template.Field(ClientField).Id, stringValue: "ACME");

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);

        var creator = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);
        await _filesClient.Authenticate(creator);

        // a content creator may create in the folder, so the folder's metadata is theirs to write, cascade included
        await _metadataApi.AssignFolderTemplatesAsync(folder.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(folder.Id, new SetMetadataValues([value]), TestContext.Current.CancellationToken);

        var cascade = (await _metadataApi.UpdateFolderTemplateAsync(folder.Id, template.Id, new UpdateFolderMetadataTemplateRequest(cascade: true), TestContext.Current.CancellationToken)).Response;
        cascade.Id.Should().NotBeNullOrEmpty();

        var roomValues = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderValuesAsync(room.Id, new SetMetadataValues([value]), TestContext.Current.CancellationToken));
        roomValues.ErrorCode.Should().Be(403, "the room itself is edited by its manager only");

        var written = (await _metadataApi.GetFolderMetadataAsync(folder.Id, TestContext.Current.CancellationToken)).Response.Templates;
        written.Should().ContainSingle(t => t.Id == template.Id).Which.Field(ClientField).Value!.StringValue.Should().Be("ACME");
    }

    [Fact]
    public async Task FolderMetadata_OnTheSectionRoots_IsRefused()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Root " + Suffix(), []);
        var value = new CustomFieldRequest("Project code", "A-42");

        // the roots answer the right to create with true for their owner and for every room admin, while nobody may
        // edit them: a cascade queued from the rooms root would walk every room of the portal
        var myDocuments = (await _foldersApi.GetMyFolderAsync(count: 1, cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        var myAssign = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.AssignFolderTemplatesAsync(myDocuments, new AssignMetadataTemplates([template.Id], cascade: true), TestContext.Current.CancellationToken));
        myAssign.ErrorCode.Should().Be(403, "the owner creates in My documents but does not write its metadata");

        var myFields = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderCustomFieldsAsync(myDocuments, new SetCustomFields([value]), TestContext.Current.CancellationToken));
        myFields.ErrorCode.Should().Be(403);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        var rooms = (await _roomsApi.GetRoomsFolderAsync(count: 1, cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        var roomsAssign = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.AssignFolderTemplatesAsync(rooms, new AssignMetadataTemplates([template.Id], cascade: true), TestContext.Current.CancellationToken));
        roomsAssign.ErrorCode.Should().Be(403, "a room admin creates rooms but does not write the metadata of the rooms root");

        var roomsFields = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderCustomFieldsAsync(rooms, new SetCustomFields([value]), TestContext.Current.CancellationToken));
        roomsFields.ErrorCode.Should().Be(403);
    }

    #endregion

    private async Task<MetadataTemplateDto> CreateTemplateAsync(string name, List<MetadataFieldRequest> fields, bool visible = true)
    {
        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    private async Task<MetadataTemplateDto> GetTemplateAsync(int templateId)
    {
        return (await _metadataApi.GetTemplateAsync(templateId, TestContext.Current.CancellationToken)).Response;
    }

    private async Task<List<EntryTemplateDto>> GetFileTemplatesAsync(int fileId)
    {
        return (await _metadataApi.GetFileMetadataAsync(fileId, TestContext.Current.CancellationToken)).Response.Templates;
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
