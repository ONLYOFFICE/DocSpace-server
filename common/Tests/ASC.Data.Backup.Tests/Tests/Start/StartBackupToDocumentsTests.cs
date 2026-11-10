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
/// POST /api/2.0/backup/startbackup into the Documents module: which folders are refused as a target, and
/// what a backup sent without a storage type writes. The section roots are folders of the portal, so the
/// folder lookup finds them, and the refusal comes from the target check, not from a missing folder.
/// </summary>
public class StartBackupToDocumentsTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    private static StartBackupRequestDto Documents(int folderId, BackupStorageType? storageType = BackupStorageType.Documents)
    {
        return new StartBackupRequestDto
        {
            StorageType = storageType,
            StorageParams = [new ItemKeyValuePairObjectObject("folderId", folderId.ToString())]
        };
    }

    [Theory]
    [InlineData(FolderType.VirtualRooms)]
    [InlineData(FolderType.Archive)]
    public async Task StartBackup_IntoTheRootOfASection_ReturnsForbiddenAndQueuesNothing(FolderType section)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await ActAsAsync(BackupCaller.Owner);

        var roots = (await _foldersApi.GetRootFoldersAsync(cancellationToken: ct)).Response;
        var root = roots.Select(content => content.Current).FirstOrDefault(folder => folder.RootFolderType == section);
        root.Should().NotBeNull($"the owner sees the {section} section among [{string.Join(", ", roots.Select(content => content.Current.RootFolderType))}]");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _backupApi.StartBackupAsync(Documents(root!.Id), ct));

        // Assert
        exception.ErrorCode.Should().Be(403);

        var snapshot = await SnapshotBackupsAsync();
        snapshot.Job.Should().BeNull();
        snapshot.HistoryIds.Should().BeEmpty();
    }

    [Fact]
    public async Task StartBackup_WithoutStorageType_WritesADocumentsArchiveWithoutALinkIntoTheRoom()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await ActAsAsync(BackupCaller.Owner);
        var roomId = await CreateBackupRoomAsync("Autotest Backup Default Storage");

        // Act - storageType is left out of the body (EmitDefaultValue = false on the SDK model)
        var job = await StartBackupAsync(Documents(roomId, storageType: null));
        var finished = await WaitForBackupJobEndAsync(job.TaskId);

        // Assert - the job ends without an error, and a Documents archive has no download link
        finished.Status.Should().Be(DistributedTaskStatus.Completed);
        finished.Error.Should().BeNullOrEmpty();
        finished.Link.Should().BeEmpty();

        var record = (await _backupApi.GetBackupHistoryAsync(cancellationToken: ct)).Response
            .Single(item => item.Id == Guid.Parse(job.TaskId));
        record.StorageType.Should().Be(BackupStorageType.Documents);

        var titles = (await _foldersApi.GetFolderByFolderIdAsync(roomId, cancellationToken: ct)).Response.Files
            .OfType<FileDto>()
            .Select(file => file.Title);
        titles.Should().Contain(record.FileName);
    }
}
