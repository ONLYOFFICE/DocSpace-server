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

[ConstraintRoute("int")]
public class MasterFormControllerInternal(FileStorageService fileStorageServiceString,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : MasterFormController<int>(fileStorageServiceString, folderDtoHelper, fileDtoHelper);

public class MasterFormControllerThirdparty(FileStorageService fileStorageService,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : MasterFormController<string>(fileStorageService, folderDtoHelper, fileDtoHelper);

public abstract class MasterFormController<T>(FileStorageService fileStorageService,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Returns the editor address of a PDF form, or of a personal draft that it creates.
    /// To start filling in a form-filling room, use `PUT api/2.0/files/file/{fileId}/manageformfilling` first.
    /// Requires read access to the form. Other callers get 403. An external link holder can call it without signing
    /// in. Without a sign-in or link key, the call returns 401. With `action` set to `view` or `embedded`, the call
    /// returns the address of the form itself. It does the same for a caller who can edit the form, and before
    /// filling starts. Otherwise, a caller with fill-forms access gets a personal draft. The first such call changes
    /// data. It copies the form to the in-progress folder under the caller's name and clears its new mark. A repeated
    /// call returns the same draft until the caller deletes it. Without a sign-in, each call creates a new draft. For
    /// an invalid draft, the call returns its address with a notice to discard it. A draft becomes invalid if the form
    /// is deleted or changed, or the caller loses fill-forms access. The next call on the form then creates a new
    /// draft. When the call creates or rejects a draft, the
    /// address ends with a `#message/` notice for the editor. For the full editor configuration, use
    /// `GET api/2.0/files/file/{fileId}/openedit`. The call returns 403 for a form in Trash or in a folder where the
    /// caller cannot fill forms. It also returns 403 for a past revision without history access. The call returns
    /// 404 for an unknown file or revision. It returns 400 if the body has no `version`. It returns 415 for a format
    /// the editors cannot open. It returns 402 if the draft exceeds the storage quota.
    /// </remarks>
    /// <summary>
    /// Open a form draft for filling
    /// </summary>
    /// <path>api/2.0/files/masterform/{fileId}/checkfillformdraft</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The editor address to open, with an optional notice fragment", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read or has no `version`")]
    [SwaggerResponse(401, "An anonymous caller has no external link")]
    [SwaggerResponse(402, "The personal draft does not fit into the storage quota of the portal or the room")]
    [SwaggerResponse(403, "The caller cannot open the form, asked for a past revision without history access, the form is in Trash, or the caller may fill the form but not the folder it lies in")]
    [SwaggerResponse(404, "The file id, or the requested version of it, resolves to nothing")]
    [SwaggerResponse(415, "The file is in a format the editors can neither edit nor open for viewing")]
    [AllowAnonymous]
    [HttpPost("masterform/{fileId}/checkfillformdraft")]
    public async Task<string> CheckFillFormDraft(CheckFillFormDraftRequestDto<T> inDto)
    {
        return await fileStorageService.CheckFillFormDraftAsync(inDto.FileId, inDto.File.Version, !inDto.File.RequestEmbedded, inDto.File.RequestView);
    }
}