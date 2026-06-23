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

namespace ASC.Core.Security.Authentication;

[Scope]
public class CookieStorage(InstanceCrypto instanceCrypto,
    TenantCookieSettingsHelper tenantCookieSettingsHelper,
    ILogger<CookieStorage> logger)
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss,fff";

    private readonly HttpContext _httpContext;

    public CookieStorage(
        IHttpContextAccessor httpContextAccessor,
        InstanceCrypto instanceCrypto,
        TenantCookieSettingsHelper tenantCookieSettingsHelper,
        ILogger<CookieStorage> logger)
        : this(instanceCrypto, tenantCookieSettingsHelper, logger)
    {
        _httpContext = httpContextAccessor.HttpContext;
    }

    public bool DecryptCookie(string cookie, out int tenant, out Guid userid, out int indexTenant, out DateTime expire, out int indexUser, out int loginEventId)
    {
        tenant = Tenant.DefaultTenant;
        userid = Guid.Empty;
        indexTenant = 0;
        expire = DateTime.MaxValue;
        indexUser = 0;
        loginEventId = 0;

        if (string.IsNullOrEmpty(cookie))
        {
            return false;
        }

        try
        {
            cookie = HttpUtility.UrlDecode(cookie).Replace(' ', '+');
            var s = instanceCrypto.Decrypt(cookie).Split('$');

            if (1 < s.Length)
            {
                tenant = int.Parse(s[1]);
            }
            if (4 < s.Length)
            {
                userid = new Guid(s[4]);
            }
            if (5 < s.Length)
            {
                indexTenant = int.Parse(s[5]);
            }
            if (6 < s.Length)
            {
                expire = DateTime.ParseExact(s[6], DateTimeFormat, CultureInfo.InvariantCulture);
            }
            if (7 < s.Length)
            {
                indexUser = int.Parse(s[7]);
            }
            if (8 < s.Length)
            {
                loginEventId = !string.IsNullOrEmpty(s[8]) ? int.Parse(s[8]) : 0;
            }
            return true;
        }
        catch (Exception err)
        {
            logger.AuthenticateError(cookie, tenant, userid, indexTenant, expire.ToString(DateTimeFormat, CultureInfo.InvariantCulture), loginEventId, err);
        }

        return false;
    }


    public (int loginEventId, DateTime expiration) GetLoginEventIdFromCookie(string cookie)
    {
        var loginEventId = 0;
        var expiration = DateTime.MaxValue;

        if (string.IsNullOrEmpty(cookie))
        {
            return (loginEventId, expiration);
        }

        try
        {
            cookie = HttpUtility.UrlDecode(cookie).Replace(' ', '+');
            var s = instanceCrypto.Decrypt(cookie).Split('$');
            if (8 < s.Length)
            {
                loginEventId = !string.IsNullOrEmpty(s[8]) ? int.Parse(s[8]) : 0;
                expiration = DateTime.ParseExact(s[6], DateTimeFormat, CultureInfo.InvariantCulture);
            }
        }
        catch (Exception err)
        {
            logger.ErrorLoginEvent(cookie, loginEventId, err);
        }

        return (loginEventId, expiration);
    }

    public async Task<string> EncryptCookieAsync(int tenant, Guid userid, int loginEventId)
    {
        var settingsTenant = await tenantCookieSettingsHelper.GetForTenantAsync(tenant);
        var expires = await tenantCookieSettingsHelper.GetExpiresTimeAsync(tenant);
        var settingsUser = await tenantCookieSettingsHelper.GetForUserAsync(tenant, userid);

        return await EncryptCookieAsync(tenant, userid, settingsTenant.Index, expires, settingsUser.Index, loginEventId);
    }

    public async Task<string> EncryptCookieAsync(int tenant, Guid userid, int indexTenant, DateTime expires, int indexUser, int loginEventId)
    {
        var s = string.Format("{0}${1}${2}${3}${4:N}${5}${6}${7}${8}",
            string.Empty, //login
            tenant,
            string.Empty, //password
            GetUserDependencySalt(),
            userid,
            indexTenant,
            expires.ToString(DateTimeFormat, CultureInfo.InvariantCulture),
            indexUser,
            loginEventId != 0 ? loginEventId.ToString() : null);

        return await instanceCrypto.EncryptAsync(s);
    }

    private string GetUserDependencySalt()
    {
        var data = string.Empty;
        try
        {
            if (_httpContext is { Connection.RemoteIpAddress: not null })
            {
                data = _httpContext.Connection.RemoteIpAddress.ToString();
            }
        }
        catch { }

        return Hasher.Base64Hash(data, HashAlg.SHA256);
    }
}