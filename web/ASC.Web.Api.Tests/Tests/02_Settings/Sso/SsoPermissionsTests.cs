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
/// Access control of the SSO endpoints. The configuration endpoints need the portal-settings right (a RoomAdmin is
/// treated like a plain user, the right check runs before anything else), the constants need only a signed-in
/// caller, and an anonymous caller is rejected everywhere except the plain read.
/// </summary>
[Trait("Category", "Settings")]
public class SsoPermissionsTests(AspireAppFixture fixture) : SsoTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task GetDefaultSsoSettings_NonAdmin_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Sso.GetDefaultSsoSettingsV2Async(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetDefaultSsoSettings_DocSpaceAdmin_ReturnsSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        var settings = await Sso.GetDefaultSsoSettingsV2Async(TestContext.Current.CancellationToken);

        // Assert
        settings.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDefaultSsoSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Sso.GetDefaultSsoSettingsV2Async(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetSsoSettingsConstants_User_ReturnsConstants()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var constants = await Sso.GetSsoSettingsV2ConstantsAsync(TestContext.Current.CancellationToken);

        // Assert
        constants.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSsoSettingsConstants_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Sso.GetSsoSettingsV2ConstantsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task GetSsoSettings_NonAdmin_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Sso.GetSsoSettingsV2Async(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task SaveSsoSettings_NonAdmin_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SaveAsync("{}"));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SaveSsoSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SaveAsync("{}"));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task ResetSsoSettings_NonAdmin_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Sso.ResetSsoSettingsV2Async(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ResetSsoSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Sso.ResetSsoSettingsV2Async(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
