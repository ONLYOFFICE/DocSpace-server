// (c) Copyright Ascensio System SIA 2009-2024
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

public class RoomContentRequestDto
{
    /// <summary>
    /// Filter by room type
    /// </summary>
    [FromQuery(Name = "type")]
    public IEnumerable<RoomType> Type { get; set; }

    /// <summary>
    /// Filter by user ID
    /// </summary>
    [FromQuery(Name = "subjectId")]
    public string SubjectId { get; set; }

    /// <summary>
    /// Specifies whether to return sections with or without subfolders
    /// </summary>
    [FromQuery(Name = "withSubfolders")]
    public bool? WithSubfolders { get; set; }

    /// <summary>
    /// Room search area (Active, Archive, Any)
    /// </summary>
    [FromQuery(Name = "searchArea")]
    public SearchArea? SearchArea { get; set; }

    /// <summary>
    /// Specifies whether to search by tags or not
    /// </summary>
    [FromQuery(Name = "withoutTags")]
    public bool? WithoutTags { get; set; }

    /// <summary>
    /// Tags in the serialized format
    /// </summary>
    [FromQuery(Name = "tags")]
    public string Tags { get; set; }

    /// <summary>
    /// Specifies whether to exclude a subject or not
    /// </summary>
    [FromQuery(Name = "excludeSubject")]
    public bool? ExcludeSubject { get; set; }

    /// <summary>
    /// Filter by provider name (None, Box, DropBox, GoogleDrive, kDrive, OneDrive, WebDav)
    /// </summary>
    [FromQuery(Name = "provider")]
    public ProviderFilter? Provider { get; set; }

    /// <summary>
    /// Filter by subject (Owner - 1, Member - 1)
    /// </summary>
    [FromQuery(Name = "subjectFilter")]
    public SubjectFilter? SubjectFilter { get; set; }

    /// <summary>
    /// Filter by quota (Default - 1, Custom - 2)
    /// </summary>
    [FromQuery(Name = "quotaFilter")]
    public QuotaFilter? QuotaFilter { get; set; }

    /// <summary>
    /// Filter by storage (Internal - 1, ThirdParty - 2)
    /// </summary>
    [FromQuery(Name = "storageFilter")]
    public StorageFilter? StorageFilter { get; set; }
}