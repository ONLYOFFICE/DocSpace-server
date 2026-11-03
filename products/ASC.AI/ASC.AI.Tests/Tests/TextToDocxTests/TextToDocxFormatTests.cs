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
/// <c>format</c> on <c>/text-to-docx</c> is the only way to ask for anything but a .docx: "Docx" | "Pdf" | "Md" decide
/// the extension of the written file, <c>extension</c> has no effect next to it, and the sibling routes a client might
/// try do not exist. Without a format the export is a .docx — the default every other test of this folder relies on
/// (<see cref="TextToDocxExportTests"/>). Ported from messages.spec.ts, "AI Messages - text-to-docx's format takes
/// Docx, Pdf or Md".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxFormatTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    [Theory]
    [InlineData("/api/2.0/ai/text-to-pdf")]
    [InlineData("/api/2.0/ai/text-to-txt")]
    [InlineData("/api/2.0/ai/text-to-md")]
    [InlineData("/api/2.0/ai/text-to-file")]
    [InlineData("/api/2.0/ai/export-text")]
    public async Task SiblingExportRoute_DoesNotExist(string route)
    {
        var folderId = await GetMyDocumentsFolderIdAsync();

        // Raw on purpose: a route that is not in the OpenAPI document has no SDK operation.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            route,
            BuildExport("T", "hello", folderId).ToJson(),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, $"POST {route}");
    }

    [Theory]
    [InlineData(AiExportTextToDocxRequest.FormatEnum.Docx, ".docx")]
    [InlineData(AiExportTextToDocxRequest.FormatEnum.Pdf, ".pdf")]
    [InlineData(AiExportTextToDocxRequest.FormatEnum.Md, ".md")]
    public async Task Export_WithFormat_WritesAFileOfThatFormat(AiExportTextToDocxRequest.FormatEnum format, string extension)
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Exported format {format} {RandomSuffix()}";
        var request = BuildExport(title, "The assistant said hello.", folderId);
        request.Format = format;

        var result = await TryExportAsync(request);
        result.StatusCode.Should().Be(HttpStatusCode.Accepted, result.Error);

        var exported = await WaitForExportedFileAsync(folderId, $"{title}{extension}");
        exported.Should().NotBeNull($"no \"{title}{extension}\" for format \"{format}\"");
        exported!.FileExst.Should().Be(extension);
    }

    [Fact]
    public async Task Export_ExtensionNextToFormat_ChangesNothing()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Exported format override {RandomSuffix()}";

        // Raw on purpose: `extension` is not a property of the SDK request, so the typed client cannot send it.
        var result = await PostExportRawAsync(WithFormat(BuildExport(title, "The assistant said hello.", folderId), "\"Pdf\"", ".txt"));
        result.StatusCode.Should().Be(HttpStatusCode.Accepted, result.Body);

        (await WaitForExportedFileAsync(folderId, $"{title}.pdf")).Should().NotBeNull(
            "\"extension\" must not override \"format\": expected a .pdf for format \"Pdf\"");
        (await ListFolderFilesAsync(folderId)).Select(file => file.Title).Should().NotContain($"{title}.txt");
    }
}
