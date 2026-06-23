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

using ASC.Files.Core;

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The request parameters for managing the owner-specific settings.
/// </summary>
/// <example>
/// {
///   "ownerId": "00000000-0000-0000-0000-000000000001"
/// }
/// </example>
public class OwnerIdSettingsRequestDto
{
    /// <summary>
    /// The ID of the owner whose settings are being managed.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public required Guid OwnerId { get; set; }
}

/// <summary>
/// The request parameters for managing the version-specific settings.
/// </summary>
/// <example>
/// {
///   "versionId": 2
/// }
/// </example>
public class SettingsRequestsDto
{
    /// <summary>
    /// The version ID.
    /// </summary>
    /// <example>2</example>
    public required int VersionId { get; set; }
}

/// <summary>
/// The request parameters for managing the user interface tips visibility.
/// </summary>
/// <example>
/// {
///   "show": true
/// }
/// </example>
public class TipsRequestDto
{
    /// <summary>
    /// Controls the visibility of the user interface tips (displayed or hidden).
    /// </summary>
    /// <example>true</example>
    public bool Show { get; set; } //tips
}

/// <summary>
/// The request parameters for setting the default product configuration.
/// </summary>
/// <example>
/// {
///   "defaultFolderType": 1
/// }
/// </example>
public class DefaultProductRequestDto
{
    /// <summary>
    /// The ID of the product to be set as default.
    /// </summary>
    /// <example>1</example>
    public required FolderType DefaultFolderType { get; set; }
}

/// <summary>
/// The request parameters for configuring the time zone settings.
/// </summary>
/// <example>
/// {
///   "lng": "en",
///   "timeZoneID": "America/New_York"
/// }
/// </example>
public class TimeZoneRequestDto
{
    /// <summary>
    /// The language code for the time zone localization.
    /// </summary>
    /// <example>en</example>
    public required string Lng { get; set; }

    /// <summary>
    /// The IANA time zone identifier.
    /// </summary>
    /// <example>America/New_York</example>
    public string TimeZoneID { get; set; }
}

/// <summary>
/// The request parameters for managing the Developer Tools access settings for the current tenant.
/// </summary>
/// <example>
/// {
///   "limitedAccessForUsers": false
/// }
/// </example>
public class TenantDevToolsAccessSettingsDto
{
    /// <summary>
    /// Determines if users have restricted access to the Developer Tools.
    /// </summary>
    /// <example>false</example>
    public bool LimitedAccessForUsers { get; set; }
}

/// <summary>
/// The request parameters for managing the visibility settings of the promotional banners for the current tenant.
/// </summary>
/// <example>
/// {
///   "hidden": true
/// }
/// </example>
public class TenantBannerSettingsDto
{
    /// <summary>
    /// The banners visibility flag.
    /// </summary>
    /// <example>true</example>
    public bool Hidden { get; set; }
}

/// <summary>
/// The request parameters for managing the tenant-level AI access settings.
/// </summary>
/// <remarks>
/// Controls whether all AI functionality (chat, agents, vectorization) is available for the current tenant.
/// When disabled, the AI Agents folder is hidden from root folder listings and AI status checks return disabled immediately.
/// Only DocSpaceAdmin users can modify this setting.
/// </remarks>
/// <example>
/// {
///   "aiEnabled": false
/// }
/// </example>
public class TenantAiAccessSettingsDto
{
    /// <summary>
    /// Specifies whether AI functionality is enabled for the tenant.
    /// Set to <c>true</c> to enable all AI features or <c>false</c> to disable them tenant-wide.
    /// </summary>
    /// <example>false</example>
    public bool Enabled { get; set; }
}