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

namespace ASC.ClearEvents.Services;

[Scope]
public class ClearEventsService(ILogger<ClearEventsService> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromDays(1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {        
        logger.InformationTimerRunnig();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RemoveOldEventsAsync(r => r.LoginEvents, nameof(TenantAuditSettings.LoginHistoryLifeTime), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.ErrorWithException(ex);
            }

            await _timer.WaitForNextTickAsync(stoppingToken);
        }
        logger.InformationTimerStopping();
    }
    

    private async Task RemoveOldEventsAsync<T>(Expression<Func<MessagesContext, DbSet<T>>> func, string settings, CancellationToken stoppingToken) where T : MessageEvent
    {
        List<T> ids;
        var compile = func.Compile();
        do
        {
            using var scope = serviceScopeFactory.CreateScope();
            await using var ef = await scope.ServiceProvider.GetService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);
            var table = compile.Invoke(ef);

            var ae = table
                .Join(ef.Tenants, r => r.TenantId, r => r.Id, (audit, tenant) => audit)
                .Select(r => new
                {
                    r.Id,
                    r.Date,
                    r.TenantId,
                    ef = r
                })
                .Where(r => r.Date < DateTime.UtcNow.AddDays(-Convert.ToDouble(
                    ef.WebstudioSettings
                    .Where(a => a.TenantId == r.TenantId && a.Id == TenantAuditSettings.ID)
                    .Select(dbWebstudioSettings => DbFunctionsExtension.JsonExtract(nameof(dbWebstudioSettings.Data).ToLower(), settings))
                    .FirstOrDefault() ?? TenantAuditSettings.MaxLifeTime.ToString())))
                .Take(1000);

            ids = await ae.Select(r => r.ef).ToListAsync(cancellationToken: stoppingToken);

            if (ids.Count == 0)
            {
                return;
            }

            table.RemoveRange(ids);
            await ef.SaveChangesAsync(stoppingToken);

        } while (ids.Count != 0);
    }
}