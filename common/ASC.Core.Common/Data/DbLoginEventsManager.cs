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

namespace ASC.Core.Data;

[Singleton]
public class LoginEventsCache
{
    private readonly ICache _cache;
    private readonly ICacheNotify<LoginEventCacheItem> _cacheNotify;
    private readonly TimeSpan _expiration = TimeSpan.FromMinutes(10);
    private const string GuidLoginEvent = "F4D8BBF6-EB63-4781-B55E-5885EAB3D759";

    public LoginEventsCache(ICache cache, ICacheNotify<LoginEventCacheItem> cacheNotify)
    {
        _cache = cache;
        _cacheNotify = cacheNotify;
        
        _cacheNotify.Subscribe(i =>
        {
            foreach (var id in i.Ids)
            {
                _cache.Remove(BuildKey(id));
            }
        }, CacheNotifyAction.Remove);
    }

    public void Insert(DbLoginEvent loginEvent)
    {
        _cache.Insert(BuildKey(loginEvent.Id), loginEvent, _expiration);
    }

    public DbLoginEvent Get(int id)
    {
        return _cache.Get<DbLoginEvent>(BuildKey(id));
    }

    public void Remove(IEnumerable<int> ids)
    {
        _cacheNotify.Publish(new LoginEventCacheItem { Ids = ids.ToList() }, CacheNotifyAction.Remove);
    }

    private static string BuildKey(int id)
    {
        return $"{GuidLoginEvent} - {id}";
    }
}


[Scope]
public class DbLoginEventsManager(
    IDbContextFactory<MessagesContext> dbContextFactory,
    LoginEventsCache cache,
    IMapper mapper)
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
        (int)MessageAction.LoginSuccessViaApiTfa
    ];

    public async Task<DbLoginEvent> GetByIdAsync(int tenantId, int id)
    {
        if (tenantId < 0 || id < 0)
        {
            return null;
        }
        
        var loginEvent = cache.Get(id);
        if (loginEvent != null)
        {
            return loginEvent;
        }

        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();
        loginEvent = await loginEventContext.LoginEvents.SingleOrDefaultAsync(e => e.TenantId == tenantId && e.Id == id);

        if (loginEvent != null)
        {
            cache.Insert(loginEvent);
        }
        
        return loginEvent;
    }

    public async Task<List<BaseEvent>> GetLoginEventsAsync(int tenantId, Guid userId)
    {
        var date = DateTime.UtcNow.AddYears(-1);

        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        var loginInfo = await Queries.LoginEventsAsync(loginEventContext, tenantId, userId, _loginActions, date).ToListAsync();

        return mapper.Map<List<DbLoginEvent>, List<BaseEvent>>(loginInfo);
    }

    public async Task LogOutEventAsync(int tenantId, int loginEventId)
    {
        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        await Queries.DeleteLoginEventsAsync(loginEventContext, tenantId, loginEventId);

        cache.Remove([loginEventId]);
    }

    public async Task LogOutAllActiveConnectionsAsync(int tenantId, Guid userId)
    {
        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        var loginEvents = await Queries.LoginEventsByUserIdAsync(loginEventContext, tenantId, userId).ToListAsync();

        await InnerLogOutAsync(loginEventContext, loginEvents);
    }

    public async Task LogOutAllActiveConnectionsForTenantAsync(int tenantId)
    {
        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        var loginEvents = await Queries.LoginEventsByTenantIdAsync(loginEventContext, tenantId).ToListAsync();

        await InnerLogOutAsync(loginEventContext, loginEvents);
    }

    public async Task<List<DbLoginEvent>> LogOutAllActiveConnectionsExceptThisAsync(int loginEventId, int tenantId, Guid userId)
    {
        await using var loginEventContext = await dbContextFactory.CreateDbContextAsync();

        var loginEvents = await Queries.LoginEventsExceptThisAsync(loginEventContext, tenantId, userId, loginEventId).ToListAsync();

        await InnerLogOutAsync(loginEventContext, loginEvents);

        return loginEvents;
    }

    private async Task InnerLogOutAsync(MessagesContext loginEventContext, List<DbLoginEvent> loginEvents)
    {
        cache.Remove(loginEvents.Select(e => e.Id));

        foreach (var loginEvent in loginEvents)
        {
            loginEvent.Active = false;
        }

        loginEventContext.UpdateRange(loginEvents);
        await loginEventContext.SaveChangesAsync();
    }
}

static file class Queries
{
    public static readonly Func<MessagesContext, int, Guid, IEnumerable<int>, DateTime, IAsyncEnumerable<DbLoginEvent>>
        LoginEventsAsync = EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, Guid userId, IEnumerable<int> loginActions, DateTime date) =>
                ctx.LoginEvents
                    .Where(r => r.TenantId == tenantId
                                && r.UserId == userId
                                && loginActions.Contains(r.Action ?? 0)
                                && r.Date >= date
                                && r.Active)
                    .OrderByDescending(r => r.Id)
                    .AsQueryable());

    public static readonly Func<MessagesContext, int, int, Task<int>> DeleteLoginEventsAsync =
        EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, int loginEventId) =>
                ctx.LoginEvents
                    .Where(r => r.TenantId == tenantId && r.Id == loginEventId)
                    .ExecuteUpdate(r => r.SetProperty(p => p.Active, false)));

    public static readonly Func<MessagesContext, int, Guid, IAsyncEnumerable<DbLoginEvent>> LoginEventsByUserIdAsync =
        EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, Guid userId) =>
                ctx.LoginEvents
                    .Where(r => r.TenantId == tenantId
                                && r.UserId == userId
                                && r.Active));

    public static readonly Func<MessagesContext, int, IAsyncEnumerable<DbLoginEvent>> LoginEventsByTenantIdAsync =
        EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId) =>
                ctx.LoginEvents
                    .Where(r => r.TenantId == tenantId
                                && r.Active));

    public static readonly Func<MessagesContext, int, Guid, int, IAsyncEnumerable<DbLoginEvent>> LoginEventsExceptThisAsync =
        EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, Guid userId, int loginEventId) =>
                ctx.LoginEvents
                    .Where(r => r.TenantId == tenantId
                                && r.UserId == userId
                                && r.Id != loginEventId
                                && r.Active));
}