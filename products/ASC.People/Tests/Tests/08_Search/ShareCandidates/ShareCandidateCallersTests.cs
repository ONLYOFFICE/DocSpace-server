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
/// The share candidates by the type of the caller and of the candidate: administrators and room admins are offered
/// every member, a user only the accounts it works with, and a guest is offered only to a caller related to it.
/// </summary>
public class ShareCandidateCallersTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsDocSpaceAdmin_OffersEveryMember()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var stranger = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync(admin);

        var offered = await UsersOfferedForFileAsync(admin, fileId);

        offered.Select(u => u.Id).Should().Contain(stranger.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsOwner_OffersEveryMember()
    {
        var stranger = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync();

        var offered = await UsersOfferedForFileAsync(Owner, fileId);

        offered.Select(u => u.Id).Should().Contain(stranger.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFoldersShared_AsUser_OffersRoomCoMemberButNotStranger()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        var stranger = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var folderId = await CreateFolderInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFolderAsync(caller, folderId);

        offered.Select(u => u.Id).Should().Contain(coMember.Id).And.NotContain(stranger.Id);
    }

    /// <summary>
    /// A share to a guest is accepted only when the caller is related to that guest, so a guest the user
    /// met in a room only is not offered either - it used to be offered and then silently dropped on save.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_DoesNotOfferGuestMetOnlyInARoom()
    {
        var caller = await InviteContact(EmployeeType.User);
        var roomGuest = await InviteGuest();
        var coMember = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(caller.Id, roomGuest.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var guestsArea = await UsersOfferedForFileAsync(caller, fileId, AccountSearchArea.Guests);
        var allAreas = await UsersOfferedForFileAsync(caller, fileId, AccountSearchArea.All);

        guestsArea.Select(u => u.Id).Should().NotContain(roomGuest.Id);
        // The user of the same room is offered, so the guest is left out as a guest, not with an empty list.
        allAreas.Select(u => u.Id).Should().Contain(coMember.Id).And.NotContain(roomGuest.Id);
    }

    /// <summary>
    /// A guest the user is related to is offered even when it is in none of the user's rooms. A user
    /// cannot relate itself to a guest, so the relation is made while it is still a room admin -
    /// inviting the guest to a room by email - and the guest then leaves the room before the demotion.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_OffersGuestItIsRelatedTo()
    {
        var caller = await InviteContact(EmployeeType.RoomAdmin);
        var guest = await InviteGuest();
        var roomId = await CreateRoomWithMembersAsync();
        await InviteToRoom(roomId, caller.Id, FileShare.RoomManager);

        await _filesClient.Authenticate(caller);
        await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest([new RoomInvitation { Email = guest.Email, Access = FileShare.Read }], notify: false, message: "", culture: "en-US"),
            cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, guest.Id, FileShare.None);

        await _peopleClient.Authenticate(Owner);
        await _userTypeApi.StartUserTypeUpdateAsync(new StartUpdateUserTypeDto(EmployeeType.User, caller.Id), TestContext.Current.CancellationToken);
        var demotion = await UserTypeUpdatePolling.WaitForCompletionAsync(_userTypeApi, caller.Id);
        demotion.IsCompleted.Should().BeTrue();
        demotion.Error.Should().BeEmpty();

        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId, AccountSearchArea.Guests);

        offered.Select(u => u.Id).Should().Contain(guest.Id);
    }

    /// <summary>
    /// A guest is offered to a caller related to it, and in the area of all accounts also to a caller that sees it on
    /// the access list of the entry: a room admin gets the guest of its room, marked as having access, although it is
    /// not related to it, and no guest from outside the room.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithRoomShared_AsRoomAdminNotRelatedToGuest_OffersOnlyTheGuestOfTheRoom()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var roomGuest = await InviteGuest();
        var otherGuest = await InviteGuest();
        var roomId = await CreateRoomWithMembersAsync(roomAdmin.Id, roomGuest.Id);

        await _peopleClient.Authenticate(roomAdmin);
        var offered = (await _peopleSearchApi.GetUsersWithRoomSharedAsync(
            roomId,
            employeeStatus: EmployeeStatus.Active,
            area: AccountSearchArea.All,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        offered.Should().ContainSingle(u => u.Id == roomGuest.Id).Which.Shared.Should().Be(true);
        offered.Select(u => u.Id).Should().NotContain(otherGuest.Id);
    }
}
