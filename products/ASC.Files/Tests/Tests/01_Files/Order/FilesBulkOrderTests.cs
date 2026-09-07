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

namespace ASC.Files.Tests.Tests._01_Files.Order;

/// <summary>
/// Functional coverage of <c>PUT /files/order</c>, the bulk-order endpoint. It only works on
/// entries inside a room (a VDR with indexing enabled) - entries in My Documents return 403, the
/// same way room-level reordering does. Permission coverage lives in
/// <see cref="FilesBulkOrderPermissionsTests"/>; the single-entry endpoint lives in
/// <see cref="FileOrderTests"/>.
/// </summary>
[Trait("Category", "Files")]
public class FilesBulkOrderTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetFilesOrder_SingleFileInVdrRoom_ReturnsOrderedEntry()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Single File Room");
        var file = await CreateFile("Autotest BulkOrder Single File", room.Id);

        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 5)]);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().HaveCount(1);
        result.Response[0].Id.Should().Be(file.Id);
        result.Response[0].Title.Should().Be("Autotest BulkOrder Single File.docx");
        result.Response[0].FileEntryType.Should().Be(FileEntryType.File);
    }

    [Fact]
    public async Task SetFilesOrder_SingleFolderInVdrRoom_ReturnsOrderedEntry()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Single Folder Room");
        var folder = await CreateFolder("Autotest BulkOrder Single Folder", room.Id);

        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(folder.Id, FileEntryType.Folder, 3)]);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().HaveCount(1);
        result.Response[0].Id.Should().Be(folder.Id);
        result.Response[0].FileEntryType.Should().Be(FileEntryType.Folder);
    }

    /// <summary>
    /// Verified against the room's actual folder content, not just the echoed response, so a batch
    /// that silently drops or misorders an item is caught here too.
    /// </summary>
    [Fact]
    public async Task SetFilesOrder_MultipleFiles_ReturnsAllOrderedAndReflectedInContent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Multi File Room");
        var file1 = await CreateFile("Autotest BulkOrder Multi File 1", room.Id);
        var file2 = await CreateFile("Autotest BulkOrder Multi File 2", room.Id);

        var request = new OrdersRequestDtoInteger([
            new OrdersItemRequestDtoInteger(file1.Id, FileEntryType.File, 2),
            new OrdersItemRequestDtoInteger(file2.Id, FileEntryType.File, 1)
        ]);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().HaveCount(2);
        result.Response.ConvertAll(e => e.Id).Should().Contain([file1.Id, file2.Id]);

        var content = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Files.First().Title.Should().Be(file2.Title);
        content.Files.Last().Title.Should().Be(file1.Title);
    }

    /// <summary>
    /// Verified against the room's actual folder content, not just the echoed response, so folders
    /// silently dropped from a mixed batch would be caught here too.
    /// </summary>
    [Fact]
    public async Task SetFilesOrder_MixOfFilesAndFolders_ReturnsAllOrderedAndReflectedInContent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Mix Room");
        var file = await CreateFile("Autotest BulkOrder Mix File", room.Id);
        var folder = await CreateFolder("Autotest BulkOrder Mix Folder", room.Id);

        var request = new OrdersRequestDtoInteger([
            new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 1),
            new OrdersItemRequestDtoInteger(folder.Id, FileEntryType.Folder, 2)
        ]);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().HaveCount(2);
        var fileItem = result.Response.Should().ContainSingle(e => e.Id == file.Id).Subject;
        var folderItem = result.Response.Should().ContainSingle(e => e.Id == folder.Id).Subject;
        fileItem.FileEntryType.Should().Be(FileEntryType.File);
        folderItem.FileEntryType.Should().Be(FileEntryType.Folder);

        var content = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Files.Should().ContainSingle(f => f.Title == file.Title);
        content.Folders.Should().ContainSingle(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task SetFilesOrder_OrderValueUpdatedAgain_ReturnsUpdatedEntry()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Update Room");
        var file = await CreateFile("Autotest BulkOrder Update File", room.Id);

        await _filesApi.SetFilesOrderAsync(
            new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 3)]),
            TestContext.Current.CancellationToken);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(
            new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 7)]),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response[0].Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task SetFilesOrder_EmptyItems_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(new OrdersRequestDtoInteger([]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>
    /// Bug 81187: order 0 in a bulk request answered HTTP 200 with the error folded into the body's
    /// <c>status</c> field, instead of the HTTP 400 the single-entry endpoint returns for the same
    /// invalid value (<see cref="FileOrderTests.SetFileOrder_ZeroValue_ReturnsBadRequest"/>). Both
    /// endpoints validate the same <c>order</c> range and should fail the same way.
    /// </summary>
    [Trait("Bug", "81187")]
    [Fact]
    public async Task SetFilesOrder_ZeroValue_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Zero Room");
        var file = await CreateFile("Autotest BulkOrder Zero File", room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.SetFilesOrderAsync(
            new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 0)]),
            TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetFilesOrder_MinimumValueOne_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Min Room");
        var file = await CreateFile("Autotest BulkOrder Min File", room.Id);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(
            new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 1)]),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response[0].Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task SetFilesOrder_MaxIntValue_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Max Room");
        var file = await CreateFile("Autotest BulkOrder Max File", room.Id);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(
            new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, int.MaxValue)]),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response[0].Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task SetFilesOrder_SameOrderValueForMultipleItems_Accepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Dup Order Room");
        var file1 = await CreateFile("Autotest BulkOrder Dup Order 1", room.Id);
        var file2 = await CreateFile("Autotest BulkOrder Dup Order 2", room.Id);

        var request = new OrdersRequestDtoInteger([
            new OrdersItemRequestDtoInteger(file1.Id, FileEntryType.File, 5),
            new OrdersItemRequestDtoInteger(file2.Id, FileEntryType.File, 5)
        ]);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().HaveCount(2);
    }

    [Fact]
    public async Task SetFilesOrder_NonExistentEntryId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(999999999, FileEntryType.File, 1)]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetFilesOrder_FileIdWithFolderEntryType_ReturnsNotFound()
    {
        // Arrange - a file id looked up as a folder must not resolve to anything
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Wrong Type Room");
        var file = await CreateFile("Autotest BulkOrder Wrong Type File", room.Id);

        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.Folder, 1)]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SetFilesOrder_ResponseHasCorrectStructure()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Structure Room");
        var file = await CreateFile("Autotest BulkOrder Structure File", room.Id);

        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 4)]);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Count.Should().Be(1);
        result.Response.Should().HaveCount(1);
        result.Response[0].Id.Should().NotBe(0);
        result.Response[0].Title.Should().NotBeNullOrEmpty();
        result.Response[0].FileEntryType.Should().NotBeNull();
    }
}
