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
/// The request parameters for getting a folder.
/// </summary>
public class GetFolderRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The user or group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// The identifier of the user who shared the folder or file.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "sharedBy")]
    public Guid? SharedBy { get; set; }

    /// <summary>
    /// The filter type.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// The room ID.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "roomId")]
    public T RoomId { get; set; }

    /// <summary>
    /// Specifies whether to exclude search by user or group ID.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "excludeSubject")]
    public bool? ExcludeSubject { get; set; }

    /// <summary>
    /// Specifies whether to return only files, only folders, or all elements from the specified folder.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "applyFilterOption")]
    public ApplyFilterOption? ApplyFilterOption { get; set; }

    /// <summary>
    /// Specifies whether to search for the specific file extension.
    /// </summary>
    /// <example>.docx</example>
    [FromQuery(Name = "extension")]
    public string Extension { get; set; }

    /// <summary>
    /// The search area.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "searchArea")]
    public SearchArea SearchArea { get; set; }

    /// <summary>
    /// The forms item key.
    /// </summary>
    /// <example>doc_key_123</example>
    [FromQuery(Name = "formsItemKey")]
    public string FormsItemKey { get; set; }

    /// <summary>
    /// The forms item type.
    /// </summary>
    /// <example>text</example>
    [FromQuery(Name = "formsItemType")]
    public string FormsItemType { get; set; }

    /// <summary>
    /// The maximum number of items to retrieve in the request.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The zero-based index of the first item to retrieve in a paginated request.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The property used for sorting the folder request results.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The order in which the results are sorted.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The text value used as a filter parameter for folder content queries.
    /// </summary>
    /// <example>My Document</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }

    /// <summary>
    /// The location context of the request, specifying the area
    /// where the operation is performed, such as a room, documents, or a link.
    /// </summary>
    /// <example>1</example>
    public Location? Location { get; set; }
}

/// <summary>
/// The request parameters for getting the "Common" folder.
/// </summary>
public class GetCommonFolderRequestDto
{
    /// <summary>
    /// The user or group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// The filter type.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// The maximum number of items to retrieve in the request.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The zero-based index of the first item to retrieve in a paginated list.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// Specifies the field by which the folder content should be sorted.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The order in which the results are sorted.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The text used as a filter or search criterion for folder content queries.
    /// </summary>
    /// <example>My Document</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}

/// <summary>
/// The request parameters for getting the "My trash" folder.
/// </summary>
public class GetMyTrashFolderRequestDto
{
    /// <summary>
    /// The user or group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// The filter type.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Specifies whether to return only files, only folders or all elements.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "applyFilterOption")]
    public ApplyFilterOption? ApplyFilterOption { get; set; }

    /// <summary>
    /// The maximum number of items to retrieve in the response.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The starting position of the items to be retrieved.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The property used to specify the sorting criteria for folder contents.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The order in which the results are sorted.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The text used for filtering or searching folder contents.
    /// </summary>
    /// <example>My Document</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}

/// <summary>
/// The request parameters for getting the root folder.
/// </summary>
public class GetRootFolderRequestDto
{
    /// <summary>
    /// The user or group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// The filter type.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Specifies whether to return the "Trash" section or not.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "withoutTrash")]
    public bool? WithoutTrash { get; set; }

    /// <summary>
    /// The maximum number of items to retrieve in the response.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The starting position of the items to be retrieved.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// Specifies the field by which the folder content should be sorted.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The order in which the results are sorted.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The text used as a filter for searching or retrieving folder contents.
    /// </summary>
    /// <example>My Document</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}

/// <summary>
/// The request parameters for getting the "Recent" folder request parameters.
/// </summary>
public class GetRecentFolderRequestDto
{
    /// <summary>
    /// The user or group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// The filter type.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Specifies whether to exclude search by user or group ID.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "excludeSubject")]
    public bool? ExcludeSubject { get; set; }

    /// <summary>
    /// Specifies whether to return only files, only folders or all elements.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "applyFilterOption")]
    public ApplyFilterOption? ApplyFilterOption { get; set; }

    /// <summary>
    /// The search area.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "searchArea")]
    public SearchArea? SearchArea { get; set; }

    /// <summary>
    /// Specifies whether to search for a specific file extension in the "Recent" folder.
    /// </summary>
    /// <example>.docx</example>
    [FromQuery(Name = "extension")]
    public string[] Extension { get; set; }

    /// <summary>
    /// The maximum number of items to return.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The starting position of the results to be returned in the query response.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// Specifies the sorting criteria for the folder request.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The order in which the results are sorted.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The text used for filtering or searching folder contents.
    /// </summary>
    /// <example>My Document</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}