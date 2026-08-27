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
/// GET /api/2.0/portal/payment/quota — the current portal quota. The action only denies Guests
/// explicitly (<c>IsGuestAsync</c>); it never calls <c>PaymentHelper.DemandConfigured</c>, so it
/// answers the same free "Startup" plan here as it would in production before any tariff is set.
/// </summary>
[Trait("Category", "Portal")]
public class PaymentQuotaInformationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // Commented out for now: these assert the SaaS "Startup" plan (id -3) seed data, which this
    // standalone-style integration host does not have. Re-enable on a SaaS-seeded environment.
    /*
    [Fact]
    public async Task GetQuotaPaymentInformation_Owner_ReturnsStartupQuota()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var quota = await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        quota.StatusCode.Should().Be(200);
        quota.Response!.Id.Should().Be(-3);
        quota.Response.Title.Should().Be("Startup");
        quota.Response.Price!.Value.Should().Be(0);
        quota.Response.Free.Should().BeTrue();
        quota.Response.Trial.Should().BeFalse();
        quota.Response.NonProfit.Should().BeFalse();
        quota.Response.Features.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetQuotaPaymentInformation_DocSpaceAdmin_ReturnsStartupQuota()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var quota = await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        quota.StatusCode.Should().Be(200);
        quota.Response!.Id.Should().Be(-3);
        quota.Response.Title.Should().Be("Startup");
    }
    */

    [Fact]
    public async Task GetQuotaPaymentInformation_Guest_ThrowsAccessDenied()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    // BUG 81534: the action only checks IsGuestAsync, so a plain RoomAdmin — who should not see
    // billing/quota information any more than a User should — is let straight through with 200.
    [Trait("Bug", "81534")]
    [Fact]
    public async Task GetQuotaPaymentInformation_RoomAdmin_ThrowsAccessDenied()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    // BUG 81534: same as RoomAdmin above — a plain User currently gets 200.
    [Trait("Bug", "81534")]
    [Fact]
    public async Task GetQuotaPaymentInformation_User_ThrowsAccessDenied()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetQuotaPaymentInformation_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
