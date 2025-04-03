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
/// The parameters for creating a third-party room.
/// </summary>
public class CreateThirdPartyRoom
{
    /// <summary>
    /// Specifies whether to create a third-party room as a new folder or not.
    /// </summary>
    public bool CreateAsNewFolder { get; set; }

    /// <summary>
    /// The third-party room name to be created.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The third-party room type to be created.
    /// </summary>
    public RoomType RoomType { get; set; }

    /// <summary>
    /// Specifies whether to create the private third-party room or not.
    /// </summary>
    public bool Private { get; set; }

    /// <summary>
    /// Specifies whether to create the third-party room with indexing.
    /// </summary>
    public bool Indexing { get; set; }
    
    /// <summary>
    /// Specifies whether to deny downloads from the third-party room.
    /// </summary>
    public bool DenyDownload { get; set; }
    
    /// <summary>
    /// The color of the third-party room.
    /// </summary>
    public string Color { get; set; }

    /// <summary>
    /// The cover of the third-party room.
    /// </summary>
    public string Cover { get; set; }

    /// <summary>
    /// The list of tags of the third-party room.
    /// </summary>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The logo request parameters of the third-party room.
    /// </summary>
    public LogoRequest Logo { get; set; }
}


/// <summary>
/// The request parameters for creating a third-party room.
/// </summary>
public class CreateThirdPartyRoomRequestDto
{
    /// <summary>
    /// The ID of the folder in the third-party storage in which the contents of the room will be stored.
    /// </summary>
    [FromRoute(Name = "id")]
    public string Id { get; set; }

    /// <summary>
    /// The third-party room information.
    /// </summary>
    [FromBody]
    public CreateThirdPartyRoom Room { get; set; }
}