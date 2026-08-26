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

namespace ASC.Files.Tests.Tests._03_Rooms.Logos;

/// <summary>
/// <c>POST /api/2.0/files/logos</c> — access control: uploading a temporary logo image is
/// portal-management, not room-management, so it is gated by employee type alone, the same for
/// every room.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLogoUploadPermissionsTests(
    AspireAppFixture fixture)
    : RoomLogoTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task Upload_AdminOrOwner_Succeeds(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var tmpFile = await UploadLogo(CreateTestImageBytes());

        // Assert
        tmpFile.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Upload_User_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadLogo(CreateTestImageBytes()));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Upload_Guest_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadLogo(CreateTestImageBytes()));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Upload_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadLogo(CreateTestImageBytes()));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
