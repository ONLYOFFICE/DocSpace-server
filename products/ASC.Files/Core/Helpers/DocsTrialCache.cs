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
/// How long the last answer known stays in use while the service fails. At least three periods of the background
/// refresh (<see cref="ReadRefreshPeriod"/> keeps the period within a third of it), so a known answer outlives a
/// refresh that fails, with a whole period to spare for the time the refreshes take.
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
        TimeSpan.FromDays(3),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromSeconds(10));

    /// <summary>
    /// Reads the period of the background refresh, <c>d.hh:mm:ss</c>: one day (<c>"1.00:00:00"</c>) by default, at
    /// least a minute, so the loop does not run hot, and at most a third of the <see cref="FailSafeMaxDuration"/> of
    /// <see cref="Default"/> (one day). The refresh waits the period after each run, so the next answer after one that
    /// finds the editing service silent comes two periods and the time of two runs later: the last answer known
    /// outlives that with a whole period to spare.
    /// </summary>
    public static TimeSpan ReadRefreshPeriod(IConfiguration configuration, ILogger logger)
    {
        return configuration.ReadInterval(RefreshPeriodKey, TimeSpan.FromDays(1), TimeSpan.FromMinutes(1), Default.FailSafeMaxDuration / 3, logger);
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
/// stored before the waiting readers go on, unless another process stored a known answer meanwhile. A
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
                // The answer is in hand: a cache that cannot keep it costs the next readers a question, never this one
                // its answer
                try
                {
                    await cache.SetAsync(GetCacheKey(), fresh, entry => SetupEntry(entry));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.WarningDocsTrialCache(ex);
                }
            }

            return fresh;
        }

        try
        {
            return await cache.GetOrSetAsync<DocsTrialCheck>(GetCacheKey(), async (context, _) =>
            {
                var check = await request();
                if (check.State != DocsTrialState.Unknown)
                {
                    return check;
                }

                // An unknown answer is a failure, not an answer: it must not replace the last answer known, which the
                // fail-safe then returns
                if (context.HasStaleValue)
                {
                    throw new InvalidOperationException("The editing service did not say whether it runs its trial");
                }

                // With no answer known, the failure itself is kept for a while, and stored here, under the lock of the
                // readers of this process: a reader waiting for this request then finds it instead of asking a
                // service that does not answer once more. Another process (the background refresh) may have stored a
                // known answer in the meantime, which this failure must not replace. A known answer stored between
                // this read and the write of the failure is still replaced, and it is gone once the failure expires
                // (the failure has no fail-safe copy): the cache offers no write on a condition, and the next fresh
                // question of the refresh stores it again
                var known = await GetLastKnownAsync();
                if (known != null)
                {
                    return known;
                }

                logger.DebugDocsTrialNoAnswerKnown("the editing service did not say whether it runs its trial");
                context.Options.Duration = options.FailureDuration;
                context.Options.IsFailSafeEnabled = false;

                return DocsTrialCheck.Unknown;
            }, entry =>
            {
                SetupEntry(entry).SetFactoryTimeouts(hardTimeout: options.HardTimeout);

                // A reader waits for the request in flight as long as it may run, rather than for the 5 seconds the
                // distributed cache gives by default: the license command alone may take those 5 seconds
                entry.LockTimeout = options.HardTimeout;
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The request ran past the hard timeout, or failed in a way the factory does not expect (the cache itself
            // failed, an entry it cannot read), with no answer known to fall back on. A timeout is expected: its cause
            // is logged where the service was asked, and the request goes on in the background and replaces this
            // answer when it ends. Any other failure is logged here, with the exception
            if (ex is SyntheticTimeoutException)
            {
                logger.DebugDocsTrialNoAnswerKnown(ex.Message);
            }
            else
            {
                logger.WarningDocsTrialCache(ex);
            }

            return await KeepFailureAsync();
        }
    }

    /// <summary>
    /// Keeps a failure with no answer known for <see cref="DocsTrialCacheOptions.FailureDuration"/>, unless another
    /// process (the background refresh) stored a known answer in the meantime, which is returned instead. A known
    /// answer stored between the read and the write here is still replaced for the failure duration; the next fresh
    /// question stores it again.
    /// </summary>
    /// <returns>The known answer another process stored, or <see cref="DocsTrialCheck.Unknown"/>.</returns>
    private async Task<DocsTrialCheck> KeepFailureAsync()
    {
        // The cache can fail here too: this failure is logged on its own, so it never hides the one being handled,
        // and the answer stays Unknown
        try
        {
            var known = await GetLastKnownAsync();
            if (known != null)
            {
                return known;
            }

            await cache.SetAsync(GetCacheKey(), DocsTrialCheck.Unknown, options.FailureDuration);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.WarningDocsTrialCache(ex);
        }

        return DocsTrialCheck.Unknown;
    }

    /// <summary>
    /// Returns the last answer of the editing service the installation knows, also one past its duration that the
    /// fail-safe still keeps, without asking the service; <c>null</c> when none is known, also when the cache cannot be
    /// read (logged as a warning).
    /// </summary>
    public async Task<DocsTrialCheck> GetLastKnownAsync()
    {
        try
        {
            var known = await cache.TryGetAsync<DocsTrialCheck>(GetCacheKey(), entry => entry.SetAllowStaleOnReadOnly());

            return known.HasValue && known.Value is { State: not DocsTrialState.Unknown } ? known.Value : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.WarningDocsTrialCache(ex);

            return null;
        }
    }

    private FusionCacheEntryOptions SetupEntry(FusionCacheEntryOptions entry)
    {
        return entry
            .SetDuration(options.Duration)
            .SetFailSafe(true, options.FailSafeMaxDuration, options.FailureDuration);
    }
}
