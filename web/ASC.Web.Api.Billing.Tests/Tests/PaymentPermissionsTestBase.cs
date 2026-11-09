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

namespace ASC.Web.Api.Billing.Tests.Tests;

/// <summary>
/// The role checks of the payment operations, which the stack without payment services never reaches: it refuses
/// every caller as "not configured" before it looks at the role. Here the portal is a billing customer whose payer is
/// the owner, every request is one the owner may make, and a member of the role under test sends it.
/// </summary>
/// <remarks>
/// Every payment operation with a role check has one request here, in one group of operations; a group has a class
/// that sends it as every refused role and a class that sends it as the callers it lets through.
/// <see cref="PaymentOperationsCoverageTests"/> holds the requests against the actions of the payment controller.
/// </remarks>
public abstract class PaymentPermissionsTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string BackUrl = "https://portal.example.com/back";
    private const string SuccessUrl = "https://portal.example.com/success";

    /// <summary>The e-mail of a payer who is no user of the portal, so neither the owner nor a member pays.</summary>
    protected const string ForeignPayer = "the-payer@example.com";

    /// <summary>
    /// The one payer operation the owner may use without being the payer (the billing account page): the others refuse
    /// an owner who does not pay.
    /// </summary>
    protected const string PayerOrOwnerOperation = "GET payment/account";

    // The limit the service limit operations read, change and delete; created by the owner before the role is switched.
    private int _serviceLimitId;

    /// <summary>Every role but the owner, who is the payer: refused by the operations for the payer.</summary>
    protected static EmployeeType[] NotThePayer => [EmployeeType.DocSpaceAdmin, .. NonAdministrators];

    /// <summary>The subscription: changing the plan, its balance, moving it to the wallet, the billing account page.</summary>
    protected static string[] SubscriptionPayerOperations =>
    [
        "PUT payment/update",
        "GET payment/subscription/balance",
        "POST payment/subscription/movetowallet",
        PayerOrOwnerOperation
    ];

    /// <summary>
    /// The one payer operation that also asks for a DocSpace administrator (attaching a payment method): a payer with a
    /// role below is refused it, while the other payer operations look at the payer alone.
    /// </summary>
    protected const string PayerAndAdministratorOperation = "GET payment/checkoutsetupurl";

    /// <summary>The roles below a DocSpace administrator a member who pays for the portal may have.</summary>
    protected static EmployeeType[] PayersBelowAdministrator => [EmployeeType.RoomAdmin, EmployeeType.User];

    /// <summary>The wallet as the payer keeps it: topping it up, its automatic top-up, attaching a payment method.</summary>
    protected static string[] WalletPayerOperations =>
    [
        "POST payment/deposit",
        "POST payment/topupsettings",
        PayerAndAdministratorOperation
    ];

    /// <summary>Buying a plan: the checkout address.</summary>
    protected static string[] CheckoutAdministratorOperations =>
    [
        "PUT payment/url"
    ];

    /// <summary>
    /// The billing customer and its wallet as an administrator reads them: the customer, the balance, the movements and
    /// the top-ups with their invoices.
    /// </summary>
    protected static string[] CustomerAdministratorOperations =>
    [
        "GET payment/customerinfo",
        "GET payment/customer/balance",
        "GET payment/customer/operations",
        "GET payment/customer/deposits",
        "GET payment/customer/deposits/{id}/invoice"
    ];

    /// <summary>The spending of the wallet: the usage by service and by month, and the operations report.</summary>
    protected static string[] UsageOperations =>
    [
        "GET payment/customer/usage",
        "GET payment/customer/usage/monthly",
        "POST payment/customer/operationsreport",
        "GET payment/customer/operationsreport",
        "DELETE payment/customer/operationsreport"
    ];

    /// <summary>The usage reports: built, followed and stopped.</summary>
    protected static string[] UsageReportOperations =>
    [
        "POST payment/customer/usage/report",
        "GET payment/customer/usage/report",
        "DELETE payment/customer/usage/report",
        "POST payment/customer/usage/monthly/report",
        "GET payment/customer/usage/monthly/report",
        "DELETE payment/customer/usage/monthly/report"
    ];

    /// <summary>
    /// The wallet services: buying them, the price of buying them, switching them on and off, the ones the portal has.
    /// </summary>
    protected static string[] WalletAdministratorOperations =>
    [
        "PUT payment/updatewallet",
        "PUT payment/calculatewallet",
        "POST payment/servicestate",
        "GET payment/servicessettings",
        "GET payment/activeservices"
    ];

    /// <summary>What accounting sells: its currencies and the prices of a service.</summary>
    protected static string[] AccountingAdministratorOperations =>
    [
        "GET payment/accounting/currencies",
        "GET payment/accounting/prices/{serviceName}"
    ];

    /// <summary>The spending limits on the AI services as an administrator reads them.</summary>
    protected static string[] ServiceLimitReadOperations =>
    [
        "GET payment/servicelimit/{id}",
        "GET payment/servicelimit/total",
        "GET payment/servicelimit/global",
        "GET payment/servicelimit/participants"
    ];

    /// <summary>The spending limits on the AI services as an administrator sets them.</summary>
    protected static string[] ServiceLimitWriteOperations =>
    [
        "POST payment/servicelimit",
        "PUT payment/servicelimit",
        "PUT payment/servicelimit/participants",
        "PUT payment/servicelimit/participants/disable",
        "DELETE payment/servicelimit/{id}"
    ];

    /// <summary>Every group of operations, each sent as the roles it refuses and as the callers it lets through.</summary>
    internal static IEnumerable<string> GroupedOperations =>
    [
        .. SubscriptionPayerOperations, .. WalletPayerOperations, .. CheckoutAdministratorOperations,
        .. CustomerAdministratorOperations, .. UsageOperations, .. UsageReportOperations, .. WalletAdministratorOperations,
        .. AccountingAdministratorOperations, .. ServiceLimitReadOperations, .. ServiceLimitWriteOperations
    ];

    /// <summary>The operations with a request here, as <c>METHOD payment/route</c> with the route template of the action.</summary>
    internal static IReadOnlyCollection<string> RequestedOperations => _requests.Keys;

    // One valid request per operation, as the owner would send it; the TypeScript suite's requests where it has them.
    private static readonly Dictionary<string, Func<PaymentPermissionsTestBase, CancellationToken, Task>> _requests = new()
    {
        ["PUT payment/update"] = (t, ct) => t._paymentApi.UpdatePaymentAsync(new QuantityRequestDto(new Dictionary<string, int> { ["admin"] = 1 }), ct),
        ["POST payment/deposit"] = (t, ct) => t._paymentApi.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), ct),
        ["GET payment/subscription/balance"] = (t, ct) => t._paymentApi.GetSubscriptionBalanceInfoAsync(ct),
        ["POST payment/subscription/movetowallet"] = (t, ct) =>
            t._paymentApi.MoveSubscriptionToWalletAsync(new QuantityRequestDto(new Dictionary<string, int> { ["admin"] = 1 }), ct),
        ["GET payment/account"] = (t, ct) => t._paymentApi.GetPaymentAccountAsync(BackUrl, ct),
        ["GET payment/checkoutsetupurl"] = (t, ct) => t._paymentApi.GetCheckoutSetupUrlAsync(BackUrl, SuccessUrl, ct),
        ["POST payment/topupsettings"] = (t, ct) => t._paymentApi.SetTenantWalletSettingsAsync(
            new TenantWalletSettingsRequestDto(new SetWalletTopUpSettingsRequest(enabled: true, minBalance: 10, upToBalance: 50, currency: "USD")), ct),

        ["PUT payment/url"] = (t, ct) => t._paymentApi.GetPaymentUrlAsync(
            new PaymentUrlRequestDto(BackUrl, SuccessUrl, new Dictionary<string, int> { [Plans.AdminPlanName] = 3 }), ct),

        ["GET payment/customerinfo"] = (t, ct) => t._paymentApi.GetCustomerInfoAsync(true, ct),
        ["GET payment/customer/balance"] = (t, ct) => t._paymentApi.GetCustomerBalanceAsync(true, ct),
        ["GET payment/customer/operations"] = (t, ct) => t._paymentApi.GetCustomerOperationsAsync(cancellationToken: ct),
        ["GET payment/customer/deposits"] = (t, ct) => t._paymentApi.GetCustomerDepositsAsync(cancellationToken: ct),
        ["GET payment/customer/deposits/{id}/invoice"] = (t, ct) => t._paymentApi.GetDepositInvoiceAsync(15342, ct),

        ["GET payment/customer/usage"] = (t, ct) => t._paymentApi.GetCustomerServiceUsageAsync(cancellationToken: ct),
        ["GET payment/customer/usage/monthly"] = (t, ct) => t._paymentApi.GetCustomerMonthlyUsageAsync(cancellationToken: ct),
        ["POST payment/customer/operationsreport"] = (t, ct) => t._paymentApi.CreateCustomerOperationsReportAsync(
            new CustomerOperationsReportRequestDto(serviceName: ["ai-tools"], credit: true, debit: true), ct),
        ["GET payment/customer/operationsreport"] = (t, ct) => t._paymentApi.GetCustomerOperationsReportAsync(ct),
        ["DELETE payment/customer/operationsreport"] = (t, ct) => t._paymentApi.TerminateCustomerOperationsReportAsync(ct),
        ["POST payment/customer/usage/report"] = (t, ct) =>
            t._paymentApi.CreateCustomerServiceUsageReportAsync(new CustomerServiceUsageReportRequestDto(), ct),
        ["GET payment/customer/usage/report"] = (t, ct) => t._paymentApi.GetCustomerServiceUsageReportAsync(ct),
        ["DELETE payment/customer/usage/report"] = (t, ct) => t._paymentApi.TerminateCustomerServiceUsageReportAsync(ct),
        ["POST payment/customer/usage/monthly/report"] = (t, ct) =>
            t._paymentApi.CreateCustomerMonthlyUsageReportAsync(new CustomerMonthlyUsageReportRequestDto(), ct),
        ["GET payment/customer/usage/monthly/report"] = (t, ct) => t._paymentApi.GetCustomerMonthlyUsageReportAsync(ct),
        ["DELETE payment/customer/usage/monthly/report"] = (t, ct) => t._paymentApi.TerminateCustomerMonthlyUsageReportAsync(ct),

        ["PUT payment/updatewallet"] = (t, ct) => t._paymentApi.UpdateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { ["storage"] = 100 }, ProductQuantityType.Add), ct),
        ["PUT payment/calculatewallet"] = (t, ct) => t._paymentApi.CalculateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { ["storage"] = 100 }, ProductQuantityType.Add), ct),
        ["POST payment/servicestate"] = (t, ct) =>
            t._paymentApi.ChangeTenantWalletServiceStateAsync(new ChangeWalletServiceStateRequestDto(TenantWalletService.AITools, true), ct),
        ["GET payment/servicessettings"] = (t, ct) => t._paymentApi.GetTenantWalletServiceSettingsAsync(ct),
        ["GET payment/activeservices"] = (t, ct) => t._paymentApi.GetActiveServicesAsync(ct),

        ["GET payment/accounting/currencies"] = (t, ct) => t.GetAccountingCurrenciesAsync(ct),
        ["GET payment/accounting/prices/{serviceName}"] = (t, ct) =>
            t._paymentApi.GetAccountingServicePricesAsync(PaymentServiceStub.CatalogueServiceName, true, ct),

        ["GET payment/servicelimit/{id}"] = (t, ct) => t._paymentApi.GetServiceLimitAsync(t._serviceLimitId, ct),
        ["GET payment/servicelimit/total"] = (t, ct) => t._paymentApi.GetTotalServiceLimitAsync(ct),
        ["GET payment/servicelimit/global"] = (t, ct) => t._paymentApi.GetGlobalServiceLimitAsync(ct),
        ["GET payment/servicelimit/participants"] = (t, ct) => t._paymentApi.GetParticipantServiceLimitsAsync(cancellationToken: ct),
        ["POST payment/servicelimit"] = (t, ct) => t._paymentApi.CreateServiceLimitAsync(
            new CreateServiceLimitRequestDto(ServiceLimitScope.Global, amountValue: 20, period: ServiceLimitPeriod.Day), ct),
        ["PUT payment/servicelimit"] = (t, ct) =>
            t._paymentApi.UpdateServiceLimitAsync(new UpdateServiceLimitRequestDto(t._serviceLimitId, amountValue: 75), ct),
        ["PUT payment/servicelimit/participants"] = (t, ct) => t._paymentApi.SetParticipantServiceLimitsAsync(
            new SetParticipantServiceLimitsRequestDto([t.Owner.Id], amountValue: 5, period: ServiceLimitPeriod.Day), ct),
        ["PUT payment/servicelimit/participants/disable"] = (t, ct) => t._paymentApi.DisableParticipantServiceLimitsAsync(ct),
        ["DELETE payment/servicelimit/{id}"] = (t, ct) => t._paymentApi.DeleteServiceLimitAsync(t._serviceLimitId, ct)
    };

    /// <summary>Every pairing of <paramref name="operations"/> with <paramref name="roles"/>.</summary>
    protected static TheoryData<string, EmployeeType> Matrix(string[] operations, EmployeeType[] roles)
    {
        var data = new TheoryData<string, EmployeeType>();

        foreach (var operation in operations)
        {
            foreach (var role in roles)
            {
                data.Add(operation, role);
            }
        }

        return data;
    }

    /// <summary>
    /// The owner, who is the payer, sends <paramref name="operation"/> and is not refused for who it is: whatever else
    /// the answer says, it is neither 401 nor 403. This is what tells a role check from a refusal of everybody.
    /// </summary>
    protected async Task AssertOwnerNotRefusedAsync(string operation)
    {
        await ArrangeAsync(operation, Owner.Email);

        await AssertNotRefusedAsync(operation, $"the owner pays for the portal and may call {operation}");
    }

    /// <summary>
    /// A DocSpace administrator who is not the owner sends <paramref name="operation"/> and is not refused for who it
    /// is. Next to the refusal of the roles below, this pins the guard at the administrator: a guard narrowed to the
    /// owner would leave every refusal test green.
    /// </summary>
    protected async Task AssertAdministratorNotRefusedAsync(string operation)
    {
        await ArrangeAsync(operation, Owner.Email);

        var administrator = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(administrator);

        await AssertNotRefusedAsync(operation, $"every DocSpace administrator may call {operation}, not only the owner");
    }

    /// <summary>
    /// A member of <paramref name="role"/> - a DocSpace administrator unless said otherwise - who is not the owner but
    /// pays for the portal sends <paramref name="operation"/> and is not refused for who it is: the guard is the payer,
    /// not the owner who usually is one, nor an administrator.
    /// </summary>
    protected async Task AssertPayerNotRefusedAsync(string operation, EmployeeType role = EmployeeType.DocSpaceAdmin)
    {
        var payer = await InviteMember(role);

        await ArrangeAsync(operation, payer.Email);
        await _webApiClient.Authenticate(payer);

        await AssertNotRefusedAsync(operation, $"the payer may call {operation}, whoever owns the portal and whatever the payer's role");
    }

    /// <summary>
    /// A member of <paramref name="role"/> who pays for the portal sends <paramref name="operation"/>, which asks for a
    /// DocSpace administrator as well, and is refused with 403 "Access denied".
    /// </summary>
    protected async Task AssertPayerForbiddenAsync(string operation, EmployeeType role)
    {
        var payer = await InviteMember(role);

        await ArrangeAsync(operation, payer.Email);
        await _webApiClient.Authenticate(payer);

        await AssertRefusedAsync(operation, $"{operation} asks for a DocSpace administrator as well as the payer, and {role} is none");
    }

    /// <summary>
    /// The owner sends <paramref name="operation"/> for a portal somebody else pays for, and is refused with 403
    /// "Access denied" like any other caller who is not the payer.
    /// </summary>
    protected async Task AssertOwnerWhoIsNotThePayerForbiddenAsync(string operation)
    {
        await ArrangeAsync(operation, ForeignPayer);

        await AssertRefusedAsync(operation, $"the owner does not pay for the portal and may not call {operation}");
    }

    /// <summary>
    /// The owner sends <paramref name="operation"/> for a portal somebody else pays for, and is not refused for who it is.
    /// </summary>
    protected async Task AssertOwnerWhoIsNotThePayerNotRefusedAsync(string operation)
    {
        await ArrangeAsync(operation, ForeignPayer);

        await AssertNotRefusedAsync(operation, $"the owner may call {operation} without paying for the portal");
    }

    /// <summary>
    /// A member of <paramref name="role"/> sends <paramref name="operation"/> and is refused with 403 "Access denied" - the
    /// answer of a role check, which a precondition refusing everybody with 403 does not give - and the spending limits
    /// of the portal are as they were before the request.
    /// </summary>
    protected async Task AssertForbiddenAsync(string operation, EmployeeType role)
    {
        // A customer with a wallet, paid for by the owner: everything before the role check passes.
        await ArrangeAsync(operation, Owner.Email);

        var member = await InviteMember(role);
        await _webApiClient.Authenticate(member);

        await AssertRefusedAsync(operation, $"{role} may not call {operation}");
    }

    // The portal is a customer whose payer has the e-mail payerEmail, with a wallet. An operation on the spending limits
    // also gets limits to work on, created by the owner: the one of the portal, which it reads, changes and deletes, and a
    // personal one switched on, which the participant operations replace or switch off. Its values differ from the ones
    // PUT payment/servicelimit/participants sets, or a refused request that wrote them would leave the snapshot as it was.
    private async Task ArrangeAsync(string operation, string payerEmail)
    {
        Payments.Customer(payerEmail).Balance(100m);

        if (operation.Contains("servicelimit", StringComparison.Ordinal))
        {
            await _webApiClient.Authenticate(Owner);

            _serviceLimitId = (await _paymentApi.CreateServiceLimitAsync(
                new CreateServiceLimitRequestDto(ServiceLimitScope.Total, amountValue: 50, period: ServiceLimitPeriod.Day),
                TestContext.Current.CancellationToken)).Response.Id;

            await _paymentApi.CreateServiceLimitAsync(
                new CreateServiceLimitRequestDto(ServiceLimitScope.Custom, userId: Owner.Id, amountValue: 7, period: ServiceLimitPeriod.Week),
                TestContext.Current.CancellationToken);
        }
    }

    // Whatever else the answer says, it is neither 401 nor 403.
    private async Task AssertNotRefusedAsync(string operation, string because)
    {
        try
        {
            await _requests[operation](this, TestContext.Current.CancellationToken);
        }
        catch (ApiException exception)
        {
            exception.ErrorCode.Should().NotBe(401).And.NotBe(403, because);
        }
    }

    // 403 "Access denied", and the spending limits of the portal as they were before the request.
    private async Task AssertRefusedAsync(string operation, string because)
    {
        var limitsBefore = PaymentServices.ServiceLimitsOf(Payments.AccountNumber);

        if (operation.Contains("servicelimit", StringComparison.Ordinal))
        {
            // An equal snapshot proves nothing about a refused change when there was nothing it could have changed.
            JsonNode.Parse(limitsBefore)!.AsArray().Should()
                .Contain(limit => (string?)limit!["scope"] == "Total")
                .And.Contain(limit => (string?)limit!["scope"] == "Custom" && (bool)limit!["enabled"]!);
        }

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _requests[operation](this, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403, because);
        exception.ErrorContent!.ToString().Should().Contain("Access denied", because);
        PaymentServices.ServiceLimitsOf(Payments.AccountNumber).Should().Be(limitsBefore, "a refused request writes nothing");
    }

    // GET payment/accounting/currencies is hidden from the OpenAPI document and so from the SDK: it is sent over raw HTTP
    // and fails as the SDK fails, so it runs through the same assertions as the other operations.
    private async Task GetAccountingCurrenciesAsync(CancellationToken cancellationToken)
    {
        using var response = await _webApiClient.GetAsync("api/2.0/portal/payment/accounting/currencies", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new ApiException((int)response.StatusCode, $"GET payment/accounting/currencies failed with {(int)response.StatusCode}", body);
        }
    }
}
