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
/// The move endpoint's own request options (<c>deleteAfter</c>, <c>returnSingleOperation</c>) and
/// its input validation, independent of any particular destination room type.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileMoveOperationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task MoveFile_DeleteAfterTrue_RemovesOperationFromQueue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var fileTitle = "Autotest MoveBatch DeleteAfter.docx";
        var file = await CreateFile(fileTitle, myDocsFolderId);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch DeleteAfter Dest");

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            DeleteAfter = true
        }, TestContext.Current.CancellationToken);

        // Assert - deleteAfter=true clears the operation out of the queue once it finishes
        var deadline = DateTime.UtcNow.AddSeconds(30);
        List<FileOperationDto> statuses;

        while (true)
        {
            statuses = (await _filesOperationsApi.GetOperationStatusesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (statuses.Count == 0 || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        statuses.Should().BeEmpty();

        var movedFile = await GetFile(file.Id);
        movedFile.FolderId.Should().Be(destRoom.Id);
    }

    [Fact]
    public async Task MoveFile_ReturnSingleOperationTrue_ReturnsOperationData()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest MoveBatch SingleOp.docx", myDocsFolderId);
        var destRoom = await CreateCustomRoom("Autotest MoveBatch SingleOp Dest");

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        results.Should().ContainSingle();
        results[0].Operation.Should().Be(FileOperationType.Move);

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results[0].Id);
        }

        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
    }

    [Fact]
    [Trait("Bug", "82243")]
    public async Task MoveFile_NonExistentFileId_ReturnsNotFound()
    {
        // An id that resolves to nothing is a 404, not the 403 the product currently returns.
        await _filesClient.Authenticate(Owner);

        var destRoom = await CreateCustomRoom("Autotest MoveBatch BadFile Dest");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                DestFolderId = new(destRoom.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(999999999)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}
