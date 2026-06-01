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

using ASC.AI.Models.ResponseDto.Integration;

namespace ASC.AI.Api.Integration;

[Scope]
[InternalRoute]
[ApiController]
[AiFeature]
[ControllerName("ai")]
[ApiExplorerSettings(IgnoreApi = true)]
public class PromptStorageController(PromptStorageService promptStorageService) : ControllerBase
{
    [HttpPost("integration/prompts")]
    public async Task<PromptDto> CreateAsync(CreatePromptRequestDto inDto)
    {
        var created = await promptStorageService.CreateAsync(inDto.Name, inDto.Text, inDto.FolderId);
        return PromptMapper.MapToDto(created);
    }

    [HttpPost("integration/prompts/batch")]
    public async Task<IEnumerable<PromptDto>> CreateManyAsync(CreatePromptsRequestDto inDto)
    {
        var prompts = inDto.Prompts.Select(PromptMapper.MapToCreateData).ToList();
        var created = await promptStorageService.CreateManyAsync(prompts);
        return created.Select(PromptMapper.MapToDto);
    }

    [HttpGet("integration/prompts/{id}")]
    public async Task<PromptDto> ReadByIdAsync(ReadPromptRequestDto inDto)
    {
        var prompt = await promptStorageService.ReadByIdAsync(inDto.Id);
        return PromptMapper.MapToDto(prompt);
    }

    [HttpGet("integration/prompts")]
    public async Task<IEnumerable<PromptDto>> ReadAllAsync()
    {
        var prompts = await promptStorageService.ReadAllAsync();
        return prompts.Select(PromptMapper.MapToDto);
    }

    [HttpGet("integration/prompt-folders/{id}/prompts")]
    public async Task<IEnumerable<PromptDto>> ReadByFolderAsync(ReadPromptsByFolderRequestDto inDto)
    {
        var prompts = await promptStorageService.ReadByFolderIdAsync(inDto.Id);
        return prompts.Select(PromptMapper.MapToDto);
    }

    [HttpPut("integration/prompts/{id}")]
    public async Task<IActionResult> UpdateAsync(UpdatePromptRequestDto inDto)
    {
        await promptStorageService.UpdateAsync(inDto.Id, inDto.Body.Name, inDto.Body.Text, inDto.Body.ChangeFolder, inDto.Body.FolderId);
        return NoContent();
    }

    [HttpDelete("integration/prompts/{id}")]
    public async Task<IActionResult> DeleteAsync(DeletePromptRequestDto inDto)
    {
        await promptStorageService.DeleteAsync(inDto.Id);
        return NoContent();
    }
}
