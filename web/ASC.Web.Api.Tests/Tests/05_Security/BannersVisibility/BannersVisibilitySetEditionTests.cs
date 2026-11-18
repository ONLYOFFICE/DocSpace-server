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

namespace ASC.Web.Api.Tests.Tests._05_Security.BannersVisibility;

/// <summary>
/// POST /api/2.0/settings/banner on an installation that is not Enterprise: the stand runs no license file, so
/// <c>TenantExtra.Enterprise</c> is false and the 402 comes before the portal-settings right is checked, for the
/// owner and for a User alike (<c>SettingsController.SetTenantBannerSettings</c>). The flag stays as it was.
/// </summary>
[Trait("Category", "Security")]
public class BannersVisibilitySetEditionTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetTenantBannerSettings_NotEnterprise_ThrowsPaymentRequiredBeforeTheRight(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityBannersVisibilityApi.SetTenantBannerSettingsAsync(
                new TenantBannerSettingsRequestDto(true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
    }

    [Fact]
    public async Task SetTenantBannerSettings_OwnerNotEnterprise_ThrowsPaymentRequiredAndFlagStays()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var before = (await _bannersVisibilityApi.GetTenantBannerSettingsAsync(TestContext.Current.CancellationToken)).Response.Hidden;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityBannersVisibilityApi.SetTenantBannerSettingsAsync(
                new TenantBannerSettingsRequestDto(!before), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        (await _bannersVisibilityApi.GetTenantBannerSettingsAsync(TestContext.Current.CancellationToken)).Response.Hidden.Should().Be(before);
    }
}
