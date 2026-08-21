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

namespace ASC.Files.Tests.Tests._03_Rooms.Reorder;

/// <summary>
/// Functional coverage of <c>PUT /files/rooms/{id}/reorder</c>: it compacts every folder/file
/// index directly under the room to a dense, gap-free <c>1..N</c> range while preserving relative
/// order, and leaves nested content and item identity untouched. Permission coverage lives in
/// <see cref="Permissions.RoomReorderPermissionsTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomReorderTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private async Task<int> CreateFolder(int roomId, string title)
    {
        return (await _foldersApi.CreateFolderAsync(roomId, new CreateFolder(title), TestContext.Current.CancellationToken)).Response.Id;
    }

    private async Task SetFolderOrder(int folderId, int order)
    {
        await _foldersApi.SetFolderOrderAsync(folderId, new OrderRequestDto(order), TestContext.Current.CancellationToken);
    }

    private async Task<(int Id, string Title)> CreateFile(int roomId, string title)
    {
        var response = (await _filesApi.CreateFileAsync(
            roomId, new CreateFileJsonElement(title), TestContext.Current.CancellationToken)).Response;

        return (response.Id, response.Title);
    }

    private async Task SetFileOrder(int fileId, int order)
    {
        await _filesApi.SetFileOrderAsync(fileId, new OrderRequestDto(order), TestContext.Current.CancellationToken);
    }

    private async Task<FolderContentDtoInteger> GetContent(int folderId)
    {
        return (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    [Fact]
    public async Task ReorderRoom_EmptyVdr_Reordered()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Empty Room");

        // Act
        var reordered = (await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        reordered.Id.Should().Be(room.Id);
    }

    [Fact]
    public async Task ReorderRoom_FoldersWithGaps_CompactedPreservingOrder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Room With Content");

        var folderA = await CreateFolder(room.Id, "Folder A");
        var folderB = await CreateFolder(room.Id, "Folder B");
        var folderC = await CreateFolder(room.Id, "Folder C");

        await SetFolderOrder(folderA, 10);
        await SetFolderOrder(folderB, 50);
        await SetFolderOrder(folderC, 30);

        var before = await GetContent(room.Id);
        before.Folders.ConvertAll(f => int.Parse(f.Order)).SequenceEqual([1, 2, 3]).Should().BeFalse(
            "freshly nudged orders must have gaps, not the sequential 1,2,3");

        // Act
        var reordered = (await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        reordered.Id.Should().Be(room.Id);

        var after = await GetContent(room.Id);
        after.Folders.ConvertAll(f => (f.Title, int.Parse(f.Order))).Should().Equal(
            ("Folder A", 1), ("Folder C", 2), ("Folder B", 3));
    }

    [Fact]
    public async Task ReorderRoom_AlreadySequential_Unchanged()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Sequential");

        await CreateFolder(room.Id, "Folder A");
        await CreateFolder(room.Id, "Folder B");
        await CreateFolder(room.Id, "Folder C");

        var before = await GetContent(room.Id);
        var ordersBefore = before.Folders.ConvertAll(f => (Title: f.Title, Order: int.Parse(f.Order)));
        ordersBefore.ConvertAll(o => o.Order).Should().Equal([1, 2, 3]);

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert - a no-op when order is already sequential
        var after = await GetContent(room.Id);
        after.Folders.ConvertAll(f => (f.Title, int.Parse(f.Order))).Should().Equal(ordersBefore);
    }

    [Fact]
    public async Task ReorderRoom_FilesWithGaps_CompactedPreservingOrder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Files");

        var fileA = await CreateFile(room.Id, "File A");
        var fileB = await CreateFile(room.Id, "File B");
        var fileC = await CreateFile(room.Id, "File C");

        await SetFileOrder(fileA.Id, 10);
        await SetFileOrder(fileC.Id, 30);
        await SetFileOrder(fileB.Id, 50);

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert - gaps removed, relative order (A < C < B) preserved
        var after = await GetContent(room.Id);
        var byTitle = after.Files.ToDictionary(f => f.Title, f => int.Parse(f.Order));
        byTitle[fileA.Title].Should().Be(1);
        byTitle[fileC.Title].Should().Be(2);
        byTitle[fileB.Title].Should().Be(3);
    }

    [Fact]
    public async Task ReorderRoom_MixedFoldersAndFiles_CompactedWithoutGaps()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Mixed");

        var folderId = await CreateFolder(room.Id, "Mixed Folder");
        var file = await CreateFile(room.Id, "Mixed File");

        await SetFolderOrder(folderId, 20);
        await SetFileOrder(file.Id, 90);

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert - the combined folder+file index is compacted to a contiguous 1..N range
        var after = await GetContent(room.Id);
        var allOrders = after.Folders.ConvertAll(f => int.Parse(f.Order))
            .Concat(after.Files.ConvertAll(f => int.Parse(f.Order)))
            .OrderBy(o => o)
            .ToList();
        allOrders.Should().Equal([1, 2]);
    }

    [Fact]
    public async Task ReorderRoom_NestedFolderContent_NotAffectedByRootReorder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Nested");

        // A folder at the room root with a nested file inside it
        var parentFolderId = await CreateFolder(room.Id, "Parent Folder");
        var nestedFile = await CreateFile(parentFolderId, "Nested File");

        // A second root folder, with a sparse order forcing a root reindex
        var rootFolderId = await CreateFolder(room.Id, "Root Folder");
        await SetFolderOrder(rootFolderId, 77);
        await SetFolderOrder(parentFolderId, 5);

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert - root level reindexed to 1..N
        var rootAfter = await GetContent(room.Id);
        rootAfter.Folders.ConvertAll(f => int.Parse(f.Order)).OrderBy(o => o).Should().Equal([1, 2]);

        // Nested file still lives inside the parent folder, untouched
        var nestedAfter = await GetContent(parentFolderId);
        nestedAfter.Files.ConvertAll(f => f.Title).Should().Contain(nestedFile.Title);
    }

    [Fact]
    public async Task ReorderRoom_SingleItem_GetsOrderOne()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Single");

        var folderId = await CreateFolder(room.Id, "Lonely Folder");
        await SetFolderOrder(folderId, 50);

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var after = await GetContent(room.Id);
        int.Parse(after.Folders[0].Order).Should().Be(1);
    }

    [Fact]
    public async Task ReorderRoom_LargeGaps_CompactedToOneToN()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Large Gaps");

        var folderA = await CreateFolder(room.Id, "Folder A");
        var folderB = await CreateFolder(room.Id, "Folder B");
        var folderC = await CreateFolder(room.Id, "Folder C");

        await SetFolderOrder(folderA, 100);
        await SetFolderOrder(folderB, 5000);
        await SetFolderOrder(folderC, 999999);

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        var after = await GetContent(room.Id);
        after.Folders.ConvertAll(f => (f.Title, int.Parse(f.Order))).Should().Equal(
            ("Folder A", 1), ("Folder B", 2), ("Folder C", 3));
    }

    [Fact]
    public async Task ReorderRoom_DuplicateOrderValues_CompactedToDenseUniqueRange()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Duplicates");

        var folderA = await CreateFolder(room.Id, "Folder A");
        var folderB = await CreateFolder(room.Id, "Folder B");
        var folderC = await CreateFolder(room.Id, "Folder C");

        // Force the same order value on every folder
        await SetFolderOrder(folderA, 5);
        await SetFolderOrder(folderB, 5);
        await SetFolderOrder(folderC, 5);

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert - the tie-break order is not contractually defined, so only the resulting
        // indexes being dense and unique (1..N) is asserted, not a specific sort.
        var after = await GetContent(room.Id);
        after.Folders.ConvertAll(f => int.Parse(f.Order)).OrderBy(o => o).Should().Equal([1, 2, 3]);
    }

    [Fact]
    public async Task ReorderRoom_RepeatedReorder_Idempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Idempotent");

        var folderA = await CreateFolder(room.Id, "Folder A");
        var folderB = await CreateFolder(room.Id, "Folder B");
        await SetFolderOrder(folderA, 40);
        await SetFolderOrder(folderB, 10);

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);
        var afterFirst = await GetContent(room.Id);
        var ordersFirst = afterFirst.Folders.ConvertAll(f => (f.Title, int.Parse(f.Order)));

        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);
        var afterSecond = await GetContent(room.Id);
        var ordersSecond = afterSecond.Folders.ConvertAll(f => (f.Title, int.Parse(f.Order)));

        // Assert
        ordersSecond.Should().Equal(ordersFirst);
    }

    [Fact]
    public async Task ReorderRoom_DoesNotDeleteDuplicateRenameOrMoveItems()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Integrity");

        var folderA = await CreateFolder(room.Id, "Folder A");
        var folderB = await CreateFolder(room.Id, "Folder B");
        await CreateFile(room.Id, "Doc");
        await SetFolderOrder(folderA, 30);
        await SetFolderOrder(folderB, 10);

        var before = await GetContent(room.Id);
        var foldersBefore = before.Folders.ConvertAll(f => f.Title).Order().ToList();
        var filesBefore = before.Files.ConvertAll(f => f.Title).Order().ToList();

        // Act
        await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert - same titles, same counts: nothing added, removed or renamed
        var after = await GetContent(room.Id);
        after.Folders.ConvertAll(f => f.Title).Order().Should().Equal(foldersBefore);
        after.Files.ConvertAll(f => f.Title).Order().Should().Equal(filesBefore);
    }

    /// <summary>
    /// An invalid/out-of-range numeric id should be a validation error (400), but the API does not
    /// pre-validate: the storage layer throws <c>InvalidOperationException</c> ("The required folder
    /// was not found") from <c>ReOrderAsync</c>, which is mapped to 403 - the same defect class as the
    /// sibling pinRoom endpoint (BUG 81850).
    /// </summary>
    [Trait("Bug", "81862")]
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999999999)]
    public async Task ReorderRoom_InvalidId_ReturnsBadRequest(int id)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ReorderRoomAsync(id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>
    /// A well-formed id for a room that was deleted should be 404 (not found), but the same
    /// missing-folder path returns 403 (BUG 81863).
    /// </summary>
    [Trait("Bug", "81863")]
    [Fact]
    public async Task ReorderRoom_DeletedRoom_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Deleted");

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task ReorderRoom_ArchivedRoom_Rejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Reorder Archived");

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ReorderRoom_NonIndexedRoom_ReorderedWithoutError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Reorder Non-indexed");

        await CreateFolder(room.Id, "Folder A");
        await CreateFolder(room.Id, "Folder B");

        // Act
        var reordered = (await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert - content must remain intact after reordering a non-indexed room
        reordered.Id.Should().Be(room.Id);

        var after = await GetContent(room.Id);
        after.Folders.ConvertAll(f => f.Title).Should().Contain(["Folder A", "Folder B"]);
    }

    [Fact]
    public async Task ReorderRoom_VdrWithIndexingDisabled_ReorderedWithoutError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        // Same room type as the indexed tests, but indexing is explicitly off - this exercises a
        // different controller path than a CustomRoom that never supports it.
        var room = await CreateVirtualRoom("Autotest Reorder VDR No Indexing", indexing: false);

        await CreateFolder(room.Id, "Folder A");
        await CreateFolder(room.Id, "Folder B");

        // Act
        var reordered = (await _roomsApi.ReorderRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert - content must remain intact after reordering a non-indexed VDR room
        reordered.Id.Should().Be(room.Id);

        var after = await GetContent(room.Id);
        after.Folders.ConvertAll(f => f.Title).Should().Contain(["Folder A", "Folder B"]);
    }
}
