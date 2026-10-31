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

namespace ASC.Files.Api;

[ConstraintRoute("int")]
[ApiEndpoint(Template = "rooms")]
[AiFeature]
public class AiHubController(
    AiHubService aiHubService,
    AiProjectSourcesDtoMapper aiProjectSourcesDtoMapper,
    AiSkillsDtoMapper aiSkillsDtoMapper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Returns the Instructions and Knowledge rooms linked to an AI project as its sources.
    /// </remarks>
    /// <summary>Get the AI project sources</summary>
    /// <path>api/2.0/files/rooms/{id}/sources</path>
    [Tags("Rooms / AI")]
    [SwaggerResponse(200, "The Instructions and Knowledge rooms linked to the project", typeof(AiProjectSourcesDto))]
    [SwaggerResponse(403, "The caller may not read the project, or the AI features are turned off on the portal")]
    [SwaggerResponse(404, "No AI project has this id")]
    [HttpGet("{id}/sources")]
    public async Task<AiProjectSourcesDto> GetProjectSources(RoomIdRequestDto<int> inDto)
    {
        var rooms = await aiHubService.GetSourcesAsync(inDto.Id);

        return await aiProjectSourcesDtoMapper.MapAsync(rooms);
    }

    /// <remarks>
    /// Links Instructions and Knowledge rooms to an AI project as its sources, which gives every project member read access to them. The caller must manage the project and each listed room; rooms that are already linked are skipped.
    /// </remarks>
    /// <summary>Link sources to an AI project</summary>
    /// <path>api/2.0/files/rooms/{id}/sources</path>
    [Tags("Rooms / AI")]
    [SwaggerResponse(204, "The rooms are linked")]
    [SwaggerResponse(400, "`roomIds` is empty or longer than 40 items, a room is not an active Instructions or Knowledge room the caller manages, or the project would hold more than 20 rooms of one kind")]
    [SwaggerResponse(403, "The caller may not edit the project, or the AI features are turned off on the portal")]
    [SwaggerResponse(404, "No AI project has this id")]
    [HttpPost("{id}/sources")]
    public async Task<IActionResult> LinkProjectSources(LinkAiProjectSourcesRequestDto inDto)
    {
        await aiHubService.LinkSourcesAsync(inDto.Id, inDto.Rooms.RoomIds);

        return NoContent();
    }

    /// <remarks>
    /// Unlinks Instructions and Knowledge rooms from an AI project; project members lose the access the link gave them. The caller must manage the project, rights on the rooms are not checked; the rooms themselves are not changed.
    /// </remarks>
    /// <summary>Unlink sources from an AI project</summary>
    /// <path>api/2.0/files/rooms/{id}/sources</path>
    [Tags("Rooms / AI")]
    [SwaggerResponse(204, "The rooms are unlinked")]
    [SwaggerResponse(400, "`roomIds` is empty or longer than 40 items")]
    [SwaggerResponse(403, "The caller may not edit the project, or the AI features are turned off on the portal")]
    [SwaggerResponse(404, "No AI project has this id")]
    [HttpDelete("{id}/sources")]
    public async Task<IActionResult> UnlinkProjectSources(UnlinkAiProjectSourcesRequestDto inDto)
    {
        await aiHubService.UnlinkSourcesAsync(inDto.Id, inDto.Rooms.RoomIds);

        return NoContent();
    }

    /// <remarks>
    /// Returns the skills of an Instructions room, or of the Instructions rooms linked to an AI project.
    /// </remarks>
    /// <summary>Get the skills of an AI room</summary>
    /// <path>api/2.0/files/rooms/{id}/skills</path>
    [Tags("Rooms / AI")]
    [SwaggerResponse(200, "The Instructions rooms with their skills", typeof(AiSkillsDto))]
    [SwaggerResponse(403, "The caller may not read the room, or the AI features are turned off on the portal")]
    [SwaggerResponse(404, "No active Instructions room or AI project has this id")]
    [HttpGet("{id}/skills")]
    public async Task<AiSkillsDto> GetRoomSkills(RoomIdRequestDto<int> inDto)
    {
        var rooms = await aiHubService.GetSkillsAsync(inDto.Id);

        return await aiSkillsDtoMapper.MapAsync(rooms);
    }

    /// <remarks>
    /// Returns the skills of several Instructions rooms. Rooms the caller cannot read are left out.
    /// </remarks>
    /// <summary>Get the skills of Instructions rooms</summary>
    /// <path>api/2.0/files/rooms/skills</path>
    [Tags("Rooms / AI")]
    [SwaggerResponse(200, "The readable Instructions rooms among the requested ones, with their skills", typeof(AiSkillsDto))]
    [SwaggerResponse(400, "`roomIds` is empty or longer than 20 items")]
    [SwaggerResponse(403, "The AI features are turned off on the portal")]
    [HttpGet("skills")]
    public async Task<AiSkillsDto> GetRoomsSkills(GetAiRoomsSkillsRequestDto inDto)
    {
        var rooms = await aiHubService.GetSkillsAsync(inDto.RoomIds);

        return await aiSkillsDtoMapper.MapAsync(rooms);
    }
}
