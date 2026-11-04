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
/// Covers the values of a multi choice field on a file and on a folder: selecting any number of options, clearing the
/// selection, and the selections the field refuses.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataMultiChoiceValueTests(AspireAppFixture fixture) : MetadataFieldTypeTestBase(fixture)
{
    public static TheoryData<MetadataEntryKind, string> Accepted => MetadataKindData.Both("all options", "a single option", "empty list clears", "null clears");

    public static TheoryData<MetadataEntryKind, string> Refused => MetadataKindData.Both("option of another field", "string member");

    public static TheoryData<MetadataEntryKind> Kinds => MetadataKindData.Kinds();

    [Theory]
    [MemberData(nameof(Accepted))]
    public async Task SetValue_Accepted_IsStored(MetadataEntryKind kind, string scenario)
    {
        var a = await ArrangeAsync(kind, MultiChoiceField);
        var options = a.Options(MultiChoiceField);
        var fieldId = a.FieldId(MultiChoiceField);

        var request = scenario switch
        {
            "all options" => new MetadataValueRequest(fieldId, optionIds: [.. options]),
            "a single option" => new MetadataValueRequest(fieldId, optionIds: [options[2]]),
            "empty list clears" => new MetadataValueRequest(fieldId, optionIds: []),
            _ => new MetadataValueRequest(fieldId, optionIds: null!)
        };

        await a.SetAsync(request);

        switch (scenario)
        {
            case "all options":
                (await a.ValueAsync(MultiChoiceField))!.OptionIds.Should().BeEquivalentTo(options);
                break;
            case "a single option":
                (await a.ValueAsync(MultiChoiceField))!.OptionIds.Should().Equal(options[2]);
                break;
            default:
                await a.AssertClearedAsync(MultiChoiceField);
                break;
        }

        await a.AssertSiblingKeptAsync();
    }

    [Theory]
    [MemberData(nameof(Refused))]
    public async Task SetValue_Refused_ReturnsBadRequestAndKeepsTheValue(MetadataEntryKind kind, string scenario)
    {
        var a = await ArrangeAsync(kind, MultiChoiceField);
        var fieldId = a.FieldId(MultiChoiceField);

        var request = scenario == "option of another field"
            ? new MetadataValueRequest(fieldId, optionIds: [a.Options(SingleChoiceField)[0]])
            : new MetadataValueRequest(fieldId, stringValue: "x");

        (await a.TrySetAsync(request)).Should().Be(400, scenario);

        await a.AssertSeedKeptAsync(MultiChoiceField);
        await a.AssertSiblingKeptAsync();
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task SetValue_WithAMalformedOptionId_ReturnsBadRequestAndKeepsTheValue(MetadataEntryKind kind)
    {
        var a = await ArrangeAsync(kind, MultiChoiceField);

        // raw: the SDK model carries a Guid, so text that is not one never leaves the client
        var status = await a.SetRawAsync(MultiChoiceField, "\"optionIds\":[\"bogus\"]");

        status.Should().Be(HttpStatusCode.BadRequest);

        await a.AssertSeedKeptAsync(MultiChoiceField);
        await a.AssertSiblingKeptAsync();
    }
}
