// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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
