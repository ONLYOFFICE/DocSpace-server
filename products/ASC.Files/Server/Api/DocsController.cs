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
/// Operations carried out by the document service on behalf of the portal.
/// </summary>
[Scope]
[ApiEndpoint("docs")]
public class DocsController(
    FileStorageService fileStorageService,
    GlobalFolderHelper globalFolderHelper,
    FileConverter fileConverter,
    FileBuilderOperationsManager fileBuilderOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FileDtoHelper fileDtoHelper,
    CommonLinkUtility commonLinkUtility)
    : ControllerBase
{
    /// <remarks>
    /// Converts a portal file into another format and saves the result as a new file, answering with that file. The
    /// caller needs read access to a source that may be copied and the right to create files in `folderId`; without
    /// `folderId` the result goes to the "My documents" section of the caller, so a guest, who has none, has to pass
    /// it. A file in a private room or in a room with a watermark is not converted, as it would leave the room without
    /// its protection. `outputtype` is the only parameter a plain conversion needs; the other fields are passed to the
    /// document service under its own names, while the source format, name and cache key are taken from the file. The
    /// call is mutating and not idempotent: the source is kept, and each call adds another file under a free name. It
    /// answers once the conversion is done, so a large file keeps the request open. Only files stored in the portal
    /// itself are accepted. To convert while copying into another folder, a third-party one included, use
    /// `POST api/2.0/files/file/{fileId}/copyas`; to convert into the portal's own editable format beside the source,
    /// use `PUT api/2.0/files/file/{fileId}/checkconversion`. A missing or unsupported `outputtype` is refused with
    /// 400, and so is a source the document service rejects: a wrong or missing `password`, a damaged file, or one too
    /// large to convert; retrying the same request will not help. Missing access or a protected room is refused with
    /// 403 and an unknown file or folder with 404, both before any conversion starts. A result that does not fit the
    /// storage quota of the portal, the room or the caller is refused with 402.
    /// </remarks>
    /// <summary>Convert a file</summary>
    /// <path>api/2.0/docs/converter</path>
    [Tags("Docs")]
    [SwaggerResponse(200, "The converted file, as it was saved in the portal", typeof(FileDto<int>))]
    [SwaggerResponse(400, "`outputtype` is missing or the file cannot be converted to it, `folderId` is missing for a caller without \"My documents\", or the document service rejects the source: a wrong or missing password, a damaged file, or one too large")]
    [SwaggerResponse(402, "The result does not fit the storage quota of the portal, the room or the caller")]
    [SwaggerResponse(403, "You cannot read or copy the file or create files in the folder, or the file lies in a private room or in a room with a watermark")]
    [SwaggerResponse(404, "The file or the folder does not exist")]
    [HttpPost("converter")]
    public async Task<FileDto<int>> ConvertFile(DocsConverterRequestDto inDto)
    {
        var file = await fileStorageService.GetFileAsync(inDto.FileId, -1);

        var folderId = inDto.FolderId ?? await globalFolderHelper.FolderMyAsync;
        if (folderId == 0)
        {
            throw new ArgumentException("You have no \"My documents\" section: name the folder to save the result in with folderId", nameof(inDto.FolderId));
        }

        var folder = await fileStorageService.GetFolderAsync(folderId);

        var body = inDto.MapToConvertFromFileBody();

        var headers = MessageSettings.GetHttpHeaders(Request);
        var converted = await fileConverter.ConvertFromFileAsync(file, folder, body, headers);

        return await fileDtoHelper.GetAsync(converted);
    }

    /// <remarks>
    /// Queues a background run of a document builder script and answers with the operation just started. The script
    /// names portal files by identifier - `builder.OpenFile("1234")` - where the document builder documentation writes
    /// an address; only portal files can be opened, and none from a private room or a room with a watermark. A script
    /// may call OpenFile and SaveFile at most 20 times each, counting every call written in it. The caller needs edit
    /// access to every file the script opens, which may also be copied, and to each file `outputs` replaces, which must
    /// not be locked or open in an editor, and the right to create files in each folder a result is saved into. Each
    /// file the script saves goes where `outputs` says, keyed by the name given to SaveFile: `fileId` stores it as a
    /// new version of that file, `folderId` as a new file there; an unlisted file goes to `folderId` or to the folder
    /// of the opened file. Each entry of `outputs` names exactly one of `fileId` and `folderId`; `title` goes only with
    /// `folderId`, since a replaced file keeps its title, and a result stored over `fileId` has to be in that file's
    /// format. The call is not idempotent: each call is a new run. Poll `GET api/2.0/files/fileops` with the returned
    /// `id` until the operation reports `finished`: `files` then lists the saved files, a replaced one with its new
    /// version, including those saved before a failure, and `error` the reason a failed build gave. A build that has
    /// not finished after 10 minutes is given up and reported as failed. A caller runs one script at a time: a new
    /// request while an earlier one is queued or running is refused with 403, so wait for `finished` before starting
    /// the next. For a plain format change use `POST api/2.0/docs/converter`. What can be refused in advance fails at
    /// once with 400, 403 or 404.
    /// </remarks>
    /// <summary>Run a document builder script</summary>
    /// <path>api/2.0/docs/builder</path>
    [Tags("Docs")]
    [SwaggerResponse(200, "The queued document builder operation to poll", typeof(FileOperationDto))]
    [SwaggerResponse(400, "The script or the argument is malformed or too long, the script calls OpenFile or SaveFile too often, a file is addressed by an address, an `outputs` entry breaks the rules above, or a saved file has nowhere to go")]
    [SwaggerResponse(403, "You cannot edit or copy a file the script opens or write a result, a file the script opens lies in a private room or in a room with a watermark, the file to replace is locked, being edited or in the trash, or another script of yours is still queued or running")]
    [SwaggerResponse(404, "A file the script opens or replaces, or a target folder, does not exist")]
    [HttpPost("builder")]
    public async Task<FileOperationDto> RunBuilderScript(DocsBuilderRequestDto inDto)
    {
        var taskId = await fileBuilderOperationsManager.Publish(inDto.Script, inDto.FolderId, inDto.Outputs, inDto.Argument, commonLinkUtility.ServerRootPath);
        var tasks = await fileBuilderOperationsManager.GetOperationResults(taskId);

        return await fileOperationDtoHelper.GetAsync(tasks.FirstOrDefault());
    }
}
