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

namespace ASC.Web.Api.Tests.Tests._02_Settings.AccessToDevTools;

/// <summary>
/// GET /api/2.0/settings/devtoolsaccess — the portal's Developer Tools access restriction.
/// Readable by the owner and a DocSpaceAdmin (see <see cref="AccessToDevToolsGetPermissionsTests"/>
/// for the fact that every other role can read it too).
///
/// The setter, <c>POST /api/2.0/settings/devtoolsaccess</c>, lives under
/// <c>DocSpace.API.SDK.Api.Security.AccessToDevToolsApi</c> — a different generated client than
/// the reader (<c>DocSpace.API.SDK.Api.Settings.AccessToDevToolsApi</c>, wired onto
/// <see cref="BaseTest"/> as <c>_accessToDevToolsApi</c>). <see cref="BaseTest"/> does not expose
/// the setter, so this suite builds its own instance from the same authenticated
/// <c>_webApiClient</c> instead of duplicating a raw HTTP call for a perfectly typed endpoint.
/// </summary>
[Trait("Category", "Settings")]
public class AccessToDevToolsGetTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private DocSpace.API.SDK.Api.Security.AccessToDevToolsApi CreateSecurityAccessToDevToolsApi()
    {
        return new(_webApiClient, new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });
    }

    [Fact]
    public async Task GetTenantAccessDevToolsSettings_Owner_ReturnsSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _accessToDevToolsApi.GetTenantAccessDevToolsSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNull();
        result.Response.LastModified.Should().NotBe(default);
    }

    [Fact]
    public async Task GetTenantAccessDevToolsSettings_DocSpaceAdmin_ReturnsSettings()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var result = await _accessToDevToolsApi.GetTenantAccessDevToolsSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenantAccessDevToolsSettings_Owner_ReflectsEnabledRestriction()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var securityApi = CreateSecurityAccessToDevToolsApi();
        await securityApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsDto(true), TestContext.Current.CancellationToken);

        // Act
        var result = await _accessToDevToolsApi.GetTenantAccessDevToolsSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.LimitedAccessForUsers.Should().BeTrue();
    }

    [Fact]
    public async Task GetTenantAccessDevToolsSettings_Owner_ReflectsDisabledRestriction()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var securityApi = CreateSecurityAccessToDevToolsApi();
        await securityApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsDto(false), TestContext.Current.CancellationToken);

        // Act
        var result = await _accessToDevToolsApi.GetTenantAccessDevToolsSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.LimitedAccessForUsers.Should().BeFalse();
    }

    [Fact]
    public async Task GetTenantAccessDevToolsSettings_DocSpaceAdmin_ReflectsRestrictionSetByOwner()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _webApiClient.Authenticate(Owner);
        var securityApi = CreateSecurityAccessToDevToolsApi();
        await securityApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsDto(true), TestContext.Current.CancellationToken);

        // Act
        await _webApiClient.Authenticate(admin);
        var result = await _accessToDevToolsApi.GetTenantAccessDevToolsSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.LimitedAccessForUsers.Should().BeTrue();
    }
}
