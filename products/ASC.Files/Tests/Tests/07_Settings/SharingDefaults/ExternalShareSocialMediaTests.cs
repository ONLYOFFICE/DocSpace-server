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

namespace ASC.Files.Tests.Tests._07_Settings.SharingDefaults;

/// <summary>
/// <c>PUT /files/settings/externalsocialmedia</c> - portal-wide and admin-only, like
/// <c>settings/external</c>. Enabling it only takes effect while external sharing itself is
/// enabled: the product ANDs the requested value with the current external-share state.
/// </summary>
[Trait("Category", "Settings")]
[Trait("Feature", "SharingDefaults")]
public class ExternalShareSocialMediaTests(AspireAppFixture fixture) : SharingDefaultsTestBase(fixture)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExternalShareSocialMedia_SetsValueAndReflectsInSettings(bool set)
    {
        // Arrange
        await _filesSettingsApi.ExternalShareAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken);

        // Act
        var result = await _filesSettingsApi.ExternalShareSocialMediaAsync(new DisplayRequestDto(set), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().Be(set);

        var settings = await GetFilesSettings();
        settings.ExternalShareSocialMedia.Should().Be(set);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExternalShareSocialMedia_RepeatedCall_IsIdempotent(bool set)
    {
        // Arrange
        await _filesSettingsApi.ExternalShareAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken);
        await _filesSettingsApi.ExternalShareSocialMediaAsync(new DisplayRequestDto(set), TestContext.Current.CancellationToken);

        // Act
        var result = await _filesSettingsApi.ExternalShareSocialMediaAsync(new DisplayRequestDto(set), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().Be(set);
    }

    [Fact]
    public async Task ExternalShareSocialMedia_NoBody_ReturnsBoolean()
    {
        // Act - sent raw: see SharingDefaultsTestBase.SendRawEmptyBodyPut.
        using var response = await SendRawEmptyBodyPut("api/2.0/files/settings/externalsocialmedia");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("response").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    [Fact]
    public async Task ExternalShareSocialMedia_ChangeByOwner_VisibleToDocSpaceAdmin()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesSettingsApi.ExternalShareAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken);
        await _filesSettingsApi.ExternalShareSocialMediaAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(admin);
        var settings = await GetFilesSettings();

        // Assert
        settings.ExternalShareSocialMedia.Should().BeTrue();
    }

    [Fact]
    public async Task ExternalShareSocialMedia_DisablingExternalShare_OverridesSocialMediaSetting()
    {
        // Arrange
        await _filesSettingsApi.ExternalShareAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken);
        await _filesSettingsApi.ExternalShareSocialMediaAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken);

        // Act
        await _filesSettingsApi.ExternalShareAsync(new DisplayRequestDto(false), TestContext.Current.CancellationToken);

        // Assert
        var settings = await GetFilesSettings();
        settings.ExternalShareSocialMedia.Should().BeFalse();
    }

    [Fact]
    public async Task ExternalShareSocialMedia_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.ExternalShareSocialMediaAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExternalShareSocialMedia_DocSpaceAdmin_CanChange(bool set)
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await _filesSettingsApi.ExternalShareAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken);

        // Act
        var result = await _filesSettingsApi.ExternalShareSocialMediaAsync(new DisplayRequestDto(set), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().Be(set);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task ExternalShareSocialMedia_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.ExternalShareSocialMediaAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You don't have enough permission to perform the operation");
    }

    [Fact]
    public async Task ExternalShareSocialMedia_TerminatedDocSpaceAdmin_Unauthorized()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        await TerminateUser(admin);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.ExternalShareSocialMediaAsync(new DisplayRequestDto(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
