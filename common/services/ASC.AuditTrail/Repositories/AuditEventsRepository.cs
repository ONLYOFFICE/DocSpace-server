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

        q1 = q1.OrderByDescending(r => r.Date);

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

        foreach (var e in events)
        {
            await geolocationHelper.AddGeolocationAsync(e);
        }

        return events;
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