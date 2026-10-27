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

namespace ASC.Webhooks.Core;

/// <summary>
/// The transport of a webhook delivery as the sender carries it out: its headers, how long it waits, when it retries.
/// The webhook contract describes the same values to receivers, so a change here reaches the published text too.
/// </summary>
public static class WebhookDelivery
{
    /// <summary>
    /// The HMAC-SHA256 of the raw body under the subscription's secret, as <c>sha256=</c> and uppercase hex. The only
    /// header that is authenticated.
    /// </summary>
    public const string SignatureHeader = "x-docspace-signature-256";

    /// <summary>
    /// A copy of <c>event.id</c>, for dropping an already-seen delivery without parsing the body.
    /// </summary>
    public const string EventIdHeader = "x-docspace-event-id";

    /// <summary>
    /// A copy of <c>event.createOn</c>, in the same ISO-8601 UTC spelling.
    /// </summary>
    public const string EventTimestampHeader = "x-docspace-event-timestamp";

    /// <summary>
    /// The configuration key that overrides <see cref="DefaultRequestTimeout"/>.
    /// </summary>
    public const string RequestTimeoutKey = "webhooks:requestTimeout";

    /// <summary>
    /// The configuration key that overrides <see cref="DefaultRetryIntervals"/>.
    /// </summary>
    public const string RetryIntervalsKey = "webhooks:retryIntervals";

    /// <summary>
    /// How far each retry interval is moved at random, as a share of the interval, either way.
    /// </summary>
    public const double RetryJitter = 0.1;

    /// <summary>
    /// How long one attempt waits for the target's answer, and then for the start of its body.
    /// </summary>
    public static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The pauses before the automatic retries of a failed delivery, one per retry; after the last one the delivery
    /// is abandoned.
    /// </summary>
    public static readonly IReadOnlyList<TimeSpan> DefaultRetryIntervals =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(5),
        TimeSpan.FromHours(10)
    ];
}
