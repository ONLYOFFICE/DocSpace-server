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
/// The target a webhook subscription calls, the events it listens for, and the secret it signs with.
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
    /// The label the subscription is listed under. It is for the administrator reading the list and is never sent to
    /// the target; it does not have to be unique.
    /// </summary>
    /// <example>Production Webhook</example>
    [StringLength(50)]
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// The address the portal posts the event payload to. It has to be an absolute `http` or `https` address outside
    /// the installation own network, and it is probed before anything is stored: it must answer a HEAD request with
    /// a success code, and a redirect does not count as one.
    /// </summary>
    /// <example>https://example.com/webhook</example>
    [Required]
    public string Uri { get; set; }

    /// <summary>
    /// The shared secret the payload signature is computed with, so the receiver can tell a genuine call from a
    /// forged one. It has to satisfy the portal password rules published by
    /// `GET api/2.0/settings/security/password`, and it is never echoed back by any operation. On an update an empty
    /// value keeps the secret already stored.
    /// </summary>
    /// <example>my-secret-key-123</example>
    [StringLength(50)]
    public string SecretKey { get; set; }

    /// <summary>
    /// Whether the subscription delivers at all. While it is off the matching events are dropped rather than queued,
    /// so nothing from that period arrives once it is switched on again.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether the target certificate is verified. Setting it demands an `https` target with a valid certificate;
    /// leaving it off delivers without checking the certificate at all.
    /// </summary>
    /// <example>true</example>
    public bool SSL { get; set; }

    /// <summary>
    /// The events the subscription listens for, as a bitmask combining the flags; 0 subscribes to all of them. Take
    /// the flags the caller role is allowed to use from `GET api/2.0/settings/webhook/triggers`, since a flag beyond
    /// that set is refused with 400. A subscription still only fires for events its creator may see.
    /// </summary>
    /// <example>0</example>
    public WebhookTrigger Triggers { get; set; }

    /// <summary>
    /// The single entity the subscription is narrowed to, by its identifier - a room or a file, for instance.
    /// Leaving it out delivers events about every entity the subscribed triggers cover.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    [StringLength(255)]
    public string TargetId { get; set; }
}

/// <summary>
/// The webhook subscription being changed, with the parameters it is to have afterwards.
/// </summary>
public class UpdateWebhooksConfigRequestsDto : CreateWebhooksConfigRequestsDto
{
    /// <summary>
    /// The subscription to act on, by the `id` that `GET api/2.0/settings/webhook` reports. It travels in the body
    /// rather than in the path, and an id that exists in no portal subscription answers 404.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }
}
