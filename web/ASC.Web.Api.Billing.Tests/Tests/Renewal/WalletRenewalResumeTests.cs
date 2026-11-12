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

namespace ASC.Web.Api.Billing.Tests.Tests.Renewal;

/// <summary>
/// What Web.Studio (<c>RenewSubscriptionService</c>) does with a renewal whose outcome it does not know: one a stopped
/// process left in its journal, and one whose payment change got an answer that cannot be read.
/// </summary>
/// <remarks>
/// A test cannot stop a process half-way, so the first three plant the entry such a process leaves
/// (<see cref="RenewalJournal"/>); the service finds it on its next run, a few seconds later, and asks billing what
/// became of the subscription. Their subscription ends days from now, so the service never renews it by itself:
/// whatever billing is asked or told about it comes from the planted entry, which is marked as started longer ago
/// than the service leaves a started renewal alone. The last test goes the whole way through the service: it is the
/// one that shows the service itself writes the pending renewal the planted one stands for.
/// </remarks>
public class WalletRenewalResumeTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ChangeSubscription = "/billing/ChangeSubscription";
    private const string Deposit = "/billing/Deposit";
    private const string ActiveResources = "/billing/GetActiveResources";

    // far beyond the window the service renews in
    private static readonly TimeSpan _dueIn = TimeSpan.FromDays(3);

    // a few renewal periods from now, for the test the service renews by itself
    private static readonly TimeSpan _dueSoon = Constants.PaymentStubRenewPeriod * 4;

    // a started renewal is left alone for 10 minutes, a "not extended" answer of a re-check is final after 10 minutes
    private static readonly TimeSpan _startedAgo = TimeSpan.FromMinutes(12);
    private static readonly TimeSpan _timedOutAgo = TimeSpan.FromMinutes(11);

    // the run that takes the entry up and what it does with it, and room for a busy machine
    private static readonly TimeSpan _settleDeadline = Constants.PaymentStubRenewPeriod * 12;

    private readonly AspireAppFixture _fixture = fixture;

    [Fact]
    public async Task ResumedRenewal_BillingSaysExtended_IsNotPaidAgain()
    {
        // Arrange
        var row = await SubscribeAsync(DateTime.UtcNow + _dueIn);

        // the stopped process got its payment change through: billing has the next period, the stack has not read it
        Payments.Paid(Plans.AdminWalletProductId, quantity: 1, row.DueDate.AddDays(30), Owner.Email);

        await using var journal = await _fixture.OpenRenewalJournalAsync(TestContext.Current.CancellationToken);

        var askedBefore = AskedAboutSubscription();

        try
        {
            // Act
            await journal.PlantAsync(StartedEntry(row), pending: null, TestContext.Current.CancellationToken);

            var left = await WaitAsync(async () => await journal.FindEntryAsync(TenantId, TestContext.Current.CancellationToken) == null, _settleDeadline);

            // Assert
            left.Should().BeTrue("billing has answered, so the renewal is settled and leaves the journal");
            AskedAboutSubscription().Should().BeGreaterThan(askedBefore, "billing is asked what became of the interrupted renewal");
            PaymentCalls.Should().NotContain(call => call.Route == ChangeSubscription, "the subscription is extended already");
            PaymentCalls.Should().NotContain(call => call.Route == Deposit, "nothing is to be paid");
        }
        finally
        {
            await journal.RemoveAsync(TenantId, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ResumedRenewal_BillingGivesNoAnswer_WaitsInTheJournalAsItWas()
    {
        // Arrange
        var row = await SubscribeAsync(DateTime.UtcNow + _dueIn);

        Payments.On(PaymentService.Billing, "POST", ActiveResources, "\"billing is down\"", HttpStatusCode.InternalServerError);

        await using var journal = await _fixture.OpenRenewalJournalAsync(TestContext.Current.CancellationToken);

        var planted = StartedEntry(row);
        var askedBefore = AskedAboutSubscription();

        try
        {
            // Act
            await journal.PlantAsync(planted, pending: null, TestContext.Current.CancellationToken);

            // Billing has been asked about it twice, and the entry is back in the journal: while a run has it, the
            // entry is there as claimed, with a new time and one more attempt.
            var waits = await WaitAsync(async () =>
                AskedAboutSubscription() >= askedBefore + 2 &&
                await journal.FindEntryAsync(TenantId, TestContext.Current.CancellationToken) == planted,
                _settleDeadline);

            // Assert
            waits.Should().BeTrue("no answer is not a refusal: the renewal is asked about again and goes back as it was read, " +
                                  "with the time and the attempts its limits are counted from");

            PaymentCalls.Should().NotContain(call => call.Route == ChangeSubscription, "the payment change may have been applied already");
            PaymentCalls.Should().NotContain(call => call.Route == Deposit, "nothing is paid before billing has answered");
        }
        finally
        {
            await journal.RemoveAsync(TenantId, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task InterruptedRenewal_PendingListSettlesIt_IsNotRenewedAgain()
    {
        var row = await SubscribeAsync(DateTime.UtcNow + _dueIn);

        await AssertPendingListSettlesAloneAsync(row, row.Quota);
    }

    // A pending renewal written before the quota was recorded - by the previous version, during a rollout - names
    // the renewal by the tenant and the due date alone. The portal has one renewal of that date, so it is this one.
    [Fact]
    public async Task InterruptedRenewal_PendingWithoutQuotaSettlesIt_IsNotRenewedAgain()
    {
        var row = await SubscribeAsync(DateTime.UtcNow + _dueIn);

        await AssertPendingListSettlesAloneAsync(row, pendingQuota: 0);
    }

    private async Task AssertPendingListSettlesAloneAsync(TariffRow row, int pendingQuota)
    {
        // Arrange
        await using var journal = await _fixture.OpenRenewalJournalAsync(TestContext.Current.CancellationToken);

        // The process stopped between the two: the payment change had got no readable answer and was handed to the
        // re-checks, the claim in the journal was not released yet. Billing still says "not extended", long enough
        // for the re-check to take that as final.
        var pending = new PendingRenewal(
            TenantId,
            pendingQuota,
            Plans.AdminWalletProductId.ToString(CultureInfo.InvariantCulture),
            $"adminwallet {row.Quantity}",
            "admin",
            row.Quantity,
            row.DueDate,
            Owner.Id,
            null,
            DateTime.UtcNow - _timedOutAgo);

        try
        {
            // Act
            await journal.PlantAsync(StartedEntry(row), pending, TestContext.Current.CancellationToken);

            var left = await WaitAsync(async () =>
                !await journal.HoldsPendingAsync(TenantId, TestContext.Current.CancellationToken) &&
                await journal.FindEntryAsync(TenantId, TestContext.Current.CancellationToken) == null,
                _settleDeadline);

            // Assert
            left.Should().BeTrue("the re-check settles the renewal, and its entry leaves the journal with it");

            PaymentCalls.Should().NotContain(call => call.Route == ChangeSubscription,
                "the re-check has settled the renewal as failed: it is not made after that as an interrupted one");
            PaymentCalls.Should().NotContain(call => call.Route == Deposit, "nothing is paid for a renewal that is settled");
        }
        finally
        {
            await journal.RemoveAsync(TenantId, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Renewal_PaymentChangeAnswerUnreadable_IsRecheckedAndNotRepeated()
    {
        // Arrange
        var due = DateTime.UtcNow + _dueSoon;

        Payments.Customer(Owner.Email)
            .Balance(100m)
            .Paid(Plans.AdminWalletProductId, quantity: 1, due, Owner.Email)
            // a proxy's page in place of billing's answer: nothing can be told about the payment change from it
            .On(PaymentService.Billing, "POST", ChangeSubscription, "<html><body>502 Bad Gateway</body></html>");

        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        await using var journal = await _fixture.OpenRenewalJournalAsync(TestContext.Current.CancellationToken);

        try
        {
            // Act
            var handedOver = await WaitAsync(() => journal.HoldsPendingAsync(TenantId, TestContext.Current.CancellationToken), _dueSoon + _settleDeadline);

            // Assert
            handedOver.Should().BeTrue("an answer that cannot be read is not a refusal: the renewal is handed to the re-checks");

            // billing had applied the change after all
            Payments.Paid(Plans.AdminWalletProductId, quantity: 1, due.AddDays(30), Owner.Email);

            var settled = await WaitAsync(async () =>
                !await journal.HoldsPendingAsync(TenantId, TestContext.Current.CancellationToken) &&
                await journal.FindEntryAsync(TenantId, TestContext.Current.CancellationToken) == null,
                _settleDeadline);

            settled.Should().BeTrue("the re-check finds the subscription extended and settles the renewal");

            PaymentCalls.Count(call => call.Route == ChangeSubscription).Should().Be(1, "the payment change is never sent a second time");
        }
        finally
        {
            await journal.RemoveAsync(TenantId, TestContext.Current.CancellationToken);
        }
    }

    // The portal pays one administrator from the wallet until the given date, and the stack has stored the row the
    // service renews.
    private async Task<TariffRow> SubscribeAsync(DateTime due)
    {
        Payments.Customer(Owner.Email)
            .Balance(100m)
            .Paid(Plans.AdminWalletProductId, quantity: 1, due, Owner.Email)
            .On(PaymentService.Billing, "POST", ChangeSubscription, "true");

        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // The journal entry names the row by its quota and its due date as the database holds it, to the second;
        // no endpoint returns the quota id of the wallet plan.
        await using var connection = await _fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT quota, quantity, due_date FROM tenants_tariffrow WHERE tenant = @tenant AND due_date IS NOT NULL ORDER BY tariff_id DESC LIMIT 1";
        command.Parameters.AddWithValue("@tenant", TenantId);

        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        (await reader.ReadAsync(TestContext.Current.CancellationToken)).Should().BeTrue("the tariff read has stored the paid row");

        return new TariffRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetDateTime(2));
    }

    // the entry a process leaves when it stops after it has claimed the renewal
    private RenewalJournalEntry StartedEntry(TariffRow row)
    {
        return new RenewalJournalEntry(TenantId, row.Quota, row.Quantity, null, null, row.DueDate, RenewalJournalEntry.Started, DateTime.UtcNow - _startedAgo, 1);
    }

    private int AskedAboutSubscription()
    {
        return PaymentCalls.Count(call => call.Route == ActiveResources);
    }

    private static async Task<bool> WaitAsync(Func<Task<bool>> until, TimeSpan within)
    {
        var deadline = DateTime.UtcNow + within;

        while (true)
        {
            var reached = await until();

            if (reached || DateTime.UtcNow >= deadline)
            {
                return reached;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    private sealed record TariffRow(int Quota, int Quantity, DateTime DueDate);
}
