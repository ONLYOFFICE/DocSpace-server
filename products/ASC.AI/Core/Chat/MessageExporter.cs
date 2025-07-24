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

using System.Text;

using ASC.Core.Tenants;
using ASC.Web.Files.Utils;

namespace ASC.AI.Core.Chat;

[Scope]
public class MessageExporter(
    DbChatDao chatDao,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    TenantUtil tenantUtil,
    IServiceProvider serviceProvider,
    SocketManager socketManager,
    AuthContext authContext,
    TenantManager tenantManager)
{
    public async Task<File<T>> ExportMessageAsync<T>(T folderId, string title, int messageId)
    {
        var message = await chatDao.GetMessageAsync(messageId, authContext.CurrentAccount.ID);
        if (message == null)
        {
            throw new ItemNotFoundException("Message not found");
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);

        if (folder == null)
        {
            throw new ItemNotFoundException("Folder not found");
        }

        if (folder.FolderType is FolderType.AiRoom)
        {
            folder = await folderDao.GetFoldersAsync(folder.Id, FolderType.ResultStorage)
                .FirstOrDefaultAsync();
            
            if (folder == null)
            {
                throw new ItemNotFoundException("Folder not found");
            }
        }

        if (!await fileSecurity.CanCreateAsync(folder))
        {
            throw new SecurityException("Access denied");
        }

        var markdown = message.ToMarkdown(tenantUtil);
        var bytes = Encoding.UTF8.GetBytes(markdown);

        await using var ms = new MemoryStream(bytes);

        return await CreateFileAsync(folder.Id, ms, $"{title}.txt");
    }

    public async Task<File<int>> ExportMessagesAsync(Guid chatId)
    {
        var chat = await chatDao.GetChatAsync(tenantManager.GetCurrentTenantId(), chatId);
        if (chat == null || chat.UserId != authContext.CurrentAccount.ID)
        {
            throw new ItemNotFoundException("Chat not found");
        }

        var folderDao = daoFactory.GetFolderDao<int>();
        var resultStorage = await folderDao.GetFoldersAsync(chat.RoomId, FolderType.ResultStorage)
            .FirstOrDefaultAsync();

        if (resultStorage == null)
        {
            throw new ItemNotFoundException("Folder not found");
        }

        if (!await fileSecurity.CanCreateAsync(resultStorage))
        {
            throw new SecurityException("Access denied");
        }

        var builder = new StringBuilder();

        await foreach (var message in chatDao.GetMessagesAsync(chatId))
        {
            builder.Append(message.ToMarkdown(tenantUtil));
            builder.Append("---\n\n");
        }

        var markdown = builder.ToString();
        var bytes = Encoding.UTF8.GetBytes(markdown);

        await using var ms = new MemoryStream(bytes);

        return await CreateFileAsync(resultStorage.Id, ms, $"{chat.Title.Trim()}.txt");
    }

    private async Task<File<T>> CreateFileAsync<T>(T parentId, Stream content, string title)
    {
        var file = serviceProvider.GetService<File<T>>()!;
        file.ParentId = parentId;
        file.Comment = FilesCommonResource.CommentCreate;
        file.Title = title;
        file.ContentLength = content.Length;
        
        var fileDao = daoFactory.GetFileDao<T>();
        var savedFile = await fileDao.SaveFileAsync(file, content);
        await socketManager.CreateFileAsync(file);
        
        return savedFile;
    }
}