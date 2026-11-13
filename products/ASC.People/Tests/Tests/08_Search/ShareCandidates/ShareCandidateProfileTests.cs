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
/// The profile of a share candidate. A user has no access to the contacts of the portal, so on every endpoint that
/// offers it accounts it gets the short profile - the name, the email, the type - without contacts, phone, location,
/// notes and the administrative details; a room admin gets the full one.
/// </summary>
public class ShareCandidateProfileTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
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
        AssertShortProfile(profile);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFoldersShared_AsUser_ReturnsShortProfileWithoutContactDetails()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        await FillContactDetailsAsync(coMember);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var folderId = await CreateFolderInMyDocumentsAsync(caller);

        var offered = await UsersOfferedForFolderAsync(caller, folderId);

        AssertShortProfile(offered.Should().ContainSingle(u => u.Id == coMember.Id).Which);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetAccountsEntriesWithFilesShared_AsUser_ReturnsShortProfileWithoutContactDetails()
    {
        var caller = await InviteContact(EmployeeType.User);
        var coMember = await InviteContact(EmployeeType.User);
        await FillContactDetailsAsync(coMember);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        await _peopleClient.Authenticate(caller);
        var entries = (await _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(
            fileId,
            filterValue: coMember.Email,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var profile = entries.Select(e => e.ActualInstance).OfType<EmployeeFullDto>().Should().ContainSingle(u => u.Id == coMember.Id).Which;
        AssertShortProfile(profile);
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

    /// <summary>
    /// What a user or a guest is not given of another account. The room admin case of this class reads the same
    /// fields filled in, so these checks cannot pass by default.
    /// </summary>
    private static void AssertShortProfile(EmployeeFullDto profile)
    {
        profile.Location.Should().BeNull();
        profile.Notes.Should().BeNull();
        profile.Contacts.Should().BeNullOrEmpty();
        profile.MobilePhone.Should().BeNull();
        profile.UserName.Should().BeNull();
        profile.RegistrationDate.Should().BeNull();
    }
}
