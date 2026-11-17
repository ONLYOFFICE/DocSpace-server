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

namespace ASC.Files.Core.Helpers;

/// <summary>
/// How long the answers of the editing service about its trial are kept.
/// </summary>
/// <param name="Duration">How long an answer is reused.</param>
/// <param name="FailSafeMaxDuration">
/// How long the last answer known stays in use while the service fails. At least the period of the background refresh
/// (<see cref="ReadRefreshPeriod"/> keeps the period within it), so a known answer is always there.
/// </param>
/// <param name="FailureDuration">
/// How long a failure is kept: the pause before the service is asked again while the last answer known is in use, and
/// how long an unknown answer is reused when no answer is known.
/// </param>
/// <param name="HardTimeout">
/// How long a cached reader waits, and how long the other readers of the same process wait for it rather than ask the
/// service themselves; the license command itself gives up after 5 seconds, so this only bounds the rest.
/// </param>
public record DocsTrialCacheOptions(TimeSpan Duration, TimeSpan FailSafeMaxDuration, TimeSpan FailureDuration, TimeSpan HardTimeout)
{
    /// <summary>
    /// The configuration key of the period of the background refresh that asks the editing service again.
    /// </summary>
    public const string RefreshPeriodKey = "files:refreshLicense:period";

    public static readonly DocsTrialCacheOptions Default = new(
        TimeSpan.FromMinutes(15),
        TimeSpan.FromDays(2),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromSeconds(10));

    /// <summary>
    /// Reads the period of the background refresh, <c>d.hh:mm:ss</c>: one day (<c>"1.00:00:00"</c>) by default, at
    /// least a minute, so the loop does not run hot, and at most <see cref="FailSafeMaxDuration"/> of
    /// <see cref="Default"/>, so the last answer known outlives the period and is there between two refreshes.
    /// </summary>
    public static TimeSpan ReadRefreshPeriod(IConfiguration configuration, ILogger logger)
    {
        return configuration.ReadInterval(RefreshPeriodKey, TimeSpan.FromDays(1), TimeSpan.FromMinutes(1), Default.FailSafeMaxDuration, logger);
    }
}

/// <summary>
/// The answer of the editing service about its own trial, kept in the distributed cache: it is one value for the whole
/// installation, asked for anonymously by the setup wizard page (does it need a license file) and refreshed by the
/// background job of another process.
/// </summary>
/// <remarks>
/// A reader gets one request to the editing service at a time per process: the others wait for it up to
/// <see cref="DocsTrialCacheOptions.HardTimeout"/>. While the service fails, the last answer known stays in use for
/// <see cref="DocsTrialCacheOptions.FailSafeMaxDuration"/> and the service is asked again after
/// <see cref="DocsTrialCacheOptions.FailureDuration"/>; with none known, the failure itself is kept for that long,
/// unless another process stored a known answer meanwhile. A
/// fresh question with a known answer replaces the cached one for every process; a fresh question with an unknown
/// answer leaves the cache alone, so a failure seen by the setup wizard or the background refresh never removes the
/// last answer known. The answer survives a restart of the processes as long as the distributed cache keeps it; with no
/// distributed cache configured, it is kept per process.
/// </remarks>
public sealed class DocsTrialCache(IFusionCache cache, ILogger<DocumentServiceLicense> logger, DocsTrialCacheOptions options)
{
    /// <summary>
    /// The key of the answer: one per installation, so it carries the installation's own tenant.
    /// </summary>
    public static string GetCacheKey()
    {
        return Tenant.DefaultTenant + "docstrial";
    }

    /// <param name="request">Asks the editing service; never throws, an answer it cannot read is Unknown.</param>
    /// <param name="useCache">Whether a cached answer may be used instead of a fresh question.</param>
    public async Task<DocsTrialCheck> GetAsync(Func<Task<DocsTrialCheck>> request, bool useCache)
    {
        if (!useCache)
        {
            var fresh = await request();
            if (fresh.State != DocsTrialState.Unknown)
            {
                await cache.SetAsync(GetCacheKey(), fresh, entry => SetupEntry(entry));
            }

            return fresh;
        }

        try
        {
            return await cache.GetOrSetAsync<DocsTrialCheck>(GetCacheKey(), async (_, _) =>
            {
                var check = await request();

                // An unknown answer is a failure, not an answer: it must not replace the last answer known
                return check.State == DocsTrialState.Unknown
                    ? throw new InvalidOperationException("The editing service did not say whether it runs its trial")
                    : check;
            }, entry =>
            {
                SetupEntry(entry).SetFactoryTimeouts(hardTimeout: options.HardTimeout);

                // The default of the distributed cache lets a waiting reader run the factory itself after 5 seconds,
                // which would send one more request to an editing service that already does not answer
                entry.LockTimeout = options.HardTimeout;
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // No answer known to fall back on when the question was asked. The cause is logged where the service was
            // asked; a request cut by the hard timeout goes on in the background and replaces this answer when it ends
            logger.DebugDocsTrialNoAnswerKnown(ex.Message);

            // Another process (the background refresh) may have stored a known answer in the meantime: the failure of
            // this one must not replace it. A known answer stored between this read and the write below is still
            // replaced for the failure duration; the next fresh question stores it again
            var known = await GetLastKnownAsync();
            if (known != null)
            {
                return known;
            }

            await cache.SetAsync(GetCacheKey(), DocsTrialCheck.Unknown, options.FailureDuration);

            return DocsTrialCheck.Unknown;
        }
    }

    /// <summary>
    /// Returns the last answer of the editing service the installation knows, also one past its duration that the
    /// fail-safe still keeps, without asking the service; <c>null</c> when none is known.
    /// </summary>
    public async Task<DocsTrialCheck> GetLastKnownAsync()
    {
        var known = await cache.TryGetAsync<DocsTrialCheck>(GetCacheKey(), entry => entry.SetAllowStaleOnReadOnly());

        return known.HasValue && known.Value is { State: not DocsTrialState.Unknown } ? known.Value : null;
    }

    private FusionCacheEntryOptions SetupEntry(FusionCacheEntryOptions entry)
    {
        return entry
            .SetDuration(options.Duration)
            .SetFailSafe(true, options.FailSafeMaxDuration, options.FailureDuration);
    }
}
