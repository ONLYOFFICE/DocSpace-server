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

namespace ASC.Web.Api.Billing.Tests.Tests.WalletServices;

/// <summary>
/// The administrators plan paid from the wallet for a year, <c>adminwalletyear</c>: it is bought with
/// <c>PUT payment/updatewallet</c> under the rules of the monthly <c>adminwallet</c>, and <c>Add</c> refuses one while
/// the other is in force.
/// </summary>
public class AdminWalletYearPlanTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ChangeRoute = "/billing/ChangeSubscription";
    private const string DepositRoute = "/billing/Deposit";
    private const string ActiveResourcesRoute = "/billing/GetActiveResources";

    [Fact]
    public async Task UpdateWalletPayment_AddYearlyPlan_BuysTheYearlyProduct()
    {
        // Arrange: a wallet that covers one administrator for a year
        Payments.Customer(Owner.Email)
            .Balance(1000m)
            .On(PaymentService.Billing, "POST", ChangeRoute, "true");

        // Act
        var bought = (await _paymentApi.UpdateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { [Plans.AdminWalletYearPlanName] = 1 }, ProductQuantityType.Add),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        bought.Should().BeTrue();

        var purchase = PaymentCalls.Should().ContainSingle(c => c.Route == ChangeRoute).Subject;
        BillingValue(purchase, "ProductId").Should().Be(Plans.AdminWalletYearProductId.ToString(CultureInfo.InvariantCulture));
        BillingValue(purchase, "ProductQty").Should().Be("1");
    }

    [Fact]
    public async Task UpdateWalletPayment_AddYearlyPlanBelowThePortalAdministrators_ThrowsBadRequest()
    {
        // Arrange: the owner and two room admins, so one administrator is fewer than the portal has
        await InviteMember(EmployeeType.RoomAdmin);
        await InviteMember(EmployeeType.RoomAdmin);

        Payments.Customer(Owner.Email)
            .Balance(1000m)
            .On(PaymentService.Billing, "POST", ChangeRoute, "true");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _paymentApi.UpdateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { [Plans.AdminWalletYearPlanName] = 1 }, ProductQuantityType.Add),
            TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Invalid quantity");
        PaymentCalls.Should().NotContain(c => c.Route == ChangeRoute, "nothing is bought");
    }

    [Theory]
    [InlineData(Plans.AdminWalletProductId, Plans.AdminWalletYearPlanName)]
    [InlineData(Plans.AdminWalletYearProductId, Plans.AdminWalletPlanName)]
    public async Task UpdateWalletPayment_AddOnePlanWhileTheTariffHoldsTheOther_ThrowsBadRequest(int heldProductId, string requestedPlan)
    {
        // Arrange: the portal is on one administrators plan, with a wallet that would cover the other
        Payments.Customer(Owner.Email)
            .Balance(1000m)
            .Paid(heldProductId, quantity: 1, DateTime.UtcNow.Date.AddMonths(1), Owner.Email)
            .On(PaymentService.Billing, "POST", ChangeRoute, "true");

        // the plan is read anew, so the purchase sees the held plan instead of the cached free one
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _paymentApi.UpdateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { [requestedPlan] = 1 }, ProductQuantityType.Add),
            TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Quota is already set");
        PaymentCalls.Should().NotContain(c => c.Route == ChangeRoute, "nothing is bought");
        PaymentCalls.Should().NotContain(c => c.Route == DepositRoute, "nothing is paid");
    }

    // A plan whose period has ended is no longer the plan of the portal, so it does not stand in the way of the other one.
    [Theory]
    [InlineData(Plans.AdminWalletProductId, Plans.AdminWalletYearPlanName, Plans.AdminWalletYearProductId)]
    [InlineData(Plans.AdminWalletYearProductId, Plans.AdminWalletPlanName, Plans.AdminWalletProductId)]
    public async Task UpdateWalletPayment_AddOnePlanAfterTheOtherHasEnded_BuysIt(int endedProductId, string requestedPlan, int requestedProductId)
    {
        // Arrange: the other administrators plan ended two days ago
        Payments.Customer(Owner.Email)
            .Balance(1000m)
            .Paid(endedProductId, quantity: 1, DateTime.UtcNow.AddDays(-2), Owner.Email)
            .On(PaymentService.Billing, "POST", ChangeRoute, "true");

        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var bought = (await _paymentApi.UpdateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { [requestedPlan] = 1 }, ProductQuantityType.Add),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        bought.Should().BeTrue();

        var purchase = PaymentCalls.Should().ContainSingle(c => c.Route == ChangeRoute).Subject;
        BillingValue(purchase, "ProductId").Should().Be(requestedProductId.ToString(CultureInfo.InvariantCulture));
    }

    // The check of the other plan and the purchase are one step per portal: of two requests sent together, the one
    // that comes second sees the plan the first one bought.
    [Fact]
    public async Task UpdateWalletPayment_BothPlansAddedAtTheSameMoment_BuysOne()
    {
        // Arrange: billing lists a product as paid for from the moment it sells it
        Payments.Customer(Owner.Email)
            .Balance(1000m)
            .On(PaymentService.Billing, "POST", ChangeRoute, call =>
            {
                var productId = int.Parse(BillingValue(call, "ProductId")!, CultureInfo.InvariantCulture);
                Payments.Paid(productId, quantity: 1, DateTime.UtcNow.Date.AddMonths(1), Owner.Email);

                return PaymentServiceAnswer.Json("true");
            });

        // Act
        var statuses = await Task.WhenAll(AddStatusAsync(Plans.AdminWalletPlanName), AddStatusAsync(Plans.AdminWalletYearPlanName));

        // Assert
        statuses.Should().BeEquivalentTo([200, 400]);
        PaymentCalls.Where(c => c.Route == ChangeRoute).Should().ContainSingle("one of the two plans is bought");
    }

    // The check of the other plan asks billing for the payments. When billing does not answer, nothing says that the
    // portal holds no other plan: the stored tariff may lack what a request before this one bought.
    [Theory]
    [InlineData(Plans.AdminWalletPlanName)]
    [InlineData(Plans.AdminWalletYearPlanName)]
    public async Task UpdateWalletPayment_AddPlanWhileBillingDoesNotListThePayments_ThrowsPaymentRequired(string requestedPlan)
    {
        // Arrange: billing would sell the plan, but fails when asked what the portal has paid for
        Payments.Customer(Owner.Email)
            .Balance(1000m)
            .On(PaymentService.Billing, "POST", ChangeRoute, "true")
            .On(PaymentService.Billing, "POST", ActiveResourcesRoute, "\"billing is down\"", HttpStatusCode.InternalServerError);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _paymentApi.UpdateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { [requestedPlan] = 1 }, ProductQuantityType.Add),
            TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        PaymentCalls.Should().Contain(c => c.Route == ActiveResourcesRoute, "billing was asked for the payments");
        PaymentCalls.Should().NotContain(c => c.Route == ChangeRoute, "nothing is bought");
        PaymentCalls.Should().NotContain(c => c.Route == DepositRoute, "nothing is paid");
    }

    private async Task<int> AddStatusAsync(string plan)
    {
        try
        {
            var answer = await _paymentApi.UpdateWalletPaymentAsync(
                new WalletQuantityRequestDto(new Dictionary<string, int?> { [plan] = 1 }, ProductQuantityType.Add),
                TestContext.Current.CancellationToken);

            return (int)answer.StatusCode;
        }
        catch (ApiException exception)
        {
            return exception.ErrorCode;
        }
    }

    // Only the other plan is refused: more administrators of the plan the portal holds are bought as before.
    [Fact]
    public async Task UpdateWalletPayment_AddThePlanThePortalHolds_BuysMoreOfIt()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .Balance(1000m)
            .Paid(Plans.AdminWalletYearProductId, quantity: 1, DateTime.UtcNow.Date.AddMonths(1), Owner.Email)
            .On(PaymentService.Billing, "POST", ChangeRoute, "true");

        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var bought = (await _paymentApi.UpdateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { [Plans.AdminWalletYearPlanName] = 2 }, ProductQuantityType.Add),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        bought.Should().BeTrue();

        var purchase = PaymentCalls.Should().ContainSingle(c => c.Route == ChangeRoute).Subject;
        BillingValue(purchase, "ProductId").Should().Be(Plans.AdminWalletYearProductId.ToString(CultureInfo.InvariantCulture));
        BillingValue(purchase, "ProductQty").Should().Be("2");
    }

    // The next period's quantity can be set only for a plan the portal holds, so Set cannot bring the other plan in.
    [Fact]
    public async Task UpdateWalletPayment_SetThePlanThePortalDoesNotHold_ReturnsFalse()
    {
        // Arrange
        Payments.Customer(Owner.Email)
            .Balance(1000m)
            .Paid(Plans.AdminWalletProductId, quantity: 1, DateTime.UtcNow.Date.AddMonths(1), Owner.Email)
            .On(PaymentService.Billing, "POST", ChangeRoute, "true");

        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);

        // Act
        var scheduled = (await _paymentApi.UpdateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { [Plans.AdminWalletYearPlanName] = 3 }, ProductQuantityType.Set),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        scheduled.Should().BeFalse();
        PaymentCalls.Should().NotContain(c => c.Route == ChangeRoute, "nothing is bought");
    }
}
