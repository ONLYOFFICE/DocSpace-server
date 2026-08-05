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
public class RoomTagDetachPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task DeleteRoomTags_OwnRoom_TagDetached(EmployeeType? employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        var tagName = $"Autotest {employeeType?.ToString() ?? "Owner"} DetachTag";
        var room = await CreateCustomRoom($"{tagName} Room");

        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(
            room.Id,
            new BatchTagsRequestDto([tagName]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().NotContain(tagName);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task DeleteRoomTags_MemberOutsideRoom_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        var tagName = $"Autotest {employeeType} Outside Detach Tag";

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest {employeeType} Outside Detach Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(
                room.Id,
                new BatchTagsRequestDto([tagName]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteRoomTags_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Anon Detach Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["Autotest Anon Detach Tag"]), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(
                room.Id,
                new BatchTagsRequestDto(["Autotest Anon Detach Tag"]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task DeleteRoomTags_TerminatedRoomManager_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Disabled Detach Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["Autotest Disabled Detach Tag"]), TestContext.Current.CancellationToken);

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.RoomManager);

        await _filesClient.Authenticate(user);
        await TerminateUser(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(
                room.Id,
                new BatchTagsRequestDto(["Autotest Disabled Detach Tag"]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// Detaching a tag is room-metadata management: only RoomManager access allows it.
    /// </summary>
    [Theory]
    [MemberData(nameof(RoomAccessData.InvitedMemberAccessesForTagging), MemberType = typeof(RoomAccessData))]
    public async Task DeleteRoomTags_InvitedMember_MatchesAccessLevel(EmployeeType employeeType, FileShare access)
    {
        // Arrange
        var tagName = $"Autotest Detach {employeeType} {access}";

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Detach Room {employeeType} {access}");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);

        // Act & Assert
        if (access == FileShare.RoomManager)
        {
            var updated = (await _roomsApi.DeleteRoomTagsAsync(
                room.Id,
                new BatchTagsRequestDto([tagName]),
                TestContext.Current.CancellationToken)).Response;

            (updated.Tags ?? []).Should().NotContain(tagName);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.DeleteRoomTagsAsync(
                    room.Id,
                    new BatchTagsRequestDto([tagName]),
                    TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(403);
        }
    }
}
