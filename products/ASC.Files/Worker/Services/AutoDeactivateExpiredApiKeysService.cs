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

using ASC.Web.Studio.Core.Notify;

using ApiKey = ASC.Core.Common.EF.Model.ApiKey;

namespace ASC.Files.Worker.Services;
[Singleton]
public class AutoDeactivateExpiredApiKeysService(
    IServiceScopeFactory scopeFactory,
    ILogger<AutoDeactivateExpiredApiKeysService> logger)
    : ActivePassiveBackgroundService<AutoDeactivateExpiredApiKeysService>(logger, scopeFactory)

{
    private readonly IServiceScopeFactory _serviceScopeFactory = scopeFactory;
    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            await using var dbContext = await scope.ServiceProvider.GetService<IDbContextFactory<ApiKeysDbContext>>().CreateDbContextAsync(stoppingToken);

            var expiredApiKeys = await Queries.GetExpiredApiKeys(dbContext).ToListAsync(stoppingToken);

            if (expiredApiKeys.Count == 0)
            {
                return;
            }

            await Queries.DeactiveExpiredApiKeys(dbContext, expiredApiKeys.Select(x => x.Id));

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var studioNotifyService = scope.ServiceProvider.GetRequiredService<StudioNotifyService>();

            foreach (var apiKey in expiredApiKeys)
            {
                try
                {
                    var tenant = await tenantManager.SetCurrentTenantAsync(apiKey.TenantId);
                    var user = await userManager.GetUsersAsync(apiKey.CreateBy);

                    if (user.Status != ASC.Core.Users.EmployeeStatus.Active ||
                        user.ActivationStatus != ASC.Core.Users.EmployeeActivationStatus.Activated)
                    {
                        continue;
                    }

                    await securityContext.AuthenticateMeWithoutCookieAsync(tenant.Id, user.Id);
                    await studioNotifyService.SendApiKeyExpiredAsync(user, apiKey.Name);
                }
                catch (Exception ex)
                {
                    logger.ErrorWithException(ex);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

static file class Queries
{
    public static readonly Func<ApiKeysDbContext, IAsyncEnumerable<ApiKey>> GetExpiredApiKeys =
        EF.CompileAsyncQuery(
            (ApiKeysDbContext ctx) =>
                ctx.DbApiKey.Where(r => r.IsActive && r.ExpiresAt != null && r.ExpiresAt.Value < DateTime.UtcNow)
                    .AsQueryable()
        );

    public static readonly Func<ApiKeysDbContext, IEnumerable<Guid>, Task<int>> DeactiveExpiredApiKeys =
        EF.CompileAsyncQuery(
            (ApiKeysDbContext ctx, IEnumerable<Guid> ids) =>
                ctx.DbApiKey.Where(r => ids.Contains(r.Id))
                    .ExecuteUpdate(x => x.SetProperty(r => r.IsActive, false))
                );


}