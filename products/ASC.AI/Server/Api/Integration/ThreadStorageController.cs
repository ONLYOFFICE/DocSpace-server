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
public class ThreadStorageController(ThreadStorageService threadStorageService) : ControllerBase
{
    [HttpPost("integration/threads")]
    public async Task<ThreadDto> CreateAsync(CreateThreadRequestDto inDto)
    {
        var created = await threadStorageService.CreateAsync(inDto.Title, inDto.ProfileId, inDto.EntityId);
        return ThreadMapper.MapToDto(created);
    }

    [HttpGet("integration/threads/{id}")]
    public async Task<ThreadDto> ReadByIdAsync(ReadThreadRequestDto inDto)
    {
        var thread = await threadStorageService.ReadByIdAsync(inDto.Id);
        return ThreadMapper.MapToDto(thread);
    }

    [HttpGet("integration/threads")]
    public async Task<IEnumerable<ThreadDto>> ReadAllAsync(ReadAllThreadsRequestDto inDto)
    {
        var threads = await threadStorageService.ReadAllAsync(inDto.EntityId);
        return threads.Select(ThreadMapper.MapToDto);
    }

    [HttpPut("integration/threads/{id}")]
    public async Task<IActionResult> UpdateAsync(UpdateThreadRequestDto inDto)
    {
        await threadStorageService.UpdateAsync(inDto.Id, inDto.Body.Title);
        return NoContent();
    }

    [HttpPatch("integration/threads/{id}/touch")]
    public async Task<IActionResult> TouchAsync(TouchThreadRequestDto inDto)
    {
        await threadStorageService.TouchAsync(inDto.Id, inDto.Body.LastEditDate, inDto.Body.ProfileId, inDto.Body.ClearProfile);
        return NoContent();
    }

    [HttpDelete("integration/threads/{id}")]
    public async Task<IActionResult> DeleteAsync(DeleteThreadRequestDto inDto)
    {
        await threadStorageService.DeleteAsync(inDto.Id);
        return NoContent();
    }
}
