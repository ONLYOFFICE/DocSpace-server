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

    // BUG 81534 asked for this endpoint to be closed to a RoomAdmin and a User, on the grounds that
    // neither should see billing information. It cannot be: the client reads it during start-up for
    // every non-guest (AuthStore.getPaymentInfo) to learn the plan's feature limits, and rethrows on
    // failure, so denying it leaves those roles on a blank page. The endpoint is role-aware instead
    // — QuotaHelper.GetFeatures drops admin-only features and withholds the usage figures from a
    // User — which is what these two cases pin.
    [Trait("Bug", "81534")]
    [Fact]
    public async Task GetQuotaPaymentInformation_RoomAdmin_ReturnsQuota()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        // Act
        var quota = await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        quota.StatusCode.Should().Be(200);
        quota.Response.Features.Should().NotBeEmpty();
    }

    [Trait("Bug", "81534")]
    [Fact]
    public async Task GetQuotaPaymentInformation_User_ReturnsQuotaWithoutUsageFigures()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var quota = await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        quota.StatusCode.Should().Be(200);
        quota.Response.Features.Should().NotBeEmpty();

        // Only the portal's total size is disclosed to a User; no other feature carries a counter.
        quota.Response.Features
            .Where(f => f.Used is not null)
            .Should().OnlyContain(f => f.Id == "total_size");
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
