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
/// The external sources the portal Content Security Policy is to trust.
/// </summary>
/// <example>
/// {
///   "domains": ["example.com", "trusted-site.com"]
/// }
/// </example>
public class CspRequestsDto
{
    /// <summary>
    /// The domains the policy trusts, as the complete list that is to hold afterwards rather than a list of
    /// additions: send the domains already trusted together with the new one to add one, leave one out to withdraw
    /// it, and send an empty list to fall back to the portal built-in policy. An entry may be a bare host, a host
    /// with a scheme, or a wildcard host such as `*.example.com`; it has to form a valid absolute address and may
    /// contain ASCII characters only. Every entry becomes an allowed source for scripts, styles, images, fonts,
    /// frames, media and connections at once - the directives cannot be set apart here.
    /// </summary>
    /// <example>["example.com", "trusted-site.com"]</example>
    public IEnumerable<string> Domains { get; set; }
}
