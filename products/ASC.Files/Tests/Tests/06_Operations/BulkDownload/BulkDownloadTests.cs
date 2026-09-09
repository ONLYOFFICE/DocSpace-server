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

namespace ASC.Files.Tests.Tests._06_Operations.BulkDownload;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/bulkdownload</c> — functional coverage: files, folders, mixed
/// selections, files inside a room, <c>returnSingleOperation</c>, an empty selection, a
/// non-existent file id and format conversion via <c>fileConvertIds</c>. Access control lives in
/// <see cref="BulkDownloadPermissionsTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class BulkDownloadTests(
    AspireAppFixture fixture)
    : BulkDownloadTestBase(fixture)
{
    [Fact]
    public async Task BulkDownload_SingleFile_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest BulkDownload Single.docx", Owner);

        // Act
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Processed.Should().Be("1");
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_MultipleFiles_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file1 = await CreateFileInMy("Autotest BulkDownload Multi1.docx", Owner);
        var file2 = await CreateFileInMy("Autotest BulkDownload Multi2.docx", Owner);

        // Act
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file1.Id), new(file2.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_SingleFolder_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var folder = await CreateFolderInMy("Autotest BulkDownload Folder", Owner);
        await CreateFile("Autotest BulkDownload In Folder.docx", folder.Id);

        // Act
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(folderIds: [new(folder.Id)], fileIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_MultipleFolders_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var folder1 = await CreateFolderInMy("Autotest BulkDownload MultiFolder1", Owner);
        var folder2 = await CreateFolderInMy("Autotest BulkDownload MultiFolder2", Owner);
        await CreateFile("Autotest BulkDownload Folder1 File.docx", folder1.Id);
        await CreateFile("Autotest BulkDownload Folder2 File.docx", folder2.Id);

        // Act
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(folderIds: [new(folder1.Id), new(folder2.Id)], fileIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_FilesAndFolders_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest BulkDownload Mixed File.docx", Owner);
        var folder = await CreateFolderInMy("Autotest BulkDownload Mixed Folder", Owner);
        await CreateFile("Autotest BulkDownload In Mixed Folder.docx", folder.Id);

        // Act
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [new(folder.Id)], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_FileFromCustomRoom_FinishesWithUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest BulkDownload Room");
        var file = await CreateFile("Autotest BulkDownload Room File.docx", room.Id);

        // Act
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Processed.Should().Be("1");
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_ReturnSingleOperationTrue_ReturnsSingleDownloadEntry()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest BulkDownload ReturnSingle.docx", Owner);

        // Act
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(file.Id)], folderIds: [], fileConvertIds: []) { ReturnSingleOperation = true },
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        results.Should().ContainSingle();
        results[0].Operation.Should().Be(FileOperationType.Download);

        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Processed.Should().Be("1");
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }

    [Fact]
    public async Task BulkDownload_EmptySelection_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert - an empty selection is a legal (if pointless) request, and must not throw.
        await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(folderIds: [], fileIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BulkDownload_NonExistentFileId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [new(999999999)], folderIds: [], fileConvertIds: []),
            cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task BulkDownload_FileConvertIds_DownloadsWithConversion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest BulkDownload Convert.docx", Owner);

        // Act
        var results = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileConvertIds: [new DownloadRequestItemDto(new DownloadRequestItemDtoKey(file.Id), "pdf")], folderIds: [], fileIds: []),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var operation = await WaitForBulkDownload(results);
        operation.Finished.Should().BeTrue();
        operation.Error.Should().BeEmpty();
        operation.Operation.Should().Be(FileOperationType.Download);
        operation.Processed.Should().Be("1");
        operation.Url.Should().Contain("filehandler.ashx?action=bulk");
    }
}
