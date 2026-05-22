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
/// The request parameters for querying the webhook delivery logs with various filter criteria.
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
    /// The delivery start time for filtering webhook logs.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [FromQuery(Name = "deliveryFrom")]
    public DateTime? DeliveryFrom { get; set; }

    /// <summary>
    /// The delivery end time for filtering webhook logs.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [FromQuery(Name = "deliveryTo")]
    public DateTime? DeliveryTo { get; set; }

    /// <summary>
    /// The destination URL where webhooks are delivered.
    /// </summary>
    /// <example>https://example.com/webhook</example>
    [FromQuery(Name = "hookUri")]
    public string HookUri { get; set; }

    /// <summary>
    /// The webhook configuration identifier.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "configId")]
    public int? ConfigId { get; set; }

    /// <summary>
    /// The unique identifier of the event that triggered the webhook.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "eventId")]
    public int? EventId { get; set; }

    /// <summary>
    /// The status of the webhook delivery group.
    /// </summary>
    /// <example>NotSent</example>
    [FromQuery(Name = "groupStatus")]
    public WebhookGroupStatus? GroupStatus { get; set; }

    /// <summary>
    /// The identifier of the user associated with the webhook event.
    /// </summary>
    /// <example>{}</example>
    [FromQuery(Name = "userId")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// The type of event that triggered the webhook.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "trigger")]
    public WebhookTrigger? Trigger { get; set; }

    /// <summary>
    /// The maximum number of webhook log records to return in the query response.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// Specifies the starting index for retrieving webhook logs.
    /// Used for pagination in the webhook delivery log queries.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }
}