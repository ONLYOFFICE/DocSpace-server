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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Quota;

/// <summary>
/// GET /api/2.0/portal/tariff — the current portal tariff. <c>PortalController.GetPortalTariff</c>
/// never denies the request; it grades the response by role instead: everyone gets
/// <see cref="Tariff.State"/>, Owner/DocSpaceAdmin also see <see cref="Tariff.Id"/>,
/// <see cref="Tariff.CustomerId"/> and <see cref="Tariff.Quotas"/>, while a RoomAdmin/User/Guest
/// gets a stripped-down tariff with <c>id = 0</c> (see
/// <see cref="PortalTariffPermissionsTests"/>).
///
/// SDK gap: <c>TariffDto.OpenSource</c>/<c>Enterprise</c>/<c>Developer</c> are on the wire but
/// missing from the generated <see cref="Tariff"/> model, so those TypeScript assertions cannot
/// be ported.
/// </summary>
[Trait("Category", "Portal")]
public class PortalTariffTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetPortalTariff_Owner_ReturnsFullTariff()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var tariff = await _portalQuotaApi.GetPortalTariffAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AssertFullTariff(tariff);
    }

    [Fact]
    public async Task GetPortalTariff_DocSpaceAdmin_ReturnsFullTariff()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var tariff = await _portalQuotaApi.GetPortalTariffAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        AssertFullTariff(tariff);
    }

    private static void AssertFullTariff(TariffWrapper tariff)
    {
        tariff.StatusCode.Should().Be(200);

        var response = tariff.Response;
        response.Should().NotBeNull();
        response.Id.Should().BeGreaterThanOrEqualTo(0);
        response.State.Should().Be(TariffState.Paid);
        // No DueDate assertion: the standalone-style default tariff of this integration host has
        // no expiry, unlike the SaaS trial/paid tariffs the TS suite runs against.
        // No per-quota id bound: quota ids are negative for the built-in plans (-1 default here,
        // -3 SaaS startup), so the TS suite's id >= 0 only holds for purchased SaaS quotas.
        response.Quotas.Should().NotBeNullOrEmpty();
    }
}
