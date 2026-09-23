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
    /// Lists the connections the calling user currently has open on this portal - one item per successful sign-in
    /// that is still active - so a client can show where the account is signed in and close what does not belong
    /// there. Any signed-in user may call it, nothing has to be called first, and the answer always covers the caller
    /// alone: the operation is read-only, idempotent and cannot show another user's connections. Items cover the last
    /// year and are ordered newest sign-in first, with the caller's own connection moved to the top and its browser,
    /// platform, IP address and location refreshed from the current request. `loginEvent` is the ID of that own
    /// connection and is `0` when the request was authenticated with a token in the `Authorization` header instead of
    /// the portal cookie; nothing is then marked as current, and a user with no stored connections gets a single item
    /// describing the current request. `country` and `city` are resolved from the IP address and stay empty when it
    /// cannot be located. Pass an item's `id` to `PUT api/2.0/security/activeconnections/logout/{loginEventId}` to
    /// end that one connection.
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
    /// Closes every active connection of the calling user and returns the link that user has to open to set a new
    /// password - the answer to a suspicious sign-in seen in `GET api/2.0/security/activeconnections`. Any signed-in
    /// user may call it for their own account and nothing has to be called first; the same clean-up for somebody else
    /// is `PUT api/2.0/security/activeconnections/logoutall/{userId}`. The call is mutating and destructive for
    /// sessions - every token and cookie issued to the user before it stops working and the clients holding them are
    /// disconnected - and it is not idempotent: the request is written to the portal audit trail, which invalidates
    /// the link any earlier call returned, and the caller's own client is handed a fresh cookie in the response and
    /// stays signed in through a new connection. The password itself is not changed here, and the link is handed back
    /// to the caller rather than mailed to the user: the URL carries a time-limited `PasswordChange` key, which the
    /// confirmation page it opens - or `PUT api/2.0/people/{userid}/password` - needs to accept the new password. A
    /// failure is swallowed instead of reported, so an empty body with status 200 means nothing was done and the call
    /// has to be repeated.
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
    /// Closes every active connection of one portal user: the connections are marked inactive, every token and cookie
    /// issued to that user before the call stops working, the clients holding them are disconnected and a logout
    /// entry is written to the portal audit trail. Nothing has to be called first; `userId` is the portal user ID
    /// that `GET api/2.0/people` returns. A user may pass their own ID, while ending somebody else's connections
    /// requires a DocSpace administrator and any other caller is refused with 403. The call is mutating, destructive
    /// for those sessions and idempotent - a user with nothing open is not an error - and it returns no content, so
    /// the state afterwards is read from `GET api/2.0/security/activeconnections`. A caller who ends their own
    /// connections is handed a fresh cookie in the response and stays signed in through a new connection. Nothing
    /// else about the user changes: the account stays enabled and the password stays valid, and to keep the current
    /// connection alive instead use `PUT api/2.0/security/activeconnections/logoutallexceptthis`.
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
    /// Closes every active connection of the calling user except the one this request was made with, so the current
    /// client keeps working while every other browser and device is signed out. Any signed-in user may call it for
    /// their own account and nothing has to be called first. The connection to keep is the one behind the portal
    /// authentication cookie: a request authenticated with a token in the `Authorization` header has none, and then
    /// every connection of the user is closed, including the one that token belongs to - read `loginEvent` from
    /// `GET api/2.0/security/activeconnections` first to see which connection, if any, will survive. The call is
    /// mutating and destructive for the other sessions, and idempotent: the tokens behind them stop working, their
    /// clients are disconnected at once and a logout entry is written to the portal audit trail. It answers with the
    /// display name of the calling user, while an empty answer with status 200 means the attempt failed and nothing
    /// can be assumed about what was closed.
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
    /// Closes one active connection: the sign-in behind `loginEventId` is marked inactive, the token and cookie tied
    /// to it stop working, the client holding it is disconnected and a logout entry is written to the portal audit
    /// trail. Take `loginEventId` from the `id` of an item of `GET api/2.0/security/activeconnections`, which also
    /// reports in `loginEvent` which connection the caller is using, so a client can avoid closing its own. A user
    /// may close their own connections, while closing somebody else's requires a DocSpace administrator and any other
    /// caller is refused with 403. The call is mutating, destructive for that one session and idempotent, and it
    /// leaves every other connection of the user alone - `PUT api/2.0/security/activeconnections/logoutallexceptthis`
    /// is the way to close the rest in one go. Only `true` means the connection was open and has just been closed;
    /// `false` comes back when this portal has no such active connection, including one that was already closed, and
    /// after any other failure.
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
