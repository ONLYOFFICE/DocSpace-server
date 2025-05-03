// (c) Copyright Ascensio System SIA 2009-2025
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
/// The base batch request parameters.
/// </summary>
public class BaseBatchRequestDto
{
    /// <summary>
    /// The list of folder IDs of the base batch request.
    /// </summary>
    public IEnumerable<JsonElement> FolderIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// The list of file IDs of the base batch request.
    /// </summary>
    public IEnumerable<JsonElement> FileIds { get; set; } = new List<JsonElement>();
}

/// <summary>
/// The request parameters for downloading files.
/// </summary>
public class DownloadRequestDto
{
    /// <summary>
    /// The list of folder IDs to be downloaded.
    /// </summary>
    public IEnumerable<JsonElement> FolderIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// The list of file IDs to be downloaded.
    /// </summary>
    public IEnumerable<JsonElement> FileIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// The list of file IDs which will be converted.
    /// </summary>
    public IEnumerable<DownloadRequestItemDto> FileConvertIds { get; set; } = new List<DownloadRequestItemDto>();
}

/// <summary>
/// The download request item with conversion parameters and security settings.
/// </summary>
public class DownloadRequestItemDto
{
    /// <summary>
    /// The unique identifier or reference key for the file to be downloaded.
    /// </summary>
    public required JsonElement Key { get; init; }

    /// <summary>
    /// The target format or conversion type for the file download.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// The optional password for accessing protected files.
    /// </summary>
    public string Password { get; init; }
}

/// <summary>
/// The request parameters for deleting files.
/// </summary>
public class DeleteBatchRequestDto
{
    /// <summary>
    /// The list of folder IDs to be deleted.
    /// </summary>
    public IEnumerable<JsonElement> FolderIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// The list of file IDs to be deleted.
    /// </summary>
    public IEnumerable<JsonElement> FileIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// Specifies whether to delete a file after the editing session is finished or not
    /// </summary>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// Specifies whether to move a file to the \"Trash\" folder or delete it immediately.
    /// </summary>
    public bool Immediately { get; set; }
}

/// <summary>
/// The request parameters for deleting file versions.
/// </summary>
public class DeleteVersionBatchRequestDto
{
    /// <summary>
    /// Specifies whether to delete a file after the editing session is finished or not.
    /// </summary>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// The file ID to delete.
    /// </summary>
    public required int FileId { get; set; }
    
    /// <summary>
    /// The collection of file versions to be deleted.
    /// </summary>
    public required IEnumerable<int> Versions { get; set; } = new List<int>();
}

/// <summary>
/// The parameters for deleting a file.
/// </summary>
public class Delete
{
    /// <summary>
    /// Specifies whether to delete a file after the editing session is finished or not.
    /// </summary>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// Specifies whether to move a file to the \"Trash\" folder or delete it immediately.
    /// </summary>
    public bool Immediately { get; set; }
}

/// <summary>
/// The request parameters for deleting a file.
/// </summary>
public class DeleteRequestDto<T>
{
    /// <summary>
    /// The file ID to delete.
    /// </summary>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The parameters for deleting a file.
    /// </summary>
    [FromBody]
    public required Delete File {  get; set; }
}

/// <summary>
/// The request parameters for copying/moving files.
/// </summary>
public class BatchRequestDto
{
    /// <summary>
    /// The list of folder IDs to be copied/moved.
    /// </summary>
    public IEnumerable<JsonElement> FolderIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// The list of file IDs to be copied/moved.
    /// </summary>
    public IEnumerable<JsonElement> FileIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// The destination folder ID.
    /// </summary>
    public JsonElement DestFolderId { get; set; }

    /// <summary>
    /// The overwriting behavior of the file copying or moving.
    /// </summary>
    public FileConflictResolveType ConflictResolveType { get; set; }

    /// <summary>
    /// Specifies whether to delete the source files/folders after they are moved or copied to the destination folder.
    /// </summary>
    public bool DeleteAfter { get; set; }

    /// <summary>
    ///  Specifies whether to copy or move the folder content or not.
    /// </summary>
    public bool Content { get; set; }

    /// <summary>
    /// Specifies whether the file is copied for filling out
    /// </summary>
    public bool ToFillOut { get; set; }
}

/// <summary>
/// The data transfer object containing the operation type for which statuses are retrieved.
/// </summary>
public class FileOperationResultRequestDto
{
    /// <summary>
    /// Specifies the type of file operation to be retrieved.
    /// </summary>
    [FromRoute(Name = "operationType")]
    public required FileOperationType OperationType { get; set; }
}