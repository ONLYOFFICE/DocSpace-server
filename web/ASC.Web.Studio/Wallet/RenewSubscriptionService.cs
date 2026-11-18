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

/// <summary>
/// Renews the subscriptions paid from the wallet shortly before their due date. Every renewal is put into a journal in
/// the distributed cache before the due-date window moves past it, and leaves the journal when it ends - unless money may
/// have moved and nothing else has the renewal on record, when its claim stays for a later run to ask billing. An
/// entry that outlives its run is taken up by a later one. What the service decides at each step is in
/// <see cref="WalletRenewalRules"/>, where unit tests reach it; this class reads, writes and calls.
/// </summary>
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
    private (TimeSpan Period, TimeSpan Advance) _timing = WalletRenewalTiming.Read(configuration, logger);

    protected override TimeSpan ExecuteTaskPeriod
    {
        get => _timing.Period;
        set => _timing = (value, _timing.Advance);
    }

    // Renewals whose payment change got no answer from the billing service, re-checked on every run that reaches the
    // journal.
    private const string PendingCacheKey = "renewsubscriptionservice_pending";

    // The journal is one value, read and written back as a whole: every change of it is made under this lock, so that
    // two instances - the active one and one still finishing its run after the active one changed - cannot overwrite
    // each other's change or claim the same renewal.
    private const string JournalLockKey = "renewsubscriptionservice_journal_lock";

    // What the lock protects is shared by the instances, so under it the journal, the pending list and the window are
    // read from the distributed level, not from this process's memory: that copy is only replaced when the
    // notification of another instance's change arrives, which can be after the lock has been handed over. For the same
    // reason a read or a write that does not reach the distributed level fails rather than quietly making do with the
    // memory level: the run then stops before the window moves, or the renewal is not claimed, and a later run takes it up.
    private static readonly FusionCacheEntryOptions _sharedReadOptions = new()
    {
        SkipMemoryCacheRead = true,
        ReThrowDistributedCacheExceptions = true
    };

    private static readonly FusionCacheEntryOptions _journalWriteOptions = new()
    {
        Duration = WalletRenewalRules.JournalLifetime,
        ReThrowDistributedCacheExceptions = true
    };

    // The pending list is shared the same way: changed under the journal lock, by adding or taking out one's own
    // renewals in the list as it is there, never by writing back a copy read earlier - which would undo what another
    // instance has written since.
    private static readonly FusionCacheEntryOptions _pendingWriteOptions = new()
    {
        Duration = WalletRenewalRules.JournalLifetime,
        ReThrowDistributedCacheExceptions = true
    };

    // A window that expired would start from now and skip the rows that came due meanwhile, so it is kept far longer
    // than any stop of the service; every run writes it anew.
    private static readonly FusionCacheEntryOptions _windowWriteOptions = new()
    {
        Duration = TimeSpan.FromDays(365),
        ReThrowDistributedCacheExceptions = true
    };

    // The claims of renewals this process has settled but could not release, the journal being out of reach at that
    // moment, each with the entry to put back in its place, if any. They are released by the next run before it looks
    // for interrupted renewals: left in the journal, such an entry would be taken for an unfinished one, and a renewal
    // that ended with the failure letter would be made - and fail - again. Only this process's memory knows the
    // renewal is settled, so a process that stops here, or turns passive, does leave it to be resumed.
    // Runs do not overlap, so the list needs no lock.
    private readonly List<(WalletRenewalJournalEntry Claimed, WalletRenewalJournalEntry PutBack)> _unreleased = [];

    // The interrupted renewals this process has already put on record. One that waits for billing goes back into
    // the journal unchanged and is taken up again by every run: it is logged as an error the first time only, as a
    // warning after that. Runs do not overlap, so the set needs no lock.
    private readonly HashSet<WalletRenewalJournalEntry> _waiting = [];

    // The changes of the pending list this process could not write, the list being out of reach at that moment:
    // the renewals to add and the settled ones to take out. The next run makes them before it reads the list. Only
    // this process's memory holds them. A process that stops here, or an instance that turns passive and does not
    // run again, loses a renewal to add - which is why such a renewal keeps its claim in the journal and is resumed
    // from there - and leaves a settled one to be settled once more.
    private readonly List<WalletPendingRenewal> _pendingToAdd = [];
    private readonly List<WalletPendingRenewal> _pendingToRemove = [];

    // The answer billing failed to give in this run, if it failed: the other interrupted renewals that may still wait
    // take it without asking (WalletRenewalRules.AsksBilling). Cleared at the start of every run and by an answer.
    private Exception _billingSilence;

    // The renewals taken on and not finished yet, see WalletRenewalJournalEntry.
    private static string GetJournalCacheKey()
    {
        return "renewsubscriptionservice_journal";
    }

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        _billingSilence = null;

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

            var now = DateTime.UtcNow;

            // Look ahead by the renew advance so quotas are renewed before their DueDate passes (no downtime).
            var to = now + _timing.Advance;

            List<WalletRenewalJournalEntry> interrupted;
            List<WalletRenewalJournalEntry> queued;
            List<WalletPendingRenewal> pending;

            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey, cancellationToken: stoppingToken))
            {
                // The window is read and moved under the lock, at the distributed level: another instance reading a
                // window this one has moved past would query again the rows this one has renewed and released already.
                var from = await hybridCache.GetOrDefaultAsync(CacheKey, to, _sharedReadOptions, stoppingToken);

                // a run that waited for the lock may find the window moved past its own end: it is never moved back
                if (to < from)
                {
                    to = from;
                }

                var walletQuotasToRenew = await Queries.GetWalletQuotasByDueDateAsync(coreDbContext, _walletQuotas.Keys.ToArray(), from, to).ToListAsync(stoppingToken);

                var journal = await LoadJournalAsync();

                // first, so that a renewal already settled is not taken for an interrupted one below
                var released = false;

                foreach (var (claimed, putBack) in _unreleased)
                {
                    released |= WalletRenewalRules.Release(journal, claimed, putBack);
                }

                // The journal is put in line with the pending list before any pending renewal is settled: an entry
                // the list covers leaves the journal first, so a run that stops after this leaves a pending renewal
                // without an entry - which the next run settles - not an entry whose pending renewal is gone. The
                // exceptions are an entry a pending renewal without a quota cannot be matched to, which is only held
                // back, and a pending renewal kept in memory (_pendingToAdd).
                pending = await SyncPendingAsync(stoppingToken);

                var pruned = WalletRenewalRules.Prune(journal, pending, _walletQuotas, now);

                foreach (var (entry, reason) in pruned)
                {
                    if (reason == WalletRenewalPruneReason.CoveredByPending)
                    {
                        logger.WarningRenewSubscriptionServiceLeftToRecheck(entry.TenantId, entry.Quota);
                    }
                    else
                    {
                        logger.WarningRenewSubscriptionServiceUnknownQuota(entry.TenantId, entry.Quota);
                    }
                }

                interrupted = journal.Where(entry => WalletRenewalRules.IsInterrupted(entry, pending, _walletQuotas, now, ExecuteTaskPeriod)).ToList();

                // what is no longer taken up is no longer waiting
                _waiting.IntersectWith(interrupted);

                queued = walletQuotasToRenew
                    .Select(row => new WalletRenewalJournalEntry(row.TenantId, row.Quota, row.Quantity, row.NextQuota, row.NextQuantity, row.DueDate.Value, WalletRenewalStage.Queued, now, 0))
                    .Where(entry => WalletRenewalRules.IsToQueue(entry, journal, pending, _walletQuotas))
                    .ToList();

                if (queued.Count > 0 || released || pruned.Count > 0)
                {
                    journal.AddRange(queued);
                    await SaveJournalAsync(journal);
                }

                // only once the journal is saved without them: a run that fails before that tries again
                _unreleased.Clear();

                // Only once the rows are in the journal: a window moved first would lose them for good to a process
                // that stops in between, as no later window covers their due dates again.
                await hybridCache.SetAsync(CacheKey, to, _windowWriteOptions, stoppingToken);
            }

            // Before the renewals, which they must not wait behind, and only in a run that has reached the journal:
            // while it is out of reach nothing is re-checked, and what waited is settled by the first run after.
            await VerifyPendingRenewalsAsync(pending, stoppingToken);

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

                    await RenewSubscriptionAsync(entry, wasInterrupted, stoppingToken);
                }
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async ValueTask RenewSubscriptionAsync(WalletRenewalJournalEntry data, bool interrupted, CancellationToken stoppingToken)
    {
        // The claim comes before anything else, the checks that can end the renewal with a failure letter included:
        // an entry that is gone or changed since this run read it belongs to another instance, which alone settles
        // it and tells the customer how it ended.
        var (claimed, claimFailed) = await TryClaimInJournalAsync(data);
        if (claimed == null)
        {
            // a journal out of reach is on record already, with its cause
            if (!claimFailed)
            {
                logger.WarningRenewSubscriptionServiceNotClaimed(data.TenantId, Describe(_walletQuotas[data.Quota], data.NextQuantity ?? data.Quantity));
            }

            return;
        }

        // as the journal holds it now: it changes once more when the payment change is recorded
        var claim = new Claim(claimed);

        var outcome = WalletRenewalOutcome.Settled;

        try
        {
            outcome = await TryRenewSubscriptionAsync(data, interrupted, claim, stoppingToken);
        }
        finally
        {
            var (release, putBack) = WalletRenewalRules.ReleaseOf(outcome, data);

            if (release)
            {
                await ReleaseInJournalAsync(claim.Entry, putBack);
            }
        }
    }

    // Makes the renewal under the given claim and says what is to become of its journal entry.
    private async Task<WalletRenewalOutcome> TryRenewSubscriptionAsync(WalletRenewalJournalEntry data, bool interrupted, Claim claim, CancellationToken stoppingToken)
    {
        var claimedAt = claim.Entry.StageSince;

        UserInfo payer = null;
        UserInfo owner = null;

        // what is being renewed, for the logs and the failure letter; refined to the purchased quota and quantity once they are known
        var renewedQuota = _walletQuotas[data.Quota];
        var renewedQuantity = data.NextQuantity ?? data.Quantity;

        // Only a renewal that got as far as the money steps may have been paid by the process that left it; one left
        // queued has paid nothing itself.
        var resumed = interrupted && data.Stage == WalletRenewalStage.Started;

        // An interrupted renewal has waited, and what happened to the subscription meanwhile only billing can tell.
        // Until it has, nothing that goes wrong may end the renewal with the failure letter, which asks the customer
        // to renew manually - on top of a payment that may have been applied.
        var billingAnswered = !interrupted;

        // whether the tariff row is no longer the one the renewal was queued for
        var outdated = false;

        // Whether the subscription is not to be renewed any more: the renewal was switched off, the tariff is overdue,
        // the scheduled quota is unknown. A renewal that paid nothing ends right there. One the process that left it
        // may have paid goes on to billing first: if the payment was applied, it is put on record all the same - only
        // the renewal itself, and the failure letter, are left out.
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
                return WalletRenewalOutcome.Settled;
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

                    return WalletRenewalOutcome.Settled;
                }
            }

            if (data.NextQuantity is <= 0)
            {
                if (!resumed)
                {
                    return WalletRenewalOutcome.Settled;
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
                    return WalletRenewalOutcome.Settled;
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
                    return WalletRenewalOutcome.Settled;
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
                // The process that left the renewal may have got as far as the payment change, with billing applying it
                // after that process was gone, and the customer may have paid manually while the entry waited -
                // renewing again would make them pay twice. No answer is no "not extended" either: the exception
                // leaves the entry for the next run, see the catch below.
                var extended = await IsSubscriptionExtendedAsync(tariffService, data, targetQuota);

                billingAnswered = true;

                var step = WalletRenewalRules.AfterBillingAnswer(extended, resumed, outdated, notToRenew,
                    data.PaymentRequestedAt != null, data.Attempt, customerInfo != null, WalletRenewalRules.CanWaitForBilling(data, DateTime.UtcNow));

                switch (step)
                {
                    case WalletRenewalAnswerStep.ReportRenewed:
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

                        return WalletRenewalOutcome.Settled;

                    case WalletRenewalAnswerStep.CloseQuietly:
                        if (extended || outdated)
                        {
                            logger.WarningRenewSubscriptionServiceOutdated(data.TenantId, Describe(targetQuota, renewedQuantity));
                        }

                        return WalletRenewalOutcome.Settled;

                    case WalletRenewalAnswerStep.Fail:
                        // A requested payment change is not sent a second time - billing may yet apply the first - and a
                        // renewal that stopped a process on each of its attempts is not started again: the customer is told.
                        throw new BillingException(data.PaymentRequestedAt != null
                            ? "The payment change was requested and billing does not report the subscription as extended"
                            : $"The renewal was interrupted on each of its {data.Attempt} attempts");

                    case WalletRenewalAnswerStep.WaitForCustomer:
                        logger.WarningRenewSubscriptionServiceWaits(data.TenantId, Describe(targetQuota, renewedQuantity), null);

                        return WalletRenewalOutcome.WaitsForBilling;
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

            var hold = WalletRenewalRules.BeforeMoneyStep(claimedAt, DateTime.UtcNow, stoppingToken.IsCancellationRequested);
            if (hold != WalletRenewalHold.None)
            {
                LogHold(hold, data.TenantId, description);

                return WalletRenewalRules.OutcomeOfHold(hold, afterTopUp: false);
            }

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

                // The top-up is held to the limit of the claim and stops with the service: a deposit already sent is let
                // finish, no further one is begun, and the poll of the balance after it is cut short.
                using var topUpLimit = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                topUpLimit.CancelAfter(WalletRenewalRules.TimeLeftUnderClaim(claimedAt, DateTime.UtcNow));

                bool covered;

                try
                {
                    covered = await tariffService.EnsureWalletBalanceAsync(data.TenantId, requiredAmount, defaultCurrency, null, siteName, true, metadata, allowTopUp, topUpLimit.Token);
                }
                catch (OperationCanceledException) when (topUpLimit.IsCancellationRequested)
                {
                    // A deposit may have been made. Whoever takes the entry up asks billing first, and tops up only
                    // what the wallet still lacks.
                    logger.ErrorRenewSubscriptionServiceTopUpStopped(data.TenantId, description);

                    return WalletRenewalOutcome.ClaimKept;
                }

                if (!covered)
                {
                    throw new BillingException("Insufficient balance");
                }
            }

            // the top-up may have taken long enough for another instance to have taken the renewal up
            hold = WalletRenewalRules.BeforeMoneyStep(claimedAt, DateTime.UtcNow, stoppingToken.IsCancellationRequested);
            if (hold != WalletRenewalHold.None)
            {
                LogHold(hold, data.TenantId, description);

                return WalletRenewalRules.OutcomeOfHold(hold, afterTopUp: true);
            }

            // The record comes before the step it describes, so it says the payment change was requested, not that
            // it was sent: from that moment whoever takes the entry up only asks billing about it - also when the
            // process stopped before the change left, which then ends with the failure letter rather than a second
            // attempt, unless the subscription changed meanwhile. It is also the last look at the journal - an entry
            // another instance has taken up meanwhile is not this process's to pay any more.
            var (marked, markFailed) = await TryMarkPaymentRequestedAsync(claim);
            if (!marked)
            {
                if (markFailed)
                {
                    logger.ErrorRenewSubscriptionServiceNotMarked(data.TenantId, description);
                }
                else
                {
                    logger.WarningRenewSubscriptionServiceTakenOver(data.TenantId, description);
                }

                return WalletRenewalOutcome.ClaimKept;
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

                var handedOver = await AddPendingRenewalAsync(new WalletPendingRenewal(
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

                return WalletRenewalRules.OutcomeOfHandOver(handedOver);
            }

            if (result)
            {
                try
                {
                    await ReportRenewedAsync(scope.ServiceProvider, data.TenantId, description);
                }
                catch (Exception ex)
                {
                    // the renewal is paid, only its audit entry is missing - no failure letter for it
                    logger.ErrorRenewSubscriptionServiceTenant(data.TenantId, ex);
                }

                return WalletRenewalOutcome.Settled;
            }
        }
        catch (Exception ex) when (!billingAnswered && WalletRenewalRules.CanWaitForBilling(data, DateTime.UtcNow))
        {
            // Whatever failed - the database, the cache, billing itself - did so before billing said what became of
            // the interrupted renewal: it is asked again on the next run, for as long as CanWaitForBilling allows.
            // An error the first time, a warning on the runs that find it still waiting.
            if (waiting)
            {
                logger.WarningRenewSubscriptionServiceWaits(data.TenantId, Describe(renewedQuota, renewedQuantity), ex);
            }
            else
            {
                logger.ErrorRenewSubscriptionServiceTenant(data.TenantId, ex);
            }

            return WalletRenewalOutcome.WaitsForBilling;
        }
        catch (Exception ex)
        {
            logger.ErrorRenewSubscriptionServiceTenant(data.TenantId, ex);
        }

        if (!WalletRenewalRules.SendsFailureLetter(notToRenew, outdated))
        {
            // billing never said whether the process that left the renewal had paid, but the subscription is not to be
            // renewed any more, or changed meanwhile: a letter asking to renew manually would be wrong
            return WalletRenewalOutcome.Settled;
        }

        await SendRenewSubscriptionErrorAsync(data.TenantId, Describe(renewedQuota, renewedQuantity), renewedQuota.ServiceName, renewedQuantity, payer, owner);

        return WalletRenewalOutcome.Settled;
    }

    // Asks billing whether the subscription was extended past the entry's due date. Once billing has given no answer in
    // this run, the other interrupted renewals take that answer without asking.
    private async Task<bool> IsSubscriptionExtendedAsync(ITariffService tariffService, WalletRenewalJournalEntry data, TenantQuota quota)
    {
        if (!WalletRenewalRules.AsksBilling(_billingSilence != null, data.PaymentRequestedAt ?? data.StageSince, DateTime.UtcNow))
        {
            throw new BillingTransportException("Billing gave no answer earlier in this run", _billingSilence);
        }

        try
        {
            var extended = await tariffService.IsSubscriptionExtendedAsync(data.TenantId, quota.ProductId, data.DueDate);

            _billingSilence = null;

            return extended;
        }
        catch (BillingTransportException ex)
        {
            _billingSilence = ex;

            throw;
        }
    }

    private void LogHold(WalletRenewalHold hold, int tenantId, string description)
    {
        if (hold == WalletRenewalHold.ClaimTooOld)
        {
            logger.ErrorRenewSubscriptionServiceClaimTooOld(tenantId, description);
        }
        else
        {
            logger.WarningRenewSubscriptionServiceStopping(tenantId, description);
        }
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

    // Re-checks the pending renewals, given as the run read them under the journal lock, and takes the settled ones
    // out of the list. The list is not claimed the way a journal entry is: two instances active at the same moment
    // settle the same renewal each - two letters, or two audit entries - as they always did.
    private async Task VerifyPendingRenewalsAsync(List<WalletPendingRenewal> pending, CancellationToken stoppingToken)
    {
        if (pending.Count == 0)
        {
            return;
        }

        var settled = new List<WalletPendingRenewal>();

        try
        {
            // Once billing has failed to answer in this run, every further check would only wait out the same
            // client timeout and get the same answer, delaying the regular renewals behind them - so the rest that
            // may still wait take that answer without asking (WalletRenewalRules.AsksBilling); an answer clears it.
            BillingTransportException billingDown = null;

            foreach (var renewal in pending)
            {
                // a stopping process settles no more: what is left stays in the list for the next one
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                var (extended, error) = WalletRenewalRules.AsksBilling(billingDown != null, renewal.TimedOutAt, DateTime.UtcNow)
                    ? await CheckExtendedAsync(renewal)
                    : (false, billingDown);

                if (error is null)
                {
                    billingDown = null;
                }
                else if (error is BillingTransportException transport)
                {
                    billingDown = transport;
                }

                if (await SettlePendingRenewalAsync(renewal, extended, error) == null)
                {
                    settled.Add(renewal);
                }
            }

            // the renewals of the journal take the same answer
            _billingSilence ??= billingDown;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }

        if (settled.Count == 0)
        {
            return;
        }

        try
        {
            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey))
            {
                var current = await LoadPendingAsync();

                if (current.RemoveAll(renewal => settled.Exists(renewal.IsSameRenewal)) > 0)
                {
                    await SavePendingAsync(current);
                }
            }
        }
        catch (Exception ex)
        {
            // settled all the same: the next run takes them out before it reads the list
            logger.ErrorWithException(ex);

            _pendingToRemove.AddRange(settled);
        }
    }

    private async Task<(bool Extended, Exception Error)> CheckExtendedAsync(WalletPendingRenewal renewal)
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
    private async Task<WalletPendingRenewal> SettlePendingRenewalAsync(WalletPendingRenewal renewal, bool extended, Exception error)
    {
        var description = renewal.Description;

        if (!extended)
        {
            // "not extended" is final once the grace period is over; no answer at all waits the full period
            var waitFor = error is null ? WalletRenewalRules.NotExtendedGrace : WalletRenewalRules.VerifyPeriod;

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

    // Hands the renewal to the re-checks; false when it could not be written to their list and is kept for the next
    // run to write. Never throws: a failure here must not end the renewal as failed, with the letter asking to renew
    // manually, when its payment change may have been applied.
    private async Task<bool> AddPendingRenewalAsync(WalletPendingRenewal renewal)
    {
        try
        {
            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey))
            {
                var pending = await LoadPendingAsync();

                pending.Add(renewal);

                await SavePendingAsync(pending);
            }

            return true;
        }
        catch (Exception ex)
        {
            // the next run adds it before it reads the list; kept first, so that nothing can come between
            _pendingToAdd.Add(renewal);

            logger.ErrorRenewSubscriptionServiceTenant(renewal.TenantId, ex);

            return false;
        }
    }

    // Under the journal lock: makes the changes of the pending list an earlier run could not write, and returns the
    // list as it is then.
    private async Task<List<WalletPendingRenewal>> SyncPendingAsync(CancellationToken stoppingToken)
    {
        var pending = await LoadPendingAsync(stoppingToken);

        var changed = pending.RemoveAll(renewal => _pendingToRemove.Exists(renewal.IsSameRenewal)) > 0;

        foreach (var renewal in _pendingToAdd.Where(renewal => !pending.Exists(renewal.IsSameRenewal)))
        {
            pending.Add(renewal);
            changed = true;
        }

        if (changed)
        {
            await SavePendingAsync(pending);
        }

        // only once they are written: a run that fails before that tries again
        _pendingToAdd.Clear();
        _pendingToRemove.Clear();

        return pending;
    }

    private async Task<List<WalletPendingRenewal>> LoadPendingAsync(CancellationToken stoppingToken = default)
    {
        // a copy, as the cache may hand out the list it keeps
        return [.. await hybridCache.GetOrDefaultAsync<List<WalletPendingRenewal>>(PendingCacheKey, null, _sharedReadOptions, stoppingToken) ?? []];
    }

    private async Task SavePendingAsync(List<WalletPendingRenewal> pending)
    {
        if (pending.Count == 0)
        {
            await hybridCache.RemoveAsync(PendingCacheKey, _pendingWriteOptions);

            return;
        }

        await hybridCache.SetAsync(PendingCacheKey, pending, _pendingWriteOptions);
    }

    private async Task<List<WalletRenewalJournalEntry>> LoadJournalAsync()
    {
        // a copy, for the same reason as the pending list
        return [.. await hybridCache.GetOrDefaultAsync<List<WalletRenewalJournalEntry>>(GetJournalCacheKey(), null, _sharedReadOptions) ?? []];
    }

    private async Task SaveJournalAsync(List<WalletRenewalJournalEntry> journal)
    {
        if (journal.Count == 0)
        {
            await hybridCache.RemoveAsync(GetJournalCacheKey(), _journalWriteOptions);

            return;
        }

        await hybridCache.SetAsync(GetJournalCacheKey(), journal, _journalWriteOptions);
    }

    // Claims the entry for this process: marks it as started, with one more attempt, and returns it as the journal
    // now holds it. Null when the journal no longer holds the entry as this run read it - another instance has settled
    // it or taken it up - or, with Failed set, when the journal could not be changed: the entry is then left as it
    // is, for the run that owns it or for the next one.
    private async Task<(WalletRenewalJournalEntry Claimed, bool Failed)> TryClaimInJournalAsync(WalletRenewalJournalEntry entry)
    {
        try
        {
            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey))
            {
                var journal = await LoadJournalAsync();

                var at = journal.FindIndex(entry.IsSameRenewal);
                if (at < 0 || journal[at] != entry)
                {
                    return (null, false);
                }

                var claimed = entry with { Stage = WalletRenewalStage.Started, StageSince = DateTime.UtcNow, Attempt = entry.Attempt + 1 };

                journal[at] = claimed;

                await SaveJournalAsync(journal);

                return (claimed, false);
            }
        }
        catch (Exception ex)
        {
            logger.ErrorRenewSubscriptionServiceTenant(entry.TenantId, ex);

            return (null, true);
        }
    }

    // Records in the claimed entry that its payment change is requested. Not marked when the journal no longer holds
    // the entry as this process claimed it - another instance has taken it up - or, with Failed set, when it could
    // not be changed: the payment change is then not sent.
    private async Task<(bool Marked, bool Failed)> TryMarkPaymentRequestedAsync(Claim claim)
    {
        try
        {
            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey))
            {
                var journal = await LoadJournalAsync();

                var at = journal.FindIndex(claim.Entry.IsSameRenewal);
                if (at < 0 || journal[at] != claim.Entry)
                {
                    return (false, false);
                }

                var marked = claim.Entry with { PaymentRequestedAt = DateTime.UtcNow };

                journal[at] = marked;

                await SaveJournalAsync(journal);

                claim.Entry = marked;

                return (true, false);
            }
        }
        catch (Exception ex)
        {
            logger.ErrorRenewSubscriptionServiceTenant(claim.Entry.TenantId, ex);

            return (false, true);
        }
    }

    // Gives up the claim: the entry is removed, or put back as the given one. Only the very entry this process claimed
    // is touched - one that is gone or changed meanwhile is another instance's by now.
    private async Task ReleaseInJournalAsync(WalletRenewalJournalEntry claimed, WalletRenewalJournalEntry putBack)
    {
        try
        {
            await using (await distributedLockProvider.TryAcquireLockAsync(JournalLockKey))
            {
                var journal = await LoadJournalAsync();

                if (WalletRenewalRules.Release(journal, claimed, putBack))
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

    private async Task SendRenewSubscriptionErrorAsync(WalletPendingRenewal renewal)
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

    // The owner may not be known yet - the renewal can fail before it is read - so the letter looks the owner up
    // itself: the letter is not lost to a missing recipient.
    private async Task SendRenewSubscriptionErrorAsync(int tenantId, string description, string serviceName, int quantity, UserInfo payer, UserInfo owner)
    {
        try
        {
            logger.ErrorRenewSubscriptionServiceFail(tenantId, description);

            await using var scope = _scopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(tenantId);

            owner ??= await scope.ServiceProvider.GetRequiredService<UserManager>().GetUsersAsync(tenant.OwnerId);

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

    // The entry a renewal is made under, as the journal holds it at the moment.
    private sealed class Claim(WalletRenewalJournalEntry entry)
    {
        public WalletRenewalJournalEntry Entry { get; set; } = entry;
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
