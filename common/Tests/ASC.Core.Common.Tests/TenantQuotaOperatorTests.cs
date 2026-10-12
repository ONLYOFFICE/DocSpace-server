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

public class TenantQuotaOperatorTests
{
    // A regular paid base plan: non-wallet, primary (non-additional).
    private static TenantQuota BasePlan() => new(1)
    {
        Name = "business",
        Price = 30m,
        Wallet = false,
        Additional = false,
        Visible = true,
        CountRoomAdmin = 3,
        MaxTotalSize = 1000
    };

    // The admin-via-wallet plan: wallet + primary (non-additional). Represents the base plan when paid from the wallet.
    private static TenantQuota AdminWallet() => new((int)TenantWalletService.Admin)
    {
        Name = "adminwallet",
        Price = 20m,
        Wallet = true,
        Additional = false,
        Visible = true,
        CountRoomAdmin = 1
    };

    // A wallet add-on: wallet + additional. Its price must NOT be added to the aggregate.
    private static TenantQuota StorageWalletAddon() => new((int)TenantWalletService.Storage)
    {
        Name = "storagewallet",
        Price = 50m,
        Wallet = true,
        Additional = true,
        Visible = true
    };

    [Fact]
    public void Multiply_AdminWallet_ScalesPriceAndAdminCount()
    {
        var adminWallet = AdminWallet();

        adminWallet *= 5;

        adminWallet.Price.Should().Be(100m);
        adminWallet.CountRoomAdmin.Should().Be(5);
    }

    [Fact]
    public void Add_BasePlanThenAdminWallet_SumsPriceAndAdminCount_KeepsBaseIdentity()
    {
        var adminWallet = AdminWallet();
        adminWallet *= 5; // Price 100, CountRoomAdmin 5

        // Mirror the production fold (TenantManager.GetTenantQuotaAsync): start from null, base first.
        TenantQuota? combined = null;
        combined += BasePlan();
        combined += adminWallet;

        combined.Price.Should().Be(130m);          // base 30 + adminwallet 100
        combined.CountRoomAdmin.Should().Be(8);    // base 3 + adminwallet 5
        combined.Name.Should().Be("business");     // identity comes from the non-wallet base plan
        combined.Wallet.Should().BeFalse();
        combined.Additional.Should().BeFalse();
    }

    [Fact]
    public void Add_PriceIsOrderIndependent()
    {
        var adminWalletA = AdminWallet();
        adminWalletA *= 5;

        TenantQuota? baseFirst = null;
        baseFirst += BasePlan();
        baseFirst += adminWalletA;

        var adminWalletB = AdminWallet();
        adminWalletB *= 5;

        TenantQuota? walletFirst = null;
        walletFirst += adminWalletB;
        walletFirst += BasePlan();

        baseFirst.Price.Should().Be(130m);
        walletFirst.Price.Should().Be(130m);
        baseFirst.Price.Should().Be(walletFirst.Price);
    }

    [Fact]
    public void Add_AdditionalWalletAddon_DoesNotAddPrice()
    {
        TenantQuota? combined = null;
        combined += BasePlan();
        combined += StorageWalletAddon();

        combined.Price.Should().Be(30m);           // additional wallet add-on price is excluded
        combined.CountRoomAdmin.Should().Be(3);    // base features preserved
        combined.Wallet.Should().BeFalse();        // identity stays the non-wallet base plan
    }
}
