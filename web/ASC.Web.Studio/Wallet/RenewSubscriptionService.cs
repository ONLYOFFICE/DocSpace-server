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

    // Read once, together: the advance defaults to the period.
    private (TimeSpan Period, TimeSpan Advance) _timing = ReadTiming(configuration, logger);

    protected override TimeSpan ExecuteTaskPeriod
    {
        get => _timing.Period;
        set => _timing = (value, _timing.Advance);
    }

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

    // What the lock protects is the journal the instances share, so under it the journal is read from the distributed
    // level, not from this process's memory: that copy is only replaced when the notification of another instance's
    // change arrives, which can be after the lock has been handed over. For the same reason a read or a write that
    // does not reach the distributed level fails rather than quietly making do with the memory level: the run then
    // stops before the window moves, or the renewal is not claimed, and the next run takes it up.
    private static readonly FusionCacheEntryOptions _sharedReadOptions = new()
    {
        SkipMemoryCacheRead = true,
        ReThrowDistributedCacheExceptions = true
    };

    // Far longer than the few runs it takes to resume an entry, so the entries outlive a restart of the service. Every
    // save starts it anew for the whole journal.
    private static readonly TimeSpan _journalLifetime = TimeSpan.FromDays(1);

    private static readonly FusionCacheEntryOptions _journalWriteOptions = new()
    {
        Duration = _journalLifetime,
        ReThrowDistributedCacheExceptions = true
    };

    // How many times a renewal is started before the customer is told it failed: a renewal that stops a process on
    // every attempt must not be retried for ever.
    private const int MaxRenewalAttempts = 3;

    // The claims of renewals this process has settled but could not release, the journal being out of reach at that
    // moment, each with the entry to put back in its place, if any. They are released by the next run before it looks
    // for interrupted renewals: left in the journal, such an entry would be taken for one a stopped process left
    // half-way, and a renewal that ended with the failure letter would be made - and fail - again. Only this
    // process's memory knows the renewal is settled, so a process that stops here does leave it to be resumed.
    // Runs do not overlap, so the list needs no lock.
    private readonly List<(RenewalJournalEntry Claimed, RenewalJournalEntry PutBack)> _unreleased = [];

    // The interrupted renewals this process has already put on record. One that waits for billing goes back into
    // the journal unchanged and is taken up again by every run: it is logged as an error the first time only, as a
    // warning after that. Runs do not overlap, so the set needs no lock.
    private readonly HashSet<RenewalJournalEntry> _waiting = [];

    // The pending renewals the re-checks of this process have looked at, settled ones included, until the journal
    // has been saved without the entries they cover: a run that settles one and then fails at the journal must not
    // leave the entry to be resumed by the next run. Like the claims above, only this process's memory holds them.
    private readonly List<PendingRenewal> _rechecked = [];

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

            // as the list was before this run settled what it could: see PruneJournal
            _rechecked.AddRange(await VerifyPendingRenewalsAsync(stoppingToken));

            var now = DateTime.UtcNow;

            // Look ahead by the renew advance so quotas are renewed before their DueDate passes (no downtime).
            var to = now + _timing.Advance;

            var from = await hybridCache.GetOrDefaultAsync(CacheKey, to, token: stoppingToken);

            var walletQuotasToRenew = await Queries.GetWalletQuotasByDueDateAsync(coreDbContext, _walletQuotas.Keys.ToArray(), from, to).ToListAsync(stoppingToken);

            List<RenewalJournalEntry> interrupted;
            List<RenewalJournalEntry> queued;

            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey, cancellationToken: stoppingToken))
            {
                var journal = await LoadJournalAsync();

                // first, so that a renewal already settled is not taken for an interrupted one below
                var released = false;

                foreach (var (claimed, putBack) in _unreleased)
                {
                    released |= Release(journal, claimed, putBack);
                }

                List<PendingRenewal> pending =
                [
                    .. _rechecked,
                    .. await hybridCache.GetOrDefaultAsync<List<PendingRenewal>>(PendingCacheKey, null, _sharedReadOptions, stoppingToken) ?? []
                ];

                var pruned = PruneJournal(journal, pending, now);

                interrupted = GetInterruptedRenewals(journal, pending, now);

                // what is no longer taken up is no longer waiting
                _waiting.IntersectWith(interrupted);

                // A row the pending list re-checks is not queued again either: its payment change may have been applied.
                queued = walletQuotasToRenew
                    .Select(row => new RenewalJournalEntry(row.TenantId, row.Quota, row.Quantity, row.NextQuota, row.NextQuantity, row.DueDate.Value, RenewalStage.Queued, now, 0))
                    .Where(entry => !journal.Exists(entry.IsSameRenewal))
                    .Where(entry => !pending.Exists(renewal => Covers(renewal, entry) || MayCover(renewal, entry)))
                    .ToList();

                if (queued.Count > 0 || released || pruned)
                {
                    journal.AddRange(queued);
                    await SaveJournalAsync(journal);
                }

                // only once the journal is saved without them: a run that fails before that tries again
                _unreleased.Clear();
                _rechecked.Clear();
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
            // the time and the attempts its limits are counted from - not as it was claimed: a wait has not got to the
            // money, so it is no attempt, and the claim's fresh time would start the wait for billing anew on every
            // run, which would then never end.
            await ReleaseInJournalAsync(claimed, waitsForBilling ? data : null);
        }
    }

    // Returns true when the renewal has to wait in the journal for the next run: it was interrupted earlier and billing
    // has not said yet what became of the subscription meanwhile.
    private async Task<bool> TryRenewSubscriptionAsync(RenewalJournalEntry data, bool interrupted)
    {
        UserInfo payer = null;
        UserInfo owner = null;

        // what is being renewed, for the logs and the failure letter; refined to the purchased quota and quantity once they are known
        var renewedQuota = _walletQuotas[data.Quota];
        var renewedQuantity = data.NextQuantity ?? data.Quantity;

        // Only a renewal that got as far as the money steps may have been paid by the process that left it; one left
        // queued has paid nothing itself.
        var resumed = interrupted && data.Stage == RenewalStage.Started;

        // An interrupted renewal has waited, and what happened to the subscription meanwhile only billing can tell.
        // Until it has, nothing that goes wrong may end the renewal with the failure letter, which asks the customer
        // to renew manually - on top of a payment that may have been applied.
        var billingAnswered = !interrupted;

        // whether the tariff row is no longer the one the renewal was queued for
        var outdated = false;

        // Whether the subscription is not to be renewed any more: the renewal was switched off, the tariff is overdue,
        // the scheduled quota is unknown. A renewal that paid nothing ends right there. One a stopped process may have
        // paid goes on to billing first: if the payment was applied, it is put on record all the same - only the
        // renewal itself, and the failure letter, are left out.
        var notToRenew = false;

        // whether this process has taken the interrupted renewal up before: it has been waiting for billing since
        var waiting = interrupted && !_waiting.Add(data);

        if (resumed && !waiting)
        {
            // on record before anything below can end the renewal
            logger.ErrorRenewSubscriptionServiceInterrupted(data.TenantId, Describe(renewedQuota, renewedQuantity), data.Stage.ToString(), data.Attempt);
        }
        else if (interrupted && !waiting)
        {
            logger.WarningRenewSubscriptionServiceLeftQueued(data.TenantId, Describe(renewedQuota, renewedQuantity));
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(data.TenantId);

            if (tenant.Status != TenantStatus.Active)
            {
                return false;
            }

            if (interrupted)
            {
                // The entry is the tariff row as it was when the renewal was queued. Since then the customer may have
                // changed the quantity, scheduled another quota or switched the renewal off, so the row is read again:
                // the renewal is made as the row says now, and not at all when the row is gone or its due date moved -
                // a renewal of that new date is another one, which a later window brings.
                await using var coreDbContext = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

                var row = await coreDbContext.TariffRowAsync(data.TenantId, data.Quota);

                outdated = row == null || row.DueDate != data.DueDate;

                if (!outdated)
                {
                    data = data with { Quantity = row.Quantity, NextQuota = row.NextQuota, NextQuantity = row.NextQuantity };
                    renewedQuantity = data.NextQuantity ?? data.Quantity;
                }
                else if (!resumed)
                {
                    logger.WarningRenewSubscriptionServiceOutdated(data.TenantId, Describe(renewedQuota, renewedQuantity));

                    return false;
                }
            }

            if (data.NextQuantity is <= 0)
            {
                if (!resumed)
                {
                    return false;
                }

                notToRenew = true;
            }

            var tariffService = scope.ServiceProvider.GetRequiredService<ITariffService>();
            var currentTariff = await tariffService.GetTariffAsync(data.TenantId, refresh: false);
            var walletQuota = _walletQuotas[data.Quota];

            if (currentTariff.State > TariffState.Paid && walletQuota.Additional)
            {
                if (!resumed)
                {
                    return false;
                }

                notToRenew = true;
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            owner = await userManager.GetUsersAsync(tenant.OwnerId);

            // when a switch to a different quota is scheduled, buy that quota outright instead of renewing the current one
            var targetQuota = walletQuota;
            if (data.NextQuota is { } nextQuotaId && !_walletQuotas.TryGetValue(nextQuotaId, out targetQuota))
            {
                logger.ErrorRenewSubscriptionServiceUnknownNextQuota(data.TenantId, nextQuotaId);

                if (!resumed)
                {
                    return false;
                }

                // billing is asked about the quota the row holds, the only one the stopped process could have paid for
                notToRenew = true;
                targetQuota = walletQuota;
            }

            // null both for a customer billing does not know and for a request that failed
            var customerInfo = await tariffService.GetCustomerInfoAsync(data.TenantId);

            // known before anything below can end the renewal with the failure letter, which goes to the payer too
            if (customerInfo != null)
            {
                payer = await userManager.GetActivePayerAsync(customerInfo);
            }

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();

            if (interrupted)
            {
                // The stopped process may have got as far as the payment change, with billing applying it after the
                // process was gone, and the customer may have paid manually while the entry waited - renewing again
                // would make them pay twice. No answer is no "not extended" either: the exception leaves the entry
                // for the next run, see the catch below.
                var extended = await tariffService.IsSubscriptionExtendedAsync(data.TenantId, targetQuota.ProductId, data.DueDate);

                billingAnswered = true;

                if (extended && !resumed)
                {
                    // not the doing of this service, which had not got to the money: whoever paid has put it on record
                    logger.WarningRenewSubscriptionServiceOutdated(data.TenantId, Describe(targetQuota, renewedQuantity));

                    return false;
                }

                if (extended)
                {
                    try
                    {
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

                if (outdated)
                {
                    logger.WarningRenewSubscriptionServiceOutdated(data.TenantId, Describe(targetQuota, renewedQuantity));

                    return false;
                }

                if (notToRenew)
                {
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
                    if (waiting)
                    {
                        logger.WarningRenewSubscriptionServiceStillWaits(data.TenantId, Describe(targetQuota, renewedQuantity), null);
                    }

                    return true;
                }
            }

            if (customerInfo == null)
            {
                // The renewal cannot be paid without knowing the payment method. Fail loudly so the owner is
                // notified instead of the subscription lapsing unnoticed.
                throw new BillingException("Customer could not be found");
            }

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
                    data.Quota,
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
        catch (Exception ex) when (!billingAnswered && CanWaitForBilling(data))
        {
            // Whatever failed - the database, the cache, billing itself - did so before billing said what became of
            // the interrupted renewal: it is asked again on the next run, for as long as CanWaitForBilling allows.
            // An error the first time, a warning on the runs that find it still waiting.
            if (waiting)
            {
                logger.WarningRenewSubscriptionServiceStillWaits(data.TenantId, Describe(renewedQuota, renewedQuantity), ex);
            }
            else
            {
                logger.ErrorRenewSubscriptionServiceTenant(data.TenantId, ex);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.ErrorRenewSubscriptionServiceTenant(data.TenantId, ex);
        }

        if (notToRenew)
        {
            // billing never said whether the stopped process had paid, but no renewal is wanted any more: a letter
            // asking to renew manually would be wrong
            return false;
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

    // Returns the pending list as it was read, before anything in it was settled.
    private async Task<List<PendingRenewal>> VerifyPendingRenewalsAsync(CancellationToken stoppingToken)
    {
        List<PendingRenewal> pending = null;

        try
        {
            // The memory level first: a pending renewal written while Redis was out of reach lives there alone, and
            // it is this process that wrote it. What another instance wrote is read under the journal lock.
            pending = await hybridCache.GetOrDefaultAsync<List<PendingRenewal>>(PendingCacheKey, token: stoppingToken);
            if (pending is not { Count: > 0 })
            {
                return [];
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

        return pending ?? [];
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
    // whose payment change timed out is re-checked, counted from when it was queued or last started. After that the
    // customer is told it failed, so an entry is not asked about for ever, nor dropped unnoticed when the journal expires.
    private static bool CanWaitForBilling(RenewalJournalEntry entry)
    {
        return DateTime.UtcNow < entry.StageSince + _renewVerifyPeriod;
    }

    // The entries an earlier run left in the journal that are now taken up again. One run ends before the next one
    // starts, so whatever is there was left by a process that stopped half-way - or by another instance that is still
    // finishing it after the active one changed. A started renewal is therefore left alone for a while first: billing
    // may still be applying its payment change. A queued one has paid nothing, and every process claims an entry
    // before starting it, so it only waits out one period: the queue of a stopped process is renewed by the next run
    // rather than long after the due date. An entry of a quota this process does not know is left to one that does.
    // An entry that may be the one a pending renewal without a quota re-checks is held back for as long as that
    // pending renewal is there, as it was before the quota was recorded.
    private List<RenewalJournalEntry> GetInterruptedRenewals(List<RenewalJournalEntry> journal, List<PendingRenewal> pending, DateTime now)
    {
        return journal
            .Where(entry => now - entry.StageSince >= (entry.Stage == RenewalStage.Started ? _renewNotExtendedGrace : ExecuteTaskPeriod))
            .Where(entry => _walletQuotas.ContainsKey(entry.Quota))
            .Where(entry => !pending.Exists(renewal => MayCover(renewal, entry)))
            .ToList();
    }

    // Takes out of the journal the entries no run is to renew. True when the journal has changed.
    // An entry the pending list re-checks: a process that stops between writing the pending renewal and releasing its
    // claim leaves both, and the same renewal would be settled twice - by the pending list, and then again as an
    // interrupted one, renewed after the customer was told to renew manually. The pending list is given as the
    // re-checks of this process saw it since the journal was last saved, together with what it holds now, so a
    // renewal settled a moment ago is covered too. A process that stops between settling one and saving the journal
    // still leaves the entry to be resumed.
    // An entry whose quota this process does not know, once it is older than the journal's lifetime: every save
    // prolongs the whole journal, so it would never expire. Not before that: during a rollout another instance may
    // know a quota this one does not.
    private bool PruneJournal(List<RenewalJournalEntry> journal, List<PendingRenewal> pending, DateTime now)
    {
        var pruned = false;

        for (var i = journal.Count - 1; i >= 0; i--)
        {
            var entry = journal[i];

            if (pending.Exists(renewal => Covers(renewal, entry)))
            {
                logger.WarningRenewSubscriptionServiceLeftToRecheck(entry.TenantId, entry.Quota);
            }
            else if (!_walletQuotas.ContainsKey(entry.Quota) && now - entry.StageSince >= _journalLifetime)
            {
                logger.WarningRenewSubscriptionServiceUnknownQuota(entry.TenantId, entry.Quota);
            }
            else
            {
                continue;
            }

            journal.RemoveAt(i);
            pruned = true;
        }

        return pruned;
    }

    // Whether the pending renewal is the renewal of the journal entry.
    private static bool Covers(PendingRenewal renewal, RenewalJournalEntry entry)
    {
        return renewal.Quota == entry.Quota && renewal.TenantId == entry.TenantId && renewal.PreviousDueDate == entry.DueDate;
    }

    // Whether a pending renewal written before the quota was recorded - by a version that did not, or rewritten by
    // one - may be the renewal of the journal entry: the tenant and the due date are all there is to tell by, and
    // another quota of the tenant can have the same date. Enough to hold the entry back, not to take it out.
    private static bool MayCover(PendingRenewal renewal, RenewalJournalEntry entry)
    {
        return renewal.Quota == 0 && renewal.TenantId == entry.TenantId && renewal.PreviousDueDate == entry.DueDate;
    }

    // core:accounting:renewperiod - how often the service looks for subscriptions that are due, and how long a queued
    // renewal is left to the process that queued it: 00:00:01 to 01:00:00, 00:01:00 when not set. The maximum keeps
    // it well under the journal's lifetime.
    // core:accounting:renewadvance - how long before its due date a subscription is renewed, so that it is extended
    // before it expires: the period to 1.00:00:00, the period when not set. Not less than the period: a row that
    // comes due between two runs would be renewed by the later one, past its due date.
    private static (TimeSpan Period, TimeSpan Advance) ReadTiming(IConfiguration configuration, ILogger logger)
    {
        var period = configuration.ReadInterval("core:accounting:renewperiod", TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1), TimeSpan.FromHours(1), logger);
        var advance = configuration.ReadInterval("core:accounting:renewadvance", period, period, TimeSpan.FromDays(1), logger);

        return (period, advance);
    }

    private async Task<List<RenewalJournalEntry>> LoadJournalAsync()
    {
        // a copy, for the same reason as the pending list
        return [.. await hybridCache.GetOrDefaultAsync<List<RenewalJournalEntry>>(JournalCacheKey, null, _sharedReadOptions) ?? []];
    }

    private async Task SaveJournalAsync(List<RenewalJournalEntry> journal)
    {
        if (journal.Count == 0)
        {
            await hybridCache.RemoveAsync(JournalCacheKey, _journalWriteOptions);

            return;
        }

        await hybridCache.SetAsync(JournalCacheKey, journal, _journalWriteOptions);
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

                if (Release(journal, claimed, putBack))
                {
                    await SaveJournalAsync(journal);
                }
            }
        }
        catch (Exception ex)
        {
            // The renewal itself is settled, but the journal still says it is under way: the next run of this
            // process releases the claim before it looks for interrupted renewals.
            logger.ErrorRenewSubscriptionServiceTenant(claimed.TenantId, ex);

            _unreleased.Add((claimed, putBack));
        }
    }

    // Takes the claimed entry out of the journal, or puts the given one back in its place. False when the journal no
    // longer holds the entry as it was claimed, and is left as it is.
    private static bool Release(List<RenewalJournalEntry> journal, RenewalJournalEntry claimed, RenewalJournalEntry putBack)
    {
        var at = journal.FindIndex(claimed.IsSameRenewal);
        if (at < 0 || journal[at] != claimed)
        {
            return false;
        }

        if (putBack != null)
        {
            journal[at] = putBack;
        }
        else
        {
            journal.RemoveAt(at);
        }

        return true;
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
    /// <see cref="ServiceName"/> and <see cref="Quantity"/> fill the failure letter. <see cref="Quota"/> is the quota of
    /// the tariff row, which tells the renewal apart from another one of the tenant with the same due date; 0 in an
    /// entry written before the quota was recorded.
    /// </summary>
    private sealed record PendingRenewal(
        int TenantId,
        int Quota,
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
