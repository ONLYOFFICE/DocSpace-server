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

namespace ASC.Files.Tests.Tests._02_Folders.UsedSpace;

/// <summary>
/// Shared helpers for every used-space suite: reading and polling the per-section usage
/// statistics of <c>GetFilesUsedSpaceAsync</c> together with the portal's total space used (the
/// "total_size" quota feature), plus the delete/trash helpers the tests exercise those counters
/// with. Every test runs on its own portal (see <see cref="BaseTest"/>), so the numbers observed
/// here are affected by nothing but the content the test itself creates.
/// </summary>
public abstract class UsedSpaceTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// The id of the quota feature holding the total space used by the portal. Its title is a
    /// localized caption, so the feature is looked up by id.
    /// </summary>
    private const string TotalSizeFeature = "total_size";

    /// <summary>
    /// A freshly registered portal fills the owner's sections with the default documents in the
    /// background, so the counters keep growing for a while after the portal becomes available.
    /// Reads the statistics until they stop changing, otherwise that content leaks into the delta
    /// measured by a test.
    /// </summary>
    protected async Task<UsedSpaceSnapshot> GetBaselineUsedSpaceAsync()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);

        // Provisioning of the owner's root folder tree is lazy, so trigger it before measuring.
        await GetUserFolderIdAsync(Owner);

        var sw = Stopwatch.StartNew();
        var previous = await GetUsedSpaceAsync();
        var stableReads = 0;

        while (sw.Elapsed < TimeSpan.FromSeconds(60))
        {
            await Task.Delay(300, TestContext.Current.CancellationToken);

            var current = await GetUsedSpaceAsync();

            stableReads = previous.SameAs(current) ? stableReads + 1 : 0;
            previous = current;

            if (stableReads == 3)
            {
                break;
            }
        }

        return previous;
    }

    protected async Task<UsedSpaceSnapshot> GetUsedSpaceAsync()
    {
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;

        return new UsedSpaceSnapshot(sections, await GetTotalUsedSpaceAsync());
    }

    /// <summary>
    /// Reads the total space used by the portal from the "total_size" quota feature.
    /// </summary>
    /// <remarks>
    /// The typed client (<c>PaymentApi.GetQuotaPaymentInformationAsync</c>) cannot be used here: its
    /// <c>QuotaDto</c> declares "title" as required while a portal of a test returns it empty, so the
    /// answer fails to deserialize. The same endpoint is therefore read as raw JSON.
    /// </remarks>
    protected async Task<long> GetTotalUsedSpaceAsync()
    {
        var response = (await _paymentApi.GetQuotaPaymentInformationAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        var feature = response.Features.FirstOrDefault(r => r.Id == TotalSizeFeature);

        feature.Should().NotBeNull();

        return Convert.ToInt64(feature!.Used.Value);
    }

    /// <summary>
    /// The counters are updated by the operation itself, so they can lag a little behind its completion.
    /// Polls the statistics until <paramref name="condition"/> holds and returns the last read snapshot,
    /// so the caller always asserts on a real value.
    /// </summary>
    protected async Task<UsedSpaceSnapshot> WaitForUsedSpaceAsync(Func<UsedSpaceSnapshot, bool> condition)
    {
        var sw = Stopwatch.StartNew();

        while (true)
        {
            var usedSpace = await GetUsedSpaceAsync();

            if (condition(usedSpace))
            {
                return usedSpace;
            }

            if (sw.Elapsed > TimeSpan.FromSeconds(10))
            {
                return usedSpace;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }

    protected async Task DeleteFileAndWait(int fileId, bool immediately)
    {
        var results = (await _filesApi.DeleteFileAsync(
            fileId,
            new Delete { Immediately = immediately },
            true,
            TestContext.Current.CancellationToken)).Response;

        await WaitForCompletionAsync(results);
    }

    protected async Task DeleteFolderAndWait(int folderId, bool immediately)
    {
        var results = (await _foldersApi.DeleteFolderAsync(
            folderId,
            new DeleteFolder { Immediately = immediately },
            TestContext.Current.CancellationToken)).Response;

        await WaitForCompletionAsync(results);
    }

    protected async Task DeleteBatchAndWait(int folderId, int fileId, bool immediately)
    {
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto
            {
                FolderIds = [new(folderId)],
                FileIds = [new(fileId)],
                Immediately = immediately
            },
            TestContext.Current.CancellationToken)).Response;

        await WaitForCompletionAsync(results);
    }

    protected async Task EmptyTrashAndWait()
    {
        var results = (await _filesOperationsApi.EmptyTrashAsync(true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        await WaitForCompletionAsync(results);
    }

    /// <summary>
    /// Waits for the operations started by a request to finish. Deleting several trees at once takes
    /// longer than the budget of <see cref="BaseTest.WaitLongOperation"/>, so the polling is done here
    /// with a larger one.
    /// </summary>
    protected async Task WaitForCompletionAsync(List<FileOperationDto> results)
    {
        var operationId = results.FirstOrDefault()?.Id;
        var sw = Stopwatch.StartNew();

        while (results.Exists(r => !r.Finished) && sw.Elapsed < TimeSpan.FromMinutes(2))
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);

            var statuses = (await _filesOperationsApi.GetOperationStatusesAsync(
                id: operationId,
                cancellationToken: TestContext.Current.CancellationToken)).Response;

            // a finished operation is eventually dropped from the queue, so an empty answer
            // means it is over - any error it reported was visible while it was still listed
            if (statuses.Count == 0)
            {
                return;
            }

            results = statuses;
        }

        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        results.Should().OnlyContain(x => x.Finished);
    }

    /// <summary>
    /// A single reading of every used space counter a test looks at: the per-section statistics and
    /// the total space used by the portal.
    /// </summary>
    protected sealed record UsedSpaceSnapshot(FilesStatisticsResultDto Sections, long Total)
    {
        public long My => Space(Sections.MyDocumentsUsedSpace);

        public long Trash => Space(Sections.TrashUsedSpace);

        public long Rooms => Space(Sections.RoomsUsedSpace);

        public long Archive => Space(Sections.ArchiveUsedSpace);

        public bool SameAs(UsedSpaceSnapshot other)
        {
            return My == other.My && Trash == other.Trash && Rooms == other.Rooms && Archive == other.Archive && Total == other.Total;
        }

        private static long Space(FilesStatisticsFolder? folder)
        {
            return folder?.UsedSpace ?? 0;
        }
    }
}
