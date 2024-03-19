// (c) Copyright Ascensio System SIA 2009-2024
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

            if (FromCors(httpContext.Request))
            {
                options.Domain = $".{coreBaseSettings.Basedomain}";
            }
        }

        var cookieName = GetFullCookiesName(type, itemId);

        httpContext.Response.Cookies.Append(cookieName, value, options);
    }

    public string GetCookies(IReadOnlyDictionary<string, string> cookies, CookiesType type, string itemId)
    {
        if (cookies == null)
        {
            return string.Empty;
        }

        var name = GetFullCookiesName(type, itemId);

        return cookies.TryGetValue(name, out var value) ? value : string.Empty;
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

        if (httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(cookieName, out var cookie))
        {
            return cookie;
        }

        if (allowHeader && httpContextAccessor.HttpContext.Request.Headers.TryGetValue(cookieName, out var cookieHeader))
        {
            return cookieHeader;
        }
        
        return string.Empty;
    }

    public void ClearCookies(CookiesType type, string itemId = null)
    {
        if (httpContextAccessor?.HttpContext == null)
        {
            return;
        }

        var cookieName = GetFullCookiesName(type, itemId);

        if (httpContextAccessor.HttpContext.Request.Cookies.ContainsKey(cookieName))
        {
            httpContextAccessor.HttpContext.Response.Cookies.Delete(cookieName, new CookieOptions { Expires = DateTime.Now.AddDays(-3) });
        }
    }

    private async Task<DateTime?> GetExpiresDateAsync(bool session)
    {
        DateTime? expires = null;

        if (!session)
        {
            var tenant = await tenantManager.GetCurrentTenantIdAsync();
            expires = await tenantCookieSettingsHelper.GetExpiresTimeAsync(tenant);
        }

        return expires;
    }

    public async Task SetLifeTimeAsync(int lifeTime, bool enabled)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
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
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        return (await tenantCookieSettingsHelper.GetForTenantAsync(tenantId));
    }

    public async Task ResetUserCookieAsync(Guid? userId = null)
    {
        var targetUserId = userId ?? securityContext.CurrentAccount.ID;
        var tenant = await tenantManager.GetCurrentTenantIdAsync();
        var settings = await tenantCookieSettingsHelper.GetForUserAsync(targetUserId);
        settings.Index += 1;
        await tenantCookieSettingsHelper.SetForUserAsync(targetUserId, settings);

        await dbLoginEventsManager.LogOutAllActiveConnectionsAsync(tenant, targetUserId);

        if (targetUserId == securityContext.CurrentAccount.ID)
        {
            await AuthenticateMeAndSetCookiesAsync(targetUserId);
        }
    }

    public async Task ResetTenantCookieAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();

        if (!await userManager.IsUserInGroupAsync(securityContext.CurrentAccount.ID, Constants.GroupAdmin.ID))
        {
            throw new SecurityException();
        }

        var settings = await tenantCookieSettingsHelper.GetForTenantAsync(tenant.Id);
        settings.Index += 1;
        await tenantCookieSettingsHelper.SetForTenantAsync(tenant.Id, settings);

        await dbLoginEventsManager.LogOutAllActiveConnectionsForTenantAsync(tenant.Id);
    }

    public async Task<string> AuthenticateMeAndSetCookiesAsync(Guid userId, MessageAction action = MessageAction.LoginSuccess, bool session = false)
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
            return await ipSecurity.VerifyAsync() ? await GetLoginEventIdAsync(action) : 0;
        }
    }

    private async Task<int> GetLoginEventIdAsync(MessageAction action)
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var userId = securityContext.CurrentAccount.ID;
        var data = new MessageUserData(tenantId, userId);

        return await messageService.SendLoginMessageAsync(data, action);
    }

    public string GetAscCookiesName()
    {
        return GetCookiesName(CookiesType.AuthKey);
    }

    public string GetConfirmCookiesName()
    {
        return GetCookiesName(CookiesType.ConfirmKey);
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

    private string GetCookiesName(CookiesType type)
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

        var request = httpContextAccessor.HttpContext?.Request;
        if (request == null || !FromCors(request) || !request.Headers.TryGetValue(HeaderNames.Origin, out var origin))
        {
            return result;
        }

        var originUri = new Uri(origin);
        var host = originUri.Host;
        var alias = host[..(host.Length - coreBaseSettings.Basedomain.Length - 1)];
        result = $"{result}_{alias}";

        return result;
    }

    private bool FromCors(HttpRequest request)
    {
        var urlRewriter = request.Url();

        try
        {
            if ( request.Headers.TryGetValue(HeaderNames.Origin, out var origin))
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
