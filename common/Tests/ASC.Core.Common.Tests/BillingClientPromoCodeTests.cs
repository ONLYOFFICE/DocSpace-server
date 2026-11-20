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
/// Contract tests for <see cref="BillingClient.ApplyPromoCodeAsync"/>: the request to <c>billing/applypromocode</c>, the
/// bonus read from the answer, and the refusal of a code told apart from the other error answers.
/// </summary>
public class BillingClientPromoCodeTests
{
    [Fact]
    public async Task ApplyPromoCode_SendsPortalCodeAndInitiator_AndReadsTheCreditedBonus()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK,
            """{"Status":"completed","Amount":10.0000,"Currency":"USD","ExpiresAt":"2027-02-07T12:00:00Z"}"""));

        var result = await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", "owner@example.com");

        result.Status.Should().Be(PromoCodeStatus.Completed);
        result.Amount.Should().Be(10m);
        result.Currency.Should().Be("USD");
        result.ExpiresAt.Should().Be(new DateTime(2027, 2, 7, 12, 0, 0, DateTimeKind.Utc));

        handler.LastMethod.Should().Be(HttpMethod.Post);
        handler.LastUri!.PathAndQuery.Should().Be("/billing/applypromocode");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("PortalId").EnumerateArray().Select(e => e.GetString()).Should().Equal("portal-1");
        body.GetProperty("Code").EnumerateArray().Select(e => e.GetString()).Should().Equal("K7QX-M42P-9TRB");
        body.GetProperty("Initiator").EnumerateArray().Select(e => e.GetString()).Should().Equal("owner@example.com");
    }

    [Fact]
    public async Task ApplyPromoCode_PendingBonusWithoutExpiry_IsReadAsPendingAndNeverExpiring()
    {
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK,
            """{"Status":"pending","Amount":5,"Currency":"EUR","ExpiresAt":null}"""));

        var result = await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", "owner@example.com");

        result.Status.Should().Be(PromoCodeStatus.Pending);
        result.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task ApplyPromoCode_NoInitiator_LeavesTheFieldOutOfTheRequest()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK,
            """{"Status":"completed","Amount":10,"Currency":"USD","ExpiresAt":null}"""));

        await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", null);

        JsonDocument.Parse(handler.LastRequestBody!).RootElement.TryGetProperty("Initiator", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("""{"Message":"error: promo_not_found"}""", "promo_not_found")]
    [InlineData("""{"Message":"error: promo_expired","Code":"promo_expired"}""", "promo_expired")]
    [InlineData("""{"Message":"error: promo_not_found","Code":"promo_already_used"}""", "promo_already_used")]
    public async Task ApplyPromoCode_RefusedCode_ThrowsPromoCodeExceptionWithTheReasonCode(string answer, string code)
    {
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, answer));

        var act = async () => await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", "owner@example.com");

        (await act.Should().ThrowExactlyAsync<BillingPromoCodeException>()).Which.Code.Should().Be(code);
    }

    [Fact]
    public async Task ApplyPromoCode_BillingFailedOnItsSide_StaysABillingFailure()
    {
        // promo_failed is the billing service failing, not refusing: the code may have been redeemed, so the caller must
        // not be told it was refused.
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK,
            """{"Message":"error: promo_failed","Code":"promo_failed"}"""));

        var act = async () => await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", "owner@example.com");

        await act.Should().ThrowExactlyAsync<BillingException>();
    }

    [Fact]
    public async Task ApplyPromoCode_RefusalCutShort_StaysABillingFailure()
    {
        // A body cut short may have named promo_failed: what it would have said is unknown, so it is no refusal.
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, """{"Message":"error: promo_f"""));

        var act = async () => await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", "owner@example.com");

        (await act.Should().ThrowAsync<BillingException>()).Which.Should().NotBeOfType<BillingPromoCodeException>();
    }

    [Fact]
    public async Task ApplyPromoCode_RefusalShapeWithAFailingStatus_StaysABillingFailure()
    {
        // Only a successful answer refuses a code; a failing status leaves the outcome unknown.
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.BadGateway,
            """{"Message":"error: promo_not_found","Code":"promo_not_found"}"""));

        var act = async () => await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", "owner@example.com");

        (await act.Should().ThrowAsync<BillingException>()).Which.Should().NotBeOfType<BillingPromoCodeException>();
    }

    [Fact]
    public async Task ApplyPromoCode_AnswerWithoutStatus_IsAContractFailure()
    {
        // A bonus whose status the answer leaves out must not be read as credited.
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK,
            """{"Amount":10,"Currency":"USD","ExpiresAt":null}"""));

        var act = async () => await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", "owner@example.com");

        await act.Should().ThrowAsync<PaymentServiceContractException>();
    }

    [Fact]
    public async Task ApplyPromoCode_ErrorAboutTheRequest_StaysABillingFailure()
    {
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK,
            """{"Message":"error: the parameter 'Code' is missed"}"""));

        var act = async () => await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", "owner@example.com");

        (await act.Should().ThrowExactlyAsync<BillingException>()).Which.Message.Should().Contain("is missed");
    }

    [Fact]
    public async Task ApplyPromoCode_PortalBillingDoesNotKnow_ThrowsBillingNotFoundException()
    {
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK,
            """{"Message":"error: cannot find customer"}"""));

        var act = async () => await client.ApplyPromoCodeAsync("portal-1", "K7QX-M42P-9TRB", "owner@example.com");

        await act.Should().ThrowExactlyAsync<BillingNotFoundException>();
    }
}
