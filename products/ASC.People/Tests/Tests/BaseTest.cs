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

namespace ASC.People.Tests.Tests;

public class BaseTest(
    AspireAppFixture fixture
) : IAsyncLifetime
{
    private PortalClients _clients = null!;

    // The portal and its owner created for this test. Both live on the per-portal client bundle,
    // so the owner Id is always the one belonging to this test's own portal — never shared.
    protected User Owner => _clients.Owner;

    protected HttpClient _filesClient = null!;
    protected HttpClient _peopleClient = null!;
    protected HttpClient _webApiClient = null!;

    protected RoomsApi _roomsApi = null!;
    protected FilesApi _filesApi = null!;
    protected FoldersApi _foldersApi = null!;
    protected SharingApi _sharingApi = null!;

    protected ProfilesApi _profilesApi = null!;
    protected PeopleSearchApi _peopleSearchApi = null!;
    protected UserTypeApi _userTypeApi = null!;
    protected UserStatusApi _userStatusApi = null!;
    protected UserDataApi _userDataApi = null!;
    protected EmailApi _emailApi = null!;
    protected PasswordApi _passwordApi = null!;
    protected PhotosApi _photosApi = null!;
    protected PeopleQuotaApi _peopleQuotaApi = null!;
    protected ThemeApi _themeApi = null!;
    protected PeopleGuestsApi _guestsApi = null!;
    protected ThirdPartyAccountsApi _thirdPartyAccountsApi = null!;

    protected GroupApi _groupApi = null!;
    protected GroupSearchApi _groupSearchApi = null!;

    protected UsersApi _portalUsersApi = null!;
    protected CommonSettingsApi _commonSettingsApi = null!;
    protected DocSpace.API.SDK.Api.Settings.QuotaApi _settingsQuotaApi = null!;
    protected WebhooksApi _webhooksApi = null!;

    public async ValueTask InitializeAsync()
    {
        var setupSw = Stopwatch.StartNew();

        // Register a brand-new portal for this test and bind a fresh set of clients to it.
        _clients = await fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        _filesClient = _clients.FilesHttpClient;
        _peopleClient = _clients.PeopleHttpClient;
        _webApiClient = _clients.WebApiHttpClient;

        _roomsApi = _clients.RoomsApi;
        _filesApi = _clients.FilesApi;
        _foldersApi = _clients.FoldersApi;
        _sharingApi = _clients.SharingApi;

        _profilesApi = _clients.ProfilesApi;
        _peopleSearchApi = _clients.PeopleSearchApi;
        _userTypeApi = _clients.UserTypeApi;
        _userStatusApi = _clients.UserStatusApi;
        _userDataApi = _clients.UserDataApi;
        _emailApi = _clients.EmailApi;
        _passwordApi = _clients.PasswordApi;
        _photosApi = _clients.PhotosApi;
        _peopleQuotaApi = _clients.PeopleQuotaApi;
        _themeApi = _clients.ThemeApi;
        _guestsApi = _clients.GuestsApi;
        _thirdPartyAccountsApi = _clients.ThirdPartyAccountsApi;

        _groupApi = _clients.GroupApi;
        _groupSearchApi = _clients.GroupSearchApi;

        _portalUsersApi = _clients.PortalUsersApi;
        _commonSettingsApi = _clients.CommonSettingsApi;
        _settingsQuotaApi = _clients.SettingsQuotaApi;
        _webhooksApi = _clients.WebhooksApi;

        await _peopleClient.Authenticate(Owner);

        Timing.Write("setup.total", setupSw.ElapsedMilliseconds);
    }

    public ValueTask DisposeAsync()
    {
        // Each test owns its portal and clients; nothing is shared, so just dispose the clients.
        _clients.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Invites and registers a new member of the given type into the current test's portal.
    /// </summary>
    protected Task<User> InviteContact(EmployeeType employeeType, User? user = null)
    {
        return Invitations.InviteContactAsync(_profilesApi, _peopleClient, employeeType, user ?? Owner, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Creates an activated guest with a known password, so tests can also sign in as it.
    /// <c>InviteContact</c> cannot make guests — the invite endpoint refuses the type.
    /// </summary>
    protected Task<User> InviteGuest(User? user = null)
    {
        return Invitations.InviteGuestAsync(_peopleClient, user ?? Owner, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// One entry point for a theory parameterised by <see cref="EmployeeType"/>, so a test does not
    /// branch between <see cref="InviteContact"/> and <see cref="InviteGuest"/> at every call site.
    /// </summary>
    protected Task<User> InviteMember(EmployeeType employeeType, User? inviter = null)
    {
        return employeeType == EmployeeType.Guest
            ? InviteGuest(inviter)
            : InviteContact(employeeType, inviter);
    }

    protected async Task<FolderDtoInteger> CreatePublicRoom(string roomTitle)
    {
        return await CreateRoom(new CreateRoomRequestDto(roomTitle, roomType: RoomType.PublicRoom));
    }

    protected async Task<FolderDtoInteger> CreateCustomRoom(string roomTitle)
    {
        return await CreateRoom(new CreateRoomRequestDto(roomTitle, roomType: RoomType.CustomRoom));
    }

    /// <summary>
    /// The single place every room is created through, so that room creation - one of the slowest
    /// calls in the suite - is measured the same way whatever type the caller asked for.
    /// </summary>
    protected async Task<FolderDtoInteger> CreateRoom(CreateRoomRequestDto request)
    {
        var sw = Stopwatch.StartNew();
        var result = (await _roomsApi.CreateRoomAsync(request, TestContext.Current.CancellationToken)).Response;
        Timing.Write($"createRoom({request.RoomType})", sw.ElapsedMilliseconds);
        return result;
    }

    /// <summary>
    /// Shares a room with a user or a group at the given access level.
    /// </summary>
    protected async Task InviteToRoom(int roomId, Guid subjectId, FileShare access)
    {
        var request = new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = subjectId, Access = access }],
            Notify = false,
            Message = "",
            Culture = "en-US"
        };

        await _roomsApi.SetRoomSecurityAsync(roomId, request, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Disables a user, which is what the API calls "terminated". A test that then wants to observe
    /// the disabled account's own behaviour must authenticate as it BEFORE this call — signing in
    /// afterwards is refused outright, which is a different response than the one under test.
    /// </summary>
    protected async Task TerminateUser(User user)
    {
        await _peopleClient.Authenticate(Owner);

        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([user.Id]),
            TestContext.Current.CancellationToken);
    }
}
