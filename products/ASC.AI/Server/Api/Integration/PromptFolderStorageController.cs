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
public class PromptFolderStorageController(PromptFolderStorageService promptFolderStorageService) : ControllerBase
{
    [HttpPost("integration/prompt-folders")]
    public async Task<PromptFolderDto> CreateAsync(CreatePromptFolderRequestDto inDto)
    {
        var created = await promptFolderStorageService.CreateAsync(inDto.Name);
        return PromptFolderMapper.MapToDto(created);
    }

    [HttpPost("integration/prompt-folders/batch")]
    public async Task<IReadOnlyList<PromptFolderDto>> CreateManyAsync(CreatePromptFoldersRequestDto inDto)
    {
        var created = await promptFolderStorageService.CreateManyAsync(inDto.Names);
        return created.Select(PromptFolderMapper.MapToDto).ToList();
    }

    [HttpGet("integration/prompt-folders/{id}")]
    public async Task<PromptFolderDto> ReadByIdAsync(ReadPromptFolderRequestDto inDto)
    {
        var folder = await promptFolderStorageService.ReadByIdAsync(inDto.Id);
        return PromptFolderMapper.MapToDto(folder);
    }

    [HttpGet("integration/prompt-folders")]
    public async Task<List<PromptFolderDto>> ReadAllAsync()
    {
        var folders = await promptFolderStorageService.ReadAllAsync();
        return folders.Select(PromptFolderMapper.MapToDto).ToList();
    }

    [HttpPut("integration/prompt-folders/{id}")]
    public async Task<IActionResult> RenameAsync(RenamePromptFolderRequestDto inDto)
    {
        await promptFolderStorageService.RenameAsync(inDto.Id, inDto.Body.Name);
        return NoContent();
    }

    [HttpDelete("integration/prompt-folders/{id}")]
    public async Task<IActionResult> DeleteAsync(DeletePromptFolderRequestDto inDto)
    {
        await promptFolderStorageService.DeleteAsync(inDto.Id);
        return NoContent();
    }
}
