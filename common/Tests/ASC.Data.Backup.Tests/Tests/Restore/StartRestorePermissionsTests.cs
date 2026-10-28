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
/// POST /api/2.0/backup/startrestore: who may start a restore, and that authorization comes before the
/// validation of the body. Every request goes through the restore guard of the base class, so none of
/// them can reach the restore queue.
/// </summary>
public class StartRestorePermissionsTests(
    AspireAppFixture fixture)
    : BackupPermissionsTests(fixture)
{
    // Never a GUID and never a path: the restore guard refuses anything that could queue a restore.
    private const string NotAGuid = "not-a-guid";

    private static StartBackupRestoreRequestDto Request()
    {
        return new StartBackupRestoreRequestDto(backupId: NotAGuid, storageType: BackupStorageType.DataStore);
    }

    [Fact]
    public async Task StartRestore_AnonymousWithoutBody_ReturnsUnauthorized()
    {
        // Arrange
        await _backupClient.Authenticate(null);

        // Act: a missing body, which the typed client cannot send with a JSON content type
        using var response = await StartRawRestoreThatCannotQueueAsync(null);

        // Assert
        ((int)response.StatusCode).Should().Be(401);
    }

    [Fact]
    public async Task StartRestore_AnonymousWithBackupIdThatIsNotAGuid_ReturnsUnauthorized()
    {
        // Act & Assert
        await ExpectUnauthorizedAsync(() => StartRestoreThatCannotQueueAsync(new StartBackupRestoreRequestDto(backupId: NotAGuid)));
    }

    /// <remarks>
    /// The TypeScript suite expects 402 here and marks it as failing on a local instance. A standalone
    /// installation does not require a paid plan, so the owner is refused for the id (it is not a backup),
    /// never for the plan and never for the permissions.
    /// </remarks>
    [Fact]
    public async Task StartRestore_OwnerOnStandalone_IsNotRefusedForThePlan()
    {
        // Act
        var status = 200;

        try
        {
            await StartRestoreThatCannotQueueAsync(Request());
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
    public async Task StartRestore_NonAdmin_ReturnsForbiddenAndStartsNothing(EmployeeType employeeType)
    {
        // Arrange
        await AuthenticateAsMemberAsync(employeeType);

        // Act
        await ExpectForbiddenAsync(() => StartRestoreThatCannotQueueAsync(Request()));

        // Assert
        await _backupClient.Authenticate(Owner);
        await ExpectRestoreNotStartedAsync();
    }
}
