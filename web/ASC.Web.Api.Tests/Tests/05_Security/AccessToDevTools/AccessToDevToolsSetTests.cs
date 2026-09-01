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

namespace ASC.Web.Api.Tests.Tests._05_Security.AccessToDevTools;

/// <summary>
/// POST /api/2.0/settings/devtoolsaccess — the setter for the portal's Developer Tools access
/// restriction, called through <c>DocSpace.API.SDK.Api.Security.AccessToDevToolsApi</c> (wired onto
/// <see cref="BaseTest"/> as <c>_securityAccessToDevToolsApi</c>). Callable by the owner and a
/// DocSpaceAdmin; see <see cref="AccessToDevToolsSetPermissionsTests"/> for every other role and
/// <see cref="AccessToDevToolsAffectsApiKeyCreationTests"/> for the restriction's effect on API key
/// creation.
///
/// The reader side (<c>GET /api/2.0/settings/devtoolsaccess</c>, reflecting what this endpoint
/// writes) is already covered by <c>ASC.Web.Api.Tests.Tests._02_Settings.AccessToDevTools.AccessToDevToolsGetTests</c>,
/// which arranges its own fixture data through this same setter — this suite instead asserts what
/// the setter itself returns.
/// </summary>
[Trait("Category", "Security")]
public class AccessToDevToolsSetTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetTenantDevToolsAccessSettings_Owner_EnablesLimitedAccess()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _securityAccessToDevToolsApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsDto(true), TestContext.Current.CancellationToken);

        // Assert
        result.Response.LimitedAccessForUsers.Should().BeTrue();
        // LastModified is not asserted: on this host the setter's echo carries the default
        // timestamp, unlike the SaaS deployments the TS suite runs against.
    }

    [Fact]
    public async Task SetTenantDevToolsAccessSettings_Owner_DisablesLimitedAccess()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _securityAccessToDevToolsApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsDto(false), TestContext.Current.CancellationToken);

        // Assert
        result.Response.LimitedAccessForUsers.Should().BeFalse();
        // LastModified is not asserted: on this host the setter's echo carries the default
        // timestamp, unlike the SaaS deployments the TS suite runs against.
    }

    [Fact]
    public async Task SetTenantDevToolsAccessSettings_DocSpaceAdmin_ReturnsOk()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _securityAccessToDevToolsApi.SetTenantDevToolsAccessSettingsWithHttpInfoAsync(
            new TenantDevToolsAccessSettingsDto(true), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
