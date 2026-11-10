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

namespace ASC.Files.Tests.Tests._03_Rooms.Instructions;

public abstract class InstructionsRoomTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int InstructionsRoomType = 10;

    protected const string MarkdownOnlyError = "Only Markdown files (.md) can be added to an instructions room";
    protected const string FrontmatterMissingError = "The skill file must start with a YAML frontmatter block";
    protected const string NameInvalidError = "The skill name must be 1 to 64 characters long";
    protected const string DescriptionInvalidError = "The skill description must be 1 to 1024 characters long";

    protected static byte[] Skill(string name, string description = "Reviews documents")
    {
        return Encoding.UTF8.GetBytes($"---\nname: {name}\ndescription: {description}\n---\n# {name}\n\nBody of the skill.\n");
    }

    protected static byte[] Text(string content)
    {
        return Encoding.UTF8.GetBytes(content);
    }

    protected async Task<int> CreateInstructionsRoomAsync(string title)
    {
        var files = new RawApiClient(_filesClient);
        using var response = await files.PostAsync("api/2.0/files/rooms", new { title, roomType = InstructionsRoomType }, TestContext.Current.CancellationToken);

        return (await files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken)).GetProperty("id").GetInt32();
    }

    protected async Task<FileDto> UploadAsync(int folderId, byte[] content, string fileName, bool? createNewIfExist = null)
    {
        var file = new FileParameter(fileName, "text/markdown", new MemoryStream(content));

        var response = await _foldersApi.UploadFileAsync(
            folderId,
            createNewIfExist: createNewIfExist,
            file: file,
            cancellationToken: TestContext.Current.CancellationToken);

        return response.Response.Single();
    }

    protected async Task<UploadSessionResponseDto> UploadChunkedAsync(int folderId, byte[] content, string fileName)
    {
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(fileName, content.Length),
            TestContext.Current.CancellationToken)).Response;

        var chunkSize = (int)settings.ChunkUploadSize;
        var chunkNumber = 1;

        for (var offset = 0; offset < content.Length; offset += chunkSize)
        {
            var length = Math.Min(chunkSize, content.Length - offset);
            await using var chunk = new MemoryStream(content, offset, length);

            await _filesOperationsApi.UploadAsyncSessionAsync(folderId, session.Id, chunkNumber, new FileParameter(chunk), TestContext.Current.CancellationToken);

            chunkNumber++;
        }

        return (await _filesOperationsApi.FinalizeSessionAsync(folderId, session.Id, TestContext.Current.CancellationToken)).Response;
    }

    protected async Task MoveAsync(int destFolderId, int fileId)
    {
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(BatchRequest(destFolderId, fileId), TestContext.Current.CancellationToken)).Response;

        await WaitAsync(results);
    }

    protected async Task CopyAsync(int destFolderId, int fileId)
    {
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(BatchRequest(destFolderId, fileId), TestContext.Current.CancellationToken)).Response;

        await WaitAsync(results);
    }

    protected async Task DeleteToTrashAsync(int fileId)
    {
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto
            {
                FolderIds = [],
                FileIds = [new(fileId)],
                Immediately = false,
                ReturnSingleOperation = true
            },
            TestContext.Current.CancellationToken)).Response;

        await WaitAsync(results);
    }

    protected async Task<List<string>> FileTitlesAsync(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return (content.Files ?? []).Select(f => f.Title).ToList();
    }

    private static BatchRequestDto BatchRequest(int destFolderId, int fileId)
    {
        return new BatchRequestDto
        {
            DestFolderId = new(destFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(fileId)],
            FolderIds = [],
            ReturnSingleOperation = true
        };
    }

    private async Task WaitAsync(List<FileOperationDto> results)
    {
        if (!results.TrueForAll(r => r.Finished))
        {
            await WaitLongOperation(results.FirstOrDefault()?.Id);
        }
    }
}
