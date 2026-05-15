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
    /// Specifies whether to include files from subfolders in the results.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "withSubFolders")]
    public bool? WithSubFolders { get; set; }

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