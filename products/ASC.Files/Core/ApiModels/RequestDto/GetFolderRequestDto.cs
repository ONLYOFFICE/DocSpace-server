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

public class GetFolderRequestDto<T>
{
    /// <summary>
    /// Folder ID
    /// </summary>
    [FromRoute(Name = "folderId")]
    public T FolderId { get; set; }

    /// <summary>
    /// User or group ID
    /// </summary>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Filter type
    /// </summary>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Room ID
    /// </summary>
    [FromQuery(Name = "roomId")]
    public T RoomId { get; set; }

    /// <summary>
    /// Specifies whether to search within the section contents or not
    /// </summary>
    [FromQuery(Name = "searchInContent")]
    public bool? SearchInContent { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "withsubfolders")]
    public bool? Withsubfolders { get; set; }

    /// <summary>
    /// Specifies whether to exclude a subject or not
    /// </summary>
    [FromQuery(Name = "excludeSubject")]
    public bool? ExcludeSubject { get; set; }

    /// <summary>
    /// Specifies whether to return only files, only folders or all elements from the specified folder
    /// </summary>
    [FromQuery(Name = "applyFilterOption")]
    public ApplyFilterOption? ApplyFilterOption { get; set; }

    /// <summary>
    /// Specifies whether to search for a specific file extension
    /// </summary>
    [FromQuery(Name = "extension")]
    public string Extension { get; set; }

    /// <summary>
    /// Search area
    /// </summary>
    [FromQuery(Name = "searchArea")]
    public SearchArea SearchArea { get; set; }


    [FromQuery(Name = "formsItemKey")]
    public string FormsItemKey { get; set; }
    
    [FromQuery(Name = "formsItemType")]
    public string FormsItemType{ get; set; }
}

/// <summary>
/// 
/// </summary>
public class GetCommonFolderRequestDto
{
    /// <summary>
    /// User or group ID
    /// </summary>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Filter type
    /// </summary>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "searchInContent")]
    public bool? SearchInContent { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "withsubfolders")]
    public bool? Withsubfolders { get; set; }
}

/// <summary>
/// 
/// </summary>
public class GetMyTrashFolderRequestDto
{
    /// <summary>
    /// User or group ID
    /// </summary>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Filter type
    /// </summary>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "searchInContent")]
    public bool? SearchInContent { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "withsubfolders")]
    public bool? Withsubfolders { get; set; }

    /// <summary>
    /// Specifies whether to return only files, only folders or all elements from the specified folder
    /// </summary>
    [FromQuery(Name = "applyFilterOption")]
    public ApplyFilterOption? ApplyFilterOption { get; set; }
}

/// <summary>
/// 
/// </summary>
public class GetRootFolderRequestDto
{
    /// <summary>
    /// User or group ID
    /// </summary>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Filter type
    /// </summary>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "withsubfolders")]
    public bool? Withsubfolders { get; set; }

    /// <summary>
    /// Specifies whether to return the "Trash" section or not
    /// </summary>
    [FromQuery(Name = "withoutTrash")]
    public bool? WithoutTrash { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "searchInContent")]
    public bool? SearchInContent { get; set; }
}

/// <summary>
/// 
/// </summary>
public class GetRecentFolderRequestDto
{
    /// <summary>
    /// User or group ID
    /// </summary>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Filter type
    /// </summary>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "searchInContent")]
    public bool? SearchInContent { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "withsubfolders")]
    public bool? Withsubfolders { get; set; }

    /// <summary>
    /// Exclude a subject from the search
    /// </summary>
    [FromQuery(Name = "excludeSubject")]
    public bool? ExcludeSubject { get; set; }

    /// <summary>
    /// Scope of filters
    /// </summary>
    [FromQuery(Name = "applyFilterOption")]
    public ApplyFilterOption? ApplyFilterOption { get; set; }

    /// <summary>
    /// Search area
    /// </summary>
    [FromQuery(Name = "searchArea")]
    public SearchArea? SearchArea { get; set; }

    /// <summary>
    /// Specifies whether to search for a specific file extension
    /// </summary>
    [FromQuery(Name = "extension")]
    public string[] Extension { get; set; }
}