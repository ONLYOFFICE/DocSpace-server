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

namespace ASC.Files.Tests.Tests._01_Files.Order;

/// <summary>
/// Permission coverage of <c>PUT /files/{fileId}/order</c>: every portal role can order its own
/// files in My Documents, but not somebody else's.
/// </summary>
[Trait("Category", "Files")]
public class FileOrderPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetFileOrder_Owner_OwnFile_Succeeds()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Order Owner File", Owner);

        // Act
        var result = await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(1), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Id.Should().Be(file.Id);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task SetFileOrder_Member_OwnFile_Succeeds(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteContact(employeeType);
        var file = await CreateFileInMy($"Autotest Order {employeeType} File", member);

        // Act
        var result = await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(2), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task SetFileOrder_UserOnAnotherUsersPrivateFile_Forbidden()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Order Private File", Owner);
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(1), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetFileOrder_GuestOnAnotherUsersFile_Forbidden()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Order Guest File", Owner);
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(1), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetFileOrder_Anonymous_Unauthorized()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Order Anon File", Owner);
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(1), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
