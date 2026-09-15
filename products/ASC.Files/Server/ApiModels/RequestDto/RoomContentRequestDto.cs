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
/// The room content request parameters.
/// </summary>
public class RoomContentRequestDto
{
    /// <summary>
    /// Keeps only the rooms of the listed kinds. Repeat the parameter to pass more than one value; they are combined
    /// with OR, and omitting it returns the rooms of every kind.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "type")]
    public IEnumerable<RoomType> Type { get; set; }

    /// <summary>
    /// Keeps only the rooms this account or group has access to, which is how the rooms of one member are listed. The
    /// identifier comes from the portal people and group listings, and the exclude flag turns the filter into its
    /// opposite.
    /// </summary>
    /// <example>9a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9</example>
    [FromQuery(Name = "subjectId")]
    public Guid? SubjectId { get; set; }

    /// <summary>
    /// Keeps only the rooms created by this account, regardless of who else was invited to them. The identifier comes
    /// from the portal people listing, and the exclude flag turns the filter into its opposite.
    /// </summary>
    /// <example>9a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9</example>
    [FromQuery(Name = "subjectOwnerId")]
    public Guid? SubjectOwnerId { get; set; }

    /// <summary>
    /// The section to list. Every section is a separate root and a room belongs to exactly one of them at a time, so
    /// archiving a room moves it out of the active section. The default is the active section, which leaves the
    /// form-filling rooms to their own value.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "searchArea")]
    public SearchArea? SearchArea { get; set; }

    /// <summary>
    /// When true, keeps only the rooms that carry no tag at all, which is the complement of the tag filter. When
    /// false or omitted, tags play no part in the selection.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "withoutTags")]
    public bool? WithoutTags { get; set; }

    /// <summary>
    /// A JSON array of tag names serialized into a single query value, for example ["Important","Legal"]. A room
    /// matches when it carries any one of them. Take the names from `GET api/2.0/files/tags`; a name that is not in
    /// the catalog simply matches nothing.
    /// </summary>
    /// <example>["Important"]</example>
    [FromQuery(Name = "tags")]
    public string Tags { get; set; }

    /// <summary>
    /// Inverts the two subject filters: when true, the rooms of the named account are the ones left out of the answer
    /// instead of the only ones kept. It does nothing on its own.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "excludeSubject")]
    public bool? ExcludeSubject { get; set; }

    /// <summary>
    /// Keeps only the rooms whose content lives in the named third-party service, for portals where rooms may be
    /// connected to external storage. The default keeps rooms of every origin.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "provider")]
    public ProviderFilter? Provider { get; set; }

    /// <summary>
    /// Splits the rooms by whether a storage quota was set on the room itself or it follows the portal default, which
    /// is how rooms with a custom limit are found.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "quotaFilter")]
    public QuotaFilter? QuotaFilter { get; set; }

    /// <summary>
    /// Splits the rooms by where their content is stored, in the portal itself or in a connected third-party account.
    /// It is the coarse form of the provider filter.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "storageFilter")]
    public StorageFilter? StorageFilter { get; set; }

    /// <summary>
    /// Splits the rooms by whether they are private, that is encrypted rooms whose content the portal cannot read.
    /// Omitting it returns both kinds.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "privacyFilter")]
    public RoomPrivacyFilter? PrivacyFilter { get; set; }

    /// <summary>
    /// How many rooms one page may carry. Ask for the next page by raising the start index by the number of rooms
    /// already received.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// How many matching rooms to skip before the page begins. Page through the answer until the skip plus the rooms
    /// received reaches the total it reports.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The field to order the rooms by, named as in the file listings: `AZ` for the title, `DateAndTime` for the last
    /// change, `DateAndTimeCreation`, `Author`, `Size`, `Type`, `RoomType`, `Tags`, `UsedSpace`, `LastOpened`. The
    /// name is matched ignoring case, an unknown one is rejected rather than ignored, and the accepted one also
    /// becomes this account's stored order.
    /// </summary>
    /// <example>DateAndTime</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The direction of the order chosen by the sort field. It has no effect when no sort field is given and the
    /// stored order of the account is used.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// Keeps only the rooms whose title contains this text, ignoring case. It is a substring match over the title
    /// alone: room content and tags are not searched.
    /// </summary>
    /// <example>Sales</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }

    /// <summary>
    /// Keeps only the rooms that belong to this room group. The identifier comes from `GET api/2.0/files/group`; the
    /// groups of portal members are a different concept and their identifiers do not match here.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "groupId")]
    public int? GroupId { get; set; }
}