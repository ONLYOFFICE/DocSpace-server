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
/// <c>format</c> is a case-sensitive enum. A misspelling, a value of the wrong JSON type, or an explicit null — which
/// is not the same as no format at all — is refused with one message instead of being silently ignored. Ported from
/// messages.spec.ts, "AI Messages - text-to-docx's format takes Docx, Pdf or Md" (the unrecognised formats) and
/// "AI Messages - text-to-docx format shapes".
/// </summary>
/// <remarks>
/// Raw throughout: none of these values fits the <c>format</c> enum of the SDK model, so the typed client cannot send them
/// (see <see cref="TextToDocxPortTestBase.WithFormat"/>).
/// </remarks>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxShapesTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    [Theory]
    [InlineData("\"pdf\"")]
    [InlineData("\"docx\"")]
    [InlineData("\"md\"")]
    [InlineData("\"bogus\"")]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("true")]
    [InlineData("[\"Docx\"]")]
    [InlineData("\"DOCX\"")]
    [InlineData("\" Docx\"")]
    public async Task Export_UnrecognisedFormat_IsRejected(string formatJson)
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var request = BuildExport($"Exported invalid format {RandomSuffix()}", "The assistant said hello.", folderId);

        var result = await PostExportRawAsync(WithFormat(request, formatJson));

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"format: {formatJson}; {result.Body}");
        result.Error.Should().Be(FormatError, $"format: {formatJson}");
    }
}
