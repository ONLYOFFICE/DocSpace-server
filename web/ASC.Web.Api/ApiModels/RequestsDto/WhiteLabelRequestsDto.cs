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
/// The request parameters for configuring the white label branding settings.
/// </summary>
/// <example>
/// {
///   "logoText": "Company Name",
///   "logo": ["item1", "item2"]
/// }
/// </example>
public class WhiteLabelRequestsDto
{
    /// <summary>
    /// The text to display alongside or in place of the logo.
    /// </summary>
    /// <example>Company Name</example>
    [StringLength(40)]
    public string LogoText { get; set; }

    /// <summary>
    /// The white label tenant IDs with their logos (light or dark).
    /// </summary>
    /// <example>["item1", "item2"]</example>
    public IEnumerable<ItemKeyValuePair<string, LogoRequestsDto>> Logo { get; set; }
}

/// <summary>
/// The request parameters for the theme-specific logo configurations.
/// </summary>
public class LogoRequestsDto
{
    /// <summary>
    /// The URL or base64-encoded image data for the light theme logo.
    /// </summary>
    /// <example>data:image/png;base64,iVBORw0KGgoAAAANS...</example>
    public string Light { get; set; }

    /// <summary>
    /// The URL or base64-encoded image data for the dark theme logo.
    /// </summary>
    /// <example>data:image/png;base64,iVBORw0KGgoAAAANS...</example>
    public string Dark { get; set; }
}

/// <summary>
/// The request parameters for querying the white label configurations.
/// </summary>
public class WhiteLabelQueryRequestsDto
{
    /// <summary>
    /// Specifies if the white label logo is for the dark theme or not.
    /// </summary>
    /// <example>true</example>
    public bool? IsDark { get; set; }

    /// <summary>
    /// Specifies if the logo is for a default tenant or not.
    /// </summary>
    /// <example>true</example>
    public bool? IsDefault { get; set; }
}