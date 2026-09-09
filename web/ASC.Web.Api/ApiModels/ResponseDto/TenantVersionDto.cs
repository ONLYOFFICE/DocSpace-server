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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The portal versions the installation offers, and the one this portal is pinned to.
/// </summary>
/// <example>
/// {
///   "current": 1,
///   "versions": [{"id": 1, "version": "2.0"}]
/// }
/// </example>
public class TenantVersionDto(int version, IEnumerable<TenantVersion> tenantVersions)
{
    /// <summary>
    /// The `id` of the entry in `versions` this portal currently runs on. It is `0` on a portal that was never
    /// pinned to a version, in which case no entry matches it.
    /// </summary>
    /// <example>1</example>
    public int Current { get; set; } = version;

    /// <summary>
    /// The versions the installation makes available, each with the `id` to pin a portal to and the version
    /// string to show. It is empty on an installation that offers no choice, which is the usual case.
    /// </summary>
    /// <example>[{"id": 1, "version": "2.0"}]</example>
    public IEnumerable<TenantVersion> Versions { get; set; } = tenantVersions;
}