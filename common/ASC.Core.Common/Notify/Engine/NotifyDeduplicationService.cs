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

namespace ASC.Notify.Engine;

/// <summary>
/// Lets a letter that opts in (<see cref="INotifyAction.SuppressRepeats"/>) reach the same recipient through the same
/// channel only once per period, however many times the event that produces it is repeated. The mark lives in the
/// distributed cache because letters of one tenant are produced by several services and their replicas; the lock
/// keeps parallel sends from slipping past the check.
/// The mark is set before the letter is delivered, not after it. A repeat that arrives while the first letter is still
/// on its way is dropped; if that delivery then fails, the mark is released for the next repeat, but the dropped one
/// is not sent again. The same holds for a request the sending code refused while an earlier one was only accepted.
/// The window lasts one delivery, or the acceptance of a letter that never reaches the engine.
/// The acceptance costs what the sending code builds before it sends - a link, an audit record - so a letter that never
/// reaches the engine (an address left out as not activated, a free portal over its letter count) costs that once per
/// acceptance rather than once per call.
/// A zero <c>core:notify:deduplication:period</c> turns the check off, and with it the only bound on the endpoints that
/// send these letters: they carry no rate limit.
/// With a distributed level the marks are read and written there only, never kept in a node's memory: a copy in memory
/// would outlive a release whose backplane message that node missed, and refuse the next request until the period ran
/// out.
/// With <c>Redis:Enabled=false</c> the cache has no distributed level, so the mark is seen only by the process that
/// set it and a repeat produced by another process goes out - enough for a single-node installation, not for a cluster.
/// </summary>
[Singleton]
public class NotifyDeduplicationService(
    IConfiguration configuration,
    IFusionCache hybridCache,
    IDistributedLockProvider distributedLockProvider,
    ILogger<NotifyDeduplicationService> logger)
{
    private static readonly TimeSpan _defaultPeriod = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _defaultAcceptedPeriod = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);

    private readonly TimeSpan _period =
        TimeSpan.TryParse(configuration["core:notify:deduplication:period"], CultureInfo.InvariantCulture, out var period) && period >= TimeSpan.Zero
            ? period
            : _defaultPeriod;

    private readonly TimeSpan _acceptedPeriod =
        TimeSpan.TryParse(configuration["core:notify:deduplication:accepted"], CultureInfo.InvariantCulture, out var accepted) && accepted >= TimeSpan.Zero
            ? accepted
            : _defaultAcceptedPeriod;

    // An invitation link expires this long after it is first opened. Suppressing a resend for longer could leave
    // the recipient holding only an expired link, so no period may outlast it.
    private readonly TimeSpan _maxPeriod = EmailValidationKeyProvider.GetValidVisitLinkInterval(configuration);

    private TimeSpan Period => _period > _maxPeriod ? _maxPeriod : _period;

    private TimeSpan AcceptedPeriod => _acceptedPeriod > Period ? Period : _acceptedPeriod;

    /// <summary>
    /// Decides, before the sending code builds what only a new letter needs (a confirmation link, an audit record, a
    /// spent invitation), whether to go on. Refuses when the letter went out within the period or was accepted a moment
    /// ago; otherwise accepts this request. The acceptance holds back a burst of calls until the engine sends the letter,
    /// or until it runs out for a letter that never reaches the engine (an address left out as not activated, a free
    /// portal over its letter count). It lives on a key of its own, which the engine never reads or writes, so a late
    /// acceptance cannot replace the engine's mark. The caller disposes the answer and completes it once the letter is
    /// handed over; a failure before that takes the acceptance back. The caller passes exactly what the letter will
    /// carry into its key.
    /// </summary>
    public async Task<NotifyAcceptance> TryAcceptAsync(int tenantId, INotifyAction action, string sender, string recipientId, string objectId, IEnumerable<string> discriminators)
    {
        if (!action.SuppressRepeats || Period <= TimeSpan.Zero)
        {
            return NotifyAcceptance.Unchecked;
        }

        var values = discriminators.ToArray();
        if (values.Length != action.DeduplicationTags.Length || !HasEveryValue(values))
        {
            return NotifyAcceptance.Unchecked;
        }

        var key = GetCacheKey(tenantId, action.ID, sender, recipientId, objectId, values);
        var acceptedKey = GetAcceptedKey(key);

        var sent = hybridCache.TryGetAsync<bool>(key, InDistributedLevel).AsTask();
        var accepted = hybridCache.TryGetAsync<string>(acceptedKey, InDistributedLevel).AsTask();

        if ((await sent).HasValue || (await accepted).HasValue)
        {
            logger.DebugDuplicateSuppressed(action.ID, sender, tenantId);
            return NotifyAcceptance.Refused;
        }

        // a zero acceptance turns off holding back a burst, not the check of a letter already sent above
        if (AcceptedPeriod <= TimeSpan.Zero)
        {
            return NotifyAcceptance.Unchecked;
        }

        // No lock: it would cost two more round trips per recipient of every call. Two calls that both find no mark are
        // both accepted, which costs one more link; the letter itself still goes out once, the engine checks it under
        // its own lock.
        var token = Guid.NewGuid().ToString("N");
        await hybridCache.SetAsync(acceptedKey, token, options => InDistributedLevel(options.SetDuration(AcceptedPeriod).SetSkipBackplaneNotifications(true)));

        return NotifyAcceptance.Recorded(this, acceptedKey, token);
    }

    // Only while the key still holds this acceptance: a request that failed late, or one accepted in parallel, must not
    // take back the acceptance of a request whose letter went out.
    internal async Task ReleaseAcceptanceAsync(string acceptedKey, string token)
    {
        var current = await hybridCache.TryGetAsync<string>(acceptedKey, InDistributedLevel);
        if (!current.HasValue || current.Value != token)
        {
            return;
        }

        await hybridCache.RemoveAsync(acceptedKey);
    }

    public async Task<bool> IsDuplicateAsync(NotifyRequest request)
    {
        if (!request.NotifyAction.SuppressRepeats)
        {
            return false;
        }

        var period = Period;
        if (period <= TimeSpan.Zero)
        {
            return false;
        }

        var discriminators = GetDiscriminators(request).ToArray();
        if (!HasEveryValue(discriminators))
        {
            return false;
        }

        var key = GetCacheKey(request._tenantId, request.NotifyAction.ID, request.CurrentSender, request.Recipient.ID, request.ObjectID, discriminators);

        if ((await hybridCache.TryGetAsync<bool>(key, InDistributedLevel)).HasValue)
        {
            logger.DebugDuplicateSuppressed(request.NotifyAction.ID, request.CurrentSender, request._tenantId);
            return true;
        }

        await using (await distributedLockProvider.TryAcquireLockAsync($"{key}_lock", _lockTimeout))
        {
            if ((await hybridCache.TryGetAsync<bool>(key, InDistributedLevel)).HasValue)
            {
                logger.DebugDuplicateSuppressed(request.NotifyAction.ID, request.CurrentSender, request._tenantId);
                return true;
            }

            // no node holds a mark in memory, so the backplane message would only make every node evict a key it does
            // not have, once per letter of a bulk send
            await hybridCache.SetAsync(key, true, options => InDistributedLevel(options.SetDuration(period).SetSkipBackplaneNotifications(true)));
        }

        request.Properties[GetMarkProperty(request.CurrentSender)] = key;

        return false;
    }

    /// <summary>
    /// Takes back the mark this request set, once its channel failed to deliver the letter, so that the next repeat
    /// goes out instead of being dropped for a letter nobody got; the acceptance of the request for it goes too. A
    /// request that was itself suppressed set no mark and releases nothing.
    /// </summary>
    public async Task ReleaseAsync(NotifyRequest request)
    {
        var property = GetMarkProperty(request.CurrentSender);
        if (request.Properties[property] is not string key)
        {
            return;
        }

        request.Properties.Remove(property);

        await hybridCache.RemoveAsync(key, InDistributedLevel);
        await hybridCache.RemoveAsync(GetAcceptedKey(key), InDistributedLevel);

        logger.DebugMarkReleased(request.NotifyAction.ID, request.CurrentSender, request._tenantId);
    }

    public static string GetCacheKey(int tenantId, string actionId, string sender, string recipientId, string objectId, IEnumerable<string> discriminators)
    {
        var identity = string.Join('\n', [actionId, sender, recipientId?.ToLowerInvariant(), objectId, .. discriminators]);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));

        return tenantId + "notifydedup" + hash;
    }

    // A letter that names a key tag but carries no value for it cannot be told apart from other letters of its kind:
    // it comes from a producer that does not fill the tag yet - a node of the previous version during an update, a
    // message queued before it - or from an Init that left the tag out. Such a letter goes out rather than be taken
    // for a repeat of a different one.
    private static bool HasEveryValue(string[] discriminators)
    {
        return Array.TrueForAll(discriminators, value => !string.IsNullOrEmpty(value));
    }

    // Past memory whenever there is a distributed level, so that no node keeps a mark another node has released.
    private void InDistributedLevel(FusionCacheEntryOptions options)
    {
        options.SetSkipMemoryCache(hybridCache.HasDistributedCache);
    }

    /// <summary>
    /// The key of the acceptance the sending code records for the letter whose mark lies under <paramref name="key"/>.
    /// </summary>
    public static string GetAcceptedKey(string key)
    {
        return key + "accepted";
    }

    private static string GetMarkProperty(string sender)
    {
        return "notifydedup:" + sender;
    }

    private static IEnumerable<string> GetDiscriminators(NotifyRequest request)
    {
        foreach (var tag in request.NotifyAction.DeduplicationTags)
        {
            yield return request.Arguments.Find(a => a.Tag == tag)?.Value?.ToString();
        }
    }
}
