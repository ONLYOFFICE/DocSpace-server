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
/// The users offered to a caller that shares a document of its own. A user used to be offered every
/// member of the portal, with the full profile of each, although it has no access to the contacts;
/// now it is offered only the owners and members of its rooms (directly or through a group), the
/// accounts it is related to and those that already have access to the document, with a short profile.
/// Guests are offered as to a room admin - only the related ones - because a share to any other guest
/// is not accepted.
/// </summary>
public class ShareCandidateUsersTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_OffersRoomCoMemberButNotStranger()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        var stranger = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().Contain(coMember.Id).And.NotContain(stranger.Id);
    }

    /// <summary>
    /// The owner of a room has no access record of its own - it is the author of the room - so it has to
    /// be offered as well, or a user cannot share with the one who invited it.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_OffersOwnerOfItsRoom()
    {
        var roomOwner = await InviteContact(EmployeeType.RoomAdmin);
        var caller = await InviteContact(EmployeeType.User);
        var roomId = await CreateCustomRoomAsync(roomOwner);
        await InviteToRoom(roomId, caller.Id, FileShare.Editing);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().Contain(roomOwner.Id);
    }

    /// <summary>
    /// A room template keeps a room type but is no room: access to it only allows to create rooms from
    /// it, so the others it is given to are not people the user works with.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_DoesNotOfferRecipientsOfTemplateItWasGiven()
    {
        var caller = await InviteContact(EmployeeType.User);
        var templateRecipient = await InviteContact(EmployeeType.RoomAdmin);
        var templateId = await CreateTemplateSharedWithAsync(caller.Email, templateRecipient.Email);

        // Without the caller's own record on the template the test would pass for the wrong reason.
        var templateShares = (await _roomsApi.GetRoomSecurityInfoAsync(templateId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        templateShares.Select(s => s.SharedToUser?.Id).Should().Contain([caller.Id, templateRecipient.Id]);

        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().NotContain(templateRecipient.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_OffersMembersOfGroupInItsRoom()
    {
        var caller = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [groupMember.Id]);
        await CreateRoomWithMembersAsync(caller.Id, group.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().Contain(groupMember.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUserInRoomThroughGroup_OffersRoomMembers()
    {
        var caller = await InviteContact(EmployeeType.User);
        var roomMember = await InviteContact(EmployeeType.User);
        var stranger = await InviteContact(EmployeeType.User);
        var callersGroup = await CreateGroupAsync(members: [caller.Id]);
        await CreateRoomWithMembersAsync(callersGroup.Id, roomMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().Contain(roomMember.Id).And.NotContain(stranger.Id);
    }

    /// <summary>
    /// A user restricted from a room personally does not see it, even when one of its groups has access,
    /// so the members of that room are no people it works with.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUserRestrictedFromRoomOfItsGroup_DoesNotOfferRoomMembers()
    {
        var caller = await InviteContact(EmployeeType.User);
        var roomMember = await InviteContact(EmployeeType.User);
        var callersGroup = await CreateGroupAsync(members: [caller.Id]);
        var roomId = await CreateRoomWithMembersAsync(callersGroup.Id, roomMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        // Offered through the group first, so the absence below comes from the restriction alone.
        (await UsersOfferedForFileAsync(caller, fileId)).Select(u => u.Id).Should().Contain(roomMember.Id);

        await RestrictInRoomAsync(roomId, caller.Id, templateSubject: callersGroup.Id);
        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().NotContain(roomMember.Id);
    }

    /// <summary>
    /// A Restrict record is no membership: the account holding it is not in the room for the caller.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_DoesNotOfferRoomMemberWithRestrictRecord()
    {
        var caller = await InviteContact(EmployeeType.User);
        var restricted = await InviteContact(EmployeeType.User);
        var roomId = await CreateRoomWithMembersAsync(caller.Id, restricted.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        (await UsersOfferedForFileAsync(caller, fileId)).Select(u => u.Id).Should().Contain(restricted.Id);

        await RestrictInRoomAsync(roomId, restricted.Id, templateSubject: restricted.Id);
        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().NotContain(restricted.Id);
    }

    /// <summary>
    /// A member of a group that is restricted from the group's room personally is out of that room, so it
    /// is no co-member of the caller there.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_DoesNotOfferGroupMemberRestrictedFromItsRoom()
    {
        var caller = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [groupMember.Id]);
        var roomId = await CreateRoomWithMembersAsync(caller.Id, group.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        (await UsersOfferedForFileAsync(caller, fileId)).Select(u => u.Id).Should().Contain(groupMember.Id);

        await RestrictInRoomAsync(roomId, groupMember.Id, templateSubject: group.Id);
        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().NotContain(groupMember.Id);
    }

    /// <summary>
    /// The restriction takes the member out of one room only: the same group in another room of the caller
    /// still makes it a co-member.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_OffersGroupMemberRestrictedFromOnlyOneOfItsRooms()
    {
        var caller = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [groupMember.Id]);
        var restrictedRoomId = await CreateRoomWithMembersAsync(caller.Id, group.Id);
        await CreateRoomWithMembersAsync(caller.Id, group.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        await RestrictInRoomAsync(restrictedRoomId, groupMember.Id, templateSubject: group.Id);
        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().Contain(groupMember.Id);
    }

    /// <summary>
    /// The manager of a group added to the caller's room is kept apart from membership, so a manager that
    /// is no member is not offered, while the members are.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_DoesNotOfferManagerOfGroupInItsRoomWhoIsNoMember()
    {
        var caller = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        var managerOnly = await InviteContact(EmployeeType.RoomAdmin);
        var group = await CreateGroupAsync(members: [groupMember.Id]);
        await MakeManagerOnlyAsync(group.Id, managerOnly.Id);
        await CreateRoomWithMembersAsync(caller.Id, group.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().Contain(groupMember.Id).And.NotContain(managerOnly.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_OffersAccountAlreadySharedWith()
    {
        var caller = await InviteContact(EmployeeType.User);
        var sharedWith = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        await _filesClient.Authenticate(caller);
        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto([new FileShareParams(sharedWith.Id, FileShare.Read)], notify: false),
            TestContext.Current.CancellationToken);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Should().ContainSingle(u => u.Id == sharedWith.Id).Which.Shared.Should().Be(true);
    }

    /// <summary>
    /// The access list of a document is shown only to those who may manage that access, so a user that may only
    /// read somebody's document is not offered the others it is shared with - that would hand it contacts it
    /// has no access to.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUserWithReadAccess_DoesNotOfferOtherRecipients()
    {
        var reader = await InviteContact(EmployeeType.User);
        var otherRecipient = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync();

        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto(
                [new FileShareParams(reader.Id, FileShare.Read), new FileShareParams(otherRecipient.Id, FileShare.Read)],
                notify: false),
            TestContext.Current.CancellationToken);

        var offered = await UsersOfferedForFileAsync(reader, fileId);

        offered.Select(u => u.Id).Should().NotContain(otherRecipient.Id);
    }

    /// <summary>
    /// A room co-member is offered to a user that may only read somebody's document, but whether the document
    /// is shared with that co-member is not told - neither by <c>shared</c> nor by <c>includeShared</c> - since the
    /// access list of the document is not shown to it.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUserWithReadAccess_DoesNotTellWhichCoMembersHaveAccess()
    {
        var reader = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(reader.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync();

        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto(
                [new FileShareParams(reader.Id, FileShare.Read), new FileShareParams(coMember.Id, FileShare.Read)],
                notify: false),
            TestContext.Current.CancellationToken);

        var offered = await UsersOfferedForFileAsync(reader, fileId);
        var onlyShared = (await _peopleSearchApi.GetUsersWithFilesSharedAsync(
            fileId,
            employeeStatus: EmployeeStatus.Active,
            includeShared: true,
            area: AccountSearchArea.People,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        offered.Should().ContainSingle(u => u.Id == coMember.Id).Which.Shared.Should().Be(false);
        onlyShared.Should().BeEmpty();
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
        await CreateRoomWithMembersAsync(caller.Id, roomGuest.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var guestsArea = await UsersOfferedForFileAsync(caller, fileId, AccountSearchArea.Guests);
        var allAreas = await UsersOfferedForFileAsync(caller, fileId, AccountSearchArea.All);

        guestsArea.Select(u => u.Id).Should().NotContain(roomGuest.Id);
        allAreas.Select(u => u.Id).Should().NotContain(roomGuest.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_ReturnsShortProfileWithoutContactDetails()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        await FillContactDetailsAsync(coMember);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        var profile = offered.Should().ContainSingle(u => u.Id == coMember.Id).Which;
        profile.Email.Should().Be(coMember.Email);
        profile.DisplayName.Should().NotBeNullOrEmpty();
        profile.IsCollaborator.Should().BeTrue();
        profile.Location.Should().BeNull();
        profile.Notes.Should().BeNull();
        profile.Contacts.Should().BeNullOrEmpty();
        profile.MobilePhone.Should().BeNull();
        profile.UserName.Should().BeNull();
        profile.RegistrationDate.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsRoomAdmin_OffersEveryMemberWithFullProfile()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var stranger = await InviteContact(EmployeeType.User);
        await FillContactDetailsAsync(stranger);
        var fileId = await CreateFileInMyDocumentsAsync(roomAdmin);

        var offered = await UsersOfferedForFileAsync(roomAdmin, fileId);

        // The same fields the short profile leaves empty, filled in here, so its checks cannot pass by default.
        var profile = offered.Should().ContainSingle(u => u.Id == stranger.Id).Which;
        profile.Location.Should().Be(ProfileLocation);
        profile.Notes.Should().Be(ProfileNotes);
        profile.Contacts.Should().ContainSingle(c => c.Type == ProfileContactType && c.Value == ProfileContactValue);
        profile.UserName.Should().NotBeNullOrEmpty();
        profile.RegistrationDate.Should().NotBeNull();
    }

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
    public async Task GetUsersWithFoldersShared_AsUser_OffersRoomCoMemberButNotStranger()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        var stranger = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var folderId = await CreateFolderInMyDocumentsAsync(caller);

        await _peopleClient.Authenticate(caller);
        var offered = (await _peopleSearchApi.GetUsersWithFoldersSharedAsync(
            folderId,
            employeeStatus: EmployeeStatus.Active,
            area: AccountSearchArea.People,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        offered.Select(u => u.Id).Should().Contain(coMember.Id).And.NotContain(stranger.Id);
    }

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

        await _peopleClient.Authenticate(caller);
        var coMemberFound = await SearchAccountIdsAsync(fileId, coMember.Email);
        var strangerFound = await SearchAccountIdsAsync(fileId, stranger.Email);

        coMemberFound.Should().Contain(coMember.Id);
        strangerFound.Should().BeEmpty();
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
    /// An account the parent folder is shared with already has access to the document, so it is offered
    /// - marked as shared - although it is in none of the user's rooms.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_OffersAccountItsParentFolderIsSharedWith()
    {
        var caller = await InviteContact(EmployeeType.User);
        var folderMember = await InviteContact(EmployeeType.User);
        var folderId = await CreateFolderInMyDocumentsAsync(caller);

        await _sharingApi.SetFolderSecurityInfoAsync(
            folderId,
            new SecurityInfoSimpleRequestDto([new FileShareParams(folderMember.Id, FileShare.Read)], notify: false),
            TestContext.Current.CancellationToken);
        var file = await _filesApi.CreateFileAsync(
            folderId,
            new CreateFileRequest("Autotest File " + Guid.NewGuid().ToString()[..8]),
            TestContext.Current.CancellationToken);

        var offered = await UsersOfferedForFileAsync(caller, file.Response.Id);

        offered.Should().ContainSingle(u => u.Id == folderMember.Id).Which.Shared.Should().Be(true);
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
