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

namespace ASC.AI.Models.ResponseDto;

/// <summary>
/// The chat message information.
/// </summary>
public class MessageDto(long id, Role role, IEnumerable<MessageContentDto> contents, ApiDateTime createdOn)
{
    /// <summary>
    /// The unique identifier of the message.
    /// </summary>
    /// <example>42</example>
    public long Id { get; } = id;

    /// <summary>
    /// The role of the message author: User or Assistant.
    /// </summary>
    /// <example>0</example>
    public Role Role { get; } = role;

    /// <summary>
    /// The ordered collection of content blocks that make up the message body (text, tool calls, or attachments).
    /// </summary>
    public IEnumerable<MessageContentDto> Contents { get; } = contents;

    /// <summary>
    /// The date and time when the message was created.
    /// </summary>

    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public ApiDateTime CreatedOn { get; } = createdOn;
}

[Scope]
public class MessageDtoConverter(
    TenantManager tenantManager,
    ApiDateTimeHelper dateTimeHelper,
    McpService mcpService,
    McpIconStore iconStore,
    DataContentDtoMapper dataContentDtoMapper)
{
    public async Task<MessageDto> ConvertAsync(Message message)
    {
        var createdOn = dateTimeHelper.Get(message.CreatedOn);
        var contents = new List<MessageContentDto>(message.Contents.Count);
        var mcpToolCalls = new Dictionary<Guid, List<ToolContentDto>>();

        foreach (var content in message.Contents)
        {
            switch (content)
            {
                case ToolCallMessageContent toolCall:
                    {
                        var toolDto = toolCall.MapToDto();
                        contents.Add(toolDto);

                        if (toolCall.McpServerInfo != null)
                        {
                            if (mcpToolCalls.TryGetValue(toolCall.McpServerInfo.ServerId, out var toolCalls))
                            {
                                toolCalls.Add(toolDto);
                            }
                            else
                            {
                                mcpToolCalls.Add(toolCall.McpServerInfo.ServerId, [toolDto]);
                            }
                        }
                
                        continue;
                    }
                case DataMessageContent data:
                    contents.Add(dataContentDtoMapper.MapToDto(data));
                    continue;
                case TextAttachmentMessageContent attachment:
                    contents.Add(attachment.MapToDto());
                    continue;
                case TextMessageContent text:
                    contents.Add(text.MapToDto());
                    break;
            }
        }

        if (mcpToolCalls.Count <= 0)
        {
            return new MessageDto(message.Id, message.Role, contents, createdOn);
        }

        var tenantId = tenantManager.GetCurrentTenantId();
            
        await foreach(var iconState in mcpService.GetIconStatesAsync(mcpToolCalls.Keys))
        {
            if (!iconState.HasIcon || !mcpToolCalls.TryGetValue(iconState.ServerId, out var toolCalls))
            {
                continue;
            }

            foreach (var toolCall in toolCalls)
            {
                toolCall.McpServerInfo!.Icon =
                    await iconStore.GetAsync(tenantId, iconState.ServerId, iconState.ModifiedOn);
            }
        }

        return new MessageDto(message.Id, message.Role, contents, createdOn);
    }
}