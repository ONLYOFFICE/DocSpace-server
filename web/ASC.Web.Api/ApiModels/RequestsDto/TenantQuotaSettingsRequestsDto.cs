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
/// The storage limit set on one tenant of a self-hosted installation.
/// </summary>
/// <example>
/// {
///   "quota": 1
/// }
/// </example>
public class TenantQuotaSettingsRequestsDto
{
    /// <summary>
    /// The tenant the limit applies to, by tenant ID. Only a self-hosted installation has more than one, which is
    /// why the operation is refused on SaaS.
    /// </summary>
    /// <example>1</example>
    public required int TenantId { get; set; }

    /// <summary>
    /// The limit in bytes. A negative value is not a smaller limit but the absence of one: it removes whatever limit
    /// the tenant had. The value is a ceiling on stored data and says nothing about how much of it is already used.
    /// </summary>
    /// <example>1048576</example>
    public long Quota { get; set; }
}
