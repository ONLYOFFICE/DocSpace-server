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

namespace ASC.AI.Core.Chat.Completion;

[Scope]
public class ChatCompletionRunner(
    ChatExecutionContextBuilder contextBuilder,
    AuthContext authContext,
    ChatHistory chatHistory,
    TenantManager tenantManager,
    ChatClientFactory chatClientFactory,
    ILogger<ChatCompletionGenerator> logger,
    AttachmentHandler attachmentHandler,
    ChatSocketClient chatSocketClient,
    ChatNameGenerator chatNameGenerator)
{
    public async Task<ChatCompletionGenerator> StartNewChatAsync(
        int roomId, string message, IEnumerable<JsonElement>? files = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        
        var context = await contextBuilder.BuildAsync(roomId);
        
        var attachmentsTask = GetAttachmentsAsync(files).ToListAsync();
        
        var attachments = await attachmentsTask;
        var userMessage = FormatUserMessage(message, attachments);

        var content = ChatPromptTemplate.GetPrompt(context.Instruction, context.ContextFolderId, context.Room.Id);
        
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, content),
            userMessage
        };
        
        context.UserMessage = userMessage;
        context.Message = message;
        
        var client = chatClientFactory.Create(context.ClientOptions, context.Tools);
        
        return new ChatCompletionGenerator(client, logger, chatSocketClient, messages, chatHistory, chatNameGenerator, context);
    }

    public async Task<ChatCompletionGenerator> StartChatAsync(
        Guid chatId, string message, IEnumerable<JsonElement>? files = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        
        var tenantId = tenantManager.GetCurrentTenantId();

        var chat = await chatHistory.GetChatAsync(tenantId, chatId);
        if (chat == null || chat.UserId != authContext.CurrentAccount.ID)
        {
            throw new ItemNotFoundException("Chat not found");
        }
        
        var context = await contextBuilder.BuildAsync(chat.RoomId);
        context.Chat = chat;
        
        var attachmentsTask = GetAttachmentsAsync(files).ToListAsync();

        var historyAdapter = HistoryHelper.GetAdapter(context.ClientOptions.Provider);
        var history = await chatHistory.GetMessagesAsync(chatId, historyAdapter).ToListAsync();
        
        var attachments = await attachmentsTask;
        
        var userMessage = FormatUserMessage(message, attachments);
        
        context.UserMessage = userMessage;
        context.Message = message;
        
        var system = ChatPromptTemplate.GetPrompt(context.Instruction, context.ContextFolderId, context.Room.Id);
        
        var messages = new List<ChatMessage>(history.Count + 2)
        {
            new(ChatRole.System, system)
        };
        
        messages.AddRange(history);
        messages.Add(userMessage);
        
        var client = chatClientFactory.Create(context.ClientOptions, context.Tools);

        return new ChatCompletionGenerator(client, logger, chatSocketClient, messages, chatHistory, chatNameGenerator, context);
    }

    private IAsyncEnumerable<AttachmentMessageContent> GetAttachmentsAsync(IEnumerable<JsonElement>? files)
    {
        if (files == null)
        {
            return AsyncEnumerable.Empty<AttachmentMessageContent>();
        }
        
        var (ids, thirdPartyIds) = FileOperationsManager.GetIds(files);

        return attachmentHandler.HandleAsync(ids, thirdPartyIds);
    }
    
    private static ChatMessage FormatUserMessage(string message, List<AttachmentMessageContent> attachments)
    {
        if (attachments.Count == 0)
        {
            return new ChatMessage(ChatRole.User, message);
        }

        var contents = new List<AIContent>(attachments.Count + 1);
        contents.AddRange(attachments.Select(attachment => (AIContent)attachment));
        contents.Add(new TextContent(message));

        return new ChatMessage { Role = ChatRole.User, Contents = contents };
    }
}