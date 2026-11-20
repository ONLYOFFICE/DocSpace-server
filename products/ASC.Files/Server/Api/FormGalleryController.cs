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
/// The form gallery of the installation, as the portal itself sees it.
/// </summary>
[Scope]
[ApiEndpoint("files")]
public class FormGalleryController(OFormRequestManager oFormRequestManager) : ControllerBase
{
    /// <remarks>
    /// Returns whether the portal can create files from the forms of its form gallery. Call it when the gallery opens.
    /// If it returns `false`, show the gallery as unavailable and do not offer its forms. Any signed-in user can call
    /// it. Other callers get 401. The call is read-only. The portal asks the gallery for its form list and keeps the
    /// answer for 10 minutes. A failed check keeps a known `true` for up to a day. With nothing known, a failed check
    /// returns `false` for 1 minute. An expired answer comes back within 200 ms if the next check takes longer. A call
    /// with no answer stored waits for the check, up to 4 seconds. Without a configured gallery, the call returns
    /// `false`.
    /// </remarks>
    /// <summary>
    /// Check the form gallery
    /// </summary>
    /// <path>api/2.0/files/formGallery/available</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "`true` when the gallery answered with its form list, `false` when no gallery is configured or the portal has failed to get the list and knows no earlier answer", typeof(bool))]
    [SwaggerResponse(401, "The caller is not signed in")]
    [HttpGet("formGallery/available")]
    public async Task<bool> GetFormGalleryAvailable()
    {
        return await oFormRequestManager.IsAvailableAsync();
    }
}
