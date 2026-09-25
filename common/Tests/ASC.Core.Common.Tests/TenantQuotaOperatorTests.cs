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

    // The free base plan every new portal is registered on.
    private static TenantQuota FreePlan() => new(-19)
    {
        Name = "free",
        Features = "free,oauth,total_size:2147483648,manager:10000,room:10000,automationapi",
        Price = 0m,
        Wallet = false,
        Additional = false,
        Visible = false
    };

    // The Business tools wallet subscription: wallet + additional, sold one unit at a time.
    private static TenantQuota BusinessToolsWalletAddon() => new((int)TenantWalletService.BusinessTools)
    {
        Name = "businesstools",
        Features = "businesstools,sms2fa,audit,ldap,sso,customization,thirdparty,restore,contentsearch,file_size:1024,statistic,free_backup:2:fixed",
        Price = 99m,
        Wallet = true,
        Additional = true,
        Visible = true
    };

    [Fact]
    public void Add_FreePlanThenBusinessTools_KeepsFreeIdentity_AddsPaidFeatures()
    {
        TenantQuota? combined = null;
        combined += FreePlan();
        combined += BusinessToolsWalletAddon();

        combined.Name.Should().Be("free");
        combined.Free.Should().BeTrue();           // the portal stays on the free plan
        combined.Price.Should().Be(0m);            // additional wallet add-on price is excluded
        combined.CountRoomAdmin.Should().Be(10000);
        combined.CountRoom.Should().Be(10000);
        combined.MaxTotalSize.Should().Be(2147483648);

        combined.BusinessTools.Should().BeTrue();
        combined.Sms2Fa.Should().BeTrue();
        combined.Audit.Should().BeTrue();
        combined.Ldap.Should().BeTrue();
        combined.Sso.Should().BeTrue();
        combined.Customization.Should().BeTrue();
        combined.ThirdParty.Should().BeTrue();
        combined.Restore.Should().BeTrue();
        combined.ContentSearch.Should().BeTrue();
        combined.Statistic.Should().BeTrue();
        combined.CountFreeBackup.Should().Be(2);   // a fixed count of an add-on reaches the portal
    }

    [Fact]
    public void Add_ExpiredBusinessTools_IsIgnored()
    {
        var businessTools = BusinessToolsWalletAddon();
        businessTools.DueDate = DateTime.UtcNow.AddDays(-1);

        TenantQuota? combined = null;
        combined += FreePlan();
        combined += businessTools;

        combined.BusinessTools.Should().BeFalse();
        combined.Audit.Should().BeFalse();
        combined.CountFreeBackup.Should().Be(0);
    }

    [Fact]
    public void Add_FixedCount_TakesMaximumInsteadOfSum()
    {
        var first = new TenantQuota(1) { Features = "free_backup:2:fixed" };
        var second = new TenantQuota(2) { Features = "free_backup:2:fixed", Wallet = true, Additional = true };

        TenantQuota? combined = null;
        combined += first;
        combined += second;

        combined.CountFreeBackup.Should().Be(2);
    }

    [Fact]
    public void Multiply_FixedCount_IsNotScaled()
    {
        var businessTools = BusinessToolsWalletAddon();

        businessTools *= 3;

        businessTools.CountFreeBackup.Should().Be(2);
    }

    [Fact]
    public void CountFreeBackup_Setter_RoundTrips()
    {
        var quota = new TenantQuota(1) { CountFreeBackup = 3 };

        quota.CountFreeBackup.Should().Be(3);
        quota.Features.Should().Be("free_backup:3:fixed");

        quota.CountFreeBackup = 0;

        quota.Features.Should().BeEmpty();
    }
}
