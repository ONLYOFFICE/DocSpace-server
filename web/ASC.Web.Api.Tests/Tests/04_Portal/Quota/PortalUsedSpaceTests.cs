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
/// GET /api/2.0/portal/usedspace — the portal's used storage, in GB. Not payment-dependent: it
/// only sums the tenant's own quota rows, so it is exercised without the billing setup the
/// TypeScript source performs (which is not available in this environment).
/// </summary>
[Trait("Category", "Portal")]
public class PortalUsedSpaceTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetPortalUsedSpace_Owner_ReturnsNonNegativeValue()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var usedSpace = await _portalQuotaApi.GetPortalUsedSpaceAsync(TestContext.Current.CancellationToken);

        // Assert
        usedSpace.StatusCode.Should().Be(200);
        usedSpace.Response.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetPortalUsedSpace_DocSpaceAdmin_ReturnsNonNegativeValue()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var usedSpace = await _portalQuotaApi.GetPortalUsedSpaceAsync(TestContext.Current.CancellationToken);

        // Assert
        usedSpace.StatusCode.Should().Be(200);
        usedSpace.Response.Should().BeGreaterThanOrEqualTo(0);
    }
}
