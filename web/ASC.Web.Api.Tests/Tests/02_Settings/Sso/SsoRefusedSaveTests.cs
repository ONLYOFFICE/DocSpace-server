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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Sso;

/// <summary>
/// A save refused for a missing or malformed required field leaves the stored single sign-on settings as they
/// were: every check runs before the save.
/// </summary>
[Trait("Category", "Settings")]
public class SsoRefusedSaveTests(AspireAppFixture fixture) : SsoTestBase(fixture)
{
    [Theory]
    [InlineData("entityId")]
    [InlineData("ssoUrl")]
    public async Task SaveSsoSettings_RefusedForMissingIdpField_KeepsStoredSettings(string missingField)
    {
        // Arrange
        await SaveAsync(SettingsJson(s => s["spLoginLabel"] = "Stored before"));
        var refused = SettingsJson(s =>
        {
            s["spLoginLabel"] = "Refused after";
            s["enableSso"] = true;
            ((Dictionary<string, object?>)s["idpSettings"]!).Remove(missingField);
        });

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SaveAsync(refused));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var settings = (await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;
        settings.SpLoginLabel.Should().Be("Stored before");
        settings.EnableSso.Should().NotBe(true);
    }

    [Fact]
    public async Task SaveSsoSettings_RefusedForMissingFieldMapping_KeepsStoredSettings()
    {
        // Arrange
        await SaveAsync(SettingsJson(s => s["spLoginLabel"] = "Stored before"));
        var refused = SettingsJson(s =>
        {
            s["spLoginLabel"] = "Refused after";
            s.Remove("fieldMapping");
        });

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SaveAsync(refused));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var settings = (await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;
        settings.SpLoginLabel.Should().Be("Stored before");
        settings.FieldMapping.Email.Should().Be("mail");
    }
}
