// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Files.Core.ApiModels.RequestDto;

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
    [FromForm(Name = "Forcesave")]
    public bool Forcesave { get; set; }
}