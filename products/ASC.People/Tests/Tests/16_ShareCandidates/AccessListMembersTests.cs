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
/// What the access list of an entry shows of the accounts on it: the members of a group given access, through
/// <c>GET /files/file/{id}/group/{groupId}/share</c>, and the profiles in a room's access list. The members of a group
/// are part of the access list, so only who may see that list gets them, and a user or a guest gets no contacts of
/// another account from either.
/// </summary>
public class AccessListMembersTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
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
        var group = await CreateGroupAsync(members: [groupMember.Id]);
        var fileId = await CreateFileInMyDocumentsAsync();

        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto(
                [new FileShareParams(reader.Id, FileShare.Read), new FileShareParams(group.Id, FileShare.Read)],
                notify: false),
            TestContext.Current.CancellationToken);

        // The owner manages the access and sees the member, so the empty answer below comes from the reader's rights.
        var asOwner = await GroupMembersOfFileAsync(Owner, fileId, group.Id);
        asOwner.Response.Select(m => m.User.Id).Should().Contain(groupMember.Id);

        var asReader = await GroupMembersOfFileAsync(reader, fileId, group.Id);

        asReader.Response.Should().BeEmpty();
        asReader.Total.Should().Be(0);
    }

    /// <summary>
    /// A group without access to the document has no members to list, and the total used to count them all the same
    /// - the size of any group of the portal, and with a filter whether a name or an email is among its members.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetGroupsMembersWithFileSecurity_ForGroupWithoutAccess_CountsNoMembers()
    {
        var groupMember = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [groupMember.Id]);
        var fileId = await CreateFileInMyDocumentsAsync();

        var members = await GroupMembersOfFileAsync(Owner, fileId, group.Id);

        members.Response.Should().BeEmpty();
        members.Total.Should().Be(0);
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
        var roomId = await CreateRoomWithMembersAsync(caller.Id, coMember.Id);

        // An administrator gets the contacts, so the empty fields below come from the caller's type.
        var asOwner = await RoomMemberProfileAsync(Owner, roomId, coMember.Id);
        asOwner.Location.Should().Be(ProfileLocation);

        var asUser = await RoomMemberProfileAsync(caller, roomId, coMember.Id);

        asUser.Location.Should().BeNull();
        asUser.Notes.Should().BeNull();
        asUser.Contacts.Should().BeNullOrEmpty();
        asUser.MobilePhone.Should().BeNull();
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

    private async Task<EmployeeFullDto> RoomMemberProfileAsync(User caller, int roomId, Guid memberId)
    {
        await _filesClient.Authenticate(caller);

        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, count: 100, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return shares.Should().ContainSingle(s => s.SharedToUser != null && s.SharedToUser.Id == memberId).Which.SharedToUser;
    }
}
