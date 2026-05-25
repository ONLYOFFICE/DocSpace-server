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

namespace ASC.Core.Data;

[Singleton]
public class LoginEventsCache(IFusionCacheProvider cacheProvider)
{
    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();
    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(10);
    private const string GuidLoginEvent = "F4D8BBF6-EB63-4781-B55E-5885EAB3D759";

    public async Task InsertAsync(DbLoginEvent loginEvent)
    {
        await _cache.SetAsync(BuildKey(loginEvent.Id), loginEvent, _expiration);
    }

    public async Task<DbLoginEvent> GetAsync(int id)
    {
        return await _cache.GetOrDefaultAsync<DbLoginEvent>(BuildKey(id));
    }

    public async Task RemoveAsync(IEnumerable<int> ids)
    {
        foreach (var id in ids)
        {
            await _cache.RemoveAsync(BuildKey(id));
        }
    }

    private static string BuildKey(int id)
    {
        return $"{GuidLoginEvent} - {id}";
    }
}


[Scope]
public class DbLoginEventsManager(
    BaseEventMapper mapper,
    IDbContextFactory<MessagesContext> dbContextFactory,
    LoginEventsCache cache)
{
    private static readonly List<int> _loginActions =
    [
        (int)MessageAction.LoginSuccess,
        (int)MessageAction.LoginSuccessViaSocialAccount,
        (int)MessageAction.LoginSuccessViaSms,
        (int)MessageAction.LoginSuccessViaApi,
        (int)MessageAction.LoginSuccessViaSocialApp,
        (int)MessageAction.LoginSuccessViaApiSms,
        (int)MessageAction.LoginSuccessViaSSO,
        (int)MessageAction.LoginSuccessViaApiSocialAccount,
        (int)MessageAction.LoginSuccesViaTfaApp,
        (int)MessageAction.LoginSuccessViaApiTfa,
        (int)MessageAction.LoginSuccessViaPassword,

        (int)MessageAction.AuthLinkActivated
    ];

    public async Task<DbLoginEvent> GetByIdAsync(int tenantId, int id)
    {
        if (tenantId < 0 || id < 0)
        {
            return null;
        }

        var loginEvent = await cache.GetAsync(id);
        if (loginEvent != null)
        {
            return loginEvent;
        }

        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();
        loginEvent = await loginEventContext.LoginEventsByIdAsync(tenantId, id);

        if (loginEvent != null)
        {
            await cache.InsertAsync(loginEvent);
        }

        return loginEvent;
    }

    public async Task<List<BaseEvent>> GetLoginEventsAsync(int tenantId, Guid userId)
    {
        var date = DateTime.UtcNow.AddYears(-1);

        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        var loginInfo = await loginEventContext.LoginEventsAsync(tenantId, userId, _loginActions, date).ToListAsync();

        return mapper.Map(loginInfo);
    }

    public async Task LogOutEventAsync(int tenantId, int loginEventId)
    {
        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        await loginEventContext.DeleteLoginEventsAsync(tenantId, loginEventId);

        await cache.RemoveAsync([loginEventId]);
    }

    public async Task LogOutAllActiveConnectionsAsync(int tenantId, Guid userId)
    {
        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        var loginEvents = await loginEventContext.LoginEventsByUserIdAsync(tenantId, userId).ToListAsync();

        await InnerLogOutAsync(loginEventContext, loginEvents);
    }

    public async Task LogOutAllActiveConnectionsForTenantAsync(int tenantId)
    {
        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        var loginEvents = await loginEventContext.LoginEventsByTenantIdAsync(tenantId).ToListAsync();

        await InnerLogOutAsync(loginEventContext, loginEvents);
    }

    public async Task<List<DbLoginEvent>> LogOutAllActiveConnectionsExceptThisAsync(int loginEventId, int tenantId, Guid userId)
    {
        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        var loginEvents = await loginEventContext.LoginEventsExceptThisAsync(tenantId, userId, loginEventId).ToListAsync();

        await InnerLogOutAsync(loginEventContext, loginEvents);

        return loginEvents;
    }

    private async Task InnerLogOutAsync(MessagesContext loginEventContext, List<DbLoginEvent> loginEvents)
    {
        if (loginEvents.Count == 0)
        {
            return;
        }

        await cache.RemoveAsync(loginEvents.Select(e => e.Id));

        foreach (var loginEvent in loginEvents)
        {
            loginEvent.Active = false;
        }

        loginEventContext.UpdateRange(loginEvents);
        await loginEventContext.SaveChangesAsync();
    }
}