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
        var options = scope.ServiceProvider.GetService<ILoggerProvider>();
        var daoFactory = scope.ServiceProvider.GetService<IDaoFactory>();
        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        var userManager = scope.ServiceProvider.GetService<UserManager>();

        await tenantManager.SetCurrentTenantAsync(_tenantId);

        var folderDao = daoFactory.GetFolderDao<int>();
        var logger = options.CreateLogger("ASC.Web");

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