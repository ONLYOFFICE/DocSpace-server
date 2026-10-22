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

namespace ASC.People.Tests.Tests._14_GroupSearch;

/// <summary>
/// The "Everyone" system group in <c>GET /api/2.0/group/file/{id}</c> and
/// <c>GET /api/2.0/group/folder/{id}</c>. Everyone has no row in <c>core_group</c>, yet the
/// share dialog has to offer it next to the portal's own groups: it is always listed, flagged as a
/// system group, marked shared exactly when the entry is shared with it, found by name and hidden
/// by <c>excludeShared</c> once shared.
/// </summary>
public class GroupEveryoneSharedTests(AspireAppFixture fixture) : GroupSearchTestBase(fixture)
{
    // Constants.GroupEveryone.ID of ASC.Core.Common
    private static readonly Guid _everyoneId = new("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e");

    [Fact]
    public async Task GetGroupsWithFilesShared_SharedWithEveryone_EveryoneIsSharedSystemGroup()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        await ShareFileWithGroupAsync(fileId, _everyoneId, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var everyone = result.Single(g => g.Id == _everyoneId);
        everyone.Shared.Should().Be(true);
        everyone.IsSystem.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_NotShared_EveryoneListedButNotShared()
    {
        var fileId = await CreateFileInMyDocumentsAsync();

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Single(g => g.Id == _everyoneId).Shared.Should().NotBe(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_EveryoneAndPortalGroup_BothListedWithTheirFlags()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, _everyoneId, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Single(g => g.Id == _everyoneId).Shared.Should().Be(true);
        var portalGroup = result.Single(g => g.Id == group.Id);
        portalGroup.Shared.Should().NotBe(true);
        portalGroup.IsSystem.Should().NotBe(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_SearchByEveryoneName_FindsEveryone()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, _everyoneId, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, filterValue: "Every", cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().ContainSingle(g => g.Id == _everyoneId);
        result.Should().OnlyContain(g => g.Id == _everyoneId);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_ExcludeShared_HidesEveryoneOnceShared()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, _everyoneId, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, excludeShared: true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().NotContain(g => g.Id == _everyoneId);
        result.Should().Contain(g => g.Id == group.Id);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_EveryoneShareRemoved_EveryoneNoLongerShared()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        await ShareFileWithGroupAsync(fileId, _everyoneId, FileShare.Read);
        await ShareFileWithGroupAsync(fileId, _everyoneId, FileShare.None);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Single(g => g.Id == _everyoneId).Shared.Should().NotBe(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_Paging_EveryoneCountedLikeAnyGroup()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        await CreateGroupAsync();
        await CreateGroupAsync();

        var all = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var firstPage = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, count: 2, startIndex: 0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var secondPage = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, count: 2, startIndex: 2, cancellationToken: TestContext.Current.CancellationToken)).Response;

        all.Should().HaveCount(3, "two portal groups plus Everyone");
        firstPage.Concat(secondPage).Select(g => g.Id).Should().BeEquivalentTo(all.Select(g => g.Id));
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_SharedWithEveryone_EveryoneIsSharedSystemGroup()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        await ShareFolderWithGroupAsync(folderId, _everyoneId, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var everyone = result.Single(g => g.Id == _everyoneId);
        everyone.Shared.Should().Be(true);
        everyone.IsSystem.Should().Be(true);
    }
}
