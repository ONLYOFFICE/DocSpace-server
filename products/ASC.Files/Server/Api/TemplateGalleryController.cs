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


namespace ASC.Files.Api;

/// <summary>
/// The template gallery of the installation (document, spreadsheet, presentation and PDF form templates), as the
/// server sees it.
/// </summary>
[Scope]
[ApiEndpoint("files")]
public class TemplateGalleryController(OFormRequestManager oFormRequestManager) : ControllerBase
{
    /// <remarks>
    /// Returns whether the template gallery answers the server with its template list. Call it when the gallery opens.
    /// If it returns `false`, show the gallery as unavailable and do not offer its templates. The check does not ask
    /// for the template files, which can be on another host. Any signed-in user can call it. Other callers get 401. The
    /// call is read-only. The server keeps a `true` for 10 minutes for all portals of the installation. A failed check
    /// keeps a known `true` for a day after the gallery last answered, however many checks fail. Without a shared
    /// cache, each server process keeps its own answers, and a restart forgets them. With nothing known, a failed check
    /// returns `false`. The server process that checked keeps that `false` for up to 1 minute. After 200 ms of waiting
    /// for a refresh, the call can return an expired answer of the last day. A process may then return its own
    /// `false` while another got `true`. The request to the gallery times out after 4 seconds.
    /// Without a configured gallery, the call returns `false`. The answer is not a promise that a template can be
    /// created. The server counts the requests of the downloads to each gallery host over 30 seconds. A request fails
    /// if it cannot connect or times out. It also fails on a server error, Request Timeout, or Too Many Requests. The
    /// server stops asking that host for 30 seconds after a failed request. This happens when at least half of 5 or
    /// more requests failed. Meanwhile, `POST api/2.0/files/{folderId}/file` with a `formId` fails, while this call
    /// can still return `true`.
    /// </remarks>
    /// <summary>
    /// Check the template gallery
    /// </summary>
    /// <path>api/2.0/files/templateGallery/available</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "`true` when the gallery answered with its template list, `false` when no gallery is configured or the server has failed to get the list and knows no `true` from the last day; while a slow check runs, also a process's own earlier `false`", typeof(bool))]
    [SwaggerResponse(401, "The caller is not signed in")]
    [HttpGet("templateGallery/available")]
    public async Task<bool> GetTemplateGalleryAvailable()
    {
        return await oFormRequestManager.IsAvailableAsync();
    }
}
