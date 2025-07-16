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
    ILogger<ChatCompletionGenerator> logger,
    AiConfigurationService configurationService)
{
    public async Task<ChatCompletionGenerator> StartNewChatAsync(int roomId, string message, int? contextFolderId = null)
    {
        var config = await GetRungConfigAsync(roomId);
        
        var tenantId = tenantManager.GetCurrentTenantId();

        var userMessage = new ChatMessage(ChatRole.User, message);
        var chat = await chatHistory.AddChatAsync(tenantId, roomId, authContext.CurrentAccount.ID, userMessage);
        
        var client = await InitializeClientAsync(tenantId, roomId, chat.Id, config);
        
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, ChatPromptTemplate.GetPrompt(config.Parameters.Prompt, contextFolderId ?? roomId, roomId)),
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

        var config = await GetRungConfigAsync(chat.RoomId);

        var client = await InitializeClientAsync(tenantId, chat.RoomId, chatId, config);

        var historyAdapter = HistoryHelper.GetAdapter(config.ProviderType);
        var history = await chatHistory.GetMessagesAsync(chatId, historyAdapter).ToListAsync();
        
        var userMessage = new ChatMessage(ChatRole.User, message);
        
        await chatHistory.UpdateChatAsync(tenantId, chatId, userMessage);
        
        var messages = new List<ChatMessage>(history.Count + 2)
        {
            new(ChatRole.System, ChatPromptTemplate.GetPrompt(config.Parameters.Prompt, contextFolderId ?? chat.RoomId, chat.RoomId))
        };
        
        messages.AddRange(history);
        messages.Add(userMessage);

        return new ChatCompletionGenerator(logger, chatId, chatHistory, client, messages);
    }

    private async Task<RunConfiguration> GetRungConfigAsync(int roomId)
    {
        var room = await GetRoomAsync(roomId);
        var provider = await configurationService.GetProviderAsync(room.SettingsChatProviderId);

        var config = new RunConfiguration
        {
            Url = provider.Url,
            Key = provider.Key,
            ProviderType = provider.Type,
            Parameters = new ChatParameters
            {
                ModelId = room.SettingsChatParameters.ModelId, 
                Prompt = room.SettingsChatParameters.Prompt
            }
        };
        return config;
    }

    private async Task<IChatClient> InitializeClientAsync(int tenantId, int roomId, Guid chatId, RunConfiguration config)
    {
        var tools = await toolsProvider.GetToolsAsync(tenantId, roomId);
        var client = chatClientFactory.Create(config);
        
        var builder = client.AsBuilder();
        builder.ConfigureOptions(x => x.ConversationId = chatId.ToString());
        
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
    
    private async Task<Folder<int>> GetRoomAsync(int roomId)
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

        return room;
    }
}