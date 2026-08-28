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

namespace ASC.Files.Tests.Tests._07_Settings.Storage;

/// <summary>
/// <c>PUT /files/forcesave</c> and <c>PUT /files/storeforcesave</c> - both toggle the caller's own
/// setting, so every authenticated role (down to Guest) may call them; only anonymous and
/// terminated callers are rejected.
/// </summary>
[Trait("Category", "Settings")]
public class ForceSavePermissionsTests(
    AspireAppFixture fixture)
    : StorageSettingsTestBase(fixture)
{
    [Fact]
    public async Task Forcesave_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.ForcesaveAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task Forcesave_Owner_CanChangeSetting()
    {
        // Act
        var response = await _filesSettingsApi.ForcesaveAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task Forcesave_AnyRole_CanChangeOwnSetting(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var response = await _filesSettingsApi.ForcesaveAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task Forcesave_TerminatedUser_Unauthorized()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);
        await TerminateUser(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.ForcesaveAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task StoreForcesave_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.StoreForcesaveAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task StoreForcesave_Owner_CanChangeSetting()
    {
        // Act
        var response = await _filesSettingsApi.StoreForcesaveAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task StoreForcesave_AnyRole_CanChangeOwnSetting(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var response = await _filesSettingsApi.StoreForcesaveAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task StoreForcesave_TerminatedUser_Unauthorized()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);
        await TerminateUser(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.StoreForcesaveAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
