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
/// Covers the template and field management endpoints: the partial field update must not change what
/// the request does not mention, the route template must own the field, the visibility survives creation.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataTemplateManagementTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateField_WithNameOnly_KeepsTheFieldType()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Rename " + Suffix(), [new MetadataFieldRequest("Amount", MetadataFieldType.Number)]);
        var field = template.Field("Amount");

        // a request without the type must be a rename only: the missing enum used to default to String
        var updated = (await _metadataApi.UpdateFieldAsync(template.Id, field.Id, new UpdateMetadataFieldRequest(name: "Amount USD"), TestContext.Current.CancellationToken)).Response;

        updated.Name.Should().Be("Amount USD");
        updated.Type.Should().Be(MetadataFieldType.Number, "the type was not mentioned in the request and must stay as it was");

        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Field("Amount USD").Type.Should().Be(MetadataFieldType.Number);
    }

    [Fact]
    public async Task UpdateField_WithNameOnly_RenamesTheChoiceField()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Rename choice " + Suffix(),
            [new MetadataFieldRequest("Status", MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: "Draft"), new MetadataFieldOptionRequest(value: "Signed")])]);
        var field = template.Field("Status");

        // used to fail with "Options are supported by choice fields only" because the type defaulted to String
        var updated = (await _metadataApi.UpdateFieldAsync(template.Id, field.Id, new UpdateMetadataFieldRequest(name: "State"), TestContext.Current.CancellationToken)).Response;

        updated.Name.Should().Be("State");
        updated.Type.Should().Be(MetadataFieldType.SingleChoice);
        updated.Options.Select(o => o.Value).Should().BeEquivalentTo(["Draft", "Signed"]);

        // the update must reach the database, not only the response built from the in-memory entity
        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Field("State").Options.Select(o => o.Value).Should().BeEquivalentTo(["Draft", "Signed"]);
    }

    [Fact]
    public async Task UpdateTemplate_PersistsTheNewNameAndVisibility()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Before " + Suffix(), []);
        var newName = "After " + Suffix();

        var updated = (await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate(newName, visible: false), TestContext.Current.CancellationToken)).Response;

        updated.Name.Should().Be(newName);
        updated.Visible.Should().BeFalse();

        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Name.Should().Be(newName, "the rename must be stored, the no-tracking context does not save a loaded entity by itself");
        reloaded.Visible.Should().BeFalse();
    }

    [Fact]
    public async Task GetTemplates_ReportsTheAuthorsAsUsers()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Authored " + Suffix(), []);

        // the author used to be a bare id, and a templates table resolved every author with a request of its own
        var listed = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Single(t => t.Id == template.Id);

        listed.CreateBy.Should().NotBeNull("the author comes as a user object");
        listed.CreateBy.Id.Should().Be(Owner.Id);
        listed.CreateBy.DisplayName.Should().NotBeNullOrEmpty("the display name is what the table shows");
        listed.ModifiedBy.Should().NotBeNull();
        listed.ModifiedBy.Id.Should().Be(Owner.Id, "the creator is the last editor until somebody changes the template");

        var updated = (await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate("Authored again " + Suffix()), TestContext.Current.CancellationToken)).Response;

        updated.ModifiedBy.Should().NotBeNull();
        updated.ModifiedBy.Id.Should().Be(Owner.Id);
        updated.CreateBy.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task UpdateTemplate_ReturnsTheTemplateWithItsFields()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Fields " + Suffix(), [new MetadataFieldRequest("Code", MetadataFieldType.String)]);

        var updated = (await _metadataApi.UpdateTemplateAsync(template.Id, new UpdateMetadataTemplate("Fields renamed " + Suffix()), TestContext.Current.CancellationToken)).Response;

        // the response used to be built from the saved copy, which carries no fields: a client re-rendering the template
        // from the PUT response dropped every field from the UI
        updated.Fields.Should().ContainSingle(f => f.Name == "Code", "the updated template is returned whole, its fields included");
    }

    [Fact]
    public async Task UpdateField_WithExplicitType_ChangesTheTypeOfAnEmptyField()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Retype " + Suffix(), [new MetadataFieldRequest("Code", MetadataFieldType.String)]);
        var field = template.Field("Code");

        var updated = (await _metadataApi.UpdateFieldAsync(template.Id, field.Id, new UpdateMetadataFieldRequest(type: MetadataFieldType.Number), TestContext.Current.CancellationToken)).Response;

        updated.Type.Should().Be(MetadataFieldType.Number);
        updated.Name.Should().Be("Code");
    }

    [Fact]
    public async Task UpdateField_ThroughAnotherTemplateRoute_ReturnsNotFound()
    {
        await _filesClient.Authenticate(Owner);
        var owner = await CreateTemplateAsync("Owner " + Suffix(), [new MetadataFieldRequest("Amount", MetadataFieldType.Number)]);
        var other = await CreateTemplateAsync("Other " + Suffix(), []);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateFieldAsync(other.Id, owner.Field("Amount").Id, new UpdateMetadataFieldRequest(name: "Hijacked"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404, "the field does not belong to the template in the route");

        var reloaded = await GetTemplateAsync(owner.Id);
        reloaded.Field("Amount").Name.Should().Be("Amount");
    }

    [Fact]
    public async Task CreateTemplate_WithVisibleFalse_KeepsItHidden()
    {
        await _filesClient.Authenticate(Owner);

        var template = await CreateTemplateAsync("Hidden " + Suffix(), [], visible: false);

        template.Visible.Should().BeFalse();

        // the flag used to be lost on insert: the column default won over the explicit false
        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Visible.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateField_RemovingAnUnusedOption_Succeeds_WhileAnotherOptionIsSelected()
    {
        var (template, field) = await ArrangeChoiceFieldWithSelectedOptionAsync("Draft");

        // any value of the field used to protect every option: the unused "Signed" could not be dropped while "Draft"
        // was selected somewhere, the field had to be cleared on every entry first
        var updated = (await _metadataApi.UpdateFieldAsync(template.Id, field.Id,
            new UpdateMetadataFieldRequest(options: [new MetadataFieldOptionRequest(field.Option("Draft"), "Draft")]), TestContext.Current.CancellationToken)).Response;

        updated.Options.Select(o => o.Value).Should().Equal("Draft");

        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Field("Status").Options.Select(o => o.Value).Should().Equal("Draft");
    }

    [Fact]
    public async Task UpdateField_RemovingASelectedOption_ReturnsBadRequest()
    {
        var (template, field) = await ArrangeChoiceFieldWithSelectedOptionAsync("Draft");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.UpdateFieldAsync(template.Id, field.Id,
                new UpdateMetadataFieldRequest(options: [new MetadataFieldOptionRequest(field.Option("Signed"), "Signed")]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400, "the removed option is selected on an entry");

        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Field("Status").Options.Select(o => o.Value).Should().BeEquivalentTo(["Draft", "Signed"]);
    }

    [Fact]
    public async Task ChangingAField_MovesTheModifiedDateOfTheTemplate()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Touched " + Suffix(), [new MetadataFieldRequest("Client", MetadataFieldType.String)]);
        var modifiedOn = template.ModifiedOn.UtcTime;

        // the fields are part of the template, so a field added, changed or removed used to leave the template's
        // "last modified" where the last rename had put it
        await NextSecondAsync();
        var created = (await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("Amount", MetadataFieldType.Number), TestContext.Current.CancellationToken)).Response;
        modifiedOn = await AssertTemplateModifiedAfterAsync(template.Id, modifiedOn, "a field was added");

        await NextSecondAsync();
        await _metadataApi.UpdateFieldAsync(template.Id, created.Id, new UpdateMetadataFieldRequest(name: "Amount USD"), TestContext.Current.CancellationToken);
        modifiedOn = await AssertTemplateModifiedAfterAsync(template.Id, modifiedOn, "a field was renamed");

        await NextSecondAsync();
        await _metadataApi.DeleteFieldAsync(template.Id, created.Id, TestContext.Current.CancellationToken);
        await AssertTemplateModifiedAfterAsync(template.Id, modifiedOn, "a field was removed");
    }

    [Fact]
    public async Task SetValues_LeavesTheModifiedDateOfTheTemplate()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Untouched " + suffix, [new MetadataFieldRequest("Client", MetadataFieldType.String)]);
        var room = await CreateCustomRoom($"Untouched {suffix}");
        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);

        // read back rather than taken from the create response: the stored date has a precision of one second
        var modifiedOn = (await GetTemplateAsync(template.Id)).ModifiedOn.UtcTime;

        // a value is a change of the entry, not of the template
        await NextSecondAsync();
        await _metadataApi.SetFolderValuesAsync(room.Id,
            new SetMetadataValues([new MetadataValueRequest(template.Field("Client").Id, stringValue: "ACME")]), TestContext.Current.CancellationToken);

        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.ModifiedOn.UtcTime.Should().Be(modifiedOn);
    }

    [Fact]
    public async Task SetValues_ReturnsEveryValueTheEntryHolds_NotOnlyTheOnesSent()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Whole " + suffix,
            [new MetadataFieldRequest("Client", MetadataFieldType.String), new MetadataFieldRequest("Amount", MetadataFieldType.Number)]);
        var room = await CreateCustomRoom($"Whole {suffix}");

        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(room.Id,
            new SetMetadataValues([new MetadataValueRequest(template.Field("Client").Id, stringValue: "ACME")]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderCustomFieldsAsync(room.Id, new SetCustomFields([new CustomFieldRequest("Project code", "A-42")]), TestContext.Current.CancellationToken);

        var result = (await _metadataApi.SetFolderValuesAsync(room.Id,
            new SetMetadataValues([new MetadataValueRequest(template.Field("Amount").Id, numberValue: 7)]), TestContext.Current.CancellationToken)).Response;

        // the answer is the state of the entry in the shape of the read, so the client needs no second request
        var stored = result.Templates.Should().ContainSingle().Which;
        stored.Id.Should().Be(template.Id);
        stored.Field("Client").Value.StringValue.Should().Be("ACME", "a value written earlier is part of the answer, not only the values of this write");
        stored.Field("Amount").Value.NumberValue.Should().Be(7);
        result.CustomFields.Should().ContainSingle().Which.Name.Should().Be("Project code");
    }

    [Fact]
    public async Task SetValues_WithTheSameFieldTwice_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Twice " + suffix, [new MetadataFieldRequest("Client", MetadataFieldType.String)]);
        var room = await CreateCustomRoom($"Twice {suffix}");
        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);

        var fieldId = template.Field("Client").Id;

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderValuesAsync(room.Id, new SetMetadataValues(
            [
                new MetadataValueRequest(fieldId, stringValue: "ACME"),
                new MetadataValueRequest(fieldId, stringValue: "Globex")
            ]), TestContext.Current.CancellationToken));

        // two rows with one key in a single write: rejected up front, not left to the database as a server error
        exception.ErrorCode.Should().Be(400, "a field is listed twice");
    }

    [Fact]
    public async Task AssignFolderTemplates_WithAnEmptyListAndCascade_AssignsNothing()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Empty {Suffix()}");

        // nothing to assign: the answer is a completed operation, not a pass over the room
        var operation = (await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([], cascade: true), TestContext.Current.CancellationToken)).Response;

        operation.IsCompleted.Should().BeTrue();
        operation.Error.Should().BeNullOrEmpty();
        (await _metadataApi.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken)).Response.Templates.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTemplate_ConcurrentlyWithTheSameName_CreatesOnlyOne()
    {
        await _filesClient.Authenticate(Owner);
        var name = "Race " + Suffix();

        // the name check is a check-then-insert, so the parallel creates all pass it; the unique index must stop all but one
        var statusCodes = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            try
            {
                await CreateTemplateAsync(name, [new MetadataFieldRequest("Client", MetadataFieldType.String)]);

                return 200;
            }
            catch (ApiException exception)
            {
                return exception.ErrorCode;
            }
        }));

        statusCodes.Count(c => c == 200).Should().Be(1, "only one template with the name may exist");
        statusCodes.Where(c => c != 200).Should().OnlyContain(c => c == 400, "a collision is a client error, not a server one");

        var templates = (await _metadataApi.GetTemplatesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        templates.Count(t => t.Name == name).Should().Be(1);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task CreateTemplate_ByANonDocSpaceAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        await _filesClient.Authenticate(Owner);
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(member);

        // the templates are the vocabulary of the whole portal: a room admin used to be allowed to create one
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateTemplateAsync("Forbidden " + Suffix(), [new MetadataFieldRequest("Client", MetadataFieldType.String)]));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// The modified date is stored with a precision of one second: a change made in the same second as the previous one
    /// would not move it.
    /// </summary>
    private static Task NextSecondAsync()
    {
        return Task.Delay(TimeSpan.FromMilliseconds(1100), TestContext.Current.CancellationToken);
    }

    private async Task<DateTime> AssertTemplateModifiedAfterAsync(int templateId, DateTime previous, string because)
    {
        var reloaded = await GetTemplateAsync(templateId);

        reloaded.ModifiedOn.UtcTime.Should().BeAfter(previous, because);
        reloaded.ModifiedBy.Id.Should().Be(Owner.Id);

        return reloaded.ModifiedOn.UtcTime;
    }

    private async Task<MetadataTemplateDto> CreateTemplateAsync(string name, List<MetadataFieldRequest> fields, bool visible = true)
    {
        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    private async Task<MetadataTemplateDto> GetTemplateAsync(int templateId)
    {
        return (await _metadataApi.GetTemplateAsync(templateId, TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// A template with the choice field Status (Draft, Signed) assigned to a room that has <paramref name="selectedOption"/> selected.
    /// </summary>
    private async Task<(MetadataTemplateDto Template, MetadataFieldDto Field)> ArrangeChoiceFieldWithSelectedOptionAsync(string selectedOption)
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();

        var template = await CreateTemplateAsync("Options " + suffix,
            [new MetadataFieldRequest("Status", MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: "Draft"), new MetadataFieldOptionRequest(value: "Signed")])]);
        var field = template.Field("Status");

        var room = await CreateCustomRoom($"Options {suffix}");
        await _metadataApi.AssignFolderTemplatesAsync(room.Id, new AssignMetadataTemplates([template.Id], cascade: false), TestContext.Current.CancellationToken);
        await _metadataApi.SetFolderValuesAsync(room.Id,
            new SetMetadataValues([new MetadataValueRequest(field.Id, optionIds: [field.Option(selectedOption)])]), TestContext.Current.CancellationToken);

        return (template, field);
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
