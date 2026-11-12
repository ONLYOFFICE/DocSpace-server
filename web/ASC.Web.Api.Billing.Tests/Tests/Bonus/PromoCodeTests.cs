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

namespace ASC.Web.Api.Billing.Tests.Tests.Bonus;

/// <summary>
/// Redeeming a promo code (<c>POST payment/promoCode</c>): the code goes to billing, which credits its bonus at once or
/// later, and a code billing refuses comes back with billing's reason code for the client to show its own text.
/// </summary>
public class PromoCodeTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ApplyPromoCodeRoute = "/billing/applypromocode";

    [Fact]
    public async Task ApplyPromoCode_BillingCreditsTheBonus_ReturnsItAndSendsTheCodeWithTheInitiator()
    {
        // Arrange
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute,
            """{"Status":"completed","Amount":10.0000,"Currency":"USD","ExpiresAt":"2027-02-07T12:00:00Z"}""");

        // Act
        var bonus = (await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(PromoCode), TestContext.Current.CancellationToken)).Response;

        // Assert
        bonus.Status.Should().Be(PromoCodeStatus.Completed);
        bonus.Amount.Should().Be(10);
        bonus.Currency.Should().Be("USD");
        bonus.ExpiresAt!.Value.ToUniversalTime().Should().Be(new DateTime(2027, 2, 7, 12, 0, 0, DateTimeKind.Utc));

        var call = PaymentCalls.Should().ContainSingle(c => c.Route == ApplyPromoCodeRoute).Subject;
        BillingValue(call, "Code").Should().Be(PromoCode);
        BillingValue(call, "Initiator").Should().Be(Owner.Email);
    }

    [Fact]
    public async Task ApplyPromoCode_CurrencyInLowerCase_IsPassedOnInUpperCase()
    {
        // Arrange
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute,
            """{"Status":"completed","Amount":10,"Currency":"usd","ExpiresAt":null}""");

        // Act
        var bonus = (await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(PromoCode), TestContext.Current.CancellationToken)).Response;

        // Assert
        bonus.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task ApplyPromoCode_CodeWithSurroundingSpaces_SendsItTrimmed()
    {
        // Arrange
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute,
            """{"Status":"completed","Amount":10,"Currency":"USD","ExpiresAt":null}""");

        // Act
        await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto($"  {PromoCode}  "), TestContext.Current.CancellationToken);

        // Assert
        var call = PaymentCalls.Should().ContainSingle(c => c.Route == ApplyPromoCodeRoute).Subject;
        BillingValue(call, "Code").Should().Be(PromoCode);
    }

    [Fact]
    public async Task ApplyPromoCode_BillingCreditsTheBonusLater_ReturnsPendingWithoutExpiry()
    {
        // Arrange
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute,
            """{"Status":"pending","Amount":5,"Currency":"USD","ExpiresAt":null}""");

        // Act
        var bonus = (await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(PromoCode), TestContext.Current.CancellationToken)).Response;

        // Assert
        bonus.Status.Should().Be(PromoCodeStatus.Pending);
        bonus.Amount.Should().Be(5);
        bonus.ExpiresAt.Should().BeNull();
    }

    [Theory]
    [InlineData("""{"Message":"error: promo_not_found"}""", "promo_not_found")]
    [InlineData("""{"Message":"error: promo_already_used","Code":"promo_already_used"}""", "promo_already_used")]
    public async Task ApplyPromoCode_BillingRefusesTheCode_ThrowsBadRequestWithTheReasonCode(string answer, string reason)
    {
        // Arrange
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute, answer);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(PromoCode), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain(reason);
        PaymentCalls.Count(c => c.Route == ApplyPromoCodeRoute).Should().Be(1, "a refused code is reported, not tried again");
    }

    [Fact]
    public async Task ApplyPromoCode_BillingDoesNotKnowThePortal_ThrowsPaymentRequired()
    {
        // Act: nothing is set for the portal, so billing answers "cannot find" as it does for an unknown portal.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(PromoCode), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("cannot find");
        PaymentCalls.Count(c => c.Route == ApplyPromoCodeRoute).Should().Be(1);
    }

    [Fact]
    public async Task ApplyPromoCode_BillingFailsOnItsSide_ThrowsPaymentRequiredNotARefusal()
    {
        // Arrange: promo_failed comes in the shape of a refusal, but the code may have been redeemed all the same.
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute,
            """{"Message":"error: promo_failed","Code":"promo_failed"}""");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(PromoCode), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("promo_failed");
        PaymentCalls.Count(c => c.Route == ApplyPromoCodeRoute).Should().Be(1);
    }

    [Fact]
    public async Task ApplyPromoCode_EmptyCode_ThrowsBadRequestWithoutAskingBilling()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(""), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        PaymentCalls.Should().NotContain(c => c.Route == ApplyPromoCodeRoute);
    }

    [Fact]
    public async Task ApplyPromoCode_CodeLongerThanHundredCharacters_ThrowsBadRequestWithoutAskingBilling()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(new string('A', 101)), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        PaymentCalls.Should().NotContain(c => c.Route == ApplyPromoCodeRoute);
    }
}
