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

namespace ASC.Files.Tests.Tests._05_Features;

[Trait("Category", "Features")]
[Trait("Feature", "Recent")]
public class RecentTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task AddToRecent_FileFromMyDocuments_AppearsInRecent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("recent_my.docx", Owner);

        // Act
        await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var recent = await GetRecentAsync(Owner);
        recent.Files.Should().ContainSingle(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddToRecent_FileFromSubFolderInMyDocuments_AppearsInRecent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var subFolder = await CreateFolderInMy("recent_subfolder", Owner);
        var file = await CreateFile("recent_in_subfolder.docx", subFolder.Id);

        // Act
        await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - a file nested in a sub-folder of "My documents" is still part of the "My documents" recent
        var recent = await GetRecentAsync(Owner, [FolderType.USER]);
        recent.Files.Should().ContainSingle(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddToRecent_FileFromRoom_AppearsInRecent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("recent_custom_room");
        var file = await CreateFile("recent_in_room.docx", room.Id);

        // Act
        await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var recent = await GetRecentAsync(Owner);
        recent.Files.Should().ContainSingle(f => f.Title == file.Title);
    }

    [Fact]
    public async Task GetRecent_WithoutFolderTypeFilter_ReturnsFilesFromMyDocumentsAndRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("recent_my.docx", Owner);

        var customRoom = await CreateCustomRoom("recent_custom_room");
        var customRoomFile = await CreateFile("recent_custom.docx", customRoom.Id);

        var publicRoom = await CreatePublicRoom("recent_public_room");
        var publicRoomFile = await CreateFile("recent_public.docx", publicRoom.Id);

        var vdrRoom = await CreateVDRRoom("recent_vdr_room");
        var vdrRoomFile = await CreateFile("recent_vdr.docx", vdrRoom.Id);

        await AddToRecent(myFile.Id, customRoomFile.Id, publicRoomFile.Id, vdrRoomFile.Id);

        // Act
        var recent = await GetRecentAsync(Owner);

        // Assert - without the filter every recent file is returned regardless of its parent folder type
        recent.Files.Should().Contain(f => f.Title == myFile.Title);
        recent.Files.Should().Contain(f => f.Title == customRoomFile.Title);
        recent.Files.Should().Contain(f => f.Title == publicRoomFile.Title);
        recent.Files.Should().Contain(f => f.Title == vdrRoomFile.Title);
    }

    [Fact]
    public async Task GetRecent_FilterByMyDocuments_ReturnsOnlyMyDocumentsFiles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("recent_my.docx", Owner);

        var customRoom = await CreateCustomRoom("recent_custom_room");
        var customRoomFile = await CreateFile("recent_custom.docx", customRoom.Id);

        await AddToRecent(myFile.Id, customRoomFile.Id);

        // Act
        var recent = await GetRecentAsync(Owner, [FolderType.USER]);

        // Assert
        recent.Files.Should().Contain(f => f.Title == myFile.Title);
        recent.Files.Should().NotContain(f => f.Title == customRoomFile.Title);
    }

    [Fact]
    public async Task GetRecent_FilterByRooms_ReturnsOnlyRoomFiles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("recent_my.docx", Owner);

        var customRoom = await CreateCustomRoom("recent_custom_room");
        var customRoomFile = await CreateFile("recent_custom.docx", customRoom.Id);

        var publicRoom = await CreatePublicRoom("recent_public_room");
        var publicRoomFile = await CreateFile("recent_public.docx", publicRoom.Id);

        await AddToRecent(myFile.Id, customRoomFile.Id, publicRoomFile.Id);

        // Act - VirtualRooms is the common ancestor of every room, so it selects all room files
        var recent = await GetRecentAsync(Owner, [FolderType.VirtualRooms]);

        // Assert
        recent.Files.Should().Contain(f => f.Title == customRoomFile.Title);
        recent.Files.Should().Contain(f => f.Title == publicRoomFile.Title);
        recent.Files.Should().NotContain(f => f.Title == myFile.Title);
    }

    [Theory]
    [MemberData(nameof(RoomTypeFilterCases))]
    public async Task GetRecent_FilterBySpecificRoomType_ReturnsOnlyThatRoomFiles(RoomType roomType, FolderType folderTypeFilter)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("recent_my.docx", Owner);

        var targetRoom = await CreateRoom(roomType, "recent_target_room");
        var targetRoomFile = await CreateFile("recent_target.docx", targetRoom.Id);

        var otherRoom = await CreateCustomRoom("recent_other_room");
        var otherRoomFile = await CreateFile("recent_other.docx", otherRoom.Id);

        await AddToRecent(myFile.Id, targetRoomFile.Id, otherRoomFile.Id);

        // Act
        var recent = await GetRecentAsync(Owner, [folderTypeFilter]);

        // Assert
        recent.Files.Should().Contain(f => f.Title == targetRoomFile.Title);
        recent.Files.Should().NotContain(f => f.Title == myFile.Title);

        if (roomType != RoomType.CustomRoom)
        {
            recent.Files.Should().NotContain(f => f.Title == otherRoomFile.Title);
        }
    }

    [Fact]
    public async Task GetRecent_FilterByMultipleFolderTypes_ReturnsFilesFromAllRequestedTypes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("recent_my.docx", Owner);

        var customRoom = await CreateCustomRoom("recent_custom_room");
        var customRoomFile = await CreateFile("recent_custom.docx", customRoom.Id);

        var publicRoom = await CreatePublicRoom("recent_public_room");
        var publicRoomFile = await CreateFile("recent_public.docx", publicRoom.Id);

        await AddToRecent(myFile.Id, customRoomFile.Id, publicRoomFile.Id);

        // Act
        var recent = await GetRecentAsync(Owner, [FolderType.USER, FolderType.CustomRoom]);

        // Assert
        recent.Files.Should().Contain(f => f.Title == myFile.Title);
        recent.Files.Should().Contain(f => f.Title == customRoomFile.Title);
        recent.Files.Should().NotContain(f => f.Title == publicRoomFile.Title);
    }

    public static TheoryData<RoomType, FolderType> RoomTypeFilterCases =>
        new()
        {
            { RoomType.CustomRoom, FolderType.CustomRoom },
            { RoomType.PublicRoom, FolderType.PublicRoom },
            { RoomType.EditingRoom, FolderType.EditingRoom },
            { RoomType.VirtualDataRoom, FolderType.VirtualDataRoom }
        };

    private async Task<FolderContentDtoInteger> GetRecentAsync(User user, List<FolderType>? folderType = null)
    {
        var recentId = await GetFolderIdAsync(FolderType.Recent, user);

        return (await _foldersApi.GetFolderByFolderIdAsync(recentId, folderType: folderType, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    private async Task AddToRecent(params int[] fileIds)
    {
        foreach (var fileId in fileIds)
        {
            await _filesApi.AddFileToRecentAsync(fileId, cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    private async Task<FolderDtoInteger> CreateRoom(RoomType roomType, string title) => roomType switch
    {
        RoomType.CustomRoom => await CreateCustomRoom(title),
        RoomType.PublicRoom => await CreatePublicRoom(title),
        RoomType.EditingRoom => await CreateCollaborationRoom(title),
        RoomType.VirtualDataRoom => await CreateVDRRoom(title),
        RoomType.FillingFormsRoom => await CreateFillingFormsRoom(title),
        _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "Unsupported room type")
    };
}
