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

namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// Markdown in the .docx export, the mirror image of the markdown tests of the write path and of a reply: everywhere
/// else the markdown has to survive as source, here it has to be read. Ported from messages.spec.ts, "AI Messages -
/// markdown in the .docx export".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class MessageMarkdownDocxTests(AspireAppFixture fixture) : TextToDocxTests.TextToDocxPortTestBase(fixture)
{
    [Fact]
    public async Task Export_QuotesListsLinksAndInlineCode_AreRenderedNotTranscribed()
    {
        // Arrange: the constructs the basic markdown export test leaves out, and the ones most likely to be dropped whole.
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Exported render {RandomSuffix()}";
        var content = string.Join(
            "\n",
            "## EXPORTHEAD",
            "",
            "> QUOTEWORD",
            "",
            "1. ORDEREDONE",
            "2. ORDEREDTWO",
            "",
            "- OUTERONE",
            "  - NESTEDONE",
            "",
            "[LINKWORD](https://example.invalid/target?a=1&b=2)",
            "",
            "`INLINEWORD` and ~~STRUCKWORD~~");

        // Act
        await ExportAcceptedAsync(title, content, folderId);

        // Assert
        var exported = await WaitForExportedFileAsync(folderId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in My Documents");
        var text = await ReadDocxTextAsync(exported!.Id);

        string[] fragments = ["EXPORTHEAD", "QUOTEWORD", "ORDEREDONE", "ORDEREDTWO", "OUTERONE", "NESTEDONE", "LINKWORD", "INLINEWORD", "STRUCKWORD"];

        foreach (var fragment in fragments)
        {
            text.Should().Contain(fragment, $"\"{fragment}\" is missing from the document");
        }

        text.IndexOf("EXPORTHEAD", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("QUOTEWORD", StringComparison.Ordinal));
        text.IndexOf("QUOTEWORD", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("ORDEREDONE", StringComparison.Ordinal));
        text.IndexOf("ORDEREDONE", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("NESTEDONE", StringComparison.Ordinal));
        text.IndexOf("NESTEDONE", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("LINKWORD", StringComparison.Ordinal));

        foreach (var syntax in new[] { "## EXPORTHEAD", "> QUOTEWORD", "- OUTERONE", "`INLINEWORD`", "~~STRUCKWORD~~", "](" })
        {
            text.Should().NotContain(syntax, $"the document shows the markdown source: {syntax}");
        }
    }

    /// <summary>
    /// The exporter rendered every other construct and stopped at math: <c>$$\frac{A}{B}$$</c> landed in the document
    /// as the literal line <c>\frac{A}{B}</c>. The correct outcome is that the document does not show TeX source.
    /// </summary>
    [Fact]
    [Trait("Bug", "83038")]
    public async Task Export_Formula_IsNotWrittenIntoTheDocumentAsRawTeX()
    {
        // Arrange
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Exported math {RandomSuffix()}";
        var content = string.Join(
            "\n",
            "# MATHEXPORT",
            "",
            "Inline math: $NUMERWORD^2$ inside a sentence.",
            "",
            "$$\\frac{NUMERWORD}{DENOMWORD} = \\sqrt{GAMMAWORD}$$",
            "",
            "TAILWORD closes the answer.");

        // Act
        await ExportAcceptedAsync(title, content, folderId);

        // Assert
        var exported = await WaitForExportedFileAsync(folderId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in My Documents");
        var text = await ReadDocxTextAsync(exported!.Id);

        // The operands reach the reader and the prose keeps its place: the defect is the form, not a data loss.
        foreach (var fragment in new[] { "MATHEXPORT", "NUMERWORD", "DENOMWORD", "GAMMAWORD", "TAILWORD" })
        {
            text.Should().Contain(fragment, $"\"{fragment}\" is missing from the document - the exporter dropped the formula outright");
        }

        text.IndexOf("MATHEXPORT", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("NUMERWORD", StringComparison.Ordinal));
        text.IndexOf("NUMERWORD", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("TAILWORD", StringComparison.Ordinal));

        foreach (var source in new[] { "\\frac{", "\\sqrt{", "\\frac{NUMERWORD}{DENOMWORD}", "$$" })
        {
            text.Should().NotContain(source, $"the document shows the TeX source instead of a formula: {source}");
        }
    }
}
