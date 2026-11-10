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


namespace ASC.Data.Backup.Tests.Tests;

public class BaseTest(
    AspireAppFixture fixture
) : IAsyncLifetime
{
    private PortalClients _clients = null!;

    // The portal and its owner created for this test, never shared with another one.
    protected User Owner => _clients.Owner;

    protected HttpClient _backupClient = null!;
    protected HttpClient _peopleClient = null!;
    protected HttpClient _filesClient = null!;
    protected HttpClient _webApiClient = null!;

    protected RawApiClient _backup = null!;
    protected BackupApi _backupApi = null!;

    protected ProfilesApi _profilesApi = null!;

    protected RoomsApi _roomsApi = null!;
    protected FoldersApi _foldersApi = null!;
    protected ThirdPartyIntegrationApi _thirdPartyIntegrationApi = null!;

    protected PaymentApi _paymentApi = null!;
    protected SettingsAuthorizationApi _settingsAuthorizationApi = null!;

    public async ValueTask InitializeAsync()
    {
        // Register a brand-new portal for this test and bind a fresh set of clients to it.
        _clients = await fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        _backupClient = _clients.BackupHttpClient;
        _peopleClient = _clients.PeopleHttpClient;
        _filesClient = _clients.FilesHttpClient;
        _webApiClient = _clients.WebApiHttpClient;

        _backup = _clients.Backup;
        _backupApi = _clients.BackupApi;
        _profilesApi = _clients.ProfilesApi;
        _roomsApi = _clients.RoomsApi;
        _foldersApi = _clients.FoldersApi;
        _thirdPartyIntegrationApi = _clients.ThirdPartyIntegrationApi;
        _paymentApi = _clients.PaymentApi;
        _settingsAuthorizationApi = _clients.SettingsAuthorizationApi;

        await _backupClient.Authenticate(Owner);
    }

    public ValueTask DisposeAsync()
    {
        _clients.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Invites a member of any type, routing guests through <see cref="InviteGuest"/> - the single
    /// dispatcher a role-parameterised theory goes through.
    /// </summary>
    protected async Task<User> InviteMember(EmployeeType employeeType, User? user = null)
    {
        return employeeType == EmployeeType.Guest
            ? await InviteGuest(user)
            : await InviteContact(employeeType, user);
    }

    /// <summary>Invites and registers a new member of the given type into this test's portal.</summary>
    protected Task<User> InviteContact(EmployeeType employeeType, User? user = null)
    {
        return Invitations.InviteContactAsync(_profilesApi, _peopleClient, employeeType, user ?? Owner, TestContext.Current.CancellationToken);
    }

    /// <summary>Creates an activated guest with a known password in this test's portal.</summary>
    protected Task<User> InviteGuest(User? user = null)
    {
        return Invitations.InviteGuestAsync(_peopleClient, user ?? Owner, TestContext.Current.CancellationToken);
    }
}
