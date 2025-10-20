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

using ASC.Api.Core.Webhook;
using ASC.Core.Common;
using ASC.Files.Core.Resources;
using ASC.People.ApiModels.ResponseDto;
using ASC.Webhooks.Core;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Data.Reassigns;

[Transient]
public class UpdateUserTypeProgressItem : DistributedTaskProgress
{
    public Guid User { get; private set; }
    public Guid ToUser { get; private set; }
    private int _tenantId;
    private Guid _currentUserId;
    private EmployeeType _employeeType;
    private IDictionary<string, StringValues> _httpHeaders;
    private UserInfo _userInfo;
    private readonly IServiceScopeFactory _serviceScopeFactory;


    public UpdateUserTypeProgressItem()
    {

    }

    public UpdateUserTypeProgressItem(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(int tenantId, Guid user, Guid toUserId, Guid currentUserId, EmployeeType employeeType, IDictionary<string, StringValues> httpHeaders)
    {
        _tenantId = tenantId;
        User = user;
        ToUser = toUserId;
        _currentUserId = currentUserId;
        _employeeType = employeeType;
        _httpHeaders = httpHeaders;
        Id = QueueWorkerUpdateUserType.GetProgressItemId(tenantId, user);
        Status = DistributedTaskStatus.Created;
        Exception = null;
        Percentage = 0;
        IsCompleted = false;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var scopeClass = scope.ServiceProvider.GetService<ChangeUserTypeProgressItemScope>();
        var (tenantManager, messageService, fileStorageService, studioNotifyService, securityContext, userManager, displayUserSettingsHelper, options, webItemSecurityCache, distributedLockProvider, socketManager, webhookManager, userFormatter, daoFactory, groupFullDtoHelper) = scopeClass;
        var logger = options.CreateLogger("ASC.Web");
        await tenantManager.SetCurrentTenantAsync(_tenantId);
        _userInfo = await userManager.GetUsersAsync(User);

        try
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(_currentUserId);
            await SetPercentageAndCheckCancellationAsync(5, true);

            await fileStorageService.DemandPermissionToReassignDataAsync(User, ToUser);
            await SetPercentageAndCheckCancellationAsync(10, true);

            await fileStorageService.ReassignRoomsAsync(User, ToUser);
            await SetPercentageAndCheckCancellationAsync(40, true);

            if (_employeeType == EmployeeType.Guest)
            {
                await securityContext.AuthenticateMeWithoutCookieAsync(ToUser);
                await fileStorageService.UpdatePersonalFolderModified(User);
                await securityContext.AuthenticateMeWithoutCookieAsync(_currentUserId);

                await SetPercentageAndCheckCancellationAsync(60, true);
            }

            await UpdateUserTypeAsync(userManager, webItemSecurityCache, distributedLockProvider, socketManager, daoFactory, groupFullDtoHelper);

            await SetPercentageAndCheckCancellationAsync(100, false);

            Status = DistributedTaskStatus.Completed;
            await SendSuccessNotifyAsync(userManager, studioNotifyService, messageService, webhookManager, displayUserSettingsHelper);
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
            await SendErrorNotifyAsync(userManager, studioNotifyService, ex.Message);
        }
        finally
        {
            logger.LogInformation($"update user type: {Status.ToString().ToLowerInvariant()}");
            IsCompleted = true;
            await PublishChanges();
        }
    }

    private async Task UpdateUserTypeAsync(UserManager userManager, WebItemSecurityCache webItemSecurityCache, IDistributedLockProvider distributedLockProvider, UserSocketManager socketManager, IDaoFactory daoFactory, GroupFullDtoHelper groupFullDtoHelper)
    {
        var currentType = await userManager.GetUserTypeAsync(_userInfo);
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
                    await socketManager.AddUserAsync(_userInfo);
                }
                else if (currentType is EmployeeType.RoomAdmin)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupRoomAdmin.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupUser.ID);
                    await socketManager.UpdateUserAsync(_userInfo);
                }
                else if (currentType is EmployeeType.DocSpaceAdmin)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupAdmin.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupUser.ID);
                    await socketManager.UpdateUserAsync(_userInfo);
                }
                if (currentType != _employeeType)
                {
                    await webItemSecurityCache.ClearCacheAsync(_tenantId);
                    await socketManager.ChangeUserTypeAsync(_userInfo, true);
                }
            }
            else if (_employeeType is EmployeeType.Guest)
            {
                if (currentType is EmployeeType.User)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupUser.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupGuest.ID);
                    await socketManager.DeleteUserAsync(User);
                    await socketManager.AddGuestAsync(_userInfo);
                }
                else if (currentType is EmployeeType.RoomAdmin)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupRoomAdmin.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupGuest.ID);
                    await socketManager.DeleteUserAsync(User);
                    await socketManager.AddGuestAsync(_userInfo);
                }
                else if (currentType is EmployeeType.DocSpaceAdmin)
                {
                    await userManager.RemoveUserFromGroupAsync(User, Constants.GroupAdmin.ID);
                    await userManager.AddUserIntoGroupAsync(User, Constants.GroupGuest.ID);
                    await socketManager.DeleteUserAsync(User);
                    await socketManager.AddGuestAsync(_userInfo);
                }

                if (currentType != _employeeType)
                {
                    await webItemSecurityCache.ClearCacheAsync(_tenantId);

                    var groups = await userManager.GetUserGroupsAsync(User);

                    foreach (var group in groups)
                    {
                        await userManager.RemoveUserFromGroupAsync(User, group.ID);

                        var groupInfo = await userManager.GetGroupInfoAsync(group.ID);
                        var groupDto = await groupFullDtoHelper.Get(groupInfo, true);
                        await socketManager.UpdateGroupAsync(groupDto);
                    }

                    var folderDao = daoFactory.GetFolderDao<int>();
                    var myId = await folderDao.GetFolderIDUserAsync(false, User);
                    var hasPersonalFolder = myId != 0;
                    await socketManager.ChangeUserTypeAsync(_userInfo, hasPersonalFolder);
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

    private async Task SendSuccessNotifyAsync(UserManager userManager, StudioNotifyService studioNotifyService, MessageService messageService, UserWebhookManager webhookManager, DisplayUserSettingsHelper displayUserSettingsHelper)
    {
        var toUser = await userManager.GetUsersAsync(ToUser);

        await studioNotifyService.SendMsgReassignsCompletedAsync(_currentUserId, _userInfo, toUser);
        await studioNotifyService.SendMsgUserTypeChangedAsync(_userInfo, FilesCommonResource.ResourceManager.GetString("RoleEnum_" + _employeeType.ToStringFast()));

        var fromUserName = _userInfo.DisplayUserName(false, displayUserSettingsHelper);

        messageService.SendHeadersMessage(MessageAction.UsersUpdatedType, MessageTarget.Create([User]), _httpHeaders, [fromUserName], [_userInfo.Id], _employeeType);

        await webhookManager.PublishAsync(WebhookTrigger.UserUpdated, _userInfo);
    }

    private async Task SendErrorNotifyAsync(UserManager userManager, StudioNotifyService studioNotifyService, string errorMessage)
    {
        var toUser = await userManager.GetUsersAsync(ToUser);

        await studioNotifyService.SendMsgReassignsFailedAsync(_currentUserId, _userInfo, toUser, errorMessage);
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
    DisplayUserSettingsHelper DisplayUserSettingsHelper,
    ILoggerProvider Options,
    WebItemSecurityCache WebItemSecurityCache,
    IDistributedLockProvider DistributedLockProvider,
    UserSocketManager SocketManager,
    UserWebhookManager WebhookManager,
    UserFormatter UserFormatter,
    IDaoFactory DaoFactory,
    GroupFullDtoHelper groupFullDtoHelper);