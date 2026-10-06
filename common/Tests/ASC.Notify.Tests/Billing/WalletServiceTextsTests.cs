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

namespace ASC.Notify.Tests.Billing;

/// <summary>
/// Every wallet quota has to be nameable in a letter: <c>renew_subscription_error</c> names the subscription
/// by the quota's <c>ServiceName</c> through <c>AccountingCustomerOperationServiceDesc_*</c> (the name) and
/// <c>AccountingCustomerOperationServiceUOM_*</c> (the unit), and prints "<c>*name*, unit: quantity</c>". A quota
/// added without those resources would reach the customer as "<c>foo, : 2</c>" - this test fails first.
///
/// The quotas are read from the seed data of the EF model (<c>DbQuotaExtension</c>'s <c>HasData</c>), not from
/// the portal's database: the test stack is not a SaaS one, and its migrations do not create wallet quotas.
/// </summary>
public class WalletServiceTextsTests
{
    [Fact]
    public async Task EveryWalletQuota_HasServiceNameAndUnitTexts()
    {
        var stack = await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException($"No {nameof(LetterStackFixture)} in the test context.");

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        await using var dbContext = await scope.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>()
            .CreateDbContextAsync(TestContext.Current.CancellationToken);

        // seed data lives only in the design-time model; the runtime one drops it
        var walletQuotas = dbContext.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(DbQuota))!
            .GetSeedData()
            .Where(row => row[nameof(DbQuota.Wallet)] is true)
            .Select(row => (Name: row[nameof(DbQuota.Name)] as string, ServiceName: row[nameof(DbQuota.ServiceName)] as string))
            .ToList();

        walletQuotas.Should().NotBeEmpty("the quota seed defines the wallet quotas");

        foreach (var quota in walletQuotas)
        {
            quota.ServiceName.Should().NotBeNullOrEmpty($"wallet quota '{quota.Name}' needs a billing service name");

            // the same lookup the letter makes, in the neutral culture every other one falls back to;
            // it also strips the "-1-hour" suffix test stands give their service names
            var (serviceName, title, unit) = ASC.Web.Core.WalletServiceDescriptionManager.GetServiceTitleAndUom(quota.ServiceName!, null, CultureInfo.InvariantCulture);

            title.Should().NotBeNullOrEmpty($"service '{serviceName}' of wallet quota '{quota.Name}' needs AccountingCustomerOperationServiceDesc_{serviceName}");
            unit.Should().NotBeNullOrEmpty($"service '{serviceName}' of wallet quota '{quota.Name}' needs AccountingCustomerOperationServiceUOM_{serviceName}");
        }
    }
}
