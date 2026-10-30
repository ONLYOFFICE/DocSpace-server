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
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the values and the custom fields of a folder, and the metadata read of a folder that holds none.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataFolderValuesTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task SetFolderValues_OfEveryType_Partial_KeepsTheRest_AndAnEmptyStringClears()
    {
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);
        var template = await CreateTemplateAsync(
        [
            new MetadataFieldRequest("s", MetadataFieldType.String),
            new MetadataFieldRequest("n", MetadataFieldType.Number),
            new MetadataFieldRequest("d", MetadataFieldType.Date),
            new MetadataFieldRequest("c", MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: "one"), new MetadataFieldOptionRequest(value: "two")])
        ]);

        await AssignToFolderAsync(folder.Id, template.Id);

        var two = template.Field("c").Option("two");
        var date = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var written = (await _metadataApi.SetFolderValuesAsync(folder.Id, new SetMetadataValues(
        [
            new MetadataValueRequest(template.Field("s").Id, stringValue: "текст"),
            new MetadataValueRequest(template.Field("n").Id, numberValue: 7),
            new MetadataValueRequest(template.Field("d").Id, dateValue: date),
            new MetadataValueRequest(template.Field("c").Id, optionIds: [two])
        ]), TestContext.Current.CancellationToken)).Response;

        ValueOf(written, template.Id, template.Field("s").Id)!.StringValue.Should().Be("текст");

        await SetFolderStringAsync(folder.Id, template.Field("s").Id, "");

        var reloaded = await GetFolderMetadataAsync(folder.Id);

        ValueOf(reloaded, template.Id, template.Field("s").Id)?.StringValue.Should().BeNullOrEmpty();
        ValueOf(reloaded, template.Id, template.Field("n").Id)!.NumberValue.Should().Be(7);
        ValueOf(reloaded, template.Id, template.Field("d").Id)!.DateValue.UtcTime.Should().Be(date);
        ValueOf(reloaded, template.Id, template.Field("c").Id)!.OptionIds.Should().Equal(two);
    }

    [Fact]
    public async Task SetFolderValues_WithAFieldOfATemplateTheFolderDoesNotCarry_ReturnsBadRequestAndWritesNothing()
    {
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);
        var mine = await CreateTemplateAsync();
        var other = await CreateTemplateAsync();
        var mineField = mine.Fields.Single().Id;

        await AssignToFolderAsync(folder.Id, mine.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderValuesAsync(folder.Id, new SetMetadataValues(
            [
                new MetadataValueRequest(mineField, stringValue: "x"),
                new MetadataValueRequest(other.Fields.Single().Id, stringValue: "y")
            ]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        ValueOf(await GetFolderMetadataAsync(folder.Id), mine.Id, mineField).Should().BeNull();
    }

    [Fact]
    public async Task SetFolderCustomFields_SetsAndRemoves_AndNeverCascadesToTheContent()
    {
        var root = await CreateFolderInMy($"root-{Suffix()}", Owner);
        var file = await CreateFile($"doc-{Suffix()}.docx", root.Id);
        var template = await CreateTemplateAsync();

        var set = (await _metadataApi.SetFolderCustomFieldsAsync(root.Id, new SetCustomFields([new CustomFieldRequest("Project", "Alpha"), new CustomFieldRequest("Temp", "t")]), TestContext.Current.CancellationToken)).Response;

        set.Select(f => f.Name).Should().BeEquivalentTo(["Project", "Temp"]);

        await _metadataApi.SetFolderCustomFieldsAsync(root.Id, new SetCustomFields([new CustomFieldRequest("temp", null!)]), TestContext.Current.CancellationToken);
        await AssignWithCascadeAsync(root.Id, [template.Id]);

        (await GetFolderMetadataAsync(root.Id)).CustomFields.Select(f => f.Name).Should().Equal("Project");
        (await GetFileMetadataAsync(file.Id)).CustomFields.Should().BeNullOrEmpty("the custom fields belong to the folder only");
    }

    [Theory]
    [InlineData("empty list")]
    [InlineData("blank name")]
    [InlineData("repeated name")]
    [InlineData("over-long value")]
    public async Task SetFolderCustomFields_WhenRefused_ReturnsBadRequestAndChangesNothing(string scenario)
    {
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);

        await _metadataApi.SetFolderCustomFieldsAsync(folder.Id, new SetCustomFields([new CustomFieldRequest("keep", "me")]), TestContext.Current.CancellationToken);

        List<CustomFieldRequest> fields = scenario switch
        {
            "empty list" => [],
            "blank name" => [new CustomFieldRequest(" ", "x")],
            "repeated name" => [new CustomFieldRequest("a", "1"), new CustomFieldRequest("A", "2")],
            _ => [new CustomFieldRequest("ok", new string('v', MetadataService.MaxStringValueLength + 1))]
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFolderCustomFieldsAsync(folder.Id, new SetCustomFields(fields), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await GetFolderMetadataAsync(folder.Id)).CustomFields.Select(f => (f.Name, f.Value)).Should().Equal(("keep", "me"));
    }

    [Fact]
    public async Task GetFolderMetadata_OfAFolderWithoutMetadata_ReturnsEmptyLists_AndOfAnUnknownFolderNotFound()
    {
        var folder = await CreateFolderInMy($"folder-{Suffix()}", Owner);

        var metadata = await GetFolderMetadataAsync(folder.Id);

        metadata.Templates.Should().BeNullOrEmpty();
        metadata.CustomFields.Should().BeNullOrEmpty();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.GetFolderMetadataAsync(MissingId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
