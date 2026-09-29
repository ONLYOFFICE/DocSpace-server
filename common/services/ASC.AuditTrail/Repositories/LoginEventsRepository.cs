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

namespace ASC.AuditTrail.Repositories;

[Scope]
public class LoginEventsRepository(
    TenantManager tenantManager,
    IDbContextFactory<MessagesContext> dbContextFactory,
    GeolocationHelper geolocationHelper,
    LoginEventMapper eventMapper)
{
    public async Task<List<LoginEvent>> GetByFilterAsync(
        Guid? login = null,
        MessageAction? action = null,
        DateTime? fromDate = null,
        DateTime? to = null,
        int startIndex = 0,
        int limit = 0,
        bool limitedActionText = false)
    {
        var tenant = tenantManager.GetCurrentTenantId();
        await using var messagesContext = await dbContextFactory.CreateDbContextAsync();

        var query =
            from q in messagesContext.LoginEvents
            from p in messagesContext.Users.Where(p => q.UserId == p.Id).DefaultIfEmpty()
            where q.TenantId == tenant
            orderby q.Date descending
            select new LoginEventQuery
            {
                Event = q,
                UserName = p.UserName,
                FirstName = p.FirstName,
                LastName = p.LastName
            };

        if (login.HasValue && login.Value != Guid.Empty)
        {
            query = query.Where(r => r.Event.UserId == login.Value);
        }

        if (action.HasValue && action.Value != MessageAction.None)
        {
            query = query.Where(r => r.Event.Action == (int)action);
        }

        var hasFromFilter = fromDate.HasValue && fromDate.Value != DateTime.MinValue;
        var hasToFilter = to.HasValue && to.Value != DateTime.MinValue;

        if (hasFromFilter || hasToFilter)
        {
            if (hasFromFilter)
            {
                query = hasToFilter ?
                    query.Where(q => q.Event.Date >= fromDate.Value & q.Event.Date <= to.Value) :
                    query.Where(q => q.Event.Date >= fromDate.Value);
            }
            else
            {
                query = query.Where(q => q.Event.Date <= to.Value);
            }
        }

        // The page window goes last: applied before the filters it would cut the log first and filter
        // only what was left of it.
        if (startIndex > 0)
        {
            query = query.Skip(startIndex);
        }
        if (limit > 0)
        {
            query = query.Take(limit);
        }

        var eventQueryList = await query.ToListAsync();
        var events = limitedActionText ? eventMapper.ToLimitedLoginEvents(eventQueryList) : eventMapper.ToLoginEvents(eventQueryList);

        foreach (var e in events)
        {
            await geolocationHelper.AddGeolocationAsync(e);
        }

        return events;
    }

    /// <summary>
    /// Streams the login events of a period, newest first, in batches of at most <paramref name="batchSize"/>.
    /// Every batch is a short query of its own that resumes after the last event of the previous one, so
    /// neither the command timeout nor the memory of the caller grows with the length of the period.
    /// </summary>
    public async IAsyncEnumerable<IReadOnlyList<LoginEvent>> GetBatchesByPeriodAsync(
        DateTime? from,
        DateTime? to,
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tenant = tenantManager.GetCurrentTenantId();

        DateTime? lastDate = null;
        var lastId = 0;

        while (true)
        {
            List<LoginEventQuery> page;

            await using (var messagesContext = await dbContextFactory.CreateDbContextAsync(cancellationToken))
            {
                var q = FilterByPeriod(messagesContext.LoginEvents.AsNoTracking(), tenant, from, to);

                if (lastDate.HasValue)
                {
                    var date = lastDate.Value;
                    var id = lastId;
                    q = q.Where(r => r.Date < date || (r.Date == date && r.Id < id));
                }

                page = await (
                        from e in q
                        from p in messagesContext.Users.Where(p => e.UserId == p.Id).DefaultIfEmpty()
                        orderby e.Date descending, e.Id descending
                        select new LoginEventQuery
                        {
                            Event = e,
                            UserName = p.UserName,
                            FirstName = p.FirstName,
                            LastName = p.LastName
                        })
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);
            }

            if (page.Count == 0)
            {
                yield break;
            }

            // The cursor is taken from the stored rows: mapping moves the event dates into the portal time zone.
            lastDate = page[^1].Event.Date;
            lastId = page[^1].Event.Id;

            var events = eventMapper.ToLoginEvents(page);

            await geolocationHelper.AddGeolocationAsync(events);

            yield return events;

            if (page.Count < batchSize)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Counts the login events <see cref="GetBatchesByPeriodAsync"/> streams for the same period.
    /// </summary>
    public async Task<int> GetCountByPeriodAsync(DateTime? from, DateTime? to)
    {
        var tenant = tenantManager.GetCurrentTenantId();
        await using var messagesContext = await dbContextFactory.CreateDbContextAsync();

        return await FilterByPeriod(messagesContext.LoginEvents, tenant, from, to).CountAsync();
    }

    private static IQueryable<DbLoginEvent> FilterByPeriod(IQueryable<DbLoginEvent> q, int tenant, DateTime? from, DateTime? to)
    {
        q = q.Where(r => r.TenantId == tenant);

        if (from.HasValue && from.Value != DateTime.MinValue)
        {
            var fromDate = from.Value;
            q = q.Where(r => r.Date >= fromDate);
        }

        if (to.HasValue && to.Value != DateTime.MinValue)
        {
            var toDate = to.Value;
            q = q.Where(r => r.Date <= toDate);
        }

        return q;
    }

    public async Task<DbLoginEvent> GetLastSuccessEventAsync(int tenantId)
    {
        await using var auditTrailContext = await dbContextFactory.CreateDbContextAsync();

        var successLoginEvents = new List<int> {
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
            (int)MessageAction.LoginSuccessViaOAuth,
            (int)MessageAction.LoginSuccessViaPassword,

            (int)MessageAction.AuthLinkActivated,

            (int)MessageAction.Logout
        };

        return await auditTrailContext.LoginEvents
            .Where(r => r.TenantId == tenantId)
            .Where(r => successLoginEvents.Contains(r.Action ?? 0))
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();
    }
}