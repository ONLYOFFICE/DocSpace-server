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
    [Fact]
    public async Task CreateTemplate_FieldsWithoutAPosition_FollowThePositionedOnesInRequestOrder()
    {
        await _filesClient.Authenticate(Owner);

        // the position used to default to zero, which put every unpositioned field first, in an order the database chose
        var template = await CreateTemplateAsync("Positions " + Suffix(),
        [
            new MetadataFieldRequest("Third", MetadataFieldType.String),
            new MetadataFieldRequest("First", MetadataFieldType.String, order: 0),
            new MetadataFieldRequest("Fourth", MetadataFieldType.String),
            new MetadataFieldRequest("Second", MetadataFieldType.String, order: 1)
        ]);

        template.Fields.Select(f => f.Name).Should().Equal("First", "Second", "Third", "Fourth");

        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Fields.Select(f => f.Name).Should().Equal("First", "Second", "Third", "Fourth");
    }

    [Fact]
    public async Task CreateField_WithoutAPosition_GoesAfterTheLastField()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Append " + Suffix(),
            [new MetadataFieldRequest("A", MetadataFieldType.String, order: 5), new MetadataFieldRequest("B", MetadataFieldType.String, order: 2)]);

        var added = (await _metadataApi.CreateFieldAsync(template.Id, new MetadataFieldRequest("C", MetadataFieldType.String), TestContext.Current.CancellationToken)).Response;

        added.Order.Should().Be(6, "the new field follows the highest position of the template");

        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Fields.Select(f => f.Name).Should().Equal("B", "A", "C");
    }

    [Fact]
    public async Task UpdateField_WithAPosition_MovesTheFieldInTheTemplate()
    {
        await _filesClient.Authenticate(Owner);
        var template = await CreateTemplateAsync("Move " + Suffix(),
            [new MetadataFieldRequest("A", MetadataFieldType.String), new MetadataFieldRequest("B", MetadataFieldType.String), new MetadataFieldRequest("C", MetadataFieldType.String)]);

        template.Fields.Select(f => f.Name).Should().Equal(["A", "B", "C"], "the fields without a position follow the request");

        var moved = (await _metadataApi.UpdateFieldAsync(template.Id, template.Field("A").Id, new UpdateMetadataFieldRequest(order: 5), TestContext.Current.CancellationToken)).Response;

        moved.Order.Should().Be(5);

        var reloaded = await GetTemplateAsync(template.Id);
        reloaded.Fields.Select(f => f.Name).Should().Equal(["B", "C", "A"], "the re-positioned field is read where its position puts it");

        // an update without a position keeps it: the rename must not move the field back
        await _metadataApi.UpdateFieldAsync(template.Id, template.Field("A").Id, new UpdateMetadataFieldRequest(name: "A renamed"), TestContext.Current.CancellationToken);

        reloaded = await GetTemplateAsync(template.Id);
        reloaded.Fields.Select(f => f.Name).Should().Equal("B", "C", "A renamed");
    }

    [Fact]
    public async Task GetEntryMetadata_ListsTheFieldsByTheirPosition()
    {
        await _filesClient.Authenticate(Owner);
        var suffix = Suffix();
        var template = await CreateTemplateAsync("Entry order " + suffix,
        [
            new MetadataFieldRequest("Second", MetadataFieldType.String, order: 1),
            new MetadataFieldRequest("Third", MetadataFieldType.String),
            new MetadataFieldRequest("First", MetadataFieldType.String, order: 0)
        ]);
        var room = await CreateCustomRoom($"Entry order {suffix}");
        var file = await CreateFile($"doc-{suffix}.docx", room.Id);

        await _metadataApi.AssignFileTemplatesAsync(file.Id, new AssignMetadataTemplates([template.Id]), TestContext.Current.CancellationToken);
        await _metadataApi.SetFileValuesAsync(file.Id,
            new SetMetadataValues([new MetadataValueRequest(template.Field("Third").Id, stringValue: "x")]), TestContext.Current.CancellationToken);

        // the metadata of the entry lists every field of the template, the ones without a value included, in the template's order
        var metadata = (await _metadataApi.GetFileMetadataAsync(file.Id, TestContext.Current.CancellationToken)).Response.Templates;
        metadata.Single(t => t.Id == template.Id).Fields.Select(f => f.Name).Should().Equal("First", "Second", "Third");
    }

    [Fact]
    public async Task CustomFields_ComeBackInTheOrderOfTheirCreation_AcrossRemovals()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Custom order " + Suffix());

        var set = await SetCustomFieldsAsync(room.Id, [new CustomFieldRequest("A", "1"), new CustomFieldRequest("B", "2"), new CustomFieldRequest("C", "3")]);

        set.Select(f => f.Name).Should().Equal(["A", "B", "C"], "the fields of one request are created in its order");

        // the removed field is dropped from the portal (no entry holds it), the later ones keep their place
        await SetCustomFieldsAsync(room.Id, [new CustomFieldRequest("B", null!)]);
        var afterRemoval = await SetCustomFieldsAsync(room.Id, [new CustomFieldRequest("D", "4")]);

        afterRemoval.Select(f => f.Name).Should().Equal(["A", "C", "D"], "a new field goes after the existing ones");

        // a name that was dropped comes back as a new field, so it takes the last place again
        var readded = await SetCustomFieldsAsync(room.Id, [new CustomFieldRequest("B", "5")]);

        readded.Select(f => f.Name).Should().Equal("A", "C", "D", "B");

        var read = (await _metadataApi.GetFolderMetadataAsync(room.Id, TestContext.Current.CancellationToken)).Response.CustomFields;
        read.Select(f => f.Name).Should().Equal(["A", "C", "D", "B"], "the read agrees with the write");

        // two fields dropped at once: the count of the remaining fields is below their positions, and the new field
        // used to take a position in between instead of the last one
        await SetCustomFieldsAsync(room.Id, [new CustomFieldRequest("A", null!), new CustomFieldRequest("C", null!)]);
        var afterTwoRemovals = await SetCustomFieldsAsync(room.Id, [new CustomFieldRequest("E", "6")]);

        afterTwoRemovals.Select(f => f.Name).Should().Equal(["D", "B", "E"], "the new field goes after the last one whatever the count");
    }

    private async Task<MetadataTemplateDto> CreateTemplateAsync(string name, List<MetadataFieldRequest> fields, bool visible = true)
    {
        return (await _metadataApi.CreateTemplateAsync(new CreateMetadataTemplateRequestDto(name, visible, fields), TestContext.Current.CancellationToken)).Response;
    }

    private async Task<MetadataTemplateDto> GetTemplateAsync(int templateId)
    {
        return (await _metadataApi.GetTemplateAsync(templateId, TestContext.Current.CancellationToken)).Response;
    }

    private async Task<List<CustomFieldValueDto>> SetCustomFieldsAsync(int folderId, List<CustomFieldRequest> fields)
    {
        return (await _metadataApi.SetFolderCustomFieldsAsync(folderId, new SetCustomFields(fields), TestContext.Current.CancellationToken)).Response;
    }

    private static string Suffix()
    {
        return Guid.NewGuid().ToString()[..8];
    }
}
