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

namespace ASC.AI.Models.ResponseDto;

/// <summary>
/// The rooms linked to an AI project.
/// </summary>
public class AiProjectRoomsDto
{
    /// <summary>
    /// Linked Instructions rooms, sorted by title.
    /// </summary>
    /// <example>[{"id": 17001, "title": "Brand voice"}]</example>
    public required List<AiProjectRoomDto> InstructionsRooms { get; set; }

    /// <summary>
    /// Linked Knowledge rooms, sorted by title.
    /// </summary>
    /// <example>[{"id": 17020, "title": "Product catalogue"}]</example>
    public required List<AiProjectRoomDto> KnowledgeRooms { get; set; }
}

/// <summary>
/// A room linked to an AI project.
/// </summary>
public class AiProjectRoomDto
{
    /// <summary>
    /// ID of the room.
    /// </summary>
    /// <example>17001</example>
    public required int Id { get; set; }

    /// <summary>
    /// Room title.
    /// </summary>
    /// <example>Brand voice</example>
    public required string Title { get; set; }

    /// <summary>
    /// Room logo.
    /// </summary>
    public LogoDto? Logo { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class AiProjectRoomsDtoMapper(RoomLogoManager roomLogoManager)
{
    public async Task<AiProjectRoomsDto> MapAsync(IEnumerable<Folder<int>> rooms)
    {
        var result = new AiProjectRoomsDto { InstructionsRooms = [], KnowledgeRooms = [] };

        foreach (var room in rooms.OrderBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase).ThenBy(r => r.Id))
        {
            var dto = Map(room);
            dto.Logo = await roomLogoManager.GetLogoAsync(room);

            (room.FolderType == FolderType.InstructionsRoom ? result.InstructionsRooms : result.KnowledgeRooms).Add(dto);
        }

        return result;
    }

    [MapperIgnoreTarget(nameof(AiProjectRoomDto.Logo))]
    private partial AiProjectRoomDto Map(Folder<int> room);
}
