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

        var eventQueryList = await query.ToListAsync();
        var events = limitedActionText ? eventMapper.ToLimitedLoginEvents(eventQueryList) : eventMapper.ToLoginEvents(eventQueryList);

        foreach (var e in events)
        {
            await geolocationHelper.AddGeolocationAsync(e);
        }

        return events;
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