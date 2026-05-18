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
/// The request parameters for saving file edits.
/// </summary>
public class SaveEditingRequestDto<T> : IModelWithFile
{
    /// <summary>
    /// The editing file ID from the request.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The editing file extension from the request.
    /// </summary>
    /// <example>.txt</example>
    [FromForm(Name = "FileExtension")]
    public string FileExtension { get; set; }

    /// <summary>
    /// The URI to download the editing file.
    /// </summary>
    /// <example>https://example.com/file.txt</example>
    public string DownloadUri { get; set; }

    /// <summary>
    /// The edited file to be saved, uploaded as part of the multipart/form-data request.
    /// This property represents the modified file content from the HTTP request form after editing operations.
    /// The file is accessed via the IFormFile interface which provides access to the file name, content type, length, and stream.
    /// </summary>
    /// <remarks>
    /// When making a request, the edited file should be sent as form data with the field name "File" and content type set to multipart/form-data.
    /// This is typically used in scenarios where a file has been edited (e.g., in an online editor) and needs to be saved back to the server.
    /// Either this property or DownloadUri should be provided to save the edited file.
    /// </remarks>
    /// <example>
    /// Example of saving an edited file using curl:
    /// <code>
    /// curl -X PUT "https://api.example.com/api/2.0/files/file/123/saveediting" \
    ///   -H "Authorization: Bearer your_token" \
    ///   -F "File=@/path/to/edited_document.docx" \
    ///   -F "FileExtension=.docx" \
    ///   -F "Forcesave=false"
    /// </code>
    ///
    /// Example of saving an edited file using C# HttpClient:
    /// <code>
    /// using var content = new MultipartFormDataContent();
    /// using var fileStream = File.OpenRead("edited_document.docx");
    /// content.Add(new StreamContent(fileStream), "File", "edited_document.docx");
    /// content.Add(new StringContent(".docx"), "FileExtension");
    /// content.Add(new StringContent("false"), "Forcesave");
    ///
    /// var response = await httpClient.PutAsync(url, content);
    /// </code>
    /// </example>
    [FromForm(Name = "File")]
    public IFormFile File { get; set; }

    /// <summary>
    /// Specifies whether to force save the file or not.
    /// </summary>
    /// <example>false</example>
    [FromForm(Name = "Forcesave")]
    public bool Forcesave { get; set; }
}