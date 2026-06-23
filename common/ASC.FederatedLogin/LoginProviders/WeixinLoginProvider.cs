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

public class WeixinLoginProvider : BaseLoginProvider<WeixinLoginProvider>, IDummyEmailProvider
{
    public const string ProfileUrl = "https://api.weixin.qq.com/sns/userinfo";

    public override string CodeUrl => "https://open.weixin.qq.com/connect/qrconnect";
    public override string AccessTokenUrl => "https://api.weixin.qq.com/sns/oauth2/access_token";

    public override string RedirectUri => this["weixinRedirectUrl"];
    public override string ClientID => this["weixinClientId"];
    public override string ClientSecret => this["weixinClientSecret"];
    public override string Scopes => "snsapi_login";

    private readonly RequestHelper _requestHelper;

    public WeixinLoginProvider() { }
    public WeixinLoginProvider(
        WeixinOAuth20TokenHelper oAuth20TokenHelper,
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
            var token = Auth(context, out var redirect, @params, additionalStateArgs) as WeixinOAuth20Token;

            return redirect ? null : GetLoginProfile($"{token?.AccessToken}&openid={token?.UnionId}");
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
        return string.IsNullOrEmpty(accessToken)
            ? throw new Exception("Login failed")
            : RequestProfile(accessToken);
    }

    public override LoginProfile GetLoginProfile(OAuth20Token token)
    {
        return token is WeixinOAuth20Token weixinOAuth20Token
            ? GetLoginProfile($"{weixinOAuth20Token.AccessToken}&openid={weixinOAuth20Token.UnionId}")
            : GetLoginProfile($"{token?.AccessToken}");
    }

    public string GenerateEmail(LoginProfile loginProfile)
    {
        var domain = CoreBaseSettings.Basedomain;
        var currentTenant = TenantManager.GetCurrentTenant(false);
        if (currentTenant != null)
        {
            domain = currentTenant.GetTenantDomain(CoreSettings);
        }
        return $"{loginProfile.Id}@{domain}";
    }

    private LoginProfile RequestProfile(string accessToken)
    {
        var openidProfile = _requestHelper.PerformRequest($"{ProfileUrl}?access_token={accessToken}");
        var loginProfile = ProfileFromWeixin(openidProfile);
        return loginProfile;
    }

    internal LoginProfile ProfileFromWeixin(string openidProfile)
    {
        var jProfile = JObject.Parse(openidProfile)
                       ?? throw new Exception("Failed to correctly process the response");

        if (jProfile.Value<int>("errcode") != 0)
        {
            throw new Exception($"Failed to parse profile: {jProfile.Value<int>("errcode")} - {jProfile.Value<int>("errmsg")}");
        }

        // No names, no email
        var profile = new LoginProfile
        {
            DisplayName = jProfile.Value<string>("nickname"),
            Id = jProfile.Value<string>("openid"),
            Avatar = jProfile.Value<string>("headimgurl"),
            Gender = jProfile.Value<int>("sex") == 1 ? "male" : "female",
            Provider = ProviderConstants.Weixin
        };

        return string.IsNullOrWhiteSpace(profile.Id)
            ? throw new Exception($"Failed to parse profile: no id found")
            : profile;
    }
}