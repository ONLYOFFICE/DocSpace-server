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
/// POST /api/2.0/settings/banner — access control for the setter, called through
/// <c>DocSpace.API.SDK.Api.Security.BannersVisibilityApi</c> (wired onto <see cref="BaseTest"/> as
/// <c>_securityBannersVisibilityApi</c>).
///
/// <c>SettingsController.SetTenantBannerSettings</c> throws <c>BillingException</c> (HTTP 402)
/// before it ever reaches the permission check unless <c>TenantExtra.Enterprise</c> is true, which
/// in turn requires Standalone mode plus a real license file
/// (<c>TariffService.Enterprise =&gt; coreBaseSettings.Standalone &amp;&amp; !string.IsNullOrEmpty(licenseReaderConfig.LicensePath)</c>).
/// The Aspire test host runs the SaaS profile with no license configured, and this project's
/// harness has no payment/tariff setup helper (unlike the TypeScript suite's
/// <c>paymentsApi.setupPayment()</c>), so every role — including the owner — gets 402 here, not
/// the 200/403 the source <c>securityBannersVisibilityApi.spec.ts</c> /
/// <c>securityBannersVisibilityApi.permissions.spec.ts</c> assert. That TS suite is itself entirely
/// <c>test.skip</c>'d for the same reason ("Promotional banners are currently hidden both in UI and
/// API"). Only the anonymous case below is ported, because the 401 there comes from the
/// authentication middleware and is reached before the Enterprise check — every other TS case
/// (owner/DocSpaceAdmin succeeding, RoomAdmin/User/Guest getting 403) would need Enterprise tariff
/// setup this harness cannot provide and is therefore not translated; see the porting report for
/// what adding that support would require.
/// </summary>
[Trait("Category", "Security")]
public class BannersVisibilitySetPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetTenantBannerSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityBannersVisibilityApi.SetTenantBannerSettingsAsync(
                new TenantBannerSettingsDto(true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
