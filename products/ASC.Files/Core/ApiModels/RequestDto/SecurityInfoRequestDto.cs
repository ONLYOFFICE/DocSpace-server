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
/// Security information request parameters
/// </summary>
public class SecurityInfoRequestDto : BaseBatchRequestDto
{
    /// <summary>
    /// Collection of sharing parameters
    /// </summary>
    public IEnumerable<FileShareParams> Share { get; set; }

    /// <summary>
    /// Notifies users about the shared file or not
    /// </summary>
    public bool Notify { get; set; }

    /// <summary>
    /// Message to send when notifying about the shared file
    /// </summary>
    public string SharingMessage { get; set; }
}

/// <summary>
/// Security information request parameters
/// </summary>
public class SecurityInfoSimpleRequestDto
{
    /// <summary>
    /// Collection of sharing parameters
    /// </summary>
    public IEnumerable<FileShareParams> Share { get; set; }

    /// <summary>
    /// Notifies users about the shared file or not
    /// </summary>
    public bool Notify { get; set; }

    /// <summary>
    /// Message to send when notifying about the shared file
    /// </summary>
    public string SharingMessage { get; set; }
}

/// <summary>
/// Security information request parameters
/// </summary>
public class FileSecurityInfoSimpleRequestDto<T>
{
    /// <summary>
    /// File ID
    /// </summary>
    [FromRoute(Name = "fileId")]
    public T FileId { get; set; }

    /// <summary>
    /// Security info simple
    /// </summary>
    [FromBody]
    public SecurityInfoSimpleRequestDto SecurityInfoSimpe { get; set; }
}

/// <summary>
/// Security information request parameters
/// </summary>
public class FolderSecurityInfoSimpleRequestDto<T>
{
    /// <summary>
    /// Folder ID
    /// </summary>
    [FromRoute(Name = "folderId")]
    public T FolderId { get; set; }

    /// <summary>
    /// Security info simple
    /// </summary>
    [FromBody]
    public SecurityInfoSimpleRequestDto SecurityInfoSimpe { get; set; }
}