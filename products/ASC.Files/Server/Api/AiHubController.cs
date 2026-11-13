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
    FolderContentDtoHelper folderContentDtoHelper,
    ApiContext apiContext,
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

    /// <remarks>
    /// Returns the AI model selected for the chat of an AI project, as the ID of an AI model profile. Any project member with read access may call it. The call is read-only. The result holds only the project's own choice: `profileId` is null when nobody has selected a model for the project yet, or when the selected profile has been removed from the portal since. In both cases the portal-wide model is used instead, which this operation does not return. To change the model, use `PUT api/2.0/files/rooms/{id}/model`.
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
    /// Selects the AI model that answers in the chat of an AI project, replacing the previous choice, and returns the model now selected. Take `profileId` from the profiles that `GET api/2.0/ai/profiles/list` returns. The caller must be able to edit the project; reading it is not enough. The change applies to every member of the project, is recorded in the audit trail and notifies the project's open clients. Repeating the call with the same profile changes nothing. A model can only be replaced by another one: there is no operation that clears the selection and returns the project to the portal-wide model. The profile must exist on the portal and be able to chat; a profile known to serve only other kinds of tasks, such as image generation, is refused with 400.
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

    /// <remarks>
    /// Returns the artifacts of a thread in an AI project: the files the chat tools created in it. Only the author of the thread can read them.
    /// </remarks>
    /// <summary>Get the AI thread artifacts</summary>
    /// <path>api/2.0/files/rooms/{id}/threads/{threadId}/artifacts</path>
    /// <collection>list</collection>
    [Tags("Rooms / AI")]
    [SwaggerResponse(200, "The artifacts of the thread", typeof(IEnumerable<FileDto<int>>))]
    [SwaggerResponse(400, "`count` is outside its allowed range or `startIndex` is negative")]
    [SwaggerResponse(403, "The caller may not read the project, or the AI features are turned off on the portal")]
    [SwaggerResponse(404, "No AI project has this id, or it has no such thread of the caller")]
    [HttpGet("{id}/threads/{threadId:guid}/artifacts")]
    public async Task<IEnumerable<FileDto<int>>> GetThreadArtifacts(GetAiThreadFilesRequestDto inDto)
    {
        var folderId = await aiHubService.GetThreadArtifactsFolderIdAsync(inDto.Id, inDto.ThreadId);
        if (folderId is null)
        {
            apiContext.SetCount(0).SetTotalCount(0);

            return [];
        }

        var content = await folderContentDtoHelper.GetAsync(
            folderId.Value,
            null,
            null,
            null,
            0,
            false,
            false,
            false,
            ApplyFilterOption.All,
            SearchArea.Active,
            inDto.SortBy,
            inDto.SortOrder,
            inDto.StartIndex,
            inDto.Count,
            inDto.Text);

        var files = content.Files.OfType<FileDto<int>>().ToList();

        apiContext.SetCount(files.Count).SetTotalCount(content.Total);

        return files;
    }
}
