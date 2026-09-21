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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The parameters of a save request: the file in the route, the new content in a multipart body or at a download
/// address, and the autosave switch.
/// </summary>
public class SaveEditingRequestDto<T> : IModelWithFile
{
    /// <summary>
    /// The file whose content is replaced. The submitted content is written onto this file, so it has to be the file
    /// the editing session was opened on rather than a copy of it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The format the submitted content is in, with the leading dot, as in `.docx`. When it differs from the format
    /// the file is stored in, the portal converts the content before saving it. Left empty, the extension is read off
    /// the download address, and failing that the stored format is assumed.
    /// </summary>
    /// <example>.txt</example>
    [FromForm(Name = "FileExtension")]
    public string FileExtension { get; set; }

    /// <summary>
    /// An address the document service saved the document at. This operation does not fetch the content from it - the
    /// content always comes from the request body - and reads it only for the extension, when no file extension is
    /// given.
    /// </summary>
    /// <example>https://example.com/file.txt</example>
    public string DownloadUri { get; set; }

    /// <summary>
    /// The edited content, sent as the `File` part of a `multipart/form-data` body. When the part is missing the raw
    /// request body is saved as the content instead, so an empty body empties the file.
    /// </summary>
    /// <example>binary file data</example>
    [FromForm(Name = "File")]
    public IFormFile File { get; set; }

    /// <summary>
    /// Records the write as an editor autosave: the file keeps its running editing session and the previous autosave
    /// revision is overwritten. Left off, the write closes the solo editing session, is refused while somebody else
    /// has the file open, and adds a version to the history.
    /// </summary>
    /// <example>false</example>
    [FromForm(Name = "Forcesave")]
    public bool Forcesave { get; set; }
}