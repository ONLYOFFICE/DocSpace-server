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
/// Which single room the calling user silences, and which way.
/// </summary>
/// <example>
/// {
///   "roomsId": {},
///   "mute": true
/// }
/// </example>
public class RoomsNotificationsSettingsRequestDto
{
    /// <summary>
    /// The room to act on. It is kept as an opaque value, so both the numeric identifier of a portal room and the
    /// string identifier of a room on a connected third-party account are accepted; neither the room existence nor
    /// the caller access to it is checked, and a mistyped identifier is stored as sent. One call carries one room.
    /// </summary>
    /// <example>{}</example>
    public object RoomsId { get; set; }

    /// <summary>
    /// Which way the room goes: `true` adds it to the caller silenced list, `false` takes it off again. While a room
    /// is silenced its activity is left out of the hourly and daily digests, the letters it would send at once are
    /// not sent, and its new-item counters are hidden.
    /// </summary>
    /// <example>true</example>
    public bool Mute { get; set; }
}
