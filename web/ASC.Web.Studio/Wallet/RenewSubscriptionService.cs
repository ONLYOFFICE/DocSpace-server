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

using System.Globalization;

using ASC.Core;
using ASC.Core.Billing;
using ASC.Core.Common.Hosting;
using ASC.Core.Common.Quota.Features;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.MessagingSystem.Core;
using ASC.Web.Core.Quota;
using ASC.Web.Studio.Core.Notify;

using Microsoft.EntityFrameworkCore;

using ZiggyCreatures.Caching.Fusion;

namespace ASC.Web.Studio.Wallet;

[Singleton]
public class RenewSubscriptionService(
        IServiceScopeFactory scopeFactory,
        ILogger<RenewSubscriptionService> logger,
        IConfiguration configuration,
        IFusionCache hybridCache,
        NotifyConfiguration notifyConfiguration)
    : ActivePassiveBackgroundService<RenewSubscriptionService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private bool _configured;

    private const string CacheKey = "renewsubscriptionservice_lastrun";

    private Dictionary<int, TenantQuota> _walletQuotas;
    private List<DbTariffRow> _closeToExpirationWalletQuotas;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration["core:accounting:renewperiod"] ?? "0:1:0", CultureInfo.InvariantCulture);


    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_configured)
            {
                notifyConfiguration.Configure();
                _configured = true;
            }

            if (_closeToExpirationWalletQuotas != null && _closeToExpirationWalletQuotas.Count > 0)
            {
                var expiredWalletQuotas = _closeToExpirationWalletQuotas.Where(x => x.DueDate < DateTime.UtcNow).ToList();

                foreach (var expiredWalletQuota in expiredWalletQuotas)
                {
                    await RenewSubscriptionAsync(expiredWalletQuota);
                }
            }

            var now = DateTime.UtcNow;
            var from = await hybridCache.GetOrDefaultAsync(CacheKey, now, token: stoppingToken);
            var to = now.Add(ExecuteTaskPeriod * 3);

            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                await using var coreDbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync(stoppingToken);

                if (_walletQuotas == null)
                {
                    var quotaService = scope.ServiceProvider.GetRequiredService<IQuotaService>();
                    var tenantQuotas = await quotaService.GetTenantQuotasAsync();
                    _walletQuotas = tenantQuotas.Where(x => x.Wallet).ToDictionary(x => x.TenantId, x => x);
                }

                _closeToExpirationWalletQuotas = await Queries.GetWalletQuotasCloseToExpirationAsync(coreDbContext, _walletQuotas.Keys.ToArray(), from, to).ToListAsync(stoppingToken);
            }

            if (_closeToExpirationWalletQuotas.Count > 0)
            {
                logger.InfoRenewSubscriptionServiceFound(_closeToExpirationWalletQuotas.Count);
            }

            await hybridCache.SetAsync(CacheKey, now, token: stoppingToken);
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async ValueTask RenewSubscriptionAsync(DbTariffRow data)
    {
        UserInfo payer = null;
        UserInfo owner = null;

        try
        {
            if (data.NextQuantity.HasValue && data.NextQuantity.Value <= 0)
            {
                return;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(data.TenantId);

            if (tenant.Status != TenantStatus.Active)
            {
                return;
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            owner = await userManager.GetUsersAsync(tenant.OwnerId);

            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();

            var payerEmail = (await tariffService.GetCustomerInfoAsync(data.TenantId)).Email;
            if (!string.IsNullOrEmpty(payerEmail))
            {
                payer = await userManager.GetUserByEmailAsync(payerEmail);
            }

            if (payer != null && payer.Id != ASC.Core.Users.Constants.LostUser.Id)
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(data.TenantId, payer.Id);
            }
            else
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(data.TenantId, owner.Id);
            }

            var walletQuota = _walletQuotas[data.Quota];

            var walletQuotaFeatureName = walletQuota.Features.Split(':').FirstOrDefault(); // wallet quota must contains only one feature

            var nextQuantity = data.NextQuantity.HasValue ?  data.NextQuantity.Value : data.Quantity;

            var currentQuota = await tenantManager.GetCurrentTenantQuotaAsync(refresh: true);

            foreach (var feature in currentQuota.TenantQuotaFeatures)
            {
                if (feature.Name == walletQuotaFeatureName)
                {
                    if (feature is MaxTotalSizeFeature size)
                    {
                        var tenantQuotaSize = size.Value; // size by tariff (quota size * quantity)

                        var maxTotalSizeStatistic = scope.ServiceProvider.GetRequiredService<MaxTotalSizeStatistic>();

                        var usedSize = await maxTotalSizeStatistic.GetValueAsync();

                        var walletQuotaSize = walletQuota.GetFeature<long>(feature.Name).Value; // wallet quota size by database

                        if (walletQuotaSize > 0 && usedSize > tenantQuotaSize + (walletQuotaSize * nextQuantity))
                        {
                            var oversize = usedSize - tenantQuotaSize;
                            nextQuantity = (int)((oversize + walletQuotaSize - 1) / walletQuotaSize); // round up
                        }
                    }

                    break; //TODO: add nextQuantity calculations for another wallet quotas (for example admins count)
                }
            }

            var quantity = new Dictionary<string, int>
            {
                { walletQuota.Name, nextQuantity }
            };

            // TODO: support other currencies
            var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();

            var result = await tariffService.PaymentChangeAsync(data.TenantId, quantity, ProductQuantityType.Renew, defaultCurrency);

            if (result)
            {
                var newTariff = await tariffService.GetTariffAsync(data.TenantId, refresh: false);

                var description = $"{walletQuota.Name} {nextQuantity}";
                var messageService = scope.ServiceProvider.GetRequiredService<MessageService>();
                messageService.Send(MessageInitiator.PaymentService, MessageAction.CustomerSubscriptionUpdated, description);

                logger.InfoRenewSubscriptionServiceDone(data.TenantId, description);

                return;
            }
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }

        await SendRenewSubscriptionErrorAsync(data.TenantId, payer, owner);
    }

    private async Task SendRenewSubscriptionErrorAsync(int tenantId, UserInfo payer, UserInfo owner)
    {
        try
        {
            logger.ErrorRenewSubscriptionServiceFail(tenantId);

            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(tenantId);

            var studioNotifyService = scope.ServiceProvider.GetRequiredService<StudioNotifyService>();
            await studioNotifyService.SendRenewSubscriptionErrorAsync(payer, owner);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }
}

static file class Queries
{
    public static readonly Func<CoreDbContext, int[], DateTime, DateTime, IAsyncEnumerable<DbTariffRow>>
        GetWalletQuotasCloseToExpirationAsync = EF.CompileAsyncQuery(
            (CoreDbContext ctx, int[] quotas, DateTime from, DateTime to) =>
                ctx.TariffRows
                    .Join(
                        ctx.Tenants.Where(t => t.Status == TenantStatus.Active),
                        tariffRow => tariffRow.TenantId,
                        tenant => tenant.Id,
                        (tariffRow, tenant) => tariffRow
                    )
                    .GroupBy(tariffRow => tariffRow.TenantId)
                    .Select(group => new {
                        TenantId = group.Key,
                        MaxTariffId = group.Max(tariffRow => tariffRow.TariffId)
                    })
                    .Join(
                        ctx.TariffRows,
                        x => new { TenantId = x.TenantId, MaxTariffId = x.MaxTariffId },
                        tariffRow => new { TenantId = tariffRow.TenantId, MaxTariffId = tariffRow.TariffId },
                        (x, tariffRow) => tariffRow
                    )
                    .Where(r => quotas.Contains(r.Quota) && r.DueDate.HasValue && r.DueDate > from && r.DueDate < to)
            );
}