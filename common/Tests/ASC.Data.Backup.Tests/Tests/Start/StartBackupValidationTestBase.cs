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
/// What the startbackup validation suites share: sending a request that is expected to be refused and
/// proving it changed nothing. The portal is fresh, so before the request there is no job, no history and
/// no counts, and after it there must still be none.
/// </summary>
public abstract class StartBackupValidationTestBase(AspireAppFixture fixture) : BackupLifecycleTestBase(fixture)
{
    private const string StartBackupPath = "api/2.0/backup/startbackup";

    /// <summary>A storageParams item.</summary>
    protected static ItemKeyValuePairObjectObject Item(object? key, object? value)
    {
        return new ItemKeyValuePairObjectObject(key!, value!);
    }

    /// <summary>A request for the portal documents with the given storage parameters.</summary>
    protected static StartBackupRequestDto Documents(params ItemKeyValuePairObjectObject[] storageParams)
    {
        return new StartBackupRequestDto { StorageType = BackupStorageType.Documents, StorageParams = storageParams.ToList() };
    }

    /// <summary>Sends a typed request that is expected to be refused and proves it started nothing.</summary>
    protected Task<BackupRejection> SendRejectedAsync(StartBackupRequestDto? dto)
    {
        return SendRejectedAsync(() => BackupRejection.FromCallAsync(() => _backupApi.StartBackupAsync(dto, TestContext.Current.CancellationToken)));
    }

    /// <summary>
    /// Sends a verbatim JSON body, with a JSON content type, that is expected to be refused and proves it
    /// started nothing.
    /// </summary>
    protected Task<BackupRejection> SendRejectedRawAsync(string rawJson)
    {
        return SendRejectedAsync(async () => await BackupRejection.FromResponseAsync(
            await _backup.PostRawAsync(StartBackupPath, rawJson, TestContext.Current.CancellationToken)));
    }

    private async Task<BackupRejection> SendRejectedAsync(Func<Task<BackupRejection>> send)
    {
        // Arrange
        var before = await SnapshotBackupsAsync();
        before.Job.Should().BeNull();
        before.HistoryIds.Should().BeEmpty();
        before.Counts.Should().BeEquivalentTo(new BackupsCountResultDto(0, 0));

        // Act
        var rejection = await send();

        // Assert - the side effect is checked before the status is returned to the caller
        (await SnapshotBackupsAsync()).Should().BeEquivalentTo(before);

        return rejection;
    }
}
