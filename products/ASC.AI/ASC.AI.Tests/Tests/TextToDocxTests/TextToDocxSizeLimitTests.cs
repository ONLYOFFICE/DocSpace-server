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
/// The request body of <c>text-to-docx</c> is capped at 15 MiB, counted in bytes over the whole JSON body (the route's
/// own body parser, <c>docxJsonParser</c> in app.ts); the old 128 KB limit is gone. The boundary is probed with an
/// empty title on purpose: anything under the cap answers 400 "title and content are required", nothing over it gets
/// that far, and no 15 MiB document is ever built. Ported from messages.spec.ts, "AI Messages - text-to-docx size
/// limit" and the 128 KB case of "text-to-docx validation".
/// </summary>
/// <remarks>
/// The boundary bodies are sent raw on purpose: the byte count of the body is what is under test, so the body is
/// spelled out here rather than left to the SDK serializer's formatting.
/// </remarks>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxSizeLimitTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    [Fact]
    public async Task Export_BodyOfExactly15MiB_ReachesValidation_AndOneByteMoreIsRefused()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var overhead = BytesOf(SizedBody("", folderId));

        var atLimitBody = SizedBody(new string('x', BodyLimitBytes - overhead), folderId);
        BytesOf(atLimitBody).Should().Be(BodyLimitBytes);

        var atLimit = await PostExportRawAsync(atLimitBody);
        atLimit.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        atLimit.Error.Should().Be(RequiredError);

        var overBody = SizedBody(new string('x', BodyLimitBytes - overhead + 1), folderId);
        BytesOf(overBody).Should().Be(BodyLimitBytes + 1);

        (await PostExportRawAsync(overBody)).StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Export_BodyLimit_CountsBytesNotCharacters()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var overhead = BytesOf(SizedBody("", folderId));

        // The same number of characters: one passes and one does not.
        const int sameChars = 10_000_000;

        (await PostExportRawAsync(SizedBody(new string('x', sameChars), folderId))).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var cyrillicBody = SizedBody(new string('я', sameChars), folderId);
        BytesOf(cyrillicBody).Should().BeGreaterThan(BodyLimitBytes);
        (await PostExportRawAsync(cyrillicBody)).StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);

        // And the edge itself, in two-byte characters: the largest content that fits is refused one character later.
        var fits = (BodyLimitBytes - overhead) / 2;

        var atLimitBody = SizedBody(new string('я', fits), folderId);
        BytesOf(atLimitBody).Should().BeLessThanOrEqualTo(BodyLimitBytes);

        var atLimit = await PostExportRawAsync(atLimitBody);
        atLimit.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        atLimit.Error.Should().Be(RequiredError);

        var overBody = SizedBody(new string('я', fits + 1), folderId);
        BytesOf(overBody).Should().BeGreaterThan(BodyLimitBytes);
        (await PostExportRawAsync(overBody)).StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task Export_OneMiBAnswer_IsExportedWholeFirstLineToLast()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Exported big {RandomSuffix()}";

        var paragraphs = Enumerable.Range(0, 27000).Select(i => $"Paragraph {i:D6} lorem ipsum dolor sit.");
        var content = $"BEGINMARK\n\n{string.Join("\n\n", paragraphs)}\n\nENDMARK";
        BytesOf(content).Should().BeGreaterThan(1024 * 1024);

        await ExportAcceptedAsync(title, content, folderId);

        var exported = await WaitForExportedFileAsync(folderId, $"{title}.docx", TimeSpan.FromMinutes(2));
        exported.Should().NotBeNull($"no \"{title}.docx\" for a 1 MiB answer");

        var text = await ReadDocxTextAsync(exported!.Id);

        foreach (var marker in new[] { "BEGINMARK", "Paragraph 000000", "Paragraph 026999", "ENDMARK" })
        {
            text.Should().Contain(marker);
        }
    }

    /// <summary>The 128 KB body used to be refused with a bodyless 413; it now exports like any other size.</summary>
    [Fact]
    public async Task Export_128KBBody_IsNoLongerRefused()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var title = $"Exported {RandomSuffix()}";

        await ExportAcceptedAsync(title, new string('x', 100000), folderId);
        (await WaitForExportedFileAsync(folderId, $"{title}.docx")).Should().NotBeNull();

        await ExportAcceptedAsync($"{title} big", new string('x', 131072), folderId);
        (await WaitForExportedFileAsync(folderId, $"{title} big.docx")).Should().NotBeNull();
    }

    /// <summary>An invalid (empty-title) body of a known size; the content needs no JSON escaping.</summary>
    private static string SizedBody(string content, int folderId) =>
        $$"""{"title":"","content":"{{content}}","folderId":{{folderId}}}""";

    private static int BytesOf(string value) => System.Text.Encoding.UTF8.GetByteCount(value);
}
