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
/// Lets the same letter reach the same recipient through the same channel only once per period, however many times
/// the event that produces it is repeated. The mark lives in the distributed cache because letters of one tenant are
/// produced by several services and their replicas; the lock keeps parallel sends from slipping past the check.
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

    public async Task<bool> IsDuplicateAsync(NotifyRequest request)
    {
        var period = request.NotifyAction.DeduplicationPeriod ?? _period;
        if (period > _maxPeriod)
        {
            period = _maxPeriod;
        }

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

            await hybridCache.SetAsync(key, true, period);
        }

        return false;
    }

    public static string GetCacheKey(int tenantId, string actionId, string sender, string recipientId, string objectId, IEnumerable<string> discriminators)
    {
        var identity = string.Join('\n', [actionId, sender, recipientId?.ToLowerInvariant(), objectId, .. discriminators]);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));

        return tenantId + "notifydedup" + hash;
    }

    private static IEnumerable<string> GetDiscriminators(NotifyRequest request)
    {
        foreach (var tag in request.NotifyAction.DeduplicationTags)
        {
            yield return request.Arguments.Find(a => a.Tag == tag)?.Value?.ToString();
        }
    }
}
