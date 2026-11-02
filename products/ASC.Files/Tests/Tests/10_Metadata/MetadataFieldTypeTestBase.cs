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
/// The kind of entry a value is written to: the file and the folder endpoints are twins, and every boundary is checked on both.
/// </summary>
public enum MetadataEntryKind
{
    File,
    Folder
}

/// <summary>
/// Builds the theory data that runs one case against both kinds of entry.
/// </summary>
public static class MetadataKindData
{
    public static TheoryData<MetadataEntryKind, T> Both<T>(params T[] cases)
    {
        var data = new TheoryData<MetadataEntryKind, T>();

        foreach (var kind in Enum.GetValues<MetadataEntryKind>())
        {
            foreach (var value in cases)
            {
                data.Add(kind, value);
            }
        }

        return data;
    }

    public static TheoryData<MetadataEntryKind, T1, T2> Both<T1, T2>(params (T1, T2)[] cases)
    {
        var data = new TheoryData<MetadataEntryKind, T1, T2>();

        foreach (var kind in Enum.GetValues<MetadataEntryKind>())
        {
            foreach (var (first, second) in cases)
            {
                data.Add(kind, first, second);
            }
        }

        return data;
    }

    public static TheoryData<MetadataEntryKind> Kinds()
    {
        var data = new TheoryData<MetadataEntryKind>();

        foreach (var kind in Enum.GetValues<MetadataEntryKind>())
        {
            data.Add(kind);
        }

        return data;
    }
}

/// <summary>
/// The boundary matrix of the value of every metadata field type, run against a file and a folder. The limits come from
/// the documented contract of the value endpoints: a string of at most 8000 characters, a whole number within
/// +-9007199254740991, a date of the years 1 to 9999 (read as UTC when it carries no offset), at most one option for a
/// single choice, an empty value clears. A refused write must leave the target value and its sibling untouched.
/// </summary>
public abstract class MetadataFieldTypeTestBase(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    protected const string StringField = "s";
    protected const string NumberField = "n";
    protected const string DateField = "d";
    protected const string SingleChoiceField = "sc";
    protected const string MultiChoiceField = "mc";
    private const string SiblingField = "sibling";

    protected static readonly DateTime _seedDate = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// An entry with one field of each type and a string sibling, the target field holding its seed value.
    /// </summary>
    protected async Task<Arrangement> ArrangeAsync(MetadataEntryKind kind, string target)
    {
        int entryId;

        if (kind == MetadataEntryKind.File)
        {
            entryId = (await CreateFileInMy($"types-{Suffix()}.docx", Owner)).Id;
        }
        else
        {
            entryId = (await CreateFolderInMy($"types-{Suffix()}", Owner)).Id;
        }

        var template = await CreateTemplateAsync(
        [
            new MetadataFieldRequest(StringField, MetadataFieldType.String),
            new MetadataFieldRequest(NumberField, MetadataFieldType.Number),
            new MetadataFieldRequest(DateField, MetadataFieldType.Date),
            new MetadataFieldRequest(SingleChoiceField, MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: "one"), new MetadataFieldOptionRequest(value: "two")]),
            new MetadataFieldRequest(MultiChoiceField, MetadataFieldType.MultiChoice, [new MetadataFieldOptionRequest(value: "a"), new MetadataFieldOptionRequest(value: "b"), new MetadataFieldOptionRequest(value: "c")]),
            new MetadataFieldRequest(SiblingField, MetadataFieldType.String)
        ]);

        if (kind == MetadataEntryKind.File)
        {
            await AssignToFileAsync(entryId, template.Id);
        }
        else
        {
            await AssignToFolderAsync(entryId, template.Id);
        }

        var arrangement = new Arrangement(this, kind, entryId, template);

        // the sibling holds a value no write to the target may disturb
        await arrangement.SetAsync(new MetadataValueRequest(arrangement.FieldId(SiblingField), stringValue: SiblingField));
        await arrangement.SetAsync(arrangement.Seed(target));

        return arrangement;
    }

    protected sealed class Arrangement(MetadataFieldTypeTestBase owner, MetadataEntryKind kind, int entryId, MetadataTemplateDto template)
    {
        public int FieldId(string name)
        {
            return template.Field(name).Id;
        }

        public List<Guid> Options(string name)
        {
            return template.Field(name).Options.Select(o => o.Id).ToList();
        }

        /// <summary>
        /// The value the target field holds before the write under test.
        /// </summary>
        public MetadataValueRequest Seed(string name)
        {
            var fieldId = FieldId(name);

            return name switch
            {
                StringField => new MetadataValueRequest(fieldId, stringValue: "seed"),
                NumberField => new MetadataValueRequest(fieldId, numberValue: 1),
                DateField => new MetadataValueRequest(fieldId, dateValue: _seedDate),
                SingleChoiceField => new MetadataValueRequest(fieldId, optionIds: [Options(name)[0]]),
                MultiChoiceField => new MetadataValueRequest(fieldId, optionIds: [Options(name)[0]]),
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
            };
        }

        public async Task<EntryMetadataDto> SetAsync(params MetadataValueRequest[] values)
        {
            var body = new SetMetadataValues([.. values]);

            return kind == MetadataEntryKind.File
                ? (await owner._metadataApi.SetFileValuesAsync(entryId, body, TestContext.Current.CancellationToken)).Response
                : (await owner._metadataApi.SetFolderValuesAsync(entryId, body, TestContext.Current.CancellationToken)).Response;
        }

        public Task<int> TrySetAsync(params MetadataValueRequest[] values)
        {
            return StatusOfAsync(async () => await SetAsync(values));
        }

        /// <summary>
        /// Writes a value member that the typed model cannot express, as the raw JSON members of one value of the target field.
        /// </summary>
        public async Task<HttpStatusCode> SetRawAsync(string fieldName, string members)
        {
            var path = $"api/2.0/files/metadata/{(kind == MetadataEntryKind.File ? "file" : "folder")}/{entryId}/values";
            var json = "{\"values\":[{\"fieldId\":" + FieldId(fieldName) + (members.Length == 0 ? "" : "," + members) + "}]}";

            using var response = await new RawApiClient(owner._filesClient).PutRawAsync(path, json, TestContext.Current.CancellationToken);

            return response.StatusCode;
        }

        public async Task<MetadataValueDto?> ValueAsync(string name)
        {
            var metadata = kind == MetadataEntryKind.File ? await owner.GetFileMetadataAsync(entryId) : await owner.GetFolderMetadataAsync(entryId);

            return ValueOf(metadata, template.Id, FieldId(name));
        }

        public async Task AssertSiblingKeptAsync()
        {
            (await ValueAsync(SiblingField))!.StringValue.Should().Be(SiblingField, "no write to another field may disturb the sibling");
        }

        /// <summary>
        /// The target field still holds the value it was seeded with.
        /// </summary>
        public async Task AssertSeedKeptAsync(string name)
        {
            var value = await ValueAsync(name);
            var seed = Seed(name);

            value.Should().NotBeNull("the refused write must keep the value");

            switch (name)
            {
                case StringField:
                    value!.StringValue.Should().Be(seed.StringValue);
                    break;
                case NumberField:
                    value!.NumberValue.Should().Be(seed.NumberValue);
                    break;
                case DateField:
                    value!.DateValue.UtcTime.Should().Be(_seedDate);
                    break;
                default:
                    value!.OptionIds.Should().Equal(seed.OptionIds);
                    break;
            }
        }

        public async Task AssertClearedAsync(string name)
        {
            var value = await ValueAsync(name);

            (value is null || (string.IsNullOrEmpty(value.StringValue) && value.NumberValue is null && value.DateValue is null && (value.OptionIds is null || value.OptionIds.Count == 0)))
                .Should().BeTrue("the field was cleared, but holds {0}", value);
        }
    }
}
