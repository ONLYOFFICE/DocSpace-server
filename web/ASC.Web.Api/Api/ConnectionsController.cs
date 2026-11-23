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

namespace ASC.Web.Api;

/// <remarks>
/// Portal security: the connections a user currently has open and the operations that end them, the audit trail and
/// login history with their reports and retention settings, the portal's Content Security Policy, and the OAuth2
/// token of the current session. The operations under `activeconnections` act on the caller's own connections;
/// addressing another user's connections requires a DocSpace administrator.
/// </remarks>
/// <name>security</name>
[Scope]
[ApiEndpoint("security", "activeconnections")]
public class ConnectionsController(
    UserManager userManager,
    SecurityContext securityContext,
    DbLoginEventsManager dbLoginEventsManager,
    IHttpContextAccessor httpContextAccessor,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    CommonLinkUtility commonLinkUtility,
    ILogger<ConnectionsController> logger,
    MessageService messageService,
    CookiesManager cookiesManager,
    CookieStorage cookieStorage,
    QuotaSocketManager quotaSocketManager,
    GeolocationHelper geolocationHelper,
    ApiDateTimeHelper apiDateTimeHelper)
    : ControllerBase
{
    /// <remarks>
    /// Returns the active connections of the calling user on this portal. Each item is an active sign-in from the last
    /// year, except one case without a cookie. Any signed-in user can call it, including a guest. The list never shows
    /// other users' connections, even to a DocSpace admin. The call is read-only. Items come newest first. If the
    /// request has a portal cookie, its connection comes first. That item shows the browser, platform, IP address, and
    /// location of this request. The `loginEvent` field holds the ID of that connection. Without the cookie, for
    /// example with a token in the `Authorization` header, `loginEvent` is 0. Then, if the user has no active
    /// connections, the list has one item for this request. To end one connection, pass its `id` to `PUT
    /// api/2.0/security/activeconnections/logout/{loginEventId}`.
    /// </remarks>
    /// <summary>
    /// Get active connections
    /// </summary>
    /// <path>api/2.0/security/activeconnections</path>
    [Tags("Security / Active connections")]
    [SwaggerResponse(200, "The caller's active connections, newest sign-in first, with `loginEvent` pointing at the connection the request itself was made with", typeof(ActiveConnectionsDto))]
    [HttpGet("")]
    public async Task<ActiveConnectionsDto> GetAllActiveConnections()
    {
        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var loginEvents = await dbLoginEventsManager.GetLoginEventsAsync(user.TenantId, user.Id);
        var tasks = loginEvents.ConvertAll(async r => await geolocationHelper.AddGeolocationAsync(r));
        var listLoginEvents = (await Task.WhenAll(tasks)).ToList();
        var loginEventId = GetLoginEventIdFromCookie();
        if (loginEventId != 0)
        {
            var loginEvent = listLoginEvents.Find(x => x.Id == loginEventId);
            if (loginEvent != null)
            {
                listLoginEvents.Remove(loginEvent);

                if (httpContextAccessor.HttpContext != null)
                {
                    var baseEvent = await GetBaseEvent();

                    loginEvent.Platform = baseEvent.Platform;
                    loginEvent.Browser = baseEvent.Browser;
                    loginEvent.IP = baseEvent.IP;
                    loginEvent.City = baseEvent.City;
                    loginEvent.Country = baseEvent.Country;
                }

                listLoginEvents.Insert(0, loginEvent);
            }
        }
        else
        {
            if (listLoginEvents.Count == 0 && httpContextAccessor.HttpContext != null)
            {
                var baseEvent = await GetBaseEvent();

                listLoginEvents.Add(baseEvent);
            }
        }

        return new ActiveConnectionsDto
        {
            LoginEvent = loginEventId,
            Items = listLoginEvents.Select(q => new ActiveConnectionsItemDto
            {
                Id = q.Id,
                Browser = q.Browser,
                City = q.City,
                Country = q.Country,
                Date = apiDateTimeHelper.Get(q.Date),
                Ip = q.IP,
                Mobile = q.Mobile,
                Page = q.Page,
                TenantId = q.TenantId,
                Platform = q.Platform,
                UserId = q.UserId

            }).ToList()
        };

        async Task<BaseEvent> GetBaseEvent()
        {
            var request = Request;
            var uaHeader = MessageSettings.GetUAHeader(request);
            var clientInfo = MessageSettings.GetClientInfo(uaHeader);
            var platformAndDevice = MessageSettings.GetPlatformAndDevice(clientInfo);
            var browser = MessageSettings.GetBrowser(clientInfo);
            var ip = MessageSettings.GetIP(request);

            var baseEvent = new BaseEvent
            {
                Platform = platformAndDevice,
                Browser = browser,
                Date = DateTime.Now,
                IP = ip
            };

            return await geolocationHelper.AddGeolocationAsync(baseEvent);
        }
    }

    /// <remarks>
    /// Ends all active connections of the calling user and returns a link to set a new password.
    /// Use it after a suspicious sign-in.
    /// Any signed-in user can call it for their own account, including a guest.
    /// To end the connections of another user, use `PUT api/2.0/security/activeconnections/logoutall/{userId}`.
    /// Every token and cookie issued to the user before the call stops working.
    /// This cannot be undone.
    /// The response sets a new auth cookie, so a cookie-based caller stays signed in.
    /// The call does not change the password or email the link.
    /// The link opens the page that sets the new password through `PUT api/2.0/people/{userId}/password`.
    /// You can repeat the call, but each call makes the earlier link invalid.
    /// Repeat it with the new cookie, not with the first token.
    /// If the call fails, it still returns 200 with an empty result, so call it again.
    /// </remarks>
    /// <summary>
    /// Log out and reset password
    /// </summary>
    /// <path>api/2.0/security/activeconnections/logoutallchangepassword</path>
    [Tags("Security / Active connections")]
    [SwaggerResponse(200, "The URL the user has to open to set a new password, or an empty result when the operation failed", typeof(string))]
    [HttpPut("logoutallchangepassword")]
    public async Task<string> LogOutAllActiveConnectionsChangePassword()
    {
        try
        {
            var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
            var userName = user.DisplayUserName(false, displayUserSettingsHelper);

            await LogOutAllActiveConnections(user.Id);

            securityContext.Logout();

            var auditEventDate = DateTime.UtcNow;
            auditEventDate = auditEventDate.AddTicks(-(auditEventDate.Ticks % TimeSpan.TicksPerSecond));

            var hash = auditEventDate.ToString("s", CultureInfo.InvariantCulture);
            var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.PasswordChange, hash, user.Id);

            messageService.Send(MessageAction.UserSentPasswordChangeInstructions, MessageTarget.Create(user.Id), auditEventDate, userName);

            return confirmationUrl;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            return null;
        }
    }

    /// <remarks>
    /// Ends all active connections of one portal user.
    /// Every token and cookie issued to that user before the call stops working.
    /// This cannot be undone.
    /// Any signed-in user can end their own connections, including a guest.
    /// Only a DocSpace admin can end the connections of another user.
    /// Other callers get 403.
    /// You can repeat the call.
    /// A user with no connections, or an unknown ID, also returns 200.
    /// The response has no body.
    /// If callers end their own connections, the response sets a new auth cookie, so they stay signed in.
    /// The account stays active, and the password stays valid.
    /// To keep the current connection, use `PUT api/2.0/security/activeconnections/logoutallexceptthis`.
    /// </remarks>
    /// <summary>
    /// Log out a user everywhere
    /// </summary>
    /// <path>api/2.0/security/activeconnections/logoutall/{userId}</path>
    [Tags("Security / Active connections")]
    [SwaggerResponse(200, "The connections of that user have been closed; the response carries no content")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator and the ID in the path is not their own")]
    [HttpPut("logoutall/{userId:guid}")]
    public async Task LogOutAllActiveConnectionsForUser(UserIdRequestDto inDto)
    {
        var currentUserId = securityContext.CurrentAccount.ID;
        if (currentUserId != inDto.Id && !await userManager.IsDocSpaceAdminAsync(currentUserId))
        {
            throw new SecurityException("Access denied");
        }

        await LogOutAllActiveConnections(inDto.Id);
    }

    /// <remarks>
    /// Ends all active connections of the calling user except the connection of this request.
    /// Any signed-in user can call it, including a guest.
    /// The call keeps the connection in the portal cookie of the request.
    /// Without the cookie, for example with a token in the `Authorization` header, it ends every connection.
    /// To see which connection stays, first read `loginEvent` of `GET api/2.0/security/activeconnections`.
    /// The tokens and cookies of the ended connections stop working.
    /// This cannot be undone, but you can repeat the call.
    /// It returns the display name of the calling user.
    /// If the call fails, it still returns 200 with an empty result.
    /// Then you cannot tell which connections it ended.
    /// </remarks>
    /// <summary>
    /// Log out other connections
    /// </summary>
    /// <path>api/2.0/security/activeconnections/logoutallexceptthis</path>
    [Tags("Security / Active connections")]
    [SwaggerResponse(200, "The display name of the calling user, or an empty result when the operation failed", typeof(string))]
    [HttpPut("logoutallexceptthis")]
    public async Task<string> LogOutAllExceptThisConnection()
    {
        try
        {
            var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
            var userName = user.DisplayUserName(false, displayUserSettingsHelper);
            var loginEventFromCookie = GetLoginEventIdFromCookie();

            var loginEvents = await dbLoginEventsManager.LogOutAllActiveConnectionsExceptThisAsync(loginEventFromCookie, user.TenantId, user.Id);

            foreach (var loginEvent in loginEvents)
            {
                await quotaSocketManager.LogoutSession(user.Id, loginEvent.Id);
            }

            messageService.Send(MessageAction.UserLogoutActiveConnections, userName);
            return userName;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            return null;
        }
    }

    /// <remarks>
    /// Ends one active connection of a user on this portal.
    /// Its token and cookie stop working, and this cannot be undone.
    /// Other connections stay open.
    /// Take `loginEventId` from the `id` of an item of `GET api/2.0/security/activeconnections`.
    /// The `loginEvent` field of that call shows your own connection, so you can avoid ending it.
    /// Any signed-in user can end their own connections, including a guest.
    /// Only a DocSpace admin can end a connection of another user.
    /// Other callers get 403.
    /// The call returns `true` when it ended the connection.
    /// It returns `false` if this portal has no such active connection or the call failed.
    /// So a repeated call returns `false`.
    /// To end all other connections in one call, use `PUT api/2.0/security/activeconnections/logoutallexceptthis`.
    /// </remarks>
    /// <summary>
    /// Log out one connection
    /// </summary>
    /// <path>api/2.0/security/activeconnections/logout/{loginEventId}</path>
    [Tags("Security / Active connections")]
    [SwaggerResponse(200, "`true` when the connection was open and has been closed, `false` when this portal has no such active connection or the attempt failed", typeof(bool))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator and the connection belongs to another user")]
    [HttpPut("logout/{loginEventId:int}")]
    public async Task<bool> LogOutActiveConnection(LoginEvenrIdRequestDto inDto)
    {
        try
        {
            var currentUserId = securityContext.CurrentAccount.ID;
            var user = await userManager.GetUsersAsync(currentUserId);

            var loginEvent = await dbLoginEventsManager.GetByIdAsync(user.TenantId, inDto.Id);

            if (loginEvent is not { Active: true })
            {
                return false;
            }

            if (loginEvent.UserId.HasValue && currentUserId != loginEvent.UserId && !await userManager.IsDocSpaceAdminAsync(user))
            {
                throw new SecurityException("Access denied");
            }

            var userName = user.DisplayUserName(false, displayUserSettingsHelper);

            await dbLoginEventsManager.LogOutEventAsync(loginEvent.TenantId, loginEvent.Id);

            if (loginEvent.UserId.HasValue)
            {
                await quotaSocketManager.LogoutSession(loginEvent.UserId.Value, loginEvent.Id);
            }

            messageService.Send(MessageAction.UserLogoutActiveConnection, userName);
            return true;
        }
        catch (SecurityException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            return false;
        }
    }

    private async Task LogOutAllActiveConnections(Guid? userId = null)
    {
        var currentUserId = securityContext.CurrentAccount.ID;
        var user = await userManager.GetUsersAsync(userId ?? currentUserId);
        var userName = user.DisplayUserName(false, displayUserSettingsHelper);
        var auditEventDate = DateTime.UtcNow;

        messageService.Send(currentUserId.Equals(user.Id) ? MessageAction.UserLogoutActiveConnections : MessageAction.UserLogoutActiveConnectionsForUser, MessageTarget.Create(user.Id), auditEventDate, userName);
        await cookiesManager.ResetUserCookieAsync(user.Id);

        await quotaSocketManager.LogoutSession(user.Id);
    }

    private int GetLoginEventIdFromCookie()
    {
        var cookie = cookiesManager.GetCookies(CookiesType.AuthKey);
        var (loginEventId, _) = cookieStorage.GetLoginEventIdFromCookie(cookie);
        return loginEventId;
    }
}
