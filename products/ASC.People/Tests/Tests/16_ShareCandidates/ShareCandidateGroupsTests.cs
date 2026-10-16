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

namespace ASC.People.Tests.Tests._16_ShareCandidates;

/// <summary>
/// The groups offered to a caller that shares a document of its own. A user used to be offered every
/// group of the portal, although it may not read them; now a caller without the permission to read
/// groups is offered only the Everyone group, while a room admin is offered every group as before.
/// </summary>
public class ShareCandidateGroupsTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetGroupsWithFilesShared_AsUser_OffersOnlyEveryone()
    {
        await CreateGroupAsync();
        var caller = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        await _peopleClient.Authenticate(caller);
        var groups = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        groups.Should().ContainSingle().Which.Id.Should().Be(EveryoneGroupId);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetGroupsWithFilesShared_AsUserSearchingAGroupByName_FindsNothing()
    {
        var group = await CreateGroupAsync();
        var caller = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        await _peopleClient.Authenticate(caller);
        var groups = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, filterValue: group.Name, cancellationToken: TestContext.Current.CancellationToken)).Response;

        groups.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetGroupsWithFoldersShared_AsUser_OffersOnlyEveryone()
    {
        await CreateGroupAsync();
        var caller = await InviteContact(EmployeeType.User);
        var folderId = await CreateFolderInMyDocumentsAsync(caller);

        await _peopleClient.Authenticate(caller);
        var groups = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        groups.Should().ContainSingle().Which.Id.Should().Be(EveryoneGroupId);
    }

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

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetGroupsWithFilesShared_AsRoomAdmin_OffersPortalGroups()
    {
        var group = await CreateGroupAsync();
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var fileId = await CreateFileInMyDocumentsAsync(roomAdmin);

        await _peopleClient.Authenticate(roomAdmin);
        var groups = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        groups.Select(g => g.Id).Should().Contain([group.Id, EveryoneGroupId]);
    }
}
