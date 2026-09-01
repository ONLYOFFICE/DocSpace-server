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

namespace ASC.Web.Api.Tests.Tests._07_Migration;

/// <summary>
/// GET /api/2.0/migration/list, GET /api/2.0/migration/status and GET /api/2.0/migration/logs
/// on a fresh portal where nothing has ever been uploaded and no migration has ever run.
///
/// POST /api/2.0/migration/init/{migratorName} (uploads a real migration archive), POST
/// /api/2.0/migration/migrate, POST /api/2.0/migration/finish and POST /api/2.0/migration/cancel
/// all depend on a migration that has actually been initialized by a background worker, which
/// this suite cannot produce, so they are out of scope. POST /api/2.0/migration/clear is
/// deterministic on a fresh portal (there is nothing to clear) but has no observable response to
/// assert beyond "did not throw", which the SDK's <c>Task</c> return already covers with no need
/// for a dedicated test.
/// </summary>
[Trait("Category", "Migration")]
public class MigrationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task ListMigrations_Owner_ReturnsAvailableMigrations()
    {
        // Act
        var migrations = await _migrationApi.ListMigrationsAsync(TestContext.Current.CancellationToken);

        // Assert
        migrations.StatusCode.Should().Be(200);
        migrations.Response.Should().Contain(["Workspace", "Nextcloud", "GoogleWorkspace"]);
        migrations.Count.Should().Be(migrations.Response.Count);
        migrations.Links.Should().NotBeNullOrEmpty();
        migrations.Links![0].Action.Should().Be("GET");
    }

    [Fact]
    public async Task ListMigrations_DocSpaceAdmin_ReturnsAvailableMigrations()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var migrations = await _migrationApi.ListMigrationsAsync(TestContext.Current.CancellationToken);

        // Assert
        migrations.StatusCode.Should().Be(200);
        migrations.Response.Should().Contain(["Workspace", "Nextcloud", "GoogleWorkspace"]);
        migrations.Count.Should().Be(migrations.Response.Count);
        migrations.Links.Should().NotBeNullOrEmpty();
        migrations.Links![0].Action.Should().Be("GET");
    }

    [Fact]
    public async Task GetMigrationStatus_Owner_ReturnsStatusWhenNothingIsRunning()
    {
        // Act
        var status = await _migrationApi.GetMigrationStatusAsync(TestContext.Current.CancellationToken);

        // Assert
        status.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetMigrationStatus_DocSpaceAdmin_ReturnsStatusWhenNothingIsRunning()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var status = await _migrationApi.GetMigrationStatusAsync(TestContext.Current.CancellationToken);

        // Assert
        status.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// A fresh portal has never queued a migration task, so <c>GetMigrationLogs</c> should report
    /// that no migration is in progress with 404, per <c>MigrationController.GetMigrationLogs</c>.
    /// </summary>
    [Trait("Bug", "81653")]
    [Fact]
    public async Task GetMigrationLogs_Owner_ThrowsNotFoundWhenNoMigrationHasRun()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _migrationApi.GetMigrationLogsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("No migration is in progress");
    }

    [Trait("Bug", "81653")]
    [Fact]
    public async Task GetMigrationLogs_DocSpaceAdmin_ThrowsNotFoundWhenNoMigrationHasRun()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _migrationApi.GetMigrationLogsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("No migration is in progress");
    }
}
