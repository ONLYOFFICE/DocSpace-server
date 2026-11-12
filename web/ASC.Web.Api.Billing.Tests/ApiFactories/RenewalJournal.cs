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

namespace ASC.Web.Api.Billing.Tests.ApiFactories;

/// <summary>
/// The journal and the pending list of <c>RenewSubscriptionService</c> in the stack's Redis, for a test of what the
/// service does with a renewal a stopped process left behind. No API call and no test can stop a process half-way,
/// so the test plants the entry such a process leaves; the service then finds it the way it would find a real one.
/// </summary>
/// <remarks>
/// The values are written through FusionCache with the serializer and the key mode of the services
/// (<c>AddHybridCache</c>), so the stored entry is the one the service reads, and every change is made under the lock
/// the service changes the journal under. The lists are shared by every portal of the run and are handled as JSON
/// arrays: an entry is added or taken out, the entries of the other portals stay as they were read, whatever fields
/// the service gives them. The records mirror the private ones of the service by property name.
/// The service reads both lists from Redis and changes them under the same lock, so what is planted or taken out here
/// is what its next run sees.
/// </remarks>
public sealed class RenewalJournal : IAsyncDisposable
{
    private const string JournalKey = "renewsubscriptionservice_journal";
    private const string PendingKey = "renewsubscriptionservice_pending";
    private const string LockKey = "renewsubscriptionservice_journal_lock";

    private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(30);

    // past this process's memory level in both directions: only what Redis holds counts
    private static readonly FusionCacheEntryOptions _options = new()
    {
        Duration = TimeSpan.FromDays(1),
        SkipMemoryCacheRead = true,
        SkipMemoryCacheWrite = true,
        ReThrowDistributedCacheExceptions = true
    };

    private readonly ConnectionMultiplexer _redis;
    private readonly RedisCache _distributedCache;
    private readonly FusionCache _cache;

    private RenewalJournal(ConnectionMultiplexer redis)
    {
        _redis = redis;

        _distributedCache = new RedisCache(new RedisCacheOptions
        {
            ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(_redis)
        });

        _cache = new FusionCache(new FusionCacheOptions { DistributedCacheKeyModifierMode = CacheKeyModifierMode.None });

        _cache.SetupDistributedCache(_distributedCache, new FusionCacheSystemTextJsonSerializer(new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        }));
    }

    public static async Task<RenewalJournal> OpenAsync(string connectionString)
    {
        return new RenewalJournal(await ConnectionMultiplexer.ConnectAsync(connectionString));
    }

    /// <summary>
    /// Adds <paramref name="entry"/> to the journal and, when given, <paramref name="pending"/> to the pending list.
    /// </summary>
    public async Task PlantAsync(RenewalJournalEntry entry, PendingRenewal? pending, CancellationToken cancellationToken)
    {
        await using (await LockAsync(cancellationToken))
        {
            if (pending != null)
            {
                var pendingList = await ReadAsync(PendingKey, cancellationToken);
                pendingList.Add(JsonSerializer.SerializeToNode(pending));

                await _cache.SetAsync(PendingKey, pendingList, _options, cancellationToken);
            }

            var journal = await ReadAsync(JournalKey, cancellationToken);
            journal.Add(JsonSerializer.SerializeToNode(entry));

            await _cache.SetAsync(JournalKey, journal, _options, cancellationToken);
        }
    }

    /// <summary>The renewal of the portal the journal holds, null when it holds none.</summary>
    public async Task<RenewalJournalEntry?> FindEntryAsync(int tenantId, CancellationToken cancellationToken)
    {
        return (await ReadAsync(JournalKey, cancellationToken)).FirstOrDefault(node => IsOf(node, tenantId)).Deserialize<RenewalJournalEntry>();
    }

    /// <summary>Whether the pending list holds a renewal of the portal.</summary>
    public async Task<bool> HoldsPendingAsync(int tenantId, CancellationToken cancellationToken)
    {
        return (await ReadAsync(PendingKey, cancellationToken)).Any(node => IsOf(node, tenantId));
    }

    /// <summary>
    /// Takes what is left of the portal's renewals out of the journal and the pending list, so that a test that
    /// leaves one there, or fails half-way, leaves nothing for the service to settle against a portal nobody scripts
    /// any more.
    /// </summary>
    public async Task RemoveAsync(int tenantId, CancellationToken cancellationToken)
    {
        await using (await LockAsync(cancellationToken))
        {
            await RemoveAsync(JournalKey, tenantId, cancellationToken);
            await RemoveAsync(PendingKey, tenantId, cancellationToken);
        }
    }

    private async Task RemoveAsync(string key, int tenantId, CancellationToken cancellationToken)
    {
        var list = await ReadAsync(key, cancellationToken);
        var left = new JsonArray([.. list.Where(node => !IsOf(node, tenantId)).Select(node => node!.DeepClone())]);

        if (left.Count == list.Count)
        {
            return;
        }

        if (left.Count == 0)
        {
            await _cache.RemoveAsync(key, _options, cancellationToken);
        }
        else
        {
            await _cache.SetAsync(key, left, _options, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cache.Dispose();
        _distributedCache.Dispose();

        await _redis.DisposeAsync();
    }

    private async Task<IAsyncDisposable> LockAsync(CancellationToken cancellationToken)
    {
        return await new RedisDistributedLock(LockKey, _redis.GetDatabase()).AcquireAsync(_lockTimeout, cancellationToken);
    }

    private async Task<JsonArray> ReadAsync(string key, CancellationToken cancellationToken)
    {
        return await _cache.GetOrDefaultAsync<JsonArray>(key, null, _options, cancellationToken) ?? [];
    }

    private static bool IsOf(JsonNode? node, int tenantId)
    {
        return node?["TenantId"]?.GetValue<int>() == tenantId;
    }
}

/// <summary>
/// A renewal in the journal, as <c>RenewSubscriptionService</c> stores it: the tariff row it renews, the stage it got
/// to (<see cref="Queued"/> or <see cref="Started"/>) and since when, and how many times it has been started.
/// </summary>
public sealed record RenewalJournalEntry(
    int TenantId,
    int Quota,
    int Quantity,
    int? NextQuota,
    int? NextQuantity,
    DateTime DueDate,
    int Stage,
    DateTime StageSince,
    int Attempt)
{
    public const int Queued = 0;
    public const int Started = 1;
}

/// <summary>
/// A renewal whose payment change got no answer, as <c>RenewSubscriptionService</c> stores it for its re-checks.
/// </summary>
public sealed record PendingRenewal(
    int TenantId,
    int Quota,
    string ProductId,
    string Description,
    string ServiceName,
    int Quantity,
    DateTime PreviousDueDate,
    Guid OwnerId,
    Guid? PayerId,
    DateTime TimedOutAt);
