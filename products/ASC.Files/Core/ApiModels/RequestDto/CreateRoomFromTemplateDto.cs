﻿// (c) Copyright Ascensio System SIA 2009-2025
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
/// The parameters for creating a room from a template.
/// </summary>
public class CreateRoomFromTemplateDto
{
    /// <summary>
    /// The template ID from which the room to be created.
    /// </summary>
    public required int TemplateId { get; set; }

    /// <summary>
    /// The room title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// The logo request parameters.
    /// </summary>
    public LogoRequest Logo { get; set; }

    /// <summary>
    /// Specifies whether to copy a logo or not.
    /// </summary>
    public bool CopyLogo { get; set; }

    /// <summary>
    /// The collection of tags.
    /// </summary>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The color of the room to be created.
    /// </summary>
    [StringLength(6)]
    public string Color { get; set; }

    /// <summary>
    /// The cover of the room to be created.
    /// </summary>
    [StringLength(50)]
    public string Cover { get; set; }

    /// <summary>
    /// The room quota.
    /// </summary>
    public long? Quota { get; set; }

    /// <summary>
    /// Specifies whether to create a room with indexing.
    /// </summary>
    public bool? Indexing { get; set; }

    /// <summary>
    /// Specifies whether to deny downloads from the room.
    /// </summary>
    public bool? DenyDownload { get; set; }

    /// <summary>
    /// The room data lifetime information.
    /// </summary>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>
    /// The watermark settings.
    /// </summary>
    public WatermarkRequestDto Watermark { get; set; }

    /// <summary>
    /// Specifies whether the room to be created is private or not.
    /// </summary>
    public bool? Private { get; set; }
}
