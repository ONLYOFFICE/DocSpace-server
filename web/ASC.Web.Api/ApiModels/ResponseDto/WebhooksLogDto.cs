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
/// One delivery attempt of a webhook: what was sent where, and what came back.
/// </summary>
/// <example>
/// {
///   "id": 1,
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
///   "delivery": "2024-01-15T10:30:00Z"
/// }
/// </example>
public class WebhooksLogDto
{
    /// <summary>
    /// The identifier of this attempt, which is what the `eventId` filter of
    /// `GET api/2.0/settings/webhooks/log` picks one record by and what
    /// `PUT api/2.0/settings/webhook/{id}/retry` re-sends. A retry produces a new record with a new identifier
    /// and leaves this one as it is.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }

    /// <summary>
    /// The name of the subscription the attempt belongs to. It is the name as it stands now, so it follows a
    /// later rename of the subscription rather than recording what it was called at the time.
    /// </summary>
    /// <example>Room activity</example>
    public string ConfigName { get; set; }

    /// <summary>
    /// The event that caused the attempt, as a single bit rather than a mask - a delivery is always for one
    /// event, even though a subscription covers several.
    /// </summary>
    /// <example>128</example>
    public WebhookTrigger Trigger { get; set; }

    /// <summary>
    /// When the attempt was queued, as a UTC instant - unlike the dates of the subscription itself, which come
    /// in the portal time zone. Records come back newest first by this moment.
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
    /// The headers the target answered with, serialised the same way as `requestHeaders`. It is empty while the
    /// attempt is still on its way and on an attempt that never reached the target.
    /// </summary>
    /// <example>{"content-type":"application/json"}</example>
    public string ResponseHeaders { get; set; }

    /// <summary>
    /// The body the target answered with, truncated for storage. Empty under the same conditions as
    /// `responseHeaders`, and also for a target that answers with no body at all.
    /// </summary>
    /// <example>{"ok":true}</example>
    public string ResponsePayload { get; set; }

    /// <summary>
    /// The HTTP status code the target answered. It is `0` while the attempt is still on its way and on one that
    /// never reached the target, so `0` is not a failure code - it is the absence of an answer.
    /// </summary>
    /// <example>200</example>
    public int Status { get; set; }

    /// <summary>
    /// When the answer came back, as a UTC instant like `creationTime`. It is empty while the attempt is still on
    /// its way, which together with `status` is how a pending record is told from a finished one.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? Delivery { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class WebhooksLogDtoMapper(TenantUtil tenantUtil)
{
    public partial WebhooksLogDto Map(DbWebhooksLog source);

    private DateTime MapDateToUtc(DateTime source) => tenantUtil.DateTimeToUtc(source);
}