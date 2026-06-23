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

namespace ASC.Core.Tenants;

/// <summary>
/// The tenant entity quota settings.
/// </summary>
/// <example>
/// {
///   "enableQuota": true,
///   "defaultQuota": 1000,
///   "lastRecalculateDate": "2024-01-01T00:00:00Z"
/// }
/// </example>
public abstract class TenantEntityQuotaSettings
{
    /// <summary>
    /// The tenant entity without quota.
    /// </summary>
    public const int NoQuota = -1;

    /// <summary>
    /// The tenant entity default quota value.
    /// </summary>
    public const int DefaultQuotaValue = -2;

    /// <summary>
    /// Specifies if the quota is enabled for the tenant entity or not.
    /// </summary>
    /// <example>true</example>
    public bool EnableQuota { get; set; }

    /// <summary>
    /// The default quota of the tenant entity.
    /// </summary>
    /// <example>1000</example>
    public long DefaultQuota { get; set; } = DefaultQuotaValue;

    /// <summary>
    /// The date of the last quota recalculation.
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    public DateTime? LastRecalculateDate { get; set; }

}