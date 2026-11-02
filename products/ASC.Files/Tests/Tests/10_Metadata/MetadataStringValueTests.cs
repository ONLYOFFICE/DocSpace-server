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
/// Covers the boundaries of a string field value on a file and on a folder: the longest accepted value, the clearing
/// ones, and the values of another member type that the field refuses.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataStringValueTests(AspireAppFixture fixture) : MetadataFieldTypeTestBase(fixture)
{
    public static TheoryData<MetadataEntryKind, string?, string?> Accepted => MetadataKindData.Both<string?, string?>(
        ("a", "a"),
        (new string('a', MetadataService.MaxStringValueLength), new string('a', MetadataService.MaxStringValueLength)),
        ("", null),
        (null, null),
        ("   ", "   "),
        ("Привет ™ ✓ é", "Привет ™ ✓ é"));

    public static TheoryData<MetadataEntryKind, string> Refused => MetadataKindData.Both("longer than the limit", "number member", "option member");

    [Theory]
    [MemberData(nameof(Accepted))]
    public async Task SetValue_Accepted_IsStoredNormalised(MetadataEntryKind kind, string? value, string? expected)
    {
        var a = await ArrangeAsync(kind, StringField);

        await a.SetAsync(new MetadataValueRequest(a.FieldId(StringField), stringValue: value!));

        if (expected is null)
        {
            await a.AssertClearedAsync(StringField);
        }
        else
        {
            (await a.ValueAsync(StringField))!.StringValue.Should().Be(expected);
        }

        await a.AssertSiblingKeptAsync();
    }

    [Theory]
    [MemberData(nameof(Refused))]
    public async Task SetValue_Refused_ReturnsBadRequestAndKeepsTheValue(MetadataEntryKind kind, string scenario)
    {
        var a = await ArrangeAsync(kind, StringField);
        var fieldId = a.FieldId(StringField);

        var request = scenario switch
        {
            "longer than the limit" => new MetadataValueRequest(fieldId, stringValue: new string('a', MetadataService.MaxStringValueLength + 1)),
            "number member" => new MetadataValueRequest(fieldId, numberValue: 5),
            _ => new MetadataValueRequest(fieldId, optionIds: [a.Options(SingleChoiceField)[0]])
        };

        (await a.TrySetAsync(request)).Should().Be(400, scenario);

        await a.AssertSeedKeptAsync(StringField);
        await a.AssertSiblingKeptAsync();
    }
}
