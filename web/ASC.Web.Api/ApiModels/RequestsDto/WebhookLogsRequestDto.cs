﻿// (c) Copyright Ascensio System SIA 2009-2024
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

public class WebhookLogsRequestDto
{
    /// <summary>
    /// Delivey start time
    /// </summary>
    [FromQuery(Name = "deliveryFrom")]

    public DateTime? DeliveryFrom { get; set; }

    /// <summary>
    /// Delivey end time
    /// </summary>
    [FromQuery(Name = "deliveryTo")]
    public DateTime? DeliveryTo {  get; set; }

    /// <summary>
    /// Hook URI
    /// </summary>
    [FromQuery(Name = "hookUri")]
    public string HookUri { get; set; }

    /// <summary>
    /// Webhook ID
    /// </summary>
    [FromQuery(Name = "webhookId")]
    public int? WebhookId {  get; set; }

    /// <summary>
    /// Config ID
    /// </summary>
    [FromQuery(Name = "configId")]
    public int? ConfigId {  get; set; }

    /// <summary>
    /// Event ID
    /// </summary>
    [FromQuery(Name = "eventId")]
    public int? EventId { get; set; }

    /// <summary>
    /// Webhook group status
    /// </summary>
    [FromQuery(Name = "groupStatus")]
    public WebhookGroupStatus? GroupStatus { get; set; }
}
