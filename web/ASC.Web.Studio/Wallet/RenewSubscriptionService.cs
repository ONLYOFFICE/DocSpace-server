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

using System.Globalization;

using ASC.Core.Billing;
using ASC.Core.Common.Hosting;
using ASC.Core.Common.Quota.Features;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.MessagingSystem.Core;
using ASC.Web.Core.PublicResources;
using ASC.Web.Core.Quota;

using Microsoft.EntityFrameworkCore;

using ZiggyCreatures.Caching.Fusion;

namespace ASC.Web.Studio.Wallet;

[Singleton]
public class RenewSubscriptionService(
        IServiceScopeFactory scopeFactory,
        ILogger<RenewSubscriptionService> logger,
        IConfiguration configuration,
        IFusionCache hybridCache)
    : ActivePassiveBackgroundService<RenewSubscriptionService>(logger, scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private const string CacheKey = "renewsubscriptionservice_lastrun";

    private Dictionary<int, TenantQuota> _walletQuotas;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Parse(configuration["core:accounting:renewperiod"] ?? "0:1:0", CultureInfo.InvariantCulture);

    // How far before DueDate to renew, so the subscription is extended before it expires (no downtime).
    // Defaults to one tick period (the same key as renewperiod) when not configured.
    private readonly TimeSpan _renewAdvance = TimeSpan.Parse(configuration["core:accounting:renewadvance"] ?? configuration["core:accounting:renewperiod"] ?? "0:1:0", CultureInfo.InvariantCulture);

    // Renewals whose payment change got no answer from the billing service, re-checked on every run.
    private const string PendingCacheKey = "renewsubscriptionservice_pending";

    // Re-checks on the runs after the one that sent the renewal. If billing still does not show the subscription
    // extended after the last of them - whether it answered or not - the renewal is treated as failed.
    private const int MaxRenewVerifyChecks = 3;

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            await using var coreDbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync(stoppingToken);

            if (_walletQuotas == null)
            {
                var quotaService = scope.ServiceProvider.GetRequiredService<IQuotaService>();
                var tenantQuotas = await quotaService.GetTenantQuotasAsync();
                _walletQuotas = tenantQuotas.Where(x => x.Wallet).ToDictionary(x => x.TenantId, x => x);
            }

            await VerifyPendingRenewalsAsync(stoppingToken);

            // Look ahead by _renewAdvance so quotas are renewed before their DueDate passes (no downtime).
            var to = DateTime.UtcNow + _renewAdvance;

            var from = await hybridCache.GetOrDefaultAsync(CacheKey, to, token: stoppingToken);

            var walletQuotasToRenew = await Queries.GetWalletQuotasByDueDateAsync(coreDbContext, _walletQuotas.Keys.ToArray(), from, to).ToListAsync(stoppingToken);

            await hybridCache.SetAsync(CacheKey, to, token: stoppingToken);

            if (walletQuotasToRenew.Count > 0)
            {
                logger.InfoRenewSubscriptionServiceFound(walletQuotasToRenew.Count);

                foreach (var walletQuotaToRenew in walletQuotasToRenew.OrderBy(x => _walletQuotas[x.Quota].Additional))
                {
                    await RenewSubscriptionAsync(walletQuotaToRenew);
                }
            }
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

        // what is being renewed, for the logs and the failure letter; refined to the purchased quota and quantity once they are known
        var renewedQuota = _walletQuotas[data.Quota];
        var renewedQuantity = data.NextQuantity ?? data.Quantity;

        try
        {
            if (data.NextQuantity is <= 0)
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

            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();
            var currentTariff = await tariffService.GetTariffAsync(data.TenantId, refresh: false);
            var walletQuota = _walletQuotas[data.Quota];

            if (currentTariff.State > TariffState.Paid && walletQuota.Additional)
            {
                return;
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            owner = await userManager.GetUsersAsync(tenant.OwnerId);

            var customerInfo = await tariffService.GetCustomerInfoAsync(data.TenantId);
            if (customerInfo == null)
            {
                // The renewal cannot be paid without knowing the payment method, and ExecuteTaskAsync has
                // already advanced the due-date window past this row, so there will be no second attempt.
                // Fail loudly so the owner is notified instead of the subscription lapsing unnoticed.
                throw new BillingException("Customer could not be found");
            }

            if (!string.IsNullOrEmpty(customerInfo.Email))
            {
                payer = await userManager.GetUserByEmailAsync(customerInfo.Email);
            }

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();

            if (payer != null && payer.Id != ASC.Core.Users.Constants.LostUser.Id)
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(data.TenantId, payer.Id);
            }
            else
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(data.TenantId, owner.Id);
            }

            // when a switch to a different quota is scheduled, buy that quota outright instead of renewing the current one
            var targetQuota = walletQuota;
            if (data.NextQuota is { } nextQuotaId && !_walletQuotas.TryGetValue(nextQuotaId, out targetQuota))
            {
                logger.ErrorRenewSubscriptionServiceUnknownNextQuota(data.TenantId, nextQuotaId);
                return;
            }

            // the feature/capacity being checked concerns whatever is actually being purchased (targetQuota),
            // not the quota being replaced - they only happen to match for the DevPack/DocsCloud switch today
            var walletQuotaFeatureName = targetQuota.Additional
                ? targetQuota.Features.Split(':').FirstOrDefault()
                : "manager"; // wallet quota must contains only one feature

            var nextQuantity = data.NextQuantity ?? data.Quantity;

            var currentQuota = await tenantManager.GetCurrentTenantQuotaAsync(refresh: true);

            var feature = currentQuota.TenantQuotaFeatures.FirstOrDefault(f => f.Name == walletQuotaFeatureName);

            if (feature != null)
            {
                if (feature is MaxTotalSizeFeature size)
                {
                    var tenantQuotaSize = size.Value; // size by tariff (quota size * quantity)

                    var maxTotalSizeStatistic = scope.ServiceProvider.GetRequiredService<MaxTotalSizeStatistic>();

                    var usedSize = await maxTotalSizeStatistic.GetValueAsync();

                    var walletQuotaSize = targetQuota.GetFeature<long>(feature.Name).Value; // wallet quota size by database

                    if (walletQuotaSize > 0 && usedSize > tenantQuotaSize + walletQuotaSize * nextQuantity)
                    {
                        var oversize = usedSize - tenantQuotaSize;
                        nextQuantity = (int)((oversize + walletQuotaSize - 1) / walletQuotaSize); // round up
                    }
                }

                if (feature is CountPaidUserFeature && !targetQuota.Additional)
                {
                    var usedCount = (await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupRoomAdmin.ID)).Length;

                    var walletQuotaCount = targetQuota.GetFeature<int>(feature.Name).Value; // wallet quota count by database

                    if (walletQuotaCount > 0 && usedCount > walletQuotaCount * nextQuantity)
                    {
                        nextQuantity = usedCount; // round up
                    }
                }
            }

            renewedQuota = targetQuota;
            renewedQuantity = nextQuantity;

            var description = Describe(renewedQuota, renewedQuantity);

            var productQuantityType = data.NextQuota is null ? ProductQuantityType.Renew : ProductQuantityType.Set;

            var quantity = new Dictionary<string, int>
            {
                { targetQuota.Name, nextQuantity }
            };

            // TODO: support other currencies
            var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();

            var metadata = new Dictionary<string, string> { { BillingClient.MetadataDetails, Resource.AutoRenewal } };

            if (!targetQuota.Additional)
            {
                // The user subscription is paid from the wallet, so make sure the wallet balance
                // covers the renewal cost (priced off the target quota), topping it up for the missing amount if necessary.
                var requiredAmount = targetQuota.Price * nextQuantity;

                var coreSettings = scope.ServiceProvider.GetRequiredService<CoreSettings>();
                var siteName = tenant.GetTenantDomain(coreSettings);

                // A delayed payment method is topped up manually, so the renewal is paid from whatever the
                // wallet already holds; if that is not enough the customer is notified instead.
                var allowTopUp = !customerInfo.IsDelayedPaymentMethod;

                if (!await tariffService.EnsureWalletBalanceAsync(data.TenantId, requiredAmount, defaultCurrency, null, siteName, true, metadata, allowTopUp))
                {
                    throw new BillingException("Insufficient balance");
                }
            }

            bool result;

            try
            {
                // throwIfFailure lets the transport failure through, which is swallowed into "false" otherwise
                result = await tariffService.PaymentChangeAsync(data.TenantId, quantity, productQuantityType, defaultCurrency, false, null, metadata, true);
            }
            catch (BillingTransportException ex)
            {
                // No answer is not a refusal: the change may have been applied with only the response lost. The
                // failure letter asks the customer to renew manually, which could make them pay twice - so check
                // again on the next runs.
                logger.WarningRenewSubscriptionServiceOutcomeUnknown(data.TenantId, description, ex.Message);

                await AddPendingRenewalAsync(new PendingRenewal(
                    data.TenantId,
                    renewedQuota.ProductId,
                    description,
                    renewedQuota.ServiceName,
                    renewedQuantity,
                    data.DueDate.Value,
                    owner.Id,
                    payer != null && payer.Id != ASC.Core.Users.Constants.LostUser.Id ? payer.Id : null,
                    0));

                return;
            }

            if (result)
            {
                await ReportRenewedAsync(scope.ServiceProvider, data.TenantId, description);

                return;
            }
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }

        await SendRenewSubscriptionErrorAsync(data.TenantId, Describe(renewedQuota, renewedQuantity), renewedQuota.ServiceName, renewedQuantity, payer, owner);
    }

    // the quota name and quantity the logs and the audit trail show for a renewal, e.g. "adminwallet 2"
    private static string Describe(TenantQuota quota, int quantity) => $"{quota.Name} {quantity}";

    private async Task ReportRenewedAsync(IServiceProvider serviceProvider, int tenantId, string description)
    {
        await serviceProvider.GetRequiredService<ITariffService>().GetTariffAsync(tenantId, refresh: false);

        var messageService = serviceProvider.GetRequiredService<MessageService>();
        messageService.Send(MessageInitiator.PaymentService, MessageAction.CustomerSubscriptionUpdated, description);

        logger.InfoRenewSubscriptionServiceDone(tenantId, description);
    }

    private async Task VerifyPendingRenewalsAsync(CancellationToken stoppingToken)
    {
        try
        {
            var pending = await hybridCache.GetOrDefaultAsync<List<PendingRenewal>>(PendingCacheKey, token: stoppingToken);
            if (pending is not { Count: > 0 })
            {
                return;
            }

            var unresolved = new List<PendingRenewal>();

            foreach (var renewal in pending)
            {
                var next = await VerifyPendingRenewalAsync(renewal);
                if (next != null)
                {
                    unresolved.Add(next);
                }
            }

            await SavePendingRenewalsAsync(unresolved);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    // Returns the renewal to check again on the next run, or null once it is settled.
    private async Task<PendingRenewal> VerifyPendingRenewalAsync(PendingRenewal renewal)
    {
        var description = renewal.Description;
        var check = renewal.Checks + 1;

        await using var scope = _scopeFactory.CreateAsyncScope();

        var extended = false;
        Exception error = null;

        try
        {
            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();
            extended = await tariffService.IsSubscriptionExtendedAsync(renewal.TenantId, renewal.ProductId, renewal.PreviousDueDate);
        }
        catch (Exception ex)
        {
            error = ex;
        }

        if (!extended)
        {
            if (check < MaxRenewVerifyChecks)
            {
                logger.WarningRenewSubscriptionServiceNotConfirmed(renewal.TenantId, description, check, error?.Message ?? "not extended yet");

                return renewal with { Checks = check };
            }

            if (error != null)
            {
                logger.ErrorWithException(error);
            }

            await SendRenewSubscriptionErrorAsync(renewal);

            return null;
        }

        try
        {
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(renewal.TenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(renewal.TenantId, renewal.PayerId ?? renewal.OwnerId);

            await ReportRenewedAsync(scope.ServiceProvider, renewal.TenantId, description);
        }
        catch (Exception ex)
        {
            // the renewal is applied, only its audit entry is missing - nothing to retry or to warn the customer about
            logger.ErrorWithException(ex);
        }

        return null;
    }

    private async Task AddPendingRenewalAsync(PendingRenewal renewal)
    {
        var pending = await hybridCache.GetOrDefaultAsync<List<PendingRenewal>>(PendingCacheKey) ?? [];

        pending.Add(renewal);

        await SavePendingRenewalsAsync(pending);
    }

    private async Task SavePendingRenewalsAsync(List<PendingRenewal> pending)
    {
        if (pending.Count == 0)
        {
            await hybridCache.RemoveAsync(PendingCacheKey);

            return;
        }

        // far longer than the few runs the checks take, so the entries survive a restart of the service
        await hybridCache.SetAsync(PendingCacheKey, pending, TimeSpan.FromDays(1));
    }

    private async Task SendRenewSubscriptionErrorAsync(PendingRenewal renewal)
    {
        UserInfo payer = null;
        UserInfo owner = null;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(renewal.TenantId);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            owner = await userManager.GetUsersAsync(renewal.OwnerId);

            if (renewal.PayerId is { } payerId)
            {
                payer = await userManager.GetUsersAsync(payerId);
            }
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }

        await SendRenewSubscriptionErrorAsync(renewal.TenantId, renewal.Description, renewal.ServiceName, renewal.Quantity, payer, owner);
    }

    private async Task SendRenewSubscriptionErrorAsync(int tenantId, string description, string serviceName, int quantity, UserInfo payer, UserInfo owner)
    {
        try
        {
            logger.ErrorRenewSubscriptionServiceFail(tenantId, description);

            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(tenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(tenantId, owner.Id);

            var studioNotifyService = scope.ServiceProvider.GetRequiredService<StudioNotifyService>();
            // the letter names the service the way the customer operations report does, keyed by the billing service name
            await studioNotifyService.SendRenewSubscriptionErrorAsync(payer, owner, serviceName, quantity);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }
    }

    /// <summary>
    /// A renewal the billing service did not answer about. Renewed as soon as billing reports the subscription to
    /// <see cref="ProductId"/> as ending after <see cref="PreviousDueDate"/>; failed when that has not happened
    /// after <see cref="MaxRenewVerifyChecks"/> checks (<see cref="Checks"/> counts the ones already made).
    /// Self-contained on purpose: it is settled without looking the wallet quota up again, so it cannot be lost
    /// to a quota that is gone by then. <see cref="Description"/> is the log and audit text ("adminwallet 2"),
    /// <see cref="ServiceName"/> and <see cref="Quantity"/> fill the failure letter.
    /// </summary>
    private sealed record PendingRenewal(
        int TenantId,
        string ProductId,
        string Description,
        string ServiceName,
        int Quantity,
        DateTime PreviousDueDate,
        Guid OwnerId,
        Guid? PayerId,
        int Checks);
}

static file class Queries
{
    public static readonly Func<CoreDbContext, int[], DateTime, DateTime, IAsyncEnumerable<DbTariffRow>>
        GetWalletQuotasByDueDateAsync = EF.CompileAsyncQuery(
            (CoreDbContext ctx, int[] quotas, DateTime from, DateTime to) =>
                ctx.TariffRows
                    .Join(
                        ctx.Tenants.Where(t => t.Status == TenantStatus.Active),
                        tariffRow => tariffRow.TenantId,
                        tenant => tenant.Id,
                        (tariffRow, tenant) => tariffRow
                    )
                    .GroupBy(tariffRow => tariffRow.TenantId)
                    .Select(group => new
                    {
                        TenantId = group.Key,
                        MaxTariffId = group.Max(tariffRow => tariffRow.TariffId)
                    })
                    .Join(
                        ctx.TariffRows,
                        x => new { x.TenantId, x.MaxTariffId },
                        tariffRow => new { tariffRow.TenantId, MaxTariffId = tariffRow.TariffId },
                        (x, tariffRow) => tariffRow
                    )
                    .Where(r => quotas.Contains(r.Quota) && r.DueDate.HasValue && r.DueDate > from && r.DueDate < to)
            );
}
