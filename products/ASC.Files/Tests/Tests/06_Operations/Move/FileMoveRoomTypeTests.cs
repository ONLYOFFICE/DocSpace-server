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
/// How PUT /api/2.0/files/fileops/move behaves against the destination room type: which room
/// types accept a plain file, which reject it, and the FillingFormsRoom form-only restriction.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileMoveRoomTypeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(RoomType.EditingRoom)]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task MoveFile_ToRoomType_ReturnsSuccess(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var fileTitle = $"Autotest MoveBatch {roomType} File.docx";
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile(fileTitle, myDocsFolderId);

        var destRoom = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest MoveBatch {roomType} Dest Room", roomType: roomType),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));

        var movedFile = await GetFile(file.Id);
        movedFile.FolderId.Should().Be(destRoom.Id);
    }

    [Fact]
    public async Task MoveFile_ToArchivedRoom_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest MoveBatch Archived Dest.docx", myDocsFolderId);
        var room = await CreateCustomRoom("Autotest MoveBatch Archived Room");

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(room.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_DocxToFillingFormsRoom_ReturnsForbidden()
    {
        // Arrange - FillingFormsRoom only accepts form files; a plain .docx must be rejected
        await _filesClient.Authenticate(Owner);

        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest MoveBatch FillingFormsRoom.docx", myDocsFolderId);
        var destRoom = await CreateFillingFormsRoom("Autotest MoveBatch FillingFormsRoom Docx");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(destRoom.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFile_FormToFillingFormsRoom_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var formFileName = "Autotest MoveBatch FillingFormsRoom Form.pdf";
        var formFileId = await UploadOoFormAsync(myDocsFolderId, formFileName);
        var destRoom = await CreateFillingFormsRoom("Autotest MoveBatch FillingFormsRoom Form");

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(formFileId)],
            FolderIds = [],
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));

        var destContent = (await _foldersApi.GetFolderByFolderIdAsync(destRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        destContent.Files.Should().NotBeEmpty();

        var srcContent = (await _foldersApi.GetFolderByFolderIdAsync(myDocsFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        srcContent.Files.Should().NotContain(f => f.Title == formFileName);
    }

    /// <summary>
    /// Uploads a small in-memory PDF that carries the ONLYOFFICE form signature, so the product's
    /// form check recognises it as a real form (rather than uploading a genuine PDF file, which is
    /// not needed here). Mirrors <c>FormsTestBase.UploadOoFormAsync</c>, duplicated locally: that
    /// base class lives in the unrelated 01_Files/Forms feature folder, so sharing it would need a
    /// helper one level above both, which is outside this task's scope.
    /// </summary>
    private async Task<int> UploadOoFormAsync(int folderId, string fileName)
    {
        using var content = new MemoryStream();

        await using (var stream = typeof(FileMoveRoomTypeTests).Assembly.GetManifestResourceStream("ASC.Files.Tests.Data.new.pdf")!)
        {
            await stream.CopyToAsync(content, TestContext.Current.CancellationToken);
        }

        var bytes = content.ToArray();

        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(fileName, bytes.Length),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var chunkSize = (int)settings.ChunkUploadSize;
        var chunkNumber = 1;

        for (var offset = 0; offset < bytes.Length; offset += chunkSize)
        {
            var length = Math.Min(chunkSize, bytes.Length - offset);
            await using var chunkStream = new MemoryStream(bytes, offset, length);

            await _filesOperationsApi.UploadAsyncSessionAsync(
                folderId,
                session.Id,
                chunkNumber,
                new FileParameter(chunkStream),
                TestContext.Current.CancellationToken);

            chunkNumber++;
        }

        var uploaded = (await _filesOperationsApi.FinalizeSessionAsync(
            folderId,
            session.Id,
            TestContext.Current.CancellationToken)).Response;

        return uploaded.File.Id;
    }
}
