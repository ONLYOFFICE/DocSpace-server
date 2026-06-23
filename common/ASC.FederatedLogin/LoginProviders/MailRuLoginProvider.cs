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
public class MailRuLoginProvider : BaseLoginProvider<MailRuLoginProvider>
{
    public override string CodeUrl => "https://connect.mail.ru/oauth/authorize";
    public override string AccessTokenUrl => "https://connect.mail.ru/oauth/token";
    public override string ClientID => this["mailRuClientId"];
    public override string ClientSecret => this["mailRuClientSecret"];
    public override string RedirectUri => this["mailRuRedirectUrl"];

    private readonly RequestHelper _requestHelper;
    private const string MailRuApiUrl = "http://www.appsmail.ru/platform/api";

    public MailRuLoginProvider() { }

    public MailRuLoginProvider(
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

    public override LoginProfile ProcessAuthorization(HttpContext context, IDictionary<string, string> @params, IDictionary<string, string> additionalStateArgs)
    {
        try
        {
            var token = Auth(context, out var redirect);

            if (redirect)
            {
                return null;
            }

            if (token == null)
            {
                throw new Exception("Login failed");
            }

            var uid = GetUid(token);

            return RequestProfile(token.AccessToken, uid);
        }
        catch (ThreadAbortException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new LoginProfile(ex);
        }
    }

    public override LoginProfile GetLoginProfile(string accessToken)
    {
        throw new NotImplementedException();
    }

    private LoginProfile RequestProfile(string accessToken, string uid)
    {
        var queryDictionary = new Dictionary<string, string>
                {
                    { "app_id", ClientID },
                    { "method", "users.getInfo" },
                    { "secure", "1" },
                    { "session_key", accessToken },
                    { "uids", uid }
                };

        var sortedKeys = queryDictionary.Keys.ToList();
        sortedKeys.Sort();
        var mailruParams = string.Join("", sortedKeys.Select(key => key + "=" + queryDictionary[key]).ToList());
        var sig = string.Join("", MD5.HashData(Encoding.ASCII.GetBytes(mailruParams + ClientSecret)).Select(b => b.ToString("x2")));

        var mailRuProfile = _requestHelper.PerformRequest(
        MailRuApiUrl
        + "?" + string.Join("&", queryDictionary.Select(pair => pair.Key + "=" + HttpUtility.UrlEncode(pair.Value)))
        + "&sig=" + HttpUtility.UrlEncode(sig));
        var loginProfile = ProfileFromMailRu(mailRuProfile);

        return loginProfile;
    }

    private LoginProfile ProfileFromMailRu(string strProfile)
    {
        var jProfile = JArray.Parse(strProfile);
        if (jProfile == null)
        {
            throw new Exception("Failed to correctly process the response");
        }

        var mailRuProfiles = jProfile.ToObject<List<MailRuProfile>>();
        if (mailRuProfiles.Count == 0)
        {
            throw new Exception("Failed to correctly process the response");
        }

        var profile = new LoginProfile
        {
            EMail = mailRuProfiles[0].Email,
            Id = mailRuProfiles[0].Uid,
            FirstName = mailRuProfiles[0].FirstName,
            LastName = mailRuProfiles[0].LastName,
            Provider = ProviderConstants.MailRu
        };

        return profile;
    }

    private class MailRuProfile
    {
        public string Uid { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    private static string GetUid(OAuth20Token token)
    {
        if (string.IsNullOrEmpty(token.OriginJson))
        {
            return null;
        }

        var parser = JObject.Parse(token.OriginJson);

        return parser.Value<string>("x_mailru_vid");
    }
}