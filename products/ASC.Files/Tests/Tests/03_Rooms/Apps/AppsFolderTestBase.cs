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

namespace ASC.Files.Tests.Tests._03_Rooms.Apps;

/// <summary>
/// Shared helpers for the <c>.ai</c> folder suites. A folder titled exactly <c>.ai</c> that sits
/// in the root of a Custom, Public, Virtual Data or Editing room gets <c>FolderType.Ai</c>, and a
/// room holds at most one of them. The type is not exposed in <c>FolderDto</c>, so the suites observe
/// it through that uniqueness rule: creating a second <c>.ai</c> fails only while a folder of type
/// <c>Apps</c> is already there.
/// </summary>
/// <remarks>
/// Skipping the upload conversion inside <c>.ai</c> is not covered: the integration-test AppHost
/// runs no document server, so no conversion happens anywhere and there is nothing to contrast.
/// </remarks>
public abstract class AppsFolderTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    protected const string AppsTitle = ".ai";

    public static TheoryData<RoomType> SmartRoomTypes =>
    [
        RoomType.CustomRoom, RoomType.PublicRoom, RoomType.VirtualDataRoom, RoomType.EditingRoom
    ];

    protected async Task<FolderDtoInteger> CreateSmartRoom(string title, RoomType roomType = RoomType.CustomRoom)
    {
        return await CreateRoom(new CreateRoomRequestDto(title, roomType: roomType));
    }

    protected Task<FolderDtoInteger> CreateAppsFolder(int parentId)
    {
        return CreateFolder(AppsTitle, parentId);
    }

    /// <summary>Asserts that the folder already holds an <c>Apps</c> folder: a new <c>.ai</c> is rejected.</summary>
    protected async Task AssertAppsFolderPresent(int folderId)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderAsync(folderId, new CreateFolder(AppsTitle), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("already contains the .ai folder");
    }

    /// <summary>Asserts that the folder holds no <c>Apps</c> folder: a new <c>.ai</c> is created.</summary>
    protected async Task AssertAppsFolderAbsent(int folderId)
    {
        var created = await CreateAppsFolder(folderId);

        created.Title.Should().Be(AppsTitle);
    }

    protected async Task MoveAndWait(int folderId, int destFolderId)
    {
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(
            BuildBatchRequest(folderId, destFolderId, FileConflictResolveType.Skip),
            TestContext.Current.CancellationToken)).Response;

        await AssertOperationSucceeded(results.FirstOrDefault()?.Id);
    }

    protected async Task CopyAndWait(int folderId, int destFolderId)
    {
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(
            BuildBatchRequest(folderId, destFolderId, FileConflictResolveType.Duplicate),
            TestContext.Current.CancellationToken)).Response;

        await AssertOperationSucceeded(results.FirstOrDefault()?.Id);
    }

    /// <summary>
    /// <see cref="DocSpace.API.SDK.Api.Files.OperationsApi.DuplicateBatchItemsAsync"/> returns
    /// <c>FileOperationArrayWrapper</c> directly, not wrapped in <c>ApiResponse&lt;T&gt;</c>.
    /// </summary>
    protected async Task DuplicateAndWait(int folderId)
    {
        var result = await _filesOperationsApi.DuplicateBatchItemsAsync(
            new DuplicateRequestDto(folderIds: [new DuplicateRequestDtoAllOfFolderIds(folderId)], fileIds: []) { ReturnSingleOperation = true },
            TestContext.Current.CancellationToken);

        await AssertOperationSucceeded(result.Response.FirstOrDefault()?.Id);
    }

    protected async Task DeleteToTrashAndWait(int folderId)
    {
        var results = (await _foldersApi.DeleteFolderAsync(
            folderId,
            new DeleteFolder { DeleteAfter = true, Immediately = false },
            TestContext.Current.CancellationToken)).Response;

        await AssertOperationSucceeded(results.FirstOrDefault()?.Id);
    }

    protected async Task<List<string>> GetFolderTitles(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return (content.Folders ?? []).Select(f => f.Title).ToList();
    }

    /// <summary>
    /// The room listing is typed <c>List&lt;FileEntryBaseDto&gt;</c>, which carries a <c>Title</c> but
    /// no <c>Id</c> (the SDK narrowness tests.md calls out), so a room created by an operation has to be
    /// found through the raw JSON.
    /// </summary>
    protected async Task<int> FindRoomIdByTitlePrefix(string titlePrefix, int exceptRoomId)
    {
        var raw = await _roomsApi.GetRoomsFolderWithHttpInfoAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(raw.RawContent);

        foreach (var entry in json.RootElement.GetProperty("response").GetProperty("folders").EnumerateArray())
        {
            var id = entry.GetProperty("id").GetInt32();

            if (id != exceptRoomId && entry.GetProperty("title").GetString()!.StartsWith(titlePrefix, StringComparison.Ordinal))
            {
                return id;
            }
        }

        throw new InvalidOperationException($"No room titled '{titlePrefix}…' other than {exceptRoomId} was found.");
    }

    private static BatchRequestDto BuildBatchRequest(int folderId, int destFolderId, FileConflictResolveType conflictResolveType)
    {
        return new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(destFolderId),
            ConflictResolveType = conflictResolveType,
            FileIds = [],
            FolderIds = [new BatchRequestDtoAllOfFolderIds(folderId)],
            ReturnSingleOperation = true
        };
    }

    private async Task AssertOperationSucceeded(string? operationId)
    {
        var statuses = await WaitLongOperation(operationId);

        statuses.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));
    }
}
