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


namespace ASC.Files.Tests.Tests._06_Operations.MyDocuments;

/// <summary>
/// A User manages files and folders in their own My documents: rename, copy, move, download and delete all
/// succeed. The functional suites of these operations run as the Owner; this one runs them as the lowest
/// type that has a My documents section.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class UserOwnDocumentsOperationsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task User_OwnMyDocuments_RenamesCopiesMovesDownloadsAndDeletes()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var myId = await GetUserFolderIdAsync(user);

        var copyTarget = await CreateFolder("Autotest Copy Target", myId);
        var moveTarget = await CreateFolder("Autotest Move Target", myId);
        var file = await CreateFile("Autotest User File.docx", myId);

        // Rename a file and a folder
        var renamedFile = (await _filesApi.UpdateFileAsync(file.Id, new UpdateFileRequest("Autotest Renamed.docx"), TestContext.Current.CancellationToken)).Response;
        renamedFile.Title.Should().Be("Autotest Renamed.docx");

        var renamedFolder = (await _foldersApi.RenameFolderAsync(moveTarget.Id, new CreateFolderRequest("Autotest Moved Here"), TestContext.Current.CancellationToken)).Response;
        renamedFolder.Title.Should().Be("Autotest Moved Here");

        // Copy the file into the first folder
        await _filesOperationsApi.CopyBatchItemsAsync(Batch(copyTarget.Id, file.Id), TestContext.Current.CancellationToken);
        (await WaitLongOperation()).Should().OnlyContain(o => o.Finished && o.Error == "");
        (await _foldersApi.GetFolderInfoAsync(copyTarget.Id, TestContext.Current.CancellationToken)).Response.FilesCount.Should().Be(1);

        // Move the file into the second folder
        await _filesOperationsApi.MoveBatchItemsAsync(Batch(moveTarget.Id, file.Id), TestContext.Current.CancellationToken);
        (await WaitLongOperation()).Should().OnlyContain(o => o.Finished && o.Error == "");
        (await _foldersApi.GetFolderInfoAsync(moveTarget.Id, TestContext.Current.CancellationToken)).Response.FilesCount.Should().Be(1);
        (await GetFile(file.Id)).FolderId.Should().Be(moveTarget.Id);

        // Download the first folder
        var download = (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(folderIds: [new(copyTarget.Id)], fileIds: [], fileConvertIds: []) { ReturnSingleOperation = true },
            TestContext.Current.CancellationToken)).Response;

        if (download.Count == 0 || download.Exists(o => !o.Finished))
        {
            download = await WaitLongOperation(download.FirstOrDefault()?.Id) ?? [];
        }

        download.Should().ContainSingle(o => o.Operation == FileOperationType.Download)
            .Which.Should().Match<FileOperationDto>(o => o.Finished && o.Error == "" && !string.IsNullOrEmpty(o.Url));

        // Delete the second folder with the moved file
        await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto(folderIds: [new(moveTarget.Id)], fileIds: []) { ReturnSingleOperation = true },
            TestContext.Current.CancellationToken);
        (await WaitLongOperation()).Should().OnlyContain(o => o.Finished && o.Error == "");

        (await _foldersApi.GetFolderInfoAsync(moveTarget.Id, TestContext.Current.CancellationToken)).Response.RootFolderType.Should().Be(FolderType.TRASH);
    }

    private static BatchRequestDto Batch(int destFolderId, int fileId)
    {
        return new BatchRequestDto
        {
            ReturnSingleOperation = true,
            FileIds = [new BatchRequestDtoAllOfFileIds(fileId)],
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(destFolderId),
            DeleteAfter = false
        };
    }
}
