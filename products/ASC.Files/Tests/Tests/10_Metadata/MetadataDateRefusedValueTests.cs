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
/// Covers the values a date field refuses on a file and on a folder: dates outside the years 1 to 9999, text that is not
/// a date, and the members of another type.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataDateRefusedValueTests(AspireAppFixture fixture) : MetadataFieldTypeTestBase(fixture)
{
    public static TheoryData<MetadataEntryKind, string> NotADate => MetadataKindData.Both("0000-01-01", "10000-01-01", "2026-13-01", "not a date");

    public static TheoryData<MetadataEntryKind, string> OtherMember => MetadataKindData.Both("string member", "number member");

    [Theory]
    [MemberData(nameof(NotADate))]
    public async Task SetValue_OfAnInvalidDate_ReturnsBadRequestAndKeepsTheValue(MetadataEntryKind kind, string text)
    {
        var a = await ArrangeAsync(kind, DateField);

        // raw: the SDK model carries a DateTime, so a year 0, a month 13 or text never leaves the client
        var status = await a.SetRawAsync(DateField, $"\"dateValue\":\"{text}\"");

        status.Should().Be(HttpStatusCode.BadRequest, text);

        await a.AssertSeedKeptAsync(DateField);
        await a.AssertSiblingKeptAsync();
    }

    [Theory]
    [MemberData(nameof(OtherMember))]
    public async Task SetValue_OfAnotherMember_ReturnsBadRequestAndKeepsTheValue(MetadataEntryKind kind, string scenario)
    {
        var a = await ArrangeAsync(kind, DateField);
        var fieldId = a.FieldId(DateField);

        var request = scenario == "string member"
            ? new MetadataValueRequest(fieldId, stringValue: "2026-03-04")
            : new MetadataValueRequest(fieldId, numberValue: 5);

        (await a.TrySetAsync(request)).Should().Be(400, scenario);

        await a.AssertSeedKeptAsync(DateField);
        await a.AssertSiblingKeptAsync();
    }
}
