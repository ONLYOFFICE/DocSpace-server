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
/// POST /api/2.0/migration/finish with <c>isSendWelcomeEmail</c> on a portal whose import never reached the user step
/// answers 404, as the log of such a portal does, and leaves the job in place. Without a job the controller raised a
/// plain exception, and with a job that imported no users it read a list that was never built: both came back as 500.
/// </summary>
[Trait("Category", "Migration")]
public class FinishMigrationWelcomeEmailTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task FinishMigration_WelcomeEmailWithoutJob_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _migrationApi.FinishMigrationAsync(new FinishDto(true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task FinishMigration_WelcomeEmailAfterFailedJob_Returns404AndKeepsJob()
    {
        // Arrange - with no backup uploaded, the job fails before it imports anybody
        await _migrationApi.UploadAndInitializeMigrationAsync("TotallyNotARealMigrator", TestContext.Current.CancellationToken);
        var before = await WaitForStatus(s => s?.IsCompleted == true);
        before.Should().NotBeNull();
        before!.IsCompleted.Should().BeTrue();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _migrationApi.FinishMigrationAsync(new FinishDto(true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        (await _migrationApi.GetMigrationStatusAsync(TestContext.Current.CancellationToken)).Response
            .Should().NotBeNull("a refused finish queues no clean-up");
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
