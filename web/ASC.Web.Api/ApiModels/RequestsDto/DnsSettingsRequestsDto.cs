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

namespace ASC.Web.Api.Models;

/// <summary>
/// The custom domain the portal answers on, and whether that mapping is in force.
/// </summary>
/// <example>
/// {
///   "dnsName": "example.com",
///   "enable": true
/// }
/// </example>
public class DnsSettingsRequestsDto
{
    /// <summary>
    /// The domain the portal is to be reachable under, as a bare hostname without a scheme. It must not collide with
    /// the reserved base domain of the installation, and a name that fails validation is refused without disturbing
    /// the mapping in force. It is read only while `enable` is true.
    /// </summary>
    /// <example>example.com</example>
    public string DnsName { get; set; }

    /// <summary>
    /// Whether the custom domain is put in force. Setting it false clears the mapping and ignores `dnsName`; setting
    /// it true also stops the previous domain from answering and rewrites any Content Security Policy entry that
    /// named it.
    /// </summary>
    /// <example>true</example>
    public bool Enable { get; set; }
}
