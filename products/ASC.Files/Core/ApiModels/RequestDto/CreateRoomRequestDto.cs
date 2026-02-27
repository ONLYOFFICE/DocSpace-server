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
/// The room type.
/// </summary>
public enum RoomType
{
    [Description("Form filling room")]
    FillingFormsRoom = 1,

    [Description("Collaboration room")]
    EditingRoom = 2,

    [Description("Custom room")]
    CustomRoom = 5,

    [Description("Public room")]
    PublicRoom = 6,

    [Description("Virtual data room")]
    VirtualDataRoom = 8,
    
    [Description("AI Room")]
    AiRoom = 9
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
            RoomType.AiRoom => FilterType.AiRooms,
            _ => FilterType.CustomRooms
        }).ToHashSet();
    }
}

/// <summary>
/// The request parameters for creating a room.
/// </summary>
public class CreateRoomRequestDto
{
    /// <summary>
    /// The room name.
    /// </summary>
    /// <example>My Room</example>
    [StringLength(170)]
    public required string Title { get; set; }

    /// <summary>
    /// The room quota.
    /// </summary>
    /// <example>1073741824</example>
    public long? Quota { get; set; }

    /// <summary>
    /// Specifies whether to create a room with indexing.
    /// </summary>
    /// <example>true</example>
    public bool? Indexing { get; set; }

    /// <summary>
    /// Specifies whether to deny downloads from the room.
    /// </summary>
    /// <example>false</example>
    public bool? DenyDownload { get; set; }

    /// <summary>
    /// The room data lifetime information.
    /// </summary>
    /// <example>{"deletePermanently": false, "period": 0, "value": 30, "enabled": true}</example>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>
    /// The watermark settings.
    /// </summary>
    /// <example>{"enabled": true, "text": "Confidential", "rotate": -45, "imageScale": 100}</example>
    public WatermarkRequestDto Watermark { get; set; }

    /// <summary>
    /// The room logo.
    /// </summary>
    /// <example>{"tmpFile": "/temp/logo.png", "x": 0, "y": 0, "width": 100, "height": 100}</example>
    public LogoRequest Logo { get; set; }

    /// <summary>
    /// The list of tags.
    /// </summary>
    /// <example>["tag1", "tag2", "tag3"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The room color.
    /// </summary>
    /// <example>#FF0000</example>
    [StringLength(6)]
    public string Color { get; set; }

    /// <summary>
    /// The room cover.
    /// </summary>
    /// <example>cover1.jpg</example>
    [StringLength(50)]
    public string Cover { get; set; }

    /// <summary>
    /// The room type.
    /// </summary>
    /// <example>2</example>
    [JsonConverter(typeof(JsonStringEnumConverter<RoomType>))]
    public required RoomType RoomType { get; set; }

    /// <summary>
    /// Specifies whether the room to be created is private or not.
    /// </summary>
    /// <example>false</example>
    public bool Private { get; set; }

    /// <summary>
    /// The collection of sharing parameters.
    /// </summary>
    /// <example>[{"shareTo": "00000000-0000-0000-0000-000000000000", "access": 1}]</example>
    [MaxEmailInvitations]
    public IEnumerable<FileShareParams> Share { get; set; }

    /// <summary>
    /// The chat settings.
    /// </summary>
    /// <example>{"providerId": 1, "modelId": "gpt-4", "prompt": "Please analyze this document"}</example>
    public ChatSettings ChatSettings { get; set; }
}