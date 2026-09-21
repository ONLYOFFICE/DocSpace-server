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

namespace ASC.Web.Api.Tests.Tests;

public class BaseTest(
    AspireAppFixture fixture
) : IAsyncLifetime
{
    private PortalClients _clients = null!;

    // The portal and its owner created for this test. Both live on the per-portal client bundle,
    // so the owner Id is always the one belonging to this test's own portal — never shared.
    protected User Owner => _clients.Owner;

    protected HttpClient _peopleClient = null!;
    protected HttpClient _webApiClient = null!;

    protected RawApiClient _webApi = null!;

    protected ProfilesApi _profilesApi = null!;
    protected ApiKeysApi _apiKeysApi = null!;
    protected MigrationApi _migrationApi = null!;

    protected HttpClient _identityClient = null!;
    protected ClientManagementApi _clientManagementApi = null!;
    protected ClientQueryingApi _clientQueryingApi = null!;
    protected ScopeManagementApi _scopeManagementApi = null!;

    protected AuthenticationApi _authenticationApi = null!;
    protected CapabilitiesApi _capabilitiesApi = null!;
    protected AccessToDevToolsApi _accessToDevToolsApi = null!;
    protected AuthorizationApi _settingsAuthorizationApi = null!;
    protected BannersVisibilityApi _bannersVisibilityApi = null!;
    protected CommonSettingsApi _commonSettingsApi = null!;
    protected CookiesApi _cookiesApi = null!;
    protected DocsCloudApi _docsCloudApi = null!;
    protected GreetingSettingsApi _greetingSettingsApi = null!;
    protected IPRestrictionsApi _ipRestrictionsApi = null!;
    protected LoginSettingsApi _loginSettingsApi = null!;
    protected MessagesApi _messagesApi = null!;
    protected NotificationsApi _notificationsApi = null!;
    protected OwnerApi _ownerApi = null!;
    protected SecurityApi _securityApi = null!;
    protected SettingsQuotaApi _settingsQuotaApi = null!;
    protected TFASettingsApi _tfaSettingsApi = null!;
    protected WebhooksApi _webhooksApi = null!;
    protected WebpluginsApi _webpluginsApi = null!;
    protected PaymentApi _paymentApi = null!;
    protected ActiveConnectionsApi _activeConnectionsApi = null!;
    protected AuditTrailDataApi _auditTrailDataApi = null!;
    protected CSPApi _cspApi = null!;
    protected FirebaseApi _firebaseApi = null!;
    protected LoginHistoryApi _loginHistoryApi = null!;
    protected OAuth2Api _oauth2Api = null!;
    protected SecurityAccessToDevToolsApi _securityAccessToDevToolsApi = null!;
    protected SecurityBannersVisibilityApi _securityBannersVisibilityApi = null!;
    protected SMTPSettingsApi _smtpSettingsApi = null!;
    protected PortalGuestsApi _portalGuestsApi = null!;
    protected PortalQuotaApi _portalQuotaApi = null!;
    protected PortalSettingsApi _portalSettingsApi = null!;
    protected PortalUsersApi _portalUsersApi = null!;

    public async ValueTask InitializeAsync()
    {
        var setupSw = Stopwatch.StartNew();

        // Register a brand-new portal for this test and bind a fresh set of clients to it.
        _clients = await fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        _peopleClient = _clients.PeopleHttpClient;
        _webApiClient = _clients.WebApiHttpClient;
        _webApi = _clients.WebApi;

        _profilesApi = _clients.ProfilesApi;
        _apiKeysApi = _clients.ApiKeysApi;
        _migrationApi = _clients.MigrationApi;

        _identityClient = _clients.IdentityHttpClient;
        _clientManagementApi = _clients.ClientManagementApi;
        _clientQueryingApi = _clients.ClientQueryingApi;
        _scopeManagementApi = _clients.ScopeManagementApi;

        _authenticationApi = _clients.AuthenticationApi;
        _capabilitiesApi = _clients.CapabilitiesApi;
        _accessToDevToolsApi = _clients.AccessToDevToolsApi;
        _settingsAuthorizationApi = _clients.SettingsAuthorizationApi;
        _bannersVisibilityApi = _clients.BannersVisibilityApi;
        _commonSettingsApi = _clients.CommonSettingsApi;
        _cookiesApi = _clients.CookiesApi;
        _docsCloudApi = _clients.DocsCloudApi;
        _greetingSettingsApi = _clients.GreetingSettingsApi;
        _ipRestrictionsApi = _clients.IpRestrictionsApi;
        _loginSettingsApi = _clients.LoginSettingsApi;
        _messagesApi = _clients.MessagesApi;
        _notificationsApi = _clients.NotificationsApi;
        _ownerApi = _clients.OwnerApi;
        _securityApi = _clients.SecurityApi;
        _settingsQuotaApi = _clients.SettingsQuotaApi;
        _tfaSettingsApi = _clients.TfaSettingsApi;
        _webhooksApi = _clients.WebhooksApi;
        _webpluginsApi = _clients.WebpluginsApi;
        _paymentApi = _clients.PaymentApi;
        _activeConnectionsApi = _clients.ActiveConnectionsApi;
        _auditTrailDataApi = _clients.AuditTrailDataApi;
        _cspApi = _clients.CspApi;
        _firebaseApi = _clients.FirebaseApi;
        _loginHistoryApi = _clients.LoginHistoryApi;
        _oauth2Api = _clients.OAuth2Api;
        _securityAccessToDevToolsApi = _clients.SecurityAccessToDevToolsApi;
        _securityBannersVisibilityApi = _clients.SecurityBannersVisibilityApi;
        _smtpSettingsApi = _clients.SmtpSettingsApi;
        _portalGuestsApi = _clients.PortalGuestsApi;
        _portalQuotaApi = _clients.PortalQuotaApi;
        _portalSettingsApi = _clients.PortalSettingsApi;
        _portalUsersApi = _clients.PortalUsersApi;

        await _webApiClient.Authenticate(Owner);

        Timing.Write("setup.total", setupSw.ElapsedMilliseconds);
    }

    public ValueTask DisposeAsync()
    {
        // Each test owns its portal and clients; nothing is shared, so just dispose the clients.
        _clients.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Invites a member of any type, routing guests through <see cref="InviteGuest"/> — the single
    /// dispatcher a role-parameterised theory goes through.
    /// </summary>
    protected async Task<User> InviteMember(EmployeeType employeeType, User? user = null)
    {
        return employeeType == EmployeeType.Guest
            ? await InviteGuest(user)
            : await InviteContact(employeeType, user);
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

    /// <summary>
    /// Creates an activated guest with a known password through <c>POST /api/2.0/people/active</c> —
    /// guests cannot be created with <see cref="InviteContact"/>.
    /// </summary>
    protected async Task<User> InviteGuest(User? user = null)
    {
        user ??= Owner;
        await _peopleClient.Authenticate(user);

        var fakeGuest = Initializer.FakerMember.Generate();

        var payload = JsonSerializer.Serialize(new
        {
            firstName = fakeGuest.FirstName,
            lastName = fakeGuest.LastName,
            email = fakeGuest.Email,
            password = fakeGuest.Password,
            type = nameof(EmployeeType.Guest),
            cultureName = "en-US",
            spam = false
        });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var guestSw = Stopwatch.StartNew();
        using var response = await _peopleClient.PostAsync("api/2.0/people/active", content, TestContext.Current.CancellationToken);
        Timing.Write($"invite.guest({user.Email})", guestSw.ElapsedMilliseconds);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Unable to create a guest ({(int)response.StatusCode}): {body}");
        }

        using var json = JsonDocument.Parse(body);
        var guestId = json.RootElement.GetProperty("response").GetProperty("id").GetGuid();

        return new User(fakeGuest.Email, fakeGuest.Password) { Id = guestId };
    }
}
