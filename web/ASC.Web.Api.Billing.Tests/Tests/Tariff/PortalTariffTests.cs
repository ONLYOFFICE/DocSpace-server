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
/// The portal's tariff as billing reports it (<c>GET portal/tariff</c>): what a portal billing knows nothing about
/// falls back to, and how a payment billing lists reaches the tariff.
/// </summary>
public class PortalTariffTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetPortalTariff_PortalWithoutPayments_IsTheFreeStartupQuota()
    {
        // Arrange: the portal was asked about when it was registered; only the requests of the read below count.
        var askedBefore = ActiveResourcesRequests();

        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        tariff.State.Should().Be(TariffState.Paid);
        tariff.Quotas.Should().ContainSingle().Which.Id.Should().Be(Plans.StartupQuotaId);
        ActiveResourcesRequests().Should().BeGreaterThan(askedBefore,
            "a refreshed read asks billing again, under the payment key of the portal");
    }

    [Fact]
    public async Task GetPortalTariff_AfterAPayment_IsThePaidQuota()
    {
        // Arrange
        var endDate = DateTime.UtcNow.Date.AddYears(1);
        Payments.Paid(Plans.AdminProductId, quantity: 10, endDate);

        // Act
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        tariff.State.Should().Be(TariffState.Paid);
        var quota = tariff.Quotas.Should().ContainSingle(q => q.Id == Plans.AdminQuotaId).Subject;
        quota.Quantity.Should().Be(10);
    }

    private int ActiveResourcesRequests()
    {
        return PaymentCalls.Count(call => call.Service == PaymentService.Billing && call.Route == "/billing/GetActiveResources");
    }
}
