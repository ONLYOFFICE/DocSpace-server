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

namespace ASC.Files.Tests.Tests._06_Operations.Terminate;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/terminate/{id}</c> (<c>terminateTasks</c>) — functional coverage
/// for every kind of operation that can be cancelled (delete, duplicate, copy, move, markAsRead,
/// emptyTrash), plus a non-existent id and an operation that has already finished. Derives from
/// <see cref="OperationsStatusesTestBase"/> to reuse its operation-starting and polling helpers
/// instead of duplicating them. Access control lives in <see cref="TerminateTasksPermissionsTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class TerminateTasksTests(
    AspireAppFixture fixture)
    : OperationsStatusesTestBase(fixture)
{
    [Fact]
    public async Task TerminateTasks_NonExistentOperationId_ReturnsEmpty()
    {
        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(
            "00000000-0000-0000-0000-000000000000", TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task TerminateTasks_DeleteOperation_RemovedFromActiveList()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest TerminateTasks Delete.docx", Owner);
        var operationId = await StartDelete(file.Id, immediately: false);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
        (await WaitUntilGone(operationId)).Should().BeEmpty();
    }

    [Fact]
    public async Task TerminateTasks_DuplicateOperation_RemovedFromActiveList()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest TerminateTasks Duplicate.docx", Owner);
        var operationId = await StartDuplicate(file.Id);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
        (await WaitUntilGone(operationId)).Should().BeEmpty();
    }

    /// <remarks>
    /// A single-file immediate delete usually finishes and is pruned from the active list before the
    /// caller can even ask about it (see <see cref="OperationsStatusesTestBase.WaitUntilGone"/>), so
    /// the operation id used here comes straight from the start call's own response rather than from
    /// polling for a "finished" entry that may never be observed.
    /// </remarks>
    [Fact]
    public async Task TerminateTasks_AlreadyCompletedOperation_ReturnsEmpty()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest TerminateTasks Completed.docx", Owner);

        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto(fileIds: [new DeleteBatchRequestDtoAllOfFileIds(file.Id)], immediately: true),
            TestContext.Current.CancellationToken)).Response;
        var operationId = results[0].Id;

        await WaitUntilGone(operationId);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task TerminateTasks_CopyOperation_RemovedFromActiveList()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest TerminateTasks Copy Source.docx", Owner);
        var destRoom = await CreateCustomRoom("Autotest TerminateTasks Copy Dest Room");

        var operationId = await StartCopy(file.Id, destRoom.Id);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
        (await WaitUntilGone(operationId)).Should().BeEmpty();
    }

    [Fact]
    public async Task TerminateTasks_MoveOperation_RemovedFromActiveList()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest TerminateTasks Move Source.docx", Owner);
        var destRoom = await CreateCustomRoom("Autotest TerminateTasks Move Dest Room");

        var operationId = await StartMove(file.Id, destRoom.Id);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
        (await WaitUntilGone(operationId)).Should().BeEmpty();
    }

    [Fact]
    public async Task TerminateTasks_MarkAsReadOperation_RemovedFromActiveList()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest TerminateTasks MarkAsRead.docx", Owner);
        var operationId = await StartMarkAsRead(file.Id);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
        (await WaitUntilGone(operationId)).Should().BeEmpty();
    }

    [Fact]
    public async Task TerminateTasks_EmptyTrashOperation_RemovedFromActiveList()
    {
        // Arrange - enough files in trash for emptying it to still be in progress when we terminate.
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);

        var files = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(i => CreateFile($"Autotest TerminateTasks EmptyTrash {i}.docx", myDocsFolderId)));

        await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto(
                fileIds: [.. files.Select(f => new DeleteBatchRequestDtoAllOfFileIds(f.Id))],
                immediately: false),
            TestContext.Current.CancellationToken);

        var results = (await _filesOperationsApi.EmptyTrashAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        var operationId = results[0].Id;

        await WaitUntilInProgress(operationId);

        // Act
        var response = (await _filesOperationsApi.TerminateTasksAsync(operationId, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
    }
}
