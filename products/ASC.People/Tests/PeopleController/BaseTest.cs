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

namespace ASC.People.Tests.PeopleController;

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
    protected HttpClient _apiClient = null!;

    protected RoomsApi _roomsApi = null!;

    protected ProfilesApi _profilesApi = null!;
    protected GroupApi _groupApi = null!;
    protected UserTypeApi _userTypeApi = null!;

    protected UsersApi _portalUsersApi = null!;
    protected CommonSettingsApi _commonSettingsApi = null!;
    protected WebhooksApi _webhooksApi = null!;

    public async ValueTask InitializeAsync()
    {
        var setupSw = Stopwatch.StartNew();

        // Register a brand-new portal for this test and bind a fresh set of clients to it.
        _clients = await fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        _filesClient = _clients.FilesHttpClient;
        _peopleClient = _clients.PeopleHttpClient;
        _apiClient = _clients.WebApiHttpClient;

        _roomsApi = _clients.RoomsApi;

        _profilesApi = _clients.ProfilesApi;
        _groupApi = _clients.GroupApi;
        _userTypeApi = _clients.UserTypeApi;

        _portalUsersApi = _clients.PortalUsersApi;
        _commonSettingsApi = _clients.CommonSettingsApi;
        _webhooksApi = _clients.WebhooksApi;

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
    protected async Task<User> InviteContact(EmployeeType employeeType, User? user = null)
    {
        user ??= Owner;
        await _peopleClient.Authenticate(user);

        var fakeMember = Initializer.FakerMember.Generate();

        var memberSw = Stopwatch.StartNew();
        var createMemberResponse = await _profilesApi.AddMemberWithHttpInfoAsync(new MemberRequestDto
        {
            CultureName = "en-US",
            Spam = false,
            Email = fakeMember.Email,
            Password = fakeMember.Password,
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName,
            Type = employeeType,
        }, TestContext.Current.CancellationToken);
        Timing.Write($"invite.addMember({employeeType})", memberSw.ElapsedMilliseconds);

        if (createMemberResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException($"Unable to invite user {employeeType}");
        }

        return new User(fakeMember.Email, fakeMember.Password) { Id = createMemberResponse.Data.Response.Id };
    }

    protected async Task<User> InviteGuest(User? user = null)
    {
        user ??= Owner;
        await _filesClient.Authenticate(user);
        await _peopleClient.Authenticate(user);

        // Create a public room
        var guestEmail = Initializer.FakerMember.Generate().Email;
        var room = await CreatePublicRoom("Test Room For Existing Guest");

        // Act - Add existing guest to the room
        var invitation = new RoomInvitation
        {
            Access = FileShare.ContentCreator,
            Email = guestEmail,
        };

        var roomInvitation = new RoomInvitationRequest
        {
            Invitations = [invitation],
            Notify = false,
            Message = "",
            Culture = "en-US"
        };

        await _roomsApi.SetRoomSecurityAsync(room.Id, roomInvitation, cancellationToken: TestContext.Current.CancellationToken);
        var result = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var guestId = result.First(r => r.SharedToUser.Email == guestEmail).SharedToUser.Id;

        return new User(guestEmail, "")
        {
            Id = guestId
        };
    }

    protected async Task<FolderDtoInteger> CreatePublicRoom(string roomTitle)
    {
        return (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(roomTitle, roomType: RoomType.PublicRoom), TestContext.Current.CancellationToken)).Response;
    }
}
