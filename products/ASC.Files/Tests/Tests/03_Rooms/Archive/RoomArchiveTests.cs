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

namespace ASC.Files.Tests.Tests._03_Rooms.Archive;

/// <summary>
/// <c>PUT /files/rooms/{id}/archive</c> — functional coverage: the archive/unarchive lifecycle,
/// what happens to a room's content and metadata across it, that an archived room is read-only,
/// and body validation. Access control lives in
/// <see cref="ASC.Files.Tests.Tests._03_Rooms.Permissions.RoomArchivePermissionsTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomArchiveTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task ArchiveRoom_OwnerArchivesOwnRoom_FullUnarchiveCycle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Archive Lifecycle Room");

        // Act - archive
        var archiveResponse = await _roomsApi.ArchiveRoomWithHttpInfoAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var archiveOperations = await WaitLongOperation();

        // Assert - archive returns 200 and the operation finishes
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        archiveOperations.Should().OnlyContain(o => o.Finished && o.Error == "");

        // Assert - archived room is in the Archive list, not the Active one
        (await GetRoomTitlesIn(SearchArea.Archive)).Should().Contain(room.Title);
        (await GetRoomTitlesIn(SearchArea.Active)).Should().NotContain(room.Title);

        // Act - unarchive
        var unarchiveResponse = await _roomsApi.UnarchiveRoomWithHttpInfoAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var unarchiveOperations = await WaitLongOperation();

        // Assert - unarchive returns 200 and the operation finishes
        unarchiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        unarchiveOperations.Should().OnlyContain(o => o.Finished && o.Error == "");

        // Assert - the room is back in the Active list and no longer in the Archive one
        (await GetRoomTitlesIn(SearchArea.Active)).Should().Contain(room.Title);
        (await GetRoomTitlesIn(SearchArea.Archive)).Should().NotContain(room.Title);
    }

    [Fact]
    public async Task ArchiveRoom_ContentIsPreserved_FileAndFolderSurvive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Archive Content Room");
        var file = await CreateFile("Autotest File Before Archive", room.Id);
        var folder = await CreateFolder("Autotest Folder Before Archive", room.Id);

        // Act
        await ArchiveRoom(room.Id);

        // Assert
        // FolderContentDtoInteger.Files/Folders are typed List<FileEntryBaseDto>, which drops Id -
        // an SDK gap noted in the tests rule, so membership is asserted by Title instead.
        var content = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Files.Should().Contain(f => f.Title == file.Title);
        content.Folders.Should().Contain(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task ArchiveRoom_ArchivedRoomIsReadOnly_WriteOperationsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("AutotestArchiveReadonlyTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Archive ReadOnly Room");
        var user = await InviteMember(EmployeeType.User);

        await ArchiveRoom(room.Id);

        // Act / Assert - createFolder is forbidden
        var createFolderException = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Autotest Folder In Archive"), TestContext.Current.CancellationToken));
        createFolderException.ErrorCode.Should().Be(403);

        // Act / Assert - rename (updateRoom) is forbidden
        var renameException = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest("Renamed Archived Room"), TestContext.Current.CancellationToken));
        renameException.ErrorCode.Should().Be(403);

        // Act / Assert - addRoomTags is forbidden
        var addTagsException = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["AutotestArchiveReadonlyTag"]), TestContext.Current.CancellationToken));
        addTagsException.ErrorCode.Should().Be(403);

        // Act / Assert - setRoomSecurity is forbidden
        var shareException = await Assert.ThrowsAsync<ApiException>(
            async () => await InviteToRoom(room.Id, user, FileShare.Editing));
        shareException.ErrorCode.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "81551")]
    public async Task ArchiveRoom_CreateFileInArchivedRoom_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Archive ReadOnly Room For createFile");

        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateFileAsync(room.Id, new CreateFileJsonElement("Autotest File In Archive"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ArchiveRoom_MetadataIsPreserved_ThroughArchiveUnarchiveCycle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("AutotestMetaTagA"), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("AutotestMetaTagB"), TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom("Autotest Archive Metadata Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["AutotestMetaTagA", "AutotestMetaTagB"]), TestContext.Current.CancellationToken);

        await ArchiveRoom(room.Id);

        // Act
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Title.Should().Be("Autotest Archive Metadata Room");
        info.RoomType.Should().Be(RoomType.CustomRoom);
        info.Tags.Should().Contain(["AutotestMetaTagA", "AutotestMetaTagB"]);
    }

    [Fact]
    public async Task ArchiveRoom_NullDeleteAfter_ReturnsBadRequest()
    {
        // Arrange - the DTO's deleteAfter is a non-nullable bool, so a JSON null can only be sent
        // as a raw request; the typed client cannot construct it.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Archive Null deleteAfter");

        // Act
        using var response = await SendRawArchive(room.Id, "{\"deleteAfter\":null}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ArchiveRoom_InvalidDeleteAfterTypeString_ReturnsBadRequest()
    {
        // Arrange - same reasoning: a string value for a bool property cannot be expressed by the
        // typed DTO.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Archive Invalid deleteAfter Type");

        // Act
        using var response = await SendRawArchive(room.Id, "{\"deleteAfter\":\"false\"}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<List<string>> GetRoomTitlesIn(SearchArea area)
    {
        var list = (await _roomsApi.GetRoomsFolderAsync(searchArea: area, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return list.Folders.ConvertAll(f => f.Title);
    }

    private async Task<HttpResponseMessage> SendRawArchive(int roomId, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/2.0/files/rooms/{roomId}/archive")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
