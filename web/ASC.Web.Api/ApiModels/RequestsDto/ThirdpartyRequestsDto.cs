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
/// What the external provider sent back to the portal callback page after the user answered the consent screen.
/// </summary>
public class ConfirmationCodeRequestDto
{
    /// <summary>
    /// Where the callback page sends the browser once it has read the outcome. It is followed as given, so it has to
    /// be an address the user can open rather than one only the portal can reach.
    /// </summary>
    /// <example>https://example.com/oauth/callback</example>
    [FromQuery(Name = "redirect")]
    public string Redirect { get; set; }

    /// <summary>
    /// The authorization code the provider issued after the user granted access. It is short-lived and single-use,
    /// and is exchanged for a token by whichever operation connects the account rather than here.
    /// </summary>
    /// <example>4/0AY0e-g7X...</example>
    [FromQuery(Name = "code")]
    public string Code { get; set; }

    /// <summary>
    /// The failure the provider reported instead of a code, such as the user declining the consent screen. When it
    /// is present the outcome is a failure whatever `code` holds.
    /// </summary>
    /// <example>access_denied</example>
    [FromQuery(Name = "error")]
    public string Error { get; set; }
}

/// <summary>
/// Which external provider the consent URL is built for.
/// </summary>
public class ConfirmationCodeUrlRequestDto
{
    /// <summary>
    /// The provider whose consent screen is wanted. Only Google, Dropbox, Docusign, Box, OneDrive, Wordpress and
    /// Github produce a URL; any other provider is answered with 200 and no URL rather than an error. The provider
    /// credentials have to be saved with `POST api/2.0/settings/authservice` first, or the URL comes back without a
    /// client identifier and the provider refuses it.
    /// </summary>
    /// <example>{}</example>
    [FromRoute(Name = "provider")]
    public LoginProvider Provider { get; set; }
}
