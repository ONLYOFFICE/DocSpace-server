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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The document service location as this portal has it configured, together with the editor entry points a client
/// needs in order to open a document.
/// </summary>
public class DocServiceUrlDto
{
    /// <summary>
    /// The editor version the running Document Server reported. It is filled in only when the version was asked for,
    /// and comes back empty otherwise. When the Document Server does not answer, a fallback version is reported
    /// rather than an error, so a value here is no proof that the server is reachable.
    /// </summary>
    /// <example>8.0.1</example>
    public required string Version { get; set; }

    /// <summary>
    /// The absolute URL of the editor api script that a client has to load before it can open a document. It is
    /// derived from the public Document Server address unless the deployment overrides it separately.
    /// </summary>
    /// <example>https://documentserver.example.com/web-apps/apps/api/documents/api.js</example>
    public required string DocServiceUrlApi { get; set; }

    /// <summary>
    /// The public Document Server address a browser loads the editor from. Empty means no document server is
    /// configured for this portal, and documents cannot be opened for editing or viewing.
    /// </summary>
    /// <example>https://documentserver.example.com/</example>
    public required string DocServiceUrl { get; set; }


    /// <summary>
    /// The absolute URL of a page a client may load in advance to warm the editor scripts up. Loading it is optional
    /// and changes nothing on the portal.
    /// </summary>
    /// <example>https://documentserver.example.com/web-apps/apps/api/documents/preload.html</example>
    public required string DocServicePreloadUrl { get; set; }

    /// <summary>
    /// The address the portal uses for its own server-to-server calls to the Document Server. When no private-network
    /// address is configured, it repeats the public one.
    /// </summary>
    /// <example>http://documentserver-internal.local/</example>
    public required string DocServiceUrlInternal { get; set; }

    /// <summary>
    /// The address the Document Server is told to call this portal back on. Empty means nothing overrides it and the
    /// portal's own resolved address is used.
    /// </summary>
    /// <example>https://portal.example.com/</example>
    public required string DocServicePortalUrl { get; set; }

    /// <summary>
    /// The name of the HTTP header that carries the signature on requests between the portal and the Document Server.
    /// The secret itself is not part of the answer, so this only tells a client whether request signing is set up and
    /// under which header.
    /// </summary>
    /// <example>Authorization</example>
    public required string DocServiceSignatureHeader { get; set; }

    /// <summary>
    /// Whether the portal validates the TLS certificate of the Document Server. False means any certificate is
    /// accepted, which is expected only in a test deployment.
    /// </summary>
    /// <example>true</example>
    public required bool DocServiceSslVerification { get; set; }

    /// <summary>
    /// Whether every one of these settings is still the one the deployment ships with. False means at least one of
    /// the addresses, the signature settings or SSL verification has been overridden for this portal.
    /// </summary>
    /// <example>true</example>
    public required bool IsDefault { get; set; }
}