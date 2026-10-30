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

namespace ASC.Webhooks.Core;

public interface IWebhookPublisher
{
    /// <summary>
    /// Whether any active subscription of the portal listens to <paramref name="trigger"/>, from the cached list and
    /// before any access check: a caller asks it first to skip the work of building an event nobody receives.
    /// </summary>
    Task<bool> HasWebhookConfigsAsync(WebhookTrigger trigger);

    Task<List<DbWebhooksConfig>> GetWebhookConfigsAsync<TSource>(WebhookTrigger trigger, IWebhookAccessChecker<TSource> checker, TSource source);

    /// <summary>
    /// Writes the event to the journal of every subscription in <paramref name="webhookConfigs"/> and hands it to the
    /// sender. Best effort: it is called once the action the event reports has taken effect, so a failure for one
    /// subscription is logged and skipped, and nothing but a cancellation is thrown.
    /// </summary>
    Task PublishAsync<TPayload, TId>(WebhookTrigger trigger, IEnumerable<DbWebhooksConfig> webhookConfigs, TPayload payload, TId dataId);

    Task<WebhookRetryResult> RetryPublishAsync(DbWebhooksLog webhookLog);
}

public enum WebhookRetryStatus
{
    Requeued,
    NotFound,
    InProgress,

    /// <summary>
    /// The subscription is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// The event was written before the update that introduced the attempt counter; its body is in the old format.
    /// </summary>
    Outdated
}

/// <summary>
/// The outcome of a manual retry. When it was queued, <paramref name="Log"/> is the new row and
/// <paramref name="StoppedIds"/> the ids of the event's earlier rows, the original one and its earlier retries, whose
/// automatic retries still due the new row ended; a row that had none left is listed as well.
/// </summary>
public record WebhookRetryResult(WebhookRetryStatus Status, DbWebhooksLog Log = null, IReadOnlyList<int> StoppedIds = null);