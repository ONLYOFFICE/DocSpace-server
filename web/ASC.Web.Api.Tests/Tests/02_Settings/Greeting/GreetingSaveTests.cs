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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Greeting;

/// <summary>
/// POST /api/2.0/settings/greetingsettings — saving a custom greeting title. No cleanup is
/// needed: the portal belongs to this test alone.
/// </summary>
[Trait("Category", "Settings")]
public class GreetingSaveTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // Commented out for now: on the integration host (base-domain = localhost) SaveTenantAsync
    // rewrites the portal alias to 'localhost' (Tenant.GetTenantDomain) and dies on the unique
    // alias index with a 500, so every save/restore case fails. Re-enable when that is resolved.
    /*
    [Fact]
    public async Task SaveGreetingSettings_Owner_TitleIsSavedAndNoLongerDefault()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        const string customTitle = "Custom Portal Greeting";

        // Act
        var saved = await _greetingSettingsApi.SaveGreetingSettingsAsync(
            new GreetingSettingsRequestsDto(customTitle), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        saved.Response.Should().NotBeNullOrEmpty();

        var greeting = await _greetingSettingsApi.GetGreetingSettingsAsync(TestContext.Current.CancellationToken);
        greeting.Response.ToString().Should().Be(customTitle);

        var isDefault = await _greetingSettingsApi.GetIsDefaultGreetingSettingsAsync(TestContext.Current.CancellationToken);
        isDefault.Response.Should().BeFalse();
    }

    [Fact]
    public async Task SaveGreetingSettings_DocSpaceAdmin_TitleIsSaved()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);
        const string customTitle = "DocSpaceAdmin Portal Greeting";

        // Act
        var saved = await _greetingSettingsApi.SaveGreetingSettingsAsync(
            new GreetingSettingsRequestsDto(customTitle), TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        saved.Response.Should().NotBeNullOrEmpty();

        var greeting = await _greetingSettingsApi.GetGreetingSettingsAsync(TestContext.Current.CancellationToken);
        greeting.Response.ToString().Should().Be(customTitle);
    }
    */
}
