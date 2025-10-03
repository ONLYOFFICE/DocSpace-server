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
public class ChatExecutionContextBuilder(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ChatDao chatDao,
    TenantManager tenantManager,
    AuthContext authContext,
    AiProviderDao providerDao,
    ChatTools chatTools,
    UserManager userManager)
{
    public async Task<ChatExecutionContext> BuildAsync(int roomId)
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        
        var room = await folderDao.GetFolderAsync(roomId);
        if (room == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanUseChatAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFolder);
        }
        
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;
        
        var providerTask = providerDao.GetProviderAsync(tenantId, room.SettingsChatProviderId);
        var resultStorageTask = folderDao.GetFoldersAsync(room.Id, FolderType.ResultStorage).FirstAsync();
        var chatSettingsTask = chatDao.GetUserChatSettingsAsync(tenantId, roomId, userId);
        var userTask = userManager.GetUsersAsync(userId);
        
        var provider = await providerTask;
        if (provider == null)
        {
            throw new ItemNotFoundException("Provider not found");
        }
        
        var chatSettings = await chatSettingsTask;
        
        var toolsTask = chatTools.GetAsync(roomId, chatSettings);
        
        var resultStorage = await resultStorageTask;
        var tools = await toolsTask;
        var user = await userTask;

        var context = new ChatExecutionContext
        {
            TenantId = tenantId,
            User = user,
            Room = room,
            ClientOptions = new ChatClientOptions
            {
                Provider = provider.Type,
                Endpoint= provider.Url,
                Key = provider.Key,
                ModelId = room.SettingsChatParameters.ModelId,
            },
            Instruction = room.SettingsChatParameters.Prompt,
            ContextFolderId = resultStorage.Id,
            ChatSettings = chatSettings,
            Tools = tools
        };
        
        return context;
    }
}

public class ChatExecutionContext : IAsyncDisposable
{
    public int TenantId { get; init; }
    public required UserInfo User { get; init; }
    public required Folder<int> Room { get; init; }
    public required ChatClientOptions ClientOptions { get; init; }
    public string? Instruction { get; init; }
    public int ContextFolderId { get; init; }
    public required UserChatSettings ChatSettings { get; init; }
    public required ToolHolder Tools { get; init; }
    public ChatMessage? UserMessage { get; set; }
    public string RawMessage { get; set; } = string.Empty;
    public List<AttachmentMessageContent> Attachments { get; set; } = [];
    public ChatSession? Chat { get; set; }

    public async ValueTask DisposeAsync()
    {
        await Tools.DisposeAsync();
    }
}