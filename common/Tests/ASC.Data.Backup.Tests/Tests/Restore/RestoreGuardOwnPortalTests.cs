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

namespace ASC.Data.Backup.Tests.Tests.Restore;

/// <summary>
/// <see cref="RestoreGuard.SendOwnPortalRestoreAsync{T}"/> lets through only a restore of the calling portal
/// from a backup of its own history: a server-wide restore, a request that names a storage and a backup id
/// that is not in that history never reach the send delegate. It needs no portal and sends nothing.
/// </summary>
public class RestoreGuardOwnPortalTests
{
    private const string OwnBackup = "0f8fad5b-d9cb-469f-a165-70867728950e";
    private static readonly string[] _ownHistory = [OwnBackup];

    public static TheoryData<string, StartBackupRestoreRequestDto> Refused => new()
    {
        { "a server-wide restore", new StartBackupRestoreRequestDto(backupId: OwnBackup, dump: true) },
        { "a storage named", new StartBackupRestoreRequestDto(backupId: OwnBackup, storageParams: [new ItemKeyValuePairObjectObject("filePath", "x")]) },
        { "a backup of another history", new StartBackupRestoreRequestDto(backupId: "7c9e6679-7425-40de-944b-e07fc1f90ae7") },
        { "a backup id that is not a GUID", new StartBackupRestoreRequestDto(backupId: "backup") }
    };

    [Theory]
    [MemberData(nameof(Refused))]
    public async Task SendOwnPortalRestoreAsync_RefusedRequest_NeverReachesTheSendDelegate(string why, StartBackupRestoreRequestDto dto)
    {
        // Arrange
        var sent = false;

        // Act
        var act = () => RestoreGuard.SendOwnPortalRestoreAsync(dto, _ownHistory, () =>
        {
            sent = true;

            return Task.FromResult(0);
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>(why);
        sent.Should().BeFalse(why);
    }

    [Fact]
    public async Task SendOwnPortalRestoreAsync_BackupOfTheOwnHistory_ReachesTheSendDelegateOnce()
    {
        // Arrange
        var sent = 0;
        var dto = new StartBackupRestoreRequestDto(backupId: OwnBackup.ToUpperInvariant());

        // Act
        await RestoreGuard.SendOwnPortalRestoreAsync(dto, _ownHistory, () => Task.FromResult(++sent));

        // Assert
        sent.Should().Be(1);
    }
}
