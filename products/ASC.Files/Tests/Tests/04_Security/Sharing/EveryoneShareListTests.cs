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

namespace ASC.Files.Tests.Tests._04_Security.Sharing;

/// <summary>
/// The "Everyone" system group in the share lists of a file and a folder
/// (<c>GET /api/2.0/files/file/{id}/share</c>, <c>GET /api/2.0/files/folder/{id}/share</c>).
/// Everyone has no row in <c>core_group</c>, yet the user-or-group share query joins the share
/// records to the groups table; these cases pin that Everyone still comes back as a group entry,
/// with its access level, in the total and on the right page.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class EveryoneShareListTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    private static readonly Guid _everyoneId = CoreConstants.GroupEveryone.ID;

    [Fact]
    public async Task GetFileSecurityInfo_SharedWithEveryone_ListsEveryoneAsSystemGroup()
    {
        var file = await CreateFileInMy("Autotest Everyone Listed.docx", Owner);
        await ShareFile(file.Id, _everyoneId, FileShare.Read);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var everyone = FindEveryone(entries);
        everyone.Should().NotBeNull();
        everyone!.SubjectType.Should().Be(SubjectType.Group);
        everyone.SharedToGroup.IsSystem.Should().BeTrue();
        everyone.SharedToGroup.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedWithEveryone_ReturnsGrantedAccess()
    {
        var file = await CreateFileInMy("Autotest Everyone Access.docx", Owner);
        await ShareFile(file.Id, _everyoneId, FileShare.ReadWrite);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        FindEveryone(entries)!.Access.Should().Be(FileShare.ReadWrite);
    }

    [Fact]
    public async Task GetFileSecurityInfo_EveryoneGroupAndUser_ListedInShareOrder()
    {
        var (fileId, groupId, userId) = await CreateFileSharedWithEveryoneGroupAndUser("Autotest Everyone Mixed.docx");

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // owner first; groups before users, groups by name ("Autotest..." before "Everyone"), the user last
        entries.Should().HaveCount(4);
        entries[0].IsOwner.Should().BeTrue();
        entries[1].SharedToGroup.Should().NotBeNull();
        entries[1].SharedToGroup.Id.Should().Be(groupId, "the portal group sorts before Everyone by name");
        entries[2].SharedToGroup.Should().NotBeNull();
        entries[2].SharedToGroup.Id.Should().Be(_everyoneId);
        entries[3].SharedToUser.Should().NotBeNull();
        entries[3].SharedToUser.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedWithEveryone_TotalCountsEveryone()
    {
        var file = await CreateFileInMy("Autotest Everyone Total.docx", Owner);
        await ShareFile(file.Id, _everyoneId, FileShare.Read);

        var total = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Total;

        total.Should().Be(2, "the owner and Everyone are the two entries of the list");
    }

    [Fact]
    public async Task GetFileSecurityInfo_PagedOneByOne_PagesJoinIntoTheFullList()
    {
        var (fileId, _, _) = await CreateFileSharedWithEveryoneGroupAndUser("Autotest Everyone Paging.docx");

        var full = (await _sharingApi.GetFileSecurityInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var paged = new List<FileShareDto>();

        // startIndex 2 and 3 reach past the owner entry added outside the DAO, so the DAO has to skip rows
        for (var startIndex = 0; startIndex < full.Count; startIndex++)
        {
            var page = (await _sharingApi.GetFileSecurityInfoAsync(fileId, count: 1, startIndex: startIndex, cancellationToken: TestContext.Current.CancellationToken)).Response;
            page.Should().ContainSingle("page {0} holds one entry", startIndex);
            paged.Add(page[0]);
        }

        full.Should().HaveCount(4);
        paged.Select(Key).Should().Equal(full.Select(Key));
    }

    [Fact]
    public async Task GetFileSecurityInfo_EveryoneShareRemoved_NoLongerListed()
    {
        var file = await CreateFileInMy("Autotest Everyone Removed.docx", Owner);
        await ShareFile(file.Id, _everyoneId, FileShare.Read);
        await ShareFile(file.Id, _everyoneId, FileShare.None);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        FindEveryone(entries).Should().BeNull();
        entries.Should().ContainSingle(e => e.IsOwner);
    }

    [Fact]
    public async Task GetFolderSecurityInfo_SharedWithEveryone_ListsEveryone()
    {
        var folder = await CreateFolderInMy("Autotest Everyone Folder", Owner);
        await ShareFolder(folder.Id, _everyoneId, FileShare.Read);

        var entries = (await _sharingApi.GetFolderSecurityInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var everyone = FindEveryone(entries);
        everyone.Should().NotBeNull();
        everyone!.Access.Should().Be(FileShare.Read);
        everyone.SharedToGroup.IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task GetFileSecurityInfo_ReadInParallel_EveryResponseIsTheSame()
    {
        var (fileId, _, _) = await CreateFileSharedWithEveryoneGroupAndUser("Autotest Everyone Parallel.docx");

        var lists = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
            (await _sharingApi.GetFileSecurityInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response));

        lists.Should().OnlyContain(l => l.Count == 4);
        lists.Select(l => string.Join("|", l.Select(Key))).Distinct().Should().ContainSingle();
    }

    private async Task<(int FileId, Guid GroupId, Guid UserId)> CreateFileSharedWithEveryoneGroupAndUser(string title)
    {
        var user = await InviteContact(EmployeeType.User);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto([user.Id], Owner.Id, $"Autotest Everyone Mix {Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy(title, Owner);
        await _sharingApi.SetFileSecurityInfoAsync(
            file.Id,
            new SecurityInfoSimpleRequestDto
            {
                Share =
                [
                    new() { ShareTo = _everyoneId, Access = FileShare.Read },
                    new() { ShareTo = group.Id, Access = FileShare.Read },
                    new() { ShareTo = user.Id, Access = FileShare.ReadWrite }
                ],
                Notify = false
            },
            TestContext.Current.CancellationToken);

        return (file.Id, group.Id, user.Id);
    }

    private static string Key(FileShareDto entry)
    {
        return entry.IsOwner ? "owner" : $"{entry.SharedToGroup?.Id}{entry.SharedToUser?.Id}";
    }

    private static FileShareDto? FindEveryone(IEnumerable<FileShareDto> entries)
    {
        return entries.FirstOrDefault(e => e.SharedToGroup != null && e.SharedToGroup.Id == _everyoneId);
    }
}
