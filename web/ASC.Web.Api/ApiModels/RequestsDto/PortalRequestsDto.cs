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
/// The request parameters for managing additional tenant information in a portal.
/// </summary>
/// <example>
/// {
///   "refresh": true
/// }
/// </example>
public class PortalExtraTenantRequestDto
{
    /// <summary>
    /// Specifies whether to fetch fresh tariff information.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "refresh")]
    public bool Refresh { get; set; }
}

/// <summary>
/// The request parameters for the portal path configuration.
/// </summary>
public class PortalPathRequestDto
{
    /// <summary>
    /// The virtual path for the portal resource access.
    /// </summary>
    /// <example>/portal/documents</example>
    [FromQuery(Name = "virtualPath")]
    public string VirtualPath { get; set; }
}

/// <summary>
/// The request parameters for managing the portal thumbnail generation.
/// </summary>
public class PortalThumbnailRequestDto
{
    /// <summary>
    /// The URL of the content to generate a thumbnail from.
    /// </summary>
    /// <example>https://example.com/image.png</example>
    [FromQuery(Name = "url")]
    public string Url { get; set; }
}

/// <summary>
/// The request parameters for the mobile application configuration of the portal.
/// </summary>
public class PortalMobileAppRequestDto
{
    /// <summary>
    /// The target mobile platform or application type.
    /// </summary>
    /// <example>IosProjects</example>
    [FromQuery(Name = "type")]
    public MobileAppType Type { get; set; }
}

/// <summary>
/// The request parameters for the portal security and configuration settings.
/// </summary>
public class PortalSettingsRequestDto
{
    /// <summary>
    /// Specifies whether to include the password hashing configuration in the response.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "withpassword")]
    public bool? WithPassword { get; set; }
}