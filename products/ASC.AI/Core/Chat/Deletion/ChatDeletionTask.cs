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

namespace ASC.AI.Core.Chat.Deletion;

[Transient]
public class ChatDeletionTask : DistributedTaskProgress
{
    private readonly IServiceScopeFactory _serviceScopeFactory = null!;
    private int _tenantId;
    private Guid _userId;
    private Guid _chatId;
    private IReadOnlyCollection<int> _fileIds = [];

    public ChatDeletionTask() { }

    public ChatDeletionTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(int tenantId, Guid userId, Guid chatId, IReadOnlyCollection<int>? fileIds = null)
    {
        _tenantId = tenantId;
        _userId = userId;
        _chatId = chatId;
        _fileIds = fileIds ?? [];
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ChatDeletionTask>>();

        try
        {
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileDao = daoFactory.GetFileDao<int>();
            var chatDao = scope.ServiceProvider.GetRequiredService<ChatDao>();

            var fileIds = new HashSet<int>(_fileIds.Where(id => id > 0));
            var chatDeletionFlow = _chatId != Guid.Empty;

            if (chatDeletionFlow)
            {
                await foreach (var fileId in chatDao.GetAttachmentFileIdsAsync(_tenantId, _chatId))
                {
                    fileIds.Add(fileId);
                }
            }

            foreach (var fileId in fileIds)
            {
                try
                {
                    await fileDao.DeleteFileAsync(fileId);
                }
                catch (Exception ex)
                {
                    logger.WarningWithException(ex);
                }
            }

            if (chatDeletionFlow)
            {
                await chatDao.HardDeleteChatAsync(_tenantId, _chatId, _userId);
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;
        }
        finally
        {
            IsCompleted = true;
            Percentage = 100;

            try
            {
                await PublishChanges();
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }
}
