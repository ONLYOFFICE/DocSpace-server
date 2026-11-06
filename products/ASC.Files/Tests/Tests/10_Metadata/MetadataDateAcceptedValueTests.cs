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
/// Covers the dates a date field accepts on a file and on a folder: the earliest and the latest instant, the epoch,
/// the clearing null, and the written forms without an offset or with another one, which are read as UTC and converted.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataDateAcceptedValueTests(AspireAppFixture fixture) : MetadataFieldTypeTestBase(fixture)
{
    public static TheoryData<MetadataEntryKind, string?> Utc => MetadataKindData.Both<string?>(
        "0001-01-01T00:00:00Z",
        "9999-12-31T23:59:59Z",
        "1970-01-01T00:00:00Z",
        null);

    public static TheoryData<MetadataEntryKind, string, string> Written => MetadataKindData.Both<string, string>(
        ("2026-03-04","2026-03-04T00:00:00Z"),
        ("2026-03-04T05:06:07", "2026-03-04T05:06:07Z"),
        ("2026-03-04T05:06:07+03:00", "2026-03-04T02:06:07Z"));

    [Theory]
    [MemberData(nameof(Utc))]
    public async Task SetValue_OfAnInstantInUtc_IsStored(MetadataEntryKind kind, string? instant)
    {
        var a = await ArrangeAsync(kind, DateField);
        var date = instant is null ? (DateTime?)null : ParseUtc(instant);

        await a.SetAsync(new MetadataValueRequest(a.FieldId(DateField), dateValue: date));

        if (date is null)
        {
            await a.AssertClearedAsync(DateField);
        }
        else
        {
            (await a.ValueAsync(DateField))!.DateValue.UtcTime.Should().Be(date.Value);
        }

        await a.AssertSiblingKeptAsync();
    }

    [Theory]
    [MemberData(nameof(Written))]
    public async Task SetValue_WrittenWithoutOffsetOrWithAnother_IsStoredAsTheSameUtcInstant(MetadataEntryKind kind, string written, string expectedUtc)
    {
        var a = await ArrangeAsync(kind, DateField);

        // raw: the SDK serialises a DateTime in its own format, it cannot send a date-only value, one without an offset or one with +03:00
        var status = await a.SetRawAsync(DateField, $"\"dateValue\":\"{written}\"");

        status.Should().Be(HttpStatusCode.OK);
        (await a.ValueAsync(DateField))!.DateValue.UtcTime.Should().Be(ParseUtc(expectedUtc));
        await a.AssertSiblingKeptAsync();
    }

    private static DateTime ParseUtc(string instant)
    {
        return DateTime.Parse(instant, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
    }
}
