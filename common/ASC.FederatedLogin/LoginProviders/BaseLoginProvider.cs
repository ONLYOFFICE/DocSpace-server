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

/// <summary>
/// The identity provider used for authentication.
/// </summary>
public enum LoginProvider
{
    [Description("Facebook")]
    Facebook,

    [Description("Google")]
    Google,

    [Description("Dropbox")]
    Dropbox,

    [Description("Docusign")]
    Docusign,

    [Description("Box")]
    Box,

    [Description("OneDrive")]
    OneDrive,

    [Description("GosUslugi")]
    GosUslugi,

    [Description("LinkedIn")]
    LinkedIn,

    [Description("MailRu")]
    MailRu,

    [Description("VK")]
    VK,

    [Description("Wordpress")]
    Wordpress,

    [Description("Yahoo")]
    Yahoo,

    [Description("Yandex")]
    Yandex,

    [Description("Github")]
    Github,

    [Description("Nextcloud")]
    Nextcloud,
}

public abstract class BaseLoginProvider<T> : Consumer, ILoginProvider where T : Consumer, ILoginProvider, new()
{
    public T Instance => ConsumerFactory.Get<T>();
    public virtual bool IsEnabled =>
        !string.IsNullOrEmpty(ClientID) &&
        !string.IsNullOrEmpty(ClientSecret) &&
        !string.IsNullOrEmpty(RedirectUri);

    public abstract string CodeUrl { get; }
    public abstract string AccessTokenUrl { get; }
    public abstract string RedirectUri { get; }
    public abstract string ClientID { get; }
    public abstract string ClientSecret { get; }
    public virtual string Scopes => string.Empty;

    protected readonly IOAuth20TokenHelper _oAuth20TokenHelper;

    protected BaseLoginProvider() { }

    protected BaseLoginProvider(
        IOAuth20TokenHelper oAuth20TokenHelper,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        string name, int order, bool paid, Dictionary<string, string> props, Dictionary<string, string> additional = null)
        : base(tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, paid, props, additional)
    {
        _oAuth20TokenHelper = oAuth20TokenHelper;
    }

    public virtual LoginProfile ProcessAuthorization(HttpContext context, IDictionary<string, string> @params, IDictionary<string, string> additionalStateArgs)
    {
        try
        {
            var token = Auth(context, out var redirect, @params, additionalStateArgs);

            if (redirect)
            {
                return null;
            }

            return GetLoginProfile(token?.AccessToken);
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

    public abstract LoginProfile GetLoginProfile(string accessToken);

    protected virtual OAuth20Token Auth(HttpContext context, out bool redirect, IDictionary<string, string> additionalArgs = null, IDictionary<string, string> additionalStateArgs = null)
    {
        var error = context.Request.Query["error"];
        if (!string.IsNullOrEmpty(error))
        {
            if (error == "access_denied")
            {
                error = "Canceled at provider";
            }

            throw new Exception(error);
        }

        var code = context.Request.Query["code"];
        if (string.IsNullOrEmpty(code))
        {
            context.Response.Redirect(_oAuth20TokenHelper.RequestCode<T>(Scopes, additionalArgs, additionalStateArgs));
            redirect = true;

            return null;
        }

        redirect = false;

        return _oAuth20TokenHelper.GetAccessToken<T>(code);
    }

    public virtual LoginProfile GetLoginProfile(OAuth20Token token)
    {
        return GetLoginProfile(token.AccessToken);
    }

    public OAuth20Token GetToken(string codeOAuth)
    {
        return _oAuth20TokenHelper.GetAccessToken<T>(codeOAuth);
    }
}
