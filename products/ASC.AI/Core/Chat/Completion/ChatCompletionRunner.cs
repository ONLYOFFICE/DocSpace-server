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
    AuthContext authContext,
    ChatHistory chatHistory,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    TenantManager tenantManager,
    ToolsProvider toolsProvider,
    ChatClientFactory chatClientFactory,
    ILogger<ChatCompletionGenerator> logger)
{
    private const string PromptTemplate =
        """
        You are an intelligent AI agent.
        Your task is to call the specified function(s) to fulfill the user's request.
        If a function call fails or returns an error, analyze the error message and attempt to correct your input or approach.
        Make up to three additional attempts, each time adjusting your function call based on the feedback or error details.
        After each failed attempt, clearly explain your reasoning for the next correction.
        If you are unable to succeed after several attempts, summarize the errors and provide a clear explanation of why the task could not be completed.
        
        Important: 
            All your reasoning, explanations, and answers must be in the same language as the user's original question.
            You operate within the context of a specific folder, identified by {0}.
            If you call a function that requires a folder identifier and the user has not provided one, automatically use the {0} from your current context.
        
        Instructions:
          - Carefully read the function documentation and input requirements.
          - On each attempt:
              -- Adjust parameters, data types, or formatting as needed based on the error message.
              -- If a required folder identifier is missing, use the contextual {0}.
              -- Avoid repeating the same mistake.
              -- Document each step and correction in your response.
              -- Stop retrying after three failed attempts, and report the final outcome.
          
        Example Structure
            Initial Attempt:
              - Describe your initial function call and reasoning.
            If Error Occurs:
             - Quote the error message.
             - Explain how you will adjust the next attempt.
             - Retry with revised parameters.
            Repeat up to Three Corrections:
              - For each, document the error and your new approach.
            Final Outcome:
              - If successful, explain what worked.
              - If unsuccessful, summarize the errors and possible solutions.
        """;

    public async Task<ChatCompletionGenerator> StartNewChatAsync(int roomId, string message, int? contextFolderId = null)
    {
        await ChekRoomAsync(roomId);
        
        var tenantId = tenantManager.GetCurrentTenantId();

        var userMessage = new ChatMessage(ChatRole.User, message);
        var chat = await chatHistory.AddChatAsync(tenantId, roomId, authContext.CurrentAccount.ID, userMessage);
        
        var client = await CreateClientAsync(tenantId, roomId, chat.Id);
        
        var folderId = contextFolderId ?? roomId;
        
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, string.Format(PromptTemplate, folderId)),
            userMessage
        };

        var metadata = new Metadata { ChatId = chat.Id };
        
        return new ChatCompletionGenerator(logger, chat.Id, chatHistory, client, messages, metadata);
    }

    public async Task<ChatCompletionGenerator> StartChatAsync(Guid chatId, string message, int? contextFolderId = null)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var chat = await chatHistory.GetChatAsync(tenantId, chatId);
        if (chat == null || chat.UserId != authContext.CurrentAccount.ID)
        {
            throw new ItemNotFoundException("Chat not found");
        }

        await ChekRoomAsync(chat.RoomId);
        
        var clientTask = await CreateClientAsync(tenantId, chat.RoomId, chatId);

        var history = await chatHistory.GetMessagesAsync(chatId).ToListAsync();
        var userMessage = new ChatMessage(ChatRole.User, message);
        
        await chatHistory.UpdateChatAsync(tenantId, chatId, userMessage);
        
        var folderId = contextFolderId ?? chat.RoomId;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, string.Format(PromptTemplate, folderId))
        };
        
        messages.AddRange(history);
        messages.Add(userMessage);

        return new ChatCompletionGenerator(logger, chatId, chatHistory, clientTask, messages);
    }

    private async Task<IChatClient> CreateClientAsync(int tenantId, int roomId, Guid chatId)
    {
        var toolsTask = toolsProvider.GetToolsAsync(tenantId, roomId);
        var client = await chatClientFactory.CreateAsync();
        
        var builder = client.AsBuilder();
        builder.ConfigureOptions(x => x.ConversationId = chatId.ToString());
        
        var tools = await toolsTask;
        if (tools is { Count: > 0 })
        {
            builder.ConfigureOptions(x =>
            {
                x.Tools = tools;
                x.ToolMode = ChatToolMode.Auto;
                x.AllowMultipleToolCalls = true;
            }).UseFunctionInvocation();
        }
        
        return builder.Build();
    }
    
    private async Task ChekRoomAsync(int roomId)
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var room = await folderDao.GetFolderAsync(roomId);
        if (room == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanUseChatsAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFolder);
        }
    }
}