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
/// The request parameters for creating the webhook configuration.
/// </summary>
/// <example>
/// {
///   "name": "Production Webhook",
///   "uri": "https://example.com/webhook",
///   "secretKey": "my-secret-key-123",
///   "enabled": true,
///   "sSL": true,
///   "triggers": {},
///   "targetId": "00000000-0000-0000-0000-000000000001"
/// }
/// </example>
public class CreateWebhooksConfigRequestsDto
{
    /// <summary>
    /// The human-readable name of the webhook configuration.
    /// </summary>
    /// <example>Production Webhook</example>
    [StringLength(50)]
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// The destination URL where the webhook events will be sent.
    /// </summary>
    /// <example>https://example.com/webhook</example>
    [Required]
    public string Uri { get; set; }

    /// <summary>
    /// The webhook secret key used to sign the webhook payloads for the security verification.
    /// </summary>
    /// <example>my-secret-key-123</example>
    [StringLength(50)]
    public string SecretKey { get; set; }

    /// <summary>
    /// Specifies whether the webhook configuration is active or not.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// Specifies whether the SSL certificate verification is required or not.
    /// </summary>
    /// <example>true</example>
    public bool SSL { get; set; }

    /// <summary>
    /// Defines which events will trigger webhook notifications.
    /// </summary>
    /// <example>{}</example>
    public WebhookTrigger Triggers { get; set; }

    /// <summary>
    /// Target ID
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string TargetId { get; set; }
}

/// <summary>
/// The request parameters for updating the webhook configuration.
/// </summary>
public class UpdateWebhooksConfigRequestsDto : CreateWebhooksConfigRequestsDto
{
    /// <summary>
    /// The webhook configuration ID.
    /// </summary>
    public required int Id { get; set; }
}