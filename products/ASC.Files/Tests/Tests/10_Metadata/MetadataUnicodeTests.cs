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
/// Covers the text the metadata stores. A character inside the Basic Multilingual Plane is stored intact; one outside
/// it (an emoji, a mathematical letter, a rare ideograph) survives the request and is echoed by the create answer, but
/// is stored as one "?" per code point, in the names, the options and the values alike.
/// </summary>
[Trait("Category", "Metadata")]
public class MetadataUnicodeTests(AspireAppFixture fixture) : MetadataApiTestBase(fixture)
{
    [Fact]
    public async Task BasicMultilingualPlaneText_SurvivesARoundTripInNamesOptionsAndValues()
    {
        const string text = "Привет 模板 ™ ✓ é ☃ ❤ ☺";

        var template = await CreateTemplateAsync(
        [
            new MetadataFieldRequest($"f {text}", MetadataFieldType.String),
            new MetadataFieldRequest("c", MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: $"o {text}")])
        ], $"{Suffix()} {text}");

        var reloaded = await GetTemplateAsync(template.Id);

        reloaded.Name.Should().Contain(text);
        reloaded.Fields[0].Name.Should().Be($"f {text}");
        reloaded.Fields[1].Options[0].Value.Should().Be($"o {text}");

        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);

        await AssignToFileAsync(file.Id, template.Id);
        await SetFileStringAsync(file.Id, reloaded.Fields[0].Id, text);
        await _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields([new CustomFieldRequest(text, text)]), TestContext.Current.CancellationToken);

        var metadata = await GetFileMetadataAsync(file.Id);

        metadata.Templates.Single().Fields[0].Value!.StringValue.Should().Be(text);
        metadata.CustomFields.Select(f => (f.Name, f.Value)).Should().Equal((text, text));
    }

    [Theory]
    [Trait("Category", "Bug")]
    [InlineData("A \U0001F642 B")]
    [InlineData("\U0001F1E9\U0001F1EA")]
    [InlineData("\U0001D4B3")]
    [InlineData("\U00020BB7")]
    public async Task CreateTemplate_WithACharacterOutsideTheBmpInTheName_StoresItIntact(string sample)
    {
        var name = $"{Suffix()} {sample}";

        var created = await CreateTemplateAsync([], name);

        created.Name.Should().Be(name, "the create answer echoes the input");
        (await GetTemplateAsync(created.Id)).Name.Should().Be(name, "the stored name must be the one that was sent");
    }

    [Fact]
    [Trait("Category", "Bug")]
    public async Task CreateField_WithAnEmojiInTheFieldNameAndTheOptionValue_StoresThemIntact()
    {
        var template = await CreateTemplateAsync([]);

        await _metadataApi.CreateFieldAsync(template.Id,
            new MetadataFieldRequest("field \U0001F642", MetadataFieldType.SingleChoice, [new MetadataFieldOptionRequest(value: "option \U0001F642")]), TestContext.Current.CancellationToken);

        var field = (await GetTemplateAsync(template.Id)).Fields.Single();

        field.Name.Should().Be("field \U0001F642");
        field.Options.Single().Value.Should().Be("option \U0001F642");
    }

    [Fact]
    [Trait("Category", "Bug")]
    public async Task SetFileValues_WithAnEmojiInAStringValueAndACustomField_StoresThemIntact()
    {
        var template = await CreateTemplateAsync();
        var file = await CreateFileInMy($"doc-{Suffix()}.docx", Owner);
        const string text = "value \U0001F642";

        await AssignToFileAsync(file.Id, template.Id);
        await SetFileStringAsync(file.Id, template.Fields.Single().Id, text);
        await _metadataApi.SetFileCustomFieldsAsync(file.Id, new SetCustomFields([new CustomFieldRequest("note", text)]), TestContext.Current.CancellationToken);

        var metadata = await GetFileMetadataAsync(file.Id);

        metadata.Templates.Single().Fields.Single().Value!.StringValue.Should().Be(text);
        metadata.CustomFields.Single().Value.Should().Be(text);
    }
}
