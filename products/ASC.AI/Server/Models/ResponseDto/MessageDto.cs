// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.AI.Models.ResponseDto;

public class MessageDto(int id,Role role, IEnumerable<MessageContentDto> contents, ApiDateTime createdOn)
{
    public int Id { get; } = id;
    public Role Role { get; } = role;
    public IEnumerable<MessageContentDto> Contents { get; } = contents;
    public ApiDateTime CreatedOn { get; } = createdOn;
}

[Scope]
public class MessageDtoConverter(
    TenantManager tenantManager,
    ApiDateTimeHelper dateTimeHelper,
    McpService mcpService,
    McpIconStore iconStore, 
    IMapper mapper)
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
                        var toolDto = mapper.Map<ToolContentDto>(toolCall);
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
                case AttachmentMessageContent attachment:
                    contents.Add(mapper.Map<AttachmentContentDto>(attachment));
                    continue;
                case TextMessageContent text:
                    contents.Add(mapper.Map<TextContentDto>(text));
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