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

using AttachmentDto = ASC.AI.Models.ResponseDto.Integration.AttachmentDto;
using AttachmentMapper = ASC.AI.Models.ResponseDto.Integration.AttachmentMapper;

namespace ASC.AI.Api.Integration;

[Scope]
[InternalRoute]
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
