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
/// The request filters of the share candidates of a document, <c>GET /people/file/{id}</c>, for a user: the search
/// text finds the accounts the user knows from the access list as it finds any other, and the inviter filter tells
/// the user nothing about whom somebody else invited.
/// </summary>
public class ShareCandidateFiltersTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
    /// <summary>
    /// The search text is matched word by word, and an account the user knows only because it is on the access
    /// list of the user's document was matched as one string there, so a first and a last name lost it.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsUser_FindsAccessListAccountByFirstAndLastName()
    {
        var caller = await InviteContact(EmployeeType.User);
        var recipient = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto([new FileShareParams(recipient.Id, FileShare.Read)], notify: false),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(Owner);
        var profile = (await _profilesApi.GetProfileByUserIdAsync(recipient.Id.ToString(), TestContext.Current.CancellationToken)).Response;

        var found = await UsersOfferedForFileAsync(caller, fileId, filterValue: $"{profile.FirstName} {profile.LastName}");

        found.Should().ContainSingle(u => u.Id == recipient.Id).Which.Shared.Should().Be(true);
    }

    /// <summary>
    /// Who invited an account is shown to DocSpace admins only, so for a user and for a room admin <c>inviterId</c>
    /// filters only when it names the caller itself; for anybody else it is ignored rather than telling whom that
    /// account invited. It was first closed for a user only, and a room admin could still filter by any inviter.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsNonAdmin_FiltersByInviterOnlyForItself(EmployeeType callerType)
    {
        var caller = await InviteContact(callerType);
        var coMember = await InviteContact(EmployeeType.User);
        await CreateRoomWithMembersAsync(caller.Id, coMember.Id);
        var fileId = await CreateFileInMyDocumentsAsync(caller);

        // The co-member invited nobody: honoured, the filter would leave it out and so tell the caller just that.
        var byAnotherInviter = await UsersOfferedForFileAsync(caller, fileId, inviterId: coMember.Id);
        var byItself = await UsersOfferedForFileAsync(caller, fileId, inviterId: caller.Id);

        byAnotherInviter.Select(u => u.Id).Should().Contain(coMember.Id);
        byItself.Select(u => u.Id).Should().NotContain(coMember.Id);
    }

    /// <summary>
    /// A DocSpace admin is shown who invited an account, so for it <c>inviterId</c> filters by any account.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithFilesShared_AsDocSpaceAdmin_FiltersByAnyInviter()
    {
        var invitedNobody = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync();

        var unfiltered = await UsersOfferedForFileAsync(Owner, fileId);
        var byAnotherInviter = await UsersOfferedForFileAsync(Owner, fileId, inviterId: invitedNobody.Id);

        unfiltered.Select(u => u.Id).Should().Contain(invitedNobody.Id);
        byAnotherInviter.Should().BeEmpty();
    }

    private async Task<List<EmployeeFullDto>> UsersOfferedForFileAsync(User caller, int fileId, string? filterValue = null, Guid? inviterId = null)
    {
        await _peopleClient.Authenticate(caller);

        return (await _peopleSearchApi.GetUsersWithFilesSharedAsync(
            fileId,
            employeeStatus: EmployeeStatus.Active,
            inviterId: inviterId,
            area: AccountSearchArea.People,
            count: 100,
            filterValue: filterValue,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
    }
}
