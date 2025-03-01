﻿// (c) Copyright Ascensio System SIA 2009-2024
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
/// Base batch request parameters
/// </summary>
public class FileBaseBatchRequestDto
{
    /// <summary>
    /// List of file IDs
    /// </summary>
    public IEnumerable<JsonElement> FileIds { get; set; } = new List<JsonElement>();
}

/// <summary>
/// Base batch request parameters
/// </summary>
public class BaseBatchRequestDto
{
    /// <summary>
    /// List of folder IDs
    /// </summary>
    public IEnumerable<JsonElement> FolderIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// List of file IDs
    /// </summary>
    public IEnumerable<JsonElement> FileIds { get; set; } = new List<JsonElement>();
}

/// <summary>
/// Request parameters for downloading files
/// </summary>
public class DownloadRequestDto : BaseBatchRequestDto
{
    /// <summary>
    /// List of file IDs which will be converted
    /// </summary>
    public IEnumerable<DownloadRequestItemDto> FileConvertIds { get; set; } = new List<DownloadRequestItemDto>();
}

public class DownloadRequestItemDto
{
    public JsonElement Key { get; init; }
    public string Value { get; init; }
    public string Password { get; init; }
}

/// <summary>
/// Request parameters for deleting files
/// </summary>
public class DeleteBatchRequestDto : BaseBatchRequestDto
{
    /// <summary>
    /// Specifies whether to delete a file after the editing session is finished or not
    /// </summary>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// Specifies whether to move a file to the \"Trash\" folder or delete it immediately
    /// </summary>
    public bool Immediately { get; set; }
}

/// <summary>
/// Parameters for deleting a file
/// </summary>
public class Delete
{
    /// <summary>
    /// Specifies whether to delete a file after the editing session is finished or not
    /// </summary>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// Specifies whether to move a file to the \"Trash\" folder or delete it immediately
    /// </summary>
    public bool Immediately { get; set; }
}

/// <summary>
/// Request parameters for deleting a file
/// </summary>
public class DeleteRequestDto<T>
{
    /// <summary>
    /// File ID
    /// </summary>
    [FromRoute(Name = "fileId")]
    public T FileId { get; set; }

    /// <summary>
    /// File
    /// </summary>
    [FromBody]
    public Delete File {  get; set; }
}

/// <summary>
/// Request parameters for copying/moving files
/// </summary>
public class BatchRequestDto : BaseBatchRequestDto
{
    /// <summary>
    /// Destination folder ID
    /// </summary>
    public JsonElement DestFolderId { get; set; }

    /// <summary>
    /// Overwriting behavior
    /// </summary>
    public FileConflictResolveType ConflictResolveType { get; set; }

    /// <summary>
    /// Specifies whether to delete a folder after the editing session is finished or not
    /// </summary>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// Content
    /// </summary>
    public bool Content { get; set; }
}

/// <summary>
/// Request parameters for checking files and folders for conflicts
/// </summary>
public class BatchSimpleRequestDto : BaseBatchRequestDto
{
    /// <summary>
    /// Destination folder ID
    /// </summary>
    public JsonElement DestFolderId { get; set; }
}