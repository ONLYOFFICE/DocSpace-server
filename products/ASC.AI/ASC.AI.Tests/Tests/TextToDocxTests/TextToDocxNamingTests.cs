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
/// How a title becomes a file name: duplicates are renamed, an over-long title is truncated, and forbidden characters
/// are replaced the way <c>POST /files/{folderId}/file</c> replaces them — that endpoint is the oracle rather than a
/// hard-coded expectation. Ported from messages.spec.ts, "AI Messages - text-to-docx export" and "naming and
/// concurrency".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxNamingTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    [Fact]
    public async Task Export_SameTitleThreeTimes_GivesNumberedCopiesAndOverwritesNothing()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var before = await WaitForStableFolderFilesAsync(folderId);
        var title = $"Exported {RandomSuffix()}";
        string[] expected = [$"{title}.docx", $"{title} (1).docx", $"{title} (2).docx"];

        for (var i = 0; i < expected.Length; i++)
        {
            await ExportAcceptedAsync(title, $"Export number {i}.", folderId);

            (await WaitForExportedFileAsync(folderId, expected[i])).Should().NotBeNull($"expected \"{expected[i]}\"");
        }

        var files = await ListFolderFilesAsync(folderId);
        files.Should().HaveCount(before.Count + expected.Length);

        // Each document keeps its own text: the first one was not replaced.
        for (var i = 0; i < expected.Length; i++)
        {
            var file = files.Single(f => f.Title == expected[i]);

            (await ReadDocxTextAsync(file.Id)).Should().Contain($"Export number {i}.");
        }
    }

    [Fact]
    public async Task Export_TitleLongerThanTheFileNameLimit_IsTruncated()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();

        await ExportAcceptedAsync(new string('L', 300), "hello", folderId);

        // 170 characters is the portal's file name limit, extension included.
        var exported = await WaitForExportedFileAsync(folderId, $"{new string('L', 165)}.docx");
        exported.Should().NotBeNull("the over-long title was not truncated to 165 chars + .docx");
        exported!.Title.Should().HaveLength(170);
    }

    /// <summary>
    /// The export builds the file name with <c>Path.GetFileNameWithoutExtension</c> (Bug 82711). On a Windows host that
    /// also takes a leading <c>a:</c> for a drive, so <c>a:*?"&lt;&gt;|b</c> lands as <c>_______b.docx</c>; on Linux this
    /// row passes.
    /// </summary>
    [Theory]
    [Trait("Bug", "82711")]
    [InlineData("Q1 report: draft")]
    [InlineData("a:*?\"<>|b")]
    public async Task Export_ForbiddenCharactersInTitle_AreReplacedLikeTheFilesApiReplacesThem(string title)
    {
        var folderId = await GetMyDocumentsFolderIdAsync();

        // The control file goes into its own folder so a matching name cannot be renamed to "… (1)".
        var controlFolderId = await CreateFolderAsync(folderId, "Autotest TextToDocx Control");
        var expected = await FilesApiTitleForAsync(controlFolderId, title);
        var before = await FileIdsAsync(folderId);

        await ExportAcceptedAsync(title, "hello", folderId);

        (await WaitForNewFilesAsync(folderId, before)).Select(file => file.Title).Should().Equal(
            [expected], $"title {title} should have become {expected}, as it does through the Files API");
    }

    /// <summary>
    /// The export treats "/" in the title as a path separator and keeps only the last segment, so <c>Notes 12/31</c>
    /// is saved as <c>31.docx</c>. The Files API replaces the separator like any other forbidden character and keeps
    /// the whole name — the behaviour asserted here. The traversal-shaped title is also why a fix has to keep the
    /// export inside <c>folderId</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "82711")]
    public async Task Export_TitleWithSlash_KeepsTheWholeNameLikeTheFilesApi()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var controlFolderId = await CreateFolderAsync(folderId, "Autotest TextToDocx Control");

        string[] titles = ["Notes 12/31", "../../Autotest Traversal", "a\\/:*?\"<>|b", "<b>bold</b>"];
        var expected = new List<string>();
        var before = await FileIdsAsync(folderId);

        foreach (var title in titles)
        {
            expected.Add(await FilesApiTitleForAsync(controlFolderId, title));

            await ExportAcceptedAsync(title, "hello", folderId);
        }

        // Every export gets its chance to land, under whatever name, before the names are compared.
        var landed = (await WaitForNewFilesAsync(folderId, before, titles.Length)).ConvertAll(file => file.Title);

        landed.Should().BeEquivalentTo(expected);
    }

    /// <summary>
    /// A lone backslash, checked on its own because the 82711 cases mix both separators. On Linux the backslash is
    /// normalised the way the Files API does it; on a Windows host <c>Path.GetFileNameWithoutExtension</c> treats it as
    /// a separator and <c>Notes 1234</c> lands as <c>31.docx</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "82711")]
    public async Task Export_BackslashInTitle_IsReplacedLikeTheFilesApiReplacesIt()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var controlFolderId = await CreateFolderAsync(folderId, "Autotest TextToDocx Control");

        var title = $"Notes {RandomSuffix()[..4]}\\31";
        var expected = await FilesApiTitleForAsync(controlFolderId, title);
        var before = await FileIdsAsync(folderId);

        await ExportAcceptedAsync(title, "hello", folderId);

        (await WaitForNewFilesAsync(folderId, before)).Select(file => file.Title).Should().Equal(
            [expected], $"title {title} should have become {expected}");
    }
}
