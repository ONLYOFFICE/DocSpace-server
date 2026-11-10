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


namespace ASC.Web.Api.Billing.Tests.Tests.Quota;

/// <summary>
/// The cheapest plan on sale that still covers the portal's use (<c>GET portal/quota/right</c>). Only plans on sale
/// count, so a portal on the free startup plan, which is not sold, is recommended the cheapest monthly plan; a paid
/// portal is recommended a plan no dearer than the one it pays for.
/// </summary>
public class PortalRightQuotaTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetRightQuota_FreePortal_RecommendsAMonthlyPlanOnSale()
    {
        // Act
        var right = (await _portalQuotaApi.GetRightQuotaAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        right.Should().NotBeNull();
        right.Visible.Should().BeTrue();
        right.ProductId.Should().NotBeNullOrEmpty();
        right.Year.Should().BeFalse();
        right.Free.Should().BeFalse();
    }

    [Fact]
    public async Task GetRightQuota_PaidPortal_Owner_RecommendsAPlanNoDearerThanThePaidOne()
    {
        // Arrange
        await PayForTenAdministratorsAsync();

        // Act & Assert
        var (current, right) = await AssertRightQuotaCoversCurrentAsync();

        right.Price.Should().BeLessThan(current.Price, "the portal uses far less than ten administrators' worth");
    }

    [Fact]
    public async Task GetRightQuota_PaidPortal_DocSpaceAdmin_RecommendsAPlanNoDearerThanThePaidOne()
    {
        // Arrange
        await PayForTenAdministratorsAsync();
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act & Assert
        await AssertRightQuotaCoversCurrentAsync();
    }

    private async Task PayForTenAdministratorsAsync()
    {
        Payments.Paid(Plans.AdminProductId, quantity: 10, DateTime.UtcNow.Date.AddYears(1));
        await _portalQuotaApi.GetPortalTariffAsync(true, TestContext.Current.CancellationToken);
    }

    private async Task<(TenantQuotaDto Current, TenantQuotaDto Right)> AssertRightQuotaCoversCurrentAsync()
    {
        var current = await _portalQuotaApi.GetPortalQuotaAsync(TestContext.Current.CancellationToken);
        var right = await _portalQuotaApi.GetRightQuotaAsync(TestContext.Current.CancellationToken);

        right.StatusCode.Should().Be(200);

        var currentQuota = current.Response;
        var rightQuota = right.Response;

        rightQuota.Should().NotBeNull();
        rightQuota.Name.Should().NotBeNullOrEmpty();
        rightQuota.Visible.Should().BeTrue();
        rightQuota.ProductId.Should().NotBeNullOrEmpty();
        rightQuota.Features.Should().NotBeNullOrEmpty();
        rightQuota.MaxFileSize.Should().BeGreaterThan(0);
        rightQuota.MaxTotalSize.Should().BeGreaterThan(0);

        rightQuota.Price.Should().BeLessThanOrEqualTo(currentQuota.Price);
        rightQuota.MaxTotalSize.Should().BeLessThanOrEqualTo(currentQuota.MaxTotalSize);
        rightQuota.CountRoomAdmin.Should().BeLessThanOrEqualTo(currentQuota.CountRoomAdmin);

        return (currentQuota, rightQuota);
    }
}
