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
/// Conversion carried out by the document service on behalf of the portal.
/// </summary>
[Scope]
[ApiEndpoint("docs")]
public class DocsController(
    FileStorageService fileStorageService,
    GlobalFolderHelper globalFolderHelper,
    FileConverter fileConverter,
    FileDtoHelper fileDtoHelper)
    : ControllerBase
{
    /// <remarks>
    /// Converts a file of this portal into another format and saves the result as a new portal file, answering with
    /// that file. The source is named by identifier and read by the server, which uploads its content to the document
    /// service, so the caller never learns the address of the document service and the document service never reaches
    /// back into the portal for the source. The caller needs read access to the source file and the right to create
    /// files in the folder; without a folder the result is saved into the "My documents" section of the caller.
    /// Everything except `fileId` and `folderId` is passed on to the document service as it defines it, and
    /// `outputtype` is the only parameter a plain conversion needs: the format of the source, its name and the key
    /// its result is cached under are read off the file and cannot be set by the caller. The source is kept and the
    /// result is saved beside it under a free name, so converting the same file twice adds a second file instead of
    /// replacing the first. The call is answered once the conversion has finished, so a large file keeps the request
    /// open for as long as the document service takes.
    /// </remarks>
    /// <summary>Convert a file</summary>
    /// <path>api/2.0/docs/converter</path>
    [Tags("Docs")]
    [SwaggerResponse(200, "The converted file, as it was saved in the portal", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You do not have enough permissions to read the file or to create files in the folder")]
    [SwaggerResponse(404, "File or folder not found")]
    [HttpPost("converter")]
    public async Task<FileDto<int>> ConvertFile(DocsConverterRequestDto inDto)
    {
        var file = await fileStorageService.GetFileAsync(inDto.FileId, -1);

        var folder = await fileStorageService.GetFolderAsync(inDto.FolderId ?? await globalFolderHelper.FolderMyAsync);

        var body = new ConvertFromFileBody
        {
            CodePage = inDto.CodePage,
            Delimiter = inDto.Delimiter,
            DocumentLayout = inDto.DocumentLayout,
            DocumentRenderer = inDto.DocumentRenderer,
            OutputType = inDto.OutputType,
            Password = inDto.Password,
            Region = inDto.Region,
            Thumbnail = inDto.Thumbnail,
            SpreadsheetLayout = inDto.SpreadsheetLayout,
            Pdf = inDto.Pdf
        };

        var converted = await fileConverter.ConvertFromFileAsync(file, folder, body);

        return await fileDtoHelper.GetAsync(converted);
    }
}
