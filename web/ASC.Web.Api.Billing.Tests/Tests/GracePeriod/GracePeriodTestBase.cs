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


namespace ASC.Web.Api.Billing.Tests.Tests.GracePeriod;

/// <summary>
/// What the grace period suites share: a plan that lapsed two days ago, inside the grace days of the stack, a billing
/// customer who pays by SEPA or by card, and the tariff as <c>GET portal/tariff</c> reports it.
/// </summary>
public abstract class GracePeriodTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string CustomerRoute = "/billing/GetCustomerInfo";

    /// <summary>
    /// Starts the grace period of the admin plan, with the owner as the customer paying by SEPA or by card. The
    /// customer and the tariff are read again, as the cached ones still describe the portal before the payment.
    /// </summary>
    protected async Task StartGracePeriodAsync(bool delayedPaymentMethod)
    {
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddDays(-2), Owner.Email)
            .Customer(Owner.Email, delayedPaymentMethod: delayedPaymentMethod);
        await RefreshCustomerAsync();
        (await ReadTariffAsync()).State.Should().Be(TariffState.Delay);
    }

    protected async Task RefreshCustomerAsync()
    {
        await _webApiClient.Authenticate(Owner);
        await _paymentApi.GetCustomerInfoAsync(true, TestContext.Current.CancellationToken);
    }

    protected int CustomerCalls()
    {
        return PaymentCalls.Count(call => call.Route == CustomerRoute);
    }

    protected async Task<(TariffState? State, bool Restricted)> ReadTariffAsync(bool refresh = true)
    {
        var tariff = (await _portalQuotaApi.GetPortalTariffAsync(refresh, TestContext.Current.CancellationToken)).Response;

        return (tariff.State, tariff.Restricted);
    }
}
