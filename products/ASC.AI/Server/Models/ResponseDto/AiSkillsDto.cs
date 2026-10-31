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
/// Instructions rooms with their skills.
/// </summary>
public class AiSkillsDto
{
    /// <summary>
    /// The rooms, one entry per requested or linked room.
    /// </summary>
    /// <example>[{"id": 17001, "title": "Brand voice", "available": true, "skills": []}]</example>
    public required List<AiSkillsRoomDto> Rooms { get; set; }
}

/// <summary>
/// An Instructions room with its skills.
/// </summary>
public class AiSkillsRoomDto
{
    /// <summary>
    /// ID of the room.
    /// </summary>
    /// <example>17001</example>
    public required int Id { get; set; }

    /// <summary>
    /// Room title; null when the room is not available.
    /// </summary>
    /// <example>Brand voice</example>
    public string? Title { get; set; }

    /// <summary>
    /// Room logo; null when the room is not available.
    /// </summary>
    public LogoDto? Logo { get; set; }

    /// <summary>
    /// Whether the room exists, is an Instructions room and can be read by the caller.
    /// </summary>
    /// <example>true</example>
    public required bool Available { get; set; }

    /// <summary>
    /// Skills of the room, sorted by file title; empty when the room is not available.
    /// </summary>
    /// <example>[{"id": "9911", "name": "brand-voice", "description": "Writes marketing texts in the brand voice."}]</example>
    public required List<AiSkillDto> Skills { get; set; }
}

/// <summary>
/// A skill of an Instructions room: a Markdown file in the room root with a YAML frontmatter.
/// </summary>
public class AiSkillDto
{
    /// <summary>
    /// ID of the skill file.
    /// </summary>
    /// <example>9911</example>
    public required string Id { get; set; }

    /// <summary>
    /// Skill name from the frontmatter.
    /// </summary>
    /// <example>brand-voice</example>
    public required string Name { get; set; }

    /// <summary>
    /// Skill description from the frontmatter.
    /// </summary>
    /// <example>Writes marketing texts in the brand voice.</example>
    public required string Description { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class AiSkillsDtoMapper(RoomLogoManager roomLogoManager)
{
    public async Task<AiSkillsDto> MapAsync(List<RoomSkills> rooms)
    {
        var result = new AiSkillsDto { Rooms = new List<AiSkillsRoomDto>(rooms.Count) };

        foreach (var room in rooms)
        {
            result.Rooms.Add(new AiSkillsRoomDto
            {
                Id = room.RoomId,
                Title = room.Room?.Title,
                Logo = room.Room == null ? null : await roomLogoManager.GetLogoAsync(room.Room),
                Available = room.Room != null,
                Skills = Map(room.Skills)
            });
        }

        return result;
    }

    [MapProperty(nameof(Skill.FileId), nameof(AiSkillDto.Id))]
    [MapperIgnoreSource(nameof(Skill.RoomId))]
    [MapperIgnoreSource(nameof(Skill.Title))]
    private partial AiSkillDto Map(Skill source);

    private partial List<AiSkillDto> Map(List<Skill> source);
}
