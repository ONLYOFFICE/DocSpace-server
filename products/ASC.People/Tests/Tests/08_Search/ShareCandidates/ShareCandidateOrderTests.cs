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

namespace ASC.People.Tests.Tests._08_Search.ShareCandidates;

/// <summary>
/// The order of the accounts in the share candidates of a document, <c>GET /people/file/{id}</c>, and in the
/// users of a room's access list, <c>GET /files/rooms/{id}/share</c> with <c>filterType</c> User: the activated
/// accounts come first, then those with an unconfirmed email, then the pending invitations. The lists were sorted
/// by the number of the activation status, which put the accounts with an unconfirmed email (0) on top.
/// </summary>
public class ShareCandidateOrderTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
    [Fact]
    public async Task GetUsersWithFilesShared_ListsActivatedThenUnconfirmedThenPending()
    {
        var activated = await InviteContact(EmployeeType.User);
        var unconfirmed = await InviteContact(EmployeeType.User);
        var pendingId = await InvitePendingAsync();
        var fileId = await CreateFileInMyDocumentsAsync();
        await SetActivationStatusAsync(activated.Id, EmployeeActivationStatus.Activated);
        await SetActivationStatusAsync(unconfirmed.Id, EmployeeActivationStatus.NotActivated);

        await _peopleClient.Authenticate(Owner);
        var offered = (await _peopleSearchApi.GetUsersWithFilesSharedAsync(
            fileId,
            area: AccountSearchArea.People,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        AssertOrder(offered.Select(u => u.Id).ToList(), activated.Id, unconfirmed.Id, pendingId);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_UserFilter_ListsActivatedThenUnconfirmedThenPending()
    {
        var activated = await InviteContact(EmployeeType.User);
        var unconfirmed = await InviteContact(EmployeeType.User);
        var pendingId = await InvitePendingAsync();
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateRoomWithMembersAsync(activated.Id, unconfirmed.Id, pendingId);
        await SetActivationStatusAsync(activated.Id, EmployeeActivationStatus.Activated);
        await SetActivationStatusAsync(unconfirmed.Id, EmployeeActivationStatus.NotActivated);

        await _filesClient.Authenticate(Owner);
        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(
            roomId,
            filterType: ShareFilterType.User,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        AssertOrder(shares.Select(s => s.SharedToUser.Id).ToList(), activated.Id, unconfirmed.Id, pendingId);
    }

    private static void AssertOrder(List<Guid> ids, Guid activated, Guid unconfirmed, Guid pending)
    {
        ids.Should().Contain([activated, unconfirmed, pending]);
        ids.IndexOf(activated).Should().BeLessThan(ids.IndexOf(unconfirmed), "an activated account comes before one with an unconfirmed email");
        ids.IndexOf(unconfirmed).Should().BeLessThan(ids.IndexOf(pending), "an account with an unconfirmed email comes before a pending invitation");
    }

    private async Task<Guid> InvitePendingAsync()
    {
        await _peopleClient.Authenticate(Owner);
        var email = Initializer.Faker.Internet.Email();
        var invited = await _profilesApi.InviteUsersAsync(
            new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.User, Email = email }]),
            TestContext.Current.CancellationToken);

        return invited.Response.Single(u => u.DisplayName == email).Id;
    }

    /// <summary>
    /// An account added by <c>InviteContact</c> has not confirmed its email, and no API sets the status without a
    /// confirmation link, so the status is written directly. The lists are read with plain SQL, so the cached profile
    /// does not matter.
    /// </summary>
    private async Task SetActivationStatusAsync(Guid userId, EmployeeActivationStatus status)
    {
        var stored = (CoreEmployeeActivationStatus)(int)status;

        await using var db = await Fixture.CreateDbContextAsync<UserDbContext>(TestContext.Current.CancellationToken);

        (await db.Users
            .Where(user => user.TenantId == TenantId && user.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.ActivationStatus, stored), TestContext.Current.CancellationToken))
            .Should().Be(1);
    }
}
