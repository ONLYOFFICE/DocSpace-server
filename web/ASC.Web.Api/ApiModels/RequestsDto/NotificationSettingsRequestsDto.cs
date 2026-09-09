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
/// Which kind of notification the calling user switches, and which way.
/// </summary>
/// <example>
/// {
///   "type": 0,
///   "isEnabled": true
/// }
/// </example>
public class NotificationSettingsRequestsDto
{
    /// <summary>
    /// The kind of notification being switched. A value outside the defined set is echoed back while nothing is
    /// stored, so confirm the result with `GET api/2.0/settings/notification/{type}` rather than trusting the
    /// answer.
    /// </summary>
    /// <example>0</example>
    public required NotificationType Type { get; set; }

    /// <summary>
    /// Whether that kind reaches the calling account. It applies to the caller own account alone and to every room
    /// at once; a single room is silenced with `POST api/2.0/settings/notification/rooms` instead.
    /// </summary>
    /// <example>true</example>
    public bool IsEnabled { get; set; }
}


/// <summary>
/// Which kind of notification is read for the calling user.
/// </summary>
public class NotificationTypeRequestsDto
{
    /// <summary>
    /// The kind of notification being asked about. A value outside the defined set fails the call rather than
    /// falling back to a default.
    /// </summary>
    /// <example>0</example>
    [FromRoute(Name = "type")]
    public required NotificationType Type { get; set; }
}
