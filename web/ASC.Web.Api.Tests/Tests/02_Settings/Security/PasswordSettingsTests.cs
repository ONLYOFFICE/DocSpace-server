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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Security;

/// <summary>
/// GET/PUT /api/2.0/settings/security/password — the portal password complexity requirements.
/// Unlike the sibling login settings, password settings are readable by any authenticated role;
/// only a portal owner or a DocSpaceAdmin may change them.
/// </summary>
[Trait("Category", "Settings")]
public class PasswordSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetPasswordSettings_Owner_ReturnsDefaultSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var settings = await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.MinLength.Should().Be(8);
        settings.Response.UpperCase.Should().BeFalse();
        settings.Response.Digits.Should().BeFalse();
        settings.Response.SpecSymbols.Should().BeFalse();
    }

    [Fact]
    public async Task GetPasswordSettings_User_CanRead()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var settings = await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.MinLength.Should().Be(8);
    }

    [Fact]
    public async Task GetPasswordSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UpdatePasswordSettings_Owner_UpdatesSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new PasswordSettingsRequestsDto(12, true, true, true);

        // Act
        var updated = await _securityApi.UpdatePasswordSettingsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        updated.StatusCode.Should().Be(200);
        updated.Response.MinLength.Should().Be(12);
        updated.Response.UpperCase.Should().BeTrue();
        updated.Response.Digits.Should().BeTrue();
        updated.Response.SpecSymbols.Should().BeTrue();

        var after = await _securityApi.GetPasswordSettingsAsync(TestContext.Current.CancellationToken);
        after.Response.MinLength.Should().Be(12);
    }

    [Fact]
    public async Task UpdatePasswordSettings_DocSpaceAdmin_UpdatesSettings()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);
        var request = new PasswordSettingsRequestsDto(15, true, false, false);

        // Act
        var updated = await _securityApi.UpdatePasswordSettingsAsync(request, TestContext.Current.CancellationToken);

        // Assert
        updated.StatusCode.Should().Be(200);
        updated.Response.MinLength.Should().Be(15);
        updated.Response.UpperCase.Should().BeTrue();
    }

    [Theory]
    [InlineData(999)]
    [InlineData(-5)]
    public async Task UpdatePasswordSettings_OutOfRangeMinLength_ThrowsValidationError(int minLength)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new PasswordSettingsRequestsDto(minLength, false, false, false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.UpdatePasswordSettingsAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("MinLength");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UpdatePasswordSettings_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);
        var request = new PasswordSettingsRequestsDto(10, false, false, false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _securityApi.UpdatePasswordSettingsAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
