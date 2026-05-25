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
/// The request parameters for updating file contents.
/// </summary>
public class FileStreamRequestDto<T> : IModelWithFile
{
    /// <summary>
    /// The file ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The file content to update the existing file, uploaded as part of the multipart/form-data request.
    /// This property represents the new file content from the HTTP request form that will replace the existing file content.
    /// The file is accessed via the IFormFile interface which provides access to the file name, content type, length, and stream.
    /// </summary>
    /// <remarks>
    /// When making a request, the file should be sent as form data with the field name "File" and content type set to multipart/form-data.
    /// This endpoint is used to update the contents of an existing file while preserving its metadata and location.
    /// The file extension can be specified separately if the file format is being changed.
    /// </remarks>
    /// <example>
    /// Example of updating file content using curl:
    /// <code>
    /// curl -X PUT "https://api.example.com/api/2.0/files/file/123/update" \
    ///   -H "Authorization: Bearer your_token" \
    ///   -F "File=@/path/to/new_content.docx" \
    ///   -F "FileExtension=.docx" \
    ///   -F "Encrypted=false" \
    ///   -F "Forcesave=false"
    /// </code>
    ///
    /// Example of updating file content using C# HttpClient:
    /// <code>
    /// using var content = new MultipartFormDataContent();
    /// using var fileStream = File.OpenRead("new_content.docx");
    /// content.Add(new StreamContent(fileStream), "File", "new_content.docx");
    /// content.Add(new StringContent(".docx"), "FileExtension");
    /// content.Add(new StringContent("false"), "Encrypted");
    /// content.Add(new StringContent("false"), "Forcesave");
    ///
    /// var response = await httpClient.PutAsync(url, content);
    /// </code>
    /// </example>
    [FromForm(Name = "File")]
    public IFormFile File { get; set; }

    /// <summary>
    /// Specifies whether to encrypt the file or not.
    /// </summary>
    /// <example>false</example>
    [FromForm(Name = "Encrypted")]
    public bool Encrypted { get; set; }

    /// <summary>
    /// Specifies whether to force save the file or not.
    /// </summary>
    /// <example>false</example>
    [FromForm(Name = "Forcesave")]
    public bool Forcesave { get; set; }

    /// <summary>
    /// The file extension.
    /// </summary>
    /// <example>.docx</example>
    [FromForm(Name = "FileExtension")]
    public string FileExtension { get; set; }
}