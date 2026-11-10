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


namespace ASC.Data.Backup.Tests.Tests.History;

/// <summary>
/// GET /api/2.0/backup/getbackuphistory for a Documents archive that is no longer where the backup put
/// it. The history checks each archive before listing it: a file in the Trash counts as gone, and the
/// record of a gone archive is removed, not only hidden, so it does not come back with the file.
/// </summary>
public class BackupHistoryMissingArchiveTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    private static readonly TimeSpan _moveTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task GetBackupHistory_ArchiveMovedToTrash_DropsTheRecordForGood()
    {
        // Arrange - a Documents backup in a room, and its archive file in that room
        var ct = TestContext.Current.CancellationToken;
        await ActAsAsync(BackupCaller.Owner);
        var roomId = await CreateBackupRoomAsync("Autotest Backup Trash");

        var taskId = await StartBackupAndWaitAsync(new StartBackupRequestDto
        {
            StorageType = BackupStorageType.Documents,
            StorageParams = [new ItemKeyValuePairObjectObject("folderId", roomId.ToString())]
        });

        var record = (await _backupApi.GetBackupHistoryAsync(cancellationToken: ct)).Response
            .Single(item => item.Id == Guid.Parse(taskId));
        var archive = await WaitForFileInRoomAsync(roomId, record.FileName, present: true);

        // Act - the archive goes to the Trash, the history is read, then the file is put back
        await _filesApi.DeleteFileAsync(archive!.Id, new DeleteFileRequest { Immediately = false }, true, ct);
        (await WaitForFileInRoomAsync(roomId, record.FileName, present: false)).Should().BeNull("the archive was moved to the Trash");

        var afterTrash = (await _backupApi.GetBackupHistoryAsync(cancellationToken: ct)).Response ?? [];

        await _operationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            ReturnSingleOperation = true,
            FileIds = [new BatchRequestDtoAllOfFileIds(archive.Id)],
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(roomId),
            DeleteAfter = false
        }, ct);
        (await WaitForFileInRoomAsync(roomId, record.FileName, present: true)).Should().NotBeNull("the archive was moved back from the Trash");

        var afterRestore = (await _backupApi.GetBackupHistoryAsync(cancellationToken: ct)).Response ?? [];

        // Assert
        afterTrash.Select(item => item.Id).Should().NotContain(record.Id, "an archive in the Trash counts as gone");
        afterRestore.Select(item => item.Id).Should().NotContain(record.Id, "the record of a gone archive was removed, not only hidden");
    }

    // Polls the room until the file is there (present) or no longer there, and returns it or null.
    private async Task<FileDto?> WaitForFileInRoomAsync(int roomId, string title, bool present)
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow + _moveTimeout;

        while (true)
        {
            var file = (await _foldersApi.GetFolderByFolderIdAsync(roomId, cancellationToken: ct)).Response.Files
                .OfType<FileDto>()
                .FirstOrDefault(item => item.Title == title);

            if ((file is not null) == present || DateTime.UtcNow >= deadline)
            {
                return file;
            }

            await Task.Delay(500, ct);
        }
    }
}
