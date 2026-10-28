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
        // a parallel loop of no threads throws, which would stop the delivery of every webhook
        ThreadCount = cfg.ThreadCount is > 0 ? cfg.ThreadCount : 10;
        TrustedDaysCount = cfg.TrustedDaysCount ?? 3;
        Blacklist = cfg.Blacklist ?? [];
        RequestTimeout = cfg.RequestTimeout is { } requestTimeout && requestTimeout > TimeSpan.Zero ? requestTimeout : WebhookDelivery.DefaultRequestTimeout;

        // the lease of a claimed batch is derived from the request timeout (WorkerService), so an unbounded value would
        // keep the rows of an instance that died mid-send, and every manual retry of their events, waiting for as long
        if (RequestTimeout > MaxRequestTimeout)
        {
            logger.WarningRequestTimeoutTooLong(RequestTimeout.Value, MaxRequestTimeout);
            RequestTimeout = MaxRequestTimeout;
        }
        JournalRetentionDays = cfg.JournalRetentionDays is > 0 ? cfg.JournalRetentionDays : null;
    }

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
