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

using MessageDto = ASC.AI.Models.ResponseDto.Integration.MessageDto;
using MessageMapper = ASC.AI.Models.ResponseDto.Integration.MessageMapper;

namespace ASC.AI.Api.Integration;

[Scope]
[InternalRoute]
[ApiController]
[AiFeature]
[ControllerName("ai")]
[ApiExplorerSettings(IgnoreApi = true)]
public class MessageStorageController(MessageStorageService messageStorageService) : ControllerBase
{
    [HttpPost("integration/threads/{threadId}/messages")]
    public async Task<MessageDto> CreateAsync(CreateMessageRequestDto inDto)
    {
        var created = await messageStorageService.CreateAsync(inDto.ThreadId, inDto.Body.Contents);
        return MessageMapper.MapToDto(created);
    }

    [HttpGet("integration/messages/{id}")]
    public async Task<MessageDto> ReadByIdAsync(ReadMessageRequestDto inDto)
    {
        var message = await messageStorageService.ReadByIdAsync(inDto.Id);
        return MessageMapper.MapToDto(message);
    }

    [HttpGet("integration/threads/{threadId}/messages")]
    public async Task<List<MessageDto>> ReadByThreadAsync(ReadMessagesByThreadRequestDto inDto)
    {
        var messages = await messageStorageService.ReadByThreadAsync(inDto.ThreadId, inDto.Limit, inDto.StartIndex);
        return messages.Select(MessageMapper.MapToDto).ToList();
    }

    [HttpPut("integration/messages/{id}")]
    public async Task<IActionResult> UpdateAsync(UpdateMessageRequestDto inDto)
    {
        await messageStorageService.UpdateAsync(inDto.Id, inDto.Body.Contents);
        return NoContent();
    }

    [HttpDelete("integration/messages/{id}")]
    public async Task<IActionResult> DeleteAsync(DeleteMessageRequestDto inDto)
    {
        await messageStorageService.DeleteAsync(inDto.Id);
        return NoContent();
    }

    [HttpDelete("integration/threads/{threadId}/messages")]
    public async Task<IActionResult> DeleteByThreadAsync(DeleteMessagesByThreadRequestDto inDto)
    {
        await messageStorageService.DeleteByThreadAsync(inDto.ThreadId);
        return NoContent();
    }
}
