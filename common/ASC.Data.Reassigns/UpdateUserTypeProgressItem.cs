// (c) Copyright Ascensio System SIA 2009-2024
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

using ASC.Core.Common;

namespace ASC.Data.Reassigns;

[Transient]
public class UpdateUserTypeProgressItem(IServiceScopeFactory serviceScopeFactory) : DistributedTaskProgress
{
    public Guid User { get; private set; }
    public Guid ToUser { get; private set; }
    private int _tenantId;
    private Guid _currentUserId;
    private EmployeeType _employeeType;

    public void Init(int tenantId, Guid user, Guid toUserId, Guid currentUserId, EmployeeType employeeType)
    {
        _tenantId = tenantId;
        User = user;
        ToUser = toUserId;
        _currentUserId = currentUserId;
        _employeeType = employeeType;
        Id = QueueWorkerUpdateUserType.GetProgressItemId(tenantId, user);
        Status = DistributedTaskStatus.Created;
        Exception = null;
        Percentage = 0;
        IsCompleted = false;
    }

    protected override async Task DoJob()
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var scopeClass = scope.ServiceProvider.GetService<ChangeUserTypeProgressItemScope>();
        var (tenantManager, messageService, fileStorageService, studioNotifyService, securityContext, userManager, userPhotoManager, displayUserSettingsHelper, options, webItemSecurityCache, distributedLockProvider, socketManager) = scopeClass;
        var logger = options.CreateLogger("ASC.Web");
        await tenantManager.SetCurrentTenantAsync(_tenantId);

        try
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(_currentUserId);

            await SetPercentageAndCheckCancellationAsync(5, true);

            await fileStorageService.DemandPermissionToReassignDataAsync(User, ToUser);

            await SetPercentageAndCheckCancellationAsync(10, true);

            if (_employeeType == EmployeeType.Guest)
            {
                await fileStorageService.ClearPersonalFolderAsync<int>(User);
            }

            await SetPercentageAndCheckCancellationAsync(40, true);

            await fileStorageService.ReassignRoomsAsync(User, ToUser);

            await SetPercentageAndCheckCancellationAsync(80, true);

            await UpdateUserTypeAsync(userManager, webItemSecurityCache, distributedLockProvider, socketManager);
            messageService.Send(MessageAction.UsersUpdatedType, MessageTarget.Create([User]));

            await SetPercentageAndCheckCancellationAsync(100, false);

            Status = DistributedTaskStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            Status = DistributedTaskStatus.Canceled;
            throw;
        }
        catch (Exception ex)
        {
            logger.ErrorUpdateTypeProgressItem(ex);
            Status = DistributedTaskStatus.Failted;
            Exception = ex;
        }
        finally
        {
            logger.LogInformation($"update user type: {Status.ToString().ToLowerInvariant()}");
            IsCompleted = true;
            await PublishChanges();
        }
    }

    private async Task UpdateUserTypeAsync(UserManager userManager, WebItemSecurityCache webItemSecurityCache, IDistributedLockProvider distributedLockProvider, UserSocketManager socketManager)
    {
        var userInfo = await userManager.GetUsersAsync(User);
        var currentType = await userManager.GetUserTypeAsync(userInfo);
        var lockHandle = await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(_tenantId));
        try
        {
            if (_employeeType is EmployeeType.User)
            {
                if (currentType is EmployeeType.Guest)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupGuest.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupUser.ID);
                    await socketManager.DeleteGuestAsync(User);
                    await socketManager.AddUserAsync(userInfo);
                }
                else if (currentType is EmployeeType.RoomAdmin)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupRoomAdmin.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupUser.ID);
                    await socketManager.UpdateUserAsync(userInfo);
                }
                else if (currentType is EmployeeType.DocSpaceAdmin)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupAdmin.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupUser.ID);
                    await socketManager.UpdateUserAsync(userInfo);
                }
                if (currentType != _employeeType)
                {
                    webItemSecurityCache.ClearCache(_tenantId);
                    await socketManager.ChangeUserTypeAsync(userInfo);
                }
            }
            else if (_employeeType is EmployeeType.Guest)
            {
                if (currentType is EmployeeType.User)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupUser.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupGuest.ID);
                    await socketManager.DeleteUserAsync(User);
                    await socketManager.AddGuestAsync(userInfo);
                }
                else if (currentType is EmployeeType.RoomAdmin)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupRoomAdmin.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupGuest.ID);
                    await socketManager.DeleteUserAsync(User);
                    await socketManager.AddGuestAsync(userInfo);
                }
                else if (currentType is EmployeeType.DocSpaceAdmin)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupAdmin.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupGuest.ID);
                    await socketManager.DeleteUserAsync(User);
                    await socketManager.AddGuestAsync(userInfo);
                }

                if (currentType != _employeeType)
                {
                    webItemSecurityCache.ClearCache(_tenantId);
                    await socketManager.ChangeUserTypeAsync(userInfo);
                }
            }
        }
        finally
        {
            if (lockHandle != null)
            {
                await lockHandle.ReleaseAsync();
            }
        }
    }

    private async Task SetPercentageAndCheckCancellationAsync(double percentage, bool publish)
    {
        Percentage = percentage;

        if (publish)
        {
            await PublishChanges();
        }

        CancellationToken.ThrowIfCancellationRequested();
    }
}

[Scope]
public record ChangeUserTypeProgressItemScope(
    TenantManager TenantManager,
    MessageService MessageService,
    FileStorageService FileStorageService,
    StudioNotifyService StudioNotifyService,
    SecurityContext SecurityContext,
    UserManager UserManager,
    UserPhotoManager UserPhotoManager,
    DisplayUserSettingsHelper DisplayUserSettingsHelper,
    ILoggerProvider Options,
    WebItemSecurityCache WebItemSecurityCache,
    IDistributedLockProvider DistributedLockProvider,
    UserSocketManager SocketManager);
