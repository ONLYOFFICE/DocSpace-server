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
/// The users a user is offered for a document of its own, <c>GET /people/file/{id}</c>, from the rooms it is in: the
/// owners and the members of those rooms, directly or through a group. A Restrict record, a room template and the
/// manager row of a group make nobody known; a room in the archive still does.
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
    /// A room moved to the archive stays a room of its members: they still open it there and see each other in it,
    /// so a co-member of an archived room remains offered.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_OffersCoMemberOfArchivedRoom()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        var stranger = await InviteContact(EmployeeType.User);
        var roomId = await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        await ArchiveRoomAsync(roomId);

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
        var coMember = await InviteContact(EmployeeType.User);
        var templateRecipient = await InviteContact(EmployeeType.RoomAdmin);
        var templateId = await CreateTemplateSharedWithAsync(caller.Email, templateRecipient.Email);

        // Without the caller's own record on the template the test would pass for the wrong reason.
        var templateShares = (await _roomsApi.GetRoomSecurityInfoAsync(templateId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        templateShares.Select(s => s.SharedToUser?.Id).Should().Contain([caller.Id, templateRecipient.Id]);

        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        // The co-member of a real room is offered, so the absence is not that of an empty list.
        offered.Select(u => u.Id).Should().Contain(coMember.Id).And.NotContain(templateRecipient.Id);
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
    /// A Restrict record of a group is no membership either: a user that is in a room only through a group restricted
    /// from it is out of that room, so the members of the room are no people it works with.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUserWhoseGroupIsRestrictedFromRoom_DoesNotOfferRoomMembers()
    {
        var caller = await InviteContact(EmployeeType.User);
        var roomMember = await InviteContact(EmployeeType.User);
        var callersGroup = await CreateGroupAsync(members: [caller.Id]);
        var roomId = await CreateRoomWithMembersAsync(callersGroup.Id, roomMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        // Offered through the group first, so the absence below comes from the restriction alone.
        (await UsersOfferedForFileAsync(caller, fileId)).Select(u => u.Id).Should().Contain(roomMember.Id);

        await RestrictInRoomAsync(roomId, callersGroup.Id, templateSubject: callersGroup.Id);
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
    /// A group restricted from the caller's room is not in that room, so its members are no co-members of the caller.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_DoesNotOfferMembersOfGroupRestrictedFromItsRoom()
    {
        var caller = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [groupMember.Id]);
        var roomId = await CreateRoomWithMembersAsync(caller.Id, group.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        (await UsersOfferedForFileAsync(caller, fileId)).Select(u => u.Id).Should().Contain(groupMember.Id);

        await RestrictInRoomAsync(roomId, group.Id, templateSubject: group.Id);
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
}
