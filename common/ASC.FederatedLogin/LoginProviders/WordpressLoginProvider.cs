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
public class WordpressLoginProvider : BaseLoginProvider<WordpressLoginProvider>
{
    public const string WordpressMeInfoUrl = "https://public-api.wordpress.com/rest/v1/me";
    public const string WordpressSites = "https://public-api.wordpress.com/rest/v1.2/sites/";

    public override string CodeUrl => "https://public-api.wordpress.com/oauth2/authorize";
    public override string AccessTokenUrl => "https://public-api.wordpress.com/oauth2/token";
    public override string RedirectUri => this["wpRedirectUrl"];
    public override string ClientID => this["wpClientId"];
    public override string ClientSecret => this["wpClientSecret"];

    public WordpressLoginProvider() { }

    public WordpressLoginProvider(
        OAuth20TokenHelper oAuth20TokenHelper,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        string name, int order, bool paid, Dictionary<string, string> props, Dictionary<string, string> additional = null)
        : base(oAuth20TokenHelper, tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, paid, props, additional)
    {
    }

    public static string GetWordpressMeInfo(RequestHelper requestHelper, string token)
    {
        var headers = new Dictionary<string, string>
                {
                    { "Authorization", "bearer " + token }
                };
        return requestHelper.PerformRequest(WordpressMeInfoUrl, "", "GET", "", headers);
    }

    public static bool CreateWordpressPost(RequestHelper requestHelper, string title, string content, string status, string blogId, OAuth20Token token)
    {
        try
        {
            var uri = WordpressSites + blogId + "/posts/new";
            const string contentType = "application/x-www-form-urlencoded";
            const string method = "POST";
            var body = "title=" + HttpUtility.UrlEncode(title) + "&content=" + HttpUtility.UrlEncode(content) + "&status=" + status + "&format=standard";
            var headers = new Dictionary<string, string>
                    {
                        { "Authorization", "bearer " + token.AccessToken }
                    };

            requestHelper.PerformRequest(uri, contentType, method, body, headers);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public override LoginProfile GetLoginProfile(string accessToken)
    {
        throw new NotImplementedException();
    }
}