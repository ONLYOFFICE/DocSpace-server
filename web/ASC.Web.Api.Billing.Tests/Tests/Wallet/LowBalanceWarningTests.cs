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
/// The low-balance warning the wallet poller of Web.Studio (<c>TopUpWalletService</c>) raises for a portal without
/// auto top-up, seen through <c>lowBalanceNotified</c> of <c>GET payment/topupsettings</c>.
/// </summary>
public class LowBalanceWarningTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // the stub profile runs the poller every Constants.PaymentStubTopUpPeriod; a few runs fit, for a busy machine
    private static readonly TimeSpan _pollerDeadline = Constants.PaymentStubTopUpPeriod * 6;

    /// <summary>
    /// A payer disabled in the portal used to make the poller fail on that portal on every run: the account of a
    /// disabled user resolves to a guest and signing in as it threw, so the warning was never raised. The poller
    /// now runs as the owner when the payer cannot be signed in as.
    /// </summary>
    [Fact]
    public async Task LowBalancePoller_PayerDisabled_StillRaisesTheWarning()
    {
        // Arrange
        var payer = await InviteMember(EmployeeType.RoomAdmin);
        Payments.Customer(payer.Email).Balance(0.5m);

        // turning auto top-up off is what stamps the low-balance threshold the poller watches
        await _webApiClient.Authenticate(payer);
        var request = new TenantWalletSettingsRequestDto(new SetWalletTopUpSettingsRequest(enabled: false, minBalance: 10, upToBalance: 50, currency: "USD"));
        var saved = (await _paymentApi.SetTenantWalletSettingsAsync(request, TestContext.Current.CancellationToken)).Response;
        saved.LowBalanceThreshold.Should().BePositive(); // a whole number, so the balance of 0.5 is below it

        await _webApiClient.Authenticate(Owner);
        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, new UpdateMembersRequestDto([payer.Id]), TestContext.Current.CancellationToken);

        // Act
        var settings = await WaitForLowBalanceWarningAsync();

        // Assert
        settings.LowBalanceNotified.Should().BeTrue();
    }

    private async Task<TenantWalletSettingsDto> WaitForLowBalanceWarningAsync()
    {
        var deadline = DateTime.UtcNow + _pollerDeadline;

        while (true)
        {
            var settings = (await _paymentApi.GetTenantWalletSettingsAsync(TestContext.Current.CancellationToken)).Response;

            if (settings.LowBalanceNotified || DateTime.UtcNow >= deadline)
            {
                return settings;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }
}
