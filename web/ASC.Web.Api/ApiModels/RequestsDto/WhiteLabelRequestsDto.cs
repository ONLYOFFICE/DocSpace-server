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
/// The branding a portal is given: the wordmark, the logo images, or both.
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
    /// The wordmark printed next to or instead of a logo image, on the login page, in the editors and in
    /// notification letters. An empty or blank value, and the built-in `ONLYOFFICE` itself, clear the setting rather
    /// than store it. The text is not rendered into the logo images, which carry their own wordmark.
    /// </summary>
    /// <example>Company Name</example>
    [StringLength(40)]
    public string LogoText { get; set; }

    /// <summary>
    /// The logo images to store, each entry naming a logo slot in its `key` - the numeric `type` published by
    /// `GET api/2.0/settings/whitelabel/logos` - and carrying the two theme images in its value. A slot left out of
    /// the list keeps the image it has, so this is a partial update rather than a replacement of the whole branding.
    /// Saving the login-page slot also rebuilds the notification logo from it.
    /// </summary>
    /// <example>["item1", "item2"]</example>
    public IEnumerable<ItemKeyValuePair<string, LogoRequestsDto>> Logo { get; set; }
}

/// <summary>
/// The two theme variants of one branding logo.
/// </summary>
public class LogoRequestsDto
{
    /// <summary>
    /// The image used on a light background, either as a `data:image/png;base64,...` payload - `png`, `jpg` and
    /// `svg` are accepted - or as the name of a file already put in the temporary store.
    /// </summary>
    /// <example>data:image/png;base64,iVBORw0KGgoAAAANS...</example>
    public string Light { get; set; }

    /// <summary>
    /// The image used on a dark background, in the same two forms as `light`. It is only stored for the slots that
    /// have a dark variant and is ignored for the favicon and the editor logos.
    /// </summary>
    /// <example>data:image/png;base64,iVBORw0KGgoAAAANS...</example>
    public string Dark { get; set; }
}

/// <summary>
/// Whose branding is read or written, and which theme of it.
/// </summary>
public class WhiteLabelQueryRequestsDto
{
    /// <summary>
    /// Which theme the answer is filled in for: `true` fills the dark image only, `false` the light one only.
    /// Omitting it fills both, leaving the dark one empty for the slots that have no separate dark image.
    /// </summary>
    /// <example>true</example>
    public bool? IsDark { get; set; }

    /// <summary>
    /// Whether the installation-wide default branding is addressed instead of this portal own. Writing the default
    /// branding is only allowed on a self-hosted installation; elsewhere it is refused with 403.
    /// </summary>
    /// <example>true</example>
    public bool? IsDefault { get; set; }
}
