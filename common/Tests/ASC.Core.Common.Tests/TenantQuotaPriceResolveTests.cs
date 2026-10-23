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
/// Tests for the price a quota is shown with (<see cref="TenantQuotaMapper"/>): the price in the currency of the
/// request region, then the price in the currency of the default region, then the price stored with the quota.
/// </summary>
public class TenantQuotaPriceResolveTests
{
    private const decimal StoredPrice = 5m;

    private static readonly RegionInfo _defaultRegion = new("US");
    private static readonly RegionInfo _germany = new("DE");

    [Fact]
    public void PriceInRequestRegionCurrency_IsTaken()
    {
        var price = TenantQuotaMapper.Resolve(Quota(), new() { ["USD"] = 7m, ["EUR"] = 6m }, _germany, _defaultRegion);

        price.Should().Be((6m, _germany.CurrencySymbol, "EUR"));
    }

    [Fact]
    public void NoPriceInRequestRegionCurrency_FallsBackToDefaultRegion()
    {
        var price = TenantQuotaMapper.Resolve(Quota(), new() { ["USD"] = 7m }, _germany, _defaultRegion);

        price.Should().Be((7m, _defaultRegion.CurrencySymbol, "USD"));
    }

    [Fact]
    public void NoPriceInEitherCurrency_FallsBackToStoredPrice()
    {
        var price = TenantQuotaMapper.Resolve(Quota(), new() { ["CNY"] = 50m }, _germany, _defaultRegion);

        price.Should().Be((StoredPrice, _defaultRegion.CurrencySymbol, "USD"));
    }

    [Fact]
    public void NoPriceInfo_FallsBackToStoredPrice()
    {
        var price = TenantQuotaMapper.Resolve(Quota(), null!, _germany, _defaultRegion);

        price.Should().Be((StoredPrice, _defaultRegion.CurrencySymbol, "USD"));
    }

    private static DbQuota Quota()
    {
        return new DbQuota { TenantId = -1, Name = "admin", Price = StoredPrice, ProductId = "1013" };
    }
}
