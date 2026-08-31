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

namespace ASC.Files.Tests.Tests._03_Rooms.Templates;

/// <summary>
/// What POST /files/rooms/fromTemplate actually copies into the new room: folders, files, nested
/// hierarchies, and the isolation of the copy from both the template and the source room.
/// </summary>
/// <remarks>
/// <see cref="DocSpace.API.SDK.Model.FolderContentDtoInteger.Folders"/> and <c>.Files</c> are typed
/// <c>List&lt;FileEntryBaseDto&gt;</c>, which carries <c>Title</c> but neither <c>Id</c> nor
/// <c>Logo</c>. Tests that need the id of a copied entry read the raw JSON instead (see
/// <see cref="GetFolderContentRaw"/>) — this is an SDK model gap, not a preference.
/// </remarks>
[Trait("Category", "Rooms")]
public class RoomFromTemplateContentTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task CreateRoomFromTemplate_EmptySourceRoom_CreatesEmptyRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest Empty", isPublic: false);

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Empty Room"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var content = (await _foldersApi.GetFolderByFolderIdAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Folders.Should().BeEmpty();
        content.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateRoomFromTemplate_FolderFromSource_IsCopied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest Folder Source");
        const string folderTitle = "Source Folder";
        await CreateFolder(folderTitle, sourceRoom.Id);

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Folder Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Folder Room"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var content = (await _foldersApi.GetFolderByFolderIdAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Folders.ConvertAll(f => f.Title).Should().Contain(folderTitle);
    }

    /// <remarks>
    /// Bug 81666: a folder nested inside another folder in the source room is not copied along with
    /// its parent — only the top-level folder makes it into the new room.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81666")]
    public async Task CreateRoomFromTemplate_NestedFolderHierarchy_IsPreserved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest Nested Source");

        var parent = await CreateFolder("Parent", sourceRoom.Id);
        await CreateFolder("Child", parent.Id);

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Nested Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Nested Room"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var rootContent = await GetFolderContentRaw(roomId);
        var copiedParent = rootContent.Folders.Should().ContainSingle(f => f.Title == "Parent").Which;

        var parentContent = await GetFolderContentRaw(copiedParent.Id);
        parentContent.Folders.ConvertAll(f => f.Title).Should().Contain("Child");
    }

    [Fact]
    public async Task CreateRoomFromTemplate_FileFromSource_IsCopied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest File Source");
        const string fileTitle = "Source File";
        await CreateFile(fileTitle, sourceRoom.Id);

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest File Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "File Room"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        var content = (await _foldersApi.GetFolderByFolderIdAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Files.ConvertAll(f => f.Title).Should().Contain(t => t.StartsWith(fileTitle));
    }

    [Fact]
    public async Task CreateRoomFromTemplate_CopiedItems_HaveDifferentIdsFromSource()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest Ids Source");
        var sourceFolder = await CreateFolder("Folder For Ids", sourceRoom.Id);

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Ids Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        // Act
        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Ids Room"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        // Assert
        roomId.Should().NotBe(sourceRoom.Id);

        var content = await GetFolderContentRaw(roomId);
        var copiedFolder = content.Folders.Should().ContainSingle(f => f.Title == "Folder For Ids").Which;
        copiedFolder.Id.Should().NotBe(sourceFolder.Id);
    }

    [Fact]
    public async Task CreateRoomFromTemplate_DeletingCopiedContent_DoesNotAffectSource()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateCustomRoom("Autotest Isolate Source");
        await CreateFolder("Isolated Folder", sourceRoom.Id);

        await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(sourceRoom.Id, "Autotest Isolate Template"), TestContext.Current.CancellationToken);
        var templateId = await WaitForRoomTemplate();

        await _roomsApi.CreateRoomFromTemplateAsync(new CreateRoomFromTemplateDto(templateId, "Isolate Room"), TestContext.Current.CancellationToken);
        var roomId = await WaitForRoomFromTemplate();

        var copyContent = await GetFolderContentRaw(roomId);
        var copiedFolder = copyContent.Folders.Should().ContainSingle(f => f.Title == "Isolated Folder").Which;

        // Act
        await _foldersApi.DeleteFolderAsync(copiedFolder.Id, new DeleteFolder(deleteAfter: false, immediately: true), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        var sourceContent = (await _foldersApi.GetFolderByFolderIdAsync(sourceRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        sourceContent.Folders.ConvertAll(f => f.Title).Should().Contain("Isolated Folder");
    }

    /// <summary>An entry (folder or file) as it comes back from a raw GET /files/{folderId} read.</summary>
    private sealed record RawEntry(int Id, string Title);

    /// <summary>The parsed body of a raw GET /files/{folderId} read.</summary>
    private sealed record RawFolderContent(List<RawEntry> Folders, List<RawEntry> Files);

    /// <summary>
    /// Reads a folder's content straight from the JSON response, for the id of an entry that
    /// <see cref="DocSpace.API.SDK.Model.FileEntryBaseDto"/> does not carry.
    /// </summary>
    private async Task<RawFolderContent> GetFolderContentRaw(int folderId)
    {
        using var response = await _filesClient.GetAsync($"api/2.0/files/{folderId}", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Unable to read folder {folderId} content ({(int)response.StatusCode}): {body}");
        }

        using var json = JsonDocument.Parse(body);
        var rootElement = json.RootElement.GetProperty("response");

        return new RawFolderContent(ReadEntries(rootElement, "folders"), ReadEntries(rootElement, "files"));
    }

    private static List<RawEntry> ReadEntries(JsonElement rootElement, string propertyName)
    {
        return rootElement.GetProperty(propertyName).EnumerateArray()
            .Select(entry => new RawEntry(entry.GetProperty("id").GetInt32(), entry.GetProperty("title").GetString()!))
            .ToList();
    }
}
