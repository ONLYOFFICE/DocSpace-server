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

namespace ASC.Files.Tests.Tests._03_Rooms.Deletion;

/// <summary>
/// <c>DELETE /files/rooms/{id}</c> — positive/functional coverage: the room disappears from every
/// listing and from <c>getRoomInfo</c>, across every room type, together with everything a room can
/// carry (files, tags, cover, logo, external link, per-room quota, an outstanding share). The async
/// operation contract and edge cases live in <see cref="RoomDeleteAsyncTests"/>; id/body validation
/// in <see cref="RoomDeleteValidationTests"/>; access control in
/// <see cref="ASC.Files.Tests.Tests._03_Rooms.Permissions.RoomDeletePermissionsTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomDeleteTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task DeleteRoom_Owner_DisappearsFromListAndGetRoomInfo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Verify Gone");

        // Act
        var response = await _roomsApi.DeleteRoomWithHttpInfoAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");

        var list = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Folders.Should().NotContain(f => f.Title == room.Title);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.EditingRoom)]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.FillingFormsRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task DeleteRoom_EveryRoomType_Deleted(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto($"Autotest Delete {roomType}", roomType: roomType), TestContext.Current.CancellationToken)).Response;

        // Act
        var response = await _roomsApi.DeleteRoomWithHttpInfoAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoom_ResponseIsFileOperationWrapper()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Response Shape");

        // Act
        var response = (await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().NotBeNullOrEmpty();

        await WaitLongOperation();
    }

    [Fact]
    public async Task DeleteRoom_ArchivedRoom_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Archived");
        await ArchiveRoom(room.Id);

        // Act
        var response = await _roomsApi.DeleteRoomWithHttpInfoAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoom_RoomWithFiles_FilesAreDeletedToo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Room With Files");
        var file = await CreateFile("delete-me.docx", room.Id);

        // Act
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");

        var roomException = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken));
        roomException.ErrorCode.Should().Be(404);

        var fileException = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));
        fileException.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoom_TagUsedOnlyByDeletedRoom_RemovedFromCatalog()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "Autotest Tag Single Use";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom("Autotest Delete Room With Single-Use Tag");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().OnlyContain(o => o.Finished);
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        tags.Should().NotContain(t => t.ToString() == tagName);
    }

    [Fact]
    public async Task DeleteRoom_TagStillUsedByAnotherRoom_StaysInCatalog()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "Autotest Tag Shared";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom("Autotest Delete Room With Shared Tag");
        var keeperRoom = await CreateCustomRoom("Autotest Keeper Room With Shared Tag");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(keeperRoom.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().OnlyContain(o => o.Finished);
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        tags.Should().Contain(t => t.ToString() == tagName);
    }

    [Fact]
    public async Task DeleteRoom_RoomWithCover_DeletedSuccessfully()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Delete Room With Cover");
        await _roomsApi.ChangeRoomCoverAsync(room.Id, new CoverRequestDto("FF5733", coverId), TestContext.Current.CancellationToken);

        // Act
        var response = await _roomsApi.DeleteRoomWithHttpInfoAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
    }

    [Fact]
    public async Task DeleteRoom_RoomWithLogo_DeletedSuccessfully()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Room With Logo");
        await ApplyMinimalLogo(room.Id);

        // Act
        var response = await _roomsApi.DeleteRoomWithHttpInfoAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
    }

    [Fact]
    public async Task DeleteRoom_RoomSharedToUser_NoLongerVisibleToThatUser()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Shared Room");
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        // Before deletion, the user can read the room.
        await _filesClient.Authenticate(user);
        var beforeInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        beforeInfo.Id.Should().Be(room.Id);

        // Act
        await _filesClient.Authenticate(Owner);
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await _filesClient.Authenticate(user);
        var afterException = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken));
        afterException.ErrorCode.Should().Be(404);

        var list = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Folders.Should().NotContain(f => f.Title == room.Title);
    }

    [Fact]
    public async Task DeleteRoom_PublicRoomWithPrimaryExternalLink_FullyRemoved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Delete PublicRoom");
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        link.SharedLink?.ShareLink.Should().NotBeNullOrEmpty();

        // Act
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");

        var infoException = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken));
        infoException.ErrorCode.Should().Be(404);

        var linkException = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, TestContext.Current.CancellationToken));
        linkException.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteRoom_RoomWithPerRoomQuota_DeletedSuccessfully()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(100 * 1024 * 1024)),
            TestContext.Current.CancellationToken);

        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Delete Quota Room", quota: 10 * 1024 * 1024, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var response = await _roomsApi.DeleteRoomWithHttpInfoAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
    }

    /// <summary>Uploads and applies the minimal valid 1x1 PNG as a room's logo.</summary>
    private async Task ApplyMinimalLogo(int roomId)
    {
        const string base64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";

        await using var stream = new MemoryStream(Convert.FromBase64String(base64Png));
        var uploaded = (await _roomsApi.UploadRoomLogoAsync(new FileParameter("logo.png", "image/png", stream), TestContext.Current.CancellationToken)).Response;
        var tmpFile = uploaded.Data?.ToString() ?? string.Empty;

        await _roomsApi.CreateRoomLogoAsync(roomId, new LogoRequest(tmpFile, 0, 0, 1, 1), TestContext.Current.CancellationToken);
    }
}
