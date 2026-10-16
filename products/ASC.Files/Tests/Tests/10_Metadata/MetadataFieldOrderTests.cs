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
/// Covers the order the fields come back in: the position a template field is created with or without, a
/// re-positioning through the field update, the same order inside the metadata of an entry, and the custom fields
/// of an entry, which have no position of their own and follow their creation.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataFieldOrderTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int StringType = 0;

    [Fact]
    public async Task CreateTemplate_FieldsWithoutAPosition_FollowThePositionedOnesInRequestOrder()
    {
        var api = await ArrangeAsync();

        // the position used to default to zero, which put every unpositioned field first, in an order the database chose
        var template = await api.CreateTemplateAsync("Positions " + Suffix(),
        [
            new MetadataFieldPayload { Name = "Third", Type = StringType },
            new MetadataFieldPayload { Name = "First", Type = StringType, Order = 0 },
            new MetadataFieldPayload { Name = "Fourth", Type = StringType },
            new MetadataFieldPayload { Name = "Second", Type = StringType, Order = 1 }
        ], TestContext.Current.CancellationToken);

        template.Fields.Select(f => f.Name).Should().Equal("First", "Second", "Third", "Fourth");

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Fields.Select(f => f.Name).Should().Equal("First", "Second", "Third", "Fourth");
    }

    [Fact]
    public async Task CreateField_WithoutAPosition_GoesAfterTheLastField()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Append " + Suffix(),
            [new MetadataFieldPayload { Name = "A", Type = StringType, Order = 5 }, new MetadataFieldPayload { Name = "B", Type = StringType, Order = 2 }],
            TestContext.Current.CancellationToken);

        var added = await api.CreateFieldAsync(template.Id, new MetadataFieldPayload { Name = "C", Type = StringType }, TestContext.Current.CancellationToken);

        added.Order.Should().Be(6, "the new field follows the highest position of the template");

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Fields.Select(f => f.Name).Should().Equal("B", "A", "C");
    }

    [Fact]
    public async Task UpdateField_WithAPosition_MovesTheFieldInTheTemplate()
    {
        var api = await ArrangeAsync();
        var template = await api.CreateTemplateAsync("Move " + Suffix(),
            [new MetadataFieldPayload { Name = "A", Type = StringType }, new MetadataFieldPayload { Name = "B", Type = StringType }, new MetadataFieldPayload { Name = "C", Type = StringType }],
            TestContext.Current.CancellationToken);

        template.Fields.Select(f => f.Name).Should().Equal(["A", "B", "C"], "the fields without a position follow the request");

        var moved = await api.UpdateFieldAsync(template.Id, template.Field("A").Id, new { order = 5 }, TestContext.Current.CancellationToken);

        moved.Order.Should().Be(5);

        var reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Fields.Select(f => f.Name).Should().Equal(["B", "C", "A"], "the re-positioned field is read where its position puts it");

        // an update without a position keeps it: the rename must not move the field back
        await api.UpdateFieldAsync(template.Id, template.Field("A").Id, new { name = "A renamed" }, TestContext.Current.CancellationToken);

        reloaded = await api.GetTemplateAsync(template.Id, TestContext.Current.CancellationToken);
        reloaded.Fields.Select(f => f.Name).Should().Equal("B", "C", "A renamed");
    }

    [Fact]
    public async Task GetEntryMetadata_ListsTheFieldsByTheirPosition()
    {
        var api = await ArrangeAsync();
        var suffix = Suffix();
        var template = await api.CreateTemplateAsync("Entry order " + suffix,
        [
            new MetadataFieldPayload { Name = "Second", Type = StringType, Order = 1 },
            new MetadataFieldPayload { Name = "Third", Type = StringType },
            new MetadataFieldPayload { Name = "First", Type = StringType, Order = 0 }
        ], TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom($"Entry order {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await api.AssignFileTemplatesAsync(file.Id, [template.Id], TestContext.Current.CancellationToken);
        await api.SetFileValuesAsync(file.Id, [new MetadataValuePayload { FieldId = template.Field("Third").Id, StringValue = "x" }], TestContext.Current.CancellationToken);

        // the metadata of the entry lists every field of the template, the ones without a value included, in the template's order
        var metadata = await api.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken);
        metadata.Single(t => t.Id == template.Id).Fields.Select(f => f.Name).Should().Equal("First", "Second", "Third");
    }

    [Fact]
    public async Task CustomFields_ComeBackInTheOrderOfTheirCreation_AcrossRemovals()
    {
        var api = await ArrangeAsync();
        var room = await CreateCustomRoom("Custom order " + Suffix());

        var set = await api.SetFolderCustomFieldsAsync(room.Id,
            [new CustomFieldPayload("A", "1"), new CustomFieldPayload("B", "2"), new CustomFieldPayload("C", "3")],
            TestContext.Current.CancellationToken);

        set.Select(f => f.Name).Should().Equal(["A", "B", "C"], "the fields of one request are created in its order");

        // the removed field is dropped from the portal (no entry holds it), the later ones keep their place
        await api.SetFolderCustomFieldAsync(room.Id, "B", null, TestContext.Current.CancellationToken);
        var afterRemoval = await api.SetFolderCustomFieldAsync(room.Id, "D", "4", TestContext.Current.CancellationToken);

        afterRemoval.Select(f => f.Name).Should().Equal(["A", "C", "D"], "a new field goes after the existing ones");

        // a name that was dropped comes back as a new field, so it takes the last place again
        var readded = await api.SetFolderCustomFieldAsync(room.Id, "B", "5", TestContext.Current.CancellationToken);

        readded.Select(f => f.Name).Should().Equal("A", "C", "D", "B");

        var read = await api.GetFolderCustomFieldsAsync(room.Id, TestContext.Current.CancellationToken);
        read.Select(f => f.Name).Should().Equal(["A", "C", "D", "B"], "the read agrees with the write");

        // two fields dropped at once: the count of the remaining fields is below their positions, and the new field
        // used to take a position in between instead of the last one
        await api.SetFolderCustomFieldsAsync(room.Id, [new CustomFieldPayload("A", null), new CustomFieldPayload("C", null)], TestContext.Current.CancellationToken);
        var afterTwoRemovals = await api.SetFolderCustomFieldAsync(room.Id, "E", "6", TestContext.Current.CancellationToken);

        afterTwoRemovals.Select(f => f.Name).Should().Equal(["D", "B", "E"], "the new field goes after the last one whatever the count");
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
