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
    private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);

    private readonly TimeSpan _period =
        TimeSpan.TryParse(configuration["core:notify:deduplication:period"], CultureInfo.InvariantCulture, out var period) && period >= TimeSpan.Zero
            ? period
            : _defaultPeriod;

    // An invitation link expires this long after it is first opened. Suppressing a resend for longer could leave
    // the recipient holding only an expired link, so no period may outlast it.
    private readonly TimeSpan _maxPeriod = EmailValidationKeyProvider.GetValidVisitLinkInterval(configuration);

    private TimeSpan Period => _period > _maxPeriod ? _maxPeriod : _period;

    /// <summary>
    /// Tells, without marking anything, whether this letter already went out to the recipient within the period. The
    /// sending code asks it before it builds what only a new letter needs - a confirmation link, an audit record, a
    /// spent invitation - and has to pass exactly what the letter carries into its key. The mark is set once the
    /// letter is sent, a moment after the request that queued it, so a burst of calls inside that moment still passes.
    /// </summary>
    public async Task<bool> IsMarkedAsync(int tenantId, INotifyAction action, string sender, string recipientId, string objectId, IEnumerable<string> discriminators)
    {
        if (!action.SuppressRepeats || Period <= TimeSpan.Zero)
        {
            return false;
        }

        var key = GetCacheKey(tenantId, action.ID, sender, recipientId, objectId, discriminators);

        return (await hybridCache.TryGetAsync<bool>(key)).HasValue;
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

        var key = GetCacheKey(request._tenantId, request.NotifyAction.ID, request.CurrentSender, request.Recipient.ID, request.ObjectID, GetDiscriminators(request));

        if ((await hybridCache.TryGetAsync<bool>(key)).HasValue)
        {
            logger.DebugDuplicateSuppressed(request.NotifyAction.ID, request.CurrentSender, request._tenantId);
            return true;
        }

        await using (await distributedLockProvider.TryAcquireLockAsync($"{key}_lock", _lockTimeout))
        {
            if ((await hybridCache.TryGetAsync<bool>(key)).HasValue)
            {
                logger.DebugDuplicateSuppressed(request.NotifyAction.ID, request.CurrentSender, request._tenantId);
                return true;
            }

            // a new mark replaces nothing another node holds in memory - a miss is never kept there - so the backplane
            // message would only make every node evict a key it does not have, once per letter of a bulk send
            await hybridCache.SetAsync(key, true, options => options.SetDuration(period).SetSkipBackplaneNotifications(true));
        }

        request.Properties[GetMarkProperty(request.CurrentSender)] = key;

        return false;
    }

    /// <summary>
    /// Takes back the mark this request set, once its channel failed to deliver the letter, so that the next repeat
    /// goes out instead of being dropped for a letter nobody got. A request that was itself suppressed set no mark and
    /// releases nothing.
    /// </summary>
    public async Task ReleaseAsync(NotifyRequest request)
    {
        var property = GetMarkProperty(request.CurrentSender);
        if (request.Properties[property] is not string key)
        {
            return;
        }

        request.Properties.Remove(property);

        // with the backplane notification: another node may hold the mark in memory since a repeat it checked
        await hybridCache.RemoveAsync(key);

        logger.DebugMarkReleased(request.NotifyAction.ID, request.CurrentSender, request._tenantId);
    }

    public static string GetCacheKey(int tenantId, string actionId, string sender, string recipientId, string objectId, IEnumerable<string> discriminators)
    {
        var identity = string.Join('\n', [actionId, sender, recipientId?.ToLowerInvariant(), objectId, .. discriminators]);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));

        return tenantId + "notifydedup" + hash;
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
