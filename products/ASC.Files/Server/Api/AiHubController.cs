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
    /// Requires read access to the project. Other callers get 403.
    /// The call is read-only and returns the two kinds of rooms in separate lists.
    /// Both lists are empty when nothing is linked.
    /// If the ID is not an AI project, the call returns 404.
    /// It also returns 403 if AI features are turned off on the portal.
    /// To link rooms, use `POST api/2.0/files/rooms/{id}/sources`.
    /// To unlink them, use `DELETE api/2.0/files/rooms/{id}/sources`.
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
    /// Links Instructions and Knowledge rooms to an AI project as its sources.
    /// Every project member then gets read access to these rooms.
    /// A member with a higher role in a room keeps that role.
    /// Requires the right to manage the project. Other callers get 403, including members with read access only.
    /// Each listed room must be an Instructions or Knowledge room that the caller also manages.
    /// If any room fails this check, the call returns 400 and links nothing.
    /// The call changes data and skips rooms that are already linked.
    /// So you can repeat it with the same body.
    /// It returns 400 if `roomIds` is empty or too long.
    /// It also returns 400 if the project would hold more than 20 rooms of one kind.
    /// If the ID is not an AI project, the call returns 404.
    /// AI features turned off on the portal also return 403.
    /// To see the linked rooms, use `GET api/2.0/files/rooms/{id}/sources`.
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
    /// Unlinks Instructions and Knowledge rooms from an AI project.
    /// Project members then lose the read access that the link gave them.
    /// The rooms themselves do not change.
    /// Requires the right to manage the project. Other callers get 403, even a manager of the listed rooms.
    /// The call does not check rights on the rooms.
    /// The call changes data and ignores IDs that are not linked to the project.
    /// So you can repeat it with the same body.
    /// The call returns 400 if `roomIds` is empty or too long.
    /// If the ID is not an AI project, the call returns 404.
    /// It also returns 403 if AI features are turned off on the portal.
    /// To see the linked rooms, use `GET api/2.0/files/rooms/{id}/sources`.
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
    /// Requires read access to the room or the project. Other callers get 403.
    /// The call is read-only.
    /// For a project, it lists the linked Instructions rooms but no Knowledge rooms.
    /// It sorts the rooms by title, then by ID.
    /// It sorts the skills of a room by file name, then by `id`.
    /// The call lists a room without skills with an empty `skills` list.
    /// A project without linked Instructions rooms gives an empty `rooms` list.
    /// An unknown ID or another kind of room, such as a Knowledge room, returns 404.
    /// It also returns 403 if AI features are turned off on the portal.
    /// To get the skills of several Instructions rooms, use `GET api/2.0/files/rooms/skills`.
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
    /// Returns the skills of several Instructions rooms.
    /// Any signed-in user can call it.
    /// The call is read-only.
    /// It returns only the Instructions rooms that the caller can read.
    /// It leaves out unknown IDs and other kinds of rooms without an error.
    /// Each room appears once, even if its ID repeats.
    /// A room without skills has an empty `skills` list.
    /// The call returns 400 if `roomIds` is empty or has more than 20 items.
    /// It returns 403 if AI features are turned off on the portal.
    /// To get the skills of one room or of an AI project, use `GET api/2.0/files/rooms/{id}/skills`.
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

    /// <remarks>
    /// Returns the AI model selected for the chat of an AI project, as the ID of an AI model profile.
    /// Requires read access to the project. Other callers get 403.
    /// The call is read-only and returns only the project's own choice.
    /// If `profileId` is null, the chat uses the portal-wide model, which this call does not return.
    /// If the ID is not an AI project, the call returns 404.
    /// It also returns 403 if AI features are turned off on the portal.
    /// To change the model, use `PUT api/2.0/files/rooms/{id}/model`.
    /// </remarks>
    /// <summary>Get the AI project model</summary>
    /// <path>api/2.0/files/rooms/{id}/model</path>
    [Tags("Rooms / AI")]
    [SwaggerResponse(200, "The AI model profile selected for the project, or null when none is selected", typeof(AiProjectModelDto))]
    [SwaggerResponse(403, "The caller may not read the project, or the AI features are turned off on the portal")]
    [SwaggerResponse(404, "No AI project has this id")]
    [HttpGet("{id}/model")]
    public async Task<AiProjectModelDto> GetProjectModel(RoomIdRequestDto<int> inDto)
    {
        var profile = await aiHubService.GetModelAsync(inDto.Id);

        return profile.Map();
    }

    /// <remarks>
    /// Selects the AI model that answers in the chat of an AI project.
    /// The new choice replaces the previous one for every project member.
    /// Requires the right to manage the project. Other callers get 403, including members with read access only.
    /// The call changes data, and you can repeat it with the same body.
    /// No call clears the selection and returns the project to the portal-wide model.
    /// The call returns 400 for an invalid body or an unknown `profileId`.
    /// It also returns 400 if the profile serves only other tasks, such as image generation.
    /// If the ID is not an AI project, the call returns 404.
    /// AI features turned off on the portal also return 403.
    /// </remarks>
    /// <summary>Set the AI project model</summary>
    /// <path>api/2.0/files/rooms/{id}/model</path>
    [Tags("Rooms / AI")]
    [SwaggerResponse(200, "The AI model profile now selected for the project", typeof(AiProjectModelDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `profileId`, the portal has no AI model profile with this id, or the profile cannot be used for chat")]
    [SwaggerResponse(403, "The caller may not edit the project, or the AI features are turned off on the portal")]
    [SwaggerResponse(404, "No AI project has this id")]
    [HttpPut("{id}/model")]
    public async Task<AiProjectModelDto> SetProjectModel(UpdateAiProjectModelRequestDto inDto)
    {
        var profile = await aiHubService.SetModelAsync(inDto.Id, inDto.Model.ProfileId);

        return profile.Map();
    }
}
