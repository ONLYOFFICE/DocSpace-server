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

namespace ASC.Data.Reassigns;

[Transient]
public class DeletePersonalFolderProgressItem : DistributedTaskProgress
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private Guid _userId;
    private int _tenantId;

    public DeletePersonalFolderProgressItem()
    {
    }

    public DeletePersonalFolderProgressItem(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(Guid userId, int tenantId)
    {
        Id = QueueDeletePersonalFolder.GetProgressItemId(tenantId, userId);
        _userId = userId;
        _tenantId = tenantId;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var fileStorageService = scope.ServiceProvider.GetService<FileStorageService>();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        var userManager = scope.ServiceProvider.GetService<UserManager>();

        await tenantManager.SetCurrentTenantAsync(_tenantId);

        var folderDao = daoFactory.GetFolderDao<int>();
        var logger = loggerFactory.CreateLogger("ASC.Web");

        try
        {
            Percentage = 10;
            await PublishChanges();

            var myId = await folderDao.GetFolderIDUserAsync(false, _userId);
            if (myId == 0)
            {
                Percentage = 100;
                return;
            }
            var my = await folderDao.GetFolderAsync(myId);

            Percentage = 20;
            await PublishChanges();

            var userTo = my.ModifiedBy;

            if (await userManager.IsGuestAsync(userTo))
            {
                userTo = tenantManager.GetCurrentTenant().OwnerId;
            }

            await fileStorageService.MoveSharedEntriesAsync(_userId, userTo);

            Percentage = 50;
            await PublishChanges();

            await fileStorageService.DeletePersonalFolderAsync(_userId);

            Percentage = 100;
        }
        catch (Exception ex)
        {
            logger.ErrorDeletePersonalFolderProgressItem(ex);
            Status = DistributedTaskStatus.Failted;
            Exception = ex;
        }
        finally
        {
            logger.LogInformation($"Delete personal folder: {Status.ToString().ToLowerInvariant()}");
            IsCompleted = true;
            await PublishChanges();
        }
    }
}