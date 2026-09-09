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
/// The ways this installation can deliver a notification, and whether each of them is usable.
/// </summary>
/// <example>
/// {
///   "channels": [{"name": "email.sender", "isEnabled": true}]
/// }
/// </example>
public class NotificationChannelStatusDto
{
    /// <summary>
    /// The channels the running installation is configured with. A channel appears only when the notification
    /// service names a sender for it, so the list can be shorter than the channels this build implements, and an
    /// empty list means the configuration names none of them.
    /// </summary>
    /// <example>[{"name": "email.sender", "isEnabled": true}]</example>
    public List<NotificationChannelDto> Channels { get; set; } = [];
}

/// <summary>
/// One delivery channel of the installation, with the state it is in for this portal.
/// </summary>
public class NotificationChannelDto
{
    /// <summary>
    /// The internal name of the channel as the notification service knows it - `email.sender` for letters,
    /// `telegram.sender` for Telegram messages. It is a key to match on, not a label to print.
    /// </summary>
    /// <example>email.sender</example>
    public required string Name { get; set; }

    /// <summary>
    /// Whether the channel can deliver for this portal. Letters are enabled whenever the channel is listed at
    /// all, while Telegram is enabled only while the portal has a bot name and token stored. It says nothing
    /// about the caller, who also has to connect their own Telegram account through
    /// `GET api/2.0/settings/telegram/link`.
    /// </summary>
    /// <example>true</example>
    public required bool IsEnabled { get; set; }
}