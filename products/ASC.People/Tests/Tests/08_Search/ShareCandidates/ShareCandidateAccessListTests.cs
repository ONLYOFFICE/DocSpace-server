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
/// What the access list of a document adds to the users offered through <c>GET /people/file/{id}</c>, and what the
/// answer tells about that list. The accounts on the list are offered, and marked as having access, only to a caller
/// that may manage the access of the document.
/// </summary>
public class ShareCandidateAccessListTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
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

    /// <summary>
    /// A group the document is shared with is on its access list as a group: its members are not made known to the
    /// user by that, unlike an account the document is shared with in person.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_DoesNotOfferMembersOfGroupTheFileIsSharedWith()
    {
        var caller = await InviteContact(EmployeeType.User);
        var sharedWith = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [groupMember.Id]);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto(
                [new FileShareParams(sharedWith.Id, FileShare.Read), new FileShareParams(group.Id, FileShare.Read)],
                notify: false),
            TestContext.Current.CancellationToken);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        offered.Select(u => u.Id).Should().Contain(sharedWith.Id).And.NotContain(groupMember.Id);
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
        var coMember = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(reader.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync();

        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto(
                [new FileShareParams(reader.Id, FileShare.Read), new FileShareParams(otherRecipient.Id, FileShare.Read)],
                notify: false),
            TestContext.Current.CancellationToken);

        var offered = await UsersOfferedForFileAsync(reader, fileId);

        // The reader's room co-member is offered, so the absence is not that of an empty list.
        offered.Select(u => u.Id).Should().Contain(coMember.Id).And.NotContain(otherRecipient.Id);
    }

    /// <summary>
    /// A room co-member is offered to a user that may only read somebody's document, but whether the document
    /// is shared with that co-member is not told - neither by <c>shared</c>, nor by <c>includeShared</c> and its total,
    /// nor by <c>excludeShared</c> leaving it out - since the access list of the document is not shown to it.
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
        var onlyShared = await UsersOfFileByAccessAsync(reader, fileId, includeShared: true);
        var withoutShared = await UsersOfFileByAccessAsync(reader, fileId, excludeShared: true);

        offered.Should().ContainSingle(u => u.Id == coMember.Id).Which.Shared.Should().Be(false);
        onlyShared.Response.Should().BeEmpty();
        onlyShared.Total.Should().Be(0);
        withoutShared.Response.Select(u => u.Id).Should().Contain(coMember.Id);
    }

    /// <summary>
    /// A document of a room is shared through the room, and a member that may not manage its access is not told who
    /// has access to it - the same rule as for a document it may only read - although the co-members are offered.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUserMemberForAFileOfTheRoom_DoesNotTellWhoHasAccess()
    {
        var member = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        var roomId = await CreateRoomWithMembersAsync(member.Id, coMember.Id);
        var fileId = await CreateFileInRoomAsync(roomId);

        var offered = await UsersOfferedForFileAsync(member, fileId);
        var onlyShared = await UsersOfFileByAccessAsync(member, fileId, includeShared: true);

        offered.Should().ContainSingle(u => u.Id == coMember.Id).Which.Shared.Should().Be(false);
        onlyShared.Response.Should().BeEmpty();
        onlyShared.Total.Should().Be(0);
    }

    /// <summary>
    /// The same for a document of a public room: this search tells who has access only to a caller that may manage
    /// it, whatever the type of the room, so a member that may not is told nothing here either.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUserMemberForAFileOfAPublicRoom_DoesNotTellWhoHasAccess()
    {
        var member = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room " + Guid.NewGuid().ToString()[..8]);
        await InviteToRoom(room.Id, member.Id, FileShare.ContentCreator);
        await InviteToRoom(room.Id, coMember.Id, FileShare.ContentCreator);
        var fileId = await CreateFileInRoomAsync(room.Id);

        var offered = await UsersOfferedForFileAsync(member, fileId);
        var onlyShared = await UsersOfFileByAccessAsync(member, fileId, includeShared: true);

        offered.Should().ContainSingle(u => u.Id == coMember.Id).Which.Shared.Should().Be(false);
        onlyShared.Response.Should().BeEmpty();
        onlyShared.Total.Should().Be(0);
    }
}
