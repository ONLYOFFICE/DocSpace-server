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


namespace ASC.Web.Api.Billing.Tests.Tests.GracePeriod;

/// <summary>
/// The money operations the grace period refuses: an add-on of the wallet (<c>PUT payment/updatewallet</c>), the
/// Docs Connect trial (<c>POST settings/docscloud/trial</c>) and the price of the move to Docs Connect Dev Pack
/// (<c>POST settings/docscloud/calculatedevpack</c>). A customer paying by SEPA passes the check of the grace period,
/// one paying by card gets 402 "Tariff is not paid" as before.
/// </summary>
/// <remarks>
/// The add-on is asked with <c>Set</c>, which charges nothing: past the check, a portal without the service gets
/// <c>false</c>. The renewal of add-ons and the backup schedule take the same decision in background services this
/// stack cannot drive; the unit test of <c>Tariff.IsRestricted</c> covers that decision.
/// </remarks>
public class GracePeriodPaymentsTests(AspireAppFixture fixture) : GracePeriodTestBase(fixture)
{
    private const string TrialRoute = "/billing/getwdocstrial";
    private const string CalculateSwitchRoute = "/billing/CalculateSwitchSubscription";

    [Fact]
    public async Task UpdateWalletPayment_AddOnInGraceWithSepa_PassesTheGracePeriod()
    {
        // Arrange
        await StartGracePeriodAsync(delayedPaymentMethod: true);

        // Act
        var updated = (await _paymentApi.UpdateWalletPaymentAsync(StorageNextQuantity(), TestContext.Current.CancellationToken)).Response;

        // Assert: past the check, the portal has no storage to change
        updated.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateWalletPayment_AddOnInGraceWithCard_ThrowsPaymentRequired()
    {
        // Arrange
        await StartGracePeriodAsync(delayedPaymentMethod: false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.UpdateWalletPaymentAsync(StorageNextQuantity(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("Tariff is not paid");
    }

    [Fact]
    public async Task StartDocsCloudTrial_GraceWithSepa_GrantsTheTrial()
    {
        // Arrange
        await StartGracePeriodAsync(delayedPaymentMethod: true);
        Payments.On(PaymentService.Billing, "POST", TrialRoute, "\"ok\"");

        // Act
        var granted = (await _docsCloudApi.StartDocsCloudTrialAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        granted.Should().BeTrue();
        PaymentCalls.Should().ContainSingle(call => call.Route == TrialRoute);
    }

    [Fact]
    public async Task StartDocsCloudTrial_GraceWithCard_ThrowsPaymentRequiredWithoutAskingBilling()
    {
        // Arrange
        await StartGracePeriodAsync(delayedPaymentMethod: false);
        Payments.On(PaymentService.Billing, "POST", TrialRoute, "\"ok\"");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.StartDocsCloudTrialAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("Tariff is not paid");
        PaymentCalls.Should().NotContain(call => call.Route == TrialRoute);
    }

    [Fact]
    public async Task CalculateDevPack_GraceWithSepa_ReturnsTheSwitchBillingCalculates()
    {
        // Arrange
        await StartDocsCloudGracePeriodAsync(delayedPaymentMethod: true);

        // Act
        var calculation = (await _docsCloudApi.CalculateDevPackAsync(new DocsCloudDevPackRequestDto(12), TestContext.Current.CancellationToken)).Response;

        // Assert
        calculation.OperationId.Should().Be(77);
        PaymentCalls.Should().ContainSingle(call => call.Route == CalculateSwitchRoute);
    }

    [Fact]
    public async Task CalculateDevPack_GraceWithCard_ThrowsPaymentRequiredWithoutAskingBilling()
    {
        // Arrange
        await StartDocsCloudGracePeriodAsync(delayedPaymentMethod: false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.CalculateDevPackAsync(new DocsCloudDevPackRequestDto(12), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("Tariff is not paid");
        PaymentCalls.Should().NotContain(call => call.Route == CalculateSwitchRoute);
    }

    private static WalletQuantityRequestDto StorageNextQuantity()
    {
        return new WalletQuantityRequestDto(new Dictionary<string, int?> { ["storage"] = 100 }, ProductQuantityType.Set);
    }

    // The admin plan lapsed two days ago while Docs Connect is paid for another month: the tariff is in the grace period
    // and the portal has the Docs Connect subscription the move starts from.
    private async Task StartDocsCloudGracePeriodAsync(bool delayedPaymentMethod)
    {
        Payments.Paid(Plans.DocsCloudProductId, quantity: 5, DateTime.UtcNow.Date.AddMonths(1), Owner.Email)
            .On(PaymentService.Billing, "POST", CalculateSwitchRoute, """{"OperationId":77,"Amount":42.5,"Currency":"USD","Quantity":12}""");
        await StartGracePeriodAsync(delayedPaymentMethod);
    }
}
