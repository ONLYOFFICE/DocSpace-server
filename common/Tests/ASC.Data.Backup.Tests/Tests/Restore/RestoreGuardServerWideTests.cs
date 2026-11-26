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
/// <see cref="RestoreGuard.SendServerWideRestoreAsync{T}"/> refuses every request while
/// <see cref="RestoreGuard.ServerWideRestoreVariable"/> is not 1, the normal state of a run of the suite, and with
/// the variable set it still refuses what is not a dump of the server's own history. It needs no portal and sends
/// nothing.
/// </summary>
public class RestoreGuardServerWideTests
{
    private const string DumpBackup = "0f8fad5b-d9cb-469f-a165-70867728950e";
    private static readonly string[] _dumpHistory = [DumpBackup];

    public static TheoryData<string, StartBackupRestoreRequestDto> Refused => new()
    {
        { "not a dump", new StartBackupRestoreRequestDto(backupId: DumpBackup) },
        { "a storage named", new StartBackupRestoreRequestDto(backupId: DumpBackup, dump: true, storageParams: [new ItemKeyValuePairObjectObject("filePath", "x")]) },
        { "a backup of another history", new StartBackupRestoreRequestDto(backupId: "7c9e6679-7425-40de-944b-e07fc1f90ae7", dump: true) },
        { "a backup id that is not a GUID", new StartBackupRestoreRequestDto(backupId: "backup", dump: true) }
    };

    [Theory]
    [MemberData(nameof(Refused))]
    public async Task SendServerWideRestoreAsync_WithTheVariable_StillRefusesWhatIsNotADumpOfTheHistory(string why, StartBackupRestoreRequestDto dto)
    {
        // Arrange
        var sent = false;

        // Act
        var act = () => RestoreGuard.SendServerWideRestoreAsync(dto, _dumpHistory, allowed: true, () =>
        {
            sent = true;

            return Task.FromResult(0);
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>(why);
        sent.Should().BeFalse(why);
    }

    [Fact]
    public async Task SendServerWideRestoreAsync_WithTheVariableAndADumpOfTheHistory_ReachesTheSendDelegateOnce()
    {
        // Arrange
        var sent = 0;
        var dto = new StartBackupRestoreRequestDto(backupId: DumpBackup, dump: true);

        // Act
        await RestoreGuard.SendServerWideRestoreAsync(dto, _dumpHistory, allowed: true, () => Task.FromResult(++sent));

        // Assert
        sent.Should().Be(1);
    }

    [Fact]
    public async Task SendServerWideRestoreAsync_WithoutTheVariable_NeverReachesTheSendDelegate()
    {
        Assert.SkipWhen(RestoreGuard.ServerWideRestoreAllowed, "the variable is set for a server-wide run");

        // Arrange
        var sent = false;
        var dto = new StartBackupRestoreRequestDto(backupId: DumpBackup, dump: true);

        // Act
        var act = () => RestoreGuard.SendServerWideRestoreAsync(dto, [DumpBackup], () =>
        {
            sent = true;

            return Task.FromResult(0);
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{RestoreGuard.ServerWideRestoreVariable}*");
        sent.Should().BeFalse();
    }
}
