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

namespace ASC.People.ApiModels.RequestDto;

/// <summary>
/// The request parameters for the authentication providers.
/// </summary>
public class AuthProvidersRequestDto
{
    /// <summary>
    /// Set it to true when the list is rendered on an invitation page: the providers that cannot be used to accept an
    /// invitation, `twitter` and `appleid`, are then left out. It defaults to false, which returns every enabled
    /// provider.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "inviteView")]
    public bool InviteView { get; set; }

    /// <summary>
    /// Set it to true when the list is rendered on a settings page, to get login URLs that open in a popup window.
    /// With the default false the URL still opens in a popup for a desktop browser, and switches to a redirect only
    /// for a mobile browser or for the DocSpace desktop application.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "settingsView")]
    public bool SettingsView { get; set; }

    /// <summary>
    /// The name of the client-side function the popup calls back when the provider authorization finishes. It is
    /// placed into the returned URLs as they are, and it is only used by the popup mode.
    /// </summary>
    /// <example>onAuthCallback</example>
    [FromQuery(Name = "clientCallback")]
    public string ClientCallback { get; set; }

    /// <summary>
    /// Keeps only the named provider, compared case-insensitively against the lowercase provider names such as
    /// `google` or `microsoft`; the special value `openid` selects `google`. Omit it to get every enabled provider.
    /// </summary>
    /// <example>google</example>
    [FromQuery(Name = "fromOnly")]
    public string FromOnly { get; set; }
}