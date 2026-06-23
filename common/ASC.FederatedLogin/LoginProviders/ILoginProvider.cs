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

public interface ILoginProvider : IOAuthProvider
{
    LoginProfile ProcessAuthorization(HttpContext context, IDictionary<string, string> @params, IDictionary<string, string> additionalStateArgs);

    LoginProfile GetLoginProfile(string accessToken);

    LoginProfile GetLoginProfile(OAuth20Token token);

    OAuth20Token GetToken(string codeOAuth);
}

public interface IOAuthProvider
{
    string Scopes { get; }
    string CodeUrl { get; }
    string AccessTokenUrl { get; }
    string RedirectUri { get; }
    string ClientID { get; }
    string ClientSecret { get; }
    bool IsEnabled { get; }
}

public class OauthProvider : Consumer, IOAuthProvider
{
    public virtual string Scopes => string.Empty;
    public virtual string CodeUrl => string.Empty;
    public virtual string AccessTokenUrl => string.Empty;
    public virtual string RedirectUri => string.Empty;
    public virtual string ClientID => string.Empty;
    public virtual string ClientSecret => string.Empty;
    public virtual bool IsEnabled => false;
    
    public OauthProvider() { }

    public OauthProvider(
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        string name, 
        int order, 
        bool paid,
        Dictionary<string, string> props, 
        Dictionary<string, string> additional = null) 
        : base(tenantManager, 
            coreBaseSettings, 
            coreSettings, 
            configuration, 
            cache, 
            consumerFactory, 
            name, 
            order, 
            paid,
            props,
            additional)
    { }
}