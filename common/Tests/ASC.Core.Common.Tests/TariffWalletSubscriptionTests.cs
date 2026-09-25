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

public class TariffWalletSubscriptionTests
{
    private static Tariff TariffOf(params ASC.Core.Billing.Quota[] quotas) => new() { DueDate = DateTime.MaxValue, Quotas = [.. quotas] };

    [Fact]
    public void HasActiveWalletSubscription_ActiveWalletAddon_ReturnsTrue()
    {
        var tariff = TariffOf(
            new ASC.Core.Billing.Quota(-19, 1),
            new ASC.Core.Billing.Quota((int)TenantWalletService.BusinessTools, 1, true, true, DateTime.UtcNow.AddDays(10), null));

        tariff.HasActiveWalletSubscription().Should().BeTrue();
    }

    [Fact]
    public void HasActiveWalletSubscription_ExpiredWalletAddon_ReturnsFalse()
    {
        var tariff = TariffOf(
            new ASC.Core.Billing.Quota(-19, 1),
            new ASC.Core.Billing.Quota((int)TenantWalletService.BusinessTools, 1, true, true, DateTime.UtcNow.AddDays(-1), null));

        tariff.HasActiveWalletSubscription().Should().BeFalse();
    }

    [Fact]
    public void HasActiveWalletSubscription_PrimaryWalletPlanOnly_ReturnsFalse()
    {
        // the administrators wallet plan is a primary plan, not a subscription on top of the free one
        var tariff = TariffOf(new ASC.Core.Billing.Quota((int)TenantWalletService.Admin, 5, false, true, DateTime.UtcNow.AddDays(10), null));

        tariff.HasActiveWalletSubscription().Should().BeFalse();
    }

    [Fact]
    public void HasActiveWalletSubscription_FreePlanOnly_ReturnsFalse()
    {
        TariffOf(new ASC.Core.Billing.Quota(-19, 1)).HasActiveWalletSubscription().Should().BeFalse();
    }
}
