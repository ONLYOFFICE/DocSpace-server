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
/// The default storage limit given to newly created users, rooms or AI agents, and whether it is enforced.
/// </summary>
/// <example>
/// {
///   "enableQuota": true
/// }
/// </example>
public class QuotaSettingsRequestsDto
{
    /// <summary>
    /// Whether the limit is enforced at all. While it is false the size is ignored and nothing created afterwards
    /// carries a limit; objects that already have one keep it either way.
    /// </summary>
    /// <example>true</example>
    public bool EnableQuota { get; set; }

    /// <summary>
    /// The starting limit, in bytes, written as a JSON number. It has to parse as a whole number and may not exceed
    /// the portal total storage quota, nor, on a self-hosted installation with a portal-wide quota switched on, that
    /// quota; anything larger is refused with 400. It is applied to objects created from now on and leaves the
    /// limits of existing ones as they are.
    /// </summary>
    /// <example>1073741824</example>
    public required JsonElement DefaultQuota { get; set; }
}
