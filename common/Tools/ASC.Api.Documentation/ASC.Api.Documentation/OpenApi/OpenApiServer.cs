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


namespace ASC.Api.Documentation;

/// <summary>
/// The server the documented URIs are relative to.
/// </summary>
internal static class OpenApiServer
{
    /// <summary>
    /// Fills in the `baseUrl` server variable when the document leaves it empty.
    /// </summary>
    /// <remarks>
    /// The emitter states no default on purpose - a DocSpace instance lives wherever it is
    /// installed - but consumers do not treat the absence kindly: the Markdown pages head every
    /// document with "http://http:", and the site builds its curl samples against whatever host
    /// is serving the documentation, which is never the portal being called. A placeholder host
    /// is wrong in a way the reader can see and correct; the alternatives are wrong silently.
    /// </remarks>
    public static void ApplyBaseUrlDefault(JsonObject document, string? serverUrl)
    {
        if (string.IsNullOrWhiteSpace(serverUrl)
            || document["servers"] is not JsonArray { Count: > 0 } servers
            || servers[0]?["variables"]?["baseUrl"] is not JsonObject baseUrl
            || !string.IsNullOrEmpty(baseUrl["default"]?.ToString()))
        {
            return;
        }

        baseUrl["default"] = serverUrl;
    }
}
