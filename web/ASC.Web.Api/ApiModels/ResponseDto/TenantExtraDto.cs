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

using License = ASC.Core.Billing.License;

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The tenant extra parameters.
/// </summary>
/// <example>
/// {
///   "customMode": true,
///   "opensource": true,
///   "enterprise": true,
///   "developer": true,
///   "tariff": {},
///   "quota": {},
///   "notPaid": true,
///   "licenseAccept": "example value",
///   "enableTariffPage": true,
///   "docServerUserQuota": "2024-01-15T10:30:00Z",
///   "docServerLicense": {}
/// }
/// </example>
public class TenantExtraDto
{
    /// <summary>
    /// Specifies if an extra tenant license is customizable or not.
    /// </summary>
    /// <example>true</example>
    public bool CustomMode { get; set; }

    /// <summary>
    /// Specifies if an extra tenant license is Community or not.
    /// </summary>
    /// <example>true</example>
    public bool Opensource { get; set; }

    /// <summary>
    /// Specifies if an extra tenant license is Enterprise or not.
    /// </summary>
    /// <example>true</example>
    public bool Enterprise { get; set; }

    /// <summary>
    /// Specifies if an extra tenant license is Developer or not.
    /// </summary>
    /// <example>true</example>
    public bool Developer { get; set; }

    /// <summary>
    /// The license tariff.
    /// </summary>
    /// <example>{}</example>
    public Tariff Tariff { get; set; }

    /// <summary>
    /// The license quota.
    /// </summary>
    /// <example>{}</example>
    public QuotaDto Quota { get; set; }

    /// <summary>
    /// Specifies if the license is paid or not.
    /// </summary>
    /// <example>true</example>
    public bool NotPaid { get; set; }

    /// <summary>
    /// The time when the license was accepted.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public string LicenseAccept { get; set; }

    /// <summary>
    /// Specifies if the tariff page is enabled or not.
    /// </summary>
    /// <example>true</example>
    public bool EnableTariffPage { get; set; }

    /// <summary>
    /// The ONLYOFFICE Docs user quotas.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public Dictionary<string, DateTime> DocServerUserQuota { get; set; }

    /// <summary>
    /// The ONLYOFFICE Docs license.
    /// </summary>
    /// <example>{}</example>
    public License DocServerLicense { get; set; }
}