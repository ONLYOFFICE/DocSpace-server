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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// <c>DELETE /api/2.0/files/folder/{folderId}/log/report</c> - giving up a folder history report.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryReportTerminateTests(
    AspireAppFixture fixture)
    : FolderHistoryReportTestBase(fixture)
{
    /// <summary>
    /// The report takes a couple of seconds, so the terminate request races with completion. Both documented
    /// outcomes are accepted and each is checked against its own invariant, after the task has stopped. A
    /// "completed" outcome is not proof that cancellation works.
    /// </summary>
    [Fact]
    public async Task Terminate_RightAfterStart_EndsCanceledWithoutResultOrCompletedWithResult()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Terminate Running");
        await _foldersApi.CreateReportFolderHistoryAsync(room.Id, AuditReportFormat.Csv, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        await _foldersApi.TerminateReportFolderHistoryAsync(room.Id, TestContext.Current.CancellationToken);
        var task = await WaitForReportAsync(room.Id);

        // Assert
        task.Should().NotBeNull();
        task!.Status.Should().BeOneOf(DistributedTaskStatus.Completed, DistributedTaskStatus.Canceled);

        if (task.Status == DistributedTaskStatus.Canceled)
        {
            task.IsCompleted.Should().BeTrue();
            (task.ResultFileId ?? 0).Should().Be(0, "a cancelled report names no file");
            task.ResultFileName.Should().BeNullOrEmpty();
            task.ResultFileUrl.Should().BeNullOrEmpty();
        }
        else
        {
            var resultId = ResultFileIdOf(task);
            (await _filesApi.GetFileInfoAsync(resultId, cancellationToken: TestContext.Current.CancellationToken)).Response.Should().NotBeNull();
            (await DownloadFileAsync(resultId)).Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task Terminate_FinishedReport_KeepsItsResultFile()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Terminate Finished");
        var task = await RunCsvReportAsync(room.Id);
        task.Should().NotBeNull();
        task!.Status.Should().Be(DistributedTaskStatus.Completed, $"the report ended with: {task.Error}");
        var fileId = ResultFileIdOf(task);
        var before = await DownloadFileAsync(fileId);

        // Act
        await _foldersApi.TerminateReportFolderHistoryAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert - the state the server reports for the task afterwards is not asserted; only the file must survive
        (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.Should().NotBeNull();
        (await GetMyFileTitlesAsync()).Should().Contain(task.ResultFileName);
        (await DownloadFileAsync(fileId)).Should().Equal(before);
    }

    [Fact]
    public async Task Terminate_NoReport_IsAcceptedRepeatedlyAndLeavesNoTask()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Terminate Nothing");

        // Act
        await _foldersApi.TerminateReportFolderHistoryAsync(room.Id, TestContext.Current.CancellationToken);
        await _foldersApi.TerminateReportFolderHistoryAsync(room.Id, TestContext.Current.CancellationToken);
        var report = await _foldersApi.GetReportFolderHistoryAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        report.StatusCode.Should().Be(200);
        report.Response.Should().BeNull();
    }

    [Fact]
    public async Task Terminate_NonExistentFolderId_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.TerminateReportFolderHistoryAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}
