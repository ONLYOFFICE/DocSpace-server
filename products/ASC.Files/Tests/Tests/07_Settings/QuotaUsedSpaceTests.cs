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

namespace ASC.Files.Tests.Tests._07_Settings;

/// <summary>
/// Verifies that the used space counters stay consistent when content is added and removed. Two
/// counters are checked side by side for every case:
/// <list type="bullet">
/// <item>the per-section statistics of <c>GetFilesUsedSpaceAsync</c>;</item>
/// <item>the total space used by the portal, reported as the "total_size" feature of
/// <c>GetQuotaPaymentInformationAsync</c>. Every test runs on its own portal, so this total is
/// affected by nothing but the content the test itself creates.</item>
/// </list>
///
/// Removing content does not release the space right away - deleting moves the content to the Trash,
/// which transfers its used space to the Trash section while the portal total stays the same, and
/// only the deletion from the Trash releases both. Both steps are covered here.
///
/// The counters are maintained incrementally while the delete operation walks the folder tree, so
/// the tests also cover deletion of deep structures and several delete operations running at the
/// same time - the cases where concurrent counter updates can be lost.
/// </summary>
[Trait("Category", "Settings")]
[Trait("Feature", "Quota")]
public class QuotaUsedSpaceTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const int TreeDepth = 3;
    private const int TreeBreadth = 2;
    private const int FilesPerFolder = 2;

    /// <summary>
    /// The id of the quota feature holding the total space used by the portal. Its title is a
    /// localized caption, so the feature is looked up by id.
    /// </summary>
    private const string TotalSizeFeature = "total_size";

    [Fact]
    public async Task CreateFile_InMyDocuments_IncreasesMyDocumentsUsedSpace()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();

        // Act
        var file = await CreateFileInMy("quota_create.docx", Owner);

        // Assert
        file.PureContentLength.Should().BePositive("a newly created file must occupy some space");

        var fileSize = file.PureContentLength!.Value;

        var after = await WaitForUsedSpaceAsync(s => s.My == before.My + fileSize && s.Total == before.Total + fileSize);
        after.My.Should().Be(before.My + fileSize,
            "creating a file must charge the \"My documents\" section by its size");
        after.Trash.Should().Be(before.Trash,
            "creating a file must not affect the Trash section");
        after.Total.Should().Be(before.Total + fileSize,
            "creating a file must increase the total space used by the portal");
    }

    [Fact]
    public async Task CreateFile_InRoom_IsChargedToRoomsSectionOnly()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("quota_room_" + Guid.NewGuid().ToString()[..8]);

        var before = await GetBaselineUsedSpaceAsync();

        // Act
        var file = await CreateFile("quota_room_file.docx", room.Id);
        var fileSize = file.PureContentLength!.Value;

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Rooms == before.Rooms + fileSize && s.Total == before.Total + fileSize);
        after.Rooms.Should().Be(before.Rooms + fileSize,
            "the content of a room must be charged to the Rooms section");
        after.My.Should().Be(before.My,
            "the content of a room must not be charged to the \"My documents\" section");
        after.Total.Should().Be(before.Total + fileSize,
            "creating a file in a room must increase the total space used by the portal");
    }

    [Fact]
    public async Task CreateFilesConcurrently_IncreasesUsedSpaceByTheirTotalSize()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();
        var myFolderId = await GetUserFolderIdAsync(Owner);

        // Act
        var files = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(i => CreateFile($"quota_parallel_{i}.docx", myFolderId)));

        // Assert
        var totalSize = files.Sum(f => f.PureContentLength ?? 0);
        totalSize.Should().BePositive();

        var after = await WaitForUsedSpaceAsync(s => s.My == before.My + totalSize && s.Total == before.Total + totalSize);
        after.My.Should().Be(before.My + totalSize,
            "concurrent uploads must not lose any counter update");
        after.Total.Should().Be(before.Total + totalSize,
            "concurrent uploads must all be counted in the total space used by the portal");
    }

    [Fact]
    public async Task MoveFileToTrash_MovesUsedSpaceFromMyDocumentsToTrash_AndEmptyTrashReleasesIt()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();

        var file = await CreateFileInMy("quota_trash.docx", Owner);
        var fileSize = file.PureContentLength!.Value;

        // Act - move the file to the trash
        await DeleteFileAndWait(file.Id, immediately: false);

        // Assert - the space moved from "My documents" to the Trash, the portal still stores the file
        var trashed = await WaitForUsedSpaceAsync(s => s.My == before.My && s.Total == before.Total + fileSize);
        trashed.My.Should().Be(before.My,
            "moving a file to the trash must release the space of the \"My documents\" section");
        trashed.Trash.Should().Be(before.Trash + fileSize,
            "a file moved to the trash must be charged to the Trash section");
        trashed.Total.Should().Be(before.Total + fileSize,
            "a trashed file is still stored, so the total space used by the portal must not change");

        // Act - empty the trash
        await EmptyTrashAndWait();

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash && s.Total == before.Total);
        after.Trash.Should().Be(before.Trash,
            "emptying the trash must release the space of the Trash section");
        after.My.Should().Be(before.My,
            "emptying the trash must not affect the \"My documents\" section");
        after.Total.Should().Be(before.Total,
            "emptying the trash must release the total space used by the portal");
    }

    [Fact]
    public async Task MoveFolderToTrash_MovesUsedSpaceFromMyDocumentsToTrash()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();

        var folder = await CreateFolderInMy("quota_folder_trash", Owner);
        var file = await CreateFile("quota_folder_trash_file.docx", folder.Id);
        var fileSize = file.PureContentLength!.Value;

        // Act
        await DeleteFolderAndWait(folder.Id, immediately: false);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.My == before.My && s.Total == before.Total + fileSize);
        after.My.Should().Be(before.My,
            "moving a folder to the trash must release the space of the \"My documents\" section");
        after.Trash.Should().Be(before.Trash + fileSize,
            "the content of a folder moved to the trash must be charged to the Trash section");
        after.Total.Should().Be(before.Total + fileSize,
            "a trashed folder is still stored, so the total space used by the portal must not change");
    }

    [Fact]
    public async Task MoveFolderWithComplexStructureToTrash_MovesAllUsedSpaceToTrash_AndEmptyTrashReleasesIt()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();

        var root = await CreateFolderInMy("quota_tree_trash", Owner);
        var treeSize = await CreateTreeAsync(root.Id, TreeDepth, TreeBreadth, FilesPerFolder);

        treeSize.Should().BePositive();

        // Act - move the whole tree to the trash
        await DeleteFolderAndWait(root.Id, immediately: false);

        // Assert
        var trashed = await WaitForUsedSpaceAsync(s => s.My == before.My && s.Total == before.Total + treeSize);
        trashed.My.Should().Be(before.My,
            "moving a tree to the trash must release the space of the \"My documents\" section");
        trashed.Trash.Should().Be(before.Trash + treeSize,
            "every file of the trashed tree must be charged to the Trash section");
        trashed.Total.Should().Be(before.Total + treeSize,
            "a trashed tree is still stored, so the total space used by the portal must not change");

        // Act - empty the trash
        await EmptyTrashAndWait();

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash && s.Total == before.Total);
        after.Trash.Should().Be(before.Trash,
            "emptying the trash must release the space of every file of the tree");
        after.My.Should().Be(before.My,
            "emptying the trash must not affect the \"My documents\" section");
        after.Total.Should().Be(before.Total,
            "emptying the trash must release the total space used by the portal");
    }

    [Fact]
    public async Task DeleteFileFromTrash_ReleasesTrashUsedSpace()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();

        var file = await CreateFileInMy("quota_delete.docx", Owner);
        var fileSize = file.PureContentLength!.Value;

        var afterCreate = await WaitForUsedSpaceAsync(s => s.My == before.My + fileSize && s.Total == before.Total + fileSize);
        afterCreate.My.Should().Be(before.My + fileSize);
        afterCreate.Total.Should().Be(before.Total + fileSize);

        await DeleteFileAndWait(file.Id, immediately: false);

        // Act - the space is released only when the file leaves the Trash
        await DeleteFileAndWait(file.Id, immediately: true);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash && s.Total == before.Total);
        after.Trash.Should().Be(before.Trash,
            "deleting a file from the Trash must release the space it occupied");
        after.My.Should().Be(before.My,
            "the \"My documents\" section has already released the space when the file was trashed");
        after.Total.Should().Be(before.Total,
            "deleting a file from the Trash must release the total space used by the portal");
    }

    [Fact]
    public async Task DeleteFolderFromTrash_ReleasesTrashUsedSpace()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();

        var folder = await CreateFolderInMy("quota_folder", Owner);
        var file = await CreateFile("quota_folder_file.docx", folder.Id);
        var fileSize = file.PureContentLength!.Value;

        var afterCreate = await WaitForUsedSpaceAsync(s => s.My == before.My + fileSize && s.Total == before.Total + fileSize);
        afterCreate.My.Should().Be(before.My + fileSize,
            "the content of a subfolder is charged to the root section of that subfolder");
        afterCreate.Total.Should().Be(before.Total + fileSize);

        await DeleteFolderAndWait(folder.Id, immediately: false);

        // Act - the space is released only when the folder leaves the Trash
        await DeleteFolderAndWait(folder.Id, immediately: true);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash && s.Total == before.Total);
        after.Trash.Should().Be(before.Trash,
            "deleting a folder from the Trash must release the space of all its content");
        after.My.Should().Be(before.My,
            "the \"My documents\" section has already released the space when the folder was trashed");
        after.Total.Should().Be(before.Total,
            "deleting a folder from the Trash must release the total space used by the portal");
    }

    [Fact]
    public async Task DeleteFolderWithComplexStructureFromTrash_ReleasesAllTrashUsedSpace()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();

        var root = await CreateFolderInMy("quota_tree", Owner);
        var treeSize = await CreateTreeAsync(root.Id, TreeDepth, TreeBreadth, FilesPerFolder);

        treeSize.Should().BePositive();

        var afterCreate = await WaitForUsedSpaceAsync(s => s.My == before.My + treeSize && s.Total == before.Total + treeSize);
        afterCreate.My.Should().Be(before.My + treeSize,
            "every file of the created tree must be charged to the \"My documents\" section");
        afterCreate.Total.Should().Be(before.Total + treeSize,
            "every file of the created tree must be counted in the total space used by the portal");

        await DeleteFolderAndWait(root.Id, immediately: false);

        var trashed = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash + treeSize && s.Total == before.Total + treeSize);
        trashed.Trash.Should().Be(before.Trash + treeSize,
            "every file of the trashed tree must be charged to the Trash section");
        trashed.Total.Should().Be(before.Total + treeSize,
            "a trashed tree is still stored, so the total space used by the portal must not change");

        // Act - a single delete operation walks the whole tree, processing its branches concurrently
        await DeleteFolderAndWait(root.Id, immediately: true);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash && s.Total == before.Total);
        after.Trash.Should().Be(before.Trash,
            "deleting a tree from the Trash must release the space of every nested file");
        after.My.Should().Be(before.My,
            "the \"My documents\" section has already released the space when the tree was trashed");
        after.Total.Should().Be(before.Total,
            "deleting a tree from the Trash must release the total space used by the portal");
    }

    [Fact]
    public async Task MoveRoomFolderWithComplexStructureToTrash_MovesUsedSpaceFromRoomsToTrash_AndDeleteFromTrashReleasesIt()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("quota_room_tree_" + Guid.NewGuid().ToString()[..8]);

        var before = await GetBaselineUsedSpaceAsync();

        var root = await CreateFolder("quota_room_tree", room.Id);
        var treeSize = await CreateTreeAsync(root.Id, TreeDepth, TreeBreadth, FilesPerFolder);

        treeSize.Should().BePositive();

        var afterCreate = await WaitForUsedSpaceAsync(s => s.Rooms == before.Rooms + treeSize && s.Total == before.Total + treeSize);
        afterCreate.Rooms.Should().Be(before.Rooms + treeSize,
            "every file of the created tree must be charged to the Rooms section");
        afterCreate.Total.Should().Be(before.Total + treeSize,
            "every file of the created tree must be counted in the total space used by the portal");

        // Act - move the tree of the room to the trash
        await DeleteFolderAndWait(root.Id, immediately: false);

        // Assert - the used space moved from the Rooms section to the Trash one
        var trashed = await WaitForUsedSpaceAsync(s => s.Rooms == before.Rooms && s.Total == before.Total + treeSize);
        trashed.Rooms.Should().Be(before.Rooms,
            "moving a tree of a room to the trash must release the space of the Rooms section");
        trashed.Trash.Should().Be(before.Trash + treeSize,
            "every file of the trashed tree must be charged to the Trash section");
        trashed.Total.Should().Be(before.Total + treeSize,
            "a trashed tree is still stored, so the total space used by the portal must not change");

        // Act - delete the tree from the trash
        await DeleteFolderAndWait(root.Id, immediately: true);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash && s.Total == before.Total);
        after.Trash.Should().Be(before.Trash,
            "deleting a tree of a room from the Trash must release the space of every nested file");
        after.My.Should().Be(before.My,
            "deleting a room folder must not affect the \"My documents\" section");
        after.Total.Should().Be(before.Total,
            "deleting a tree of a room from the Trash must release the total space used by the portal");
    }

    [Fact]
    public async Task DeleteComplexFolderAndFileFromTrashAsBatch_ReleasesAllTrashUsedSpace()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();

        var root = await CreateFolderInMy("quota_batch", Owner);
        var treeSize = await CreateTreeAsync(root.Id, TreeDepth, TreeBreadth, FilesPerFolder);

        var looseFile = await CreateFileInMy("quota_batch_file.docx", Owner);
        var totalSize = treeSize + looseFile.PureContentLength!.Value;

        var afterCreate = await WaitForUsedSpaceAsync(s => s.My == before.My + totalSize && s.Total == before.Total + totalSize);
        afterCreate.My.Should().Be(before.My + totalSize);
        afterCreate.Total.Should().Be(before.Total + totalSize);

        await DeleteBatchAndWait(root.Id, looseFile.Id, immediately: false);

        var trashed = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash + totalSize && s.Total == before.Total + totalSize);
        trashed.Trash.Should().Be(before.Trash + totalSize,
            "every trashed item must be charged to the Trash section");
        trashed.Total.Should().Be(before.Total + totalSize,
            "trashed items are still stored, so the total space used by the portal must not change");

        // Act - one operation deletes a whole tree and a standalone file at the same time
        await DeleteBatchAndWait(root.Id, looseFile.Id, immediately: true);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash && s.Total == before.Total);
        after.Trash.Should().Be(before.Trash,
            "a batch delete from the Trash must release the space of every deleted item");
        after.My.Should().Be(before.My,
            "the \"My documents\" section has already released the space when the items were trashed");
        after.Total.Should().Be(before.Total,
            "a batch delete from the Trash must release the total space used by the portal");
    }

    [Fact]
    public async Task DeleteSeveralComplexFoldersFromTrashConcurrently_ReleasesAllTrashUsedSpace()
    {
        // Arrange
        var before = await GetBaselineUsedSpaceAsync();

        const int rootsCount = 4;

        var roots = new List<FolderDtoInteger>(rootsCount);
        for (var i = 0; i < rootsCount; i++)
        {
            roots.Add(await CreateFolderInMy($"quota_concurrent_{i}", Owner));
        }

        var treeSizes = await Task.WhenAll(roots.Select(r => CreateTreeAsync(r.Id, TreeDepth, TreeBreadth, FilesPerFolder)));
        var totalSize = treeSizes.Sum();

        totalSize.Should().BePositive();

        var afterCreate = await WaitForUsedSpaceAsync(s => s.My == before.My + totalSize && s.Total == before.Total + totalSize);
        afterCreate.My.Should().Be(before.My + totalSize,
            "files created concurrently must all be charged to the \"My documents\" section");
        afterCreate.Total.Should().Be(before.Total + totalSize,
            "files created concurrently must all be counted in the total space used by the portal");

        foreach (var root in roots)
        {
            await DeleteFolderAndWait(root.Id, immediately: false);
        }

        var trashed = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash + totalSize && s.Total == before.Total + totalSize);
        trashed.Trash.Should().Be(before.Trash + totalSize,
            "every trashed tree must be charged to the Trash section");
        trashed.Total.Should().Be(before.Total + totalSize,
            "trashed trees are still stored, so the total space used by the portal must not change");

        // Act - start every delete operation at once so their counter updates overlap
        var started = await Task.WhenAll(roots.Select(r =>
            _foldersApi.DeleteFolderAsync(r.Id, new DeleteFolder { Immediately = true }, TestContext.Current.CancellationToken)));

        foreach (var operation in started)
        {
            await WaitForCompletionAsync(operation.Response);
        }

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Trash == before.Trash && s.Total == before.Total);
        after.Trash.Should().Be(before.Trash,
            "concurrent delete operations must not lose any counter update");
        after.My.Should().Be(before.My,
            "the \"My documents\" section has already released the space when the trees were trashed");
        after.Total.Should().Be(before.Total,
            "concurrent delete operations must release the total space used by the portal");
    }

    /// <summary>
    /// Creates a folder tree under <paramref name="parentId"/> and returns the total size of the created files.
    /// Siblings are created concurrently to keep the setup of the long running tests short.
    /// </summary>
    private async Task<long> CreateTreeAsync(int parentId, int depth, int breadth, int filesPerFolder)
    {
        var files = await Task.WhenAll(Enumerable.Range(0, filesPerFolder)
            .Select(i => CreateFile($"quota_tree_{depth}_{i}_{Guid.NewGuid():N}.docx", parentId)));

        var size = files.Sum(f => f.PureContentLength ?? 0);

        if (depth <= 1)
        {
            return size;
        }

        var folders = await Task.WhenAll(Enumerable.Range(0, breadth)
            .Select(i => CreateFolder($"quota_tree_{depth}_{i}_{Guid.NewGuid().ToString()[..8]}", parentId)));

        var nestedSizes = await Task.WhenAll(folders.Select(f => CreateTreeAsync(f.Id, depth - 1, breadth, filesPerFolder)));

        return size + nestedSizes.Sum();
    }

    /// <summary>
    /// A freshly registered portal fills the owner's sections with the default documents in the
    /// background, so the counters keep growing for a while after the portal becomes available.
    /// Reads the statistics until they stop changing, otherwise that content leaks into the delta
    /// measured by a test.
    /// </summary>
    private async Task<UsedSpace> GetBaselineUsedSpaceAsync()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);

        // Provisioning of the owner's root folder tree is lazy, so trigger it before measuring.
        await GetUserFolderIdAsync(Owner);

        var sw = Stopwatch.StartNew();
        var previous = await GetUsedSpaceAsync();
        var stableReads = 0;

        while (sw.Elapsed < TimeSpan.FromSeconds(60))
        {
            await Task.Delay(300, TestContext.Current.CancellationToken);

            var current = await GetUsedSpaceAsync();

            stableReads = previous.SameAs(current) ? stableReads + 1 : 0;
            previous = current;

            if (stableReads == 3)
            {
                break;
            }
        }

        return previous;
    }

    private async Task<UsedSpace> GetUsedSpaceAsync()
    {
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;

        return new UsedSpace(sections, await GetTotalUsedSpaceAsync());
    }

    /// <summary>
    /// Reads the total space used by the portal from the "total_size" quota feature.
    /// </summary>
    /// <remarks>
    /// The typed client (<c>PaymentApi.GetQuotaPaymentInformationAsync</c>) cannot be used here: its
    /// <c>QuotaDto</c> declares "title" as required while a portal of a test returns it empty, so the
    /// answer fails to deserialize. The same endpoint is therefore read as raw JSON.
    /// </remarks>
    private async Task<long> GetTotalUsedSpaceAsync()
    {
        var response = (await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        var feature = response.Features.FirstOrDefault(r=> r.Id == TotalSizeFeature);

        feature.Should().NotBeNull();

        return Convert.ToInt64(feature!.Used.Value);
    }

    /// <summary>
    /// The counters are updated by the operation itself, so they can lag a little behind its completion.
    /// Polls the statistics until <paramref name="condition"/> holds and returns the last read snapshot,
    /// so the caller always asserts on a real value.
    /// </summary>
    private async Task<UsedSpace> WaitForUsedSpaceAsync(Func<UsedSpace, bool> condition)
    {
        var sw = Stopwatch.StartNew();

        while (true)
        {
            var usedSpace = await GetUsedSpaceAsync();

            if (condition(usedSpace))
            {
                return usedSpace;
            }

            if (sw.Elapsed > TimeSpan.FromSeconds(10))
            {
                return usedSpace;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }

    private async Task DeleteFileAndWait(int fileId, bool immediately)
    {
        var results = (await _filesApi.DeleteFileAsync(
            fileId,
            new Delete { Immediately = immediately },
            true,
            TestContext.Current.CancellationToken)).Response;

        await WaitForCompletionAsync(results);
    }

    private async Task DeleteFolderAndWait(int folderId, bool immediately)
    {
        var results = (await _foldersApi.DeleteFolderAsync(
            folderId,
            new DeleteFolder { Immediately = immediately },
            TestContext.Current.CancellationToken)).Response;

        await WaitForCompletionAsync(results);
    }

    private async Task DeleteBatchAndWait(int folderId, int fileId, bool immediately)
    {
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto
            {
                FolderIds = [new(folderId)],
                FileIds = [new(fileId)],
                Immediately = immediately
            },
            TestContext.Current.CancellationToken)).Response;

        await WaitForCompletionAsync(results);
    }

    private async Task EmptyTrashAndWait()
    {
        var results = (await _filesOperationsApi.EmptyTrashAsync(true, TestContext.Current.CancellationToken)).Response;

        await WaitForCompletionAsync(results);
    }

    /// <summary>
    /// Waits for the operations started by a request to finish. Deleting several trees at once takes
    /// longer than the budget of the shared helper, so the polling is done here with a larger one.
    /// </summary>
    private async Task WaitForCompletionAsync(List<FileOperationDto> results)
    {
        var operationId = results.FirstOrDefault()?.Id;
        var sw = Stopwatch.StartNew();

        while (results.Exists(r => !r.Finished) && sw.Elapsed < TimeSpan.FromMinutes(2))
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);

            var statuses = (await _filesOperationsApi.GetOperationStatusesAsync(
                id: operationId,
                cancellationToken: TestContext.Current.CancellationToken)).Response;

            // a finished operation is eventually dropped from the queue, so an empty answer
            // means it is over - any error it reported was visible while it was still listed
            if (statuses.Count == 0)
            {
                return;
            }

            results = statuses;
        }

        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        results.Should().OnlyContain(x => x.Finished);
    }

    /// <summary>
    /// A single reading of every used space counter a test looks at: the per-section statistics and
    /// the total space used by the portal.
    /// </summary>
    private sealed record UsedSpace(FilesStatisticsResultDto Sections, long Total)
    {
        public long My => Space(Sections.MyDocumentsUsedSpace);

        public long Trash => Space(Sections.TrashUsedSpace);

        public long Rooms => Space(Sections.RoomsUsedSpace);

        public long Archive => Space(Sections.ArchiveUsedSpace);

        public bool SameAs(UsedSpace other)
        {
            return My == other.My && Trash == other.Trash && Rooms == other.Rooms && Archive == other.Archive && Total == other.Total;
        }

        private static long Space(FilesStatisticsFolder? folder)
        {
            return folder?.UsedSpace ?? 0;
        }
    }
}
