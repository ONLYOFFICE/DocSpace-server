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

[Scope]
public class LinkedInLoginProvider : BaseLoginProvider<LinkedInLoginProvider>
{
    public override string AccessTokenUrl => "https://www.linkedin.com/oauth/v2/accessToken";
    public override string RedirectUri => this["linkedInRedirectUrl"];
    public override string ClientID => this["linkedInKey"];
    public override string ClientSecret => this["linkedInSecret"];
    public override string CodeUrl => "https://www.linkedin.com/oauth/v2/authorization";
    public override string Scopes => "r_liteprofile r_emailaddress";

    private const string LinkedInProfileUrl = "https://api.linkedin.com/v2/me";
    private const string LinkedInEmailUrl = "https://api.linkedin.com/v2/emailAddress?q=members&projection=(elements*(handle~))";
    private readonly RequestHelper _requestHelper;

    public LinkedInLoginProvider() { }

    public LinkedInLoginProvider(
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

    internal LoginProfile ProfileFromLinkedIn(string linkedInProfile)
    {
        var jProfile = JObject.Parse(linkedInProfile);
        if (jProfile == null)
        {
            throw new Exception("Failed to correctly process the response");
        }

        var profile = new LoginProfile
        {
            Id = jProfile.Value<string>("id"),
            FirstName = jProfile.Value<string>("localizedFirstName"),
            LastName = jProfile.Value<string>("localizedLastName"),
            EMail = jProfile.Value<string>("emailAddress"),
            Provider = ProviderConstants.LinkedIn
        };

        return profile;
    }

    internal static string EmailFromLinkedIn(string linkedInEmail)
    {
        var jEmail = JObject.Parse(linkedInEmail);
        if (jEmail == null)
        {
            throw new Exception("Failed to correctly process the response");
        }

        return jEmail.SelectToken("elements[0].handle~.emailAddress").ToString();
    }

    private LoginProfile RequestProfile(string accessToken)
    {
        var linkedInProfile = _requestHelper.PerformRequest(LinkedInProfileUrl,
            headers: new Dictionary<string, string> { { "Authorization", "Bearer " + accessToken } });
        var loginProfile = ProfileFromLinkedIn(linkedInProfile);

        var linkedInEmail = _requestHelper.PerformRequest(LinkedInEmailUrl, headers: new Dictionary<string, string> { { "Authorization", "Bearer " + accessToken } });
        loginProfile.EMail = EmailFromLinkedIn(linkedInEmail);

        return loginProfile;
    }
}