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

namespace ASC.Webhooks;

[Singleton]
public class Settings
{
    public Settings()
    {
    }

    public Settings(IConfiguration configuration, ILogger<Settings> logger)
    {
        var cfg = configuration.GetSection("webhooks").Get<Settings>();

        // the in-process retries it counted were replaced by the persisted schedule of webhooks:retryIntervals
        if (configuration["webhooks:repeatcount"] != null)
        {
            logger.WarningObsoleteRepeatCount();
        }

        RetryIntervals = cfg.RetryIntervals is { Length: > 0 } ? cfg.RetryIntervals : [.. WebhookDelivery.DefaultRetryIntervals];

        // an interval of zero or less, or one of a few milliseconds, schedules the retry at once, so a failing event
        // would use up its attempts back to back, and then disable its subscription; a negative one also shortens the
        // retry window the unsent rows are stopped by
        var shortInterval = Array.FindIndex(RetryIntervals, i => i < MinRetryInterval);
        if (shortInterval >= 0)
        {
            logger.WarningRetryIntervalTooShort(RetryIntervals[shortInterval], MinRetryInterval);
            RetryIntervals = [.. WebhookDelivery.DefaultRetryIntervals];
        }

        // a huge interval overflows the date of the next attempt, which is computed after the request went out: the
        // attempt would then never be recorded, and the row sent again after every lease, for ever
        var longInterval = Array.FindIndex(RetryIntervals, i => i > MaxRetryInterval);
        if (longInterval >= 0)
        {
            logger.WarningRetryIntervalTooLong(RetryIntervals[longInterval], MaxRetryInterval);
            RetryIntervals = [.. WebhookDelivery.DefaultRetryIntervals];
        }

        // a parallel loop of no threads throws, which would stop the delivery of every webhook; a huge one makes every
        // poll walk that many slots and claim with an IN list of that size
        ThreadCount = cfg.ThreadCount is > 0 ? cfg.ThreadCount : 10;
        if (ThreadCount > MaxThreadCount)
        {
            logger.WarningValueTooLarge("webhooks:threadCount", ThreadCount.Value, MaxThreadCount);
            ThreadCount = MaxThreadCount;
        }

        // 0 or less would disable a subscription the first time a delivery uses up its retries, even one that
        // succeeded a minute earlier; a huge value overflows the date the check compares with
        TrustedDaysCount = cfg.TrustedDaysCount ?? WebhookDelivery.DefaultTrustedDays;
        if (TrustedDaysCount < 1)
        {
            logger.WarningValueNotPositive(WebhookDelivery.TrustedDaysKey, TrustedDaysCount.Value, WebhookDelivery.DefaultTrustedDays);
            TrustedDaysCount = WebhookDelivery.DefaultTrustedDays;
        }
        else if (TrustedDaysCount > MaxDays)
        {
            logger.WarningValueTooLarge(WebhookDelivery.TrustedDaysKey, TrustedDaysCount.Value, MaxDays);
            TrustedDaysCount = MaxDays;
        }

        Blacklist = cfg.Blacklist ?? [];
        RequestTimeout = cfg.RequestTimeout ?? WebhookDelivery.DefaultRequestTimeout;

        // a deadline of a few milliseconds times out every attempt before any target can answer, so every event would
        // use up its retries and every subscription of every portal would be disabled
        if (RequestTimeout < MinRequestTimeout)
        {
            logger.WarningRequestTimeoutTooShort(RequestTimeout.Value, MinRequestTimeout, WebhookDelivery.DefaultRequestTimeout);
            RequestTimeout = WebhookDelivery.DefaultRequestTimeout;
        }

        // the lease of a claimed batch is derived from the request timeout (WorkerService), so an unbounded value would
        // keep the rows of an instance that died mid-send, and every manual retry of their events, waiting for as long
        if (RequestTimeout > MaxRequestTimeout)
        {
            logger.WarningRequestTimeoutTooLong(RequestTimeout.Value, MaxRequestTimeout);
            RequestTimeout = MaxRequestTimeout;
        }
        JournalRetentionDays = cfg.JournalRetentionDays is > 0 ? cfg.JournalRetentionDays : null;
        if (JournalRetentionDays > MaxDays)
        {
            logger.WarningValueTooLarge("webhooks:journalRetentionDays", JournalRetentionDays.Value, MaxDays);
            JournalRetentionDays = MaxDays;
        }
    }

    /// <summary>
    /// The shortest webhooks:retryIntervals entry taken; a list with a shorter one is replaced by the default intervals.
    /// Half a second still spaces attempts at the sender's own pace; the integration-test profile retries that fast.
    /// </summary>
    public static readonly TimeSpan MinRetryInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// The longest webhooks:retryIntervals entry taken; a list with a longer one is replaced by the default intervals.
    /// </summary>
    public static readonly TimeSpan MaxRetryInterval = TimeSpan.FromDays(7);

    /// <summary>
    /// The most delivery slots webhooks:threadCount may ask for; a larger value is cut to it.
    /// </summary>
    public const int MaxThreadCount = 100;

    /// <summary>
    /// The most days webhooks:trustedDaysCount and webhooks:journalRetentionDays may name; a larger value is cut to it.
    /// </summary>
    public const int MaxDays = 3650;

    /// <summary>
    /// The shortest webhooks:requestTimeout taken; a shorter one is replaced by the default timeout.
    /// </summary>
    public static readonly TimeSpan MinRequestTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The longest webhooks:requestTimeout taken; a longer one is cut to it.
    /// </summary>
    public static readonly TimeSpan MaxRequestTimeout = TimeSpan.FromMinutes(5);

    public TimeSpan[] RetryIntervals { get; init; }
    public int? ThreadCount { get; init; }
    public int? TrustedDaysCount { get; init; }
    public string[] Blacklist { get; init; }
    public TimeSpan? RequestTimeout { get; init; }

    /// <summary>
    /// How many days a delivery record is kept in the journal once nothing will send it any more, for every portal;
    /// unset or 0 keeps the records for ever. Each manual retry adds a record with a copy of the body, so the journal
    /// otherwise only grows.
    /// </summary>
    public int? JournalRetentionDays { get; init; }
}
