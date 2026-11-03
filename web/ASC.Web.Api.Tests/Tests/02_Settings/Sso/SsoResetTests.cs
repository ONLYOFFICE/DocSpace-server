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
/// DELETE /api/2.0/settings/ssov2 — a saved configuration goes back to the built-in defaults.
/// </summary>
[Trait("Category", "Settings")]
public class SsoResetTests(AspireAppFixture fixture) : SsoTestBase(fixture)
{
    [Fact]
    public async Task ResetSsoSettings_SavedConfiguration_RestoresDefaults()
    {
        // Arrange
        await SaveAsync(SettingsJson(s => s["enableSso"] = true));

        // Act
        await Sso.ResetSsoSettingsV2Async(TestContext.Current.CancellationToken);
        var settings = (await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.EnableSso.Should().BeFalse();
        settings.IdpSettings.EntityId.Should().BeEmpty();
    }
}
