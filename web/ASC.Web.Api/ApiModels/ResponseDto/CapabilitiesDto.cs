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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The sign-in methods this portal offers, as a login client needs them before anyone has signed in.
/// </summary>
public class CapabilitiesDto
{
    /// <summary>
    /// Whether members may sign in with their directory credentials. It is `false` both when LDAP sign-in is
    /// switched off and when the pricing plan or the installation does not include it, and also when the settings
    /// could not be read at all - a `false` here means the method is not offered, never that it is unknown.
    /// </summary>
    /// <example>false</example>
    public required bool LdapEnabled { get; set; }

    /// <summary>
    /// The directory domain members authenticate against, to be shown next to the login field. It is empty
    /// whenever `ldapEnabled` is `false`, and also while the portal has not completed a directory synchronisation.
    /// </summary>
    /// <example>example.com</example>
    public string LdapDomain { get; set; }

    /// <summary>
    /// The keys of the external identity providers to offer, ordered for the country the caller's IP address
    /// resolves to and reduced to those this installation has credentials for. Pass one of them as `provider` to
    /// `POST api/2.0/authentication`. An empty list means external sign-in is not on offer.
    /// </summary>
    /// <example>["google", "facebook", "microsoft"]</example>
    public required List<string> Providers { get; set; }

    /// <summary>
    /// The caption for the single sign-on button in the portal language, empty whenever `ssoUrl` is.
    /// </summary>
    /// <example>Enterprise SSO</example>
    public required string SsoLabel { get; set; }

    /// <summary>
    /// Whether external identity providers may be used on this portal at all. While it is `false`, `providers` is
    /// empty because the list is not even assembled.
    /// </summary>
    /// <example>true</example>
    public required bool OauthEnabled { get; init; }

    /// <summary>
    /// The address to send the browser to for SAML single sign-on. It is empty when single sign-on is not on
    /// offer, which is the one thing to test - there is no separate flag for it.
    /// </summary>
    /// <example>https://sso.example.com/login</example>
    [Url]
    public required string SsoUrl { get; set; }

    /// <summary>
    /// Whether the installation exposes its built-in identity server, which is what the portal's own OAuth
    /// applications authenticate against. It concerns third-party applications signing in to the portal, not
    /// portal members signing in to an external provider - that is `providers`.
    /// </summary>
    /// <example>false</example>
    public required bool IdentityServerEnabled { get; set; }
}