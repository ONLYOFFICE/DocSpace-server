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

using Constants = ASC.Core.Users.Constants;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace ASC.Web.Core;

public enum CookiesType
{
    AuthKey,
    SocketIO,
    ShareLink,
    AnonymousSessionKey,
    ConfirmKey
}

[Scope]
public class CookiesManager(
    IHttpContextAccessor httpContextAccessor,
    UserManager userManager,
    SecurityContext securityContext,
    TenantCookieSettingsHelper tenantCookieSettingsHelper,
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings,
    DbLoginEventsManager dbLoginEventsManager,
    MessageService messageService,
    IPSecurity.IPSecurity ipSecurity,
    IConfiguration configuration,
    SettingsManager settingsManager)
{
    public const string AuthCookiesName = "asc_auth_key";
    private const string SocketIOCookiesName = "socketio.sid";
    private const string ShareLinkCookiesName = "sharelink";
    private const string AnonymousSessionKeyCookiesName = "anonymous_session_key";
    private const string ConfirmCookiesName = "asc_confirm_key";

    public async Task SetCookiesAsync(CookiesType type, string value, bool session = false, string itemId = null)
    {
        if (httpContextAccessor?.HttpContext == null)
        {
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;

        var options = new CookieOptions
        {
            Expires = await GetExpiresDateAsync(session)
        };

        if (type is CookiesType.AuthKey or CookiesType.ConfirmKey or CookiesType.AnonymousSessionKey or CookiesType.ShareLink)
        {
            options.HttpOnly = true;

            SameSiteMode? sameSiteMode = null;
            if (Enum.TryParse<SameSiteMode>(configuration["web:samesite"], out var sameSiteModeFromConfig))
            {
                sameSiteMode = sameSiteModeFromConfig;
            }

            if (sameSiteMode.HasValue && sameSiteMode.Value != SameSiteMode.None)
            {
                options.SameSite = sameSiteMode.Value;
            }

            var requestUrlScheme = httpContext.Request.Url().Scheme;

            if (Uri.UriSchemeHttps.Equals(requestUrlScheme, StringComparison.OrdinalIgnoreCase))
            {
                options.Secure = true;

                if (sameSiteMode is SameSiteMode.None)
                {
                    options.SameSite = sameSiteMode.Value;
                }
                else if (!sameSiteMode.HasValue)
                {
                    var cspSettings = await settingsManager.LoadAsync<CspSettings>();

                    if (cspSettings.Domains != null && cspSettings.Domains.Any())
                    {
                        options.SameSite = SameSiteMode.None;
                    }
                }
            }

            if (options.SameSite == SameSiteMode.Unspecified)
            {
                options.SameSite = SameSiteMode.Strict;
            }

            // CHIPS: when the cookie is used cross-site (SameSite=None over HTTPS, e.g. embedded in an iframe),
            // mark it Partitioned so modern browsers (Chrome, Firefox, Safari 18.4+) keep it in a
            // per-top-level-site jar instead of dropping it as a third-party tracking cookie.
            if (options is { SameSite: SameSiteMode.None, Secure: true })
            {
                options.Extensions.Add("Partitioned");
            }

            if (FromCors(httpContext.Request))
            {
                options.Domain = $".{coreBaseSettings.Basedomain}";
            }
        }

        var cookieName = GetFullCookiesName(type, itemId);

        httpContext.Response.Cookies.Append(cookieName, value, options);
    }

    public string GetCookies(CookiesType type)
    {
        return httpContextAccessor?.HttpContext != null && httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(GetCookiesName(type), out var cookie)
            ? cookie
            : string.Empty;
    }

    public string GetCookies(CookiesType type, string itemId, bool allowHeader = false)
    {
        if (httpContextAccessor?.HttpContext == null)
        {
            return string.Empty;
        }

        var cookieName = GetFullCookiesName(type, itemId);

        if (allowHeader && httpContextAccessor.HttpContext.Request.Headers.TryGetValue(cookieName, out var cookieHeader))
        {
            return cookieHeader;
        }

        if (httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(cookieName, out var cookie) && !string.IsNullOrEmpty(cookie))
        {
            return cookie;
        }

        return string.Empty;
    }

    public void ClearCookies(CookiesType type, string itemId = null)
    {
        if (httpContextAccessor?.HttpContext == null)
        {
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        var cookieName = GetFullCookiesName(type, itemId);

        if (!httpContext.Request.Cookies.ContainsKey(cookieName))
        {
            return;
        }

        var expires = DateTime.Now.AddDays(-3);
        var isHttps = Uri.UriSchemeHttps.Equals(httpContext.Request.Url().Scheme, StringComparison.OrdinalIgnoreCase);

        var domains = new List<string> { null };
        if (!string.IsNullOrEmpty(coreBaseSettings.Basedomain))
        {
            domains.Add($".{coreBaseSettings.Basedomain}");
        }

        // The cookie may exist as several distinct browser entries depending on how it was set (SetCookiesAsync):
        //  - host-only vs Domain-scoped (FromCors / SaaS sub-domains),
        //  - unpartitioned vs Partitioned (CHIPS, cross-site embedding over HTTPS).
        // Emit a matching deletion for every combination; non-matching ones are harmless (already-expired) no-ops.
        // Only the first header uses Delete (it deduplicates by name); the rest use Append so previously
        // emitted deletion headers are not dropped.
        var first = true;

        foreach (var domain in domains)
        {
            Clear(domain, false);

            if (isHttps)
            {
                Clear(domain, true);
            }
        }

        return;

        void Clear(string domain, bool partitioned)
        {
            var options = new CookieOptions { Expires = expires, Domain = domain };

            if (partitioned)
            {
                options.Secure = true;
                options.SameSite = SameSiteMode.None;
                options.Extensions.Add("Partitioned");
            }

            if (first)
            {
                httpContext.Response.Cookies.Delete(cookieName, options);
                first = false;
            }
            else
            {
                httpContext.Response.Cookies.Append(cookieName, string.Empty, options);
            }
        }
    }

    private async Task<DateTime?> GetExpiresDateAsync(bool session)
    {
        DateTime? expires = null;

        if (!session)
        {
            var tenant = tenantManager.GetCurrentTenantId();
            expires = await tenantCookieSettingsHelper.GetExpiresTimeAsync(tenant);
        }

        return expires;
    }

    public async Task SetLifeTimeAsync(int lifeTime, bool enabled)
    {
        var tenant = tenantManager.GetCurrentTenant();
        if (!await userManager.IsUserInGroupAsync(securityContext.CurrentAccount.ID, Constants.GroupAdmin.ID))
        {
            throw new SecurityException();
        }

        var settings = await tenantCookieSettingsHelper.GetForTenantAsync(tenant.Id);
        settings.Enabled = enabled;

        if (lifeTime > 0)
        {
            settings.Index += 1;
            settings.LifeTime = lifeTime > 9999 ? 9999 : lifeTime;
        }
        else
        {
            settings.LifeTime = 0;
        }

        await tenantCookieSettingsHelper.SetForTenantAsync(tenant.Id, settings);

        if (enabled && lifeTime > 0)
        {
            await dbLoginEventsManager.LogOutAllActiveConnectionsForTenantAsync(tenant.Id);
        }

        await AuthenticateMeAndSetCookiesAsync(securityContext.CurrentAccount.ID);
    }

    public async Task<TenantCookieSettings> GetLifeTimeAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return await tenantCookieSettingsHelper.GetForTenantAsync(tenantId);
    }

    public async Task ResetUserCookieAsync(Guid? userId = null, bool keepMeAuthenticated = true)
    {
        var targetUserId = userId ?? securityContext.CurrentAccount.ID;
        var tenant = tenantManager.GetCurrentTenantId();
        var settings = await tenantCookieSettingsHelper.GetForUserAsync(targetUserId);
        settings.Index += 1;
        await tenantCookieSettingsHelper.SetForUserAsync(targetUserId, settings);

        await dbLoginEventsManager.LogOutAllActiveConnectionsAsync(tenant, targetUserId);

        if (keepMeAuthenticated && targetUserId == securityContext.CurrentAccount.ID)
        {
            await AuthenticateMeAndSetCookiesAsync(targetUserId);
        }
    }

    public async Task ResetTenantCookieAsync()
    {
        var tenant = tenantManager.GetCurrentTenant();

        if (!await userManager.IsUserInGroupAsync(securityContext.CurrentAccount.ID, Constants.GroupAdmin.ID))
        {
            throw new SecurityException();
        }

        var settings = await tenantCookieSettingsHelper.GetForTenantAsync(tenant.Id);
        settings.Index += 1;
        await tenantCookieSettingsHelper.SetForTenantAsync(tenant.Id, settings);

        await dbLoginEventsManager.LogOutAllActiveConnectionsForTenantAsync(tenant.Id);
    }

    public async Task<string> AuthenticateMeAndSetCookiesAsync(Guid userId, MessageAction action = MessageAction.LoginSuccess, bool session = false, string initiator = null, params string[] description)
    {
        var isSuccess = true;
        var cookies = string.Empty;

        try
        {
            cookies = await securityContext.AuthenticateMeAsync(userId, FuncLoginEvent);
        }
        catch (Exception)
        {
            isSuccess = false;
            throw;
        }
        finally
        {
            if (isSuccess)
            {
                await SetCookiesAsync(CookiesType.AuthKey, cookies, session);
            }
        }

        return cookies;

        async Task<int> FuncLoginEvent()
        {
            return await ipSecurity.VerifyAsync() ? await GetLoginEventIdAsync(action, initiator, description) : 0;
        }
    }

    private async Task<int> GetLoginEventIdAsync(MessageAction action, string initiator, params string[] description)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = securityContext.CurrentAccount.ID;
        var data = new MessageUserData(tenantId, userId);

        return await messageService.SendLoginMessageAsync(data, action, initiator, description);
    }

    public string GetAscCookiesName()
    {
        return GetCookiesName(CookiesType.AuthKey);
    }

    public string GetConfirmCookiesName(string itemId = null)
    {
        return GetFullCookiesName(CookiesType.ConfirmKey, itemId);
    }

    public static string GetAnonymousSessionKeyCookiesName()
    {
        return GetCookiesName(CookiesType.AnonymousSessionKey);
    }

    private string GetFullCookiesName(CookiesType type, string itemId = null)
    {
        var name = GetCookiesName(type);

        if (!string.IsNullOrEmpty(itemId))
        {
            name += itemId;
        }

        return name;
    }

    private static string GetCookiesName(CookiesType type)
    {
        var result = type switch
        {
            CookiesType.AuthKey => AuthCookiesName,
            CookiesType.SocketIO => SocketIOCookiesName,
            CookiesType.ShareLink => ShareLinkCookiesName,
            CookiesType.AnonymousSessionKey => AnonymousSessionKeyCookiesName,
            CookiesType.ConfirmKey => ConfirmCookiesName,
            _ => string.Empty
        };

        return result;
    }

    private bool FromCors(HttpRequest request)
    {
        var urlRewriter = request.Url();

        try
        {
            if (request.Headers.TryGetValue(HeaderNames.Origin, out var origin))
            {
                var originUri = new Uri(origin);
                var baseDomain = coreBaseSettings.Basedomain;

                if (!string.IsNullOrEmpty(baseDomain) && urlRewriter.Host != originUri.Host && originUri.Host.EndsWith(baseDomain))
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {

        }

        return false;
    }
}
