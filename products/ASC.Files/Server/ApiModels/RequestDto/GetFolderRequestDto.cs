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
/// The query that reads one page of the contents of a folder.
/// </summary>
public class GetFolderRequestDto<T>
{
    /// <summary>
    /// The folder whose contents are listed. Each section root has an operation of its own, such as
    /// `GET api/2.0/files/@my`, and every other folder is opened by the identifier a listing gave for it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// Restricts the listing to the entries authored by this portal member, or by the members of this group; the same
    /// parameter accepts either kind of identifier. Omit it to list everything the caller can read.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Restricts the listing to the entries this member shared, which narrows a shared listing down to what one
    /// person handed out.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "sharedBy")]
    public Guid? SharedBy { get; set; }

    /// <summary>
    /// Narrows the listing to a single kind of entry, such as documents, spreadsheets, images or one type of room.
    /// Omit it to list every kind the folder holds.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Keeps only the entries that lie in this room, which matters when the listing being read gathers entries from
    /// more than one of them.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "roomId")]
    public T RoomId { get; set; }

    /// <summary>
    /// Keeps only the folders of these kinds, each given as the number of a folder type; it is how a listing is
    /// narrowed down to, say, the form-filling folders of a room.
    /// </summary>
    /// <example>[2]</example>
    [FromQuery(Name = "folderType")]
    public List<FolderType> FolderType { get; set; }

    /// <summary>
    /// Turns `userIdOrGroupId` around: with true the entries of that member or group are the ones left out, with
    /// false they are the only ones kept.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "excludeSubject")]
    public bool? ExcludeSubject { get; set; }

    /// <summary>
    /// Chooses which half of the listing `filterType` and `filterValue` are applied to: with `Files` the folders come
    /// back unfiltered, with `Folders` the files do, and with `All` both halves are filtered.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "applyFilterOption")]
    public ApplyFilterOption? ApplyFilterOption { get; set; }

    /// <summary>
    /// Whether a narrowed request reaches into the subfolders: with true, which is what an omitted parameter means,
    /// matching entries are gathered from the whole subtree, with false only the top level is read. It makes a
    /// difference only once `filterType`, `userIdOrGroupId` or `filterValue` narrows the request, because an
    /// unfiltered listing always shows the top level alone.
    /// </summary>
    /// <example>true</example>
    [FromQuery(Name = "withSubFolders")]
    public bool? WithSubFolders { get; set; }

    /// <summary>
    /// Keeps only the files carrying one of these extensions, several of them separated by commas; the leading dot is
    /// optional.
    /// </summary>
    /// <example>docx,pdf</example>
    [FromQuery(Name = "extension")]
    public string Extension { get; set; }

    /// <summary>
    /// Which area a listing that spans several of them is taken from - the active rooms, the archive, the room
    /// templates or the form-filling rooms. A folder that belongs to one area only settles the area itself and
    /// ignores the parameter.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "searchArea")]
    public SearchArea SearchArea { get; set; }

    /// <summary>
    /// Keeps only the completed forms whose form field of this name holds a value. Take the name from
    /// `GET api/2.0/files/{folderId}/formfilter`, and use it in the folder that gathers the completed copies of a
    /// form-filling room.
    /// </summary>
    /// <example>first_name</example>
    [FromQuery(Name = "formsItemKey")]
    public string FormsItemKey { get; set; }

    /// <summary>
    /// The kind of the form field named by `formsItemKey`, taken from the same list; the two are sent together.
    /// </summary>
    /// <example>text</example>
    [FromQuery(Name = "formsItemType")]
    public string FormsItemType { get; set; }

    /// <summary>
    /// The size of one page of the listing. Pair it with `startIndex` to walk through the result, and compare the two
    /// with `total` in the response to see when the last page has been read.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The number of matching entries to skip before the returned page begins; add `count` to it to ask for the next
    /// page.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The name of the field the entries are ordered by, matched case-insensitively against the file sort fields:
    /// `DateAndTime`, `AZ`, `Size`, `Author`, `Type`, `New`, `DateAndTimeCreation`, `RoomType`, `Tags`, `Room`,
    /// `CustomOrder`, `LastOpened` and `UsedSpace`. A recognized value is also saved as the default order of the
    /// account and reused by later listings that omit the parameter, while a value matching none of the fields leaves
    /// that saved order in place.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The direction in which the `sortBy` field is ordered. It is saved together with `sortBy` as the default order
    /// of the account.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The search string the listing is filtered by: it is matched as a substring of entry titles and, for files,
    /// against the indexed document content as well. Omit it to list the folder unfiltered.
    /// </summary>
    /// <example>My Document</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }

    /// <summary>
    /// Where the entries of a tag-based listing have to live to be kept: `Room` keeps what lies in a room,
    /// `Documents` what lies in a personal section, and `Link` what was reached through an external link that is
    /// still valid. It shapes the "Favorites" and "Recent" listings and does nothing in an ordinary folder.
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
    /// Restricts the listing to the entries authored by this portal member, or by the members of this group; the same
    /// parameter accepts either kind of identifier. Omit it to list everything the caller can read.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Narrows the listing to a single kind of entry, such as documents, images or one type of room. Omit it to list
    /// every kind the section holds.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// The size of one page of section content. Pair it with `startIndex` to walk the listing, and compare the two
    /// with `total` in the response to see when the last page has been read.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The number of matching entries to skip before the returned page begins; add `count` to it to ask for the next
    /// page.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The name of the field the entries are ordered by, matched case-insensitively against the file sort fields:
    /// `DateAndTime`, `AZ`, `Size`, `Author`, `Type`, `New`, `DateAndTimeCreation`, `RoomType`, `Tags`, `Room`,
    /// `CustomOrder`, `LastOpened` and `UsedSpace`. A recognized value is also saved as the default order of the
    /// account and reused by later listings that omit the parameter, while a value matching none of the fields leaves
    /// that saved order in place.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The direction in which the `sortBy` field is ordered. It is saved together with `sortBy` as the default order
    /// of the account.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The search string the section is filtered by: it is matched as a substring of entry titles and, for files,
    /// against the indexed document content as well. Omit it to list the section unfiltered.
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
    /// Restricts the listing to the entries authored by this portal member, or by the members of this group; the same
    /// parameter accepts either kind of identifier. Omit it to list everything the caller can read.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Narrows the listing to a single kind of entry, such as documents, images or one type of room. Omit it to list
    /// every kind the section holds.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Chooses which half of the listing `filterType` and `filterValue` are applied to: with `Files` the folders come
    /// back unfiltered, with `Folders` the files do, and with `All` both halves are filtered.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "applyFilterOption")]
    public ApplyFilterOption? ApplyFilterOption { get; set; }

    /// <summary>
    /// The size of one page of section content. Pair it with `startIndex` to walk the listing, and compare the two
    /// with `total` in the response to see when the last page has been read.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The number of matching entries to skip before the returned page begins; add `count` to it to ask for the next
    /// page.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The name of the field the entries are ordered by, matched case-insensitively against the file sort fields:
    /// `DateAndTime`, `AZ`, `Size`, `Author`, `Type`, `New`, `DateAndTimeCreation`, `RoomType`, `Tags`, `Room`,
    /// `CustomOrder`, `LastOpened` and `UsedSpace`. A recognized value is also saved as the default order of the
    /// account and reused by later listings that omit the parameter, while a value matching none of the fields leaves
    /// that saved order in place.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The direction in which the `sortBy` field is ordered. It is saved together with `sortBy` as the default order
    /// of the account.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The search string the section is filtered by, matched as a substring of entry titles. Omit it to list the
    /// section unfiltered.
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
    /// Restricts the listing to the entries authored by this portal member, or by the members of this group; the same
    /// parameter accepts either kind of identifier. Omit it to list everything the caller can read.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Narrows the content listed inside every returned section to a single kind of entry, such as documents, images
    /// or one type of room. Omit it to list every kind the sections hold.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Set it to `true` to leave the "Trash" section out of the returned set of sections; with `false`, or when the
    /// parameter is omitted, the section is returned whenever the account has one of its own.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "withoutTrash")]
    public bool? WithoutTrash { get; set; }

    /// <summary>
    /// The size of the content page returned for each section separately, so a value of 1 yields one entry per
    /// section rather than one entry in total.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The number of matching entries skipped in each section before its page begins; add `count` to it to ask for
    /// the next page of every section.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The name of the field the entries are ordered by, matched case-insensitively against the file sort fields:
    /// `DateAndTime`, `AZ`, `Size`, `Author`, `Type`, `New`, `DateAndTimeCreation`, `RoomType`, `Tags`, `Room`,
    /// `CustomOrder`, `LastOpened` and `UsedSpace`. A recognized value is also saved as the default order of the
    /// account and reused by later listings that omit the parameter, while a value matching none of the fields leaves
    /// that saved order in place.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The direction in which the `sortBy` field is ordered. It is saved together with `sortBy` as the default order
    /// of the account.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The search string the content of every section is filtered by: it is matched as a substring of entry titles
    /// and, for files, against the indexed document content as well. Omit it to list the sections unfiltered.
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
    /// Restricts the listing to the files authored by this portal member, or by the members of this group; the same
    /// parameter accepts either kind of identifier. Omit it to list the whole history.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userIdOrGroupId")]
    public Guid? UserIdOrGroupId { get; set; }

    /// <summary>
    /// Narrows the listing to a single kind of file, such as documents, spreadsheets or images. Omit it to list every
    /// kind the history holds.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "filterType")]
    public FilterType? FilterType { get; set; }

    /// <summary>
    /// Inverts `userIdOrGroupId`: with `true` the files of that member or group are the ones left out of the listing
    /// instead of the only ones kept.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "excludeSubject")]
    public bool? ExcludeSubject { get; set; }

    /// <summary>
    /// Chooses which half of a listing `filterType` and `filterValue` are applied to. The "Recent" section holds
    /// files only, so the value does not change what comes back.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "applyFilterOption")]
    public ApplyFilterOption? ApplyFilterOption { get; set; }

    /// <summary>
    /// The area a listing is taken from. The "Recent" section is assembled from the caller's own open history rather
    /// than from an area, so the value does not change which files are returned.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "searchArea")]
    public SearchArea? SearchArea { get; set; }

    /// <summary>
    /// The file extensions the listing is limited to, matched against the end of the file name. The leading dot is
    /// optional, and the parameter is repeated once per extension.
    /// </summary>
    /// <example>.docx</example>
    [FromQuery(Name = "extension")]
    public string[] Extension { get; set; }

    /// <summary>
    /// The size of one page of section content. Pair it with `startIndex` to walk the listing, and compare the two
    /// with `total` in the response to see when the last page has been read.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The number of matching entries to skip before the returned page begins; add `count` to it to ask for the next
    /// page.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The name of the field the entries are ordered by, matched case-insensitively against the file sort fields:
    /// `DateAndTime`, `AZ`, `Size`, `Author`, `Type`, `New`, `DateAndTimeCreation`, `RoomType`, `Tags`, `Room`,
    /// `CustomOrder`, `LastOpened` and `UsedSpace`. A recognized value is also saved as the default order of the
    /// account and reused by later listings that omit the parameter, while a value matching none of the fields leaves
    /// that saved order in place. The "Recent" section keeps its own newest-first order, so the value does not
    /// reorder this listing.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The direction in which the `sortBy` field is ordered. It is saved together with `sortBy` as the default order
    /// of the account. The "Recent" section keeps its own newest-first order, so the value does not reorder this
    /// listing.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// The search string the history is filtered by: it is matched as a substring of file titles and against the
    /// indexed document content as well. Omit it to list the whole history.
    /// </summary>
    /// <example>My Document</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}
