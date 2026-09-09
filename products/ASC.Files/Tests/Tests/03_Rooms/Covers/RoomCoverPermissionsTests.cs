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

namespace ASC.Files.Tests.Tests._03_Rooms.Covers;

[Trait("Category", "Rooms")]
public class RoomCoverPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    private const string CoverColor = "FF5733";

    #region GET /files/rooms/covers - access control

    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetCovers_PortalMember_ReturnsGallery(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var covers = (await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        covers.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Bug", "81012")]
    public async Task GetCovers_Guest_Forbidden()
    {
        // Arrange
        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetCovers_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion

    #region PUT /files/rooms/{id}/cover - access control

    // Changing a room cover is room-management: the room owner may do it, and so may a member
    // invited with RoomManager access. Everyone else — including a DocSpaceAdmin who is not a
    // member of the room — gets 403.

    [Fact]
    public async Task ChangeCover_OwnerOwnRoom_Changed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Owner Room");

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto(CoverColor, coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(room.Id);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task ChangeCover_OwnRoom_Changed(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var member = await InviteMember(employeeType);

        await _filesClient.Authenticate(member);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom($"Autotest Cover {employeeType} Own Room");

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto(CoverColor, coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(room.Id);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task ChangeCover_ForeignRoomWithoutInvitation_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom($"Autotest Cover {employeeType} Room");

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto(CoverColor, coverId),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task ChangeCover_InvitedRoomManager_Changed(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom($"Autotest Cover {employeeType} RoomManager");

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, FileShare.RoomManager);

        await _filesClient.Authenticate(member);

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto(CoverColor, coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(room.Id);
    }

    /// <summary>
    /// No access level below RoomManager lets an invited user change the cover. RoomManager itself
    /// cannot be granted to a User, so the highest level a User can reach here is ContentCreator.
    /// </summary>
    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.ContentCreator)]
    public async Task ChangeCover_InvitedUser_Forbidden(FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom($"Autotest Cover User {access} Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, access);

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto(CoverColor, coverId),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task ChangeCover_UserInAdminRoom_Forbidden(EmployeeType roomOwnerType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomOwner = await InviteMember(roomOwnerType);

        await _filesClient.Authenticate(roomOwner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom($"Autotest Cover User In {roomOwnerType} Room");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto(CoverColor, coverId),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ChangeCover_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Cover Anon Room");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto(CoverColor),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion
}
