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
/// Whether a portal application is switched on.
/// </summary>
/// <example>
/// {
///   "enabled": true
/// }
/// </example>
public class SetAppEnabledBody
{
    /// <summary>
    /// Whether the application is available in this portal. Switching it off leaves its settings document stored, so
    /// switching it back on restores the configuration it had; connected clients are told of the new state without a
    /// reload.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }
}

/// <summary>
/// Which portal application is switched, and which way.
/// </summary>
public class SetAppEnabledRequestDto
{
    /// <summary>
    /// The application to switch, by the identifier `GET api/2.0/apps` reports. It has to be an application declared
    /// in the installation configuration; an unknown identifier answers 404 rather than creating anything.
    /// </summary>
    /// <example>ai-room</example>
    [FromRoute(Name = "id")]
    public string Id { get; set; }

    /// <summary>
    /// The new state of the application. Only the enabled flag travels here; the settings document is changed
    /// through `PUT api/2.0/apps/{id}/settings`.
    /// </summary>
    [FromBody]
    public required SetAppEnabledBody Body { get; set; }
}

/// <summary>
/// The configuration document a portal application keeps.
/// </summary>
/// <example>
/// {
///   "settings": {}
/// }
/// </example>
public class SetAppSettingsBody
{
    /// <summary>
    /// The configuration the application reads, as any valid JSON value. Its shape is defined by the application and
    /// is neither validated nor interpreted by the portal, which stores it verbatim. It replaces the whole stored
    /// document rather than merging into it, and `null` drops it so the application falls back to its own defaults.
    /// </summary>
    /// <example>{}</example>
    public JsonElement Settings { get; set; }
}

/// <summary>
/// Which portal application the configuration document is stored for.
/// </summary>
public class SetAppSettingsRequestDto
{
    /// <summary>
    /// The application whose configuration is stored, by the identifier `GET api/2.0/apps` reports. An identifier
    /// not declared in the installation configuration answers 404.
    /// </summary>
    /// <example>ai-room</example>
    [FromRoute(Name = "id")]
    public string Id { get; set; }

    /// <summary>
    /// The configuration to store for this portal, replacing whatever was stored before.
    /// </summary>
    [FromBody]
    public required SetAppSettingsBody Body { get; set; }
}

/// <summary>
/// Which portal application is read.
/// </summary>
public class GetAppRequestDto
{
    /// <summary>
    /// The application to read, by the identifier `GET api/2.0/apps` reports - one of the feature modules the portal
    /// can turn on, such as `ai-room` or `docs-cloud`. An identifier not declared in the installation configuration
    /// answers 404, which is also how a caller learns that an application does not exist here.
    /// </summary>
    /// <example>ai-room</example>
    [FromRoute(Name = "id")]
    public string Id { get; set; }
}
