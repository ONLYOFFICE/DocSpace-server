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
