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

using Status = ASC.Files.Core.Security.Status;

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The external sharing information and validation data.
/// </summary>
public class ExternalShareDto
{
    /// <summary>
    /// The external data status.
    /// </summary>
    /// <example>0</example>
    public required Status Status { get; set; }

    /// <summary>
    /// The external data ID.
    /// </summary>
    /// <example>123</example>
    public string Id { get; set; }

    /// <summary>
    /// The external data title.
    /// </summary>
    /// <example>Shared Document</example>
    public string Title { get; set; }

    /// <summary>
    /// The type of the external data.
    /// </summary>
    /// <example>0</example>
    public FileEntryType? Type { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public required int TenantId { get; set; }

    /// <summary>
    /// The unique identifier of the shared entity.
    /// </summary>
    /// <example>456</example>
    public string EntityId { get; set; }

    /// <summary>
    /// The title of the shared entity.
    /// </summary>
    /// <example>Entity Title</example>
    public string EntityTitle { get; set; }

    /// <summary>
    /// The entry type of the external data.
    /// </summary>
    /// <example>0</example>
    public FileEntryType? EntityType { get; set; }

    /// <summary>
    /// Indicates whether the entity represents a room.
    /// </summary>
    /// <example>false</example>
    public bool? IsRoom { get; set; }

    /// <summary>
    /// Specifies whether to share the external data or not.
    /// </summary>
    /// <example>true</example>
    public required bool Shared { get; set; }

    /// <summary>
    /// The link ID of the external data.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid LinkId { get; set; }

    /// <summary>
    /// Specifies whether the user is authenticated or not.
    /// </summary>
    /// <example>true</example>
    public required bool IsAuthenticated { get; set; }

    /// <summary>
    /// The room ID of the external data.
    /// </summary>
    /// <example>false</example>
    public bool IsRoomMember { get; set; }
}


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class ExternalShareDtoMapper
{
    public static partial ExternalShareDto Map(this ValidationInfo source);
}