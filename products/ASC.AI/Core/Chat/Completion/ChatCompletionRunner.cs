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
    ExecutionContextProvider contextProvider, 
    IHttpClientFactory httpClientFactory,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    TenantManager tenantManager,
    ToolsProvider toolsProvider)
{
    private static readonly ChatMessage _systemMessage = new(ChatRole.System, "You are a helpful assistant."); // TODO: move to prompt file
    
    public async Task<ChatCompletionGenerator> StartNewChatAsync(int roomId, string message)
    {
        await ChekRoomAsync(roomId);
        
        var tenantId = tenantManager.GetCurrentTenantId();
        
        var clientTask = CreateClientAsync();
        var toolsTask = toolsProvider.GetToolsAsync(tenantId, roomId);

        var userMessage = new ChatMessage(ChatRole.User, message);
        var chat = await chatHistory.AddChatAsync(tenantId, roomId, authContext.CurrentAccount.ID, userMessage);
        
        var messages = new List<ChatMessage>
        {
            _systemMessage,
            userMessage
        };

        var metadata = new Metadata { ChatId = chat.Id };
        
        return new ChatCompletionGenerator(chat.Id, chatHistory, await clientTask, messages, await toolsTask, metadata);
    }

    public async Task<ChatCompletionGenerator> StartChatAsync(Guid chatId, string message)
    {
        var chat = await chatHistory.GetChatAsync(chatId);
        if (chat == null || chat.UserId != authContext.CurrentAccount.ID)
        {
            throw new ItemNotFoundException("Chat not found");
        }

        await ChekRoomAsync(chat.RoomId);
        
        var clientTask = CreateClientAsync();
        var toolsTask = toolsProvider.GetToolsAsync(tenantManager.GetCurrentTenantId(), chat.RoomId);

        var history = await chatHistory.GetMessagesAsync(chatId).ToListAsync();
        var userMessage = new ChatMessage(ChatRole.User, message);
        
        await chatHistory.UpdateChatAsync(chatId, userMessage);

        var messages = new List<ChatMessage> { _systemMessage };
        messages.AddRange(history);
        messages.Add(userMessage);

        return new ChatCompletionGenerator(chatId, chatHistory, await clientTask, messages, await toolsTask);
    }

    private async Task<IChatClient> CreateClientAsync()
    {
        var context = await contextProvider.GetExecutionContextAsync();
        
        return new ChatClientBuilder(new OpenAIClient(
                    new ApiKeyCredential(context.Key),
                    new OpenAIClientOptions
                    { 
                        Endpoint = context.Endpoint,
                        Transport = new HttpClientPipelineTransport(httpClientFactory.CreateClient())
                    })
                .GetChatClient(context.Model)
                .AsIChatClient())
            .UseFunctionInvocation()
            .Build();
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