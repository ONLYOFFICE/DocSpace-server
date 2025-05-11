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
/// The security information request parameters.
/// </summary>
public class SecurityInfoRequestDto
{
    /// <summary>
    /// The list of the shared folder IDs.
    /// </summary>
    public List<JsonElement> FolderIds { get; set; } = [];

    /// <summary>
    /// The list of the shared file IDs.
    /// </summary>
    public List<JsonElement> FileIds { get; set; } = [];

    /// <summary>
    /// The collection of sharing parameters.
    /// </summary>
    public List<FileShareParams> Share { get; set; }

    /// <summary>
    /// Specifies whether to notify users about the shared file or not.
    /// </summary>
    public bool Notify { get; set; }

    /// <summary>
    /// The message to send when notifying about the shared file.
    /// </summary>
    public string SharingMessage { get; set; }
}

/// <summary>
/// The parameters of the security information request.
/// </summary>
public class SecurityInfoSimpleRequestDto
{
    /// <summary>
    /// The collection of sharing parameters.
    /// </summary>
    public List<FileShareParams> Share { get; set; }

    /// <summary>
    /// Specifies whether to notify users about the shared file or not.
    /// </summary>
    public bool Notify { get; set; }

    /// <summary>
    /// The message to send when notifying about the shared file.
    /// </summary>
    public string SharingMessage { get; set; }
}

/// <summary>
/// The parameters of the security information request for the specified file.
/// </summary>
public class FileSecurityInfoSimpleRequestDto<T>
{
    /// <summary>
    /// The file ID.
    /// </summary>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The parameters of the security information simple request.
    /// </summary>
    [FromBody]
    public SecurityInfoSimpleRequestDto SecurityInfoSimpe { get; set; }
}

/// <summary>
/// The security information request parameters for the specified folder.
/// </summary>
public class FolderSecurityInfoSimpleRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The parameters of the security information simple request.
    /// </summary>
    [FromBody]
    public SecurityInfoSimpleRequestDto SecurityInfoSimpe { get; set; }
}