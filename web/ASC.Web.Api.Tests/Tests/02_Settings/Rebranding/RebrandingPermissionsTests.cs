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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Rebranding;

/// <summary>
/// Access control of the rebranding endpoints. The company, additional and licensor reads need a signed-in
/// caller but no admin right; the logos list is public; the rest needs the portal-settings right, which a
/// plain User lacks (checked before the plan gate for the logo-text save).
/// </summary>
[Trait("Category", "Settings")]
public class RebrandingPermissionsTests(AspireAppFixture fixture) : RebrandingTestBase(fixture)
{
    [Fact]
    public async Task GetCompanyWhiteLabelSettings_User_ReturnsSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var response = await Rebranding.GetCompanyWhiteLabelSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetCompanyWhiteLabelSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.GetCompanyWhiteLabelSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetAdditionalWhiteLabelSettings_User_ReturnsSettings()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var response = await Rebranding.GetAdditionalWhiteLabelSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAdditionalWhiteLabelSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.GetAdditionalWhiteLabelSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetLicensorData_User_ReturnsLicensor()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var response = await Rebranding.GetLicensorDataAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetLicensorData_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.GetLicensorDataAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetWhiteLabelLogos_Anonymous_ReturnsLogos()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var response = await Rebranding.GetWhiteLabelLogosAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetEnableWhitelabel_DocSpaceAdmin_ReturnsAvailability()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        // Act
        var response = await Rebranding.GetEnableWhitelabelAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetEnableWhitelabel_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.GetEnableWhitelabelAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetIsDefaultWhiteLabelLogos_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.GetIsDefaultWhiteLabelLogosAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetWhiteLabelLogoText_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.GetWhiteLabelLogoTextAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetIsDefaultWhiteLabelLogoText_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.GetIsDefaultWhiteLabelLogoTextAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RestoreWhiteLabelLogoText_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.RestoreWhiteLabelLogoTextAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SaveWhiteLabelLogoText_User_ThrowsAccessDeniedBeforePlanCheck()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveWhiteLabelLogoTextAsync(
                whiteLabelRequestDto: new WhiteLabelRequestDto(logoText: "Autotest Brand"),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RestoreWhiteLabelLogos_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.RestoreWhiteLabelLogosAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SaveWhiteLabelSettings_User_ThrowsAccessDeniedBeforePlanCheck()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveWhiteLabelSettingsAsync(
                whiteLabelRequestDto: new WhiteLabelRequestDto(logo: []),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SaveWhiteLabelSettingsFromFiles_User_ThrowsAccessDeniedBeforePlanCheck()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveWhiteLabelSettingsFromFilesAsync(
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SaveCompanyWhiteLabelSettings_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveCompanyWhiteLabelSettingsAsync(
                CompanyRequest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SaveAdditionalWhiteLabelSettings_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveAdditionalWhiteLabelSettingsAsync(
                AdditionalRequest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    // The two deletes reset default-tenant settings shared by every portal on the host, so the owner
    // call is not ported. A plain User is refused by the first line of the action (EditPortalSettings),
    // before anything is touched, which makes this one safe.
    [Fact]
    public async Task DeleteCompanyWhiteLabelSettings_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.DeleteCompanyWhiteLabelSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteAdditionalWhiteLabelSettings_User_ThrowsAccessDenied()
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteContact(EmployeeType.User));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.DeleteAdditionalWhiteLabelSettingsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
