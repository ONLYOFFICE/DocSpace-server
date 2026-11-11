// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Files.Tests.Tests._01_Files.Forms.Versions;

/// <summary>
/// What the forms database does when a step fails or a form outgrows one read of the search index: the submissions
/// are read page by page past the index's window (<see cref="FormFillingReportCreator.ReadPagesAsync{T}"/>), a version
/// whose table cannot be decided still has its submission written
/// (<see cref="FormFillingReportCreator.DecideOrFallBackAsync{T}"/>), and the state of a table that took a rename is
/// saved again on a passing fault before the table lock is let go
/// (<see cref="FormFillingReportCreator.RetryTransientAsync"/>). Pure logic, no portal.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormDbFaultTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(999, 1)]
    [InlineData(1000, 2)]
    [InlineData(2500, 3)]
    [InlineData(12345, 13)]
    public async Task ReadPagesAsync_MoreThanTheIndexWindow_HandsEverySubmissionOnce(int count, int expectedReads)
    {
        var ids = Enumerable.Range(1, count).Select(i => i * 3).ToList();
        var reads = 0;
        var seen = new List<int>();

        var success = await FormFillingReportCreator.ReadPagesAsync(
            afterId =>
            {
                reads++;
                IReadOnlyCollection<int> page = ids.Where(id => afterId == null || id > afterId).Take(1000).ToList();
                return Task.FromResult((true, page));
            },
            id => id,
            1000,
            page =>
            {
                seen.AddRange(page);
                return Task.CompletedTask;
            });

        success.Should().BeTrue();
        seen.Should().Equal(ids);
        reads.Should().Be(expectedReads);
    }

    [Fact]
    public async Task ReadPagesAsync_PageFails_ReportsItAfterTheEarlierPages()
    {
        var ids = Enumerable.Range(1, 2500).ToList();
        var seen = new List<int>();

        var success = await FormFillingReportCreator.ReadPagesAsync(
            afterId =>
            {
                IReadOnlyCollection<int> page = ids.Where(id => afterId == null || id > afterId).Take(1000).ToList();
                return Task.FromResult((afterId is null or < 1000, page));
            },
            id => id,
            1000,
            page =>
            {
                seen.AddRange(page);
                return Task.CompletedTask;
            });

        success.Should().BeFalse();
        seen.Should().Equal(ids.Take(1000));
    }

    [Fact]
    public async Task DecideOrFallBackAsync_DecisionFails_WritesWithTheStoredState()
    {
        var stored = new FormFillingProperties { VersionLinks = [new FormVersionLink { Version = 2, TableVersion = 1 }] };
        Exception? logged = null;

        var state = await FormFillingReportCreator.DecideOrFallBackAsync(
            () => throw new DistributedLockException(LockStatus.NotAcquired, "forms_db_version", 30000),
            () => Task.FromResult(stored),
            e => logged = e);

        state.Should().BeSameAs(stored);
        logged.Should().BeOfType<DistributedLockException>();
    }

    [Fact]
    public async Task RetryTransientAsync_LockOfTheSubmissionsBusyTwice_SavesOnTheThirdTry()
    {
        var calls = 0;

        await FormFillingReportCreator.RetryTransientAsync(() =>
        {
            calls++;
            return calls < 3
                ? throw new DistributedLockException(LockStatus.NotAcquired, "fillform", 30000)
                : Task.CompletedTask;
        }, FormFillingReportCreator.VersionStateSaveAttempts, TimeSpan.Zero);

        calls.Should().Be(3);
    }

    [Fact]
    public async Task RetryTransientAsync_LockNeverFree_GivesUpAfterTheAttempts()
    {
        var calls = 0;

        var act = () => FormFillingReportCreator.RetryTransientAsync(() =>
        {
            calls++;
            throw new DistributedLockException(LockStatus.NotAcquired, "fillform", 30000);
        }, FormFillingReportCreator.VersionStateSaveAttempts, TimeSpan.Zero);

        await act.Should().ThrowAsync<DistributedLockException>();
        calls.Should().Be(FormFillingReportCreator.VersionStateSaveAttempts);
    }

    [Fact]
    public async Task RetryTransientAsync_FaultThatWouldRepeat_IsNotTriedAgain()
    {
        var calls = 0;

        var act = () => FormFillingReportCreator.RetryTransientAsync(() =>
        {
            calls++;
            throw new InvalidOperationException("the form has no properties");
        }, FormFillingReportCreator.VersionStateSaveAttempts, TimeSpan.Zero);

        await act.Should().ThrowAsync<InvalidOperationException>();
        calls.Should().Be(1);
    }
}
