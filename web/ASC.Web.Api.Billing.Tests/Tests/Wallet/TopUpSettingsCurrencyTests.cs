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


namespace ASC.Web.Api.Billing.Tests.Tests.Wallet;

/// <summary>
/// The currency of the automatic top-up (<c>POST payment/topupsettings</c>): the job tops up the sub-account of that
/// currency, so one accounting does not keep is refused instead of being stored.
/// </summary>
/// <remarks>Suspected defect W5 of the billing spec: the currency is stored unchecked.</remarks>
public class TopUpSettingsCurrencyTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    public async Task SetTenantWalletSettings_CurrencyAccountingDoesNotUse_ThrowsBadRequestAndKeepsTheSettings()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);

        // Act: XXX is the ISO code for "no currency", which no accounting configuration lists.
        var exception = await Record.ExceptionAsync(async () => await _paymentApi.SetTenantWalletSettingsAsync(
            new TenantWalletSettingsRequestDto(new SetWalletTopUpSettingsRequest(enabled: true, minBalance: 10, upToBalance: 50, currency: "XXX")),
            TestContext.Current.CancellationToken));

        // Assert
        var stored = (await _paymentApi.GetTenantWalletSettingsAsync(TestContext.Current.CancellationToken)).Response;
        stored.Currency.Should().NotBe("XXX", "a top-up in a currency the wallet has no sub-account for can never run");

        exception.Should().BeOfType<ApiException>().Which.ErrorCode.Should().Be(400);
    }
}
