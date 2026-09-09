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

namespace ASC.Files.Tests.Tests._02_Folders.Subfolders;

/// <summary>
/// <c>GET /api/2.0/files/@root</c> — the slowest call in the suite, since it builds the full
/// content of every section. Every test here needs the root itself, so none of them can avoid it,
/// unlike the rest of the suite which resolves individual section roots more cheaply.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class RootFoldersTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetRootFolders_ReturnsNonEmptyArrayOfSectionsWithStructure()
    {
        await _filesClient.Authenticate(Owner);

        var result = await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Should().HaveCount(9);
        var titles = result.Response.Select(s => s.Current.Title).ToList();
        titles.Should().Contain(
        [
            "Files", "Rooms", "Trash", "Favorites", "Recent", "Archive", "Shared with me", "AI agents", "Forms"
        ]);
    }

    [Fact]
    public async Task GetRootFolders_EachSectionHasCurrentWithIdFilesFoldersTotal()
    {
        await _filesClient.Authenticate(Owner);

        var result = await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        foreach (var section in result.Response)
        {
            section.Current.Id.Should().BeGreaterThan(0);
            section.Current.Title.Should().NotBeNull();
            section.Current.Security.Read.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetRootFolders_WithoutTrashTrue_ExcludesTrashSection()
    {
        var trashId = await GetTrashFolderIdAsync(Owner);

        var result = await _foldersApi.GetRootFoldersAsync(withoutTrash: true, cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Select(s => s.Current.Id).Should().NotContain(trashId);
    }

    [Fact]
    public async Task GetRootFolders_DefaultResponse_IncludesTrashSection()
    {
        var trashId = await GetTrashFolderIdAsync(Owner);

        var result = await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Select(s => s.Current.Id).Should().Contain(trashId);
    }

    [Fact]
    public async Task GetRootFolders_FilterTypeFoldersOnly_HidesFilesFromMyDocuments()
    {
        var myDocsId = await GetUserFolderIdAsync(Owner);
        await CreateFile("Autotest File FoldersOnly Root", myDocsId);
        await CreateFolder("Autotest Subfolder FoldersOnly Root", myDocsId);

        var result = await _foldersApi.GetRootFoldersAsync(filterType: FilterType.FoldersOnly, cancellationToken: TestContext.Current.CancellationToken);

        var myDocsSection = result.Response.Single(s => s.Current.Id == myDocsId);
        (myDocsSection.Files?.Count ?? 0).Should().Be(0);
        (myDocsSection.Folders ?? []).Select(f => f.Title).Should().Contain("Autotest Subfolder FoldersOnly Root");
    }

    [Fact]
    public async Task GetRootFolders_FilterTypeFilesOnly_HidesFoldersFromMyDocuments()
    {
        var myDocsId = await GetUserFolderIdAsync(Owner);
        await CreateFile("Autotest File FilesOnly Root", myDocsId);
        await CreateFolder("Autotest Subfolder FilesOnly Root", myDocsId);

        var result = await _foldersApi.GetRootFoldersAsync(filterType: FilterType.FilesOnly, cancellationToken: TestContext.Current.CancellationToken);

        var myDocsSection = result.Response.Single(s => s.Current.Id == myDocsId);
        (myDocsSection.Folders?.Count ?? 0).Should().Be(0);
        (myDocsSection.Files ?? []).Any(f => f.Title != null && f.Title.Contains("Autotest File FilesOnly Root")).Should().BeTrue();
    }

    [Fact]
    public async Task GetRootFolders_FilterTypeCustomRooms_ReturnsOnlyCustomRooms()
    {
        var customTitle = "Autotest Custom Room Root Filter";
        var fillingTitle = "Autotest Filling Room Root Filter";
        await CreateCustomRoom(customTitle);
        await CreateFillingFormsRoom(fillingTitle);

        var result = await _foldersApi.GetRootFoldersAsync(filterType: FilterType.CustomRooms, cancellationToken: TestContext.Current.CancellationToken);

        var titles = result.Response.SelectMany(s => s.Folders ?? []).Select(f => f.Title).ToList();
        titles.Should().Contain(customTitle);
        titles.Should().NotContain(fillingTitle);
    }

    [Fact]
    public async Task GetRootFolders_FilterTypeFillingFormsRooms_ReturnsOnlyFillingFormsRooms()
    {
        var fillingTitle = "Autotest Filling Room Root FillingFilter";
        var customTitle = "Autotest Custom Room Root FillingFilter";
        await CreateFillingFormsRoom(fillingTitle);
        await CreateCustomRoom(customTitle);

        var result = await _foldersApi.GetRootFoldersAsync(filterType: FilterType.FillingFormsRooms, cancellationToken: TestContext.Current.CancellationToken);

        var titles = result.Response.SelectMany(s => s.Folders ?? []).Select(f => f.Title).ToList();
        titles.Should().Contain(fillingTitle);
        titles.Should().NotContain(customTitle);
    }

    [Fact]
    public async Task GetRootFolders_FilterTypePublicRooms_ReturnsOnlyPublicRooms()
    {
        var publicTitle = "Autotest Public Room Root PublicFilter";
        var customTitle = "Autotest Custom Room Root PublicFilter";
        await CreatePublicRoom(publicTitle);
        await CreateCustomRoom(customTitle);

        var result = await _foldersApi.GetRootFoldersAsync(filterType: FilterType.PublicRooms, cancellationToken: TestContext.Current.CancellationToken);

        var titles = result.Response.SelectMany(s => s.Folders ?? []).Select(f => f.Title).ToList();
        titles.Should().Contain(publicTitle);
        titles.Should().NotContain(customTitle);
    }

    [Fact]
    public async Task GetRootFolders_CountOne_LimitsItemsPerSection()
    {
        var myDocsId = await GetUserFolderIdAsync(Owner);
        for (var i = 1; i <= 3; i++)
        {
            await CreateFile($"Autotest File Root Count {i}", myDocsId);
        }

        var result = await _foldersApi.GetRootFoldersAsync(count: 1, cancellationToken: TestContext.Current.CancellationToken);

        foreach (var section in result.Response)
        {
            ((section.Files?.Count ?? 0) + (section.Folders?.Count ?? 0)).Should().BeLessThanOrEqualTo(1);
        }

        var myDocsSection = result.Response.Single(s => s.Current.Id == myDocsId);
        myDocsSection.Total.Should().BeGreaterThan(1);
        ((myDocsSection.Files?.Count ?? 0) + (myDocsSection.Folders?.Count ?? 0)).Should().Be(1);
    }

    [Fact]
    public async Task GetRootFolders_FilterValue_FiltersContentByTitle()
    {
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var uniqueTitle = "Autotest FilterValue Unique Root";
        await CreateFile(uniqueTitle, myDocsId);
        await CreateFile("Autotest FilterValue Other Root", myDocsId);

        // A filterValue search is served from the index, which is written asynchronously - poll on a
        // deadline and assert on the last observed state rather than racing the indexer.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        List<FileEntryBaseDto> matchingFiles;

        while (true)
        {
            var result = await _foldersApi.GetRootFoldersAsync(filterValue: uniqueTitle, cancellationToken: TestContext.Current.CancellationToken);

            matchingFiles = result.Response.SelectMany(s => s.Files ?? [])
                .Where(f => f.Title != null && f.Title.Contains(uniqueTitle))
                .ToList();

            if (matchingFiles.Count > 0 || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        matchingFiles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRootFolders_FilterValueWithNoMatches_ReturnsZeroTotalAcrossAllSections()
    {
        await _filesClient.Authenticate(Owner);

        var result = await _foldersApi.GetRootFoldersAsync(filterValue: "zzz_no_match_autotest_xyz_99999", cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Sum(s => s.Total).Should().Be(0);
    }
}
