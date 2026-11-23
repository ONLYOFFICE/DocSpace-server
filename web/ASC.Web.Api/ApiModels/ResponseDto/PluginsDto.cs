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
/// What the installation allows to be done with web plugins.
/// </summary>
/// <example>
/// {
///   "enabled": true,
///   "upload": true,
///   "delete": true,
///   "personal": true
/// }
/// </example>
public class PluginsDto
{
    /// <summary>
    /// Whether web plugins run on this portal at all. While it is `false` the operations under
    /// `api/2.0/settings/webplugins` are of no use, whatever the other flags say. It is `false`, and so are
    /// `upload` and `delete`, unless the installation switched them on in its configuration.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether a plugin may be added through `POST api/2.0/settings/webplugins`: for the portal by an
    /// administrator, or for themselves by a member while `personal` is `true`. While it is `false` nothing new can
    /// be added, and the plugins installed before stay available.
    /// </summary>
    /// <example>true</example>
    public bool Upload { get; set; }

    /// <summary>
    /// Whether a portal plugin may be removed again through `DELETE api/2.0/settings/webplugins/{name}`. A plugin
    /// installed for the whole installation can be removed on a standalone installation only, and a member removes
    /// their own personal plugins whatever this flag says.
    /// </summary>
    /// <example>true</example>
    public bool Delete { get; set; }

    /// <summary>
    /// Whether a portal member other than a guest may install plugins for themselves alone through
    /// `POST api/2.0/settings/webplugins?personal=true`. It is `true` unless the installation switched personal
    /// plugins off in its configuration, and it is of no use while `enabled` is `false`; uploading one still needs
    /// `upload`.
    /// </summary>
    /// <example>true</example>
    public bool Personal { get; set; }
}