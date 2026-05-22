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

using Role = ASC.Core.Common.EF.Model.Ai.Role;

namespace ASC.AI.Core.Chat.History;

[Scope]
public class ChatHistory(ChatDao chatDao)
{
    public Task<ChatSession> AddChatAsync(
        int tenantId,
        int roomId,
        Guid userId,
        Guid chatId,
        string title,
        string message,
        List<AttachmentMessageContent> attachments)
    {
        var contents = new List<MessageContent>(attachments) { new TextMessageContent(message) };

        return chatDao.AddChatAsync(tenantId, roomId, userId, chatId, title,
            new Message(0, Role.User, contents, DateTime.UtcNow));
    }

    public Task UpdateChatAsync(int tenantId, Guid chatId, string message, List<AttachmentMessageContent> attachments)
    {
        var contents = new List<MessageContent>(attachments) { new TextMessageContent(message) };
        
        return chatDao.UpdateChatAsync(tenantId, chatId, new Message(0, Role.User, contents, DateTime.UtcNow));
    }
    
    public Task UpdateChatTitleAsync(int tenantId, Guid chatId, string title)
    {
        return chatDao.UpdateChatTitleAsync(tenantId, chatId, title);
    }

    public Task<ChatSession?> GetChatAsync(int tenantId, Guid chatId)
    {
        return chatDao.GetChatAsync(tenantId, chatId);
    }
    
    public Task<long> AddAssistantMessagesAsync(Guid chatId, IEnumerable<ChatMessage> chatMessages)
    {
        var contents = new List<MessageContent>();
        var toolCalls = new Dictionary<string, ToolCallMessageContent>();
        
        foreach (var message in chatMessages)
        {
            foreach (var content in message.Contents)
            {
                switch (content)
                {
                    case TextContent textContent:
                        {
                            contents.Add(new TextMessageContent(textContent.Text));
                            continue;
                        }
                    case FunctionCallContent functionCallContent:
                        {
                            var mcpServerInfo = functionCallContent.GetMcpServerInfo();
                            if (mcpServerInfo is { Icon: not null })
                            {
                                mcpServerInfo.Icon = null;
                            }
                            
                            var toolCall = new ToolCallMessageContent(
                                functionCallContent.CallId, 
                                functionCallContent.Name, 
                                functionCallContent.Arguments,
                                mcpServerInfo: mcpServerInfo);
                    
                            toolCalls.Add(functionCallContent.CallId, toolCall);
                            continue;
                        }
                    case FunctionResultContent functionResultContent:
                        {
                            var tool = toolCalls.GetValueOrDefault(functionResultContent.CallId);
                            if (tool == null)
                            {
                                continue;
                            }
                    
                            tool.Result = functionResultContent.Result;
                            contents.Add(tool);
                            continue;
                        }
                }
            }
        }
        
        if (contents.Count == 0)
        {
            return Task.FromResult(0L);
        }
        
        var msg = new Message(0, Role.Assistant, contents, DateTime.UtcNow);

        return chatDao.AddMessageAsync(chatId, msg);
    }

    public async Task<int?> GetLastFormFileIdAsync(Guid chatId)
    {
        int? lastFileId = null;

        await foreach (var msg in chatDao.GetMessagesAsync(chatId))
        {
            foreach (var content in msg.Contents)
            {
                if (content is TextAttachmentMessageContent attachment && attachment.Id.TryGetInt32(out var fileId))
                {
                    lastFileId = fileId;
                }
            }
        }

        return lastFileId;
    }

    public async IAsyncEnumerable<ChatMessage> GetMessagesAsync(
        Guid chatId,
        HistoryAdapter adapter,
        ChatMessage systemMessage,
        ChatMessage userMessage)
    {
        yield return systemMessage;

        var history = chatDao.GetMessagesAsync(chatId);

        await foreach (var msg in adapter.AdaptHistoryAsync(history))
        {
            yield return msg;
        }

        yield return userMessage;
    }
}