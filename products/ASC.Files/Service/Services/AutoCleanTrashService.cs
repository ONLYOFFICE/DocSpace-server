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

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Files.Service.Services;

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

        await Parallel.ForEachAsync(activeTenantsUsers,
                                    new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = stoppingToken }, //System.Environment.ProcessorCount
                                    DeleteFilesAndFoldersAsync);
    }

    private async ValueTask DeleteFilesAndFoldersAsync(TenantUserSettings tenantUser, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(tenantUser.TenantId);

            var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileOperationsManager = scope.ServiceProvider.GetRequiredService<FileOperationsManager>();
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
            if(trashId == 0)
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

            await fileOperationsManager.PublishDelete(foldersList, filesList, true, true, true);

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

class TenantUserSettings
{
    public int TenantId { get; init; }
    public Guid UserId { get; init; }
    public DateToAutoCleanUp Setting { get; init; }
}
