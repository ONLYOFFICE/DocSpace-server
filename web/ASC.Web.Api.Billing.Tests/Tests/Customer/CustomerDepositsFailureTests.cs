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


namespace ASC.Web.Api.Billing.Tests.Tests.Customer;

/// <summary>
/// The top-ups of the wallet (<c>GET payment/customer/deposits</c>) and the invoice of one
/// (<c>GET payment/customer/deposits/{id}/invoice</c>) when accounting fails: both answer 402, so a client can tell a
/// failure from a portal without top-ups.
/// </summary>
/// <remarks>Suspected defect W12 of the billing spec: the list answers an empty result instead.</remarks>
public class CustomerDepositsFailureTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string Failure = """{"title":"Internal Server Error","status":500}""";

    [Fact]
    [Trait("Category", "Bug")]
    public async Task GetCustomerDeposits_AccountingFails_ThrowsPaymentRequired()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m)
            .On(PaymentService.Accounting, "GET", "/customer/{portalId}/deposits", Failure, HttpStatusCode.InternalServerError);

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _paymentApi.GetCustomerDepositsAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        PaymentCalls.Should().Contain(call => call.Route == "/customer/{portalId}/deposits", "the list is read from accounting");
        exception.Should().BeOfType<ApiException>("a failed read is not a portal without top-ups")
            .Which.ErrorCode.Should().Be(402);
    }

    [Fact]
    public async Task GetDepositInvoice_AccountingFails_ThrowsPaymentRequired()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m)
            .On(PaymentService.Accounting, "GET", "/customer/{portalId}/deposits", Failure, HttpStatusCode.InternalServerError);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetDepositInvoiceAsync(15342, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        PaymentCalls.Should().Contain(call => call.Route == "/customer/{portalId}/deposits");
    }
}
