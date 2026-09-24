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
    private const int StringType = 0;
    private const int NumberType = 2;
    private const int SingleChoiceType = 3;

    [Fact]
    public async Task UpdateField_WithNameOnly_KeepsTheFieldType()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Rename " + Suffix(),
            [new MetadataFieldPayload { Name = "Amount", Type = NumberType }], TestContext.Current.CancellationToken);
        var field = template.Field("Amount");

        // a request without the type must be a rename only: the missing enum used to default to String
        var updated = await api.UpdateFieldAsync(template.Id, field.Id, new { name = "Amount USD" }, TestContext.Current.CancellationToken);

        updated.Name.Should().Be("Amount USD");
        updated.Type.Should().Be(NumberType, "the type was not mentioned in the request and must stay as it was");

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Field("Amount USD").Type.Should().Be(NumberType);
    }

    [Fact]
    public async Task UpdateField_WithNameOnly_RenamesTheChoiceField()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Rename choice " + Suffix(),
        [
            new MetadataFieldPayload
            {
                Name = "Status",
                Type = SingleChoiceType,
                Options = [new MetadataFieldOptionPayload { Value = "Draft" }, new MetadataFieldOptionPayload { Value = "Signed" }]
            }
        ], TestContext.Current.CancellationToken);
        var field = template.Field("Status");

        // used to fail with "Options are supported by choice fields only" because the type defaulted to String
        var updated = await api.UpdateFieldAsync(template.Id, field.Id, new { name = "State" }, TestContext.Current.CancellationToken);

        updated.Name.Should().Be("State");
        updated.Type.Should().Be(SingleChoiceType);
        updated.Options.Select(o => o.Value).Should().BeEquivalentTo(["Draft", "Signed"]);

        // the update must reach the database, not only the response built from the in-memory entity
        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Field("State").Options.Select(o => o.Value).Should().BeEquivalentTo(["Draft", "Signed"]);
    }

    [Fact]
    public async Task UpdateTemplate_PersistsTheNewNameAndVisibility()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Before " + Suffix(), [], TestContext.Current.CancellationToken);
        var newName = "After " + Suffix();

        var updated = await api.UpdateTemplateAsync(template.Id, new { name = newName, visible = false }, TestContext.Current.CancellationToken);

        updated.Name.Should().Be(newName);
        updated.Visible.Should().BeFalse();

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Name.Should().Be(newName, "the rename must be stored, the no-tracking context does not save a loaded entity by itself");
        reloaded.Visible.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTemplate_ReturnsTheTemplateWithItsFields()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Fields " + Suffix(),
            [new MetadataFieldPayload { Name = "Code", Type = StringType }], TestContext.Current.CancellationToken);

        var updated = await api.UpdateTemplateAsync(template.Id, new { name = "Fields renamed " + Suffix() }, TestContext.Current.CancellationToken);

        // the response used to be built from the saved copy, which carries no fields: a client re-rendering the template
        // from the PUT response dropped every field from the UI
        updated.Fields.Should().ContainSingle(f => f.Name == "Code", "the updated template is returned whole, its fields included");
    }

    [Fact]
    public async Task UpdateField_WithExplicitType_ChangesTheTypeOfAnEmptyField()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Retype " + Suffix(),
            [new MetadataFieldPayload { Name = "Code", Type = StringType }], TestContext.Current.CancellationToken);
        var field = template.Field("Code");

        var updated = await api.UpdateFieldAsync(template.Id, field.Id, new { type = NumberType }, TestContext.Current.CancellationToken);

        updated.Type.Should().Be(NumberType);
        updated.Name.Should().Be("Code");
    }

    [Fact]
    public async Task UpdateField_ThroughAnotherTemplateRoute_ReturnsNotFound()
    {
        var api = await ArrangeAsync();
        var owner = await api.CreateTemplateAsync("Owner " + Suffix(),
            [new MetadataFieldPayload { Name = "Amount", Type = NumberType }], TestContext.Current.CancellationToken);
        var other = await api.CreateTemplateAsync("Other " + Suffix(), [], TestContext.Current.CancellationToken);

        using var response = await api.UpdateFieldResponseAsync(other.Id, owner.Field("Amount").Id, new { name = "Hijacked" }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "the field does not belong to the template in the route");

        var reloaded = await api.GetTemplateAsync(owner.Id, TestContext.Current.CancellationToken);
        reloaded.Field("Amount").Name.Should().Be("Amount");
    }

    [Fact]
    public async Task CreateTemplate_WithVisibleFalse_KeepsItHidden()
    {
        var api = await ArrangeAsync();

        var template = await api.CreateTemplateAsync("Hidden " + Suffix(), [], TestContext.Current.CancellationToken, visible: false);

        template.Visible.Should().BeFalse();

        // the flag used to be lost on insert: the column default won over the explicit false
        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Visible.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateField_RemovingAnUnusedOption_Succeeds_WhileAnotherOptionIsSelected()
    {
        var (api, template, field) = await ArrangeChoiceFieldWithSelectedOptionAsync("Draft");

        // any value of the field used to protect every option: the unused "Signed" could not be dropped while "Draft"
        // was selected somewhere, the field had to be cleared on every entry first
        var updated = await api.UpdateFieldAsync(template.Id, field.Id,
            new { options = new[] { new { id = field.Option("Draft"), value = "Draft" } } }, TestContext.Current.CancellationToken);

        updated.Options.Select(o => o.Value).Should().Equal("Draft");

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Field("Status").Options.Select(o => o.Value).Should().Equal("Draft");
    }

    [Fact]
    public async Task UpdateField_RemovingASelectedOption_ReturnsBadRequest()
    {
        var (api, template, field) = await ArrangeChoiceFieldWithSelectedOptionAsync("Draft");

        using var response = await api.UpdateFieldResponseAsync(template.Id, field.Id,
            new { options = new[] { new { id = field.Option("Signed"), value = "Signed" } } }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the removed option is selected on an entry");

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Field("Status").Options.Select(o => o.Value).Should().BeEquivalentTo(["Draft", "Signed"]);
    }

    private async Task<MetadataApiClient> ArrangeAsync()
    {
        await _filesClient.Authenticate(Owner);

        return new MetadataApiClient(_filesClient);
    }

    /// <summary>
    /// A template with the choice field Status (Draft, Signed) assigned to a room that has <paramref name="selectedOption"/> selected.
    /// </summary>
    private async Task<(MetadataApiClient Api, MetadataTemplateResponse Template, MetadataFieldResponse Field)> ArrangeChoiceFieldWithSelectedOptionAsync(string selectedOption)
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();

        var template = await api.CreateTemplateAsync("Options " + suffix,
        [
            new MetadataFieldPayload
            {
                Name = "Status",
                Type = SingleChoiceType,
                Options = [new MetadataFieldOptionPayload { Value = "Draft" }, new MetadataFieldOptionPayload { Value = "Signed" }]
            }
        ], TestContext.Current.CancellationToken);
        var field = template.Field("Status");

        var room = await CreateCustomRoom($"Options {suffix}");
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = field.Id, OptionIds = [field.Option(selectedOption)] }], TestContext.Current.CancellationToken);

        return (api, template, field);
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }

    [Fact]
    public async Task SetValues_ReturnsEveryValueTheEntryHolds_NotOnlyTheOnesSent()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Whole " + suffix,
            [new MetadataFieldPayload { Name = "Client", Type = StringType }, new MetadataFieldPayload { Name = "Amount", Type = NumberType }],
            TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Whole {suffix}");

        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);
        await api.SetFolderValuesAsync(room.Id, [new MetadataValuePayload { FieldId = template.Field("Client").Id, StringValue = "ACME" }], TestContext.Current.CancellationToken);
        await api.SetFolderCustomFieldAsync(room.Id, "Project code", "A-42", TestContext.Current.CancellationToken);

        var result = await api.SetFolderValuesWithResultAsync(room.Id, [new MetadataValuePayload { FieldId = template.Field("Amount").Id, NumberValue = 7 }], TestContext.Current.CancellationToken);

        // the answer is the state of the entry in the shape of the read, so the client needs no second request
        var stored = result.Templates.Should().ContainSingle().Which;
        stored.Id.Should().Be(template.Id);
        stored.Field("Client").Value!.StringValue.Should().Be("ACME", "a value written earlier is part of the answer, not only the values of this write");
        stored.Field("Amount").Value!.NumberValue.Should().Be(7);
        result.CustomFields.Should().ContainSingle().Which.Name.Should().Be("Project code");
    }


    [Fact]
    public async Task SetValues_WithTheSameFieldTwice_ReturnsBadRequest()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Twice " + suffix, [new MetadataFieldPayload { Name = "Client", Type = StringType }], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Twice {suffix}");
        await api.AssignFolderTemplatesAsync(room.Id, [template.Id], cascade: false, TestContext.Current.CancellationToken);

        var fieldId = template.Field("Client").Id;

        using var response = await api.SetFolderValuesResponseAsync(room.Id,
        [
            new MetadataValuePayload { FieldId = fieldId, StringValue = "ACME" },
            new MetadataValuePayload { FieldId = fieldId, StringValue = "Globex" }
        ], TestContext.Current.CancellationToken);

        // two rows with one key in a single write: rejected up front, not left to the database as a server error
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "a field is listed twice");
    }

    [Fact]
    public async Task AssignFolderTemplates_WithAnEmptyListAndCascade_AssignsNothing()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom($"Empty {Suffix()}");

        // nothing to assign: the answer is a completed operation, not a pass over the room
        var operation = await api.AssignFolderTemplatesWithStatusAsync(room.Id, [], cascade: true, TestContext.Current.CancellationToken);

        operation.IsCompleted.Should().BeTrue();
        operation.Error.Should().BeNullOrEmpty();
        (await api.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTemplate_ConcurrentlyWithTheSameName_CreatesOnlyOne()
    {
        var api = await ArrangeAsync();
        var name = "Race " + Suffix();

        // the name check is a check-then-insert, so the parallel creates all pass it; the unique index must stop all but one
        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => api.CreateTemplateResponseAsync(name, [new MetadataFieldPayload { Name = "Client", Type = StringType }], TestContext.Current.CancellationToken)));

        try
        {
            responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1, "only one template with the name may exist");
            responses.Where(r => r.StatusCode != HttpStatusCode.OK).Should().OnlyContain(r => r.StatusCode == HttpStatusCode.BadRequest, "a collision is a client error, not a server one");

            var templates = await api.GetTemplatesAsync(TestContext.Current.CancellationToken);
            templates.Count(t => t.Name == name).Should().Be(1);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task CreateTemplate_ByANonDocSpaceAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        var api = await ArrangeAsync();
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(member);

        // the templates are the vocabulary of the whole portal: a room admin used to be allowed to create one
        using var response = await api.CreateTemplateResponseAsync("Forbidden " + Suffix(), [new MetadataFieldPayload { Name = "Client", Type = StringType }], TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

}
