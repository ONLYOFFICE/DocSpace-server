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

using AttachmentDto = ASC.AI.Models.ResponseDto.Integration.AttachmentDto;
using AttachmentMapper = ASC.AI.Models.ResponseDto.Integration.AttachmentMapper;

namespace ASC.AI.Api.Integration;

[Scope]
[DefaultRoute]
[ApiController]
[AiFeature]
[ControllerName("ai")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AttachmentsStorageController(AttachmentsStorageService attachmentsStorageService) : ControllerBase
{
    [HttpPost("integration/attachments")]
    public async Task<List<AttachmentDto>> CreateManyAsync(CreateAttachmentsRequestDto inDto)
    {
        return await attachmentsStorageService.CreateManyAsync(inDto.Body.EntryIds)
            .Select(AttachmentMapper.MapToDto)
            .ToListAsync();
    }

    [HttpGet("integration/attachments/{id}")]
    public async Task<AttachmentDto> ReadByIdAsync(ReadAttachmentRequestDto inDto)
    {
        var attachment = await attachmentsStorageService.ReadByIdAsync(inDto.Id);
        return AttachmentMapper.MapToDto(attachment);
    }

    [HttpPost("integration/attachments/read")]
    public async Task<List<AttachmentDto>> ReadManyByIdsAsync(ReadAttachmentsRequestDto inDto)
    {
        return await attachmentsStorageService.ReadManyByIdsAsync(inDto.Body.Ids)
            .Select(AttachmentMapper.MapToDto)
            .ToListAsync();
    }

    [HttpPut("integration/attachments")]
    public async Task<IActionResult> UpdateBindingAsync(UpdateAttachmentsBindingRequestDto inDto)
    {
        await attachmentsStorageService.UpdateManyAsync(inDto.Body.Ids, inDto.Body.MessageId);
        return NoContent();
    }

    [HttpDelete("integration/attachments/{id}")]
    public async Task<IActionResult> DeleteAsync(DeleteAttachmentRequestDto inDto)
    {
        await attachmentsStorageService.DeleteAsync(inDto.Id);
        return NoContent();
    }

    [HttpDelete("integration/attachments")]
    public async Task<IActionResult> DeleteManyAsync(DeleteAttachmentsRequestDto inDto)
    {
        await attachmentsStorageService.DeleteManyAsync(inDto.Body.Ids);
        return NoContent();
    }
}
