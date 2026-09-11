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
/// One event a webhook can listen to, with the bit that selects it and whether the caller may subscribe to it.
/// </summary>
/// <example>
/// {
///   "name": "file.created",
///   "id": 128,
///   "available": true
/// }
/// </example>
public class WebhookTriggerDto
{
    /// <summary>
    /// The event name exactly as it appears in a delivered payload, so a receiver can match on it. The entry
    /// named `*` is not an event but the catch-all.
    /// </summary>
    /// <example>file.created</example>
    public string Name { get; set; }

    /// <summary>
    /// The bit that stands for this event in the `triggers` bitmask of a subscription. Add the bits of the wanted
    /// events together; the catch-all entry has the value `0` and is used on its own rather than added to
    /// anything.
    /// </summary>
    /// <example>128</example>
    public long Id { get; set; }

    /// <summary>
    /// Whether the caller's own role may subscribe to this event - a plain member cannot subscribe to user, group
    /// or room creation, where a room administrator can. An unavailable event is listed all the same, and sending
    /// its bit to `POST api/2.0/settings/webhook` is refused as an invalid request.
    /// </summary>
    /// <example>true</example>
    public bool Available { get; set; }
}
