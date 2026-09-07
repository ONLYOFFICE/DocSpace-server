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

namespace ASC.Files.Tests.Tests._06_Operations.Move;

/// <summary>
/// Conflict-resolution behaviour of PUT /api/2.0/files/fileops/move, and a couple of edge cases
/// (empty batch, moving a file onto its own parent) that share the same "arrange a file + a
/// destination room" shape.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileMoveConflictTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task MoveFile_ConflictSkip_KeepsExistingFileUnchanged()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var fileTitle = "Autotest MoveBatch Skip Conflict.docx";
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file1 = await CreateFile(fileTitle, myDocsFolderId);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Skip Room");

        // Seed a same-titled file already in the destination.
        await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file1.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var destBefore = await GetFileIdsInFolder(destRoom.Id);
        var existingFileId = destBefore[0];

        var file2 = await CreateFile(fileTitle, myDocsFolderId);

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file2.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert - the existing file in the destination is untouched, no duplicate created
        var destAfter = await GetFileIdsInFolder(destRoom.Id);
        destAfter.Should().ContainSingle();
        destAfter[0].Should().Be(existingFileId);

        // The skipped file stays in the source, since it was never moved.
        var file2AfterMove = await GetFile(file2.Id);
        file2AfterMove.FolderId.Should().Be(myDocsFolderId);
    }

    [Fact]
    public async Task MoveFile_ConflictOverwrite_ReplacesExistingFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var fileTitle = "Autotest MoveBatch Overwrite Conflict.docx";
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Overwrite Room");

        var file1 = await CreateFile(fileTitle, myDocsFolderId);
        await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file1.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var destBefore = await GetFileIdsInFolder(destRoom.Id);
        var originalFileId = destBefore[0];

        var file2 = await CreateFile(fileTitle, myDocsFolderId);

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Overwrite,
            FileIds = [new(file2.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert - same entry (same id), no duplicate created
        var destAfter = await GetFileIdsInFolder(destRoom.Id);
        destAfter.Should().ContainSingle();
        destAfter[0].Should().Be(originalFileId);
    }

    [Fact]
    public async Task MoveFile_ConflictDuplicate_CreatesAdditionalCopy()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var fileTitle = "Autotest MoveBatch Duplicate Conflict.docx";
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Duplicate Room");

        var file1 = await CreateFile(fileTitle, myDocsFolderId);
        await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file1.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var file2 = await CreateFile(fileTitle, myDocsFolderId);

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Duplicate,
            FileIds = [new(file2.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        var destAfter = (await _foldersApi.GetFolderByFolderIdAsync(destRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        destAfter.Files.Should().HaveCount(2);
    }

    [Fact]
    public async Task MoveBatch_EmptyFileAndFolderIds_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch Empty Dest Room");

        // Act & Assert - an empty batch is accepted gracefully, mirroring copyBatchItems
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        results.Should().NotBeNull();
    }

    [Fact]
    public async Task MoveFile_ToSameFolderAsSource_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest MoveBatch SameDest Room");
        var file = await CreateFile("Autotest MoveBatch SameDest File.docx", room.Id);

        // Act & Assert - moving a file onto the folder it already lives in must not error out
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(room.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        results.Should().NotBeNull();
    }

    /// <summary>
    /// Reads the ids of the files directly in a folder from the raw response body.
    /// <c>FolderContentDtoInteger.Files</c> is typed <c>List&lt;FileEntryBaseDto&gt;</c>, which
    /// carries <c>Title</c> but not <c>Id</c> - an SDK model gap - so identity checks (same entry
    /// vs. a new one with the same title) have to go through the raw JSON.
    /// </summary>
    private async Task<List<int>> GetFileIdsInFolder(int folderId)
    {
        var response = await _foldersApi.GetFolderByFolderIdWithHttpInfoAsync(folderId, cancellationToken: TestContext.Current.CancellationToken);

        using var payload = JsonDocument.Parse(response.RawContent);
        var files = payload.RootElement.GetProperty("response").GetProperty("files");

        return [.. files.EnumerateArray().Select(f => f.GetProperty("id").GetInt32())];
    }
}
