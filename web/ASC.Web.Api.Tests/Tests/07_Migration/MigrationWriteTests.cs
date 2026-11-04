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
/// The write endpoints of the migration API on a portal that has never uploaded a backup:
/// cancel, clear, finish, migrate and init are idempotent no-ops or queue a job that fails
/// cleanly, so none of them needs a real archive. A parse failure is only visible through
/// GET /api/2.0/migration/status, which is written by a background worker, so it is polled.
/// </summary>
[Trait("Category", "Migration")]
public class MigrationWriteTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CancelMigration_NothingRunning_IsIdempotent()
    {
        // Act
        await _migrationApi.CancelMigrationAsync(TestContext.Current.CancellationToken);
        await _migrationApi.CancelMigrationAsync(TestContext.Current.CancellationToken);

        // Assert
        (await _migrationApi.GetMigrationStatusAsync(TestContext.Current.CancellationToken)).Response.Should().BeNull();
    }

    [Fact]
    public async Task ClearMigration_NothingRunning_IsIdempotent()
    {
        // Act
        await _migrationApi.ClearMigrationAsync(TestContext.Current.CancellationToken);
        await _migrationApi.ClearMigrationAsync(TestContext.Current.CancellationToken);

        // Assert
        (await _migrationApi.GetMigrationStatusAsync(TestContext.Current.CancellationToken)).Response.Should().BeNull();
    }

    /// <summary>
    /// A body-less request is refused by the model binder; the typed client cannot send it
    /// (a null DTO becomes no body and no content type), so this one goes over raw HTTP.
    /// </summary>
    [Fact]
    public async Task FinishMigration_MissingBody_ThrowsBadRequest()
    {
        // Arrange
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        // Act
        using var response = await _webApiClient.PostAsync("api/2.0/migration/finish", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FinishMigration_NothingRunningWithoutWelcomeEmail_Succeeds()
    {
        // Act
        var act = async () => await _migrationApi.FinishMigrationAsync(new FinishDto(false), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartMigration_NothingParsed_IsNoOp()
    {
        // Act
        var act = async () => await _migrationApi.StartMigrationAsync(new MigrationApiInfo(), TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Nextcloud")]
    [InlineData("TotallyNotARealMigrator")]
    public async Task UploadAndInitializeMigration_NoBackupUploaded_QueuesAndFailsCleanly(string migratorName)
    {
        // Act
        await _migrationApi.UploadAndInitializeMigrationAsync(migratorName, TestContext.Current.CancellationToken);

        // Assert
        var status = await WaitForStatus(s => s?.IsCompleted == true);
        status.Should().NotBeNull();
        status!.IsCompleted.Should().BeTrue();
        status.Error.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// ClearMigration answers 200 but the finished/failed job keeps being reported by the status
    /// endpoint: the job is never actually cleared.
    /// </summary>
    [Trait("Bug", "84792")]
    [Fact]
    public async Task ClearMigration_FinishedFailedJob_RemovesTheJob()
    {
        // Arrange
        await _migrationApi.UploadAndInitializeMigrationAsync("TotallyNotARealMigrator", TestContext.Current.CancellationToken);
        var before = await WaitForStatus(s => s?.IsCompleted == true);
        before.Should().NotBeNull();

        // Act
        await _migrationApi.ClearMigrationAsync(TestContext.Current.CancellationToken);

        // Assert
        var after = await WaitForStatus(s => s == null);
        after.Should().BeNull();
    }

    /// <summary>
    /// A job that failed before it wrote any log should answer 404 like the no-job case, but
    /// answers 403 with a broken Kestrel response ("Content-Length mismatch").
    /// </summary>
    [Trait("Bug", "84791")]
    [Fact]
    public async Task GetMigrationLogs_JobFailedBeforeWritingLog_ThrowsNotFound()
    {
        // Arrange
        await _migrationApi.UploadAndInitializeMigrationAsync("Nextcloud", TestContext.Current.CancellationToken);
        await WaitForStatus(s => s?.IsCompleted == true);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _migrationApi.GetMigrationLogsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    private async Task<MigrationStatusDto?> WaitForStatus(Func<MigrationStatusDto?, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var status = (await _migrationApi.GetMigrationStatusAsync(TestContext.Current.CancellationToken)).Response;

            if (until(status) || DateTime.UtcNow >= deadline)
            {
                return status;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
