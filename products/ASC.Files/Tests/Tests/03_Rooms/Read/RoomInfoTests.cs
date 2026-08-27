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

namespace ASC.Files.Tests.Tests._03_Rooms.Read;

/// <summary>
/// GET /files/rooms/:id - basic contract, and how the response holds up across actions that touch
/// the room afterwards (share, archive/unarchive, repeated reads, failed no-op writes).
/// </summary>
[Trait("Category", "Rooms")]
public class RoomInfoTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.EditingRoom)]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.FillingFormsRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task GetRoomInfo_OwnerReadsRoom_ReturnsCreatedData(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = $"Autotest GetInfo {roomType}";
        var created = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: roomType), TestContext.Current.CancellationToken)).Response;

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(created.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Id.Should().Be(created.Id);
        info.Title.Should().Be(title);
        info.RoomType.Should().Be(roomType);
    }

    [Fact]
    public async Task GetRoomInfo_RoomWithoutCustomQuota_IsCustomQuotaFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Default Quota");

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.IsCustomQuota.Should().NotBe(true);
    }

    [Fact]
    public async Task GetRoomInfo_SharingWithUser_DoesNotBreakOwnerRead()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Share Reflection");
        var user = await InviteContact(EmployeeType.User);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Id.Should().Be(room.Id);
        info.Access.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRoomInfo_ArchivedRoom_RootFolderTypeIsArchive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Archive Reflection");

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.RootFolderType.Should().Be(FolderType.Archive);
    }

    [Fact]
    public async Task GetRoomInfo_UnarchivedRoom_RootFolderTypeIsVirtualRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unarchive Reflection");

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.RootFolderType.Should().Be(FolderType.VirtualRooms);
    }

    [Fact]
    public async Task GetRoomInfo_RepeatedCalls_ReturnTheSameState()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Idempotent Read");

        // Act
        var a = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var b = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var c = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        b.Id.Should().Be(a.Id);
        c.Id.Should().Be(a.Id);
        b.Title.Should().Be(a.Title);
        c.Title.Should().Be(a.Title);
        b.RoomType.Should().Be(a.RoomType);
        c.RoomType.Should().Be(a.RoomType);
    }

    [Fact]
    public async Task GetRoomInfo_FailedTagDeletion_DoesNotChangeTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var tagName = "AutotestStableTag" + Guid.NewGuid().ToString()[..8];
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom("Autotest Tag Stability");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act - deleting a tag the room never had is a no-op
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["AutotestNoSuchTag"]), TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Tags.Should().Contain(tagName);
    }

    [Fact]
    public async Task GetRoomInfo_FailedTitleUpdate_LeavesOriginalTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var original = "Autotest Title Stability";
        var room = await CreateCustomRoom(original);

        // Act - an empty title is rejected, so the update must not take effect
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: ""), TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be(original);
    }

    [Fact]
    public async Task GetRoomInfo_AfterAsyncArchiveCompletes_ReflectsArchivedState()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Final State");

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.RootFolderType.Should().Be(FolderType.Archive);
    }
}
