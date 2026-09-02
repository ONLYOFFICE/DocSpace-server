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

namespace ASC.Web.Api.Tests.ApiFactories;

/// <summary>
/// The Web.Api suite's per-portal API clients.
/// </summary>
public sealed class PortalClients : PortalClientsBase
{
    public HttpClient PeopleHttpClient { get; }
    public HttpClient IdentityHttpClient { get; }

    // Identity (OAuth2) service — the registration container serves /api/2.0/clients and /api/2.0/scopes
    public ClientManagementApi ClientManagementApi { get; }
    public ClientQueryingApi ClientQueryingApi { get; }
    public ScopeManagementApi ScopeManagementApi { get; }

    // People service — member invitation for the role helpers, plus the endpoints the SDK files
    // under other tags but People actually serves (guests share link, API keys)
    public ProfilesApi ProfilesApi { get; }
    public ApiKeysApi ApiKeysApi { get; }

    // WebApi service
    public AuthenticationApi AuthenticationApi { get; }
    public CapabilitiesApi CapabilitiesApi { get; }
    public AccessToDevToolsApi AccessToDevToolsApi { get; }
    public AuthorizationApi SettingsAuthorizationApi { get; }
    public BannersVisibilityApi BannersVisibilityApi { get; }
    public CommonSettingsApi CommonSettingsApi { get; }
    public CookiesApi CookiesApi { get; }
    public DocsCloudApi DocsCloudApi { get; }
    public GreetingSettingsApi GreetingSettingsApi { get; }
    public IPRestrictionsApi IpRestrictionsApi { get; }
    public LoginSettingsApi LoginSettingsApi { get; }
    public MessagesApi MessagesApi { get; }
    public NotificationsApi NotificationsApi { get; }
    public OwnerApi OwnerApi { get; }
    public SecurityApi SecurityApi { get; }
    public SettingsQuotaApi SettingsQuotaApi { get; }
    public TFASettingsApi TfaSettingsApi { get; }
    public WebhooksApi WebhooksApi { get; }
    public WebpluginsApi WebpluginsApi { get; }
    public PaymentApi PaymentApi { get; }
    public ActiveConnectionsApi ActiveConnectionsApi { get; }
    public AuditTrailDataApi AuditTrailDataApi { get; }
    public CSPApi CspApi { get; }
    public FirebaseApi FirebaseApi { get; }
    public LoginHistoryApi LoginHistoryApi { get; }
    public OAuth2Api OAuth2Api { get; }
    public SecurityAccessToDevToolsApi SecurityAccessToDevToolsApi { get; }
    public SecurityBannersVisibilityApi SecurityBannersVisibilityApi { get; }
    public SMTPSettingsApi SmtpSettingsApi { get; }
    public MigrationApi MigrationApi { get; }
    public PortalGuestsApi PortalGuestsApi { get; }
    public PortalQuotaApi PortalQuotaApi { get; }
    public PortalSettingsApi PortalSettingsApi { get; }
    public PortalUsersApi PortalUsersApi { get; }

    public PortalClients(PortalContext context) : base(context)
    {
        PeopleHttpClient = CreateClient(ResourceNames.People);

        var peopleConfig = new Configuration { BasePath = BasePathOf(ResourceNames.People) };
        ProfilesApi = new ProfilesApi(PeopleHttpClient, peopleConfig);

        // The SDK files this under "Portal / Guests", but the route (/api/2.0/people/guests/...)
        // is served by the People service — so it rides the People client, not WebApi.
        PortalGuestsApi = new PortalGuestsApi(PeopleHttpClient, peopleConfig);
        ApiKeysApi = new ApiKeysApi(PeopleHttpClient, peopleConfig);

        IdentityHttpClient = CreateClient(ResourceNames.IdentityRegistration);

        // The identity (Spring) side closes idle keep-alive connections aggressively; reusing them
        // from the shared pool races with that and dies with "response ended prematurely" under
        // parallel load. One connection per request costs little here and removes the race.
        IdentityHttpClient.DefaultRequestHeaders.ConnectionClose = true;

        // Identity's audit path (HttpUtils.getClientBrowser) NPEs into a 500 on a request without
        // a User-Agent — every real client sends one, so these tests do too.
        IdentityHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ASC.Web.Api.Tests/1.0");

        var identityConfig = new Configuration { BasePath = BasePathOf(ResourceNames.IdentityRegistration) };
        ClientManagementApi = new ClientManagementApi(IdentityHttpClient, identityConfig);
        ClientQueryingApi = new ClientQueryingApi(IdentityHttpClient, identityConfig);
        ScopeManagementApi = new ScopeManagementApi(IdentityHttpClient, identityConfig);

        var webApiConfig = new Configuration { BasePath = BasePathOf(ResourceNames.WebApi) };
        AuthenticationApi = new AuthenticationApi(WebApiHttpClient, webApiConfig);
        CapabilitiesApi = new CapabilitiesApi(WebApiHttpClient, webApiConfig);
        AccessToDevToolsApi = new AccessToDevToolsApi(WebApiHttpClient, webApiConfig);
        SettingsAuthorizationApi = new AuthorizationApi(WebApiHttpClient, webApiConfig);
        BannersVisibilityApi = new BannersVisibilityApi(WebApiHttpClient, webApiConfig);
        CommonSettingsApi = new CommonSettingsApi(WebApiHttpClient, webApiConfig);
        CookiesApi = new CookiesApi(WebApiHttpClient, webApiConfig);
        DocsCloudApi = new DocsCloudApi(WebApiHttpClient, webApiConfig);
        GreetingSettingsApi = new GreetingSettingsApi(WebApiHttpClient, webApiConfig);
        IpRestrictionsApi = new IPRestrictionsApi(WebApiHttpClient, webApiConfig);
        LoginSettingsApi = new LoginSettingsApi(WebApiHttpClient, webApiConfig);
        MessagesApi = new MessagesApi(WebApiHttpClient, webApiConfig);
        NotificationsApi = new NotificationsApi(WebApiHttpClient, webApiConfig);
        OwnerApi = new OwnerApi(WebApiHttpClient, webApiConfig);
        SecurityApi = new SecurityApi(WebApiHttpClient, webApiConfig);
        SettingsQuotaApi = new SettingsQuotaApi(WebApiHttpClient, webApiConfig);
        TfaSettingsApi = new TFASettingsApi(WebApiHttpClient, webApiConfig);
        WebhooksApi = new WebhooksApi(WebApiHttpClient, webApiConfig);
        WebpluginsApi = new WebpluginsApi(WebApiHttpClient, webApiConfig);
        PaymentApi = new PaymentApi(WebApiHttpClient, webApiConfig);
        ActiveConnectionsApi = new ActiveConnectionsApi(WebApiHttpClient, webApiConfig);
        AuditTrailDataApi = new AuditTrailDataApi(WebApiHttpClient, webApiConfig);
        CspApi = new CSPApi(WebApiHttpClient, webApiConfig);
        FirebaseApi = new FirebaseApi(WebApiHttpClient, webApiConfig);
        LoginHistoryApi = new LoginHistoryApi(WebApiHttpClient, webApiConfig);
        OAuth2Api = new OAuth2Api(WebApiHttpClient, webApiConfig);
        SecurityAccessToDevToolsApi = new SecurityAccessToDevToolsApi(WebApiHttpClient, webApiConfig);
        SecurityBannersVisibilityApi = new SecurityBannersVisibilityApi(WebApiHttpClient, webApiConfig);
        SmtpSettingsApi = new SMTPSettingsApi(WebApiHttpClient, webApiConfig);
        MigrationApi = new MigrationApi(WebApiHttpClient, webApiConfig);
        PortalQuotaApi = new PortalQuotaApi(WebApiHttpClient, webApiConfig);
        PortalSettingsApi = new PortalSettingsApi(WebApiHttpClient, webApiConfig);
        PortalUsersApi = new PortalUsersApi(WebApiHttpClient, webApiConfig);
    }
}
