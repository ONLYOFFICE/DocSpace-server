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

namespace ASC.Files.Tests.Tests._06_Operations.EmptyTrash;

/// <summary>
/// Shared setup for <c>PUT /api/2.0/files/fileops/emptytrash</c>: moving a file/folder to the Trash
/// and emptying it, for the currently authenticated user.
/// </summary>
public abstract class EmptyTrashTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Moves a file to the Trash and waits for the move to finish.
    /// </summary>
    protected async Task DeleteFileToTrashAsync(int fileId)
    {
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FileIds = [new(fileId)], Immediately = false },
            TestContext.Current.CancellationToken)).Response;

        if (results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));
    }

    /// <summary>
    /// Moves a folder to the Trash and waits for the move to finish.
    /// </summary>
    protected async Task DeleteFolderToTrashAsync(int folderId)
    {
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto { FolderIds = [new(folderId)], Immediately = false },
            TestContext.Current.CancellationToken)).Response;

        if (results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));
    }

    /// <summary>
    /// Uploads the embedded ONLYOFFICE PDF form into the given folder through an upload session -
    /// the only kind of file a Filling Forms room accepts.
    /// </summary>
    protected async Task<int> UploadPdfFormAsync(int folderId, string title)
    {
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        await using var stream = typeof(EmptyTrashTestBase).Assembly.GetManifestResourceStream("ASC.Files.Tests.Data.new.pdf")!;

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(title, stream.Length),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var chunkSize = (int)settings.ChunkUploadSize;
        var buffer = new byte[chunkSize];
        var chunkNumber = 1;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, chunkSize), TestContext.Current.CancellationToken)) > 0)
        {
            await using var chunkStream = new MemoryStream(buffer, 0, bytesRead);

            await _filesOperationsApi.UploadAsyncSessionAsync(folderId, session.Id, chunkNumber, new FileParameter(chunkStream), TestContext.Current.CancellationToken);

            chunkNumber++;
        }

        var result = (await _filesOperationsApi.FinalizeSessionAsync(folderId, session.Id, TestContext.Current.CancellationToken)).Response;
        result.Uploaded.Should().BeTrue();

        return result.File.Id;
    }

    /// <summary>
    /// Empties the Trash of the currently authenticated user and waits for the operation to finish.
    /// The started operation(s) are returned as last observed, per the same "may already be pruned"
    /// caveat as every other batch operation endpoint.
    /// </summary>
    protected async Task<List<FileOperationDto>> EmptyTrashAndWaitAsync(bool? single = null)
    {
        var results = (await _filesOperationsApi.EmptyTrashAsync(single, TestContext.Current.CancellationToken)).Response;

        if (results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));

        return results;
    }

    /// <summary>
    /// Empties the Trash restricted to items originally located in the given section(s). The generated
    /// SDK has no <c>folderType</c> parameter on <c>emptytrash</c>, so the request is issued directly.
    /// </summary>
    protected async Task EmptyTrashForFolderTypesAndWaitAsync(bool single, params FolderType[] folderTypes)
    {
        var url = $"api/2.0/files/fileops/emptytrash?single={(single ? "true" : "false")}";
        url = folderTypes.Aggregate(url, (current, type) => current + $"&folderType={(int)type}");

        using var response = await _filesClient.PutAsync(url, null, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeTrue(body);

        using var json = JsonDocument.Parse(body);
        var operations = json.RootElement.GetProperty("response");

        if (operations.GetArrayLength() == 0)
        {
            await WaitLongOperation();
            return;
        }

        await WaitLongOperation(operations[0].GetProperty("id").GetString());
    }

    /// <summary>
    /// Reads the Trash of the currently authenticated user.
    /// </summary>
    protected async Task<FolderContentDtoInteger> GetTrashAsync()
    {
        return (await _foldersApi.GetTrashFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
    }
}
