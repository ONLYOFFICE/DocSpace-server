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
/// now it is offered only the members of its rooms (directly or through a group), the accounts it is
/// related to and those that already have access to the document, with a short profile.
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

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_OffersGuestOfItsRoomInGuestsArea()
    {
        var caller = await InviteContact(EmployeeType.User);
        var roomGuest = await InviteGuest();
        var strangerGuest = await InviteGuest();
        await CreateRoomWithMembersAsync(caller.Id, roomGuest.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId, AccountSearchArea.Guests);

        offered.Select(u => u.Id).Should().Contain(roomGuest.Id).And.NotContain(strangerGuest.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_ReturnsShortProfileWithoutContactDetails()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        await FillLocationAndNotesAsync(coMember);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFileAsync(caller, fileId);

        var profile = offered.Should().ContainSingle(u => u.Id == coMember.Id).Which;
        profile.Email.Should().Be(coMember.Email);
        profile.DisplayName.Should().NotBeNullOrEmpty();
        profile.IsCollaborator.Should().BeTrue();
        profile.Location.Should().BeNull();
        profile.Notes.Should().BeNull();
        profile.RegistrationDate.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsRoomAdmin_OffersEveryMemberWithFullProfile()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var stranger = await InviteContact(EmployeeType.User);
        await FillLocationAndNotesAsync(stranger);
        var fileId = await CreateFileInMyDocumentsAsync(roomAdmin);

        var offered = await UsersOfferedForFileAsync(roomAdmin, fileId);

        var profile = offered.Should().ContainSingle(u => u.Id == stranger.Id).Which;
        profile.Location.Should().Be(ProfileLocation);
        profile.Notes.Should().Be(ProfileNotes);
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

    private const string ProfileLocation = "Autotest Location";
    private const string ProfileNotes = "Autotest Notes";

    /// <summary>
    /// Location and notes can only be set by the account itself, so it signs in to fill them.
    /// </summary>
    private async Task FillLocationAndNotesAsync(User user)
    {
        await _peopleClient.Authenticate(user);
        await _profilesApi.UpdateMemberAsync(
            user.Id.ToString(),
            new UpdateMemberRequestDto(location: ProfileLocation, comment: ProfileNotes),
            TestContext.Current.CancellationToken);
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
}
