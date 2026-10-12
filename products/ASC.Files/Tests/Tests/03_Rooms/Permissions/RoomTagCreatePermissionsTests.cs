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
public class RoomTagCreatePermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task CreateTag_AllowedRoles_Created(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var response = await _roomsApi.CreateRoomTagAsync(
            new CreateTagRequestDto("Autotest Tag"),
            TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be("Autotest Tag");
        response.Count.Should().Be(1);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task CreateTag_UserOrGuest_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTagAsync(
                new CreateTagRequestDto("Autotest Tag"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task CreateTag_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTagAsync(
                new CreateTagRequestDto("Autotest Tag"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CreateTag_TerminatedUser_Unauthorized()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        await TerminateUser(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTagAsync(
                new CreateTagRequestDto("Autotest Tag"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetTags_Owner_SeesOwnTag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("OwnerVisibleTag"), TestContext.Current.CancellationToken);

        // Act
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        tags.Should().Contain("OwnerVisibleTag");
    }

    [Fact]
    public async Task GetTags_DocSpaceAdmin_SeesOwnersTag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("AdminVisibleTag"), TestContext.Current.CancellationToken);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        tags.Should().Contain("AdminVisibleTag");
    }

    /// <summary>
    /// A member only sees tags of rooms visible to them — the list is empty when no such room exists.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetTags_MemberWithoutVisibleRooms_ReturnsEmpty(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto($"{employeeType}VisibleTag"), TestContext.Current.CancellationToken);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTags_RoomAdmin_SeesOwnRoomTagsOnly()
    {
        // Arrange
        const string ownTag = "RoomAdminOwnTag";
        const string otherTag = "OwnerOnlyTag";

        await _filesClient.Authenticate(Owner);
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(roomAdmin);
        var ownRoom = await CreateCustomRoom("Autotest RoomAdmin Own Tagged Room");
        await _roomsApi.AddRoomTagsAsync(ownRoom.Id, new BatchTagsRequestDto([ownTag]), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var otherRoom = await CreateCustomRoom("Autotest Owner Tagged Room Hidden From RoomAdmin");
        await _roomsApi.AddRoomTagsAsync(otherRoom.Id, new BatchTagsRequestDto([otherTag]), TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(roomAdmin);
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        tags.Should().Contain(ownTag);
        tags.Should().NotContain(otherTag);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetTags_InvitedMember_SeesTagsOfInvitedRoomsOnly(EmployeeType employeeType)
    {
        // Arrange
        var visibleTag = $"{employeeType}SharedRoomTag";
        var hiddenTag = $"{employeeType}HiddenRoomTag";

        await _filesClient.Authenticate(Owner);
        var member = await InviteMember(employeeType);

        var sharedRoom = await CreateCustomRoom($"Autotest {employeeType} Shared Room");
        await _roomsApi.AddRoomTagsAsync(sharedRoom.Id, new BatchTagsRequestDto([visibleTag]), TestContext.Current.CancellationToken);
        await InviteToRoom(sharedRoom.Id, member, FileShare.Editing);

        var hiddenRoom = await CreateCustomRoom($"Autotest {employeeType} Hidden Room");
        await _roomsApi.AddRoomTagsAsync(hiddenRoom.Id, new BatchTagsRequestDto([hiddenTag]), TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(member);
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        tags.Should().Contain(visibleTag);
        tags.Should().NotContain(hiddenTag);
    }

    [Fact]
    public async Task GetTags_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
