// (c) Copyright Ascensio System SIA 2009-2025
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

using ASC.Common.Security.Authorizing;
using ASC.Core.Users;
using ASC.Web.Files.Services.WCFService;

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Files.Service.Services;

[Singleton]
public class AutoDeletePersonalFolderService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AutoDeletePersonalFolderService> logger)
    : ActivePassiveBackgroundService<AutoDeletePersonalFolderService>(logger, scopeFactory)
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

            await Parallel.ForEachAsync(activeTenantsUsers,
                new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = stoppingToken }, //System.Environment.ProcessorCount
                DeleteFilesAndFoldersAsync);
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
            await tenantManager.SetCurrentTenantAsync(tenantUser.TenantId);

            var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileDateTime = scope.ServiceProvider.GetRequiredService<FileDateTime>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            var fileStorageService = scope.ServiceProvider.GetRequiredService<FileStorageService>();

            var userAccount = await authManager.GetAccountByIDAsync(tenantUser.TenantId, tenantUser.UserId);

            if (Equals(userAccount, Constants.Guest))
            {
                return;
            }

            if (!(await userManager.IsGuestAsync(tenantUser.UserId)))
            {
                return;
            }

            await securityContext.AuthenticateMeWithoutCookieAsync(userAccount);

            var folderDao = daoFactory.GetFolderDao<int>();
            var now = DateTime.UtcNow;

            var myId = await folderDao.GetFolderIDUserAsync(false, tenantUser.UserId);
            if (myId == 0)
            {
                return;
            }
            var my = await folderDao.GetFolderAsync(myId);

            if (fileDateTime.GetModifiedOnWithAutoCleanUp(my.ModifiedOn, DateToAutoCleanUp.ThirtyDays, true) > now)
            {
                return;
            }

            logger.InfoCleanUp(myId);

            var userTo = my.ModifiedBy;

            if (await userManager.IsGuestAsync(userTo))
            {
                userTo = tenantManager.GetCurrentTenant().OwnerId;
            }

            await fileStorageService.MoveSharedFilesAsync(tenantUser.UserId, userTo);
            await fileStorageService.DeletePersonalFolderAsync(tenantUser.UserId);

            logger.InfoCleanUpFinish(myId);
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
                   .Join(ctx.UserGroups, x => x.users.Id, y => y.Userid, (x, y) => new { users = x.users, tenants = x.tenants, userGroups = y})
                   .Where(x => x.tenants.Status == TenantStatus.Active)
                   .Where(x => x.userGroups.UserGroupId == AuthConstants.Guest.ID && !x.userGroups.Removed)
                   .Select(r => new TenantUserSettings
                   {
                       TenantId = r.tenants.Id,
                       UserId = r.users.Id,
                       Setting = AutoCleanUpData.GetDefault().Gap
                   }));

}

