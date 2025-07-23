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

namespace ASC.AI.Core.Chat.History;

public interface IHistoryWriterFactory
{
    public Task<HistoryWriter> CreateAsync();
}

public class HistoryWriter(ChatSession chat, ChatHistory chatHistory, bool isNew)
{
    public ChatSession Chat => chat;
    public bool IsNew => isNew;
    
    public async Task<int> WriteAsync(IList<ChatMessage> messages)
    {
        if (messages.Count > 0)
        {
            return await chatHistory.AddAssistantMessagesAsync(chat.Id, messages);
        }

        return 0;
    }
}

public class StartHistoryWriterFactory(
    int tenantId,
    int roomId,
    Guid userId,
    string message,
    List<AttachmentMessageContent> attachments,
    ChatHistory chatHistory) : IHistoryWriterFactory
{
    public async Task<HistoryWriter> CreateAsync()
    {
        var chat = await chatHistory.AddChatAsync(tenantId, roomId, userId, message, attachments);
        return new HistoryWriter(chat, chatHistory, true);
    }
}

public class ContinueHistoryWriterFactory(
    int tenantId,
    ChatSession chat,
    string message,
    List<AttachmentMessageContent> attachments,
    ChatHistory chatHistory) : IHistoryWriterFactory
{
    
    public async Task<HistoryWriter> CreateAsync()
    {
        await chatHistory.UpdateChatAsync(tenantId, chat.Id, message, attachments);
        return new HistoryWriter(chat, chatHistory, false);
    }
}