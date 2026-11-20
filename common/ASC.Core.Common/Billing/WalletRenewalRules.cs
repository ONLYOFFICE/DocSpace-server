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

namespace ASC.Core.Billing;

/// <summary>
/// How far a renewal in the journal got: <see cref="Queued"/> when no process has taken it up and nothing was paid,
/// <see cref="Started"/> once a process claimed it, so money may have moved. Whether its payment change was requested
/// the entry says itself (<see cref="WalletRenewalJournalEntry.PaymentRequestedAt"/>). Stored as its number.
/// </summary>
public enum WalletRenewalStage
{
    Queued,
    Started
}

/// <summary>
/// What is to become of a renewal's journal entry once the process that claimed it is done with it.
/// </summary>
public enum WalletRenewalOutcome
{
    /// <summary>Over in this process, whichever way: the claim is given up and the entry leaves the journal.</summary>
    Settled,

    /// <summary>Interrupted earlier, and billing has not said yet what became of it: the entry goes back as it was read.</summary>
    WaitsForBilling,

    /// <summary>Nothing was paid in this process - it is stopping: the entry goes back as it was read.</summary>
    Untouched,

    /// <summary>Money may have moved, or another instance has the entry: it stays as this process last wrote it.</summary>
    ClaimKept
}

/// <summary>Why a money step of a renewal is not begun.</summary>
public enum WalletRenewalHold
{
    None,

    /// <summary>The claim is older than <see cref="WalletRenewalRules.ClaimWorkLimit"/>.</summary>
    ClaimTooOld,

    /// <summary>The service is stopping.</summary>
    Stopping
}

/// <summary>
/// What an interrupted renewal does once billing has answered whether the subscription was extended meanwhile.
/// </summary>
public enum WalletRenewalAnswerStep
{
    /// <summary>Extended by the process that left the renewal: its audit entry is written, nothing is paid.</summary>
    ReportRenewed,

    /// <summary>Closed without a payment and without the failure letter.</summary>
    CloseQuietly,

    /// <summary>Ended with the failure letter: the payment change was requested, or the attempts are spent.</summary>
    Fail,

    /// <summary>Billing answered about the subscription but not about the customer: asked again on the next run.</summary>
    WaitForCustomer,

    /// <summary>Not extended, nothing in the way: the renewal is made now.</summary>
    Pay
}

/// <summary>Why an entry is taken out of the journal without being renewed.</summary>
public enum WalletRenewalPruneReason
{
    /// <summary>The pending list re-checks its payment change and settles it.</summary>
    CoveredByPending,

    /// <summary>Its quota is not a wallet quota any more, and it is older than the journal's lifetime.</summary>
    UnknownQuota
}

/// <summary>
/// A renewal of one tariff row the renewal service has taken on: the row as it was when its due date came into the
/// window (<see cref="DueDate"/> is the date being renewed from), the <see cref="Stage"/> it got to and since when, and
/// how many times it has been started. A renewal is told apart from the others by its tenant, quota and due date.
/// <see cref="PaymentRequestedAt"/> is when its payment change was requested - recorded just before it is sent, so it
/// may not have left - and null while it has not been: from then on the renewal is only asked about, never paid again.
/// </summary>
public sealed record WalletRenewalJournalEntry(
    int TenantId,
    int Quota,
    int Quantity,
    int? NextQuota,
    int? NextQuantity,
    DateTime DueDate,
    WalletRenewalStage Stage,
    DateTime StageSince,
    int Attempt,
    DateTime? PaymentRequestedAt = null)
{
    public bool IsSameRenewal(WalletRenewalJournalEntry other)
    {
        return other.TenantId == TenantId && other.Quota == Quota && other.DueDate == DueDate;
    }
}

/// <summary>
/// A renewal the billing service did not answer about. Renewed as soon as billing reports the subscription to
/// <see cref="ProductId"/> as ending after <see cref="PreviousDueDate"/>; failed when billing still answers that it
/// does not once <see cref="WalletRenewalRules.NotExtendedGrace"/> has passed since <see cref="TimedOutAt"/>, the
/// moment the request timed out, or when billing has not answered at all within
/// <see cref="WalletRenewalRules.VerifyPeriod"/>. Self-contained on purpose: it is settled without looking the wallet
/// quota up again, so it cannot be lost to a quota that is gone by then. <see cref="Description"/> is the log and audit
/// text ("adminwallet 2"), <see cref="ServiceName"/> and <see cref="Quantity"/> fill the failure letter.
/// <see cref="Quota"/> is the quota of the tariff row, which tells the renewal apart from another one of the tenant
/// with the same due date; 0 in an entry written by the release before it, which recorded no quota.
/// </summary>
public sealed record WalletPendingRenewal(
    int TenantId,
    int Quota,
    string ProductId,
    string Description,
    string ServiceName,
    int Quantity,
    DateTime PreviousDueDate,
    Guid OwnerId,
    Guid? PayerId,
    DateTime TimedOutAt)
{
    /// <summary>
    /// By what every version writes: the release before this one wrote the list without the quota, and the renewal is
    /// still the same one.
    /// </summary>
    public bool IsSameRenewal(WalletPendingRenewal other)
    {
        return other.TenantId == TenantId && other.ProductId == ProductId && other.PreviousDueDate == PreviousDueDate && other.TimedOutAt == TimedOutAt;
    }
}

/// <summary>
/// The decisions of the wallet renewal (<c>RenewSubscriptionService</c> of Web.Studio), as functions of what they decide
/// on and of the time given to them, so that each one is checked by a unit test: most of the cases they decide - a
/// process stopped half-way, a second instance, a journal out of reach - cannot be brought about on a test stand.
/// </summary>
public static class WalletRenewalRules
{
    /// <summary>
    /// How long a started renewal is left alone before another run takes it up, counted from the later of its claim
    /// and the request of its payment change: billing can finish a request it stopped answering minutes later. Also how
    /// long after a timed-out payment change the pending list still takes "not extended" as not final.
    /// </summary>
    public static readonly TimeSpan NotExtendedGrace = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long a renewal waits while billing gives no answer at all, before the customer is told it failed: counted
    /// from the request of the payment change, else from the claim or the queueing; for the pending list, from the
    /// timeout.
    /// </summary>
    public static readonly TimeSpan VerifyPeriod = TimeSpan.FromMinutes(30);

    /// <summary>
    /// How long after it was claimed a renewal may still begin a money step, and when a top-up under way is stopped.
    /// The two minutes up to <see cref="NotExtendedGrace"/> cover the one request a deadline cannot cut - a deposit,
    /// bounded by the 60 s client timeout - and the difference of the clocks of two instances: one stamps the claim,
    /// the other compares it.
    /// </summary>
    public static readonly TimeSpan ClaimWorkLimit = TimeSpan.FromMinutes(8);

    /// <summary>
    /// How long the journal and the pending list are kept in the cache; every save starts it anew for the whole list.
    /// Far longer than the few runs it takes to resume an entry, so the entries outlive a restart of the service.
    /// </summary>
    public static readonly TimeSpan JournalLifetime = TimeSpan.FromDays(1);

    /// <summary>
    /// How many times a renewal is started before the customer is told it failed: a renewal that stops a process on
    /// every attempt must not be retried for ever.
    /// </summary>
    public const int MaxAttempts = 3;

    /// <summary>Whether the claim made at <paramref name="claimedAt"/> is too old to begin a money step under.</summary>
    public static bool ClaimIsTooOld(DateTime claimedAt, DateTime now)
    {
        return now - claimedAt > ClaimWorkLimit;
    }

    /// <summary>How long a money step may still run under the claim made at <paramref name="claimedAt"/>.</summary>
    public static TimeSpan TimeLeftUnderClaim(DateTime claimedAt, DateTime now)
    {
        var left = claimedAt + ClaimWorkLimit - now;

        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    /// <summary>
    /// Whether a money step may begin: not under a claim that is too old, nor in a stopping service.
    /// </summary>
    public static WalletRenewalHold BeforeMoneyStep(DateTime claimedAt, DateTime now, bool stopping)
    {
        if (ClaimIsTooOld(claimedAt, now))
        {
            return WalletRenewalHold.ClaimTooOld;
        }

        return stopping ? WalletRenewalHold.Stopping : WalletRenewalHold.None;
    }

    /// <summary>
    /// The outcome of a money step that is not begun. Before the top-up a stopping process has paid nothing in this
    /// run, so the entry goes back as it was read; after it, a top-up may be in the wallet, and the claim is kept so
    /// that the next run asks billing first. A claim too old keeps the claim either way: another instance may be
    /// taking the entry up.
    /// </summary>
    public static WalletRenewalOutcome OutcomeOfHold(WalletRenewalHold hold, bool afterTopUp)
    {
        return hold == WalletRenewalHold.Stopping && !afterTopUp
            ? WalletRenewalOutcome.Untouched
            : WalletRenewalOutcome.ClaimKept;
    }

    /// <summary>
    /// The outcome of a payment change that got no readable answer: given up when the pending list has it on record,
    /// kept when the list was out of reach - a process that stops before the next run then leaves a started entry,
    /// resumed through billing, rather than the renewal to nobody.
    /// </summary>
    public static WalletRenewalOutcome OutcomeOfHandOver(bool handedOver)
    {
        return handedOver ? WalletRenewalOutcome.Settled : WalletRenewalOutcome.ClaimKept;
    }

    /// <summary>
    /// What the release does with the claimed entry: whether it is released at all, and the entry to put back in its
    /// place (null: the entry leaves the journal). A renewal waiting for billing, and one a stopping process did not
    /// start paying, go back as they were read, with the time and the attempts their limits are counted from - not as
    /// claimed: a wait is no attempt, and the claim's fresh time would start the wait anew on every run.
    /// </summary>
    public static (bool Release, WalletRenewalJournalEntry PutBack) ReleaseOf(WalletRenewalOutcome outcome, WalletRenewalJournalEntry asRead)
    {
        return outcome switch
        {
            WalletRenewalOutcome.Settled => (true, null),
            WalletRenewalOutcome.WaitsForBilling or WalletRenewalOutcome.Untouched => (true, asRead),
            _ => (false, null)
        };
    }

    /// <summary>
    /// Takes the claimed entry out of the journal, or puts <paramref name="putBack"/> in its place. False when the
    /// journal no longer holds the entry as it was claimed - another instance's by now - and is left as it is.
    /// </summary>
    public static bool Release(List<WalletRenewalJournalEntry> journal, WalletRenewalJournalEntry claimed, WalletRenewalJournalEntry putBack)
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

    /// <summary>
    /// Whether an interrupted renewal billing gives no answer about may wait for the next run: for
    /// <see cref="VerifyPeriod"/> from the request of its payment change, else from when it was queued or last started.
    /// After that the customer is told it failed, so an entry is not asked about for ever.
    /// </summary>
    public static bool CanWaitForBilling(WalletRenewalJournalEntry entry, DateTime now)
    {
        return MayWaitForBilling(entry.PaymentRequestedAt ?? entry.StageSince, now);
    }

    /// <summary>
    /// Whether a renewal billing has not answered about since <paramref name="waitingSince"/> may wait for the next
    /// run: for <see cref="VerifyPeriod"/>.
    /// </summary>
    public static bool MayWaitForBilling(DateTime waitingSince, DateTime now)
    {
        return now < waitingSince + VerifyPeriod;
    }

    /// <summary>
    /// Whether billing is asked about a renewal, waiting since <paramref name="waitingSince"/>, once it gave no answer
    /// about another one earlier in the same run. A renewal that may still wait takes that answer without asking - each
    /// question would only wait out the same client timeout while the renewals behind it pass their due dates. One past
    /// its wait is asked itself: that answer decides whether the customer is told the renewal failed, and must be
    /// this renewal's own.
    /// </summary>
    public static bool AsksBilling(bool silentInThisRun, DateTime waitingSince, DateTime now)
    {
        return !silentInThisRun || !MayWaitForBilling(waitingSince, now);
    }

    /// <summary>The later of the claim and the request of the payment change.</summary>
    public static DateTime LastStartedOrRequested(WalletRenewalJournalEntry entry)
    {
        return entry.PaymentRequestedAt is { } requestedAt && requestedAt > entry.StageSince ? requestedAt : entry.StageSince;
    }

    /// <summary>
    /// Whether a run takes the entry up as an interrupted renewal. An entry that outlives the run that wrote it was
    /// left unfinished - by a process that stopped, by one that kept its claim, or by another instance still at it. A
    /// started one is left alone for <see cref="NotExtendedGrace"/> from the later of its claim and the request of its
    /// payment change; a queued one has paid nothing and waits one <paramref name="period"/>. An entry of a quota this
    /// process does not know is left to one that does, and one that a pending renewal without a quota may re-check is
    /// held back while that pending renewal is there.
    /// </summary>
    public static bool IsInterrupted(WalletRenewalJournalEntry entry, IReadOnlyCollection<WalletPendingRenewal> pending, IReadOnlyDictionary<int, TenantQuota> walletQuotas, DateTime now, TimeSpan period)
    {
        var due = entry.Stage == WalletRenewalStage.Started
            ? now - LastStartedOrRequested(entry) >= NotExtendedGrace
            : now - entry.StageSince >= period;

        return due && walletQuotas.ContainsKey(entry.Quota) && !pending.Any(renewal => MayCover(renewal, entry));
    }

    /// <summary>
    /// Whether a row that came into the window is queued: not when the journal has its renewal already, nor when the
    /// pending list re-checks it - its payment change may have been applied.
    /// </summary>
    public static bool IsToQueue(WalletRenewalJournalEntry row, IReadOnlyCollection<WalletRenewalJournalEntry> journal, IReadOnlyCollection<WalletPendingRenewal> pending, IReadOnlyDictionary<int, TenantQuota> walletQuotas)
    {
        return !journal.Any(row.IsSameRenewal) && !pending.Any(renewal => Covers(renewal, row, walletQuotas) || MayCover(renewal, row));
    }

    /// <summary>
    /// Takes out of the journal the entries no run is to renew, and returns them with the reason. An entry the pending
    /// list re-checks: a process that stops between writing the pending renewal and releasing its claim leaves both,
    /// and the same renewal would be settled twice. This runs before the pending list is settled, so the entry is gone
    /// by the time its pending renewal is. An entry whose quota this process does not know, once it is older than
    /// <see cref="JournalLifetime"/>: every save prolongs the whole journal, so it would never expire. Not before that:
    /// during a rollout another instance may know a quota this one does not.
    /// </summary>
    public static List<(WalletRenewalJournalEntry Entry, WalletRenewalPruneReason Reason)> Prune(List<WalletRenewalJournalEntry> journal, IReadOnlyCollection<WalletPendingRenewal> pending, IReadOnlyDictionary<int, TenantQuota> walletQuotas, DateTime now)
    {
        var pruned = new List<(WalletRenewalJournalEntry, WalletRenewalPruneReason)>();

        for (var i = journal.Count - 1; i >= 0; i--)
        {
            var entry = journal[i];

            WalletRenewalPruneReason reason;

            if (pending.Any(renewal => Covers(renewal, entry, walletQuotas)))
            {
                reason = WalletRenewalPruneReason.CoveredByPending;
            }
            else if (!walletQuotas.ContainsKey(entry.Quota) && now - entry.StageSince >= JournalLifetime)
            {
                reason = WalletRenewalPruneReason.UnknownQuota;
            }
            else
            {
                continue;
            }

            journal.RemoveAt(i);
            pruned.Add((entry, reason));
        }

        return pruned;
    }

    /// <summary>
    /// Whether the pending renewal is the renewal of the journal entry. One without a quota is told by what it bought:
    /// the product and the service of the entry's quota, or of the quota the entry was to switch to.
    /// </summary>
    public static bool Covers(WalletPendingRenewal renewal, WalletRenewalJournalEntry entry, IReadOnlyDictionary<int, TenantQuota> walletQuotas)
    {
        if (renewal.TenantId != entry.TenantId || renewal.PreviousDueDate != entry.DueDate)
        {
            return false;
        }

        if (renewal.Quota != 0)
        {
            return renewal.Quota == entry.Quota;
        }

        return Bought(renewal, entry.Quota, walletQuotas) || entry.NextQuota is { } nextQuota && Bought(renewal, nextQuota, walletQuotas);
    }

    /// <summary>
    /// Whether a pending renewal without a quota may still be the renewal of an entry it could not be matched to by
    /// what it bought - the row may have changed since. The tenant and the due date are all there is left to tell by:
    /// enough to hold the entry back while the pending renewal is there, not to take it out.
    /// </summary>
    public static bool MayCover(WalletPendingRenewal renewal, WalletRenewalJournalEntry entry)
    {
        return renewal.Quota == 0 && renewal.TenantId == entry.TenantId && renewal.PreviousDueDate == entry.DueDate;
    }

    /// <summary>
    /// What an interrupted renewal does once billing has answered whether the subscription was extended meanwhile.
    /// <paramref name="resumed"/>: it was started, so the process that left it may have paid. Extended by that process
    /// - its audit entry; extended otherwise (paid by hand, or a renewal left queued) - closed quietly. Not extended: a
    /// row that is gone or whose due date moved, and a renewal no longer wanted, are closed quietly; one whose payment
    /// change was requested, or that spent its attempts, fails with the letter; one whose customer billing did not
    /// answer about waits while <paramref name="canWait"/>; anything else is paid now.
    /// </summary>
    public static WalletRenewalAnswerStep AfterBillingAnswer(bool extended, bool resumed, bool outdated, bool notToRenew, bool paymentRequested, int attempt, bool customerKnown, bool canWait)
    {
        if (extended)
        {
            return resumed ? WalletRenewalAnswerStep.ReportRenewed : WalletRenewalAnswerStep.CloseQuietly;
        }

        if (outdated || notToRenew)
        {
            return WalletRenewalAnswerStep.CloseQuietly;
        }

        if (paymentRequested || attempt >= MaxAttempts)
        {
            return WalletRenewalAnswerStep.Fail;
        }

        if (!customerKnown && canWait)
        {
            return WalletRenewalAnswerStep.WaitForCustomer;
        }

        return WalletRenewalAnswerStep.Pay;
    }

    /// <summary>
    /// Whether a renewal that failed sends the failure letter: not when it is no longer wanted, and not when its row is
    /// gone or its due date moved - the subscription changed meanwhile, most likely paid, and "renew manually" could
    /// make the customer pay twice.
    /// </summary>
    public static bool SendsFailureLetter(bool notToRenew, bool outdated)
    {
        return !notToRenew && !outdated;
    }

    private static bool Bought(WalletPendingRenewal renewal, int quota, IReadOnlyDictionary<int, TenantQuota> walletQuotas)
    {
        return walletQuotas.TryGetValue(quota, out var walletQuota)
               && walletQuota.ProductId == renewal.ProductId
               && walletQuota.ServiceName == renewal.ServiceName;
    }
}
