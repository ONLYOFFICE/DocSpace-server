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
namespace ASC.Web.Api.Tests.Tests._02_Settings.Sso;

/// <summary>
/// GET /api/2.0/settings/ssov2, /default and /constants. The constants are static reference data open to any
/// signed-in caller; the other two need the portal-settings right (standalone: no plan gate). An anonymous
/// caller of the plain read gets only the flag the sign-in page needs.
/// </summary>
[Trait("Category", "Settings")]
public class SsoReadTests(AspireAppFixture fixture) : SsoTestBase(fixture)
{
    [Fact]
    public async Task GetDefaultSsoSettings_Owner_ReturnsBuiltInTemplate()
    {
        // Act
        var settings = (await Sso.GetDefaultSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.EnableSso.Should().BeFalse();
        settings.IdpSettings.EntityId.Should().BeEmpty();
        settings.SpLoginLabel.Should().Be("Single Sign-on");
    }

    [Fact]
    public async Task GetSsoSettingsConstants_Owner_ReturnsSamlFieldConstants()
    {
        // Act
        var constants = (await Sso.GetSsoSettingsV2ConstantsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        constants.SsoBindingType.Saml20HttpPost.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSsoSettings_Owner_ReturnsSettingsWithDefaultLoginLabel()
    {
        // Act
        var settings = (await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;

        // Assert: nothing is saved yet, so the label falls back to the built-in one
        settings.Should().NotBeNull();
        settings.SpLoginLabel.Should().Be("Single Sign-on");
    }

    [Fact]
    public async Task GetSsoSettings_Anonymous_ReturnsOnlyHideAuthPage()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var settings = (await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.Should().NotBeNull();
        settings.IdpSettings.Should().BeNull();
        settings.SpLoginLabel.Should().BeNull();
    }
}
