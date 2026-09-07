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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Payments;

/// <summary>
/// The read half of the payment API surface whose role checks are masked in this environment:
/// each of these actions calls <c>PaymentHelper.DemandConfigured</c> (directly, or through
/// <c>DemandCustomerPayerAsync</c>/<c>EnsureCustomerAndAdminRightsAsync</c>) before it ever checks
/// who is calling, and this environment has no billing/wallet service configured
/// (<c>payment.url</c>/<c>docscloud.url</c> are empty — see
/// <c>PaymentHelper.DemandConfigured</c>). That throws <see cref="InvalidOperationException"/>,
/// which the unhandled-exception middleware maps to 403 "Tariff service is not configured" for
/// every caller, Owner included — so the TS suite's per-role 403 assertions ("Access denied")
/// cannot be reproduced here; only the authentication guard, which runs before the controller is
/// ever reached, still proves what it always proves: anonymous is refused with 401.
/// </summary>
[Trait("Category", "Portal")]
public class PaymentReadEndpointsAnonymousAccessTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetCustomerOperations_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCustomerOperationsAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCustomerServiceUsage_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCustomerServiceUsageAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCustomerOperationsReport_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCustomerOperationsReportAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetPaymentAccount_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentAccountAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCustomerInfo_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCustomerInfoAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCustomerBalance_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCustomerBalanceAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCheckoutSetupUrl_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCheckoutSetupUrlAsync(
                "https://example.com", "https://example.com", TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetTenantWalletServiceSettings_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetTenantWalletServiceSettingsAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetRestrictedAiModels_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetRestrictedAiModelsAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetAiPrices_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetAiPricesAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetSubscriptionBalanceInfo_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetSubscriptionBalanceInfoAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCustomerMonthlyUsage_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCustomerMonthlyUsageAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCustomerMonthlyUsageReport_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCustomerMonthlyUsageReportAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetCustomerServiceUsageReport_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCustomerServiceUsageReportAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
