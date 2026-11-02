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
/// POST /api/2.0/settings/ssov2 — saving the SAML configuration, the values the handler normalizes on the way in,
/// and the validation of its required fields.
/// </summary>
/// <remarks>
/// BUG 84685, 84686, 84687 and 84688 share one shape: the handler reports each missing or malformed required
/// field with a bare <c>throw new Exception(...)</c>, which the global handler turns into a 500 with a stack
/// trace instead of a validation error.
/// </remarks>
[Trait("Category", "Settings")]
public class SsoSaveTests(AspireAppFixture fixture) : SsoTestBase(fixture)
{
    [Fact]
    public async Task SaveSsoSettings_ValidConfiguration_IsStoredAndReadBack()
    {
        // Act
        await SaveAsync(SettingsJson(s =>
        {
            s["enableSso"] = true;
            s["spLoginLabel"] = "Custom SSO Label";
        }));
        var settings = (await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.EnableSso.Should().BeTrue();
        settings.IdpSettings.EntityId.Should().Be("https://idp.example.com/entity");
        settings.SpLoginLabel.Should().Be("Custom SSO Label");
    }

    [Fact]
    public async Task SaveSsoSettings_LoginLabelOver100Characters_IsCutTo100()
    {
        // Act
        await SaveAsync(SettingsJson(s => s["spLoginLabel"] = new string('L', 150)));
        var settings = (await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.SpLoginLabel.Should().HaveLength(100);
    }

    [Fact]
    public async Task SaveSsoSettings_UnrecognizedUsersType_IsNormalizedToUser()
    {
        // Act
        await SaveAsync(SettingsJson(s => s["usersType"] = 99));
        var settings = (await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.UsersType.Should().Be(4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task SaveSsoSettings_RecognizedUsersType_IsKept(int usersType)
    {
        // Act
        await SaveAsync(SettingsJson(s => s["usersType"] = usersType));
        var settings = (await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.UsersType.Should().Be(usersType);
    }

    [Fact]
    public async Task SaveSsoSettings_EmptySerializeSettings_ThrowsBadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SaveAsync(string.Empty));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Trait("Bug", "84685")]
    [Fact]
    public async Task SaveSsoSettings_MissingEntityId_ThrowsBadRequest()
    {
        // Arrange
        var settings = SettingsJson(s => s["idpSettings"] = new Dictionary<string, object?>
        {
            ["ssoUrl"] = "https://idp.example.com/sso",
            ["ssoBinding"] = ValidBinding
        });

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SaveAsync(settings));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Trait("Bug", "84686")]
    [Fact]
    public async Task SaveSsoSettings_MissingSsoUrl_ThrowsBadRequest()
    {
        // Arrange
        var settings = SettingsJson(s => s["idpSettings"] = new Dictionary<string, object?>
        {
            ["entityId"] = "https://idp.example.com/entity",
            ["ssoBinding"] = ValidBinding
        });

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SaveAsync(settings));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Trait("Bug", "84687")]
    [Fact]
    public async Task SaveSsoSettings_IncompleteFieldMapping_ThrowsBadRequest()
    {
        // Arrange
        var settings = SettingsJson(s => s["fieldMapping"] = new Dictionary<string, object?>
        {
            ["firstName"] = "a",
            ["lastName"] = "b"
        });

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SaveAsync(settings));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Trait("Bug", "84688")]
    [Fact]
    public async Task SaveSsoSettings_MalformedSloUrl_ThrowsBadRequest()
    {
        // Arrange
        var settings = SettingsJson(s => s["idpSettings"] = new Dictionary<string, object?>
        {
            ["entityId"] = "https://idp.example.com/entity",
            ["ssoUrl"] = "https://idp.example.com/sso",
            ["ssoBinding"] = ValidBinding,
            ["sloUrl"] = "not-a-url",
            ["sloBinding"] = ValidBinding
        });

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SaveAsync(settings));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}
