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


namespace ASC.Web.Api.Billing.Tests.Tests.Quota;

/// <summary>
/// The plan the portal is on as the payment page shows it (<c>GET payment/quota</c>): the free startup plan until
/// billing lists a payment, the paid plan after it.
/// </summary>
public class PaymentQuotaTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetQuotaPaymentInformation_Owner_ReturnsStartupQuota()
    {
        // Act
        var quota = await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        quota.StatusCode.Should().Be(200);
        quota.Response.Id.Should().Be(Plans.StartupQuotaId);
        quota.Response.Title.Should().Be("Startup");
        quota.Response.Price.Value.Should().Be(0);
        quota.Response.Free.Should().BeTrue();
        quota.Response.Trial.Should().BeFalse();
        quota.Response.NonProfit.Should().BeFalse();
        quota.Response.Features.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetQuotaPaymentInformation_DocSpaceAdmin_ReturnsStartupQuota()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var quota = await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        quota.StatusCode.Should().Be(200);
        quota.Response.Id.Should().Be(Plans.StartupQuotaId);
        quota.Response.Title.Should().Be("Startup");
    }

    [Fact]
    public async Task GetQuotaPaymentInformation_AfterAPayment_ReturnsThePaidPlan()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddYears(1));

        // Act
        var quota = await _paymentApi.GetQuotaPaymentInformationAsync(true, TestContext.Current.CancellationToken);

        // Assert
        quota.Response.Free.Should().BeFalse();
        quota.Response.Price.Value.Should().BeGreaterThan(0);
        quota.Response.Features.Should().Contain(feature => feature.Id == "manager" && feature.Value.ToString() == "10",
            "ten units of the plan cover ten administrators");
    }
}
