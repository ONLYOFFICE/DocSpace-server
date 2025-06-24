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

namespace ASC.AI.Core.Chat;

[Scope]
public class ChatHistory(DbChatDao chatDao)
{
    public Task<ChatSession> AddChatAsync(int tenantId, int roomId, Guid userId, ChatMessage message)
    {
        const string suffix = "...";
        const int maxTitleLength = 255;
        
        var title = message.Text.Trim();
        if (title.Length > maxTitleLength)
        {
            title = title[..(maxTitleLength - suffix.Length)].TrimEnd() + suffix;
        }

        return chatDao.AddChatAsync(tenantId, roomId, userId, title,
            new Message(MessageType.UserMessage, [new TextMessageContent(message.Text)], DateTime.UtcNow));
    }

    public Task UpdateChatAsync(int tenantId, Guid chatId, ChatMessage message)
    {
        return chatDao.UpdateChatAsync(tenantId, chatId, new Message(MessageType.UserMessage, 
            [new TextMessageContent(message.Text)], DateTime.UtcNow));
    }

    public Task<ChatSession?> GetChatAsync(int tenantId, Guid chatId)
    {
        return chatDao.GetChatAsync(tenantId, chatId);
    }
    
    public Task AddMessagesAsync(Guid chatId, IEnumerable<ChatMessage> chatMessages)
    {
        var messages = new List<Message>();
        var toolCalls = new Dictionary<string, ToolCallMessageContent>();
        
        foreach (var message in chatMessages)
        {
            var contents = new List<MessageContent>();
            var type = message.Role.ToMessageType();
            
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
                            var toolCall = new ToolCallMessageContent(
                                functionCallContent.CallId, functionCallContent.Name, functionCallContent.Arguments);
                    
                            toolCalls.Add(functionCallContent.CallId, toolCall);
                            continue;
                        }
                    case FunctionResultContent functionResultContent:
                        {
                            type = MessageType.ToolCall;
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

            if (contents.Count == 0)
            {
                continue;
            }
            
            messages.Add(new Message(type, contents, DateTime.UtcNow));
        }

        return chatDao.AddMessagesAsync(chatId, messages);
    }

    public async IAsyncEnumerable<ChatMessage> GetMessagesAsync(Guid chatId)
    {
        await foreach (var msg in chatDao.GetMessagesAsync(chatId))
        {
            var aiContents = new List<AIContent>();
            var functionResults = new List<AIContent>();
            
            foreach (var content in msg.Contents)
            {
                switch (content)
                {
                    case TextMessageContent textContent:
                        var text = new TextContent(textContent.Text);
                        aiContents.Add(text);
                        continue;
                    case ToolCallMessageContent toolContent:
                        var functionCall = new FunctionCallContent(toolContent.CallId, toolContent.Name, toolContent.Arguments);
                        aiContents.Add(functionCall);
                        
                        if (toolContent.Result != null)
                        {
                            functionResults.Add(new FunctionResultContent(toolContent.CallId, toolContent.Result));
                        }
                        continue;
                }
            }

            var message = new ChatMessage { Role = msg.MessageType.ToChatRole(), Contents = aiContents };
            yield return message;

            if (functionResults.Count == 0)
            {
                continue;
            }
            
            var resultMessage = new ChatMessage { Role = ChatRole.Assistant, Contents = functionResults };
            
            yield return resultMessage;
        }
    }
}