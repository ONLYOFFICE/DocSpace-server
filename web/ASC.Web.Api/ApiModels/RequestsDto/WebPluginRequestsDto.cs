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
/// The state the portal keeps for an installed web plugin: whether it runs, and its own settings blob.
/// </summary>
public class WebPluginRequests
{
    /// <summary>
    /// Whether the plugin runs in this portal. Switching it on adds the domains its manifest declares to the portal
    /// Content Security Policy and switching it off takes them away again; connected clients are told of the new
    /// state without a reload.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// The configuration the plugin reads at run time, as a JSON document serialised into a string. Its shape is
    /// defined by the plugin and not by the portal, which stores it encrypted for this portal alone. It replaces
    /// whatever was stored rather than merging into it, so send `{}` when there is nothing to keep.
    /// </summary>
    /// <example>{"theme":"dark","autoSave":true}</example>
    [StringLength(255)]
    public required string Settings { get; set; }
}

/// <summary>
/// Which installed web plugin is changed, and the state it is to have.
/// </summary>
public class WebPluginRequestsDto
{
    /// <summary>
    /// The plugin to change, by the manifest name `GET api/2.0/settings/webplugins` publishes as `name`, matched
    /// without regard to case. It is neither the localized display name nor the JavaScript object name in
    /// `pluginName`, so it cannot be read off the interface; a name that is not installed answers 404.
    /// </summary>
    /// <example>example-plugin</example>
    [FromRoute(Name = "name")]
    public required string Name { get; set; }

    /// <summary>
    /// The whole state the plugin is to have afterwards. It replaces what was stored instead of merging into it, so
    /// both the enabled flag and the settings have to be sent every time.
    /// </summary>
    /// <example>{"enabled": true, "settings": "{\"theme\":\"dark\"}"}</example>
    [FromBody]
    public required WebPluginRequests WebPlugin { get; set; }
}

/// <summary>
/// Which installed web plugin an operation addresses.
/// </summary>
public class WebPluginNameRequestDto
{
    /// <summary>
    /// The plugin to act on, by the manifest name `GET api/2.0/settings/webplugins` publishes as `name`, matched
    /// without regard to case. It is neither the localized display name nor the JavaScript object name in
    /// `pluginName`; a name that is not installed answers 404.
    /// </summary>
    /// <example>example-plugin</example>
    [FromRoute(Name = "name")]
    public required string Name { get; set; }
}

/// <summary>
/// Whether an uploaded plugin package is installed for the whole installation or for this portal alone.
/// </summary>
public class WebPluginFromFileRequestDto
{
    /// <summary>
    /// Whether the plugin is installed for every portal of the installation rather than only this one. It is
    /// accepted on a self-hosted installation alone and refused with 403 elsewhere; an installation-wide plugin also
    /// hides a portal plugin that carries the same name.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "system")]
    public bool System { get; set; }
}


/// <summary>
/// Which installed web plugins are listed.
/// </summary>
public class GetWebPluginsRequestDto
{
    /// <summary>
    /// Which plugins are kept: `true` the ones switched on, `false` the ones switched off. Omitting it lists every
    /// installed plugin whatever its state.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "enabled")]
    public bool? Enabled { get; set; }
}
