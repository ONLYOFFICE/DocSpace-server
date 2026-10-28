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

namespace ASC.ClearEvents.Tests.Tests;

/// <summary>
/// A portal of its own per test, the Files clients signed in as its owner, and the two things every cleanup test
/// does: put audit events of a known age into the trail, and run a pass to see which of them survive it.
/// </summary>
public abstract class BaseTest(AspireAppFixture fixture) : IAsyncLifetime
{
    /// <summary>The <c>entry_type</c> of a folder or room in <c>files_audit_reference</c> (FileEntryType.Folder).</summary>
    protected const byte FolderEntry = 1;

    /// <summary>The <c>entry_type</c> of a file in <c>files_audit_reference</c> (FileEntryType.File).</summary>
    protected const byte FileEntry = 2;

    /// <summary>The free and paid retention <see cref="Retention"/> runs with unless a test says otherwise.</summary>
    protected const int FreeLifeTimeDays = 365;
    protected const int PaidLifeTimeDays = 1095;

    private PortalClients _clients = null!;

    protected int TenantId => _clients.TenantId;

    protected User Owner => _clients.Owner;

    protected AspireAppFixture Fixture => fixture;

    protected FoldersApi _foldersApi = null!;
    protected FilesApi _filesApi = null!;
    protected OperationsApi _operationsApi = null!;
    protected RoomsApi _roomsApi = null!;

    public async ValueTask InitializeAsync()
    {
        _clients = await fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        _foldersApi = _clients.FoldersApi;
        _filesApi = _clients.FilesApi;
        _operationsApi = _clients.OperationsApi;
        _roomsApi = _clients.RoomsApi;

        await _clients.FilesHttpClient.Authenticate(Owner);
    }

    public virtual ValueTask DisposeAsync()
    {
        _clients.Dispose();

        return ValueTask.CompletedTask;
    }

    /// <summary>Cleanup settings: enabled, with the retention and batch size of production unless overridden.</summary>
    protected static AuditTrailRetentionConfiguration Retention(
        bool enabled = true,
        int freeLifeTimeDays = FreeLifeTimeDays,
        int paidLifeTimeDays = PaidLifeTimeDays,
        int batchSize = 1000,
        TimeSpan? period = null)
    {
        return new AuditTrailRetentionConfiguration
        {
            Enabled = enabled,
            FreeLifeTimeDays = freeLifeTimeDays,
            PaidLifeTimeDays = paidLifeTimeDays,
            BatchSize = batchSize,
            Period = period ?? TimeSpan.FromDays(1)
        };
    }

    /// <summary>
    /// Runs one cleanup pass over every tenant. A pass skips a tenant it fails on and only logs it, so a call the
    /// tariff stand-in cannot answer is checked here: unchecked, it would leave every event of the tenant in place
    /// and pass every test that asserts what was kept.
    /// </summary>
    protected async Task RunCleanupAsync(AuditTrailRetentionConfiguration? retention = null)
    {
        await fixture.Cleaner.ClearAsync(retention ?? Retention(), TestContext.Current.CancellationToken);

        TestTariffs.UnexpectedCalls.Should().BeEmpty("the pass must only ask the tariff what TestTariffs answers");
    }

    /// <summary>
    /// Writes an audit event of this portal dated <paramref name="daysAgo"/> days back, in the history of the
    /// given folders or rooms. The entry ids need not exist: the cleanup only looks at whether a reference does.
    /// </summary>
    protected Task<int> SeedEventAsync(int daysAgo, params int[] historyOfFolderIds)
    {
        return SeedEventForTenantAsync(TenantId, daysAgo, historyOfFolderIds);
    }

    /// <summary>As <see cref="SeedEventAsync(int, int[])"/>, in the audit trail of another portal.</summary>
    protected async Task<int> SeedEventForTenantAsync(int tenantId, int daysAgo, params int[] historyOfFolderIds)
    {
        await using var ef = await fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var auditEvent = new DbAuditEvent
        {
            TenantId = tenantId,
            Date = DateTime.UtcNow.AddDays(-daysAgo),
            Action = (int)AuditAction.RoomRenamed,
            UserId = Owner.Id,
            FilesReferences = historyOfFolderIds
                .Select(id => new DbFilesAuditReference { EntryId = id, EntryType = FolderEntry })
                .ToList()
        };

        ef.AuditEvents.Add(auditEvent);

        await ef.SaveChangesAsync(TestContext.Current.CancellationToken);

        return auditEvent.Id;
    }

    /// <summary>Which of <paramref name="eventIds"/> are still in the audit trail.</summary>
    protected async Task<List<int>> RemainingEventsAsync(IEnumerable<int> eventIds)
    {
        var ids = eventIds.ToList();

        await using var ef = await fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        return await ef.AuditEvents
            .Where(r => ids.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>The events of this portal in the history of an entry, as <c>files_audit_reference</c> has them.</summary>
    protected async Task<List<int>> HistoryEventsAsync(int entryId, byte entryType)
    {
        await using var ef = await fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        return await ef.AuditEvents
            .Where(r => r.TenantId == TenantId && r.FilesReferences.Any(a => a.EntryId == entryId && a.EntryType == entryType))
            .Select(r => r.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Reads the audit trail until the history of an entry holds an event for each of <paramref name="actions"/>,
    /// since the events are written after the request that caused them returns. Returns the ids of the events in
    /// that history last seen, so a timed-out assertion shows what was there.
    /// </summary>
    protected async Task<List<int>> PollHistoryEventsAsync(int entryId, byte entryType, params AuditAction[] actions)
    {
        var expected = actions.Select(a => (int?)a).ToList();
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            await using var ef = await fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

            var events = await ef.AuditEvents
                .Where(r => r.TenantId == TenantId && r.FilesReferences.Any(a => a.EntryId == entryId && a.EntryType == entryType))
                .Select(r => new { r.Id, r.Action })
                .ToListAsync(TestContext.Current.CancellationToken);

            if (expected.All(a => events.Exists(e => e.Action == a)) || DateTime.UtcNow >= deadline)
            {
                return events.ConvertAll(e => e.Id);
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Every entry the given events are in the history of, as (entry id, entry type).</summary>
    protected async Task<List<(int EntryId, byte EntryType)>> HistoriesOfAsync(IEnumerable<int> eventIds)
    {
        var ids = eventIds.ToList();

        await using var ef = await fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var references = await ef.FilesAuditReferences
            .Where(r => ids.Contains(r.AuditEventId))
            .Select(r => new { r.EntryId, r.EntryType })
            .Distinct()
            .ToListAsync(TestContext.Current.CancellationToken);

        return references.ConvertAll(r => (r.EntryId, r.EntryType));
    }

    /// <summary>
    /// Moves every audit event of this portal <paramref name="days"/> days into the past, so that the events
    /// the services wrote a moment ago are as old as the cleanup needs them to be.
    /// </summary>
    protected async Task AgeAuditTrailAsync(int days)
    {
        await using var ef = await fixture.Cleaner.CreateDbContextAsync(TestContext.Current.CancellationToken);

        await ef.AuditEvents
            .Where(r => r.TenantId == TenantId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Date, r => r.Date.AddDays(-days)), TestContext.Current.CancellationToken);
    }

    protected async Task<FolderDto> CreateCustomRoomAsync(string title)
    {
        return (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<int> GetMyDocumentsIdAsync()
    {
        return (await _foldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;
    }

    protected async Task<FileDto> CreateFileAsync(int folderId, string title)
    {
        return (await _filesApi.CreateFileAsync(folderId, new CreateFileRequest(title), TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// Waits for the long operations of the caller to finish and asserts that they did without an error: a delete
    /// still running, or failed, would otherwise surface as an unexplained result of the cleanup pass after it.
    /// </summary>
    protected async Task WaitLongOperationAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var statuses = (await _operationsApi.GetOperationStatusesAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (statuses.TrueForAll(r => r.Finished) || DateTime.UtcNow >= deadline)
            {
                statuses.Should().OnlyContain(r => r.Finished && string.IsNullOrEmpty(r.Error));

                return;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Reads a room's history until every one of <paramref name="actions"/> is in it, since the audit events are
    /// written after the request that caused them returns. Returns the actions last seen, so a timed-out
    /// assertion shows what was there.
    /// </summary>
    protected async Task<List<MessageAction?>> PollRoomHistoryAsync(int roomId, params MessageAction[] actions)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var history = (await _foldersApi.GetFolderHistoryAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
            var seen = history.ConvertAll(e => e.Action?.Id);

            if (actions.All(a => seen.Contains(a)) || DateTime.UtcNow >= deadline)
            {
                return seen;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
