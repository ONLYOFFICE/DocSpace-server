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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// One delivery of a webhook event, updated in place by its automatic retries: what was sent where, and what came
/// back the last time. A manual retry is a delivery record of its own for the same event.
/// </summary>
/// <example>
/// {
///   "id": 1,
///   "eventId": 1,
///   "retriedBy": null,
///   "configName": "Room activity",
///   "trigger": 128,
///   "creationTime": "2024-01-15T10:30:00Z",
///   "method": "POST",
///   "route": "https://example.com/hooks/docspace",
///   "requestHeaders": "{\"x-docspace-signature\":\"9f86d081884c7d65\"}",
///   "requestPayload": "{\"id\":42,\"title\":\"report.docx\"}",
///   "responseHeaders": "{\"content-type\":\"application/json\"}",
///   "responsePayload": "{\"ok\":true}",
///   "status": 200,
///   "delivery": "2024-01-15T10:30:00Z",
///   "attempts": 1,
///   "nextAttemptOn": null
/// }
/// </example>
public class WebhooksLogDto
{
    /// <summary>
    /// The identifier of this delivery record, which is what `PUT api/2.0/settings/webhook/{id}/retry` re-sends.
    /// The automatic retries of the delivery update this same record, while a manual retry gets a record and an
    /// identifier of its own.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }

    /// <summary>
    /// The identifier of the event the delivery is for, which the receiver sees as `event.id` and in the
    /// `x-docspace-event-id` header. It is the `id` of the event's first delivery record, so it equals `id` there and
    /// stays the same on every manual retry, which makes it the value to group the records of one event by.
    /// </summary>
    /// <example>1</example>
    public int EventId { get; set; }

    /// <summary>
    /// The member who asked for this delivery with a manual retry. It is empty on the first delivery of an event,
    /// which the portal sends by itself.
    /// </summary>
    /// <example>{ "displayName": "Mike Zanyatski" }</example>
    public EmployeeDto RetriedBy { get; set; }

    /// <summary>
    /// The name of the subscription the delivery belongs to. It is the name as it stands now, so it follows a
    /// later rename of the subscription rather than recording what it was called at the time.
    /// </summary>
    /// <example>Room activity</example>
    public string ConfigName { get; set; }

    /// <summary>
    /// The event that caused the delivery, as a single bit rather than a mask - a delivery is always for one
    /// event, even though a subscription covers several.
    /// </summary>
    /// <example>128</example>
    public WebhookTrigger Trigger { get; set; }

    /// <summary>
    /// When this delivery record was queued, as a UTC instant ending in `Z` - unlike the dates of the subscription
    /// itself, which carry the offset of the portal time zone. For the first delivery that is when the event happened; for a manual retry it is the
    /// moment of the retry. Records come back newest first by this moment.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// The HTTP method the delivery was sent with, which is `POST` for every webhook the portal sends.
    /// </summary>
    /// <example>POST</example>
    public string Method { get; set; }

    /// <summary>
    /// The address the delivery was sent to, which is the subscription's URL as it stood at the time - so an
    /// older record can name an address the subscription no longer uses.
    /// </summary>
    /// <example>https://example.com/hooks/docspace</example>
    public string Route { get; set; }

    /// <summary>
    /// The headers the portal sent, serialised as one string, including the signature header a receiver verifies
    /// the payload with.
    /// </summary>
    /// <example>{"x-docspace-signature":"9f86d081884c7d65"}</example>
    public string RequestHeaders { get; set; }

    /// <summary>
    /// The body the portal sent, which is the event payload as JSON text. It is stored as it was sent, so it
    /// still describes the entity as it looked at the time of the event.
    /// </summary>
    /// <example>{"id":42,"title":"report.docx"}</example>
    public string RequestPayload { get; set; }

    /// <summary>
    /// The headers the target answered with on the latest attempt, serialised the same way as `requestHeaders`. It
    /// is empty until an attempt is answered, and when the target was not reached.
    /// </summary>
    /// <example>{"content-type":"application/json"}</example>
    public string ResponseHeaders { get; set; }

    /// <summary>
    /// The body the target answered with on the latest attempt, truncated for storage. Empty under the same
    /// conditions as `responseHeaders`, and also for a target that answers with no body at all. A record the portal
    /// stopped without sending holds the portal's own note here instead - a text that starts with `Not sent:` and
    /// gives the reason - which is not an answer of the target: on a record that never went out `status` is then `0`
    /// and `responseHeaders` is empty, while on one stopped after failed attempts they still describe the last of them.
    /// </summary>
    /// <example>{"ok":true}</example>
    public string ResponsePayload { get; set; }

    /// <summary>
    /// The HTTP status code the target answered on the latest attempt. It is `0` while the first attempt is still
    /// on its way and on one that never reached the target, so `0` is not a failure code - it is the absence of an
    /// answer. While `nextAttemptOn` is set it is the status of the failed attempt that caused the retry.
    /// </summary>
    /// <example>200</example>
    public int Status { get; set; }

    /// <summary>
    /// When the successful answer came back, as a UTC instant ending in `Z` like `creationTime`. It stays empty until an
    /// attempt succeeds, and for good once the portal has given up retrying.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? Delivery { get; set; }

    /// <summary>
    /// How many times the portal has tried to deliver this record so far. A failed attempt is retried with
    /// growing pauses - on the default schedule about 1 minute, 5 minutes, 30 minutes, 2, 5 and 10 hours, though
    /// an installation may configure its own - all of them under the same record and the same
    /// `x-docspace-event-id` header, so a receiver can drop a delivery it has already processed.
    /// </summary>
    /// <example>1</example>
    public int Attempts { get; set; }

    /// <summary>
    /// When the next attempt is due, as a UTC instant ending in `Z` like `creationTime`. While an attempt is being sent
    /// right now it holds the moment that attempt's lease expires, a few minutes ahead, rather than a scheduled retry;
    /// it changes again once the attempt ends. Empty means no attempt will follow: the record is delivered, its retries
    /// ran out, a manual retry of the same event took over from it, or the portal stopped it - because the subscription
    /// was disabled or removed or its address rejected, the subscription's creator may no longer receive the event,
    /// the stored body cannot be read or is in the payload format of a previous version, the portal is removed, the
    /// record waited longer than the retry schedule allows, or the portal was restored from a backup taken before the
    /// record was answered. A stopped record that was never answered says why in `responsePayload`.
    /// </summary>
    /// <example>2024-01-15T10:35:00Z</example>
    public DateTime? NextAttemptOn { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class WebhooksLogDtoMapper(EmployeeDtoHelper employeeDtoHelper)
{
    public async Task<WebhooksLogDto> MapAsync(DbWebhooksLog source)
    {
        var result = Map(source);

        result.RetriedBy = source.RetriedBy.HasValue ? await employeeDtoHelper.GetAsync(source.RetriedBy.Value) : null;

        return result;
    }

    [MapperIgnoreTarget(nameof(WebhooksLogDto.RetriedBy))]
    private partial WebhooksLogDto Map(DbWebhooksLog source);

    // the journal stores UTC instants, which a datetime column reads back as Unspecified: mark them, do not convert
    private static DateTime MapDateToUtc(DateTime source) => DateTime.SpecifyKind(source, DateTimeKind.Utc);
}