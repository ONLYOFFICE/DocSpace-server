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

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Files.Worker.Services;

[Singleton]
public class AutoCleanTrashService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AutoCleanTrashService> logger)
    : ActivePassiveBackgroundService<AutoCleanTrashService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration.GetValue<string>("files:autoCleanUp:period") ?? "0:5:0");

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            List<TenantUserSettings> activeTenantsUsers;

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                await using var userDbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<UserDbContext>>().CreateDbContextAsync(stoppingToken);
                activeTenantsUsers = await Queries.DefaultTenantUserSettingsAsync(userDbContext).ToListAsync(stoppingToken);
            }

            if (activeTenantsUsers.Count == 0)
            {
                return;
            }

            logger.InfoFoundUsers(activeTenantsUsers.Count);

            foreach (var tenantsUser in activeTenantsUsers)
            {
                await DeleteFilesAndFoldersAsync(tenantsUser, stoppingToken);
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async ValueTask DeleteFilesAndFoldersAsync(TenantUserSettings tenantUser, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            tenantManager.SetCurrentTenant(new Tenant(tenantUser.TenantId, string.Empty));

            var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileOperationsManager = scope.ServiceProvider.GetRequiredService<FileDeleteOperationsManager>();
            var fileDateTime = scope.ServiceProvider.GetRequiredService<FileDateTime>();

            var userAccount = await authManager.GetAccountByIDAsync(tenantUser.TenantId, tenantUser.UserId);

            if (Equals(userAccount, Constants.Guest))
            {
                return;
            }

            await securityContext.AuthenticateMeWithoutCookieAsync(userAccount);

            var fileDao = daoFactory.GetFileDao<int>();
            var folderDao = daoFactory.GetFolderDao<int>();
            var now = DateTime.UtcNow;

            var trashId = await folderDao.GetFolderIDTrashAsync(false, tenantUser.UserId);
            if (trashId == 0)
            {
                return;
            }

            var foldersList = await folderDao.GetFoldersAsync(trashId)
                .Where(x => fileDateTime.GetModifiedOnWithAutoCleanUp(x.ModifiedOn, tenantUser.Setting, true) < now)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);

            var filesList = await fileDao.GetFilesAsync(trashId, null, default, false, Guid.Empty, string.Empty, null, false)
                .Where(x => fileDateTime.GetModifiedOnWithAutoCleanUp(x.ModifiedOn, tenantUser.Setting, true) < now)
                .Select(y => y.Id)
                .ToListAsync(cancellationToken);

            if (foldersList.Count == 0 && filesList.Count == 0)
            {
                return;
            }

            logger.InfoCleanUp(tenantUser.TenantId, trashId);

            await fileOperationsManager.Publish(foldersList, filesList, true, true, true);

            logger.InfoCleanUpWait(tenantUser.TenantId, trashId);

            while (true)
            {
                var statuses = await fileOperationsManager.GetOperationResults();

                if (statuses.TrueForAll(r => r.Finished))
                {
                    break;
                }

                await Task.Delay(1000, cancellationToken);
            }

            logger.InfoCleanUpFinish(tenantUser.TenantId, trashId);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }
}

static file class Queries
{
    public static readonly Func<UserDbContext, IAsyncEnumerable<TenantUserSettings>>
        DefaultTenantUserSettingsAsync = EF.CompileAsyncQuery(
            (UserDbContext ctx) =>
                ctx.Users
                   .Join(ctx.Tenants, x => x.TenantId, y => y.Id, (users, tenants) => new { users, tenants })
                   .Where(x => x.tenants.Status == TenantStatus.Active)
                   .Select(r => new TenantUserSettings
                   {
                       TenantId = r.tenants.Id,
                       UserId = r.users.Id,
                       Setting = AutoCleanUpData.GetDefault().Gap
                   }));

}

internal class TenantUserSettings
{
    public int TenantId { get; init; }
    public Guid UserId { get; init; }
    public DateToAutoCleanUp Setting { get; init; }
}