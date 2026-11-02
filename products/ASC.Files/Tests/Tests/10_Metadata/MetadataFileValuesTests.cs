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
/// Covers the values and the custom fields of a file: a write of every field type, the partial write, the refusals
/// that must leave the stored values untouched, and the limits of the custom fields.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataFileValuesTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task SetFileValues_OfEveryType_ReturnsAndStoresTheWholeMetadata()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var template = await CreateTemplateAsync(
        [
            new MetadataFieldRequest("s", MetadataFieldType.String),
            new MetadataFieldRequest("n", MetadataFieldType.Number),
            new MetadataFieldRequest("d", MetadataFieldType.Date),
            new MetadataFieldRequest("sc", MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: "one"), new MetadataFieldOptionRequest(value: "two")]),
            new MetadataFieldRequest("mc", MetadataFieldType.MultiChoice, [new MetadataFieldOptionRequest(value: "x"), new MetadataFieldOptionRequest(value: "y"), new MetadataFieldOptionRequest(value: "z")])
        ]);

        await AssignToFileAsync(file.Id, template.Id);

        var date = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        var two = template.Field("sc").Option("two");
        var multi = new List<Guid> { template.Field("mc").Option("x"), template.Field("mc").Option("z") };

        var written = (await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues(
        [
            new MetadataValueRequest(template.Field("s").Id, stringValue: "Привет 你好"),
            new MetadataValueRequest(template.Field("n").Id, numberValue: 42),
            new MetadataValueRequest(template.Field("d").Id, dateValue: date),
            new MetadataValueRequest(template.Field("sc").Id, optionIds: [two]),
            new MetadataValueRequest(template.Field("mc").Id, optionIds: multi)
        ]), TestContext.Current.CancellationToken)).Response;

        foreach (var metadata in new[] { written, await GetFileMetadataAsync(file.Id) })
        {
            ValueOf(metadata, template.Id, template.Field("s").Id)!.StringValue.Should().Be("Привет 你好");
            ValueOf(metadata, template.Id, template.Field("n").Id)!.NumberValue.Should().Be(42);
            ValueOf(metadata, template.Id, template.Field("d").Id)!.DateValue.UtcTime.Should().Be(date);
            ValueOf(metadata, template.Id, template.Field("sc").Id)!.OptionIds.Should().Equal(two);
            ValueOf(metadata, template.Id, template.Field("mc").Id)!.OptionIds.Should().BeEquivalentTo(multi);
        }
    }

    [Fact]
    public async Task SetFileValues_Partial_KeepsTheOtherValues_AndAnEmptyStringClearsTheField()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var template = await CreateTemplateAsync([new MetadataFieldRequest("a", MetadataFieldType.String), new MetadataFieldRequest("b", MetadataFieldType.String)]);
        var a = template.Field("a").Id;
        var b = template.Field("b").Id;

        await AssignToFileAsync(file.Id, template.Id);
        await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues([new MetadataValueRequest(a, stringValue: "1"), new MetadataValueRequest(b, stringValue: "2")]), TestContext.Current.CancellationToken);

        await SetFileStringAsync(file.Id, a, "changed");

        var changed = await GetFileMetadataAsync(file.Id);

        ValueOf(changed, template.Id, a)!.StringValue.Should().Be("changed");
        ValueOf(changed, template.Id, b)!.StringValue.Should().Be("2");

        await SetFileStringAsync(file.Id, a, "");

        var cleared = await GetFileMetadataAsync(file.Id);

        ValueOf(cleared, template.Id, a)?.StringValue.Should().BeNullOrEmpty();
        ValueOf(cleared, template.Id, b)!.StringValue.Should().Be("2");
    }

    [Theory]
    [InlineData("number field with a string value", 400)]
    [InlineData("field of a template the file does not carry", 400)]
    [InlineData("same field listed twice", 400)]
    [InlineData("non-existent field", 404)]
    public async Task SetFileValues_WhenRefused_LeavesTheExistingValuesUntouched(string scenario, int expectedStatus)
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var mine = await CreateTemplateAsync([new MetadataFieldRequest("str", MetadataFieldType.String), new MetadataFieldRequest("num", MetadataFieldType.Number)]);
        var other = await CreateTemplateAsync();
        var str = mine.Field("str").Id;

        await AssignToFileAsync(file.Id, mine.Id);
        await SetFileStringAsync(file.Id, str, "keep");

        List<MetadataValueRequest> values = scenario switch
        {
            "number field with a string value" => [new MetadataValueRequest(str, stringValue: "changed"), new MetadataValueRequest(mine.Field("num").Id, stringValue: "x")],
            "field of a template the file does not carry" => [new MetadataValueRequest(str, stringValue: "changed"), new MetadataValueRequest(other.Fields.Single().Id, stringValue: "x")],
            "same field listed twice" => [new MetadataValueRequest(str, stringValue: "1"), new MetadataValueRequest(str, stringValue: "2")],
            _ => [new MetadataValueRequest(str, stringValue: "changed"), new MetadataValueRequest(MissingId, stringValue: "x")]
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFileValuesAsync(file.Id, new SetMetadataValues(values), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(expectedStatus);
        ValueOf(await GetFileMetadataAsync(file.Id), mine.Id, str)!.StringValue.Should().Be("keep", "a refused batch writes none of its values");
    }

    [Fact]
    public async Task SetFileCustomFields_SetsUpdatesCaseInsensitivelyAndRemoves_LeavingTheUnlistedNamesAlone()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);

        var first = (await _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields([new CustomFieldRequest("Owner", "Анна"), new CustomFieldRequest("Dept", "QA")]), TestContext.Current.CancellationToken)).Response;

        first.Select(f => (f.Name, f.Value)).Should().BeEquivalentTo([("Owner", "Анна"), ("Dept", "QA")]);

        await _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields([new CustomFieldRequest("OWNER", "Boris")]), TestContext.Current.CancellationToken);

        (await GetFileMetadataAsync(file.Id)).CustomFields.ToDictionary(f => f.Name.ToLowerInvariant(), f => f.Value)
            .Should().BeEquivalentTo(new Dictionary<string, string> { ["owner"] = "Boris", ["dept"] = "QA" });

        await _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields([new CustomFieldRequest("dept", "")]), TestContext.Current.CancellationToken);

        (await GetFileMetadataAsync(file.Id)).CustomFields.Select(f => f.Name.ToLowerInvariant()).Should().Equal("owner");
    }

    [Fact]
    public async Task SetFileCustomFields_DoNotDisturbTheTemplateValues()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        var template = await CreateTemplateAsync();
        var fieldId = template.Fields.Single().Id;

        await AssignToFileAsync(file.Id, template.Id);
        await SetFileStringAsync(file.Id, fieldId, "tpl");

        await _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields([new CustomFieldRequest("free", "text")]), TestContext.Current.CancellationToken);

        var metadata = await GetFileMetadataAsync(file.Id);

        ValueOf(metadata, template.Id, fieldId)!.StringValue.Should().Be("tpl");
        metadata.CustomFields.Select(f => f.Name).Should().Equal("free");
    }

    [Theory]
    [InlineData("empty list")]
    [InlineData("blank name")]
    [InlineData("repeated name")]
    [InlineData("over-long name")]
    [InlineData("over-long value")]
    public async Task SetFileCustomFields_WhenRefused_ReturnsBadRequestAndChangesNothing(string scenario)
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);

        await _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields([new CustomFieldRequest("keep", "me")]), TestContext.Current.CancellationToken);

        List<CustomFieldRequest> fields = scenario switch
        {
            "empty list" => [],
            "blank name" => [new CustomFieldRequest("  ", "x")],
            "repeated name" => [new CustomFieldRequest("a", "1"), new CustomFieldRequest("A", "2")],
            "over-long name" => [new CustomFieldRequest(new string('n', MetadataService.MaxNameLength + 1), "x")],
            _ => [new CustomFieldRequest("ok", new string('v', MetadataService.MaxStringValueLength + 1))]
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields(fields), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await GetFileMetadataAsync(file.Id)).CustomFields.Select(f => (f.Name, f.Value)).Should().Equal(("keep", "me"));
    }

    [Fact]
    public async Task SetFileCustomFields_HoldsAtMostFiftyFields()
    {
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);

        await _metadataApi.SetFileCustomFieldsAsync(file.Id,
            new SetCustomFields(Enumerable.Range(0, MetadataService.MaxCustomFieldsPerEntry).Select(i => new CustomFieldRequest($"f{i}", "v")).ToList()), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFileCustomFieldsAsync(file.Id,
                new SetCustomFields(Enumerable.Range(0, MetadataService.MaxCustomFieldsPerEntry + 1).Select(i => new CustomFieldRequest($"g{i}", "v")).ToList()), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await GetFileMetadataAsync(file.Id)).CustomFields.Should().HaveCount(MetadataService.MaxCustomFieldsPerEntry);
    }

    [Fact]
    public async Task SetFileCustomFields_OnAnUnknownFile_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _metadataApi.SetFileCustomFieldsAsync(MissingId, new SetCustomFields([new CustomFieldRequest("a", "b")]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
