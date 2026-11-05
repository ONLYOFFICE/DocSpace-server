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
/// Contract tests for the Refit-backed <see cref="BillingClient"/>: the client talks to an <see cref="HttpStub"/>
/// over loopback, and the tests assert the request line, the multimap JSON body wire format, the HMAC
/// authorization header, the 200-with-error-body exception mapping and the refresh-only retry behaviour.
/// </summary>
public class BillingClientTests
{
    [Fact]
    public async Task GetCurrentPayments_BuildsExpectedUrlAndBody()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK,
            """[{"EndDate":"2024-01-15T10:30:00Z","PaymentEmail":"user@example.com","PaymentId":1,"PaymentStatus":2,"ProductId":3,"Quantity":4}]"""));

        var payments = await client.GetCurrentPaymentsAsync("portal-1", false);

        handler.LastMethod.Should().Be(HttpMethod.Post);
        handler.LastUri!.AbsolutePath.Should().Be("/billing/GetActiveResources");
        handler.LastContentType.Should().Be("application/json; charset=utf-8");

        var body = JsonDocument.Parse(handler.LastRequestBody!);
        body.RootElement.GetProperty("PortalId")[0].GetString().Should().Be("portal-1");

        payments.Should().ContainSingle();
        payments[0].PaymentEmail.Should().Be("user@example.com");
        payments[0].PaymentStatus.Should().Be(2);
        payments[0].ProductId.Should().Be(3);
    }

    [Fact]
    public async Task BaseUrlWithSubPathAndTrailingSlash_PreservesSubPath()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, "[]"), path: "/sub/");

        await client.GetPaymentsAsync("portal-1");

        handler.LastUri!.AbsolutePath.Should().Be("/sub/billing/GetPayments");
    }

    [Fact]
    public async Task GetPaymentUrl_BuildsMultimapBodyWithOptionalParameters()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, "\"https://pay.example.com/checkout\""));

        var url = await client.GetPaymentUrlAsync(
            "portal-1",
            ["product-1", "product-2", "product-1"],
            campaign: "summer",
            currency: "EUR",
            backUrl: "https://portal/back",
            successUrl: "https://portal/success");

        url.Should().Be("https://pay.example.com/checkout");
        handler.LastUri!.AbsolutePath.Should().Be("/billing/GetSinglePaymentUrl");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("PortalId")[0].GetString().Should().Be("portal-1");
        // Duplicates are removed, order is preserved, and repeated keys become a multi-value array.
        body.GetProperty("ProductId").EnumerateArray().Select(e => e.GetString()).Should().Equal("product-1", "product-2");
        body.GetProperty("PaymentSystemId")[0].GetString().Should().Be("9");
        body.GetProperty("campaign")[0].GetString().Should().Be("summer");
        body.GetProperty("Currency")[0].GetString().Should().Be("EUR");
        // BackRef is the after-payment redirect (successUrl), ShopUrl is the cancel redirect (backUrl).
        body.GetProperty("BackRef")[0].GetString().Should().Be("https://portal/success");
        body.GetProperty("ShopUrl")[0].GetString().Should().Be("https://portal/back");
        // Omitted optional parameters must not be sent at all.
        body.TryGetProperty("AffiliateId", out _).Should().BeFalse();
        body.TryGetProperty("PartnerId", out _).Should().BeFalse();
        body.TryGetProperty("Language", out _).Should().BeFalse();
        body.TryGetProperty("CustomerEmail", out _).Should().BeFalse();
        body.TryGetProperty("Quantity", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetDocsCloudTrial_SendsCustomerDetailsAndOmitsEmptyOnes()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, "\"ok\""));

        var customer = new DocsCloudTrialCustomer(
            "admin@example.com", "https://portal.example.com", "John Smith", null, "en", "");
        var result = await client.GetDocsCloudTrialAsync("portal-1", customer);

        result.Should().BeTrue();
        handler.LastUri!.AbsolutePath.Should().Be("/billing/getwdocstrial");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("PortalId")[0].GetString().Should().Be("portal-1");
        body.GetProperty("Email")[0].GetString().Should().Be("admin@example.com");
        body.GetProperty("PortalLink")[0].GetString().Should().Be("https://portal.example.com");
        body.GetProperty("Name")[0].GetString().Should().Be("John Smith");
        body.GetProperty("LanguageCode")[0].GetString().Should().Be("en");
        // Absent optional details must not be sent at all.
        body.TryGetProperty("Phone", out _).Should().BeFalse();
        body.TryGetProperty("AffiliateId", out _).Should().BeFalse();
    }

    [Fact]
    public async Task TopUpDeposit_SendsInvariantAmountAndMetadataAsJsonString()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, "\"ok\""));

        var metadata = new Dictionary<string, string> { { "details", "Auto top-up" } };
        var result = await client.TopUpDepositAsync("portal-1", 1234.56m, "USD", "participant", "site", metadata);

        result.Should().BeTrue();
        handler.LastUri!.AbsolutePath.Should().Be("/billing/Deposit");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("Amount")[0].GetString().Should().Be("1234.56");
        body.GetProperty("Currency")[0].GetString().Should().Be("USD");
        body.GetProperty("CustomerParticipantName")[0].GetString().Should().Be("participant");
        body.GetProperty("SiteName")[0].GetString().Should().Be("site");
        // Metadata is sent as a JSON-serialized dictionary string, not as a nested object.
        body.GetProperty("Metadata")[0].GetString().Should().Be("""{"details":"Auto top-up"}""");
    }

    [Fact]
    public async Task TopUpDeposit_ReturnsFalse_WhenResponseIsNotOk()
    {
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, "\"pending\""));

        var result = await client.TopUpDepositAsync("portal-1", 10m, "USD", null, null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePayment_SendsQuantitiesAndQuantityType()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, "true"));

        var changed = await client.ChangePaymentAsync("portal-1", ["product-1", "product-2"], [5, 10],
            (ProductQuantityType)1, "USD", "participant");

        changed.Should().BeTrue();
        handler.LastUri!.AbsolutePath.Should().Be("/billing/ChangeSubscription");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("ProductId").EnumerateArray().Select(e => e.GetString()).Should().Equal("product-1", "product-2");
        body.GetProperty("ProductQty").EnumerateArray().Select(e => e.GetString()).Should().Equal("5", "10");
        body.GetProperty("ProductQuantityType")[0].GetString().Should().Be("1");
        body.GetProperty("Currency")[0].GetString().Should().Be("USD");
    }

    [Fact]
    public async Task GetProductPriceInfo_Wallet_OmitsPortalIdAndUnwrapsEnvelope()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK,
            """{"11":{"product-1":{"USD":10.5}}}"""));

        var prices = await client.GetProductPriceInfoAsync(null, wallet: true, ["product-1", "product-2"]);

        handler.LastUri!.AbsolutePath.Should().Be("/billing/GetProductsPrices");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.TryGetProperty("PortalId", out _).Should().BeFalse();
        body.GetProperty("PaymentSystemId")[0].GetString().Should().Be("11");

        prices.Should().HaveCount(2);
        prices["product-1"].Should().ContainKey("USD").WhoseValue.Should().Be(10.5m);
        prices["product-2"].Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentPayments_FiltersStatus4_WhenNotTest()
    {
        const string payments = """[{"PaymentId":1,"PaymentStatus":4},{"PaymentId":2,"PaymentStatus":1}]""";

        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, payments));
        var (testClient, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, payments), test: true);

        (await client.GetCurrentPaymentsAsync("portal-1", false)).Should().ContainSingle()
            .Which.PaymentId.Should().Be(2);
        (await testClient.GetCurrentPaymentsAsync("portal-1", false)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Requests_IncludeValidHmacAuthorizationHeader()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, "\"ok\""));

        await client.GetDocsCloudTrialAsync("portal-1", null);

        handler.LastAuthorization.Should().NotBeNull();

        var token = handler.LastAuthorization!;
        token.Should().StartWith($"ASC {Key}:");

        var parts = token["ASC ".Length..].Split(':');
        parts.Should().HaveCount(3);
        parts[0].Should().Be(Key);

        var timestamp = parts[1];
        timestamp.Should().MatchRegex(@"^\d{14}$");

        // The signature must be the HMAC-SHA1 of "{timestamp}\n{key}" keyed by the secret.
        token.Should().Be(ExpectedToken(Key, Secret, timestamp));
    }

    [Fact]
    public async Task GetAccountLink_UnwrapsJsonStringResponse()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, "\"https://billing.example.com/account\""));

        var link = await client.GetAccountLinkAsync("portal-1", "https://portal/back");

        link.Should().Be("https://billing.example.com/account");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("BackRef")[0].GetString().Should().Be("https://portal/back");
    }

    [Fact]
    public async Task CannotFindErrorBody_ThrowsBillingNotFoundException_WithoutRetry()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, """{"Message":"error: cannot find portal"}"""));

        var act = async () => await client.GetPaymentsAsync("portal-1");

        (await act.Should().ThrowExactlyAsync<BillingNotFoundException>())
            .Which.Message.Should().Contain("cannot find portal");
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task OtherErrorBody_ThrowsBillingException_CaseInsensitive()
    {
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, """{"message":"ERROR: something went wrong"}"""));

        var act = async () => await client.GetPaymentsAsync("portal-1");

        (await act.Should().ThrowExactlyAsync<BillingException>())
            .Which.Message.Should().Contain("something went wrong");
    }

    [Fact]
    public async Task TransportFailure_IsMappedToBillingTransportException()
    {
        // A transport failure never reaches ExceptionFactory, which only sees HTTP responses; the
        // TransportExceptionFactory keeps it inside the BillingException hierarchy instead of leaking
        // Refit.ApiRequestException to the callers. It gets its own type because no answer is not a
        // rejection: a timed-out request may still have been applied on the billing side.
        var (client, _) = CreateBillingClient(HttpStub.Unreachable());

        var act = async () => await client.GetPaymentsAsync("portal-1");

        (await act.Should().ThrowExactlyAsync<BillingTransportException>())
            .WithInnerException<HttpRequestException>();
    }

    [Fact]
    public async Task UnreadableAnswer_IsMappedToBillingException()
    {
        // An answer that is not the JSON the call expects - an HTML page from a proxy, a changed contract - is a billing
        // failure like any other, and must stay inside the hierarchy the callers catch rather than leak Refit's own.
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, "<html>Bad Gateway</html>"));

        var act = async () => await client.GetPaymentsAsync("portal-1");

        (await act.Should().ThrowExactlyAsync<BillingException>())
            .WithInnerException<JsonException>();
    }

    [Fact]
    public async Task EmptyResponseBody_ThrowsBillingNotConfiguredException()
    {
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, ""));

        var act = async () => await client.GetPaymentsAsync("portal-1");

        await act.Should().ThrowExactlyAsync<BillingNotConfiguredException>();
    }

    [Fact]
    public async Task NonSuccessStatus_ThrowsBillingExceptionWithStatusAndBody()
    {
        var (client, _) = CreateBillingClient(_ => Reply(HttpStatusCode.InternalServerError, "boom"));

        var act = async () => await client.GetPaymentsAsync("portal-1");

        (await act.Should().ThrowExactlyAsync<BillingException>())
            .Which.Message.Should().Contain("InternalServerError").And.Contain("boom");
    }

    [Fact]
    public async Task GetCurrentPayments_WithRefresh_RetriesOnCannotFind_ThenSucceeds()
    {
        var calls = 0;
        var (client, handler) = CreateBillingClient(_ =>
        {
            Interlocked.Increment(ref calls);
            return calls == 1
                ? Reply(HttpStatusCode.OK, """{"Message":"error: cannot find portal"}""")
                : Reply(HttpStatusCode.OK, """[{"PaymentId":1,"PaymentStatus":1}]""");
        });

        var payments = await client.GetCurrentPaymentsAsync("portal-1", true);

        payments.Should().ContainSingle();
        handler.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetCurrentPayments_WithRefresh_ExhaustsRetries_ThenThrows()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, """{"Message":"error: cannot find portal"}"""));

        var act = async () => await client.GetCurrentPaymentsAsync("portal-1", true);

        await act.Should().ThrowExactlyAsync<BillingNotFoundException>();
        handler.CallCount.Should().Be(3); // initial call + 2 retries
    }

    [Fact]
    public async Task GetCurrentPayments_WithoutRefresh_IsNotRetried()
    {
        var (client, handler) = CreateBillingClient(_ => Reply(HttpStatusCode.OK, """{"Message":"error: cannot find portal"}"""));

        var act = async () => await client.GetCurrentPaymentsAsync("portal-1", false);

        await act.Should().ThrowExactlyAsync<BillingNotFoundException>();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task NotConfigured_ThrowsBillingNotConfiguredException_WithoutSendingRequest()
    {
        var client = BuildProvider().GetRequiredService<BillingClient>();

        client.Configured.Should().BeFalse();

        var act = async () => await client.GetPaymentsAsync("portal-1");

        await act.Should().ThrowExactlyAsync<BillingNotConfiguredException>();
    }
}
