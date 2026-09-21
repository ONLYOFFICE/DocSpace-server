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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The filters that narrow the webhook delivery log, and the window of the page returned from it.
/// </summary>
/// <example>
/// {
///   "deliveryFrom": "2024-01-15T10:30:00Z",
///   "deliveryTo": "2024-01-15T10:30:00Z",
///   "hookUri": "https://example.com/webhook",
///   "configId": 1,
///   "eventId": 1,
///   "groupStatus": "EnumValue",
///   "userId": {},
///   "trigger": {},
///   "count": 1,
///   "startIndex": 1
/// }
/// </example>
public class WebhookLogsRequestDto
{
    /// <summary>
    /// The earliest delivery moment a record may carry. Records of attempts still on their way have no delivery
    /// moment yet and fall outside any bound set here.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [FromQuery(Name = "deliveryFrom")]
    public DateTime? DeliveryFrom { get; set; }

    /// <summary>
    /// The latest delivery moment a record may carry. All the filters combine with AND, so it narrows whatever the
    /// other ones already kept.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [FromQuery(Name = "deliveryTo")]
    public DateTime? DeliveryTo { get; set; }

    /// <summary>
    /// The subscription target address, matched in full rather than as a prefix. Filtering by `configId` is the
    /// reliable way to pick one subscription, since several may share an address.
    /// </summary>
    /// <example>https://example.com/webhook</example>
    [FromQuery(Name = "hookUri")]
    public string HookUri { get; set; }

    /// <summary>
    /// The subscription whose deliveries are kept, by the `id` that `GET api/2.0/settings/webhook` reports.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "configId")]
    public int? ConfigId { get; set; }

    /// <summary>
    /// A single delivery record, by its own identifier. It narrows the answer to that one record, which is how a
    /// client follows up a retry it queued earlier.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "eventId")]
    public int? EventId { get; set; }

    /// <summary>
    /// The classes of answered status to keep, as a bitmask; 0 keeps every record whatever the target answered.
    /// </summary>
    /// <example>NotSent</example>
    [FromQuery(Name = "groupStatus")]
    public WebhookGroupStatus? GroupStatus { get; set; }

    /// <summary>
    /// The member whose subscriptions the records belong to, by portal user ID - who created the subscription, not
    /// who caused the event. For a caller who is not a DocSpace administrator it is overwritten with the caller own
    /// ID, so such a caller never sees another member deliveries whatever is sent here.
    /// </summary>
    /// <example>{}</example>
    [FromQuery(Name = "userId")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// The single event kind to keep; 0 keeps every kind. It names one trigger rather than a mask of several, unlike
    /// the `triggers` a subscription is created with.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "trigger")]
    public WebhookTrigger? Trigger { get; set; }

    /// <summary>
    /// How many records one page may hold. The maximum is also the default, so a client that wants shorter pages has
    /// to ask for them; the number of records matching the filter comes back as `total` beside the page.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// How many matching records to skip before the page begins, counting from the newest. Advance it by `count` to
    /// walk back through the log.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }
}
