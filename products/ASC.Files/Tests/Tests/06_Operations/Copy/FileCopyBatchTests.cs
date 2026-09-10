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

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

/// <summary>
/// Basic <c>PUT /api/2.0/files/fileops/copy</c> scenarios for files: locations (MyDocs, rooms,
/// subfolders), destination room types and request-shape edge cases. Conflict resolution and
/// deeply nested destinations have their own classes.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileCopyBatchTests(
    AspireAppFixture fixture)
    : CopyTestBase(fixture)
{
    [Fact]
    public async Task CopyFile_FromMyDocsToCustomRoom_AppearsInDestination_SourcePreserved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch File to Room.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Dest CustomRoom");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
        FileTitles(await GetFolderContent(myDocsId)).Should().Contain(sourceFile.Title, "copy must not remove the source");
    }

    [Fact]
    public async Task CopyMultipleFiles_AllAppearInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var file1 = await CreateFile("Autotest CopyBatch Multi File1.docx", myDocsId);
        var file2 = await CreateFile("Autotest CopyBatch Multi File2.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Multi Dest");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [file1.Id, file2.Id]));

        // Assert
        var destTitles = FileTitles(await GetFolderContent(destRoom.Id));
        destTitles.Should().Contain(file1.Title);
        destTitles.Should().Contain(file2.Title);
    }

    [Fact]
    public async Task CopyFile_InterRoom_AppearsInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateCustomRoom("Autotest CopyBatch Inter-Room Src");
        var sourceFile = await CreateFile("Autotest CopyBatch Inter-Room File.docx", srcRoom.Id);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Inter-Room Dest");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_ToArchivedRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Archived Dest.docx", myDocsId);
        var room = await CreateCustomRoom("Autotest CopyBatch Archived Room");

        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(room.Id, fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_ToNonExistentDestFolder_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch BadDest.docx", myDocsId);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(999999999, fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CopyFile_FromRoomToMyDocs_AppearsInMyDocs_SourcePreserved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateCustomRoom("Autotest CopyBatch RoomToMyDocs Src");
        var sourceFile = await CreateFile("Autotest CopyBatch RoomToMyDocs File.docx", srcRoom.Id);
        var myDocsId = await GetUserFolderIdAsync(Owner);

        // Act
        await CopyAndWait(BuildCopyRequest(myDocsId, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(myDocsId)).Should().Contain(sourceFile.Title);
        FileTitles(await GetFolderContent(srcRoom.Id)).Should().Contain(sourceFile.Title, "copy must not remove the source");
    }

    [Fact]
    public async Task CopyFile_FromRoomSubfolderToAnotherRoom_AppearsInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateCustomRoom("Autotest CopyBatch SubSrc Room");
        var srcFolder = await CreateFolder("Autotest CopyBatch SubSrc Folder", srcRoom.Id);
        var sourceFile = await CreateFile("Autotest CopyBatch SubSrc File.docx", srcFolder.Id);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch SubDest Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_WithDeleteAfterTrue_AppearsInDestination_SourcePreserved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch DeleteAfter File.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch DeleteAfter Room");

        // Act: deleteAfter=true removes the operation record once it finishes, so the destination
        // has to be polled directly rather than waited on through the operation status.
        await _filesOperationsApi.CopyBatchItemsAsync(
            BuildCopyRequest(destRoom.Id, deleteAfter: true, fileIds: [sourceFile.Id]),
            TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(30);
        List<string> destTitles;

        while (true)
        {
            destTitles = FileTitles(await GetFolderContent(destRoom.Id));

            if (destTitles.Contains(sourceFile.Title) || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        // Assert
        destTitles.Should().Contain(sourceFile.Title);
        FileTitles(await GetFolderContent(myDocsId)).Should().Contain(sourceFile.Title, "copy must not remove the source");
    }

    [Fact]
    public async Task CopyFile_DuplicateFileIdInRequest_CreatesOneCopy()
    {
        // Arrange: the same fileId twice — the first copy succeeds, the second is skipped as a
        // name conflict against the entry the first one just created.
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch DupId File.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch DupId Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id, sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().ContainSingle(t => t == sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_ToPublicRoom_AppearsInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch PublicRoom File.docx", myDocsId);
        var destRoom = await CreatePublicRoom("Autotest CopyBatch PublicRoom");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_ToEditingRoom_AppearsInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch EditingRoom File.docx", myDocsId);
        var destRoom = await CreateCollaborationRoom("Autotest CopyBatch EditingRoom");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_ToVirtualDataRoom_AppearsInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch VirtualDataRoom File.docx", myDocsId);
        var destRoom = await CreateVirtualRoom("Autotest CopyBatch VirtualDataRoom");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyBatch_EmptyFileAndFolderIds_ReturnsOk()
    {
        // Arrange: an empty batch is accepted gracefully, consistent with addFavorites/checkMove.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CopyBatch Empty Src Room");

        // Act
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(
            BuildCopyRequest(room.Id),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        results.Should().NotBeNull();
    }
}
