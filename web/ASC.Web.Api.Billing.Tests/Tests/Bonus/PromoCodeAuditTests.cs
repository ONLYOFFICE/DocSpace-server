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
/// The audit trail of a redeemed promo code (<c>POST payment/promoCode</c>): one event with the amount and currency of
/// the bonus, never the code, whether billing credits the bonus at once or later.
/// </summary>
public class PromoCodeAuditTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string ApplyPromoCodeRoute = "/billing/applypromocode";

    [Theory]
    [InlineData("completed")]
    [InlineData("pending")]
    public async Task ApplyPromoCode_BillingAcceptsTheCode_WritesOneAuditEventWithoutTheCode(string status)
    {
        // Arrange
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute,
            $$"""{"Status":"{{status}}","Amount":25,"Currency":"usd","ExpiresAt":null}""");

        // Act
        await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(PromoCode), TestContext.Current.CancellationToken);
        var events = await AuditWithSentinelAsync(events => events.Any(e => e.Action != SentinelAction));

        // Assert
        var audit = events.Should().ContainSingle(e => e.Action != SentinelAction).Subject;
        audit.Action.Should().Be("Promo code applied: 25 USD", "the code stays out: anyone who reads the trail could redeem a reusable one");
        (audit.Target ?? []).Should().NotContain(t => t.Contains(PromoCode));
    }

    // The amount is written as billing means it, whatever the scale billing sends it with and whatever the culture of
    // the caller: a Russian owner would otherwise read "10,5 USD", and an amount of 10.0000 "10.0000 USD".
    [Theory]
    [InlineData("10.0000", "en-US", "Promo code applied: 10 USD")]
    [InlineData("10.5", "ru", "Promo code applied: 10.5 USD")]
    public async Task ApplyPromoCode_AnyScaleAndCulture_WritesTheAmountInvariantly(string amount, string culture, string expected)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        await _profilesApi.UpdateMemberCultureAsync(Owner.Id.ToString(), new UpdateMemberCultureRequest(culture), TestContext.Current.CancellationToken);
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute,
            $$"""{"Status":"completed","Amount":{{amount}},"Currency":"usd","ExpiresAt":null}""");

        // Act
        await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(PromoCode), TestContext.Current.CancellationToken);
        var events = await AuditWithSentinelAsync(events => events.Any(e => e.Action != SentinelAction));

        // Assert
        events.Should().ContainSingle(e => e.Action != SentinelAction).Which.Action.Should().Be(expected);
    }

    [Fact]
    public async Task ApplyPromoCode_BillingRefusesTheCode_WritesNoAuditEvent()
    {
        // Arrange
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute, """{"Message":"error: promo_not_found"}""");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto(PromoCode), TestContext.Current.CancellationToken));
        var events = await AuditWithSentinelAsync(_ => true);

        // Assert: the refusal itself, so a request stopped earlier for another reason cannot pass for it.
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("promo_not_found");
        events.Should().ContainSingle().Which.Action.Should().Be(SentinelAction);
    }

    private const string SentinelAction = "Promo code applied: 7 USD";

    // The trail is written after the request returns, by writers that run in parallel, so its order is not that of the
    // requests. A code redeemed after the one under test is a marker: the trail is polled until the marker and what
    // `expected` waits for are both there, or 30 s pass. A copy written later than both is not caught.
    private async Task<List<AuditEventDto>> AuditWithSentinelAsync(Func<List<AuditEventDto>, bool> expected)
    {
        Payments.On(PaymentService.Billing, "POST", ApplyPromoCodeRoute,
            """{"Status":"completed","Amount":7,"Currency":"USD","ExpiresAt":null}""");
        await _paymentApi.ApplyPromoCodeAsync(new ApplyPromoCodeRequestDto("SENTINEL"), TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var events = ((await _auditTrailDataApi.GetAuditEventsByFilterAsync(action: MessageAction.CustomerPromoCodeApplied, cancellationToken: TestContext.Current.CancellationToken)).Response ?? [])
                .Where(e => e.ActionId == MessageAction.CustomerPromoCodeApplied)
                .ToList();

            if ((events.Any(e => e.Action == SentinelAction) && expected(events)) || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }
}
