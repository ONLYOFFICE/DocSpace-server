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
/// The billing customer of the portal (<c>GET payment/customerinfo</c>): read from billing, with the portal user whose
/// e-mail the customer has as its payer.
/// </summary>
public class CustomerInfoTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetCustomerInfo_PortalIsACustomer_ReturnsItWithTheOwnerAsPayer()
    {
        // Arrange
        Payments.Customer(Owner.Email);

        // Act
        var customer = (await _paymentApi.GetCustomerInfoAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        customer.Should().NotBeNull();
        customer.PaymentMethodStatus.Should().Be(PaymentMethodStatus.Set);
        customer.Payer.Should().NotBeNull();
        customer.Payer.Id.Should().Be(Owner.Id);
        PaymentCalls.Should().Contain(call => call.Route == "/billing/GetCustomerInfo");
    }

    [Fact]
    public async Task GetCustomerInfo_CustomerEmailOfNoPortalUser_ReturnsItWithoutAPayer()
    {
        // Arrange
        Payments.Customer("somebody-else@example.com");

        // Act
        var customer = (await _paymentApi.GetCustomerInfoAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        customer.Should().NotBeNull();
        customer.Payer.Should().BeNull("only a portal user can be the payer");
    }

    [Fact]
    public async Task GetCustomerInfo_PortalIsNotACustomer_ReturnsNothing()
    {
        // Act
        var customer = (await _paymentApi.GetCustomerInfoAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        customer.Should().BeNull();
    }
}
