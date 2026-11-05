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

namespace ASC.AI.Tests.Tests.TextToDocxTests;

/// <summary>
/// What a text-to-docx export puts into the document: the text, the markdown rendered rather than transcribed, and
/// text outside ASCII. Ported from messages.spec.ts, "AI Messages - text-to-docx export".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxExportTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    private const string CopyMarker = "COPYCHECK";

    // Every construct carries a word of its own: a document that renders the markdown keeps the word and drops the
    // syntax around it.
    private const string MarkdownAnswer =
        "# " + CopyMarker + "\n\n" +
        "**BOLDWORD** and *ITALICWORD*\n\n" +
        "- LISTONE\n" +
        "- LISTTWO\n\n" +
        "```js\n" +
        "const answer = 42;\n" +
        "```\n\n" +
        "| CELLA | CELLB |\n" +
        "| --- | --- |\n" +
        "| 1 | 2 |";

    // CJK, a diacritic, an ß, an em dash and a non-BMP emoji: what an exporter working on bytes loses.
    private const string UnicodeAnswer =
        "## Assistant report\n\n" +
        "**Bold** and *italic* — file saved.\n\n" +
        "- Item one\n" +
        "- 第二项\n\n" +
        "café naïve, größer, 🚀";

    [Fact]
    public async Task Export_IntoMyDocuments_CreatesOneDocxHoldingTheText()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var before = await WaitForStableFolderFilesAsync(folderId);

        var title = $"Exported {RandomSuffix()}";
        const string content = "The assistant said hello.";

        await ExportAcceptedAsync(title, content, folderId);

        // "A file whose name starts with the title" is not enough: one new, non-empty .docx holding the text sent.
        var exported = await WaitForExportedFileAsync(folderId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in My Documents after the export");
        exported!.FileExst.Should().Be(".docx");
        exported.PureContentLength.Should().BeGreaterThan(0);

        (await ListFolderFilesAsync(folderId)).Should().HaveCount(before.Count + 1);

        (await ReadDocxTextAsync(exported.Id)).Should().Contain(content);
    }

    [Fact]
    public async Task Export_MarkdownAnswer_KeepsEveryConstructAndDropsTheSyntax()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Exported markdown {RandomSuffix()}";

        await ExportAcceptedAsync(title, MarkdownAnswer, folderId);

        var exported = await WaitForExportedFileAsync(folderId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in My Documents");
        exported!.FileExst.Should().Be(".docx");
        exported.PureContentLength.Should().BeGreaterThan(0);

        var text = await ReadDocxTextAsync(exported.Id);

        foreach (var fragment in new[] { CopyMarker, "BOLDWORD", "ITALICWORD", "LISTONE", "LISTTWO", "const answer = 42;", "CELLA", "CELLB" })
        {
            text.Should().Contain(fragment, $"\"{fragment}\" is missing from the document");
        }

        // Top to bottom, the way it was written.
        text.IndexOf(CopyMarker, StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("LISTONE", StringComparison.Ordinal));
        text.IndexOf("LISTONE", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("LISTTWO", StringComparison.Ordinal));
        text.IndexOf("LISTTWO", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("CELLA", StringComparison.Ordinal));

        // The markdown was read, not transcribed.
        foreach (var syntax in new[] { "**", $"# {CopyMarker}", "| ---", "```" })
        {
            text.Should().NotContain(syntax, "the document shows the markdown source");
        }
    }

    [Fact]
    public async Task Export_UnicodeAnswer_SurvivesTitleIncluded()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();

        // The control file goes into a folder of its own so it cannot turn the export into "… (1).docx".
        var controlFolderId = await CreateFolderAsync(folderId, "Autotest TextToDocx Unicode Control");
        var title = $"Assistant report café 第二项 {RandomSuffix()}";
        var expectedTitle = await FilesApiTitleForAsync(controlFolderId, title);

        await ExportAcceptedAsync(title, UnicodeAnswer, folderId);

        var exported = await WaitForExportedFileAsync(folderId, expectedTitle);
        exported.Should().NotBeNull($"no \"{expectedTitle}\" in My Documents — the Files API keeps that name for the same title");

        var text = await ReadDocxTextAsync(exported!.Id);

        foreach (var fragment in new[] { "Assistant report", "Bold", "italic", "Item one", "第二项", "café naïve", "größer", "—", "🚀" })
        {
            text.Should().Contain(fragment, $"\"{fragment}\" is missing from the document");
        }

        text.IndexOf("Item one", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("第二项", StringComparison.Ordinal));

        foreach (var syntax in new[] { "## ", "**", "- Item one" })
        {
            text.Should().NotContain(syntax, "the document shows the markdown source");
        }
    }

    /// <summary>
    /// The 202 is exactly <c>{"success":true}</c>: no id and no link, so a client can only find the document by
    /// listing the folder. Pinned on purpose — an id appearing in this body is the fix, and it should show up as a
    /// failing test rather than pass unnoticed.
    /// </summary>
    [Fact]
    public async Task Export_AcceptedBody_CarriesNoIdOfTheDocument()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Exported handle {RandomSuffix()}";

        // Raw on purpose — the response side: AiExportTextToDocx202Response keeps only `success`, so an extra field
        // in the body (the id this test watches for) would be dropped by the typed client.
        var result = await PostExportRawAsync(BuildExport(title, "The assistant said hello.", folderId).ToJson());

        result.StatusCode.Should().Be(HttpStatusCode.Accepted, result.Body);
        JsonEquals(result.Body, """{"success":true}""").Should().BeTrue(result.Body);

        // And the only handle there is really does lead to the document.
        (await WaitForExportedFileAsync(folderId, $"{title}.docx")).Should().NotBeNull($"no \"{title}.docx\" in My Documents");
    }
}
