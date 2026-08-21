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

namespace ASC.Files.Tests.Tests._03_Rooms.Tags;

/// <summary>
/// <c>DELETE /files/rooms/{id}/tags</c>: detach must not disturb the rest of the room, and must
/// compose correctly with the other tag endpoints. Positive/functional behavior of detach itself
/// lives in <see cref="RoomTagDetachTests"/>; validation lives in
/// <see cref="RoomTagDetachValidationTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagDetachIntegrationTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task DeleteRoomTags_DoesNotChangeRoomTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string title = "Autotest Title Preserved On Detach";
        var room = await CreateCustomRoom(title);
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["TitleTag"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["TitleTag"]), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be(title);
    }

    [Fact]
    public async Task DeleteRoomTags_DoesNotChangeRoomType()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Type Preserved On Detach");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["TypeTag"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["TypeTag"]), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.RoomType.Should().Be(RoomType.CustomRoom);
    }

    [Fact]
    public async Task DeleteRoomTags_DoesNotChangeCoverColorOrLogo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Cover Preserved On Detach");
        var coverId = await GetFirstCoverId();
        await _roomsApi.ChangeRoomCoverAsync(room.Id, new CoverRequestDto("FF5733", coverId), TestContext.Current.CancellationToken);

        var infoBefore = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var logoBefore = JsonSerializer.Serialize(infoBefore.Logo);

        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["CoverTag"]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["CoverTag"]), TestContext.Current.CancellationToken);

        // Assert
        var infoAfter = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        JsonSerializer.Serialize(infoAfter.Logo).Should().Be(logoBefore);
    }

    [Fact]
    public async Task DeleteRoomTags_DoesNotChangeSharingSettings()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Sharing Preserved On Detach");
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["SharingTag"]), TestContext.Current.CancellationToken);

        var shareBefore = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["SharingTag"]), TestContext.Current.CancellationToken);

        // Assert
        var shareAfter = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        shareAfter.Should().BeEquivalentTo(shareBefore);
    }

    [Fact]
    public async Task DeleteRoomTags_DoesNotAffectFilesOrFoldersInsideRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Contents Preserved On Detach");
        const string innerFolderTitle = "Inner Folder";
        await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder(innerFolderTitle), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ContentsTag"]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ContentsTag"]), TestContext.Current.CancellationToken);

        // Assert
        // FolderContentDtoInteger.Folders is typed List<FileEntryBaseDto>, which carries Title but
        // neither Id nor Logo — so the inner folder is identified by title, not id.
        var content = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Folders.Select(f => f.Title).Should().Contain(innerFolderTitle);
    }

    [Fact]
    public async Task DeleteRoomTags_RoomsListReflectsUpdatedTagsAfterDetach()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string roomTitle = "Autotest List Reflects Detach";
        var room = await CreateCustomRoom(roomTitle);
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ListTag"]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ListTag"]), TestContext.Current.CancellationToken);

        // Assert
        // FolderContentDtoInteger.Folders is typed List<FileEntryBaseDto>, which drops both "id"
        // and "tags" — read the raw JSON to get at the room's tags in the list response.
        using var response = await _filesClient.GetAsync(
            $"api/2.0/files/rooms?filterValue={Uri.EscapeDataString(roomTitle)}",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);

        var listedRoom = document.RootElement.GetProperty("response").GetProperty("folders").EnumerateArray()
            .Single(f => f.GetProperty("title").GetString() == roomTitle);
        var tags = listedRoom.TryGetProperty("tags", out var tagsElement)
            ? tagsElement.EnumerateArray().Select(t => t.GetString()).ToList()
            : [];

        tags.Should().NotContain("ListTag");
    }

    [Fact]
    public async Task DeleteRoomTags_GetRoomInfoReflectsUpdatedTagsAfterDetach()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetRoomInfo Detach");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["InfoTag"]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["InfoTag"]), TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        (info.Tags ?? []).Should().NotContain("InfoTag");
    }

    [Fact]
    public async Task DeleteRoomTags_FullLifecycle_CreateTagAddDetach()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "LifecycleTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Lifecycle");

        var attached = (await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken)).Response;
        (attached.Tags ?? []).Should().Contain(name);

        // Act
        var detached = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (detached.Tags ?? []).Should().NotContain(name);
    }

    [Fact]
    public async Task DeleteRoomTags_AfterDetach_GlobalTagCanStillBeDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "GlobalDeletableTag";
        var room = await CreateCustomRoom("Autotest Global Deletable");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(name);
    }

    [Fact]
    public async Task DeleteRoomTags_AfterGlobalDelete_TagCanBeReattachedBecauseAddRoomTagsAutoCreatesIt()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "RecreatableViaAdd";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Reattach After Global Delete");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(name);

        // Act — addRoomTags auto-creates a missing tag.
        var reattached = (await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (reattached.Tags ?? []).Should().Contain(name);
    }

    [Fact]
    public async Task DeleteRoomTags_DoesNotCreateMissingTagInCatalog()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "NeverCreatedByDetach";
        var listBefore = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        listBefore.Should().NotContain(name);

        var room = await CreateCustomRoom("Autotest Detach Does Not Create");

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Assert
        var listAfter = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        listAfter.Should().NotContain(name);
    }
}
