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
public class LoginEventsRepository(TenantManager tenantManager,
    IDbContextFactory<MessagesContext> dbContextFactory,
    IMapper mapper,
    GeolocationHelper geolocationHelper)
{
    public async Task<IEnumerable<LoginEvent>> GetByFilterAsync(
        Guid? login = null,
        MessageAction? action = null,
        DateTime? fromDate = null,
        DateTime? to = null,
        int startIndex = 0,
        int limit = 0)
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

        if (startIndex > 0)
        {
            query = query.Skip(startIndex);
        }
        if (limit > 0)
        {
            query = query.Take(limit);
        }

        if (login.HasValue && login.Value != Guid.Empty)
        {
            query = query.Where(r => r.Event.UserId == login.Value);
        }

        if (action.HasValue && action.Value != MessageAction.None)
        {
            query = query.Where(r => r.Event.Action == (int)action);
        }

        var hasFromFilter = (fromDate.HasValue && fromDate.Value != DateTime.MinValue);
        var hasToFilter = (to.HasValue && to.Value != DateTime.MinValue);

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

        var events = mapper.Map<List<LoginEventQuery>, IEnumerable<LoginEvent>>(await query.ToListAsync());

        foreach (var e in events)
        {
            await geolocationHelper.AddGeolocationAsync(e);
        }
        return events;
    }

    public async Task<DbLoginEvent> GetLastSuccessEventAsync(int tenantId)
    {
        await using var auditTrailContext = await dbContextFactory.CreateDbContextAsync();

        var successLoginEvents = new List<int>() {
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

            (int)MessageAction.Logout
        };

        return await auditTrailContext.LoginEvents
            .Where(r => r.TenantId == tenantId)
            .Where(r => successLoginEvents.Contains(r.Action ?? 0))
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();
    }
}