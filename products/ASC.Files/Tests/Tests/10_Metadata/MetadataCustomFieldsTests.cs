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
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Custom {suffix}");

        var set = await api.SetFolderCustomFieldsAsync(room.Id,
            [new CustomFieldPayload("Project code", "A-42"), new CustomFieldPayload("Client", "ACME")],
            TestContext.Current.CancellationToken);

        set.Should().BeEquivalentTo([new { Name = "Project code", Value = "A-42" }, new { Name = "Client", Value = "ACME" }]);

        var stored = await api.GetFolderCustomFieldsAsync(room.Id, TestContext.Current.CancellationToken);
        stored.Should().BeEquivalentTo(set);
    }

    [Fact]
    public async Task SetCustomFields_WithTheSameNameInAnotherCase_ReusesTheField()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Reuse {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await api.SetFolderCustomFieldAsync(room.Id, "Reference", "R-1", TestContext.Current.CancellationToken);
        var onFile = await api.SetFileCustomFieldAsync(file.Id, "reference", "R-2", TestContext.Current.CancellationToken);

        // the field is found by its name regardless of the case, so the file shows the name the field was created with
        onFile.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Reference", Value = "R-2" });
    }

    [Fact]
    public async Task SetCustomFields_LeavesTheFieldsNotListedAlone()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Partial {suffix}");

        await api.SetFolderCustomFieldsAsync(room.Id,
            [new CustomFieldPayload("Project code", "A-42"), new CustomFieldPayload("Client", "ACME")],
            TestContext.Current.CancellationToken);

        var after = await api.SetFolderCustomFieldAsync(room.Id, "Client", "Globex", TestContext.Current.CancellationToken);

        after.Should().BeEquivalentTo([new { Name = "Project code", Value = "A-42" }, new { Name = "Client", Value = "Globex" }]);
    }

    [Fact]
    public async Task SetCustomFields_WithANullValue_RemovesTheFieldFromThisEntryOnly()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Remove {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await api.SetFolderCustomFieldAsync(room.Id, "Project code", "A-42", TestContext.Current.CancellationToken);
        await api.SetFileCustomFieldAsync(file.Id, "Project code", "B-7", TestContext.Current.CancellationToken);

        var after = await api.SetFileCustomFieldAsync(file.Id, "Project code", null, TestContext.Current.CancellationToken);

        after.Should().BeEmpty();
        (await api.GetFolderCustomFieldsAsync(room.Id, TestContext.Current.CancellationToken))
            .Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Project code", Value = "A-42" }, "the other entry keeps the field");
    }

    [Fact]
    public async Task SetCustomFields_RenamesAFieldOnTheEntry_ByRemovingAndAdding()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Rename {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await api.SetFolderCustomFieldAsync(room.Id, "Client", "ACME", TestContext.Current.CancellationToken);
        await api.SetFileCustomFieldAsync(file.Id, "Client", "Globex", TestContext.Current.CancellationToken);

        var renamed = await api.SetFileCustomFieldsAsync(file.Id,
            [new CustomFieldPayload("Client", null), new CustomFieldPayload("Customer", "Globex")],
            TestContext.Current.CancellationToken);

        renamed.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Customer", Value = "Globex" });
        (await api.GetFolderCustomFieldsAsync(room.Id, TestContext.Current.CancellationToken))
            .Should().ContainSingle().Which.Name.Should().Be("Client", "a rename on one entry is not a rename of the tenant's field");
    }

    [Fact]
    public async Task SetCustomFields_RemovingAFieldTheEntryDoesNotHold_IsANoOp()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var room = await CreateCustomRoom($"NoOp {suffix}");

        var result = await api.SetFolderCustomFieldAsync(room.Id, $"Never set {suffix}", null, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SetCustomFields_WithAnEmptyName_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom($"Empty {Suffix()}");

        using var response = await api.SetFolderCustomFieldsResponseAsync(room.Id, [new CustomFieldPayload("  ", "A-42")], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetCustomFields_WithTheSameNameTwice_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom($"Twice {Suffix()}");

        using var response = await api.SetFolderCustomFieldsResponseAsync(room.Id,
            [new CustomFieldPayload("Client", "ACME"), new CustomFieldPayload("client", "Globex")],
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the name is the key of the field, so it cannot be listed twice");
    }

    [Fact]
    public async Task SetCustomFields_BeyondTheLimit_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Limit {suffix}");

        var tooMany = Enumerable.Range(1, 51).Select(i => new CustomFieldPayload($"Field {i} {suffix}", "x")).ToList();

        using var response = await api.SetFolderCustomFieldsResponseAsync(room.Id, tooMany, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await api.GetFolderCustomFieldsAsync(room.Id, TestContext.Current.CancellationToken)).Should().BeEmpty("a refused request writes nothing");
    }

    [Fact]
    public async Task EntryMetadata_ReturnsTheCustomFieldsApartFromTheTemplates()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Contracts " + suffix, [new MetadataFieldPayload { Name = "Client", Type = 0 }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Apart {suffix}");

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = template.Field("Client").Id, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await api.SetFolderCustomFieldAsync(room.Id, "Client", "Globex", TestContext.Current.CancellationToken);

        var templates = await api.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken);
        templates.Should().ContainSingle().Which.Id.Should().Be(template.Id, "the hidden template of the custom fields must not appear among the templates");
        templates.Single().Field("Client").Value!.StringValue.Should().Be("ACME", "a custom field with the same name as a template field is a different field");

        var customFields = await api.GetFolderCustomFieldsAsync(room.Id, TestContext.Current.CancellationToken);
        customFields.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "Client", Value = "Globex" });
    }

    [Fact]
    public async Task Templates_DoNotIncludeTheTemplateOfTheCustomFields()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Hidden {suffix}");

        await api.SetFolderCustomFieldAsync(room.Id, $"Project code {suffix}", "A-42", TestContext.Current.CancellationToken);
        var template = await api.CreateTemplateAsync("Visible " + suffix, [new MetadataFieldPayload { Name = "Client", Type = 0 }], TestContext.Current.CancellationToken);

        var templates = await api.GetTemplatesAsync(TestContext.Current.CancellationToken);

        templates.Select(t => t.Id).Should().Equal(new[] { template.Id }, "only the user templates are listed");
        templates.SelectMany(t => t.Fields).Should().NotContain(f => f.Name == $"Project code {suffix}");
    }

    [Fact]
    public async Task CreateTemplate_NamedSystem_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom($"Named {Suffix()}");

        // the hidden template is called "System": the name is unique per tenant, so it is reserved once the custom fields exist
        await api.SetFolderCustomFieldAsync(room.Id, "Project code", "A-42", TestContext.Current.CancellationToken);
        using var response = await api.CreateTemplateResponseAsync("System", [new MetadataFieldPayload { Name = "Client", Type = 0 }], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the name belongs to the hidden template");
    }

    [Fact]
    public async Task CreateTemplate_NamedSystem_BeforeAnyCustomField_ReturnsBadRequest_AndCustomFieldsStillWork()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom($"Reserved {Suffix()}");

        // the name is reserved before the hidden template exists: taken first by a user template, it would make the
        // first custom field write of the tenant fail on the unique name for good
        using var response = await api.CreateTemplateResponseAsync("system", [new MetadataFieldPayload { Name = "Client", Type = 0 }], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the name is reserved for the hidden template");

        var fields = await api.SetFolderCustomFieldAsync(room.Id, "Project code", "A-42", TestContext.Current.CancellationToken);

        fields.Should().ContainSingle(f => f.Name == "Project code" && f.Value == "A-42");
    }

    [Fact]
    public async Task CustomFields_CanBeSetByAnEditor_OnTheFilesTheyEdit_ButNotOnTheRoom()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var room = await CreateCustomRoom($"Rights {suffix}");
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

        var onFile = await api.SetFileCustomFieldAsync(file.Id, "Project code", "A-42", TestContext.Current.CancellationToken);
        onFile.Should().ContainSingle().Which.Value.Should().Be("A-42", "an editing member may set custom fields on the files they can edit");

        using var onRoom = await api.SetFolderCustomFieldsResponseAsync(room.Id, [new CustomFieldPayload("Project code", "A-42")], TestContext.Current.CancellationToken);
        onRoom.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the room itself is edited by its manager only");
    }

    [Fact]
    public async Task Rooms_FilteredByCustomFieldName_ReturnTheMatchingRoom()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var field = $"Client {suffix}";
        var matching = await CreateCustomRoom($"Match {suffix}");
        var other = await CreateCustomRoom($"Other {suffix}");

        await api.SetFolderCustomFieldAsync(matching.Id, field, "ACME", TestContext.Current.CancellationToken);
        await api.SetFolderCustomFieldAsync(other.Id, field, "Globex", TestContext.Current.CancellationToken);

        // the custom fields have no identifier outside, so the condition names the field instead of the fieldId
        var condition = new { name = field, op = "eq", value = "ACME" };

        var rooms = await PollRoomsAsync(api, condition, matching.Id);

        rooms.RoomIds().Should().Contain(matching.Id);
        rooms.RoomIds().Should().NotContain(other.Id);
    }

    [Fact]
    public async Task Rooms_FilteredByTheNameOfADroppedCustomField_ReturnBadRequest()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var field = $"Ephemeral {suffix}";
        var room = await CreateCustomRoom($"Dropped {suffix}");

        await api.SetFolderCustomFieldAsync(room.Id, field, "x", TestContext.Current.CancellationToken);

        using var whileHeld = await api.GetRoomsResponseAsync(metadataFilters: [new { name = field, op = "eq", value = "x" }], cancellationToken: TestContext.Current.CancellationToken);
        whileHeld.StatusCode.Should().Be(HttpStatusCode.OK, "the field exists while an entry holds a value for it");

        await api.SetFolderCustomFieldAsync(room.Id, field, null, TestContext.Current.CancellationToken);

        // the last value is gone, so the field is dropped from the dictionary and is unknown to the filter
        using var afterDrop = await api.GetRoomsResponseAsync(metadataFilters: [new { name = field, op = "eq", value = "x" }], cancellationToken: TestContext.Current.CancellationToken);
        afterDrop.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<RoomsContentResponse> PollRoomsAsync(MetadataApiClient api, object condition, int expectedRoomId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var rooms = await api.GetRoomsAsync(metadataFilters: [condition], cancellationToken: TestContext.Current.CancellationToken);

            if (rooms.RoomIds().Contains(expectedRoomId) || DateTime.UtcNow > deadline)
            {
                return rooms;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

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
