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

using System.ComponentModel;

namespace ASC.Files.Core.ApiModels.RequestDto;


public enum RoomType
{
    [SwaggerEnum(Description = "Form filling room")]
    FillingFormsRoom = 1,

    [SwaggerEnum(Description = "Collaboration room")]
    EditingRoom = 2,

    [SwaggerEnum(Description = "Custom room")]
    CustomRoom = 5,

    [SwaggerEnum(Description = "Public room")]
    PublicRoom = 6,

    [SwaggerEnum(Description = "Virtual data room")]
    VirtualDataRoom = 8
}

public static class RoomTypeExtensions
{
    public static IEnumerable<FilterType> MapToFilterType(IEnumerable<RoomType> types)
    {
        if (types == null || !types.Any())
        {
            return null;
        }
        
        return types.Select(x => x switch
        {
            RoomType.FillingFormsRoom => FilterType.FillingFormsRooms,
            RoomType.EditingRoom => FilterType.EditingRooms,
            RoomType.CustomRoom => FilterType.CustomRooms,
            RoomType.PublicRoom => FilterType.PublicRooms,
            RoomType.VirtualDataRoom => FilterType.VirtualDataRooms,
            _ => FilterType.CustomRooms
        }).ToHashSet();
    }
}

/// <summary>
/// Request parameters for creating a room
/// </summary>
public class CreateRoomRequestDto : UpdateRoomRequest
{
    /// <summary>
    /// Room type
    /// </summary>
    public RoomType RoomType { get; set; }

    /// <summary>
    /// Private
    /// </summary>
    public bool Private { get; set; }

    /// <summary>
    /// Collection of sharing parameters
    /// </summary>
    public IEnumerable<FileShareParams> Share { get; set; }
}