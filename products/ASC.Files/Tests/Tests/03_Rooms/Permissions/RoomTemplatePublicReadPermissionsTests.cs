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

namespace ASC.Files.Tests.Tests._03_Rooms.Permissions;

[Trait("Category", "Rooms")]
public class RoomTemplatePublicReadPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetTemplatePublicSettings_OwnerOwnTemplate_ReturnsFlag(bool isPublic)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate($"Autotest GetPublic Owner {isPublic}", isPublic);

        // Act
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;

        // Assert
        actual.Should().Be(isPublic);
    }

    [Fact]
    public async Task GetTemplatePublicSettings_RoomAdminOwnPrivateTemplate_ReturnsFlag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(roomAdmin);
        var templateId = await CreateTemplate("Autotest GetPublic RoomAdmin Own", isPublic: false);

        // Act
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;

        // Assert
        actual.Should().BeFalse();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task GetTemplatePublicSettings_AdminForeignPublicTemplate_ReturnsFlag(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate($"Autotest GetPublic {employeeType} OthersPublic", isPublic: true);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;

        // Assert
        actual.Should().BeTrue();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task GetTemplatePublicSettings_AdminForeignPrivateTemplate_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate($"Autotest GetPublic {employeeType} OthersPrivate", isPublic: false);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetTemplatePublicSettings_OwnerReadsAdminTemplate_ReturnsFlag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        var templateId = await CreateTemplate("Autotest GetPublic OwnerReadsAdmin", isPublic: true);

        await _filesClient.Authenticate(Owner);

        // Act
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;

        // Assert
        actual.Should().BeTrue();
    }

    [Theory]
    [InlineData(EmployeeType.User, true)]
    [InlineData(EmployeeType.User, false)]
    [InlineData(EmployeeType.Guest, true)]
    [InlineData(EmployeeType.Guest, false)]
    public async Task GetTemplatePublicSettings_UserOrGuest_Forbidden(EmployeeType employeeType, bool isPublic)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate($"Autotest GetPublic {employeeType} {isPublic}", isPublic);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetTemplatePublicSettings_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest GetPublic Anon", isPublic: true);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
