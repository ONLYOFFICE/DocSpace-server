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

namespace ASC.AuditTrail.Repositories;

[Scope]
public class AuditEventsRepository(AuditActionMapper auditActionMapper,
        TenantManager tenantManager,
        IDbContextFactory<MessagesContext> dbContextFactory,
        IMapper mapper,
        GeolocationHelper geolocationHelper)
    {
    public async Task<IEnumerable<AuditEvent>> GetByFilterAsync(
        Guid? userId = null,
        ProductType? productType = null,
        ModuleType? moduleType = null,
        ActionType? actionType = null,
        MessageAction? action = null,
        EntryType? entry = null,
        string target = null,
        DateTime? from = null,
        DateTime? to = null,
        int startIndex = 0,
        int limit = 0,
        Guid? withoutUserId = null)
    {
        return await GetByFilterWithActionsAsync(
            userId,
            productType,
            moduleType,
            actionType,
            [action],
            entry,
            target,
            from,
            to,
            startIndex,
            limit,
            withoutUserId);
    }

    public async Task<IEnumerable<AuditEvent>> GetByFilterWithActionsAsync(
        Guid? userId = null,
        ProductType? productType = null,
        ModuleType? moduleType = null,
        ActionType? actionType = null,
        List<MessageAction?> actions = null,
        EntryType? entry = null,
        string target = null,
        DateTime? from = null,
        DateTime? to = null,
        int startIndex = 0,
        int limit = 0,
        Guid? withoutUserId = null,
        string description = null)
    {
        var tenant = await tenantManager.GetCurrentTenantIdAsync();
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
            IEnumerable<KeyValuePair<MessageAction, MessageMaps>> actionsList = new List<KeyValuePair<MessageAction, MessageMaps>>();

            var isFindActionType = actionType.HasValue && actionType.Value != ActionType.None;

            if (productType.HasValue && productType.Value != ProductType.None)
            {
                var productMapper = auditActionMapper.Mappers.Find(m => m.Product == productType.Value);

                if (productMapper != null)
                {
                    if (moduleType.HasValue && moduleType.Value != ModuleType.None)
                    {
                        var moduleMapper = productMapper.Mappers.Find(m => m.Module == moduleType.Value);
                        if (moduleMapper != null)
                        {
                            actionsList = moduleMapper.Actions;
                        }
                    }
                    else
                    {
                        actionsList = productMapper.Mappers.SelectMany(r => r.Actions);
                    }
                }
            }
            else
            {
                actionsList = auditActionMapper.Mappers
                        .SelectMany(r => r.Mappers)
                        .SelectMany(r => r.Actions);
            }
            
            var isNeedFindEntry = entry.HasValue && entry.Value != EntryType.None && target != null;
            if (isFindActionType || isNeedFindEntry)
            {
                actionsList = actionsList
                        .Where(a => (!isFindActionType || a.Value.ActionType == actionType.Value) && (!isNeedFindEntry || (entry.Value == a.Value.EntryType1) || entry.Value == a.Value.EntryType2))
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

        var hasFromFilter = (from.HasValue && from.Value != DateTime.MinValue);
        var hasToFilter = (to.HasValue && to.Value != DateTime.MinValue);

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
        
        var events = mapper.Map<List<AuditEventQuery>, IEnumerable<AuditEvent>>(await q2.ToListAsync());
        
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