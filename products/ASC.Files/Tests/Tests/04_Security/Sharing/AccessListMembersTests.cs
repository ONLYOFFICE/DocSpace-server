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
/// What the access list of an entry shows of the accounts on it: the members of a group given access, through
/// <c>GET /files/{file|folder}/{id}/group/{groupId}/share</c>, and the profiles in a room's access list. The members
/// of a group are part of the access list, so only who may see that list gets them - who may manage the access, or
/// any member of a room - and a user gets no contacts of another account from either.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class AccessListMembersTests(AspireAppFixture fixture) : SharingTestBase(fixture)
{
    private const string ProfileLocation = "Autotest Location";
    private const string ProfileNotes = "Autotest Notes";

    /// <summary>
    /// A user that may only read somebody's document does not see its access list, and used to get the members of a
    /// group the document is shared with, with the access of each, all the same.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetGroupsMembersWithFileSecurity_AsUserWithReadAccess_ReturnsNoMembers()
    {
        var reader = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(groupMember);
        var file = await CreateFileInMy("Autotest Access List File.docx", Owner);

        await ShareFile(file.Id, reader.Id, FileShare.Read);
        await ShareFile(file.Id, group.Id, FileShare.Read);

        // The owner manages the access and sees the member, so the empty answer below comes from the reader's rights.
        var asOwner = await GroupMembersOfFileAsync(Owner, file.Id, group.Id);
        asOwner.Response.Select(m => m.User.Id).Should().Contain(groupMember.Id);

        var asReader = await GroupMembersOfFileAsync(reader, file.Id, group.Id);

        asReader.Response.Should().BeEmpty();
        asReader.Total.Should().Be(0);
    }

    /// <summary>
    /// The same for a folder: its access list, and the members of a group on it, are for who may manage the access.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetGroupsMembersWithFolderSecurity_AsUserWithReadAccess_ReturnsNoMembers()
    {
        var reader = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(groupMember);
        var folder = await CreateFolderInMy("Autotest Access List Folder", Owner);

        await ShareFolder(folder.Id, reader.Id, FileShare.Read);
        await ShareFolder(folder.Id, group.Id, FileShare.Read);

        var asOwner = await GroupMembersOfFolderAsync(Owner, folder.Id, group.Id);
        asOwner.Response.Select(m => m.User.Id).Should().Contain(groupMember.Id);

        var asReader = await GroupMembersOfFolderAsync(reader, folder.Id, group.Id);

        asReader.Response.Should().BeEmpty();
        asReader.Total.Should().Be(0);
    }

    /// <summary>
    /// A group without access to the document has no members to list, and the total used to count them all the same
    /// - the size of any group of the portal, and with a filter whether a name or an email is among its members.
    /// A group with access is counted, so the zero is that of the group, not of the endpoint.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetGroupsMembersWithFileSecurity_ForGroupWithoutAccess_CountsNoMembers()
    {
        var groupMember = await InviteContact(EmployeeType.User);
        var groupWithAccess = await CreateGroupAsync(groupMember);
        var groupWithoutAccess = await CreateGroupAsync(groupMember);
        var file = await CreateFileInMy("Autotest Access List File.docx", Owner);

        await ShareFile(file.Id, groupWithAccess.Id, FileShare.Read);

        var counted = await GroupMembersOfFileAsync(Owner, file.Id, groupWithAccess.Id);
        var notCounted = await GroupMembersOfFileAsync(Owner, file.Id, groupWithoutAccess.Id);

        // The group counts its manager, the owner, as well; what matters is that a group with access is counted.
        counted.Response.Select(m => m.User.Id).Should().Contain(groupMember.Id);
        counted.Total.Should().BePositive();
        notCounted.Response.Should().BeEmpty();
        notCounted.Total.Should().Be(0);
    }

    /// <summary>
    /// The access list of a room is shown to its members, so a user member gets the members of a group of the room -
    /// but without the contacts, the location and the notes an administrator reads there.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetGroupsMembersWithFolderSecurity_AsUserMemberOfRoom_ReturnsMembersWithoutContacts()
    {
        var caller = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        await FillContactDetailsAsync(groupMember);
        var group = await CreateGroupAsync(groupMember);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Access List Room");
        await InviteToRoom(room.Id, caller, FileShare.Editing);
        await InviteGroupToRoomAsync(room.Id, group.Id, FileShare.Editing);

        var asOwner = await GroupMembersOfFolderAsync(Owner, room.Id, group.Id);
        asOwner.Response.Should().ContainSingle(m => m.User.Id == groupMember.Id).Which.User.Location.Should().Be(ProfileLocation);

        var asUser = await GroupMembersOfFolderAsync(caller, room.Id, group.Id);

        var profile = asUser.Response.Should().ContainSingle(m => m.User.Id == groupMember.Id).Which.User;
        profile.Location.Should().BeNull();
        profile.Notes.Should().BeNull();
        profile.Contacts.Should().BeNullOrEmpty();
        profile.MobilePhone.Should().BeNull();
    }

    /// <summary>
    /// The access list of a room is shown to its members, and a user member got the full profiles on it - contacts,
    /// location and notes - although a user has no access to the contacts of the portal.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetRoomSecurityInfo_AsUserMember_ReturnsNoContactsOfOtherMembers()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        await FillContactDetailsAsync(coMember);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Access List Room");
        await InviteToRoom(room.Id, caller, FileShare.Editing);
        await InviteToRoom(room.Id, coMember, FileShare.Editing);

        // An administrator gets the contacts, so the empty fields below come from the caller's type.
        var asOwner = await RoomMemberProfileAsync(Owner, room.Id, coMember.Id);
        asOwner.Location.Should().Be(ProfileLocation);

        var asUser = await RoomMemberProfileAsync(caller, room.Id, coMember.Id);

        asUser.Location.Should().BeNull();
        asUser.Notes.Should().BeNull();
        asUser.Contacts.Should().BeNullOrEmpty();
        asUser.MobilePhone.Should().BeNull();
    }

    /// <summary>A group created by the owner with the given members.</summary>
    private async Task<GroupDto> CreateGroupAsync(params User[] members)
    {
        await _peopleClient.Authenticate(Owner);

        return (await _groupApi.AddGroupAsync(
            new GroupRequestDto([.. members.Select(m => m.Id)], Owner.Id, "Autotest Group " + Guid.NewGuid().ToString()[..8]),
            TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>Location, notes and contacts can only be set by the account itself, so it signs in to fill them.</summary>
    private async Task FillContactDetailsAsync(User user)
    {
        await _peopleClient.Authenticate(user);
        await _profilesApi.UpdateMemberAsync(
            user.Id.ToString(),
            new UpdateMemberRequestDto(location: ProfileLocation, comment: ProfileNotes, contacts: [new Contact("skype", "autotest.contact")]),
            TestContext.Current.CancellationToken);
    }

    private async Task InviteGroupToRoomAsync(int roomId, Guid groupId, FileShare access)
    {
        await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest
            {
                Invitations = [new RoomInvitation { Id = groupId, Access = access }],
                Notify = false
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task<GroupMemberSecurityArrayWrapper> GroupMembersOfFileAsync(User caller, int fileId, Guid groupId)
    {
        await _filesClient.Authenticate(caller);

        return await _sharingApi.GetGroupsMembersWithFileSecurityAsync(
            fileId,
            groupId,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task<GroupMemberSecurityArrayWrapper> GroupMembersOfFolderAsync(User caller, int folderId, Guid groupId)
    {
        await _filesClient.Authenticate(caller);

        return await _sharingApi.GetGroupsMembersWithFolderSecurityAsync(
            folderId,
            groupId,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task<EmployeeFullDto> RoomMemberProfileAsync(User caller, int roomId, Guid memberId)
    {
        await _filesClient.Authenticate(caller);

        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, count: 100, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return shares.Should().ContainSingle(s => s.SharedToUser != null && s.SharedToUser.Id == memberId).Which.SharedToUser;
    }
}
