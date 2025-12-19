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


using ASC.Web.Studio.Core.Notify;

using ApiKey = ASC.Core.Common.EF.Model.ApiKey;

namespace ASC.Files.Service.Services;
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