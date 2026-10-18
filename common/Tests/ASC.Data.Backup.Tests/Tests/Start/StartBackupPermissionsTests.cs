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
/// POST /api/2.0/backup/startbackup: who may start a backup, and that authorization comes before the
/// validation of the body. The test stack runs as a standalone installation, which does not enforce a
/// paid plan, so the owner is never refused with 402 here.
/// </summary>
public class StartBackupPermissionsTests(
    AspireAppFixture fixture)
    : BackupPermissionsTests(fixture)
{
    private const string MissingFolderId = "99999999";

    // Not a defined BackupStorageType: well-formed JSON of the right type that the controller refuses
    // only after the permission check.
    private const BackupStorageType UndefinedStorageType = (BackupStorageType)99;

    private static StartBackupRequestDto Documents(string folderId)
    {
        return new StartBackupRequestDto
        {
            StorageType = BackupStorageType.Documents,
            StorageParams = [new ItemKeyValuePairObjectObject("folderId", folderId)]
        };
    }

    [Fact]
    public async Task StartBackup_Anonymous_ReturnsUnauthorized()
    {
        // Act & Assert
        await ExpectUnauthorizedAsync(() => _backupApi.StartBackupAsync(
            new StartBackupRequestDto { StorageType = BackupStorageType.DataStore },
            TestContext.Current.CancellationToken));
    }

    /// <remarks>
    /// The TypeScript suite expects 402 here and marks it as failing on a local instance. A standalone
    /// installation does not require a paid plan for a backup, so the owner's backup is queued and runs.
    /// </remarks>
    [Fact]
    public async Task StartBackup_OwnerOnStandalone_DoesNotRequirePaidPlan()
    {
        // Act
        var taskId = await RunBackupAsync();

        // Assert
        taskId.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task StartBackup_NonAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        // Arrange
        await AuthenticateAsMemberAsync(employeeType);

        // Act & Assert
        await ExpectForbiddenAsync(() => _backupApi.StartBackupAsync(
            new StartBackupRequestDto { StorageType = BackupStorageType.DataStore },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartBackup_DocumentsFolderDoesNotExist_ReturnsNotFound()
    {
        // Act & Assert
        await ExpectStatusAsync(
            () => _backupApi.StartBackupAsync(Documents(MissingFolderId), TestContext.Current.CancellationToken),
            404);
    }

    [Fact]
    [Trait("Bug", "79523")]
    public async Task StartBackup_FolderIdIsNotANumber_ReturnsBadRequest()
    {
        // Act & Assert
        await ExpectStatusAsync(
            () => _backupApi.StartBackupAsync(Documents("invalid_value"), TestContext.Current.CancellationToken),
            400);
    }

    [Fact]
    public async Task StartBackup_ThirdPartyStorageWithARoomFolder_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var room = await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: "Autotest Room", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        var dto = new StartBackupRequestDto
        {
            StorageType = BackupStorageType.ThridpartyDocuments,
            StorageParams = [new ItemKeyValuePairObjectObject("folderId", room.Response.Id.ToString())]
        };

        // Act & Assert
        await ExpectStatusAsync(() => _backupApi.StartBackupAsync(dto, TestContext.Current.CancellationToken), 400);
    }

    [Fact]
    public async Task StartBackup_AnonymousWithUndefinedStorageType_ReturnsUnauthorized()
    {
        // Act & Assert
        await ExpectUnauthorizedAsync(() => _backupApi.StartBackupAsync(
            new StartBackupRequestDto { StorageType = UndefinedStorageType },
            TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task StartBackup_NonAdminWithUndefinedStorageType_ReturnsForbiddenAndStartsNothing(EmployeeType employeeType)
    {
        // Arrange
        await AuthenticateAsMemberAsync(employeeType);

        // Act
        await ExpectForbiddenAsync(() => _backupApi.StartBackupAsync(
            new StartBackupRequestDto { StorageType = UndefinedStorageType },
            TestContext.Current.CancellationToken));

        // Assert
        await _backupClient.Authenticate(Owner);
        var snapshot = await SnapshotBackupsAsync();

        snapshot.Job.Should().BeNull();
        snapshot.HistoryIds.Should().BeEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task StartBackup_NonAdminWithMissingFolder_ReturnsForbiddenNotNotFound(EmployeeType employeeType)
    {
        // Arrange
        await AuthenticateAsMemberAsync(employeeType);

        // Act & Assert
        await ExpectForbiddenAsync(() => _backupApi.StartBackupAsync(
            Documents(MissingFolderId),
            TestContext.Current.CancellationToken));
    }
}
