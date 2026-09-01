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
/// The write half of the payment API surface whose role checks are masked in this environment —
/// see <see cref="PaymentReadEndpointsAnonymousAccessTests"/> for why. Every one of these actions
/// calls <c>PaymentHelper.DemandConfigured</c> (directly, or through
/// <c>DemandCustomerPayerAsync</c>/<c>EnsureCustomerAndAdminRightsAsync</c>) before it checks who
/// is calling, so only the authentication guard is left to verify: anonymous is refused with 401
/// regardless of the (deliberately minimal, since it is never reached) request body.
/// </summary>
[Trait("Category", "Portal")]
public class PaymentMutatingEndpointsAnonymousAccessTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetPaymentUrl_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetPaymentUrlAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UpdateWalletPayment_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.UpdateWalletPaymentAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CalculateWalletPayment_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.CalculateWalletPaymentAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task ChangeTenantWalletServiceState_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.ChangeTenantWalletServiceStateAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateCustomerOperationsReport_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.CreateCustomerOperationsReportAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task TerminateCustomerOperationsReport_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TerminateCustomerOperationsReportAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UpdatePayment_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.UpdatePaymentAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task TopUpDeposit_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TopUpDepositAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SetRestrictedAiModels_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetRestrictedAiModelsAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task TerminateCustomerMonthlyUsageReport_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TerminateCustomerMonthlyUsageReportAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateCustomerMonthlyUsageReport_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.CreateCustomerMonthlyUsageReportAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateCustomerServiceUsageReport_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.CreateCustomerServiceUsageReportAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task TerminateCustomerServiceUsageReport_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.TerminateCustomerServiceUsageReportAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task MoveSubscriptionToWallet_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.MoveSubscriptionToWalletAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SetTenantWalletSettings_Anonymous_ThrowsUnauthorized()
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetTenantWalletSettingsAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
