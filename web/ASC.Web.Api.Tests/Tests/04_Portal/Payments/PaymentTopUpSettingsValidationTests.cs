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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Payments;

/// <summary>
/// POST /api/2.0/portal/payment/topupsettings — validation of the auto top-up bounds. The action itself
/// needs a configured billing service and the payer, which this environment does not have, but the body
/// is validated before the action runs, so a rejected body answers 400 here just as it does on a billing
/// portal, while an accepted one only gets as far as the payer check.
/// </summary>
[Trait("Category", "Portal")]
public class PaymentTopUpSettingsValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetTenantWalletSettings_MinBalanceBelowRange_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetTenantWalletSettingsAsync(
                Settings(minBalance: 4, upToBalance: 1000),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>
    /// Each bound used to be checked against its own range only, so an upper bound below the trigger was
    /// stored, leaving auto top-up configured to refill the wallet into a state that immediately triggered
    /// another refill. The request body now also requires `upToBalance` to be greater than `minBalance`.
    /// </summary>
    [Fact]
    [Trait("Bug", "84314")]
    public async Task SetTenantWalletSettings_UpToBalanceBelowMinBalance_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetTenantWalletSettingsAsync(
                Settings(minBalance: 100, upToBalance: 50),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("UpToBalance");
    }

    [Fact]
    public async Task SetTenantWalletSettings_UpToBalanceEqualToMinBalance_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetTenantWalletSettingsAsync(
                Settings(minBalance: 100, upToBalance: 100),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetTenantWalletSettings_ValidBounds_PassValidation()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.SetTenantWalletSettingsAsync(
                Settings(minBalance: 5, upToBalance: 6),
                TestContext.Current.CancellationToken));

        // Assert
        // The body is accepted and the request reaches the payer check, which this environment
        // without a billing service refuses.
        exception.ErrorCode.Should().Be(403);
    }

    private static TenantWalletSettingsRequestDto Settings(int minBalance, int upToBalance)
    {
        return new TenantWalletSettingsRequestDto(new SetWalletTopUpSettingsRequest(enabled: true, minBalance: minBalance, upToBalance: upToBalance));
    }
}
