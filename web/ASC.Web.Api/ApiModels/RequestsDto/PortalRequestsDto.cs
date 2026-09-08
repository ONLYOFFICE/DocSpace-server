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
/// Whether the tariff behind the extra tenant information is taken from the cache or the billing system.
/// </summary>
/// <example>
/// {
///   "refresh": true
/// }
/// </example>
public class PortalExtraTenantRequestDto
{
    /// <summary>
    /// Whether the tariff is re-read from the billing system instead of the portal cache. The remote read is slower,
    /// so ask for it after a payment rather than on every page.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "refresh")]
    public bool Refresh { get; set; }
}

/// <summary>
/// The portal-relative path that is turned into an absolute URL.
/// </summary>
public class PortalPathRequestDto
{
    /// <summary>
    /// The path to resolve. It is taken as it is: an omitted or empty value yields the portal root, a value starting
    /// with `/` is appended to that root, a value starting with `~/` is resolved against the virtual root, and one
    /// that already begins with `http://`, `https://` or `mailto:` is handed back unchanged. Nothing checks that the
    /// path exists or that the caller may open it.
    /// </summary>
    /// <example>/portal/documents</example>
    [FromQuery(Name = "virtualPath")]
    public string VirtualPath { get; set; }
}

/// <summary>
/// The page a preview image is requested for.
/// </summary>
public class PortalThumbnailRequestDto
{
    /// <summary>
    /// The absolute address of the page to picture. It is passed on to the thumbnail service the installation is
    /// configured with, so a page that service cannot reach yields no image; HTML-escaped ampersands are restored
    /// before the address is used.
    /// </summary>
    /// <example>https://example.com/image.png</example>
    [FromQuery(Name = "url")]
    public string Url { get; set; }
}

/// <summary>
/// Which mobile application the calling user has installed.
/// </summary>
public class PortalMobileAppRequestDto
{
    /// <summary>
    /// The application that was installed. The installation is recorded against the calling user address, so it
    /// tells the portal which app that person uses rather than counting devices.
    /// </summary>
    /// <example>IosProjects</example>
    [FromQuery(Name = "type")]
    public MobileAppType Type { get; set; }
}

/// <summary>
/// Whether the portal settings answer carries the client-side password hashing parameters.
/// </summary>
public class PortalSettingsRequestDto
{
    /// <summary>
    /// Whether the answer also carries the salt, iteration count and hash size a client needs to hash a password
    /// before sending it to the authentication operations. They are included for an anonymous caller anyway; for a
    /// signed-in one they are left out unless this is set.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "withpassword")]
    public bool? WithPassword { get; set; }
}
