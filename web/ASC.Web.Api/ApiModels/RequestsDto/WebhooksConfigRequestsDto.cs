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
    /// <example>0</example>
    public WebhookTrigger Triggers { get; set; }

    /// <summary>
    /// Target ID
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    [StringLength(255)]
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
    /// <example>1</example>
    public required int Id { get; set; }
}