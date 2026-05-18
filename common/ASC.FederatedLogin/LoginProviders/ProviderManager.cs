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
public class ProviderManager(ConsumerFactory consumerFactory)
{
    public bool IsNotEmpty
    {
        get
        {
            return AuthProviders
                .Select(GetLoginProvider)
                .Any(loginProvider => loginProvider is { IsEnabled: true });
        }
    }

    public static readonly List<string> AuthProviders =
    [
        ProviderConstants.Google,
        ProviderConstants.Zoom,
        ProviderConstants.LinkedIn,
        ProviderConstants.Facebook,
        ProviderConstants.Twitter,
        ProviderConstants.Microsoft,
        ProviderConstants.AppleId,
        ProviderConstants.Weixin,
        ProviderConstants.Nextcloud,
    ];

    public static readonly List<string> InviteExceptProviders =
    [
        ProviderConstants.Twitter,
        ProviderConstants.AppleId
    ];

    public static readonly List<string> DummyEmailProviders =
    [
        ProviderConstants.Weixin,
        ProviderConstants.Nextcloud,
    ];

    public static List<string> GetSortedAuthProviders(string geoInfoKey)
    {
        if (geoInfoKey == "CN")
        {
            var result = new List<string> { ProviderConstants.Weixin };
            result.AddRange(AuthProviders.Where(x => x != ProviderConstants.Weixin));
            return result;
        }
        return AuthProviders;
    }

    public ILoginProvider GetLoginProvider(string providerType)
    {
        return consumerFactory.GetByKey(providerType) as ILoginProvider;
    }

    public LoginProfile Process(string providerType, HttpContext context, IDictionary<string, string> @params, IDictionary<string, string> additionalStateArgs = null)
    {
        return GetLoginProvider(providerType).ProcessAuthorization(context, @params, additionalStateArgs);
    }

    public LoginProfile GetLoginProfile(string providerType, string accessToken = null, string codeOAuth = null)
    {
        var consumer = GetLoginProvider(providerType);
        if (consumer == null)
        {
            throw new ArgumentException("Unknown provider type", nameof(providerType));
        }

        try
        {
            if (accessToken == null && codeOAuth != null)
            {
                return consumer.GetLoginProfile(consumer.GetToken(codeOAuth));
            }
            return consumer.GetLoginProfile(accessToken);
        }
        catch (Exception ex)
        {
            return new LoginProfile(ex);
        }
    }
}