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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;

/// <summary>
/// <c>POST /files/rooms/thirdparty/{id}</c> and the room endpoints over a room whose identifier is
/// a string: creating a room on a connected Nextcloud account and reading it back through the
/// string overloads of the SDK (<c>GetRoomInfoAsync(string)</c>, <c>GetFolderByFolderIdAsync(string)</c>,
/// <c>DeleteRoomAsync(string)</c>). Skipped unless Nextcloud is configured in the environment.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Requires", "Nextcloud")]
[Collection("Nextcloud")]
public class ThirdPartyRoomTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    [Fact]
    public async Task CreateRoomThirdParty_ConnectedFolder_ReturnsRoomWithStringId()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var title = UniqueTitle("TP Room Create");
        var connection = await ConnectNextcloud($"{title} (storage)");

        // Act
        var room = (await _roomsApi.CreateRoomThirdPartyAsync(
            connection.Id,
            new CreateThirdPartyRoom(title: title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Id.Should().Be(connection.Id, "the connected folder itself becomes the room");
        room.RoomType.Should().Be(RoomType.CustomRoom);
        room.RootFolderType.Should().Be(FolderType.VirtualRooms);
        room.ProviderKey.Should().Be(NextcloudProviderKey);
        room.ProviderId.Should().Be(connection.ProviderId);

        // The response still carries the connection's title ("... (storage)"): the room title is
        // stored on the provider after the folder was read. The room is read again for the title.
        var reread = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        reread.Title.Should().Be(title);
    }

    [Fact]
    public async Task GetRoomInfo_StringId_ReturnsThirdPartyRoom()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var title = UniqueTitle("TP Room Info");
        var (providerId, folderId, roomId) = await CreateNextcloudRoom(title);

        // Act
        var room = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Id.Should().Be(folderId);
        room.Title.Should().Be(title);
        room.RoomType.Should().Be(RoomType.CustomRoom);
        room.ProviderKey.Should().Be(NextcloudProviderKey);
        room.ProviderId.Should().Be(providerId);
    }

    [Fact]
    public async Task GetRoomsFolder_ThirdPartyRoom_IsListedWithProviderKey()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var title = UniqueTitle("TP Room Listed");
        await CreateNextcloudRoom(title);

        // Act
        var rooms = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var listed = rooms.Folders.Should().ContainSingle(f => f.Title == title).Subject;
        listed.ProviderKey.Should().Be(NextcloudProviderKey);
    }

    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.EditingRoom)]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.FillingFormsRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task CreateRoomThirdParty_SupportedRoomType_CreatesRoomOfThatType(RoomType roomType)
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);

        // Act
        var (_, _, roomId) = await CreateNextcloudRoom(UniqueTitle($"TP Room {roomType}"), roomType);

        // Assert
        var room = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        room.RoomType.Should().Be(roomType);
    }

    [Fact]
    public async Task CreateRoomThirdParty_ConnectionAlreadyBacksRoom_Forbidden()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, folderId, _) = await CreateNextcloudRoom(UniqueTitle("TP Room Reused"));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomThirdPartyAsync(
                folderId,
                new CreateThirdPartyRoom(title: UniqueTitle("TP Room Second"), roomType: RoomType.EditingRoom),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403, "one connection can back one room only");
    }

    [Fact]
    public async Task GetFolderByFolderId_RoomId_ReturnsRoomAsCurrentFolder()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var title = UniqueTitle("TP Room Content");
        var (_, _, roomId) = await CreateNextcloudRoom(title);

        // Act
        var content = (await _foldersApi.GetFolderByFolderIdAsync(
            roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        content.Current.Id.Should().Be(roomId);
        content.Current.Title.Should().Be(title);
        content.Current.RoomType.Should().Be(RoomType.CustomRoom);
        content.Current.RootFolderType.Should().Be(FolderType.VirtualRooms);
    }

    [Fact]
    public async Task DeleteRoom_ThirdPartyRoom_RemovesRoomAndConnection()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var title = UniqueTitle("TP Room Delete");
        // Deleting a room walks its whole subtree for permission checks, and the shared account
        // root is large - the room is a subfolder of its own here so the operation stays short.
        var (_, _, roomId) = await CreateNextcloudRoom(title, createAsNewFolder: true);

        // Act
        var operation = (await _roomsApi.DeleteRoomAsync(
            roomId, new DeleteRoomRequest(true), TestContext.Current.CancellationToken)).Response;
        var finished = await WaitLongOperation(operation.Id);

        // Assert
        finished.Should().NotBeNull();
        finished!.Should().OnlyContain(r => r.Finished && string.IsNullOrEmpty(r.Error));

        var rooms = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        rooms.Folders.Should().NotContain(f => f.Title == title);

        (await ConnectedAccountTitles()).Should().NotContain($"{title} (storage)",
            "deleting a third-party room disconnects the account behind it");
    }
}
