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
/// Everything a payment page needs about the portal at once: the build, the subscription, the quota and the
/// document-server licence.
/// </summary>
/// <example>
/// {
///   "customMode": false,
///   "opensource": false,
///   "enterprise": true,
///   "developer": false,
///   "notPaid": false,
///   "licenseAccept": "01/15/2024 10:30:00",
///   "enableTariffPage": true
/// }
/// </example>
public class TenantExtraDto
{
    /// <summary>
    /// Whether the installation runs in the vendor's white-label mode, in which the payment pages and the vendor
    /// links are suppressed. This flag and the three below describe the build, not the subscription.
    /// </summary>
    /// <example>false</example>
    public bool CustomMode { get; set; }

    /// <summary>
    /// Whether the installation runs the open-source build, which has no paid plan at all.
    /// </summary>
    /// <example>false</example>
    public bool Opensource { get; set; }

    /// <summary>
    /// Whether the installation runs on an Enterprise licence file, which is what makes the licence operations
    /// under `api/2.0/settings/license` usable.
    /// </summary>
    /// <example>true</example>
    public bool Enterprise { get; set; }

    /// <summary>
    /// Whether the installation runs on a Developer licence, an Enterprise licence meant for embedding rather
    /// than for production use.
    /// </summary>
    /// <example>false</example>
    public bool Developer { get; set; }

    /// <summary>
    /// The subscription in force, in the internal shape rather than the one `GET api/2.0/portal/tariff` returns -
    /// nothing here is withheld by role, since the whole operation already demands the portal-settings right.
    /// </summary>
    /// <example>{}</example>
    public Tariff Tariff { get; set; }

    /// <summary>
    /// The quota the subscription grants, with its features and how much of each is already used, exactly as
    /// `GET api/2.0/portal/payment/quota` reports it.
    /// </summary>
    /// <example>{}</example>
    public QuotaDto Quota { get; set; }

    /// <summary>
    /// Whether the portal is behind on payment, which is what puts the interface into its restricted state.
    /// Note the sense: `true` means unpaid.
    /// </summary>
    /// <example>false</example>
    public bool NotPaid { get; set; }

    /// <summary>
    /// When the licence agreement was accepted for this installation, as a formatted date string rather than an
    /// ISO timestamp. A date at the minimum a date can hold means it has never been accepted.
    /// </summary>
    /// <example>01/15/2024 10:30:00</example>
    public string LicenseAccept { get; set; }

    /// <summary>
    /// Whether the interface should offer its payment page at all. It is `false` in white-label mode, on an
    /// Amazon-image installation and on a server installation with no licence path configured, where paying
    /// happens outside the portal.
    /// </summary>
    /// <example>true</example>
    public bool EnableTariffPage { get; set; }

    /// <summary>
    /// The editing users the document server counts against its own licence, each with the moment its seat frees
    /// up. It is empty when the document server could not be reached, which is not the same as no users.
    /// </summary>
    /// <example>{ "00000000-0000-0000-0000-000000000001": "2024-01-15T10:30:00Z" }</example>
    public Dictionary<string, DateTime> DocServerUserQuota { get; set; }

    /// <summary>
    /// The licence of the document server behind this portal, which is a separate licence from the portal's own
    /// subscription. It is empty when the document server could not be reached.
    /// </summary>
    /// <example>{}</example>
    public License DocServerLicense { get; set; }
}