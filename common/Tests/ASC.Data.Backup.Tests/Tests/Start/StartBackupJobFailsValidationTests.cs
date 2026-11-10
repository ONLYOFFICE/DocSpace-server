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


namespace ASC.Data.Backup.Tests.Tests.Start;

/// <summary>
/// POST /api/2.0/backup/startbackup with a storage the server accepts and the job then fails to write
/// into. The API says a non-empty <c>error</c> of the progress is the only report of such a failure, and
/// the failed job must leave no archive behind.
/// </summary>
public class StartBackupJobFailsValidationTests(
    AspireAppFixture fixture)
    : StartBackupValidationTestBase(fixture)
{
    private static readonly Dictionary<string, Func<StartBackupRequestDto>> _failsInTheJob = new()
    {
        ["a ThirdPartyConsumer request without a module"] = () => new StartBackupRequestDto { StorageType = BackupStorageType.ThirdPartyConsumer },
        ["a ThirdPartyConsumer request with an unknown module"] = () => new StartBackupRequestDto
        {
            StorageType = BackupStorageType.ThirdPartyConsumer,
            StorageParams = [Item("module", "no-such-module")]
        },
        ["the unsupported CustomCloud storage"] = () => new StartBackupRequestDto { StorageType = BackupStorageType.CustomCloud },
        ["a storageType outside the enum"] = () => new StartBackupRequestDto { StorageType = (BackupStorageType)99 },
        ["a negative storageType"] = () => new StartBackupRequestDto { StorageType = (BackupStorageType)(-1) }
    };

    public static TheoryData<string> FailsInTheJobCases => [.. _failsInTheJob.Keys];

    [Theory]
    [MemberData(nameof(FailsInTheJobCases))]
    public async Task StartBackup_StorageTheJobCannotWriteTo_AcceptsItAndTheJobFailsWithoutAnArchive(string name)
    {
        // Act
        var started = await _backupApi.StartBackupAsync(_failsInTheJob[name](), TestContext.Current.CancellationToken);

        // Assert
        started.StatusCode.Should().Be(200);
        started.Response.TaskId.Should().NotBeNullOrEmpty();

        var job = await WaitForBackupJobEndAsync(started.Response.TaskId);
        job.Error.Should().NotBeNullOrEmpty();

        (await ListHistoryIdsAsync()).Should().BeEmpty();
    }
}
