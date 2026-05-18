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

using ASC.Core.Common.Identity;
using ASC.Web.Core.WebZones;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Data.Reassigns;

/// <summary>
/// </summary>
[Transient]
public class RemoveProgressItem : DistributedTaskProgress
{
    /// <summary>ID of the user whose data is deleted</summary>
    /// <type>System.Guid, System</type>
    public Guid UserId { get; private set; }

    /// <summary>The user whose data is deleted</summary>
    /// <type>ASC.Core.Users.UserInfo, ASC.Core.Common</type>
    public UserInfo User { get; private set; }

    public bool IsGuest { get; private set; }

    //private readonly IFileStorageService _docService;
    //private readonly MailGarbageEngine _mailEraser;

    private IDictionary<string, StringValues> _httpHeaders;
    private int _tenantId;
    private Guid _currentUserId;
    private bool _notify;
    private bool _deleteProfile;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RemoveProgressItem()
    {

    }

    /// <summary>
    /// </summary>
    public RemoveProgressItem(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    //_docService = Web.Files.Classes.Global.FileStorageService;
    //_mailEraser = new MailGarbageEngine();

    public void Init(IDictionary<string, StringValues> httpHeaders, int tenantId, UserInfo user, Guid currentUserId, bool notify, bool deleteProfile, bool isGuest)
    {
        _httpHeaders = httpHeaders;
        _tenantId = tenantId;
        User = user;
        UserId = user.Id;
        _currentUserId = currentUserId;
        _notify = notify;
        _deleteProfile = deleteProfile;
        Id = QueueWorkerRemove.GetProgressItemId(tenantId, UserId);
        Status = DistributedTaskStatus.Created;
        Exception = null;
        Percentage = 0;
        IsCompleted = false;
        IsGuest = isGuest;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var scopeClass = scope.ServiceProvider.GetService<RemoveProgressItemScope>();
        var (tenantManager, messageService, fileStorageService, studioNotifyService, securityContext, userManager, userPhotoManager, webItemManagerSecurity, userFormatter, options, client) = scopeClass;
        var logger = options.CreateLogger("ASC.Web");
        await tenantManager.SetCurrentTenantAsync(_tenantId);
        var userName = userFormatter.GetUserName(User);

        try
        {
            Percentage = 0;
            Status = DistributedTaskStatus.Running;

            await securityContext.AuthenticateMeWithoutCookieAsync(_currentUserId);

            Percentage = 5;
            await PublishChanges();

            await fileStorageService.DemandPermissionToDeletePersonalDataAsync(User);

            Percentage = 10;
            await PublishChanges();

            var wrapper = await GetUsageSpace(webItemManagerSecurity);

            await fileStorageService.MoveSharedEntriesAsync(UserId, _currentUserId);

            Percentage = 30;
            await PublishChanges();

            await fileStorageService.DeletePersonalDataAsync(UserId);

            Percentage = 45;
            await PublishChanges();

            await fileStorageService.ReassignProvidersAsync(UserId, _currentUserId);

            Percentage = 50;
            await PublishChanges();

            await fileStorageService.ReassignRoomsAsync(UserId, _currentUserId);

            Percentage = 60;
            await PublishChanges();

            await fileStorageService.ReassignRoomsFoldersAsync(UserId);

            Percentage = 70;
            await PublishChanges();

            await fileStorageService.ReassignRoomsFilesAsync(UserId);

            Percentage = 95;
            await PublishChanges();

            //_mailEraser.ClearUserMail(_userId);
            //await DeleteTalkStorage(storageFactory);

            if (_deleteProfile)
            {
                await client.DeleteClientsAsync(UserId);
                await DeleteUserProfile(userManager, userPhotoManager, messageService, userName);
            }

            await SendSuccessNotifyAsync(studioNotifyService, messageService, userName, wrapper);

            Percentage = 100;
            Status = DistributedTaskStatus.Completed;
        }
        catch (Exception ex)
        {
            logger.ErrorRemoveProgressItem(ex);
            Status = DistributedTaskStatus.Failted;
            Exception = ex;
            await SendErrorNotifyAsync(studioNotifyService, ex.Message, userName);
        }
        finally
        {
            logger.LogInformation("data deletion is complete");
            IsCompleted = true;
            await PublishChanges();
        }
    }

    public object Clone()
    {
        return MemberwiseClone();
    }

    private async Task<UsageSpaceWrapper> GetUsageSpace(WebItemManagerSecurity webItemManagerSecurity)
    {
        var usageSpaceWrapper = new UsageSpaceWrapper();

        var webItems = await webItemManagerSecurity.GetItemsAsync(WebZoneType.All, ItemAvailableState.All);

        foreach (var item in webItems)
        {
            IUserSpaceUsage manager;

            if (item.ID == WebItemManager.DocumentsProductID)
            {
                manager = item.Context.SpaceUsageStatManager as IUserSpaceUsage;
                if (manager == null)
                {
                    continue;
                }

                usageSpaceWrapper.DocsSpace = await manager.GetUserSpaceUsageAsync(UserId);
            }

            if (item.ID == WebItemManager.MailProductID)
            {
                manager = item.Context.SpaceUsageStatManager as IUserSpaceUsage;
                if (manager == null)
                {
                    continue;
                }

                usageSpaceWrapper.MailSpace = await manager.GetUserSpaceUsageAsync(UserId);
            }

            if (item.ID == WebItemManager.TalkProductID)
            {
                manager = item.Context.SpaceUsageStatManager as IUserSpaceUsage;
                if (manager == null)
                {
                    continue;
                }

                usageSpaceWrapper.TalkSpace = await manager.GetUserSpaceUsageAsync(UserId);
            }
        }
        return usageSpaceWrapper;
    }

    private async Task DeleteUserProfile(UserManager userManager, UserPhotoManager userPhotoManager, MessageService messageService, string userName)
    {
        await userPhotoManager.RemovePhotoAsync(UserId);
        await userManager.DeleteUserAsync(UserId);

        if (_httpHeaders != null)
        {
            messageService.SendHeadersMessage(MessageAction.UserDeleted, MessageTarget.Create(UserId), _httpHeaders, userName);
        }
        else
        {
            messageService.Send(MessageAction.UserDeleted, MessageTarget.Create(UserId), userName);
        }
    }

    private async Task SendSuccessNotifyAsync(StudioNotifyService studioNotifyService, MessageService messageService, string userName, UsageSpaceWrapper wrapper)
    {
        if (_notify)
        {
            await studioNotifyService.SendMsgRemoveUserDataCompletedAsync(_currentUserId, User, userName, wrapper.DocsSpace, 0, 0, 0);
        }

        if (_httpHeaders != null)
        {
            messageService.SendHeadersMessage(MessageAction.UserDataRemoving, MessageTarget.Create(UserId), _httpHeaders, userName);
        }
        else
        {
            messageService.Send(MessageAction.UserDataRemoving, MessageTarget.Create(UserId), userName);
        }
    }

    private async Task SendErrorNotifyAsync(StudioNotifyService studioNotifyService, string errorMessage, string userName)
    {
        if (!_notify)
        {
            return;
        }

        await studioNotifyService.SendMsgRemoveUserDataFailedAsync(_currentUserId, User, userName, errorMessage);
    }
}

[Scope]
public record RemoveProgressItemScope(
    TenantManager TenantManager,
    MessageService MessageService,
    FileStorageService FileStorageService,
    StudioNotifyService StudioNotifyService,
    SecurityContext SecurityContext,
    UserManager UserManager,
    UserPhotoManager UserPhotoManager,
    WebItemManagerSecurity WebItemManagerSecurity,
    UserFormatter UserFormatter,
    ILoggerProvider Options,
    IdentityClient Client);

internal class UsageSpaceWrapper
{
    public long DocsSpace { get; set; }
    public long MailSpace { get; set; }
    public long TalkSpace { get; set; }
}
