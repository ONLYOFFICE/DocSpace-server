// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Files.AutoCleanUp;

[Singleton]
public class Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory)
{
    public async Task DeleteExpiredFilesInTrash(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        List<TenantUserSettings> activeTenantsUsers;

        await using (var scope = serviceScopeFactory.CreateAsyncScope())
        {
            await using var dbContext = scope.ServiceProvider.GetRequiredService<IDbContextFactory<WebstudioDbContext>>().CreateDbContext();
            activeTenantsUsers = await GetTenantsUsersAsync(dbContext);
        }

        if (!activeTenantsUsers.Any())
        {
            return;
        }

        logger.InfoFoundUsers(activeTenantsUsers.Count);

        await Parallel.ForEachAsync(activeTenantsUsers,
                                    new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = cancellationToken }, //System.Environment.ProcessorCount
                                    DeleteFilesAndFoldersAsync);
    }

    private async ValueTask DeleteFilesAndFoldersAsync(TenantUserSettings tenantUser, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(tenantUser.TenantId);

            var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileStorageService = scope.ServiceProvider.GetRequiredService<FileStorageService>();
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

            await fileStorageService.DeleteItemsAsync("delete", filesList, foldersList, true, false, true);

            logger.InfoCleanUpWait(tenantUser.TenantId, trashId);

            while (true)
            {
                var statuses = fileStorageService.GetTasksStatuses();

                if (statuses.TrueForAll(r => r.Finished))
                {
                    break;
                }

                await Task.Delay(100, cancellationToken);
            }

            logger.InfoCleanUpFinish(tenantUser.TenantId, trashId);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    private async Task<List<TenantUserSettings>> GetTenantsUsersAsync(WebstudioDbContext dbContext)
    {
        var filesSettingsId = new FilesSettings().ID;
        return await Queries.TenantUserSettingsAsync(dbContext, filesSettingsId).ToListAsync();
    }
}

static file class Queries
{
    public static readonly Func<WebstudioDbContext, Guid, IAsyncEnumerable<TenantUserSettings>>
        TenantUserSettingsAsync = EF.CompileAsyncQuery(
            (WebstudioDbContext ctx, Guid filesSettingsId) =>
                ctx.Tenants
                    .Join(ctx.WebstudioSettings, a => a.Id, b => b.TenantId,
                        (tenants, settings) => new { tenants, settings })
                    .Where(x => x.tenants.Status == TenantStatus.Active &&
                                x.settings.Id == filesSettingsId &&
                                Convert.ToBoolean(DbFunctionsExtension.JsonValue(nameof(x.settings.Data).ToLower(),
                                    "AutomaticallyCleanUp.IsAutoCleanUp")))
                    .Select(r => new TenantUserSettings
                    {
                        TenantId = r.tenants.Id,
                        UserId = r.settings.UserId,
                        Setting = (DateToAutoCleanUp)Convert.ToInt32(
                            DbFunctionsExtension.JsonValue(nameof(r.settings.Data).ToLower(), "AutomaticallyCleanUp.Gap"))
                    }));
}