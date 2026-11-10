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


namespace ASC.Data.Backup.Tests.Tests.Delete;

/// <summary>
/// DELETE /api/2.0/backup/deletebackup: who may delete a backup, and that authorization comes before
/// the lookup of the id, so a refused caller learns nothing about whether it exists.
/// </summary>
public class DeleteBackupPermissionsTests(
    AspireAppFixture fixture)
    : BackupPermissionsTests(fixture)
{
    private static readonly Guid _unknownId = new("11111111-2222-3333-4444-555555555555");

    [Fact]
    public async Task DeleteBackup_Anonymous_ReturnsUnauthorized()
    {
        // Act & Assert
        await ExpectUnauthorizedAsync(() => _backupApi.DeleteBackupAsync(_unknownId, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// BUG 80605: an ID with no backup record used to fail with 500 - the service read the tenant of a
    /// record it had not found. It now answers 404 before anything is deleted.
    /// </summary>
    [Fact]
    [Trait("Bug", "80605")]
    public async Task DeleteBackup_UnknownId_ReturnsNotFound()
    {
        // Act & Assert
        await ExpectStatusAsync(() => _backupApi.DeleteBackupAsync(Guid.Empty, TestContext.Current.CancellationToken), 404);
    }

    /// <remarks>
    /// BUG 80608 in the TypeScript suite expects 402 here and marks it as failing on a local instance. A
    /// standalone installation does not require a paid plan, so the owner is answered for the id (404, nothing
    /// to delete), never for the plan and never for the permissions.
    /// </remarks>
    [Fact]
    [Trait("Bug", "80608")]
    public async Task DeleteBackup_OwnerOnStandalone_IsNotRefusedForThePlan()
    {
        // Act
        var status = 200;

        try
        {
            await _backupApi.DeleteBackupAsync(_unknownId, TestContext.Current.CancellationToken);
        }
        catch (ApiException e)
        {
            status = e.ErrorCode;
        }

        // Assert
        status.Should().NotBe(402).And.NotBe(403);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task DeleteBackup_NonAdmin_ReturnsForbiddenAndDeletesNothing(EmployeeType employeeType)
    {
        // Arrange
        await RunBackupAsync();
        var before = await SnapshotBackupsAsync();

        await AuthenticateAsMemberAsync(employeeType);

        // Act
        await ExpectForbiddenAsync(() => _backupApi.DeleteBackupAsync(_unknownId, TestContext.Current.CancellationToken));

        // Assert: the job in the queue is left out, because a finished job may drop out of it between the two reads
        await _backupClient.Authenticate(Owner);
        var after = await SnapshotBackupsAsync();

        after.Should().BeEquivalentTo(before, options => options.Excluding(snapshot => snapshot.Job));
    }
}
