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
/// <c>title</c> and <c>content</c> are required non-blank strings: blank, missing, null or of the wrong JSON type is
/// one refusal with one message — not a 500 from a failed cast and not a coerced "123". Ported from messages.spec.ts,
/// "AI Messages - text-to-docx validation" and the whitespace-content case of "text-to-docx export".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxValidationTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    [Theory]
    [InlineData("", "hello")]
    [InlineData("   ", "hello")]
    [InlineData("T", "")]
    public async Task Export_BlankTitleOrContent_IsRejected(string title, string content)
    {
        var folderId = await GetMyDocumentsFolderIdAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(BuildExport(title, content, folderId), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        ErrorOf(exception.ErrorContent?.ToString()).Should().Be(RequiredError);
    }

    /// <summary>Bodies the SDK model cannot carry: a null or missing field, or a value of the wrong JSON type.</summary>
    [Theory]
    [InlineData("""{"title":null,"content":"hello"}""")]
    [InlineData("""{"content":"hello"}""")]
    [InlineData("""{"title":"T","content":null}""")]
    [InlineData("""{"title":"T"}""")]
    [InlineData("""{}""")]
    [InlineData("""{"title":123,"content":"hello"}""")]
    [InlineData("""{"title":true,"content":"hello"}""")]
    [InlineData("""{"title":["T"],"content":"hello"}""")]
    [InlineData("""{"title":{"a":1},"content":"hello"}""")]
    [InlineData("""{"title":"T","content":123}""")]
    [InlineData("""{"title":"T","content":true}""")]
    [InlineData("""{"title":"T","content":["hello"]}""")]
    [InlineData("""{"title":"T","content":{"a":1}}""")]
    public async Task Export_MissingOrMistypedTitleOrContent_IsRejected(string body)
    {
        var folderId = await GetMyDocumentsFolderIdAsync();

        var result = await PostExportRawAsync(WithFolderId(body, folderId));

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest, result.Body);
        result.Error.Should().Be(RequiredError);
    }

    /// <summary>
    /// A whitespace-only title was always refused; the content used not to be trimmed before validating, and a blank
    /// document was built instead. Fixed: the route checks the trimmed length.
    /// </summary>
    [Fact]
    [Trait("Bug", "82712")]
    public async Task Export_WhitespaceOnlyContent_IsRejectedAndBuildsNoDocument()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Exported {RandomSuffix()}";

        var result = await TryExportAsync(BuildExport(title, "   ", folderId));

        // The document, not just the status code, is what this records: a blank .docx used to be produced.
        await WaitForExportToSettleAsync(result.StatusCode);
        (await ListFolderFilesAsync(folderId)).Select(file => file.Title).Should().NotContain($"{title}.docx");

        result.Error.Should().Be(RequiredError);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
