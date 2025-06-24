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
public class ChatService(
    DbChatDao chatDao, 
    AuthContext authContext,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    TenantManager tenantManager)
{
    public async Task<ChatSession> RenameChatAsync(Guid chatId, string title)
    {
        var chat = await GetChatAsync(chatId);
        
        chat.Title = title;
        chat.ModifiedOn = DateTime.UtcNow;
        
        await chatDao.UpdateChatAsync(chat);
        
        return chat; 
    }
    
    public async IAsyncEnumerable<ChatSession> GetChatsAsync(int roomId, int offset, int limit)
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

        await foreach (var chat in chatDao.GetChatsAsync(tenantManager.GetCurrentTenantId(), roomId, 
                           authContext.CurrentAccount.ID, offset, limit))
        {
            yield return chat;
        }
    }
    
    public Task<int> GetChatsTotalCountAsync(int roomId)
    {
        return chatDao.GetChatsTotalCountAsync(tenantManager.GetCurrentTenantId(), roomId, authContext.CurrentAccount.ID);
    }

    public async IAsyncEnumerable<Message> GetMessagesAsync(Guid chatId, int offset, int limit)
    {
        var chat = await GetChatAsync(chatId);

        await foreach (var message in chatDao.GetMessagesAsync(chat.Id, offset, limit))
        {
            yield return message;
        }
    }

    public Task<int> GetMessagesTotalCountAsync(Guid chatId)
    {
        return chatDao.GetMessagesTotalCountAsync(chatId);
    }

    public async Task DeleteChatAsync(Guid chatId)
    {
        var chat = await GetChatAsync(chatId);
        await chatDao.DeleteChatsAsync(tenantManager.GetCurrentTenantId(), [chat.Id]);
    }

    private async Task<ChatSession> GetChatAsync(Guid chatId)
    {
        var chat = await chatDao.GetChatAsync(tenantManager.GetCurrentTenantId(), chatId);
        if (chat == null || chat.UserId != authContext.CurrentAccount.ID)
        {
            throw new ItemNotFoundException("Chat not found");
        }
        
        return chat;
    }
}