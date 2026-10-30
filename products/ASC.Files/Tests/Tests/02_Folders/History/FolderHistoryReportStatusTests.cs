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
/// <c>GET /api/2.0/files/folder/{folderId}/log/report</c> - the status of a folder history report.
/// Starting a report is <see cref="FolderHistoryReportTests"/>, access control is
/// <see cref="FolderHistoryReportPermissionsTests"/>.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryReportStatusTests(
    AspireAppFixture fixture)
    : FolderHistoryReportTestBase(fixture)
{
    [Fact]
    public async Task GetReport_NoReportStarted_ReturnsSuccessEnvelopeWithoutTask()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Get Empty");

        // Act
        var wrapper = await _foldersApi.GetReportFolderHistoryAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert - a well-formed 200 envelope without a task, not a failed request
        wrapper.StatusCode.Should().Be(200);
        wrapper.Response.Should().BeNull();
    }

    [Fact]
    public async Task GetReport_StartedReport_HasWellFormedTaskReportedUnderTheSameId()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Task Contract");

        // Act
        var task = (await _foldersApi.CreateReportFolderHistoryAsync(
            room.Id, AuditReportFormat.Csv, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var polled = (await _foldersApi.GetReportFolderHistoryAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        task.Id.Should().NotBeNullOrEmpty();
        task.Error.Should().BeNullOrEmpty();
        task.Percentage.Should().BeInRange(0, 100);
        task.Status.Should().BeOneOf(DistributedTaskStatus.Created, DistributedTaskStatus.Running, DistributedTaskStatus.Completed);

        polled.Should().NotBeNull("GET reports the started task");
        polled!.Id.Should().Be(task.Id);
        polled.Percentage.Should().BeInRange(0, 100);

        await CancelReportAsync(room.Id);
    }

    /// <summary>
    /// The TypeScript original builds an XLSX report and checks the zip signature of the result; here the
    /// report is CSV (see <see cref="FolderHistoryReportTestBase"/>), so the content is only checked to be
    /// non-empty and the rest of the lifecycle - the task, the file info and "My documents" - is asserted as is.
    /// </summary>
    [Fact]
    public async Task GetReport_CsvReport_CompletesAndResultIsAvailableInMyDocuments()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Lifecycle");

        // Act
        var task = await RunCsvReportAsync(room.Id);

        // Assert
        task.Should().NotBeNull();
        task!.Status.Should().Be(DistributedTaskStatus.Completed, $"the report ended with: {task.Error}");
        task.Error.Should().BeNullOrEmpty();
        task.Percentage.Should().Be(100);
        task.ResultFileName.Should().NotBeNullOrEmpty();
        task.ResultFileUrl.Should().NotBeNullOrEmpty();

        var fileId = ResultFileIdOf(task);
        var info = (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be(task.ResultFileName);

        (await GetMyFileTitlesAsync()).Should().Contain(task.ResultFileName);
        (await DownloadFileAsync(fileId)).Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetReport_SecondReportAfterCompletion_CompletesWithItsOwnResultFile()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Rerun");
        var first = await RunCsvReportAsync(room.Id);
        first.Should().NotBeNull();
        first!.Status.Should().Be(DistributedTaskStatus.Completed, $"the first report ended with: {first.Error}");

        // Act
        var second = await RunCsvReportAsync(room.Id);

        // Assert
        second.Should().NotBeNull();
        second!.Status.Should().Be(DistributedTaskStatus.Completed, $"the second report ended with: {second.Error}");
        second.Error.Should().BeNullOrEmpty();

        // Guards against reading the first run's finished task as the second run
        ResultFileIdOf(second).Should().NotBe(ResultFileIdOf(first), "the second run has its own result file");

        (await _filesApi.GetFileInfoAsync(ResultFileIdOf(second), cancellationToken: TestContext.Current.CancellationToken))
            .Response.Title.Should().Be(second.ResultFileName);
        (await DownloadFileAsync(ResultFileIdOf(second))).Should().NotBeEmpty();
    }

    /// <summary>
    /// Observation of current behaviour: the task is bound to the folder it was started for. The documentation
    /// speaks of "this folder and caller" but does not state how tasks of different folders relate.
    /// </summary>
    [Fact]
    public async Task GetReport_ReportOfOneFolder_IsNotReportedForAnotherFolder()
    {
        // Arrange
        var roomA = await CreateCustomRoom("Autotest Report Folder A");
        var roomB = await CreateCustomRoom("Autotest Report Folder B");
        var reportA = await RunCsvReportAsync(roomA.Id);
        reportA.Should().NotBeNull();
        reportA!.Status.Should().Be(DistributedTaskStatus.Completed, $"the report of A ended with: {reportA.Error}");

        // Act
        var beforeB = await _foldersApi.GetReportFolderHistoryAsync(roomB.Id, TestContext.Current.CancellationToken);
        var reportB = await RunCsvReportAsync(roomB.Id);
        var afterA = (await _foldersApi.GetReportFolderHistoryAsync(roomA.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        beforeB.StatusCode.Should().Be(200);
        beforeB.Response.Should().BeNull("folder B has not been reported yet");

        reportB.Should().NotBeNull();
        reportB!.Status.Should().Be(DistributedTaskStatus.Completed, $"the report of B ended with: {reportB.Error}");
        ResultFileIdOf(reportB).Should().NotBe(ResultFileIdOf(reportA));

        afterA.Should().NotBeNull("folder A still reports its task");
        ResultFileIdOf(afterA!).Should().Be(ResultFileIdOf(reportA));
    }

    [Fact]
    public async Task GetReport_NonExistentFolderId_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetReportFolderHistoryAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}
