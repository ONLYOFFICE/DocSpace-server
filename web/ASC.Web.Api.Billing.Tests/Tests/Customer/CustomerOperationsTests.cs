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
/// The money movements on the wallet (<c>GET payment/customer/operations</c>) and its top-ups
/// (<c>GET payment/customer/deposits</c>): a page read from accounting for a billing customer.
/// </summary>
public class CustomerOperationsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetCustomerOperations_Customer_ReturnsThePageAccountingGives()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m)
            .On(PaymentService.Accounting, "GET", "/customer/{portalId}/operations",
                """{"collection":[{"date":"2026-10-20T10:00:00Z","service":"ai-tools","description":"Chat","quantity":3,"currency":"USD","credit":0,"debit":1.5}],"offset":5,"limit":10,"totalQuantity":6,"totalPage":1,"currentPage":1}""");

        // Act
        var report = (await _paymentApi.GetCustomerOperationsAsync(offset: 5, limit: 10, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        report.TotalQuantity.Should().Be(6);
        var operation = report.Collection.Should().ContainSingle().Subject;
        operation.Service.Should().Be("ai-tools");
        operation.Quantity.Should().Be(3);
        operation.Debit.Should().Be(1.5);

        var request = PaymentCalls.Should().ContainSingle(call => call.Route == "/customer/{portalId}/operations").Subject;
        var query = HttpUtility.ParseQueryString(request.Query);
        query["offset"].Should().Be("5");
        query["limit"].Should().Be("10");
    }

    [Fact]
    public async Task GetCustomerOperations_PortalIsNotACustomer_ReturnsNothingWithoutAskingAccounting()
    {
        // Act
        var report = (await _paymentApi.GetCustomerOperationsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        report.Should().BeNull();
        PaymentCalls.Should().NotContain(call => call.Route == "/customer/{portalId}/operations");
    }

    [Fact]
    public async Task GetCustomerDeposits_Customer_ReturnsTheTopUpsWithTheirInvoices()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m)
            .On(PaymentService.Accounting, "GET", "/customer/{portalId}/deposits",
                """{"collection":[{"id":15342,"date":"2026-10-23T11:13:53Z","sum":40.0,"currency":"USD","status":"Completed","paymentRef":"pi_3QabcDEF"},{"id":15343,"date":"2026-10-24T11:13:53Z","sum":20.0,"currency":"USD","status":"Completed","paymentRef":"ch_3QabcDEF"}],"offset":0,"limit":25,"totalQuantity":2,"totalPage":1,"currentPage":1}""");

        // Act
        var report = (await _paymentApi.GetCustomerDepositsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        report.TotalQuantity.Should().Be(2);
        report.Collection.Select(d => (d.Id, d.Amount, d.HasInvoice)).Should().Equal((15342, 40.0, true), (15343, 20.0, false));
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetCustomerOperations_NotAnAdministrator_ThrowsForbidden(EmployeeType employeeType)
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetCustomerOperationsAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
