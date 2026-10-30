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

namespace ASC.Files.Tests.Tests._06_Operations.Statuses;

/// <summary>
/// <c>GET /api/2.0/files/fileops</c> while operations run: the record of a started operation is Created at
/// progress 0, moves through Running with progress that only grows and ends Completed at 100; a finished record
/// is handed out once (by any read, or by the answer to the next start of the same group) and then dropped;
/// <c>deleteAfter=true</c> never hands it out. Kinds listed together and account isolation are in
/// <see cref="OperationStatusListingTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class OperationStatusLifecycleTests(
    AspireAppFixture fixture)
    : OperationFlowTestBase(fixture)
{
    [Fact]
    public async Task GetOperationStatuses_StartedOperation_IsCreatedAtZeroThenCompletedAtHundredWithSameIdAndNoError()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Status Shape.docx", myDocs);
        var room = await CreateCustomRoom("Autotest Status Shape Room");

        // Act
        var started = Started(await StartCopyOfFile(file.Id, room.Id), FileOperationType.Copy);

        // Assert
        started.Id.Should().MatchRegex("^[0-9a-f-]{36}$");
        started.Status.Should().Be(DistributedTaskStatus.Created);
        started.Progress.Should().Be(0);
        started.Processed.Should().Be("0");
        started.Error.Should().BeNullOrEmpty();
        started.Finished.Should().BeFalse();

        var done = await FinishedRecord(started.Id);

        done.Id.Should().Be(started.Id);
        done.Operation.Should().Be(FileOperationType.Copy);
        done.Status.Should().Be(DistributedTaskStatus.Completed);
        done.Progress.Should().Be(100);
        done.Processed.Should().Be("1");
        done.Error.Should().BeNullOrEmpty();
        done.Files.Should().HaveCount(1);
        done.Url.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetOperationStatuses_LongCopy_RunsWithGrowingProgressAndProcessedRepeatedReadsChangeNothingAndEndsAtHundred()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Status Timeline", LongFiles);
        var room = await CreateCustomRoom("Autotest Status Timeline Room");
        var op = Started(await StartCopyOfFolder(folderId, room.Id), FileOperationType.Copy);

        // Act
        var samples = new List<FileOperationDto>();
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (DateTime.UtcNow < deadline)
        {
            // two reads in a row: the second must not differ in identity or lose the record
            var first = (await GetStatuses(op.Id)).Find(o => o.Id == op.Id);

            if (first is null)
            {
                break;
            }

            samples.Add(first);

            if (first.Finished)
            {
                break;
            }

            var second = (await GetStatuses(op.Id)).Find(o => o.Id == op.Id);

            if (second is not null)
            {
                second.Id.Should().Be(first.Id);

                // a finished record is handed out by this read; it must not be lost to the next iteration
                if (second.Finished)
                {
                    samples.Add(second);
                    break;
                }
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        // Assert
        var running = samples.FindAll(s => !s.Finished);
        running.Count.Should().BeGreaterThan(1);
        running.Should().Contain(s => s.Progress > 0 && s.Progress < 100, "an intermediate progress was observed");
        running.Should().OnlyContain(s => s.Id == op.Id && s.Status == DistributedTaskStatus.Running && string.IsNullOrEmpty(s.Error));

        for (var i = 1; i < samples.Count; i++)
        {
            samples[i].Progress.Should().BeGreaterThanOrEqualTo(samples[i - 1].Progress);
            long.Parse(samples[i].Processed, CultureInfo.InvariantCulture)
                .Should().BeGreaterThanOrEqualTo(long.Parse(samples[i - 1].Processed, CultureInfo.InvariantCulture));
        }

        var last = samples[^1];
        last.Finished.Should().BeTrue();
        last.Progress.Should().Be(100);
        last.Status.Should().Be(DistributedTaskStatus.Completed);
        last.Error.Should().BeNullOrEmpty();
        (await CountFiles(room.Id)).Should().Be(LongFiles);
    }

    [Fact]
    public async Task GetOperationStatuses_FinishedRecord_IsHandedOutOnceAndThenDropped()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Status Once.docx", myDocs);
        var room = await CreateCustomRoom("Autotest Status Once Room");
        var op = Started(await StartCopyOfFile(file.Id, room.Id), FileOperationType.Copy);

        // Act
        var handedOut = await FinishedRecord(op.Id);

        // Assert
        handedOut.Finished.Should().BeTrue();
        (await GetStatuses()).Should().BeEmpty();
        (await GetStatuses(op.Id)).Should().BeEmpty();
        (await CountFiles(room.Id)).Should().Be(1);
    }

    [Fact]
    public async Task GetOperationStatuses_DeleteAfterTrue_NeverReportsTheFinishedOperation()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Status DeleteAfter.docx", myDocs);
        var room = await CreateCustomRoom("Autotest Status DeleteAfter Room");

        // Act
        await StartCopyOfFile(file.Id, room.Id, deleteAfter: true);

        // Assert: the work is done when the file is there; the record must never have been kept
        (await WaitForCount(() => CountFiles(room.Id), 1)).Should().Be(1);
        await ExpectNothingRunning();
        (await GetStatuses()).Should().BeEmpty();
    }

    [Fact]
    public async Task GetOperationStatuses_AnswerToStart_AlsoCarriesEarlierFinishedRecordsOnceAndNewOperationComesLast()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var room = await CreateCustomRoom("Autotest Status Handout Room");
        var first = await CreateFile("Autotest Status Handout One.docx", myDocs);
        var second = await CreateFile("Autotest Status Handout Two.docx", myDocs);
        var one = Started(await StartCopyOfFile(first.Id, room.Id), FileOperationType.Copy);

        // wait for the first copy to land without reading its record
        (await WaitForCount(() => CountFiles(room.Id), 1)).Should().Be(1);

        // Act
        var answer = await StartCopyOfFile(second.Id, room.Id);

        // Assert
        answer.Count.Should().BeGreaterThanOrEqualTo(2);
        answer.Find(o => o.Id == one.Id)?.Finished.Should().BeTrue();
        var fresh = Started(answer, FileOperationType.Copy);
        fresh.Id.Should().NotBe(one.Id);
        answer[^1].Id.Should().Be(fresh.Id);

        // the earlier record was handed out by that answer and is not reported again
        await FinishedRecord(fresh.Id);
        (await GetStatuses()).Should().BeEmpty();
    }
}
