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
/// One export makes one file, and simultaneous exports neither collide nor stray into each other's folders. Ported
/// from messages.spec.ts, "AI Messages - text-to-docx naming and concurrency".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxConcurrencyTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    [Fact]
    public async Task Export_OneAcceptedExport_CreatesExactlyOneFile()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var before = (await WaitForStableFolderFilesAsync(folderId)).ConvertAll(file => file.Id);
        var title = $"Exported {RandomSuffix()}";

        await ExportAcceptedAsync(title, "hello", folderId);
        (await WaitForExportedFileAsync(folderId, $"{title}.docx")).Should().NotBeNull();

        // Long enough for a stray second job to land too.
        await WaitForExportToSettleAsync();

        var added = (await ListFolderFilesAsync(folderId)).Where(file => !before.Contains(file.Id)).Select(file => file.Title);
        added.Should().Equal($"{title}.docx");
    }

    /// <summary>
    /// Two simultaneous exports with one title must land as <c>&lt;title&gt;.docx</c> and <c>&lt;title&gt; (1).docx</c>.
    /// Both pick the free name before either file exists and land as two files named <c>&lt;title&gt;.docx</c> in one
    /// folder — measured on 2026-11-02 in both runs, alone and in the full suite.
    /// </summary>
    [Fact]
    [Trait("Bug", "84459")]
    public async Task Export_TwoSimultaneousExportsWithOneTitle_BothSurviveUnderDistinctNames()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var before = (await WaitForStableFolderFilesAsync(folderId)).ConvertAll(file => file.Id);
        var title = $"Exported {RandomSuffix()}";

        await Task.WhenAll(
            ExportAcceptedAsync(title, "ALPHA text of the first export.", folderId),
            ExportAcceptedAsync(title, "BRAVO text of the second export.", folderId));

        string[] names = [$"{title}.docx", $"{title} (1).docx"];

        // Both exports land, under whatever names; the comparison below is what catches a collision.
        await WaitForNewFilesAsync(folderId, before, names.Length);

        var added = (await WaitForStableFolderFilesAsync(folderId)).Where(file => !before.Contains(file.Id)).ToList();
        added.Select(file => file.Title).Should().BeEquivalentTo(
            names,
            $"files added (id:title): {string.Join(", ", added.Select(file => $"{file.Id}:{file.Title}"))}");

        var texts = new List<string>();

        foreach (var file in added)
        {
            texts.Add(await ReadDocxTextAsync(file.Id));
        }

        texts.Should().Contain(text => text.Contains("ALPHA", StringComparison.Ordinal));
        texts.Should().Contain(text => text.Contains("BRAVO", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Export_SimultaneousExportsWithDifferentTitles_EachLandInTheirOwnFolder()
    {
        var myId = await GetMyDocumentsFolderIdAsync();
        int[] folders = [await CreateFolderAsync(myId, "Autotest Parallel A"), await CreateFolderAsync(myId, "Autotest Parallel B")];

        var jobs = Enumerable.Range(0, 6)
            .Select(i => (Title: $"Exported {i} {RandomSuffix()}", Content: $"CONTENT-{i}-ONLY", FolderId: folders[i % 2]))
            .ToList();

        await Task.WhenAll(jobs.Select(job => ExportAcceptedAsync(job.Title, job.Content, job.FolderId)));

        foreach (var job in jobs)
        {
            (await WaitForExportedFileAsync(job.FolderId, $"{job.Title}.docx")).Should().NotBeNull(
                $"\"{job.Title}.docx\" missing from its folder");
        }

        foreach (var folderId in folders)
        {
            var expected = jobs.Where(job => job.FolderId == folderId).Select(job => $"{job.Title}.docx");
            var titles = (await WaitForStableFolderFilesAsync(folderId)).Select(file => file.Title);

            titles.Should().BeEquivalentTo(expected, "a folder holds its own exports and nobody else's");
        }

        var first = (await ListFolderFilesAsync(folders[0])).Single(file => file.Title == $"{jobs[0].Title}.docx");
        var text = await ReadDocxTextAsync(first.Id);

        text.Should().Contain("CONTENT-0-ONLY");
        text.Should().NotContain("CONTENT-1-ONLY");
    }
}
