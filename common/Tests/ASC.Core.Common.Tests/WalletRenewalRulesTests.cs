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

namespace ASC.Core.Common.Tests;

/// <summary>
/// The decisions of the wallet renewal (<see cref="WalletRenewalRules"/>) the integration suite cannot bring about: a
/// process stopped between two steps, a second instance, a claim that grew old, a journal written by an older release.
/// Every time is given to the rule, so no test waits.
/// </summary>
public class WalletRenewalRulesTests
{
    private const int TenantId = 7;
    private const int AdminQuota = -10;
    private const int StorageQuota = -11;
    private const int OtherAdminQuota = -12;

    private static readonly DateTime _now = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _dueDate = new(2026, 11, 17, 12, 5, 0, DateTimeKind.Utc);
    private static readonly TimeSpan _period = TimeSpan.FromMinutes(1);

    private static readonly Dictionary<int, TenantQuota> _walletQuotas = new()
    {
        [AdminQuota] = new TenantQuota(AdminQuota) { Name = "adminwallet", ProductId = "admin", ServiceName = "admins", Wallet = true },
        [StorageQuota] = new TenantQuota(StorageQuota) { Name = "storagewallet", ProductId = "storage", ServiceName = "disk", Wallet = true, Additional = true },
        [OtherAdminQuota] = new TenantQuota(OtherAdminQuota) { Name = "adminwallet2", ProductId = "admin2", ServiceName = "admins2", Wallet = true }
    };

    private static WalletRenewalJournalEntry Entry(WalletRenewalStage stage = WalletRenewalStage.Started, DateTime? since = null, int attempt = 1, DateTime? paymentRequestedAt = null, int quota = AdminQuota, int? nextQuota = null)
    {
        return new WalletRenewalJournalEntry(TenantId, quota, 2, nextQuota, null, _dueDate, stage, since ?? _now, attempt, paymentRequestedAt);
    }

    private static WalletPendingRenewal Pending(int quota = AdminQuota, string productId = "admin", string serviceName = "admins", DateTime? previousDueDate = null)
    {
        return new WalletPendingRenewal(TenantId, quota, productId, "adminwallet 2", serviceName, 2, previousDueDate ?? _dueDate, Guid.NewGuid(), null, _now);
    }

    // --- the claim's age and the money steps

    [Fact]
    public void BeforeMoneyStep_ClaimAtItsLimit_IsNotHeld()
    {
        WalletRenewalRules.BeforeMoneyStep(_now, _now + WalletRenewalRules.ClaimWorkLimit, false).Should().Be(WalletRenewalHold.None);
    }

    [Fact]
    public void BeforeMoneyStep_ClaimPastItsLimit_IsHeldAsTooOld_EvenWhenStopping()
    {
        var later = _now + WalletRenewalRules.ClaimWorkLimit + TimeSpan.FromSeconds(1);

        WalletRenewalRules.BeforeMoneyStep(_now, later, false).Should().Be(WalletRenewalHold.ClaimTooOld);
        WalletRenewalRules.BeforeMoneyStep(_now, later, true).Should().Be(WalletRenewalHold.ClaimTooOld);
    }

    [Fact]
    public void BeforeMoneyStep_FreshClaimInAStoppingService_IsHeldAsStopping()
    {
        WalletRenewalRules.BeforeMoneyStep(_now, _now, true).Should().Be(WalletRenewalHold.Stopping);
    }

    [Fact]
    public void ClaimWorkLimit_LeavesTheOneDepositAndTheClocksRoomBeforeAnotherRunTakesTheEntryUp()
    {
        // a deposit the deadline cannot cut is bounded by the 60 s client timeout
        (WalletRenewalRules.NotExtendedGrace - WalletRenewalRules.ClaimWorkLimit).Should().BeGreaterThan(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void TimeLeftUnderClaim_CountsDownToTheLimit_AndNeverBelowZero()
    {
        WalletRenewalRules.TimeLeftUnderClaim(_now, _now + TimeSpan.FromMinutes(3)).Should().Be(WalletRenewalRules.ClaimWorkLimit - TimeSpan.FromMinutes(3));
        WalletRenewalRules.TimeLeftUnderClaim(_now, _now + WalletRenewalRules.ClaimWorkLimit + TimeSpan.FromMinutes(1)).Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(WalletRenewalHold.Stopping, false, WalletRenewalOutcome.Untouched)]
    [InlineData(WalletRenewalHold.Stopping, true, WalletRenewalOutcome.ClaimKept)]
    [InlineData(WalletRenewalHold.ClaimTooOld, false, WalletRenewalOutcome.ClaimKept)]
    [InlineData(WalletRenewalHold.ClaimTooOld, true, WalletRenewalOutcome.ClaimKept)]
    public void OutcomeOfHold_GivesTheEntryBackOnlyWhenNothingCanHaveBeenPaid(WalletRenewalHold hold, bool afterTopUp, WalletRenewalOutcome expected)
    {
        WalletRenewalRules.OutcomeOfHold(hold, afterTopUp).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, WalletRenewalOutcome.Settled)]
    [InlineData(false, WalletRenewalOutcome.ClaimKept)]
    public void OutcomeOfHandOver_KeepsTheClaimWhenThePendingListWasOutOfReach(bool handedOver, WalletRenewalOutcome expected)
    {
        WalletRenewalRules.OutcomeOfHandOver(handedOver).Should().Be(expected);
    }

    // --- the release

    [Fact]
    public void ReleaseOf_EachOutcome()
    {
        var asRead = Entry(attempt: 1, since: _now - TimeSpan.FromMinutes(20));

        WalletRenewalRules.ReleaseOf(WalletRenewalOutcome.Settled, asRead).Should().Be((true, (WalletRenewalJournalEntry?)null));
        WalletRenewalRules.ReleaseOf(WalletRenewalOutcome.WaitsForBilling, asRead).Should().Be((true, asRead));
        WalletRenewalRules.ReleaseOf(WalletRenewalOutcome.Untouched, asRead).Should().Be((true, asRead));
        WalletRenewalRules.ReleaseOf(WalletRenewalOutcome.ClaimKept, asRead).Release.Should().BeFalse();
    }

    [Fact]
    public void Release_RemovesTheClaimedEntry_OrPutsTheGivenOneBack()
    {
        var claimed = Entry(attempt: 2);
        var asRead = Entry(attempt: 1, since: _now - TimeSpan.FromMinutes(20));
        var other = Entry(quota: StorageQuota);

        var journal = new List<WalletRenewalJournalEntry> { other, claimed };
        WalletRenewalRules.Release(journal, claimed, null).Should().BeTrue();
        journal.Should().Equal(other);

        journal = [other, claimed];
        WalletRenewalRules.Release(journal, claimed, asRead).Should().BeTrue();
        journal.Should().Equal(other, asRead);
    }

    [Fact]
    public void Release_LeavesAnEntryAnotherInstanceHasChangedOrRemoved()
    {
        var claimed = Entry(attempt: 2);
        var takenOver = claimed with { Attempt = 3, StageSince = _now + TimeSpan.FromMinutes(10) };

        var journal = new List<WalletRenewalJournalEntry> { takenOver };
        WalletRenewalRules.Release(journal, claimed, null).Should().BeFalse();
        journal.Should().Equal(takenOver);

        journal = [];
        WalletRenewalRules.Release(journal, claimed, null).Should().BeFalse();
        journal.Should().BeEmpty();
    }

    // --- waiting for billing

    [Fact]
    public void CanWaitForBilling_CountsFromTheClaim_UpToTheVerifyPeriod()
    {
        var entry = Entry(since: _now);

        WalletRenewalRules.CanWaitForBilling(entry, _now + WalletRenewalRules.VerifyPeriod - TimeSpan.FromSeconds(1)).Should().BeTrue();
        WalletRenewalRules.CanWaitForBilling(entry, _now + WalletRenewalRules.VerifyPeriod).Should().BeFalse();
    }

    [Fact]
    public void CanWaitForBilling_CountsFromThePaymentRequest_WhenThereIsOne()
    {
        var requestedAt = _now + TimeSpan.FromMinutes(5);
        var entry = Entry(since: _now, paymentRequestedAt: requestedAt);

        WalletRenewalRules.CanWaitForBilling(entry, requestedAt + WalletRenewalRules.VerifyPeriod - TimeSpan.FromSeconds(1)).Should().BeTrue();
        WalletRenewalRules.CanWaitForBilling(entry, requestedAt + WalletRenewalRules.VerifyPeriod).Should().BeFalse();
    }

    [Fact]
    public void AsksBilling_AfterASilenceInTheRun_OnlyARenewalPastItsWait()
    {
        var waitingSince = _now;

        WalletRenewalRules.AsksBilling(false, waitingSince, _now).Should().BeTrue("billing has not failed in this run");
        WalletRenewalRules.AsksBilling(true, waitingSince, _now + WalletRenewalRules.VerifyPeriod - TimeSpan.FromSeconds(1))
            .Should().BeFalse("a renewal that may still wait takes the answer billing failed to give another one");
        WalletRenewalRules.AsksBilling(true, waitingSince, _now + WalletRenewalRules.VerifyPeriod)
            .Should().BeTrue("an answer that decides on the failure letter is the renewal's own");
    }

    // --- which entries a run takes up

    [Fact]
    public void IsInterrupted_StartedEntry_WaitsTheGraceFromItsClaim()
    {
        var entry = Entry(since: _now);

        WalletRenewalRules.IsInterrupted(entry, [], _walletQuotas, _now + WalletRenewalRules.NotExtendedGrace - TimeSpan.FromSeconds(1), _period).Should().BeFalse();
        WalletRenewalRules.IsInterrupted(entry, [], _walletQuotas, _now + WalletRenewalRules.NotExtendedGrace, _period).Should().BeTrue();
    }

    [Fact]
    public void IsInterrupted_StartedEntry_WaitsTheGraceFromItsPaymentRequest_WhenThatIsLater()
    {
        var requestedAt = _now + TimeSpan.FromMinutes(7);
        var entry = Entry(since: _now, paymentRequestedAt: requestedAt);

        WalletRenewalRules.IsInterrupted(entry, [], _walletQuotas, requestedAt + WalletRenewalRules.NotExtendedGrace - TimeSpan.FromSeconds(1), _period).Should().BeFalse();
        WalletRenewalRules.IsInterrupted(entry, [], _walletQuotas, requestedAt + WalletRenewalRules.NotExtendedGrace, _period).Should().BeTrue();
    }

    [Fact]
    public void IsInterrupted_QueuedEntry_WaitsOnePeriod()
    {
        var entry = Entry(WalletRenewalStage.Queued, since: _now, attempt: 0);

        WalletRenewalRules.IsInterrupted(entry, [], _walletQuotas, _now + _period - TimeSpan.FromSeconds(1), _period).Should().BeFalse();
        WalletRenewalRules.IsInterrupted(entry, [], _walletQuotas, _now + _period, _period).Should().BeTrue();
    }

    [Fact]
    public void IsInterrupted_EntryOfAnUnknownQuota_IsLeftToAnInstanceThatKnowsIt()
    {
        var entry = Entry(since: _now - TimeSpan.FromHours(1), quota: -99);

        WalletRenewalRules.IsInterrupted(entry, [], _walletQuotas, _now, _period).Should().BeFalse();
    }

    [Fact]
    public void IsInterrupted_EntryAPendingRenewalWithoutAQuotaMayCover_IsHeldBack()
    {
        var entry = Entry(since: _now - TimeSpan.FromHours(1));

        // written by the release before this one: no quota, and a product the entry's quota does not have
        var pending = Pending(quota: 0, productId: "changed", serviceName: "changed");

        WalletRenewalRules.IsInterrupted(entry, [pending], _walletQuotas, _now, _period).Should().BeFalse();
        WalletRenewalRules.IsInterrupted(entry, [pending with { PreviousDueDate = _dueDate.AddMonths(1) }], _walletQuotas, _now, _period).Should().BeTrue();
    }

    [Fact]
    public void IsToQueue_RowTheJournalOrThePendingListHas_IsNotQueuedAgain()
    {
        var row = Entry(WalletRenewalStage.Queued, attempt: 0);

        WalletRenewalRules.IsToQueue(row, [], [], _walletQuotas).Should().BeTrue();
        WalletRenewalRules.IsToQueue(row, [Entry(attempt: 2)], [], _walletQuotas).Should().BeFalse();
        WalletRenewalRules.IsToQueue(row, [], [Pending()], _walletQuotas).Should().BeFalse();
        WalletRenewalRules.IsToQueue(row, [], [Pending(quota: 0, productId: "changed", serviceName: "changed")], _walletQuotas).Should().BeFalse();
        WalletRenewalRules.IsToQueue(row, [], [Pending(quota: OtherAdminQuota)], _walletQuotas).Should().BeTrue();
    }

    // --- pruning the journal

    [Fact]
    public void Prune_TakesOutEveryEntryThePendingListCovers_AndKeepsTheRest()
    {
        var admin = Entry();
        var storage = Entry(quota: StorageQuota);
        var other = Entry(quota: OtherAdminQuota);

        var journal = new List<WalletRenewalJournalEntry> { admin, other, storage };
        var pending = new List<WalletPendingRenewal> { Pending(), Pending(quota: StorageQuota, productId: "storage", serviceName: "disk") };

        var pruned = WalletRenewalRules.Prune(journal, pending, _walletQuotas, _now);

        journal.Should().Equal(other);
        pruned.Should().Equal((storage, WalletRenewalPruneReason.CoveredByPending), (admin, WalletRenewalPruneReason.CoveredByPending));
    }

    [Fact]
    public void Prune_PendingRenewalWithoutAQuota_CoversByWhatItBought_TheQuotaOrTheScheduledOne()
    {
        var byQuota = Entry();
        var byNextQuota = Entry(quota: StorageQuota, nextQuota: OtherAdminQuota);

        var journal = new List<WalletRenewalJournalEntry> { byQuota, byNextQuota };
        var pending = new List<WalletPendingRenewal>
        {
            Pending(quota: 0),
            Pending(quota: 0, productId: "admin2", serviceName: "admins2")
        };

        WalletRenewalRules.Prune(journal, pending, _walletQuotas, _now).Should().HaveCount(2);
        journal.Should().BeEmpty();
    }

    [Fact]
    public void Prune_PendingRenewalWithoutAQuota_ThatBoughtSomethingElse_TakesNothingOut()
    {
        var entry = Entry();
        var journal = new List<WalletRenewalJournalEntry> { entry };

        WalletRenewalRules.Prune(journal, [Pending(quota: 0, productId: "changed", serviceName: "changed")], _walletQuotas, _now).Should().BeEmpty();
        journal.Should().Equal(entry);
    }

    [Fact]
    public void Prune_EntryOfAnUnknownQuota_GoesOnlyOnceOlderThanTheJournalLifetime()
    {
        var young = Entry(quota: -99, since: _now - WalletRenewalRules.JournalLifetime + TimeSpan.FromSeconds(1));
        var old = Entry(quota: -98, since: _now - WalletRenewalRules.JournalLifetime);

        var journal = new List<WalletRenewalJournalEntry> { young, old };

        WalletRenewalRules.Prune(journal, [], _walletQuotas, _now).Should().Equal((old, WalletRenewalPruneReason.UnknownQuota));
        journal.Should().Equal(young);
    }

    // --- after billing answered about an interrupted renewal

    [Theory]
    // extended: by the process that left it, or otherwise
    [InlineData(true, true, false, false, false, 1, true, true, WalletRenewalAnswerStep.ReportRenewed)]
    [InlineData(true, false, false, false, false, 0, true, true, WalletRenewalAnswerStep.CloseQuietly)]
    [InlineData(true, true, true, true, true, 3, false, false, WalletRenewalAnswerStep.ReportRenewed)]
    // not extended, and the row moved or the renewal is no longer wanted
    [InlineData(false, true, true, false, true, 3, true, true, WalletRenewalAnswerStep.CloseQuietly)]
    [InlineData(false, true, false, true, true, 3, true, true, WalletRenewalAnswerStep.CloseQuietly)]
    // not extended: the payment change was requested, or the attempts are spent
    [InlineData(false, true, false, false, true, 1, true, true, WalletRenewalAnswerStep.Fail)]
    [InlineData(false, true, false, false, false, WalletRenewalRules.MaxAttempts, true, true, WalletRenewalAnswerStep.Fail)]
    [InlineData(false, true, false, false, false, WalletRenewalRules.MaxAttempts - 1, true, true, WalletRenewalAnswerStep.Pay)]
    // not extended, the customer unknown: waits while it may, then is paid - and fails on the missing customer
    [InlineData(false, true, false, false, false, 1, false, true, WalletRenewalAnswerStep.WaitForCustomer)]
    [InlineData(false, true, false, false, false, 1, false, false, WalletRenewalAnswerStep.Pay)]
    [InlineData(false, false, false, false, false, 0, true, true, WalletRenewalAnswerStep.Pay)]
    public void AfterBillingAnswer_EachCase(bool extended, bool resumed, bool outdated, bool notToRenew, bool paymentRequested, int attempt, bool customerKnown, bool canWait, WalletRenewalAnswerStep expected)
    {
        WalletRenewalRules.AfterBillingAnswer(extended, resumed, outdated, notToRenew, paymentRequested, attempt, customerKnown, canWait).Should().Be(expected);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void SendsFailureLetter_OnlyForARenewalStillWantedOnAnUnchangedRow(bool notToRenew, bool outdated, bool expected)
    {
        WalletRenewalRules.SendsFailureLetter(notToRenew, outdated).Should().Be(expected);
    }

    // --- what the journal and the pending list hold, as other releases write and read them

    [Fact]
    public void JournalEntry_RoundTripsThroughJson_WithTheStageAsItsNumber()
    {
        var entry = Entry(paymentRequestedAt: _now);

        var json = JsonSerializer.Serialize(new List<WalletRenewalJournalEntry> { entry });

        json.Should().Contain("\"Stage\":1");
        JsonSerializer.Deserialize<List<WalletRenewalJournalEntry>>(json).Should().Equal(entry);
    }

    [Fact]
    public void JournalEntry_WrittenWithoutThePaymentMark_ReadsAsNotRequested()
    {
        var json = $"[{{\"TenantId\":{TenantId},\"Quota\":{AdminQuota},\"Quantity\":2,\"NextQuota\":null,\"NextQuantity\":null,\"DueDate\":\"2026-11-17T12:05:00Z\",\"Stage\":1,\"StageSince\":\"2026-11-17T12:00:00Z\",\"Attempt\":1}}]";

        JsonSerializer.Deserialize<List<WalletRenewalJournalEntry>>(json).Should().ContainSingle().Which.PaymentRequestedAt.Should().BeNull();
    }

    [Fact]
    public void PendingRenewal_WrittenWithoutAQuota_IsTheSameRenewalAsOneWithIt()
    {
        var withQuota = Pending();

        withQuota.IsSameRenewal(withQuota with { Quota = 0 }).Should().BeTrue();
        withQuota.IsSameRenewal(withQuota with { TimedOutAt = _now.AddSeconds(1) }).Should().BeFalse();
    }
}
