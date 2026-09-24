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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The addresses allowed to reach the portal, and whether the restriction is enforced.
/// </summary>
public class IpRestrictionsDto
{
    /// <summary>
    /// The allowed addresses, each entry pairing a single IPv4 or IPv6 address with the flag that limits it to
    /// administrators. This is the whole list that is to hold afterwards: entries not repeated here are deleted.
    /// Ranges written as `from-to` and CIDR blocks are refused with 400, even though the portal matches such forms
    /// when they are already stored. Enforcement spares only the portal owner and the installation own networks, so
    /// a list without the caller address locks the remaining administrators out.
    /// </summary>
    /// <example>[{ "ip": "192.0.2.1", "forAdmin": false }]</example>
    public required IEnumerable<IpRestrictionBase> IpRestrictions { get; set; }

    /// <summary>
    /// Whether the list is enforced. Leaving it out follows the list - on when addresses are sent, off when the list
    /// is empty - and sending `true` with an empty list is refused with 400, since that would admit nobody.
    /// </summary>
    /// <example>true</example>
    public bool? Enable { get; set; }
}
