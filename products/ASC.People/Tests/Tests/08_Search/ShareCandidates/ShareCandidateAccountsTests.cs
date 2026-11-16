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

namespace ASC.People.Tests.Tests._08_Search.ShareCandidates;

/// <summary>
/// The combined search of users and groups, <c>GET /accounts/{file|folder}/{id}/search</c>, for a caller without
/// access to the contacts of the portal: it finds the accounts the caller works with and no group but Everyone.
/// </summary>
public class ShareCandidateAccountsTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetAccountsEntriesWithFilesShared_AsUser_FindsCoMemberButNotStrangerByName()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        var stranger = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var coMemberName = await FullNameOfAsync(coMember);
        var strangerName = await FullNameOfAsync(stranger);

        await _peopleClient.Authenticate(caller);
        var coMemberFound = await SearchAccountIdsAsync(fileId, coMemberName);
        var strangerFound = await SearchAccountIdsAsync(fileId, strangerName);

        coMemberFound.Should().Contain(coMember.Id);
        strangerFound.Should().NotContain(stranger.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetAccountsEntriesWithFoldersShared_AsUser_FindsCoMemberButNotStrangerByEmail()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        var stranger = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var folderId = await CreateFolderInMyDocumentsAsync(caller);

        await _peopleClient.Authenticate(caller);
        var coMemberFound = await SearchFolderAccountIdsAsync(folderId, coMember.Email);
        var strangerFound = await SearchFolderAccountIdsAsync(folderId, stranger.Email);

        coMemberFound.Should().Contain(coMember.Id);
        strangerFound.Should().BeEmpty();
    }

    /// <summary>
    /// A guest with full access to somebody's document may search whom to share it with, and is offered
    /// by the same rule as a user: the members of its rooms, never a stranger.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetAccountsEntriesWithFilesShared_AsGuestWithFullAccess_FindsCoMemberButNotStranger()
    {
        var guest = await InviteGuest();
        var coMember = await InviteContact(EmployeeType.User);
        var stranger = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(guest.Id, coMember.Id);

        var fileId = await CreateFileInMyDocumentsAsync();
        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto([new FileShareParams(guest.Id, FileShare.ReadWrite)], notify: false),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(guest);
        var coMemberFound = await SearchAccountIdsAsync(fileId, coMember.Email);
        var strangerFound = await SearchAccountIdsAsync(fileId, stranger.Email);

        coMemberFound.Should().Contain(coMember.Id);
        strangerFound.Should().BeEmpty();
    }

    /// <summary>
    /// A user is offered no group but Everyone, so searching accounts by the name of a group finds no group.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetAccountsEntriesWithFilesShared_AsUserSearchingAGroupByName_FindsNoGroup()
    {
        var group = await CreateGroupAsync();
        var caller = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        await _peopleClient.Authenticate(caller);
        var groupEntries = (await _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(fileId, filterValue: group.Name, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var everyoneEntries = (await _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(fileId, filterValue: "Everyone", cancellationToken: TestContext.Current.CancellationToken)).Response;

        // The Everyone group does come back as a group here, so an empty result above is not a group the
        // client failed to recognise.
        everyoneEntries.Select(e => e.ActualInstance).OfType<GroupDto>().Should().ContainSingle(g => g.Id == EveryoneGroupId);
        groupEntries.Select(e => e.ActualInstance).OfType<GroupDto>().Should().BeEmpty();
    }

    private async Task<List<Guid>> SearchAccountIdsAsync(int fileId, string filterValue)
    {
        var entries = (await _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(
            fileId,
            filterValue: filterValue,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        return entries
            .Select(e => e.ActualInstance)
            .OfType<EmployeeFullDto>()
            .Select(u => u.Id)
            .ToList();
    }

    private async Task<List<Guid>> SearchFolderAccountIdsAsync(int folderId, string filterValue)
    {
        var entries = (await _peopleSearchApi.GetAccountsEntriesWithFoldersSharedAsync(
            folderId,
            filterValue: filterValue,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        return entries
            .Select(e => e.ActualInstance)
            .OfType<EmployeeFullDto>()
            .Select(u => u.Id)
            .ToList();
    }
}
