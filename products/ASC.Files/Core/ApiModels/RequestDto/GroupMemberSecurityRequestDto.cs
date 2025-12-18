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
/// The request parameters for getting the group member security information.
/// </summary>
public class GroupMemberSecurityFolderRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The group ID.
    /// </summary>
    [FromRoute(Name = "groupId")]
    public required Guid GroupId { get; set; }

    /// <summary>
    /// The number of items to be retrieved in the current query.
    /// </summary>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The starting index for the query result set.
    /// </summary>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The filter value used for searching or querying group members based on text input.
    /// </summary>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}
/// <summary>
/// The group member security request generic parameters.
/// </summary>
public class GroupMemberSecurityFileRequestDto<T>
{
    /// <summary>
    /// The file ID.
    /// </summary>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The group ID.
    /// </summary>
    [FromRoute(Name = "groupId")]
    public required Guid GroupId { get; set; }

    /// <summary>
    /// The number of items to be retrieved in the current query.
    /// </summary>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The starting index for the query result set.
    /// </summary>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The filter value used for searching or querying group members based on text input.
    /// </summary>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}