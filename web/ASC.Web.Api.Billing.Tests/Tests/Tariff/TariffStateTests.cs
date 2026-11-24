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


namespace ASC.Web.Api.Billing.Tests.Tests.Tariff;

/// <summary>
/// The state of the tariff (<c>GET portal/tariff</c>) as the due date of the payments billing lists moves into the
/// past: a grace period first, then not paid.
/// </summary>
public class TariffStateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetPortalTariff_DueDateTwoDaysAgo_IsInTheGracePeriodThatEndsAfterTheDueDate()
    {
        // Arrange
        var endDate = DateTime.UtcNow.Date.AddDays(-2);
        Payments.Paid(Plans.AdminProductId, quantity: 10, endDate);

        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        tariff.State.Should().Be(TariffState.Delay);
        tariff.DueDate.UtcTime.Date.Should().Be(endDate);
        tariff.DelayDueDate.UtcTime.Date.Should().BeAfter(DateTime.UtcNow.Date.AddDays(-1),
            "the grace period of a plan that lapsed two days ago has not run out yet");
    }

    [Fact]
    public async Task GetPortalTariff_DueDateAYearAgo_IsNotPaid()
    {
        // Arrange
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddYears(-1));

        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        tariff.State.Should().Be(TariffState.NotPaid);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.RequestTimeout, false)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    public async Task GetPortalTariff_BillingAnswersCannotFind_IsARefundOnlyWhenTheOutcomeIsKnown(HttpStatusCode status, bool refund)
    {
        // Arrange: a paid plan, read once, then billing answers its payments with "cannot find"
        var endDate = DateTime.UtcNow.Date.AddYears(1);
        Payments.Paid(Plans.AdminProductId, quantity: 10, endDate);
        (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response.State.Should().Be(TariffState.Paid);
        Payments.On(PaymentService.Billing, "POST", "/billing/GetActiveResources", $$"""{"Message":"error: cannot find {{Payments.PortalId}}"}""", status);

        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert: under 200 billing stopped reporting the payment, so the read takes it for a refund and moves the due
        // date into the past; under a failing status the outcome is unknown and the paid tariff keeps its due date. The
        // state of a refund is not asserted: the stack does not save the moved due date (billing verification, "Refund").
        PaymentCalls.Count(call => call.Route == "/billing/GetActiveResources").Should().BeGreaterThan(1);
        if (refund)
        {
            tariff.DueDate.UtcTime.Should().BeBefore(DateTime.UtcNow, "the read takes the answer for a refund");
        }
        else
        {
            tariff.State.Should().Be(TariffState.Paid);
            tariff.DueDate.UtcTime.Date.Should().Be(endDate);
        }
    }
}
