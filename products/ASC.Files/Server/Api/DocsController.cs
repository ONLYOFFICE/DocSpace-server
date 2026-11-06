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
    /// Converts a portal file to another format and saves the result as a new file. Requires read access to a file that
    /// allows copying and the right to create files in the folder. Other callers get 403. It also returns 403 if the file
    /// or folder is in a private room or a room with a watermark. Without `folderId`, the portal saves the result in the "My
    /// documents" section of the caller. A guest has no such section, so a guest without `folderId` gets 400. A plain conversion
    /// needs only `outputtype`. The call passes the other fields to the document service. The call changes data, keeps
    /// the source, and returns the new file. Each repeated call adds another file with a free name. The call returns only
    /// after the conversion ends. So a large file keeps the request open. The call accepts only files stored in the
    /// portal itself. To convert a file while you copy it to another folder, including a third-party one, use
    /// `POST api/2.0/files/file/{fileId}/copyas`. To convert a file to the editable format of the portal next to the
    /// source, use `PUT api/2.0/files/file/{fileId}/checkconversion`. The call returns 400 for a missing or unsupported
    /// `outputtype`. It also returns 400 for a wrong or missing `password`, or a file too large to convert. Repeating
    /// such a request does not help. The call returns 404 for an unknown file or folder. It returns 403 and 404 before
    /// the conversion starts. The call returns 402 if the result exceeds the storage quota of the portal, the room, or
    /// the caller. If the document service fails the conversion, the call returns 502. The service may be down, or the
    /// file may be damaged. A retry helps only in the first case.
    /// </remarks>
    /// <summary>Convert a file</summary>
    /// <path>api/2.0/docs/converter</path>
    [RaisesWebhook(WebhookTrigger.FileCreated)]
    [Tags("Docs")]
    [SwaggerResponse(200, "The converted file, as it was saved in the portal", typeof(FileDto<int>))]
    [SwaggerResponse(400, "`outputtype` is missing or the file cannot be converted to it, `folderId` is missing for a caller without \"My documents\", or the document service rejects the source: a wrong or missing password, or a file too large")]
    [SwaggerResponse(402, "The result does not fit the storage quota of the portal, the room or the caller")]
    [SwaggerResponse(403, "You cannot read or copy the file or create files in the folder, the file lies in a private room or in a room with a watermark, or the folder lies in a private room or in a room with a watermark")]
    [SwaggerResponse(404, "The file or the folder does not exist")]
    [SwaggerResponse(502, "The document service is unreachable or failed the conversion without naming the reason; the source may be damaged")]
    [HttpPost("converter")]
    public async Task<FileDto<int>> ConvertFile(ConvertFileRequestDto inDto)
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
    /// Queues a document builder script to run in the background. The script can open only portal files, by id. Requires
    /// edit and copy access to every file the script opens. Other callers get 403. Each file `outputs` replaces needs
    /// edit access, and each target folder needs the right to create files. Without them, the call also returns 403. It
    /// also returns 403 if a replaced file is locked, open in an editor, or in the trash. A file or folder in a private
    /// room or a room with a watermark also returns 403. The call queues work and returns the started operation. Do not
    /// repeat it. Each call starts a new run. A caller can run one script at a time. A new call while an earlier script
    /// is queued or running returns 403. To track the run, poll `GET api/2.0/files/fileops` with the returned `id` until
    /// `finished` is true. Then `files` lists the saved files, including those saved before a failure. A replaced file
    /// appears with its new version. On a failure, `error` gives the reason. The portal stops a build after 10 minutes
    /// and reports it as failed. The portal saves files missing from `outputs` in `folderId` or the folder of the
    /// opened file. To only change the format of a file, use `POST api/2.0/docs/converter`. The call returns 400 if
    /// the script has more than 20 OpenFile or SaveFile calls. It also returns 400 for an `outputs` key that the script
    /// never saves. It also returns 400 if an `outputs` entry has both `fileId` and `folderId`, or neither. A replaced
    /// file keeps its title, so an entry with `fileId` and `title` returns 400. It also returns 400 if the saved name has
    /// another extension than the file at `fileId`. A malformed script or argument also returns 400, and so does a saved
    /// file with nowhere to go. The call returns 404 if an opened or replaced file, or a target folder, does not exist.
    /// The call returns these codes before it queues the script.
    /// </remarks>
    /// <summary>Run a document builder script</summary>
    /// <path>api/2.0/docs/builder</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, When = "for each new file the queued script saves")]
    [RaisesWebhook(WebhookTrigger.FileUpdated, When = "for each existing file the queued script replaces")]
    [Tags("Docs")]
    [SwaggerResponse(200, "The queued document builder operation to poll", typeof(FileOperationDto))]
    [SwaggerResponse(400, "The script or the argument is malformed or too long, the script calls OpenFile or SaveFile too often, a file is addressed by an address, an `outputs` entry breaks the rules above, or a saved file has nowhere to go")]
    [SwaggerResponse(403, "You cannot edit or copy a file the script opens or write a result, a file the script opens lies in a private room or in a room with a watermark, the file to replace or a target folder lies in a private room or in a room with a watermark, the file to replace is locked, being edited or in the trash, or another script of yours is still queued or running")]
    [SwaggerResponse(404, "A file the script opens or replaces, or a target folder, does not exist")]
    [HttpPost("builder")]
    public async Task<FileOperationDto> RunBuilderScript(RunBuilderScriptRequestDto inDto)
    {
        var taskId = await fileBuilderOperationsManager.Publish(inDto.Script, inDto.FolderId, inDto.Outputs?.MapToFileBuilderOutputData(), inDto.Argument, commonLinkUtility.ServerRootPath);
        var tasks = await fileBuilderOperationsManager.GetOperationResults(taskId);

        return await fileOperationDtoHelper.GetAsync(tasks.FirstOrDefault());
    }
}
