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


namespace ASC.Data.Backup.Tests.Tests.Counts;

/// <summary>
/// GET /api/2.0/backup/getbackupscountbypaid: the free and paid backup counts of a period, and the same
/// period validation on getbackupscount. The test stack runs as a standalone installation, where the free
/// monthly allowance is not enforced and no backup is charged to a wallet, so every backup counts as free.
/// </summary>
public class BackupsCountByPaidTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    private static DateTime Utc(int year, int month, int day)
    {
        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime? UtcOrNull(string? date)
    {
        return date is null ? null : DateTime.SpecifyKind(DateTime.Parse(date), DateTimeKind.Utc);
    }

    private async Task<BackupsCountResultDto> CountsAsync(DateTime? from = null, DateTime? to = null, bool? paid = null)
    {
        var counts = await _backupApi.GetBackupsCountsAsync(from, to, paid, TestContext.Current.CancellationToken);

        counts.StatusCode.Should().Be(200);

        return counts.Response;
    }

    [Fact]
    public async Task GetBackupsCounts_PortalWithoutBackups_ReturnsZeros()
    {
        // Act
        var counts = await CountsAsync();

        // Assert
        counts.Free.Should().Be(0);
        counts.Paid.Should().Be(0);
    }

    [Fact]
    public async Task GetBackupsCounts_CompletedBackup_CountsItAsFree()
    {
        // Arrange
        var taskId = await RunBackupAsync();

        // Act
        var counts = await CountsAsync();

        // Assert
        counts.Free.Should().Be(1);
        counts.Paid.Should().Be(0);

        // The `paid` query parameter is ignored: both figures are always returned.
        var paidRequested = await CountsAsync(paid: true);
        paidRequested.Should().BeEquivalentTo(counts);

        // getbackupscount reports the same figures one kind at a time.
        var free = await _backupApi.GetBackupsCountAsync(cancellationToken: TestContext.Current.CancellationToken);
        var paid = await _backupApi.GetBackupsCountAsync(paid: true, cancellationToken: TestContext.Current.CancellationToken);
        free.Response.Should().Be(1);
        paid.Response.Should().Be(0);

        // The free count agrees with the history.
        var historyIds = await ReadHistoryIdsAsync();
        historyIds.Should().ContainSingle().Which.Should().BeEquivalentTo(taskId);
        counts.Free.Should().Be(historyIds.Count);

        // Repeated reads return the same figures.
        (await CountsAsync()).Should().BeEquivalentTo(counts);

        // The generated model ignores a field it does not know, so the keys of the answer are read raw.
        using var raw = await _backup.GetAsync("api/2.0/backup/getbackupscountbypaid", TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await raw.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        document.RootElement.GetProperty("response").EnumerateObject().Select(property => property.Name).Order()
            .Should().Equal("free", "paid");
    }

    [Fact]
    public async Task GetBackupsCounts_PeriodAroundBackup_CountsOnlyWhenItContainsTheBackup()
    {
        // Arrange
        await RunBackupAsync();

        // Act
        var containing = await CountsAsync(Utc(2000, 1, 1), Utc(2099, 12, 31));
        var past = await CountsAsync(Utc(2000, 1, 1), Utc(2000, 12, 31));
        var future = await CountsAsync(Utc(2098, 1, 1), Utc(2098, 12, 31));

        // Assert
        containing.Free.Should().Be(1);
        containing.Paid.Should().Be(0);
        past.Should().BeEquivalentTo(new BackupsCountResultDto(0, 0));
        future.Should().BeEquivalentTo(new BackupsCountResultDto(0, 0));
    }

    [Fact]
    public async Task GetBackupsCounts_ValidBounds_AreAccepted()
    {
        // from equal to to
        var sameDay = await CountsAsync(Utc(2020, 5, 5), Utc(2020, 5, 5));
        sameDay.Should().BeEquivalentTo(new BackupsCountResultDto(0, 0));

        // full UTC timestamps
        var timestamps = await CountsAsync(
            new DateTime(2020, 5, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2020, 5, 5, 23, 59, 59, DateTimeKind.Utc));
        timestamps.Should().BeEquivalentTo(new BackupsCountResultDto(0, 0));

        // only `from`: `to` defaults to the moment of the call
        await CountsAsync(Utc(2020, 5, 5));
    }

    [Theory]
    [InlineData("2030-01-02", "2030-01-01")]
    [InlineData(null, "2000-01-01")] // the default `from` is the start of the current month, later than this `to`
    public async Task GetBackupsCounts_StartAfterEnd_ReturnsBadRequestOnBothEndpoints(string? from, string? to)
    {
        // Act
        var countsByPaid = await Assert.ThrowsAsync<ApiException>(
            async () => await _backupApi.GetBackupsCountsAsync(UtcOrNull(from), UtcOrNull(to), cancellationToken: TestContext.Current.CancellationToken));
        var count = await Assert.ThrowsAsync<ApiException>(
            async () => await _backupApi.GetBackupsCountAsync(UtcOrNull(from), UtcOrNull(to), cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        countsByPaid.ErrorCode.Should().Be(400);
        count.ErrorCode.Should().Be(400);
    }

    [Theory]
    [InlineData("getbackupscountbypaid", "from=not-a-date")]
    [InlineData("getbackupscountbypaid", "to=not-a-date")]
    [InlineData("getbackupscount", "from=not-a-date")]
    [InlineData("getbackupscount", "to=not-a-date")]
    public async Task GetBackupsCounts_BoundThatIsNotADate_ReturnsBadRequest(string endpoint, string query)
    {
        // Act (raw: the typed client sends a DateTime, so a value that is not a date cannot be expressed)
        using var response = await _backup.GetAsync($"api/2.0/backup/{endpoint}?{query}", TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(400);
    }

    [Fact]
    public async Task GetBackupsCounts_DeletedBackups_AreStillCounted()
    {
        // Arrange
        var firstId = await RunBackupAsync();
        await RunBackupAsync();

        var counts = await CountsAsync();
        counts.Free.Should().Be(2);
        counts.Paid.Should().Be(0);

        // Act: deleting one backup shrinks the history but not the counts
        var deleted = await _backupApi.DeleteBackupAsync(Guid.Parse(firstId), TestContext.Current.CancellationToken);

        // Assert
        deleted.Response.Should().BeTrue();

        var historyIds = await ReadHistoryIdsAsync();
        historyIds.Should().NotContain(id => string.Equals(id, firstId, StringComparison.OrdinalIgnoreCase));
        historyIds.Should().HaveCount(1);
        (await CountsAsync()).Should().BeEquivalentTo(new BackupsCountResultDto(2, 0));

        // Act: clearing the history empties it but leaves the counts
        await _backupApi.DeleteBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        (await ReadHistoryIdsAsync()).Should().BeEmpty();
        (await CountsAsync()).Should().BeEquivalentTo(new BackupsCountResultDto(2, 0));
    }
}
