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
/// The automatic top-up of the wallet (<c>POST payment/topupsettings</c>, <c>GET payment/topupsettings</c>): stored
/// for the payer of a customer that has a wallet.
/// </summary>
public class TopUpSettingsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task SetTenantWalletSettings_Payer_StoresTheSettings()
    {
        // Arrange
        Payments.Customer(Owner.Email).Balance(10m);

        // Act
        var saved = (await _paymentApi.SetTenantWalletSettingsAsync(Settings(), TestContext.Current.CancellationToken)).Response;

        // Assert
        saved.Enabled.Should().BeTrue();
        saved.MinBalance.Should().Be(10);
        saved.UpToBalance.Should().Be(50);
        saved.Currency.Should().Be("USD");

        var read = (await _paymentApi.GetTenantWalletSettingsAsync(TestContext.Current.CancellationToken)).Response;
        read.Should().BeEquivalentTo(saved);
    }

    [Fact]
    public async Task SetTenantWalletSettings_CustomerWithoutAWallet_ThrowsNotFound()
    {
        // Arrange
        Payments.Customer(Owner.Email);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetTenantWalletSettingsAsync(Settings(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Balance could not be found");
    }

    [Fact]
    public async Task SetTenantWalletSettings_PortalIsNotACustomer_ThrowsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetTenantWalletSettingsAsync(Settings(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("Customer could not be found");
    }

    [Fact]
    public async Task SetTenantWalletSettings_NotThePayer_ThrowsForbidden()
    {
        // Arrange
        Payments.Customer("the-payer@example.com").Balance(10m);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetTenantWalletSettingsAsync(Settings(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("Access denied");
    }

    private static TenantWalletSettingsRequestDto Settings()
    {
        return new TenantWalletSettingsRequestDto(new SetWalletTopUpSettingsRequest(enabled: true, minBalance: 10, upToBalance: 50, currency: "USD"));
    }
}
