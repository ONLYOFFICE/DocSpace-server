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
public abstract class PaymentPermissionsTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>Every role but the owner, who is the payer: refused by the operations for the payer.</summary>
    protected static EmployeeType[] NotThePayer => [EmployeeType.DocSpaceAdmin, .. NonAdministrators];

    /// <summary>The subscription: changing the plan, its balance, moving it to the wallet, the billing account page.</summary>
    protected static string[] SubscriptionPayerOperations =>
    [
        "PUT payment/update",
        "GET payment/subscription/balance",
        "POST payment/subscription/movetowallet",
        "GET payment/account"
    ];

    /// <summary>The wallet as the payer keeps it: topping it up, its automatic top-up, attaching a payment method.</summary>
    protected static string[] WalletPayerOperations =>
    [
        "POST payment/deposit",
        "POST payment/topupsettings",
        "GET payment/checkoutsetupurl"
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

    /// <summary>The wallet services: buying them, the price of buying them, switching them on and off.</summary>
    protected static string[] WalletAdministratorOperations =>
    [
        "PUT payment/updatewallet",
        "PUT payment/calculatewallet",
        "POST payment/servicestate",
        "GET payment/servicessettings"
    ];

    // One valid request per operation, as the owner would send it; the TypeScript suite's requests where it has them.
    private static readonly Dictionary<string, Func<PaymentApi, CancellationToken, Task>> _requests = new()
    {
        ["PUT payment/update"] = (api, ct) => api.UpdatePaymentAsync(new QuantityRequestDto(new Dictionary<string, int> { ["admin"] = 1 }), ct),
        ["POST payment/deposit"] = (api, ct) => api.TopUpDepositAsync(new TopUpDepositRequestDto(100, "USD"), ct),
        ["GET payment/subscription/balance"] = (api, ct) => api.GetSubscriptionBalanceInfoAsync(ct),
        ["POST payment/subscription/movetowallet"] = (api, ct) =>
            api.MoveSubscriptionToWalletAsync(new QuantityRequestDto(new Dictionary<string, int> { ["admin"] = 1 }), ct),
        ["GET payment/account"] = (api, ct) => api.GetPaymentAccountAsync("https://portal.example.com/back", ct),
        ["GET payment/checkoutsetupurl"] = (api, ct) =>
            api.GetCheckoutSetupUrlAsync("https://portal.example.com/back", "https://portal.example.com/success", ct),
        ["POST payment/topupsettings"] = (api, ct) => api.SetTenantWalletSettingsAsync(
            new TenantWalletSettingsRequestDto(new SetWalletTopUpSettingsRequest(enabled: true, minBalance: 10, upToBalance: 50, currency: "USD")), ct),

        ["PUT payment/updatewallet"] = (api, ct) => api.UpdateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { ["storage"] = 100 }, ProductQuantityType.Add), ct),
        ["PUT payment/calculatewallet"] = (api, ct) => api.CalculateWalletPaymentAsync(
            new WalletQuantityRequestDto(new Dictionary<string, int?> { ["storage"] = 100 }, ProductQuantityType.Add), ct),
        ["POST payment/servicestate"] = (api, ct) =>
            api.ChangeTenantWalletServiceStateAsync(new ChangeWalletServiceStateRequestDto(TenantWalletService.AITools, true), ct),
        ["GET payment/servicessettings"] = (api, ct) => api.GetTenantWalletServiceSettingsAsync(ct),

        ["GET payment/customer/usage"] = (api, ct) => api.GetCustomerServiceUsageAsync(cancellationToken: ct),
        ["GET payment/customer/usage/monthly"] = (api, ct) => api.GetCustomerMonthlyUsageAsync(cancellationToken: ct),
        ["POST payment/customer/operationsreport"] = (api, ct) => api.CreateCustomerOperationsReportAsync(
            new CustomerOperationsReportRequestDto(serviceName: ["ai-tools"], credit: true, debit: true), ct),
        ["GET payment/customer/operationsreport"] = (api, ct) => api.GetCustomerOperationsReportAsync(ct),
        ["DELETE payment/customer/operationsreport"] = (api, ct) => api.TerminateCustomerOperationsReportAsync(ct),
        ["POST payment/customer/usage/report"] = (api, ct) => api.CreateCustomerServiceUsageReportAsync(new CustomerServiceUsageReportRequestDto(), ct),
        ["GET payment/customer/usage/report"] = (api, ct) => api.GetCustomerServiceUsageReportAsync(ct),
        ["DELETE payment/customer/usage/report"] = (api, ct) => api.TerminateCustomerServiceUsageReportAsync(ct),
        ["POST payment/customer/usage/monthly/report"] = (api, ct) =>
            api.CreateCustomerMonthlyUsageReportAsync(new CustomerMonthlyUsageReportRequestDto(), ct),
        ["GET payment/customer/usage/monthly/report"] = (api, ct) => api.GetCustomerMonthlyUsageReportAsync(ct),
        ["DELETE payment/customer/usage/monthly/report"] = (api, ct) => api.TerminateCustomerMonthlyUsageReportAsync(ct)
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
        Payments.Customer(Owner.Email).Balance(100m);

        await AssertNotRefusedAsync(operation, $"the owner pays for the portal and may call {operation}");
    }

    /// <summary>
    /// A DocSpace administrator who is not the owner sends <paramref name="operation"/> and is not refused for who it
    /// is. Next to the refusal of the roles below, this pins the guard at the administrator: a guard narrowed to the
    /// owner would leave every refusal test green.
    /// </summary>
    protected async Task AssertAdministratorNotRefusedAsync(string operation)
    {
        Payments.Customer(Owner.Email).Balance(100m);

        var administrator = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(administrator);

        await AssertNotRefusedAsync(operation, $"every DocSpace administrator may call {operation}, not only the owner");
    }

    // Whatever else the answer says, it is neither 401 nor 403.
    private async Task AssertNotRefusedAsync(string operation, string because)
    {
        try
        {
            await _requests[operation](_paymentApi, TestContext.Current.CancellationToken);
        }
        catch (ApiException exception)
        {
            exception.ErrorCode.Should().NotBe(401).And.NotBe(403, because);
        }
    }

    /// <summary>A member of <paramref name="role"/> sends <paramref name="operation"/> and is refused with 403.</summary>
    protected async Task AssertForbiddenAsync(string operation, EmployeeType role)
    {
        // A customer with a wallet, paid for by the owner: everything before the role check passes.
        Payments.Customer(Owner.Email).Balance(100m);

        var member = await InviteMember(role);
        await _webApiClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _requests[operation](_paymentApi, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403, $"{role} may not call {operation}");
    }
}
