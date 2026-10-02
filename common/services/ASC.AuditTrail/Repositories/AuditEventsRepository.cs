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
public class AuditEventsRepository(AuditActionMapper auditActionMapper,
        TenantManager tenantManager,
        IDbContextFactory<MessagesContext> dbContextFactory,
        AuditEventMapper mapper,
        GeolocationHelper geolocationHelper)
{
    public async Task<IEnumerable<AuditEvent>> GetByFilterAsync(
        Guid? userId = null,
        LocationType? moduleType = null,
        ActionType? actionType = null,
        MessageAction? action = null,
        EntryType? entry = null,
        string target = null,
        DateTime? from = null,
        DateTime? to = null,
        int startIndex = 0,
        int limit = 0,
        Guid? withoutUserId = null,
        bool limitedActionText = false)
    {
        return await GetByFilterWithActionsAsync(
            userId,
            moduleType,
            actionType,
            [action],
            entry,
            target,
            from,
            to,
            startIndex,
            limit,
            withoutUserId,
            null,
            limitedActionText);
    }

    public async Task<IEnumerable<AuditEvent>> GetByFilterWithActionsAsync(
        Guid? userId = null,
        LocationType? locationType = null,
        ActionType? actionType = null,
        List<MessageAction?> actions = null,
        EntryType? entry = null,
        string target = null,
        DateTime? from = null,
        DateTime? to = null,
        int startIndex = 0,
        int limit = 0,
        Guid? withoutUserId = null,
        string description = null,
        bool limitedActionText = false)
    {
        var tenant = tenantManager.GetCurrentTenantId();
        await using var auditTrailContext = await dbContextFactory.CreateDbContextAsync();

        var q1 = auditTrailContext.AuditEvents
            .Where(r => r.TenantId == tenant);

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            q1 = q1.Where(r => r.UserId == userId.Value);
        }
        else if (withoutUserId.HasValue && withoutUserId.Value != Guid.Empty)
        {
            q1 = q1.Where(r => r.UserId != withoutUserId.Value);
        }

        if (actions != null && actions.Count != 0 && actions[0] != null && actions[0] != MessageAction.None)
        {
            q1 = q1.Where(r => actions.Contains(r.Action != null ? (MessageAction)r.Action : MessageAction.None));

            if (target != null)
            {
                q1 = q1.Where(r => r.Target == target);
            }
        }
        else
        {
            var actionsList = new List<KeyValuePair<MessageAction, MessageMaps>>();

            var isFindActionType = actionType.HasValue && actionType.Value != ActionType.None;

            if (locationType.HasValue && locationType.Value != LocationType.None)
            {
                foreach (var mappers in auditActionMapper.Mappers)
                {
                    var moduleMapper = mappers.Mappers.Find(m => m.Location == locationType.Value);
                    actionsList.AddRange(moduleMapper.Actions);
                }
            }
            else
            {
                actionsList = auditActionMapper.Mappers
                       .SelectMany(r => r.Mappers)
                       .SelectMany(r => r.Actions)
                       .ToList();
            }

            var isNeedFindEntry = entry.HasValue && entry.Value != EntryType.None && target != null;
            if (isFindActionType || isNeedFindEntry)
            {
                actionsList = actionsList
                        .Where(a => (!isFindActionType || a.Value.ActionType == actionType.Value) && (!isNeedFindEntry || entry.Value == a.Value.EntryType1 || entry.Value == a.Value.EntryType2))
                        .ToList();
            }

            if (isNeedFindEntry)
            {
                q1 = FindByEntry(q1, entry.Value, target, actionsList);
            }
            else
            {
                var keys = actionsList.Select(x => (int)x.Key).ToList();
                q1 = q1.Where(r => keys.Contains(r.Action ?? 0));
            }
        }

        var hasFromFilter = from.HasValue && from.Value != DateTime.MinValue;
        var hasToFilter = to.HasValue && to.Value != DateTime.MinValue;

        if (hasFromFilter || hasToFilter)
        {
            if (hasFromFilter)
            {
                q1 = hasToFilter ? q1.Where(q => q.Date >= from.Value && q.Date <= to.Value) : q1.Where(q => q.Date >= from.Value);
            }
            else if (hasToFilter)
            {
                q1 = q1.Where(q => q.Date <= to.Value);
            }
        }

        if (!string.IsNullOrEmpty(description))
        {
            q1 = q1.Where(r => r.DescriptionRaw.Contains(description));
        }

        // Dates are stored to the second, so events can share one: the id settles their order, or a page window
        // could return an event twice across pages and skip another.
        q1 = q1.OrderByDescending(r => r.Date).ThenByDescending(r => r.Id);

        if (startIndex > 0)
        {
            q1 = q1.Skip(startIndex);
        }

        if (limit > 0)
        {
            q1 = q1.Take(limit);
        }

        var q2 = q1.Select(x => new AuditEventQuery
        {
            Event = x,
            UserData = auditTrailContext.Users.Where(u => u.TenantId == tenant && u.Id == x.UserId).Select(u => new UserData
            {
                FirstName = u.FirstName,
                LastName = u.LastName
            }).FirstOrDefault()
        });

        var eventQueryList = await q2.ToListAsync();
        var events = limitedActionText ? mapper.ToLimitedAuditEvents(eventQueryList) : mapper.ToAuditEvents(eventQueryList);

        await geolocationHelper.AddGeolocationAsync(events);

        return events;
    }

    /// <summary>
    /// Streams the audit events of a period, newest first, in batches of at most <paramref name="batchSize"/>.
    /// Every batch is a short query of its own that resumes after the last event of the previous one, so
    /// neither the command timeout nor the memory of the caller grows with the length of the period.
    /// </summary>
    public async IAsyncEnumerable<IReadOnlyList<AuditEvent>> GetBatchesByPeriodAsync(
        DateTime? from,
        DateTime? to,
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tenant = tenantManager.GetCurrentTenantId();
        var actions = GetMappedActions();

        DateTime? lastDate = null;
        var lastId = 0;

        while (true)
        {
            List<AuditEventQuery> page;

            await using (var auditTrailContext = await dbContextFactory.CreateDbContextAsync(cancellationToken))
            {
                var q = FilterByPeriod(auditTrailContext.AuditEvents.AsNoTracking(), tenant, actions, from, to);

                if (lastDate.HasValue)
                {
                    var date = lastDate.Value;
                    var id = lastId;
                    q = q.Where(r => r.Date < date || (r.Date == date && r.Id < id));
                }

                page = await (
                        from e in q
                        from u in auditTrailContext.Users.Where(u => u.TenantId == tenant && u.Id == e.UserId).DefaultIfEmpty()
                        orderby e.Date descending, e.Id descending
                        select new AuditEventQuery
                        {
                            Event = e,
                            UserData = new UserData
                            {
                                FirstName = u.FirstName,
                                LastName = u.LastName
                            }
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

            var events = mapper.ToAuditEvents(page);

            await geolocationHelper.AddGeolocationAsync(events);

            yield return events;

            if (page.Count < batchSize)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Counts the audit events <see cref="GetBatchesByPeriodAsync"/> streams for the same period.
    /// </summary>
    public async Task<int> GetCountByPeriodAsync(DateTime? from, DateTime? to)
    {
        var tenant = tenantManager.GetCurrentTenantId();
        await using var auditTrailContext = await dbContextFactory.CreateDbContextAsync();

        return await FilterByPeriod(auditTrailContext.AuditEvents, tenant, GetMappedActions(), from, to).CountAsync();
    }

    // An unfiltered audit trail still keeps only the actions some mapper knows how to describe, exactly as
    // GetByFilterWithActionsAsync does when no action, location or action type is given.
    private List<int> GetMappedActions()
    {
        return auditActionMapper.Mappers
            .SelectMany(r => r.Mappers)
            .SelectMany(r => r.Actions)
            .Select(r => (int)r.Key)
            .Distinct()
            .ToList();
    }

    private static IQueryable<DbAuditEvent> FilterByPeriod(IQueryable<DbAuditEvent> q, int tenant, List<int> actions, DateTime? from, DateTime? to)
    {
        q = q.Where(r => r.TenantId == tenant && actions.Contains(r.Action ?? 0));

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

    private static IQueryable<DbAuditEvent> FindByEntry(IQueryable<DbAuditEvent> q, EntryType entry, string target, IEnumerable<KeyValuePair<MessageAction, MessageMaps>> actions)
    {
        var dict = actions.Where(d => d.Value.EntryType1 == entry || d.Value.EntryType2 == entry).ToDictionary(a => (int)a.Key, a => a.Value);

        q = q.Where(r => dict.Keys.Contains(r.Action.Value)
            && r.Target.Contains(target));

        return q;
    }

    public async Task<DbAuditEvent> GetLastEventAsync(int tenantId)
    {
        await using var auditTrailContext = await dbContextFactory.CreateDbContextAsync();

        return await auditTrailContext.AuditEvents
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<int>> GetTenantsAsync(DateTime? from = null, DateTime? to = null)
    {
        await using var feedDbContext = await dbContextFactory.CreateDbContextAsync();

        return await Queries.TenantsAsync(feedDbContext, from, to).ToListAsync();
    }
}

static file class Queries
{
    public static readonly Func<MessagesContext, DateTime?, DateTime?, IAsyncEnumerable<int>> TenantsAsync =
        EF.CompileAsyncQuery(
            (MessagesContext ctx, DateTime? from, DateTime? to) =>
                ctx.AuditEvents
                    .Where(r => r.Date >= from && r.Date <= to)
                    .Select(r => r.TenantId)
                    .Distinct());
}