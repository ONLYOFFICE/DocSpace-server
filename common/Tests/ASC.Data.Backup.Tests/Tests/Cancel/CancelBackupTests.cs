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


namespace ASC.Data.Backup.Tests.Tests.Cancel;

/// <summary>
/// POST /api/2.0/backup/cancelbackup of a backup that is running. A cancel only takes once the backup
/// worker has the job running, so the backup is started with <c>StartBackupAndCatchItRunningAsync</c>,
/// and the queue is watched until it is quiet because the cancelled job leaves it.
/// </summary>
public class CancelBackupTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(BackupCallerData.All), MemberType = typeof(BackupCallerData))]
    public async Task CancelBackup_RunningBackup_StopsItAndLeavesNoCopy(BackupCaller caller)
    {
        // Arrange
        await ActAsAsync(caller);
        var taskId = await StartBackupAndCatchItRunningAsync();

        // Act
        var cancelled = await _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken);

        // Assert
        cancelled.StatusCode.Should().Be(200);
        cancelled.Response.Should().BeTrue();

        var observation = await ObserveJobUntilQuiescentAsync(taskId);
        observation.Settled.Should().BeTrue($"the queue never became quiet after the cancel; states: {string.Join(" -> ", observation.States)}");

        // No job at all, or a job that ended as canceled - not one that ran to the end.
        if (observation.Final is not null)
        {
            observation.Final.IsCompleted.Should().BeTrue();
            observation.Final.Status.Should().Be(DistributedTaskStatus.Canceled, $"states: {string.Join(" -> ", observation.States)}");
        }

        var historyIds = await ReadHistoryIdsAsync();
        historyIds.Should().NotContain(id => string.Equals(id, taskId, StringComparison.OrdinalIgnoreCase), "a canceled backup leaves no copy");
    }
}
