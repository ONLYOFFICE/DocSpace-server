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

using ASC.AI.Models.RequestDto.Integration;
using ASC.AI.Service;

namespace ASC.AI.Api.Integration;

[Scope]
[DefaultRoute]
[ApiController]
[AiFeature]
[ControllerName("ai")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AssignmentsStorageController(AssignmentsStorageService assignmentsStorageService) : ControllerBase
{
    [HttpPost("integration/assignments")]
    public async Task CreateAsync(CreateAssignmentRequestDto inDto)
    {
        await assignmentsStorageService.CreateAsync(inDto.ActionType, inDto.ProfileId);
    }

    [HttpGet("integration/assignments/{actionType}")]
    public async Task<Guid?> ReadByTypeAsync(ReadAssignmentRequestDto inDto)
    {
        return await assignmentsStorageService.ReadByTypeAsync(inDto.ActionType);
    }

    [HttpGet("integration/assignments")]
    public async Task<Dictionary<string, Guid>> ReadAllAsync()
    {
        return await assignmentsStorageService.ReadAllAsync();
    }

    [HttpPut("integration/assignments/{actionType}")]
    public async Task UpdateAsync(UpdateAssignmentRequestDto inDto)
    {
        await assignmentsStorageService.UpdateAsync(inDto.ActionType, inDto.Body.ProfileId);
    }

    [HttpPut("integration/assignments")]
    public async Task UpsertManyAsync(UpsertAssignmentsRequestDto inDto)
    {
        await assignmentsStorageService.UpsertManyAsync(inDto.Assignments);
    }

    [HttpDelete("integration/assignments/{actionType}")]
    public async Task<IActionResult> DeleteAsync(DeleteAssignmentRequestDto inDto)
    {
        await assignmentsStorageService.DeleteAsync(inDto.ActionType);
        return NoContent();
    }

    [HttpDelete("integration/assignments")]
    public async Task<IActionResult> DeleteManyAsync(DeleteAssignmentsRequestDto inDto)
    {
        await assignmentsStorageService.DeleteManyAsync(inDto.Body.ActionTypes);
        return NoContent();
    }
}
