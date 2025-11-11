// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode


namespace ASC.FederatedLogin.LoginProviders;

public class WeixinLoginProvider : BaseLoginProvider<WeixinLoginProvider>
{
    public const string ProfileUrl = "https://api.weixin.qq.com/sns/userinfo";

    public override string CodeUrl { get { return "https://open.weixin.qq.com/connect/qrconnect"; } }
    public override string AccessTokenUrl { get { return "https://api.weixin.qq.com/sns/oauth2/access_token"; } }

    public override string RedirectUri { get { return this["weixinRedirectUrl"]; } }
    public override string ClientID { get { return this["weixinClientId"]; } }
    public override string ClientSecret { get { return this["weixinClientSecret"]; } }
    public override string Scopes { get { return "snsapi_login"; } }

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
        string name, int order, Dictionary<string, string> props, Dictionary<string, string> additional = null)
        : base(oAuth20TokenHelper, tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, props, additional)
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