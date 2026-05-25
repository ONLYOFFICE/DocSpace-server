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

using ASC.Api.Core.Webhook;
using ASC.Core.Common.Identity;
using ASC.People.ApiModels.ResponseDto;
using ASC.Webhooks.Core;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Data.Reassigns;

/// <summary>
/// </summary>
[Transient]
public class ReassignProgressItem : DistributedTaskProgress
{
    /// <summary>The user whose data is reassigned</summary>
    /// <type>System.Guid, System</type>
    public Guid FromUser { get; private set; }

    /// <summary>The user to whom this data is reassigned</summary>
    /// <type>System.Guid, System</type>
    public Guid ToUser { get; private set; }

    private IDictionary<string, StringValues> _httpHeaders;
    private int _tenantId;
    private Guid _currentUserId;
    private bool _notify;
    private bool _deleteProfile;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ReassignProgressItem()
    {

    }

    /// <summary>
    /// </summary>
    public ReassignProgressItem(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(IDictionary<string, StringValues> httpHeaders, int tenantId, Guid fromUserId, Guid toUserId, Guid currentUserId, bool notify, bool deleteProfile)
    {
        _httpHeaders = httpHeaders;
        _tenantId = tenantId;
        FromUser = fromUserId;
        ToUser = toUserId;
        _currentUserId = currentUserId;
        _notify = notify;
        _deleteProfile = deleteProfile;
        Id = QueueWorkerReassign.GetProgressItemId(tenantId, fromUserId);
        Status = DistributedTaskStatus.Created;
        Exception = null;
        Percentage = 0;
        IsCompleted = false;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var scopeClass = scope.ServiceProvider.GetService<ReassignProgressItemScope>();
        var (tenantManager, messageService, fileStorageService, studioNotifyService, securityContext, userManager, userPhotoManager, displayUserSettingsHelper, options, socketManager, webhookManager, client, groupFullDtoHelper) = scopeClass;
        var logger = options.CreateLogger("ASC.Web");
        await tenantManager.SetCurrentTenantAsync(_tenantId);

        try
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(_currentUserId);

            await SetPercentageAndCheckCancellationAsync(5, true);

            await fileStorageService.DemandPermissionToReassignDataAsync(FromUser, ToUser);

            await SetPercentageAndCheckCancellationAsync(10, true);

            List<int> personalFolderIds = null;

            await fileStorageService.MoveSharedEntriesAsync(FromUser, ToUser);

            await SetPercentageAndCheckCancellationAsync(20, true);

            if (_deleteProfile)
            {
                await fileStorageService.DeletePersonalDataAsync(FromUser);
            }
            else
            {
                personalFolderIds = await fileStorageService.GetPersonalFolderIdsAsync<int>(FromUser);
            }

            await SetPercentageAndCheckCancellationAsync(30, true);

            await fileStorageService.ReassignProvidersAsync(FromUser, ToUser);

            await SetPercentageAndCheckCancellationAsync(50, true);

            await fileStorageService.ReassignFoldersAsync(FromUser, ToUser, personalFolderIds);

            await SetPercentageAndCheckCancellationAsync(70, true);

            await fileStorageService.ReassignFilesAsync(FromUser, ToUser, personalFolderIds);

            await SetPercentageAndCheckCancellationAsync(90, true);

            await SendSuccessNotifyAsync(userManager, studioNotifyService, messageService, displayUserSettingsHelper);

            await SetPercentageAndCheckCancellationAsync(95, true);

            if (_deleteProfile)
            {
                await client.DeleteClientsAsync(FromUser);
                await DeleteUserProfile(userManager, userPhotoManager, messageService, displayUserSettingsHelper, socketManager, webhookManager, groupFullDtoHelper);
            }

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
            logger.ErrorReassignProgressItem(ex);
            Status = DistributedTaskStatus.Failted;
            Exception = ex;
            await SendErrorNotifyAsync(userManager, studioNotifyService, ex.Message);
        }
        finally
        {
            logger.LogInformation($"data reassignment {Status.ToString().ToLowerInvariant()}");
            IsCompleted = true;
            await PublishChanges();
        }
    }

    public object Clone()
    {
        return MemberwiseClone();
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

    private async Task SendSuccessNotifyAsync(UserManager userManager, StudioNotifyService studioNotifyService, MessageService messageService, DisplayUserSettingsHelper displayUserSettingsHelper)
    {
        var fromUser = await userManager.GetUsersAsync(FromUser);
        var toUser = await userManager.GetUsersAsync(ToUser);

        if (_notify)
        {
            await studioNotifyService.SendMsgReassignsCompletedAsync(_currentUserId, fromUser, toUser);
        }

        var fromUserName = fromUser.DisplayUserName(false, displayUserSettingsHelper);
        var toUserName = toUser.DisplayUserName(false, displayUserSettingsHelper);

        if (_httpHeaders != null)
        {
            messageService.SendHeadersMessage(MessageAction.UserDataReassigns, MessageTarget.Create(FromUser), _httpHeaders, [fromUserName, toUserName]);
        }
        else
        {
            messageService.Send(MessageAction.UserDataReassigns, MessageTarget.Create(FromUser), fromUserName, toUserName);
        }
    }

    private async Task SendErrorNotifyAsync(UserManager userManager, StudioNotifyService studioNotifyService, string errorMessage)
    {
        var fromUser = await userManager.GetUsersAsync(FromUser);
        var toUser = await userManager.GetUsersAsync(ToUser);

        await studioNotifyService.SendMsgReassignsFailedAsync(_currentUserId, fromUser, toUser, errorMessage);
    }

    private async Task DeleteUserProfile(UserManager userManager, UserPhotoManager userPhotoManager, MessageService messageService, DisplayUserSettingsHelper displayUserSettingsHelper, UserSocketManager socketManager, UserWebhookManager webhookManager, GroupFullDtoHelper groupFullDtoHelper)
    {
        var user = await userManager.GetUsersAsync(FromUser);
        var isGuest = await userManager.IsGuestAsync(FromUser);
        var userName = user.DisplayUserName(false, displayUserSettingsHelper);
        var groups = await userManager.GetUserGroupsAsync(user.Id);

        await userPhotoManager.RemovePhotoAsync(user.Id);
        await userManager.DeleteUserAsync(user.Id);

        if (isGuest)
        {
            await socketManager.DeleteGuestAsync(user.Id);
        }
        else
        {
            await socketManager.DeleteUserAsync(user.Id);
            foreach (var group in groups)
            {
                var groupInfo = await userManager.GetGroupInfoAsync(group.ID);
                var groupDto = await groupFullDtoHelper.Get(groupInfo, true);
                await socketManager.UpdateGroupAsync(groupDto);
            }
        }
        if (_httpHeaders != null)
        {
            messageService.SendHeadersMessage(MessageAction.UserDeleted, MessageTarget.Create(FromUser), _httpHeaders, userName);
        }
        else
        {
            messageService.Send(MessageAction.UserDeleted, MessageTarget.Create(FromUser), userName);
        }

        await webhookManager.PublishAsync(WebhookTrigger.UserDeleted, user);
    }
}

[Scope]
public record ReassignProgressItemScope(
    TenantManager TenantManager,
    MessageService MessageService,
    FileStorageService FileStorageService,
    StudioNotifyService StudioNotifyService,
    SecurityContext SecurityContext,
    UserManager UserManager,
    UserPhotoManager UserPhotoManager,
    DisplayUserSettingsHelper DisplayUserSettingsHelper,
    ILoggerProvider Options,
    UserSocketManager SocketManager,
    UserWebhookManager WebhookManager,
    IdentityClient Client,
    GroupFullDtoHelper groupFullDtoHelper);