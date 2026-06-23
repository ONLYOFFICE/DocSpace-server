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

namespace ASC.FederatedLogin.Helpers;

[Scope]
public class OAuth20TokenHelper(
    IHttpContextAccessor httpContextAccessor,
    ConsumerFactory consumerFactory,
    RequestHelper requestHelper) : IOAuth20TokenHelper
{
    public string RequestCode<T>(string scope = null, IDictionary<string, string> additionalArgs = null, IDictionary<string, string> additionalStateArgs = null)
        where T : Consumer, IOAuthProvider, new()
    {
        var loginProvider = consumerFactory.Get<T>();
        var requestUrl = loginProvider.CodeUrl;
        var clientID = loginProvider.ClientID;
        var redirectUri = loginProvider.RedirectUri;

        var uriBuilder = new UriBuilder(requestUrl);

        var query = uriBuilder.Query;
        if (!string.IsNullOrEmpty(query))
        {
            query += "&";
        }

        query += "response_type=code";

        if (!string.IsNullOrEmpty(clientID))
        {
            query += $"&client_id={HttpUtility.UrlEncode(clientID)}";
        }

        if (!string.IsNullOrEmpty(redirectUri))
        {
            query += $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}";
        }

        if (!string.IsNullOrEmpty(scope))
        {
            query += $"&scope={HttpUtility.UrlEncode(scope)}";
        }

        var u = httpContextAccessor.HttpContext.Request.Url();

        var stateUriBuilder = new UriBuilder(u.Scheme, u.Host, u.Port, $"thirdparty/{loginProvider.Name.ToLower()}/code");

        if (additionalStateArgs is { Count: > 0 })
        {
            var stateQuery = "";
            stateQuery = additionalStateArgs.Keys
                .Where(a => a != null)
                .Aggregate(stateQuery, (current, a) => $"{current}&{a.Trim()}={additionalStateArgs[a] ?? "".Trim()}");

            stateUriBuilder.Query = stateQuery[1..];
        }

        var state = HttpUtility.UrlEncode(stateUriBuilder.Uri.AbsoluteUri);
        query += $"&state={state}";

        if (additionalArgs != null)
        {
            query = additionalArgs.Keys.Where(additionalArg => additionalArg != null)
                                  .Aggregate(query, (current, additionalArg) =>
                                                    current
                                                    + "&" + HttpUtility.UrlEncode(additionalArg.Trim())
                                                    + "=" + HttpUtility.UrlEncode((additionalArgs[additionalArg] ?? "").Trim()));
        }

        return uriBuilder.Uri + "?" + query;
    }

    public OAuth20Token GetAccessToken<T>(string authCode) where T : Consumer, IOAuthProvider, new()
    {
        var loginProvider = consumerFactory.Get<T>();
        
        return GetAccessToken(loginProvider, authCode);
    }

    public OAuth20Token GetAccessToken(IOAuthProvider loginProvider, string authCode)
    {
        var requestUrl = loginProvider.AccessTokenUrl;
        var clientId = loginProvider.ClientID;
        var clientSecret = loginProvider.ClientSecret;
        var redirectUri = loginProvider.RedirectUri;

        ArgumentException.ThrowIfNullOrEmpty(authCode);
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        ArgumentException.ThrowIfNullOrEmpty(clientSecret);

        var data = $"code={HttpUtility.UrlEncode(authCode)}&client_id={HttpUtility.UrlEncode(clientId)}&client_secret={HttpUtility.UrlEncode(clientSecret)}";

        if (!string.IsNullOrEmpty(redirectUri))
        {
            data += "&redirect_uri=" + HttpUtility.UrlEncode(redirectUri);
        }

        data += "&grant_type=authorization_code";

        var json = requestHelper.PerformRequest(requestUrl, "application/x-www-form-urlencoded", "POST", data);
        if (json != null)
        {
            if (!json.StartsWith('{'))
            {
                json = "{\"" + json.Replace("=", "\":\"").Replace("&", "\",\"") + "\"}";
            }

            var token = OAuth20Token.FromJson(json);
            if (token == null)
            {
                return null;
            }

            token.ClientID = clientId;
            token.ClientSecret = clientSecret;
            token.RedirectUri = redirectUri;
            token.OriginJson = json;

            return token;
        }

        return null;
    }

    public OAuth20Token RefreshToken<T>(OAuth20Token token) where T : Consumer, IOAuthProvider, new()
    {
        var loginProvider = consumerFactory.Get<T>();

        return RefreshToken(loginProvider.AccessTokenUrl, token);
    }

    public OAuth20Token RefreshToken(string requestUrl, OAuth20Token token)
    {
        if (token == null || !CanRefresh(token))
        {
            throw new ArgumentException("Can not refresh given token", nameof(token));
        }

        var data = $"client_id={HttpUtility.UrlEncode(token.ClientID)}&client_secret={HttpUtility.UrlEncode(token.ClientSecret)}&refresh_token={HttpUtility.UrlEncode(token.RefreshToken)}&grant_type=refresh_token";

        var json = requestHelper.PerformRequest(requestUrl, "application/x-www-form-urlencoded", "POST", data);
        if (json != null)
        {
            var refreshed = OAuth20Token.FromJson(json);
            refreshed.ClientID = token.ClientID;
            refreshed.ClientSecret = token.ClientSecret;
            refreshed.RedirectUri = token.RedirectUri;
            refreshed.RefreshToken ??= token.RefreshToken;

            return refreshed;
        }

        return token;
    }

    private static bool CanRefresh(OAuth20Token token)
    {
        return !string.IsNullOrEmpty(token.ClientID) && !string.IsNullOrEmpty(token.ClientSecret);
    }
}