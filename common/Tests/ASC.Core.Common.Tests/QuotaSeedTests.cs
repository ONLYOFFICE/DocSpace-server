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
/// The quota seed is read by lookups that take one row per key: a payment of the billing service is matched to
/// the quota with its product id, and the usage report keys the wallet quotas by service name. Two rows with one
/// key break those lookups for every portal, so the seed is checked as a whole.
/// </summary>
public class QuotaSeedTests
{
    [Fact]
    public void ProductIds_AreUnique()
    {
        var productIds = SeededQuotas()
            .Where(q => !string.IsNullOrEmpty(q.ProductId))
            .Select(q => q.ProductId)
            .ToList();

        productIds.Should().NotBeEmpty("the quota seed defines the paid products");
        productIds.Should().OnlyHaveUniqueItems("a payment is matched to one quota by its product id");
    }

    [Fact]
    public void ServiceNamesOfWalletQuotas_AreUnique()
    {
        var serviceNames = SeededQuotas()
            .Where(q => q.Wallet && !string.IsNullOrEmpty(q.ServiceName))
            .Select(q => q.ServiceName)
            .ToList();

        serviceNames.Should().NotBeEmpty("the quota seed defines the wallet quotas");
        serviceNames.Should().OnlyHaveUniqueItems("the usage report keys the wallet quotas by service name");
    }

    [Fact]
    public void YearlyWalletPlan_IsSeededAsPrimaryWalletQuotaWithYear()
    {
        var quota = SeededQuotas().Should().ContainSingle(q => q.TenantId == (int)TenantWalletService.AdminYear).Subject;
        var monthly = SeededQuotas().Should().ContainSingle(q => q.TenantId == (int)TenantWalletService.Admin).Subject;

        quota.Name.Should().Be("adminwalletyear");
        quota.Price.Should().Be(220m);
        quota.Wallet.Should().BeTrue();
        quota.Additional.Should().BeFalse();
        quota.Visible.Should().BeTrue();
        quota.ProductId.Should().NotBeNullOrEmpty("a wallet quota without a product id cannot be bought by name");

        var features = quota.Features.Split(',');
        features.Should().Contain("year");
        features.Where(f => f != "year").Should().Equal(monthly.Features.Split(','), "the yearly plan differs from the monthly one by the year feature only");
    }

    /// <summary>
    /// The seed rows of the model the migrations are generated from. The provider is needed to build the model, a
    /// connection is never opened. Seed data lives only in the design-time model; the runtime one drops it.
    /// </summary>
    private static List<DbQuota> SeededQuotas()
    {
        var options = new DbContextOptionsBuilder<ASC.Migrations.Core.MigrationContext>()
            .UseMySql(
                "Server=localhost;Database=docspace",
                ServerVersion.Parse("9.2.0"),
                providerOptions => providerOptions.UseMicrosoftJson())
            .Options;

        using var context = new ASC.Migrations.Core.MigrationContext(options);

        return
        [
            .. Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions
                .GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>(context).Model
                .FindEntityType(typeof(DbQuota))!
                .GetSeedData()
                .Select(row => new DbQuota
                {
                    TenantId = (int)row[nameof(DbQuota.TenantId)]!,
                    Name = row[nameof(DbQuota.Name)] as string,
                    Features = row[nameof(DbQuota.Features)] as string,
                    Price = (decimal)row[nameof(DbQuota.Price)]!,
                    ProductId = row[nameof(DbQuota.ProductId)] as string,
                    ServiceName = row[nameof(DbQuota.ServiceName)] as string,
                    Visible = row[nameof(DbQuota.Visible)] is true,
                    Wallet = row[nameof(DbQuota.Wallet)] is true,
                    Additional = row[nameof(DbQuota.Additional)] is true
                })
        ];
    }
}
