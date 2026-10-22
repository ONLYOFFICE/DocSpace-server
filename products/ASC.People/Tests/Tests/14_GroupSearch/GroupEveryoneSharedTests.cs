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
/// system group, marked shared exactly when the entry is shared with it - directly or through a
/// parent folder - found by name, hidden by <c>excludeShared</c> once shared, and counted in
/// <c>count</c>, which comes from a separate count query.
/// </summary>
public class GroupEveryoneSharedTests(AspireAppFixture fixture) : GroupSearchTestBase(fixture)
{
    private static readonly Guid _everyoneId = CoreConstants.GroupEveryone.ID;

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

        var wrapper = await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken);

        wrapper.Response.Single(g => g.Id == _everyoneId).Shared.Should().NotBe(true);
        wrapper.Count.Should().Be(1, "Everyone is the only group of a fresh portal");
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
    public async Task GetGroupsWithFilesShared_SearchByEveryoneName_FindsAndCountsEveryone()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, _everyoneId, FileShare.Read);

        var wrapper = await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, filterValue: "Every", cancellationToken: TestContext.Current.CancellationToken);

        wrapper.Response.Should().ContainSingle(g => g.Id == _everyoneId);
        wrapper.Response.Should().OnlyContain(g => g.Id == _everyoneId);
        wrapper.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_ExcludeShared_HidesAndDoesNotCountEveryoneOnceShared()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, _everyoneId, FileShare.Read);

        var wrapper = await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, excludeShared: true, cancellationToken: TestContext.Current.CancellationToken);

        wrapper.Response.Should().NotContain(g => g.Id == _everyoneId);
        wrapper.Response.Should().Contain(g => g.Id == group.Id);
        wrapper.Count.Should().Be(1, "only the portal group is left");
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

        var all = await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken);
        var firstPage = await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, count: 2, startIndex: 0, cancellationToken: TestContext.Current.CancellationToken);
        var secondPage = await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, count: 2, startIndex: 2, cancellationToken: TestContext.Current.CancellationToken);

        all.Response.Should().HaveCount(3, "two portal groups plus Everyone");
        all.Count.Should().Be(3);
        firstPage.Count.Should().Be(2);
        secondPage.Count.Should().Be(1);
        firstPage.Response.Concat(secondPage.Response).Select(g => g.Id).Should().Equal(all.Response.Select(g => g.Id));
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_EveryoneOnParentFolder_EveryoneIsShared()
    {
        var fileId = await CreateFileInFolderSharedWithEveryoneAsync();

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Single(g => g.Id == _everyoneId).Shared.Should().Be(true, "the parent folder shares the file with Everyone");
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_EveryoneOnParentFolder_SearchByNameStillMarksShared()
    {
        var fileId = await CreateFileInFolderSharedWithEveryoneAsync();

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, filterValue: "Every", cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().ContainSingle(g => g.Id == _everyoneId)
            .Which.Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_EveryoneOnParentFolder_ExcludeSharedHidesEveryone()
    {
        var fileId = await CreateFileInFolderSharedWithEveryoneAsync();
        var group = await CreateGroupAsync();

        var wrapper = await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, excludeShared: true, cancellationToken: TestContext.Current.CancellationToken);

        wrapper.Response.Should().NotContain(g => g.Id == _everyoneId);
        wrapper.Response.Should().Contain(g => g.Id == group.Id);
        wrapper.Count.Should().Be(1);
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

    [Fact]
    public async Task SharesAndGroupsOfEveryoneShare_ReadInParallel_EveryResponseIsTheSame()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto(
                [new FileShareParams(_everyoneId, FileShare.Read), new FileShareParams(group.Id, FileShare.Read)],
                notify: false),
            TestContext.Current.CancellationToken);

        var shareLists = Enumerable.Range(0, 5).Select(async _ =>
            string.Join("|", (await _sharingApi.GetFileSecurityInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response
                .Select(e => $"{e.SharedToGroup?.Id}{e.SharedToUser?.Id}:{e.Access}")));
        var groupLists = Enumerable.Range(0, 5).Select(async _ =>
            string.Join("|", (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response
                .Select(g => $"{g.Id}:{g.Shared}")));

        var shares = await Task.WhenAll(shareLists);
        var groups = await Task.WhenAll(groupLists);

        shares.Distinct().Should().ContainSingle();
        shares[0].Should().Contain(_everyoneId.ToString()).And.Contain(group.Id.ToString());
        groups.Distinct().Should().ContainSingle();
        groups[0].Should().Contain($"{_everyoneId}:True");
    }

    private async Task<int> CreateFileInFolderSharedWithEveryoneAsync()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        await ShareFolderWithGroupAsync(folderId, _everyoneId, FileShare.Read);

        var file = await _filesApi.CreateFileAsync(
            folderId,
            new CreateFileRequest("Autotest Inherited " + Guid.NewGuid().ToString()[..8]),
            TestContext.Current.CancellationToken);

        return file.Response.Id;
    }
}
