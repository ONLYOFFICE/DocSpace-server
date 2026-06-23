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

namespace ASC.FederatedLogin.LoginProviders;

public class MicrosoftLoginProvider : BaseLoginProvider<MicrosoftLoginProvider>
{
    private const string MicrosoftProfileUrl = "https://graph.microsoft.com/oidc/userinfo";

    public override string AccessTokenUrl => "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    public override string RedirectUri => this["microsoftRedirectUrl"];
    public override string ClientID => this["microsoftClientId"];
    public override string ClientSecret => this["microsoftClientSecret"];
    public override string CodeUrl => "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
    public override string Scopes => "openid,email,profile";

    private readonly RequestHelper _requestHelper;

    public MicrosoftLoginProvider() { }
    public MicrosoftLoginProvider(
        OAuth20TokenHelper oAuth20TokenHelper,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        RequestHelper requestHelper,
        string name, int order, bool paid, Dictionary<string, string> props, Dictionary<string, string> additional = null)
            : base(oAuth20TokenHelper, tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, paid, props, additional)
    {
        _requestHelper = requestHelper;
    }

    public override LoginProfile GetLoginProfile(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new Exception("Login failed");
        }

        return RequestProfile(accessToken);
    }

    private LoginProfile RequestProfile(string accessToken)
    {
        var openidProfile = _requestHelper.PerformRequest(MicrosoftProfileUrl, headers: new Dictionary<string, string> { { "Authorization", "Bearer " + accessToken } });
        var loginProfile = ProfileFromMicrosoft(openidProfile);
        return loginProfile;
    }

    internal LoginProfile ProfileFromMicrosoft(string openidProfile)
    {
        var jProfile = JObject.Parse(openidProfile);
        if (jProfile == null)
        {
            throw new Exception("Failed to correctly process the response");
        }

        var profile = new LoginProfile
        {
            FirstName = jProfile.Value<string>("given_name"),
            LastName = jProfile.Value<string>("family_name"),
            EMail = jProfile.Value<string>("email"),
            Id = jProfile.Value<string>("sub"),
            Provider = ProviderConstants.Microsoft
        };

        return profile;
    }
}