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
/// Shared setup of the history report status and terminate suites. Reports are built as CSV: a
/// spreadsheet report needs the document server, which the integration host does not run, and it
/// would hold the worker until that request times out. The format is not what these suites are about.
/// </summary>
public abstract class FolderHistoryReportTestBase(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    /// <summary>
    /// Polls the report of <paramref name="folderId"/> until the task has stopped (finished, cancelled or
    /// failed) and returns the last observed state, <c>null</c> when the server reported no task. The loop
    /// ends on a deadline instead of throwing so a timed-out assertion shows what was actually there.
    /// </summary>
    protected async Task<DocumentBuilderTaskDto?> WaitForReportAsync(int folderId, int timeoutSeconds = 60)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        DocumentBuilderTaskDto? report;

        while (true)
        {
            report = (await _foldersApi.GetReportFolderHistoryAsync(folderId, TestContext.Current.CancellationToken)).Response;

            var stopped = report is not null
                && (report.Status == DistributedTaskStatus.Failted
                    || (report.IsCompleted
                        && report.Status is not DistributedTaskStatus.Created and not DistributedTaskStatus.Running));

            if (stopped || DateTime.UtcNow >= deadline)
            {
                return report;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Starts a CSV report of <paramref name="folderId"/> and waits until it has stopped.</summary>
    protected async Task<DocumentBuilderTaskDto?> RunCsvReportAsync(int folderId)
    {
        await _foldersApi.CreateReportFolderHistoryAsync(folderId, AuditReportFormat.Csv, cancellationToken: TestContext.Current.CancellationToken);

        return await WaitForReportAsync(folderId);
    }

    /// <summary>The result file id of a report; the model types it as a bare object holding a JSON number.</summary>
    protected static int ResultFileIdOf(DocumentBuilderTaskDto report)
    {
        return int.Parse(report.ResultFileId.ToString()!, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Reads a saved file through the file handler: the SDK exposes no operation that returns a file's content.
    /// </summary>
    protected async Task<byte[]> DownloadFileAsync(int fileId)
    {
        using var response = await _filesClient.GetAsync($"filehandler.ashx?action=download&fileid={fileId}", TestContext.Current.CancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeTrue($"the file {fileId} should be downloadable");

        return bytes;
    }

    /// <summary>The titles of the files in the owner's "My documents".</summary>
    protected async Task<List<string>> GetMyFileTitlesAsync()
    {
        var myFolderId = await GetUserFolderIdAsync(Owner);
        var content = (await _foldersApi.GetFolderByFolderIdAsync(myFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Files.ConvertAll(f => f.Title);
    }
}
