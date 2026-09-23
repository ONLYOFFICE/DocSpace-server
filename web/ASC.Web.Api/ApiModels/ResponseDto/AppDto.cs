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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// One feature module of the portal: whether it is switched on here, and the settings stored for it.
/// </summary>
public class AppDto
{
    /// <summary>
    /// The application's stable key, declared in the installation configuration - `ai-rooms`, `docs-cloud` and
    /// the like. It is what every other operation of this group addresses an application by, and a client maps it
    /// to a title and an icon of its own; the portal ships no display name for it.
    /// </summary>
    /// <example>ai-rooms</example>
    public string Id { get; set; }

    /// <summary>
    /// Whether the application is switched on for this portal. It is the portal's own flag where one has been
    /// saved, and the default the installation configuration gives the application otherwise.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// The settings document saved for this portal, stored and returned verbatim - the portal never looks inside
    /// it, and only the application knows its shape. It is empty while the portal has saved none, which means the
    /// application falls back to its own defaults, and it also survives the application being switched off.
    /// </summary>
    /// <example>{"theme":"dark","language":"en"}</example>
    public JsonElement? Settings { get; set; }
}
