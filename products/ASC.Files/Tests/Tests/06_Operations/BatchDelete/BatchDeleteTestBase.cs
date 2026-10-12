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

namespace ASC.Files.Tests.Tests._06_Operations.BatchDelete;

/// <summary>
/// Shared setup for the <c>fileops/delete</c> suite: waiting for the batch delete operation to
/// finish and reading back folder/trash contents by title.
/// </summary>
public abstract class BatchDeleteTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Waits for the batch delete operation to finish, following the same "empty array on a fast
    /// response" allowance as every other batch endpoint in this suite: <c>DeleteBatchItemsAsync</c>
    /// returns whatever <c>GetOperationResults()</c> holds at that instant, so an operation that
    /// already finished before the response was built can come back pruned.
    /// </summary>
    protected async Task<FileOperationDto> WaitForBatchDelete(List<FileOperationDto> results)
    {
        if (results.Count == 0 || results.Exists(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id) ?? [];
        }

        var operation = results.FirstOrDefault(r => r.Operation == FileOperationType.Delete) ?? results.FirstOrDefault();

        operation.Should().NotBeNull("a batch delete operation must be reported within the poll deadline");

        return operation!;
    }

    protected async Task<bool> FolderContainsFileTitleAsync(int folderId, string title)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Files.Exists(f => f.Title == title);
    }

    protected async Task<bool> FolderContainsFolderTitleAsync(int folderId, string title)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Folders.Exists(f => f.Title == title);
    }

    protected async Task<bool> TrashContainsFileTitleAsync(string title)
    {
        var trash = (await _foldersApi.GetTrashFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return trash.Files.Exists(f => f.Title == title);
    }

    protected async Task<bool> TrashContainsFolderTitleAsync(string title)
    {
        var trash = (await _foldersApi.GetTrashFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return trash.Folders.Exists(f => f.Title == title);
    }
}
