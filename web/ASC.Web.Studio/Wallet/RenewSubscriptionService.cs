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

namespace ASC.Web.Studio.Wallet;

[Singleton]
public class RenewSubscriptionService(
        IServiceScopeFactory scopeFactory,
        ILogger<RenewSubscriptionService> logger,
        IConfiguration configuration,
        IFusionCache hybridCache,
        IDistributedLockProvider distributedLockProvider)
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

    // How long after the timeout billing's answer "not extended" is still not taken as final: billing can finish
    // a request it stopped answering minutes later, and a premature "renew manually" can make the customer pay
    // twice. Fixed times rather than a number of runs, which would shrink with renewperiod.
    private static readonly TimeSpan _renewNotExtendedGrace = TimeSpan.FromMinutes(10);

    // How long after the timeout a renewal is re-checked while billing gives no answer at all, before the
    // customer is told it failed.
    private static readonly TimeSpan _renewVerifyPeriod = TimeSpan.FromMinutes(30);

    // The renewals taken on and not finished yet. Every renewal clears its own entry however it ends, so an entry
    // that outlives the run that wrote it was left by a process that stopped half-way, and the next runs take it up.
    private const string JournalCacheKey = "renewsubscriptionservice_journal";

    // The journal is one value, read and written back as a whole: every change of it is made under this lock, so that
    // two instances - the active one and one still finishing its run after the active one changed - cannot overwrite
    // each other's change or start the same renewal.
    private const string JournalLockKey = "renewsubscriptionservice_journal_lock";

    // How many times a renewal is started before the customer is told it failed: a renewal that stops a process on
    // every attempt must not be retried for ever.
    private const int MaxRenewalAttempts = 3;

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

            var now = DateTime.UtcNow;

            // Look ahead by _renewAdvance so quotas are renewed before their DueDate passes (no downtime).
            var to = now + _renewAdvance;

            var from = await hybridCache.GetOrDefaultAsync(CacheKey, to, token: stoppingToken);

            var walletQuotasToRenew = await Queries.GetWalletQuotasByDueDateAsync(coreDbContext, _walletQuotas.Keys.ToArray(), from, to).ToListAsync(stoppingToken);

            List<RenewalJournalEntry> interrupted;
            List<RenewalJournalEntry> queued;

            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey, cancellationToken: stoppingToken))
            {
                var journal = await LoadJournalAsync();

                interrupted = await GetInterruptedRenewalsAsync(journal, now);

                queued = walletQuotasToRenew
                    .Select(row => new RenewalJournalEntry(row.TenantId, row.Quota, row.Quantity, row.NextQuota, row.NextQuantity, row.DueDate.Value, RenewalStage.Queued, now, 0))
                    .Where(entry => !journal.Exists(entry.IsSameRenewal))
                    .ToList();

                if (queued.Count > 0)
                {
                    journal.AddRange(queued);
                    await SaveJournalAsync(journal);
                }
            }

            // Only once the rows are in the journal: a window moved first would lose them for good to a process
            // that stops in between, as no later window covers their due dates again.
            await hybridCache.SetAsync(CacheKey, to, token: stoppingToken);

            var renewals = interrupted.Select(entry => (Entry: entry, Interrupted: true))
                .Concat(queued.Select(entry => (Entry: entry, Interrupted: false)))
                .OrderBy(x => _walletQuotas[x.Entry.Quota].Additional)
                .ToList();

            if (renewals.Count > 0)
            {
                logger.InfoRenewSubscriptionServiceFound(renewals.Count);

                foreach (var (entry, wasInterrupted) in renewals)
                {
                    // A stopping process starts no new renewal: what is left stays queued in the journal for the
                    // next process, rather than being cut off half-way when the host stops waiting.
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    await RenewSubscriptionAsync(entry, wasInterrupted);
                }
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async ValueTask RenewSubscriptionAsync(RenewalJournalEntry data, bool interrupted)
    {
        // The claim comes before anything else, the checks that can end the renewal with a failure letter included:
        // an entry that is gone or changed since this run read it belongs to another instance, which alone settles
        // it and tells the customer how it ended.
        var claimed = await TryClaimInJournalAsync(data);
        if (claimed == null)
        {
            logger.WarningRenewSubscriptionServiceNotClaimed(data.TenantId, Describe(_walletQuotas[data.Quota], data.NextQuantity ?? data.Quantity));

            return;
        }

        var waitsForBilling = false;

        try
        {
            waitsForBilling = await TryRenewSubscriptionAsync(data, interrupted);
        }
        finally
        {
            // However the renewal ended in this process, it is over: only a process that stops half-way leaves its
            // claim behind. An interrupted one billing could not tell anything about goes back as it was read, with
            // the time and the attempts its limits are counted from.
            await ReleaseInJournalAsync(claimed, waitsForBilling ? data : null);
        }
    }

    // Returns true when the renewal has to wait in the journal for the next run: it was interrupted earlier and billing
    // gave no answer about it yet.
    private async Task<bool> TryRenewSubscriptionAsync(RenewalJournalEntry data, bool interrupted)
    {
        UserInfo payer = null;
        UserInfo owner = null;

        // what is being renewed, for the logs and the failure letter; refined to the purchased quota and quantity once they are known
        var renewedQuota = _walletQuotas[data.Quota];
        var renewedQuantity = data.NextQuantity ?? data.Quantity;

        // Only a renewal that got as far as the money steps has an outcome to ask billing about; one left queued has
        // paid nothing and is simply made now.
        var resumed = interrupted && data.Stage == RenewalStage.Started;

        if (resumed)
        {
            // on record before anything below can end the renewal
            logger.ErrorRenewSubscriptionServiceInterrupted(data.TenantId, Describe(renewedQuota, renewedQuantity), data.Stage.ToString(), data.Attempt);
        }
        else if (interrupted)
        {
            logger.WarningRenewSubscriptionServiceLeftQueued(data.TenantId, Describe(renewedQuota, renewedQuantity));
        }

        try
        {
            if (data.NextQuantity is <= 0)
            {
                return false;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(data.TenantId);

            if (tenant.Status != TenantStatus.Active)
            {
                return false;
            }

            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();
            var currentTariff = await tariffService.GetTariffAsync(data.TenantId, refresh: false);
            var walletQuota = _walletQuotas[data.Quota];

            if (currentTariff.State > TariffState.Paid && walletQuota.Additional)
            {
                return false;
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            owner = await userManager.GetUsersAsync(tenant.OwnerId);

            // when a switch to a different quota is scheduled, buy that quota outright instead of renewing the current one
            var targetQuota = walletQuota;
            if (data.NextQuota is { } nextQuotaId && !_walletQuotas.TryGetValue(nextQuotaId, out targetQuota))
            {
                logger.ErrorRenewSubscriptionServiceUnknownNextQuota(data.TenantId, nextQuotaId);
                return false;
            }

            // null both for a customer billing does not know and for a request that failed
            var customerInfo = await tariffService.GetCustomerInfoAsync(data.TenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();

            if (resumed)
            {
                // The stopped process may have got as far as the payment change, with billing applying it after the
                // process was gone - renewing again, or telling the customer to renew manually, would make them pay
                // twice. So billing is asked first, before anything else can end the renewal.
                bool extended;

                try
                {
                    extended = await tariffService.IsSubscriptionExtendedAsync(data.TenantId, targetQuota.ProductId, data.DueDate);
                }
                catch (Exception ex) when (CanWaitForBilling(data))
                {
                    // no answer is no "not extended" either: ask again on the next run
                    logger.ErrorRenewSubscriptionServiceTenant(data.TenantId, ex);

                    return true;
                }
                catch (Exception ex)
                {
                    throw new BillingException("Billing gave no answer about the interrupted renewal", ex);
                }

                if (extended)
                {
                    try
                    {
                        if (customerInfo != null)
                        {
                            payer = await userManager.GetActivePayerAsync(customerInfo);
                        }

                        await securityContext.AuthenticateMeWithoutCookieAsync(data.TenantId, (payer ?? owner).Id);

                        await ReportRenewedAsync(scope.ServiceProvider, data.TenantId, Describe(targetQuota, renewedQuantity));
                    }
                    catch (Exception ex)
                    {
                        // the renewal is applied, only its audit entry is missing - nothing to warn the customer about
                        logger.ErrorRenewSubscriptionServiceTenant(data.TenantId, ex);
                    }

                    return false;
                }

                if (data.Attempt >= MaxRenewalAttempts)
                {
                    throw new BillingException($"The renewal was interrupted on each of its {data.Attempt} attempts");
                }

                if (customerInfo == null && CanWaitForBilling(data))
                {
                    // billing answered about the subscription but not about the customer: the renewal is made once
                    // it does, rather than reported as failed over a request that did not get through
                    return true;
                }
            }

            if (customerInfo == null)
            {
                // The renewal cannot be paid without knowing the payment method. Fail loudly so the owner is
                // notified instead of the subscription lapsing unnoticed.
                throw new BillingException("Customer could not be found");
            }

            payer = await userManager.GetActivePayerAsync(customerInfo);

            await securityContext.AuthenticateMeWithoutCookieAsync(data.TenantId, (payer ?? owner).Id);

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

            // From here on money moves (a top-up of the wallet, then the payment change), so a process that stops
            // past this point leaves a renewal whose outcome only billing can tell - which is what the entry, marked
            // as started when it was claimed, has the next runs ask.

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
            catch (Exception ex) when (ex is BillingTransportException or PaymentServiceContractException)
            {
                // No answer, or one that cannot be read, is not a refusal: the change may have been applied with only
                // the response lost. The failure letter asks the customer to renew manually, which could make them pay
                // twice - so check again on the next runs.
                // The exception, not only its message: the message names the call, the cause - a timeout, DNS, TLS, an
                // unreadable body - is in the inner exception.
                logger.ErrorRenewSubscriptionServiceOutcomeUnknown(data.TenantId, description, ex);

                await AddPendingRenewalAsync(new PendingRenewal(
                    data.TenantId,
                    renewedQuota.ProductId,
                    description,
                    renewedQuota.ServiceName,
                    renewedQuantity,
                    data.DueDate,
                    owner.Id,
                    payer?.Id,
                    DateTime.UtcNow));

                return false;
            }

            if (result)
            {
                await ReportRenewedAsync(scope.ServiceProvider, data.TenantId, description);

                return false;
            }
        }
        catch (Exception ex)
        {
            logger.ErrorRenewSubscriptionServiceTenant(data.TenantId, ex);
        }

        await SendRenewSubscriptionErrorAsync(data.TenantId, Describe(renewedQuota, renewedQuantity), renewedQuota.ServiceName, renewedQuantity, payer, owner);

        return false;
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

            // Once billing has failed to answer in this run, every further check would only wait out the same
            // client timeout and get the same answer, delaying the regular renewals behind them - so the rest
            // take that answer without asking.
            BillingTransportException billingDown = null;

            foreach (var renewal in pending)
            {
                var (extended, error) = billingDown is null
                    ? await CheckExtendedAsync(renewal)
                    : (false, billingDown);

                billingDown ??= error as BillingTransportException;

                var next = await SettlePendingRenewalAsync(renewal, extended, error);
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

    private async Task<(bool Extended, Exception Error)> CheckExtendedAsync(PendingRenewal renewal)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();

            return (await tariffService.IsSubscriptionExtendedAsync(renewal.TenantId, renewal.ProductId, renewal.PreviousDueDate), null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    // Returns the renewal to check again on the next run, or null once it is settled.
    private async Task<PendingRenewal> SettlePendingRenewalAsync(PendingRenewal renewal, bool extended, Exception error)
    {
        var description = renewal.Description;

        if (!extended)
        {
            // "not extended" is final once the grace period is over; no answer at all waits the full period
            var waitFor = error is null ? _renewNotExtendedGrace : _renewVerifyPeriod;

            if (DateTime.UtcNow < renewal.TimedOutAt + waitFor)
            {
                logger.WarningRenewSubscriptionServiceNotConfirmed(renewal.TenantId, description, renewal.TimedOutAt, error?.Message ?? "not extended yet", error);

                return renewal;
            }

            if (error != null)
            {
                logger.ErrorRenewSubscriptionServiceTenant(renewal.TenantId, error);
            }

            await SendRenewSubscriptionErrorAsync(renewal);

            return null;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(renewal.TenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(renewal.TenantId, renewal.PayerId ?? renewal.OwnerId);

            await ReportRenewedAsync(scope.ServiceProvider, renewal.TenantId, description);
        }
        catch (Exception ex)
        {
            // the renewal is applied, only its audit entry is missing - nothing to retry or to warn the customer about
            logger.ErrorRenewSubscriptionServiceTenant(renewal.TenantId, ex);
        }

        return null;
    }

    private async Task AddPendingRenewalAsync(PendingRenewal renewal)
    {
        // a copy: the memory level of the cache hands out the stored list itself, which must not change under a reader
        List<PendingRenewal> pending = [.. await hybridCache.GetOrDefaultAsync<List<PendingRenewal>>(PendingCacheKey) ?? [], renewal];

        await SavePendingRenewalsAsync(pending);
    }

    // Whether an interrupted renewal billing gives no answer about may wait for the next run: for as long as a renewal
    // whose payment change timed out is re-checked, counted from its last start. After that the customer is told it
    // failed, so an entry is not asked about for ever, nor dropped unnoticed when the journal expires.
    private static bool CanWaitForBilling(RenewalJournalEntry entry)
    {
        return DateTime.UtcNow < entry.StageSince + _renewVerifyPeriod;
    }

    // The entries an earlier run left in the journal that are now taken up again. One run ends before the next one
    // starts, so whatever is there was left by a process that stopped half-way - or by another instance that is still
    // finishing it after the active one changed. A started renewal is therefore left alone for a while first: billing
    // may still be applying its payment change. A queued one has paid nothing, and every process claims an entry
    // before starting it, so it only waits out one period: the queue of a stopped process is renewed by the next run
    // rather than long after the due date. A renewal the pending list already re-checks is not taken up a second time.
    private async Task<List<RenewalJournalEntry>> GetInterruptedRenewalsAsync(List<RenewalJournalEntry> journal, DateTime now)
    {
        if (journal.Count == 0)
        {
            return [];
        }

        var pending = await hybridCache.GetOrDefaultAsync<List<PendingRenewal>>(PendingCacheKey) ?? [];

        return journal
            .Where(entry => now - entry.StageSince >= (entry.Stage == RenewalStage.Started ? _renewNotExtendedGrace : ExecuteTaskPeriod))
            .Where(entry => _walletQuotas.ContainsKey(entry.Quota))
            .Where(entry => !pending.Exists(renewal => renewal.TenantId == entry.TenantId && renewal.PreviousDueDate == entry.DueDate))
            .ToList();
    }

    private async Task<List<RenewalJournalEntry>> LoadJournalAsync()
    {
        // a copy, for the same reason as the pending list
        return [.. await hybridCache.GetOrDefaultAsync<List<RenewalJournalEntry>>(JournalCacheKey) ?? []];
    }

    private async Task SaveJournalAsync(List<RenewalJournalEntry> journal)
    {
        if (journal.Count == 0)
        {
            await hybridCache.RemoveAsync(JournalCacheKey);

            return;
        }

        // far longer than the few runs it takes to resume an entry, so the entries outlive a restart of the service
        await hybridCache.SetAsync(JournalCacheKey, journal, TimeSpan.FromDays(1));
    }

    // Claims the entry for this process: marks it as started, with one more attempt, and returns it as the journal
    // now holds it. Null when the journal no longer holds the entry as this run read it - another instance has settled
    // it or taken it up - or when the journal could not be changed: the entry is then left as it is, for the run that
    // owns it or for the next one.
    private async Task<RenewalJournalEntry> TryClaimInJournalAsync(RenewalJournalEntry entry)
    {
        try
        {
            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey))
            {
                var journal = await LoadJournalAsync();

                var at = journal.FindIndex(entry.IsSameRenewal);
                if (at < 0 || journal[at] != entry)
                {
                    return null;
                }

                var claimed = entry with { Stage = RenewalStage.Started, StageSince = DateTime.UtcNow, Attempt = entry.Attempt + 1 };

                journal[at] = claimed;

                await SaveJournalAsync(journal);

                return claimed;
            }
        }
        catch (Exception ex)
        {
            logger.ErrorRenewSubscriptionServiceTenant(entry.TenantId, ex);

            return null;
        }
    }

    // Gives up the claim: the entry is removed, or put back as the given one. Only the very entry this process claimed
    // is touched - one that is gone or changed meanwhile is another instance's by now.
    private async Task ReleaseInJournalAsync(RenewalJournalEntry claimed, RenewalJournalEntry putBack)
    {
        try
        {
            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey))
            {
                var journal = await LoadJournalAsync();

                var at = journal.FindIndex(claimed.IsSameRenewal);
                if (at < 0 || journal[at] != claimed)
                {
                    return;
                }

                if (putBack != null)
                {
                    journal[at] = putBack;
                }
                else
                {
                    journal.RemoveAt(at);
                }

                await SaveJournalAsync(journal);
            }
        }
        catch (Exception ex)
        {
            // the renewal itself is settled; a leftover entry only gets it checked against billing once more
            logger.ErrorRenewSubscriptionServiceTenant(claimed.TenantId, ex);
        }
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
            logger.ErrorRenewSubscriptionServiceTenant(renewal.TenantId, ex);
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
            logger.ErrorRenewSubscriptionServiceTenant(tenantId, ex);
        }
    }

    /// <summary>
    /// A renewal the billing service did not answer about. Renewed as soon as billing reports the subscription to
    /// <see cref="ProductId"/> as ending after <see cref="PreviousDueDate"/>; failed when billing still answers
    /// that it does not once <see cref="_renewNotExtendedGrace"/> has passed since <see cref="TimedOutAt"/>, the
    /// moment the request timed out, or when billing has not answered at all within <see cref="_renewVerifyPeriod"/>.
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
        DateTime TimedOutAt);

    private enum RenewalStage
    {
        // in the journal, not taken up by any process: nothing has been paid
        Queued,

        // claimed by a process, so money may have moved: its checks, the wallet top-up and the payment change all come
        // after this mark
        Started
    }

    /// <summary>
    /// A renewal of one tariff row this service has taken on: the row as it was when its due date came into the window
    /// (<see cref="DueDate"/> is the date being renewed from), the <see cref="Stage"/> it got to and since when, and how
    /// many times it has been <see cref="RenewalStage.Started"/>. A renewal is told apart from the others by its tenant,
    /// quota and due date.
    /// </summary>
    private sealed record RenewalJournalEntry(
        int TenantId,
        int Quota,
        int Quantity,
        int? NextQuota,
        int? NextQuantity,
        DateTime DueDate,
        RenewalStage Stage,
        DateTime StageSince,
        int Attempt)
    {
        public bool IsSameRenewal(RenewalJournalEntry other)
        {
            return other.TenantId == TenantId && other.Quota == Quota && other.DueDate == DueDate;
        }
    }
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
