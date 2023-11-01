// (c) Copyright Ascensio System SIA 2010-2022
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

[Scope]
public class DbLoginEventsManager
{
    private const string GuidLoginEvent = "F4D8BBF6-EB63-4781-B55E-5885EAB3D759";
    private static readonly List<int> _loginActions = new()
    {
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
    };

    private readonly IDbContextFactory<MessagesContext> _dbContextFactory;
    private readonly IMapper _mapper;
    private readonly ICache _cache;

    public DbLoginEventsManager(
        IDbContextFactory<MessagesContext> dbContextFactory,
        IMapper mapper,
        ICache cache)
    {
        _dbContextFactory = dbContextFactory;
        _mapper = mapper;
        _cache = cache;
    }

    public async Task<DbLoginEvent> GetByIdAsync(int id)
    {
        if (id < 0) return null;

        var loginEvent = _cache.Get<DbLoginEvent>($"{GuidLoginEvent} - {id}");
        if(loginEvent == null)
        {
            await using var loginEventContext = await _dbContextFactory.CreateDbContextAsync();
            loginEvent = await loginEventContext.LoginEvents.FindAsync(id);

            _cache.Insert($"{GuidLoginEvent} - {id}", loginEvent, TimeSpan.FromMinutes(10));
        }
        return loginEvent;
    }

    public async Task<List<BaseEvent>> GetLoginEventsAsync(int tenantId, Guid userId)
    {
        var date = DateTime.UtcNow.AddYears(-1);

        await using var loginEventContext = await _dbContextFactory.CreateDbContextAsync();

        var loginInfo = await Queries.LoginEventsAsync(loginEventContext, tenantId, userId, _loginActions, date).ToListAsync();

        return _mapper.Map<List<DbLoginEvent>, List<BaseEvent>>(loginInfo);
    }

    public async Task LogOutEventAsync(int loginEventId)
    {
        await using var loginEventContext = await _dbContextFactory.CreateDbContextAsync();

        await Queries.DeleteLoginEventsAsync(loginEventContext, loginEventId);

        ResetCache(loginEventId);
    }

    public async Task LogOutAllActiveConnectionsAsync(int tenantId, Guid userId)
    {
        await using var loginEventContext = await _dbContextFactory.CreateDbContextAsync();

        var loginEvents = await Queries.LoginEventsByUserIdAsync(loginEventContext, tenantId, userId).ToListAsync();

        await InnerLogOutAsync(loginEventContext, loginEvents);
    }

    public async Task LogOutAllActiveConnectionsForTenantAsync(int tenantId)
    {
        await using var loginEventContext = await _dbContextFactory.CreateDbContextAsync();

        var loginEvents = await Queries.LoginEventsByTenantIdAsync(loginEventContext, tenantId).ToListAsync();

        await InnerLogOutAsync(loginEventContext, loginEvents);
    }

    public async Task LogOutAllActiveConnectionsExceptThisAsync(int loginEventId, int tenantId, Guid userId)
    {
        await using var loginEventContext = await _dbContextFactory.CreateDbContextAsync();

        var loginEvents = await Queries.LoginEventsExceptThisAsync(loginEventContext, tenantId, userId, loginEventId).ToListAsync();

        await InnerLogOutAsync(loginEventContext, loginEvents);
    }

    private async Task InnerLogOutAsync(MessagesContext loginEventContext, List<DbLoginEvent> loginEvents)
    {
        ResetCache(loginEvents);

        foreach (var loginEvent in loginEvents)
        {
            loginEvent.Active = false;
        }

        loginEventContext.UpdateRange(loginEvents);
        await loginEventContext.SaveChangesAsync();
    }

    private void ResetCache(int id)
    {
        _cache.Remove($"{GuidLoginEvent} - {id}");
    }

    private void ResetCache(List<DbLoginEvent> loginEvents)
    {
        foreach(var loginEvent in loginEvents)
        {
            _cache.Remove($"{GuidLoginEvent} - {loginEvent.Id}");
        }
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

    public static readonly Func<MessagesContext, int, Task<int>> DeleteLoginEventsAsync =
        EF.CompileAsyncQuery(
            (MessagesContext ctx, int loginEventId) =>
                ctx.LoginEvents
                    .Where(r => r.Id == loginEventId)
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