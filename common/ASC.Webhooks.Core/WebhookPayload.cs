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

/// <summary>
/// The body of every delivery. `payload` is narrowed by `event.trigger` per x-docspace-trigger-payloads.
/// </summary>
public class WebhookPayload<T1, T2>
{
    /// <summary>
    /// What happened and when.
    /// </summary>
    public WebhookPayloadEventInfo Event { get; set; }

    /// <summary>
    /// The trigger-specific body. Left untyped so that codegen stays clean in all 9 target languages; the runtime
    /// narrows it.
    /// </summary>
    public T1 Payload { get; set; }

    /// <summary>
    /// The subscription the delivery is for.
    /// </summary>
    public WebhookPayloadConfigInfo<T2> Webhook { get; set; }

    public WebhookPayload()
    {
    }

    public WebhookPayload(WebhookTrigger trigger, DbWebhooksConfig config, T1 data, T2 dataId, Guid userId)
    {
        var now = GetShortUtcNow();

        Event = new WebhookPayloadEventInfo
        {
            CreateBy = userId,
            CreateOn = now,
            Id = 0, // log Id is unknown until saved. initialized on send
            Trigger = trigger.ToCustomString(),
            TriggerId = (long)trigger
        };

        Payload = data;

        var triggers = config.Triggers == WebhookTrigger.All
            ? [config.Triggers.ToCustomString()]
            : Enum.GetValues<WebhookTrigger>()
                .Where(flag => config.Triggers.HasFlag(flag) && flag != 0)
                .Select(flag => flag.ToCustomString())
                .ToArray();

        var target = string.IsNullOrEmpty(config.TargetId)
            ? null
            : new WebhookPayloadTargetInfo<T2>
            {
                Id = dataId,
                Type = trigger.GetTargetType()
            };

        Webhook = new WebhookPayloadConfigInfo<T2>
        {
            Id = config.Id,
            Name = config.Name,
            Url = config.Uri,
            Triggers = triggers,
            Target = target

            // initialized on send: LastFailureOn, LastFailureContent, LastSuccessOn, RetryCount, RetryOn
        };
    }

    public DateTime GetShortUtcNow()
    {
        var now = DateTime.UtcNow;
        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
        return now;
    }
}

/// <summary>
/// The event a delivery reports.
/// </summary>
public class WebhookPayloadEventInfo
{
    /// <summary>
    /// The ID of the first delivery-log record of the event, and the idempotency key. It is stable across every
    /// resend of one event, the server's automatic retries and a manual retry from the admin UI alike: a manual
    /// retry is a delivery-log record of its own, but it still sends the ID of the first one. The same ID therefore
    /// always means the same logical event. It is also echoed in the x-docspace-event-id header.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// When the event happened, in UTC and whole seconds. Echoed in the x-docspace-event-timestamp header, but that
    /// header is not signed, so replay defence must read this value from the verified body.
    /// </summary>
    public DateTime CreateOn { get; set; }

    /// <summary>
    /// The ID of the user whose action caused the event. Omitted when it is the empty GUID, as for background jobs.
    /// </summary>
    public Guid CreateBy { get; set; }

    /// <summary>
    /// The trigger name, e.g. "file.created". "*" only in the legacy-payload fallback.
    /// </summary>
    public string Trigger { get; set; }

    /// <summary>
    /// The bit value of the trigger. Omitted when 0, the "*" fallback.
    /// </summary>
    public long TriggerId { get; set; }
}

/// <summary>
/// The entity the subscription is scoped to. Present only when the subscription is scoped to one entity.
/// </summary>
public class WebhookPayloadTargetInfo<T>
{
    /// <summary>
    /// The ID of the entity.
    /// </summary>
    public T Id { get; set; }

    /// <summary>
    /// The prefix of the trigger the delivery is for: user, group, file, folder, room, form or agent today.
    /// Deliberately not an enum: every new trigger family adds a value, and a receiver built against an older
    /// contract must keep parsing it.
    /// </summary>
    public string Type { get; set; }
}

/// <summary>
/// The subscription, plus its delivery state at send time.
/// </summary>
public class WebhookPayloadConfigInfo<T2>
{
    /// <summary>
    /// The subscription ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The subscription name given by its creator.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The URL the delivery is posted to.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// The subscribed events, or ["*"] for the catch-all.
    /// </summary>
    public string[] Triggers { get; set; }

    /// <summary>
    /// The entity the subscription is scoped to; absent for a subscription to every entity.
    /// </summary>
    public WebhookPayloadTargetInfo<T2> Target { get; set; }

    /// <summary>
    /// When the last delivery to this subscription failed, in UTC; absent when none has failed.
    /// </summary>
    public DateTime? LastFailureOn { get; set; }

    /// <summary>
    /// What the receiver answered, or the network error, when the last delivery to this subscription failed.
    /// </summary>
    public string LastFailureContent { get; set; }

    /// <summary>
    /// When the last delivery to this subscription succeeded, in UTC; absent when none has succeeded yet.
    /// </summary>
    public DateTime? LastSuccessOn { get; set; }

    /// <summary>
    /// How many times this delivery has been retried. Omitted on the first attempt (0); present from attempt 2.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// When the next retry is scheduled, in UTC; absent when no retry is pending.
    /// </summary>
    public DateTime? RetryOn { get; set; }
}
