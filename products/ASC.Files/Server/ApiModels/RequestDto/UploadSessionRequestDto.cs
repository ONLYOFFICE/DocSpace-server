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
/// The request parameters for uploading a file in a session.
/// </summary>
public class UploadSessionRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public T FolderId { get; set; }

    /// <summary>
    /// The upload session ID.
    /// </summary>
    /// <example>session_abc123</example>
    [FromRoute(Name = "sessionId")]
    public string SessionId { get; set; }

    /// <summary>
    /// The file to be uploaded as part of the multipart/form-data request.
    /// This property represents the uploaded file content from the HTTP request form.
    /// The file is accessed via the IFormFile interface which provides access to the file name, content type, length, and stream.
    /// </summary>
    /// <remarks>
    /// When making a request, the file should be sent as form data with the content type set to multipart/form-data.
    /// The file stream can be accessed using the OpenReadStream() method.
    /// </remarks>
    /// <example>
    /// Example of sending a file using curl:
    /// <code>
    /// curl -X POST "https://api.example.com/api/2.0/files/folder/1/upload/session_abc123" \
    ///   -H "Authorization: Bearer your_token" \
    ///   -F "file=@/path/to/document.pdf"
    /// </code>
    ///
    /// Example of sending a file using C# HttpClient:
    /// <code>
    /// using var content = new MultipartFormDataContent();
    /// using var fileStream = File.OpenRead("document.pdf");
    /// content.Add(new StreamContent(fileStream), "file", "document.pdf");
    ///
    /// var response = await httpClient.PostAsync(url, content);
    /// </code>
    /// </example>
    public IFormFile File { get; set; }
}

/// <summary>
/// The request parameters for async uploading a file chunk in a session.
/// </summary>
public class UploadSessionAsyncRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public T FolderId { get; set; }

    /// <summary>
    /// The upload session ID.
    /// </summary>
    /// <example>session_abc123</example>
    [FromRoute(Name = "sessionId")]
    public string SessionId { get; set; }

    /// <summary>
    /// The chunk number.
    /// </summary>
    /// <example>1</example>
    [FromQuery]
    public int? ChunkNumber { get; set; }

    /// <summary>
    /// The file chunk to be uploaded as part of the multipart/form-data request.
    /// This property represents the uploaded file chunk content from the HTTP request form for chunked upload operations.
    /// The file chunk is accessed via the IFormFile interface which provides access to the chunk content and length.
    /// </summary>
    /// <remarks>
    /// When making a request, the file chunk should be sent as form data with the content type set to multipart/form-data.
    /// For large files, the upload can be split into multiple chunks, each sent in a separate request with a corresponding ChunkNumber.
    /// </remarks>
    /// <example>
    /// Example of sending a file chunk using curl:
    /// <code>
    /// curl -X POST "https://api.example.com/api/2.0/files/folder/1/upload/session_abc123?chunkNumber=1" \
    ///   -H "Authorization: Bearer your_token" \
    ///   -F "file=@/path/to/chunk1.part"
    /// </code>
    ///
    /// Example of sending a file chunk using C# HttpClient:
    /// <code>
    /// using var content = new MultipartFormDataContent();
    /// var chunkStream = new MemoryStream(chunkBytes);
    /// content.Add(new StreamContent(chunkStream), "file", "document.pdf");
    ///
    /// var response = await httpClient.PostAsync($"{url}?chunkNumber=1", content);
    /// </code>
    /// </example>
    [FromForm]
    public IFormFile File { get; set; }
}
