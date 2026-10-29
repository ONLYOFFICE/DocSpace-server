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
/// Contract tests for <see cref="BillingClient.GetInvoiceLinkAsync"/>: the request to <c>billing/GetInvoiceLink</c>,
/// the hosted invoice address read from the answer, and the error bodies a caller tells apart.
/// </summary>
public class BillingClientInvoiceLinkTests
{
    [Fact]
    public async Task GetInvoiceLink_SendsPortalAndPaymentRef_AndUnwrapsJsonString()
    {
        var (client, handler) = BillingClientTests.CreateClient(_ => BillingClientTests.Json(HttpStatusCode.OK,
            "\"https://invoice.stripe.com/i/acct_1L1C3n/test_YWNj\""));

        var link = await client.GetInvoiceLinkAsync("portal-1", "pi_3QabcDEF");

        link.Should().Be("https://invoice.stripe.com/i/acct_1L1C3n/test_YWNj");

        handler.LastMethod.Should().Be(HttpMethod.Post);
        handler.LastUri!.ToString().Should().Be("https://billing.example.com/billing/GetInvoiceLink");

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        body.GetProperty("PortalId").EnumerateArray().Select(e => e.GetString()).Should().Equal("portal-1");
        body.GetProperty("PaymentRef").EnumerateArray().Select(e => e.GetString()).Should().Equal("pi_3QabcDEF");
    }

    [Fact]
    public async Task GetInvoiceLink_CannotFindErrorBody_ThrowsBillingNotFoundException()
    {
        var (client, _) = BillingClientTests.CreateClient(_ => BillingClientTests.Json(HttpStatusCode.OK,
            """{"Message":"error: cannot find invoice"}"""));

        var act = async () => await client.GetInvoiceLinkAsync("portal-1", "pi_3QabcDEF");

        await act.Should().ThrowExactlyAsync<BillingNotFoundException>();
    }

    [Fact]
    public async Task GetInvoiceLink_OtherErrorBody_ThrowsBillingException()
    {
        var (client, _) = BillingClientTests.CreateClient(_ => BillingClientTests.Json(HttpStatusCode.OK,
            """{"Message":"error: stripe is not available"}"""));

        var act = async () => await client.GetInvoiceLinkAsync("portal-1", "pi_3QabcDEF");

        (await act.Should().ThrowExactlyAsync<BillingException>())
            .Which.Message.Should().Contain("stripe is not available");
    }
}
