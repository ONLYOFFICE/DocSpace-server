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
/// Covers the custom text fields: free-form "name → value" pairs set on a file, a folder or a room by anyone who can
/// edit the entry. Internally they live in a hidden system template, and these tests pin down that the template never
/// shows through, that the fields are addressed by name only, and that the dictionary follows the values.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataCustomFieldsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task SetCustomFields_SetsSeveralFieldsAtOnce_AndReturnsThem()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Custom {suffix}");

        var set = await SetFolderCustomFieldsAsync(room.Id,
            [new CustomFieldRequest("Project code", "A-42"), new CustomFieldRequest("Client", "ACME")]);

        set.Should().BeEquivalentTo([new { Name = "Project code", Value = "A-42" }, new { Name = "Client", Value = "ACME" }]);

        var stored = await GetFolderCustomFieldsAsync(room.Id);
        stored.Should().BeEquivalentTo(set);
    }

    [Fact]
    public async Task SetCustomFields_WithTheSameNameInAnotherCase_ReusesTheField()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Reuse {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await SetFolderCustomFieldAsync(room.Id, "Reference", "R-1");
        var onFile = await SetFileCustomFieldAsync(file.Id, "reference", "R-2");

        // the field is found by its name regardless of the case, so the file shows the name the field was created with
        onFile.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Reference", Value = "R-2" });
    }

    [Fact]
    public async Task SetCustomFields_LeavesTheFieldsNotListedAlone()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Partial {suffix}");

        await SetFolderCustomFieldsAsync(room.Id,
            [new CustomFieldRequest("Project code", "A-42"), new CustomFieldRequest("Client", "ACME")]);

        var after = await SetFolderCustomFieldAsync(room.Id, "Client", "Globex");

        after.Should().BeEquivalentTo([new { Name = "Project code", Value = "A-42" }, new { Name = "Client", Value = "Globex" }]);
    }

    [Fact]
    public async Task SetCustomFields_WithANullValue_RemovesTheFieldFromThisEntryOnly()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Remove {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await SetFolderCustomFieldAsync(room.Id, "Project code", "A-42");
        await SetFileCustomFieldAsync(file.Id, "Project code", "B-7");

        var after = await SetFileCustomFieldAsync(file.Id, "Project code", null);

        after.Should().BeEmpty();
        (await GetFolderCustomFieldsAsync(room.Id))
            .Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Project code", Value = "A-42" }, "the other entry keeps the field");
    }

    [Fact]
    public async Task SetCustomFields_RenamesAFieldOnTheEntry_ByRemovingAndAdding()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Rename {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await SetFolderCustomFieldAsync(room.Id, "Client", "ACME");
        await SetFileCustomFieldAsync(file.Id, "Client", "Globex");

        var renamed = (await _metadataApi.SetFileCustomFieldsAsync(file.Id,
            new SetCustomFields([new CustomFieldRequest("Client", null!), new CustomFieldRequest("Customer", "Globex")]),
            TestContext.Current.CancellationToken)).Response;

        renamed.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Customer", Value = "Globex" });
        (await GetFolderCustomFieldsAsync(room.Id))
            .Should().ContainSingle().Which.Name.Should().Be("Client", "a rename on one entry is not a rename of the tenant's field");
    }

    [Fact]
    public async Task SetCustomFields_RemovingAFieldTheEntryDoesNotHold_IsANoOp()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"NoOp {suffix}");

        var result = await SetFolderCustomFieldAsync(room.Id, $"Never set {suffix}", null);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SetCustomFields_WithAnEmptyName_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Empty {Suffix()}");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SetFolderCustomFieldsAsync(room.Id, [new CustomFieldRequest("  ", "A-42")]));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetCustomFields_WithTheSameNameTwice_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Twice {Suffix()}");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SetFolderCustomFieldsAsync(room.Id,
                [new CustomFieldRequest("Client", "ACME"), new CustomFieldRequest("client", "Globex")]));

        exception.ErrorCode.Should().Be(400, "the name is the key of the field, so it cannot be listed twice");
    }

    [Fact]
    public async Task SetCustomFields_BeyondTheLimit_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Limit {suffix}");

        var tooMany = Enumerable.Range(1, 51).Select(i => new CustomFieldRequest($"Field {i} {suffix}", "x")).ToList();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SetFolderCustomFieldsAsync(room.Id, tooMany));

        exception.ErrorCode.Should().Be(400);
        (await GetFolderCustomFieldsAsync(room.Id)).Should().BeEmpty("a refused request writes nothing");
    }

    [Fact]
    public async Task EntryMetadata_ReturnsTheCustomFieldsApartFromTheTemplates()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Contracts " + suffix, [new MetadataFieldRequest("Client", MetadataFieldType.String)]);
        var room = await CreateCustomRoom($"Apart {suffix}");

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(room.Id,
            new SetMetadataValues([new MetadataValueRequest(template.Field("Client").Id, stringValue: "ACME")]), TestContext.Current.CancellationToken);
        await SetFolderCustomFieldAsync(room.Id, "Client", "Globex");

        var templates = (await _metadataApi.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken)).Response.Templates;
        templates.Should().ContainSingle().Which.Id.Should().Be(template.Id, "the hidden template of the custom fields must not appear among the templates");
        templates.Single().Field("Client").Value.StringValue.Should().Be("ACME", "a custom field with the same name as a template field is a different field");

        var customFields = await GetFolderCustomFieldsAsync(room.Id);
        customFields.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Client", Value = "Globex" });
    }

    [Fact]
    public async Task Templates_DoNotIncludeTheTemplateOfTheCustomFields()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Hidden {suffix}");

        await SetFolderCustomFieldAsync(room.Id, $"Project code {suffix}", "A-42");
        var template = await CreateTemplateAsync("Visible " + suffix, [new MetadataFieldRequest("Client", MetadataFieldType.String)]);

        var templates = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        templates.Select(t => t.Id).Should().Equal(new[] { template.Id }, "only the user templates are listed");
        templates.SelectMany(t => t.Fields).Should().NotContain(f => f.Name == $"Project code {suffix}");
    }

    [Fact]
    public async Task CreateTemplate_NamedSystem_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Named {Suffix()}");

        // the hidden template is called "System": the name is unique per tenant, so it is reserved once the custom fields exist
        await SetFolderCustomFieldAsync(room.Id, "Project code", "A-42");
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync("System", [new MetadataFieldRequest("Client", MetadataFieldType.String)]));

        exception.ErrorCode.Should().Be(400, "the name belongs to the hidden template");
    }

    [Fact]
    public async Task CreateTemplate_NamedSystem_BeforeAnyCustomField_ReturnsBadRequest_AndCustomFieldsStillWork()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Reserved {Suffix()}");

        // the name is reserved before the hidden template exists: taken first by a user template, it would make the
        // first custom field write of the tenant fail on the unique name for good
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync("system", [new MetadataFieldRequest("Client", MetadataFieldType.String)]));

        exception.ErrorCode.Should().Be(400, "the name is reserved for the hidden template");

        var fields = await SetFolderCustomFieldAsync(room.Id, "Project code", "A-42");

        fields.Should().ContainSingle(f => f.Name == "Project code" && f.Value == "A-42");
    }

    [Fact]
    public async Task CustomFields_CanBeSetByAnEditor_OnTheFilesTheyEdit_ButNotOnTheFolderOrTheRoom()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Rights {suffix}");
        var folder = await CreateFolder($"Sub {suffix}", room.Id);
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        var member = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Access = FileShare.Editing, Id = member.Id }],
            Notify = false,
            Message = "",
            Culture = "en-US"
        }, cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(member);

        var onFile = await SetFileCustomFieldAsync(file.Id, "Project code", "A-42");
        onFile.Should().ContainSingle().Which.Value.Should().Be("A-42", "an editing member may set custom fields on the files they can edit");

        var onRoom = await Assert.ThrowsAsync<ApiException>(
            async () => await SetFolderCustomFieldsAsync(room.Id, [new CustomFieldRequest("Project code", "A-42")]));
        onRoom.ErrorCode.Should().Be(403, "the room itself is edited by its manager only");

        var onFolder = await Assert.ThrowsAsync<ApiException>(
            async () => await SetFolderCustomFieldsAsync(folder.Id, [new CustomFieldRequest("Project code", "A-42")]));
        onFolder.ErrorCode.Should().Be(403, "the fields of a folder follow the right to create in it, which the editing role lacks");
    }

    [Fact]
    public async Task Rooms_FilteredByCustomFieldName_ReturnTheMatchingRoom()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var field = $"Client {suffix}";
        var matching = await CreateCustomRoom($"Match {suffix}");
        var other = await CreateCustomRoom($"Other {suffix}");

        await SetFolderCustomFieldAsync(matching.Id, field, "ACME");
        await SetFolderCustomFieldAsync(other.Id, field, "Globex");

        // the custom fields have no identifier outside, so the condition names the field instead of the fieldId
        var condition = new MetadataFilterConditionRequest(name: field, op: "eq", value: "ACME");

        var rooms = await PollRoomsAsync(condition, matching.Id);

        rooms.RoomIds().Should().Contain(matching.Id);
        rooms.RoomIds().Should().NotContain(other.Id);
    }

    [Fact]
    public async Task Rooms_FilteredByTheNameOfADroppedCustomField_ReturnBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var field = $"Ephemeral {suffix}";
        var room = await CreateCustomRoom($"Dropped {suffix}");

        await SetFolderCustomFieldAsync(room.Id, field, "x");

        // the field exists while an entry holds a value for it: the listing is answered
        await _roomsApi.GetRoomsWithMetadataAsync(metadataFilters: [new MetadataFilterConditionRequest(name: field, op: "eq", value: "x")], cancellationToken: TestContext.Current.CancellationToken);

        await SetFolderCustomFieldAsync(room.Id, field, null);

        // the last value is gone, so the field is dropped from the dictionary and is unknown to the filter
        var afterDrop = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsWithMetadataAsync(metadataFilters: [new MetadataFilterConditionRequest(name: field, op: "eq", value: "x")], cancellationToken: TestContext.Current.CancellationToken));
        afterDrop.ErrorCode.Should().Be(400);
    }

    private async Task<RoomsContentResponse> PollRoomsAsync(MetadataFilterConditionRequest condition, int expectedRoomId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var rooms = await _roomsApi.GetRoomsWithMetadataAsync(metadataFilters: [condition], cancellationToken: TestContext.Current.CancellationToken);

            if (rooms.RoomIds().Contains(expectedRoomId) || DateTime.UtcNow > deadline)
            {
                return rooms;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    private async Task<MetadataTemplateDto> CreateTemplateAsync(string name, List<MetadataFieldRequest> fields, bool visible = true)
    {
        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    // a null value removes the field: the SDK annotates CustomFieldRequest.value as non-nullable, but it is sent as "value":null
    private async Task<List<CustomFieldValueDto>> SetFolderCustomFieldsAsync(int folderId, List<CustomFieldRequest> fields)
    {
        return (await _metadataApi.SetFolderCustomFieldsAsync(folderId, new SetCustomFields(fields), TestContext.Current.CancellationToken)).Response;
    }

    private Task<List<CustomFieldValueDto>> SetFolderCustomFieldAsync(int folderId, string name, string? value)
    {
        return SetFolderCustomFieldsAsync(folderId, [new CustomFieldRequest(name, value!)]);
    }

    private async Task<List<CustomFieldValueDto>> SetFileCustomFieldAsync(int fileId, string name, string? value)
    {
        return (await _metadataApi.SetFileCustomFieldsAsync(fileId, new SetCustomFields([new CustomFieldRequest(name, value!)]), TestContext.Current.CancellationToken)).Response;
    }

    private async Task<List<CustomFieldValueDto>> GetFolderCustomFieldsAsync(int folderId)
    {
        return (await _metadataApi.GetFolderMetadataAsync(folderId, TestContext.Current.CancellationToken)).Response.CustomFields;
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
