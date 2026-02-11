// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
    /// <example>EnumValue</example>
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
    /// <example>{}</example>
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