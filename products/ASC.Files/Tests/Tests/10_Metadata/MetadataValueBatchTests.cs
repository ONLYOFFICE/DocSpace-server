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
/// Covers what the typed value model cannot send: a value member left out altogether, which clears the field, and a
/// number that is no whole number within the range. Also that a batch with one invalid value writes none of them.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataValueBatchTests(AspireAppFixture fixture) : MetadataFieldTypeTestBase(fixture)
{
    public static TheoryData<MetadataEntryKind> Kinds => MetadataKindData.Kinds();

    public static TheoryData<MetadataEntryKind, string> EveryType => MetadataKindData.Both(StringField, NumberField, DateField, SingleChoiceField, MultiChoiceField);

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task SetValues_WithOneInvalidValue_WritesNoneOfThem(MetadataEntryKind kind)
    {
        var a = await ArrangeAsync(kind, StringField);

        await a.SetAsync(a.Seed(NumberField));

        var status = await a.TrySetAsync(
            new MetadataValueRequest(a.FieldId(StringField), stringValue: "changed"),
            new MetadataValueRequest(a.FieldId(NumberField), numberValue: MetadataService.MaxNumberValue + 1));

        status.Should().Be(400);

        await a.AssertSeedKeptAsync(StringField);
        await a.AssertSeedKeptAsync(NumberField);
    }

    [Theory]
    [MemberData(nameof(EveryType))]
    public async Task SetValue_WithTheValueMemberLeftOut_ClearsTheField(MetadataEntryKind kind, string field)
    {
        var a = await ArrangeAsync(kind, field);

        // raw: a typed model always puts every value member on the wire, as null at the least
        var status = await a.SetRawAsync(field, "");

        status.Should().Be(HttpStatusCode.OK);

        await a.AssertClearedAsync(field);
        await a.AssertSiblingKeptAsync();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task SetValue_OfAFractionalNumber_ReturnsBadRequestAndKeepsTheValue(MetadataEntryKind kind)
    {
        var a = await ArrangeAsync(kind, NumberField);

        // raw: numberValue is a long in the SDK, so a fraction never leaves the client. The contract is "a whole number":
        // the TypeScript suite accepts either an exact store or a refusal, but the stored value could not be read back
        // through a long, and rounding is the one outcome nobody allows
        var status = await a.SetRawAsync(NumberField, "\"numberValue\":42.5");

        status.Should().Be(HttpStatusCode.BadRequest);

        await a.AssertSeedKeptAsync(NumberField);
        await a.AssertSiblingKeptAsync();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task SetValue_OfANumberFarBeyondTheRange_ReturnsBadRequestAndKeepsTheValue(MetadataEntryKind kind)
    {
        var a = await ArrangeAsync(kind, NumberField);

        // raw: 1e30 does not fit the long the SDK carries
        var status = await a.SetRawAsync(NumberField, "\"numberValue\":1e30");

        status.Should().Be(HttpStatusCode.BadRequest);

        await a.AssertSeedKeptAsync(NumberField);
        await a.AssertSiblingKeptAsync();
    }
}
