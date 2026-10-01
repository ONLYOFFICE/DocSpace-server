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
/// Folders inside a third-party (Nextcloud) room through the string overloads of the SDK:
/// <c>CreateFolderAsync(string)</c>, <c>GetFolderInfoAsync(string)</c>, <c>RenameFolderAsync(string)</c>,
/// <c>GetFolderByFolderIdAsync(string)</c>, <c>GetFoldersAsync(string)</c>, <c>GetFolderPathAsync(string)</c>
/// and <c>DeleteFolderAsync(string)</c>. Skipped unless Nextcloud is configured in the environment.
/// </summary>
/// <remarks>
/// The room root is the shared Nextcloud account, so assertions over it only look for what the
/// test created itself, never at counts. Every test works inside its own uniquely named folder,
/// which the base class removes from the storage afterwards.
/// </remarks>
[Trait("Category", "Rooms")]
[Trait("Requires", "Nextcloud")]
[Collection("Nextcloud")]
public class ThirdPartyFolderTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    [Fact]
    public async Task CreateFolder_InThirdPartyRoom_ReturnsThirdPartyFolder()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, _, roomId) = await CreateNextcloudRoom(UniqueTitle("TP Folder Create Room"), createAsNewFolder: true);
        var title = UniqueTitle("TP Folder Create");

        // Act
        var folder = await CreateThirdPartyFolder(roomId, title);

        // Assert
        folder.Id.Should().NotBeNullOrEmpty();
        folder.Id.Should().NotBe(roomId);
        folder.Title.Should().Be(title);
        folder.ParentId.Should().Be(roomId);
        folder.RootFolderType.Should().Be(FolderType.VirtualRooms);
        folder.ProviderKey.Should().Be(NextcloudProviderKey);
    }

    [Fact]
    public async Task GetFolderInfo_StringId_ReturnsCreatedFolder()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (roomId, work) = await CreateWorkFolder("TP Folder Info");

        // Act
        var folder = (await _foldersApi.GetFolderInfoAsync(work.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        folder.Id.Should().Be(work.Id);
        folder.Title.Should().Be(work.Title);
        folder.ParentId.Should().Be(roomId);
        folder.RootFolderType.Should().Be(FolderType.VirtualRooms);
    }

    [Fact]
    public async Task GetFolderByFolderId_RoomId_ListsCreatedFolder()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (roomId, work) = await CreateWorkFolder("TP Folder Listed");

        // Act
        var (_, folders) = await ListThirdPartyFolder(roomId);

        // Assert
        folders.Should().Contain(work.Title);
    }

    [Fact]
    public async Task RenameFolder_StringId_ChangesTitle()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (roomId, work) = await CreateWorkFolder("TP Folder Rename");
        var newTitle = UniqueTitle("TP Folder Renamed");

        // Act
        var renamed = (await _foldersApi.RenameFolderAsync(
            work.Id, new CreateFolder(newTitle), TestContext.Current.CancellationToken)).Response;

        // Assert
        renamed.Title.Should().Be(newTitle);

        // A WebDAV id is path based, so the renamed folder is read back by the id the rename
        // returned, and it is that id the cleanup has to delete.
        DeleteAfterTest(renamed.Id);
        var reread = (await _foldersApi.GetFolderInfoAsync(renamed.Id, TestContext.Current.CancellationToken)).Response;
        reread.Title.Should().Be(newTitle);

        var (_, folders) = await ListThirdPartyFolder(roomId);
        folders.Should().Contain(newTitle).And.NotContain(work.Title);
    }

    [Fact]
    public async Task CreateFolder_InThirdPartyFolder_IsNestedUnderIt()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP Folder Nested");
        var childTitle = UniqueTitle("TP Folder Child");

        // Act
        var child = await CreateThirdPartyFolder(work.Id, childTitle);

        // Assert
        child.ParentId.Should().Be(work.Id);

        var subfolders = (await _foldersApi.GetFoldersAsync(work.Id, TestContext.Current.CancellationToken)).Response;
        subfolders.Select(f => f.Title).Should().ContainSingle(t => t == childTitle);

        var content = (await _foldersApi.GetFolderByFolderIdAsync(
            work.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Current.Id.Should().Be(work.Id);
        content.Folders.Select(f => f.Title).Should().Equal(childTitle);
        content.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFolderPath_StringId_LeadsFromRoomToFolder()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (roomId, work) = await CreateWorkFolder("TP Folder Path");
        var child = await CreateThirdPartyFolder(work.Id, UniqueTitle("TP Folder Path Child"));
        var room = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;

        // Act
        var path = (await _foldersApi.GetFolderPathAsync(child.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        var titles = path.Select(f => f.Title).ToList();
        titles.Should().EndWith(new[] { room.Title, work.Title, child.Title });
    }

    [Fact]
    public async Task DeleteFolder_StringId_RemovesFolderFromStorage()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);
        var (_, work) = await CreateWorkFolder("TP Folder Delete");
        var child = await CreateThirdPartyFolder(work.Id, UniqueTitle("TP Folder Delete Child"));

        // Act
        var results = (await _foldersApi.DeleteFolderAsync(
            child.Id, new DeleteFolder(false, true), TestContext.Current.CancellationToken)).Response;
        var finished = await WaitLongOperation(results.FirstOrDefault()?.Id);

        // Assert
        finished.Should().NotBeNull();
        finished!.Should().OnlyContain(r => r.Finished && string.IsNullOrEmpty(r.Error));

        var (_, folders) = await ListThirdPartyFolder(work.Id);
        folders.Should().NotContain(child.Title);

        var status = await WaitForFolderGone(child.Id);
        status.Should().Be(404, "a third-party folder has no trash: once deleted it is gone");
    }
}
