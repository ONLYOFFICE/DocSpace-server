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
public class RoomTagDeletePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task DeleteCustomTags_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Tag"), TestContext.Current.CancellationToken);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteCustomTagsAsync(
                new BatchTagsRequestDto(["Autotest Tag"]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteCustomTags_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Tag"), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteCustomTagsAsync(
                new BatchTagsRequestDto(["Autotest Tag"]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task DeleteCustomTags_TerminatedUser_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Tag"), TestContext.Current.CancellationToken);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        await TerminateUser(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteCustomTagsAsync(
                new BatchTagsRequestDto(["Autotest Tag"]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    // Only Owner and DocSpaceAdmin may read tag-linkage info. RoomAdmin/User/Guest get 403,
    // anonymous gets 401. Note: RoomAdmin is forbidden here, unlike POST /files/tags
    // (CreateRoomTag), where RoomAdmin is allowed.

    [Fact]
    public async Task HasTagLinks_Owner_ReturnsTrue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await SeedLinkedTag("OwnerHasLinksTag");

        // Act
        var hasLinks = (await _roomsApi.HasTagLinksAsync("OwnerHasLinksTag", "OwnerHasLinksTag", TestContext.Current.CancellationToken)).Response;

        // Assert
        hasLinks.Should().BeTrue();
    }

    [Fact]
    public async Task HasTagLinks_DocSpaceAdmin_ReturnsTrue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await SeedLinkedTag("AdminHasLinksTag");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var hasLinks = (await _roomsApi.HasTagLinksAsync("AdminHasLinksTag", "AdminHasLinksTag", TestContext.Current.CancellationToken)).Response;

        // Assert
        hasLinks.Should().BeTrue();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task HasTagLinks_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        var tagName = $"{employeeType}HasLinksTag";

        await _filesClient.Authenticate(Owner);
        await SeedLinkedTag(tagName);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task HasTagLinks_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await SeedLinkedTag("AnonHasLinksTag");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.HasTagLinksAsync("AnonHasLinksTag", "AnonHasLinksTag", TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
