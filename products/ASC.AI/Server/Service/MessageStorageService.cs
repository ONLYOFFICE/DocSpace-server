// (c) Copyright Ascensio System SIA 2009-2026
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

using ASC.AI.Integration.Messages;

using Message = ASC.AI.Integration.Messages.Message;

namespace ASC.AI.Service;

[Scope]
public class MessageStorageService(
    TenantManager tenantManager,
    MessagesStorage storage,
    ThreadStorageService threadStorageService)
{
    public async Task<Message> CreateAsync(Guid threadId, string contents)
    {
        var thread = await threadStorageService.ReadByIdAsync(threadId);

        return await storage.CreateAsync(tenantManager.GetCurrentTenantId(), thread.Id, contents);
    }

    public async Task<Message> ReadByIdAsync(Guid messageId)
    {
        var threadMessage = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), messageId)
            ?? throw new ItemNotFoundException();

        await threadStorageService.AssertAccessAsync(threadMessage.Thread);

        return threadMessage.Message;
    }

    public async Task<List<Message>> ReadByThreadAsync(Guid threadId, int? limit = null, int? startIndex = null)
    {
        var thread = await threadStorageService.ReadByIdAsync(threadId);

        return await storage.ReadByThreadAsync(tenantManager.GetCurrentTenantId(), thread.Id, limit, startIndex);
    }

    public async Task UpdateAsync(Guid messageId, string contents)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var threadMessage = await storage.ReadByIdAsync(tenantId, messageId)
            ?? throw new ItemNotFoundException();

        await threadStorageService.AssertAccessAsync(threadMessage.Thread);

        await storage.UpdateAsync(tenantId, messageId, contents);
    }

    public async Task DeleteAsync(Guid messageId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var threadMessage = await storage.ReadByIdAsync(tenantId, messageId)
            ?? throw new ItemNotFoundException();

        await threadStorageService.AssertAccessAsync(threadMessage.Thread);

        await storage.DeleteAsync(tenantId, messageId);
    }

    public async Task DeleteByThreadAsync(Guid threadId)
    {
        var thread = await threadStorageService.ReadByIdAsync(threadId);

        await storage.DeleteByThreadAsync(tenantManager.GetCurrentTenantId(), thread.Id);
    }
}
