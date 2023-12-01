// (c) Copyright Ascensio System SIA 2010-2023
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

using ASC.Web.Core.WebZones;

namespace ASC.Data.Reassigns;

/// <summary>
/// </summary>
[Transient]
public class RemoveProgressItem(IServiceScopeFactory serviceScopeFactory) : DistributedTaskProgress
{
    /// <summary>ID of the user whose data is deleted</summary>
    /// <type>System.Guid, System</type>
    public Guid FromUser { get; private set; }

    /// <summary>The user whose data is deleted</summary>
    /// <type>ASC.Core.Users.UserInfo, ASC.Core.Common</type>
    public UserInfo User { get; private set; }

    //private readonly IFileStorageService _docService;
    //private readonly MailGarbageEngine _mailEraser;

    private IDictionary<string, StringValues> _httpHeaders;
    private int _tenantId;
    private Guid _currentUserId;
    private bool _notify;
    private bool _deleteProfile;

    //_docService = Web.Files.Classes.Global.FileStorageService;
    //_mailEraser = new MailGarbageEngine();

    public void Init(IDictionary<string, StringValues> httpHeaders, int tenantId, UserInfo user, Guid currentUserId, bool notify, bool deleteProfile)
    {
        _httpHeaders = httpHeaders;
        _tenantId = tenantId;
        User = user;
        FromUser = user.Id;
        _currentUserId = currentUserId;
        _notify = notify;
        _deleteProfile = deleteProfile;
        Id = QueueWorkerRemove.GetProgressItemId(tenantId, FromUser);
        Status = DistributedTaskStatus.Created;
        Exception = null;
        Percentage = 0;
        IsCompleted = false;
    }

    protected override async Task DoJob()
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var scopeClass = scope.ServiceProvider.GetService<RemoveProgressItemScope>();
        var (tenantManager, messageService, fileStorageService, studioNotifyService, securityContext, userManager, userPhotoManager, messageTarget, webItemManagerSecurity,  userFormatter, options) = scopeClass;
        var logger = options.CreateLogger("ASC.Web");
        await tenantManager.SetCurrentTenantAsync(_tenantId);
        var userName = userFormatter.GetUserName(User);

        try
        {
            Percentage = 0;
            Status = DistributedTaskStatus.Running;

            await securityContext.AuthenticateMeWithoutCookieAsync(_currentUserId);

            Percentage = 5;
            PublishChanges();

            await fileStorageService.DemandPermissionToDeletePersonalDataAsync(User);

            Percentage = 10;
            PublishChanges();

            var wrapper = await GetUsageSpace(webItemManagerSecurity);

            Percentage = 30;
            PublishChanges();

            await fileStorageService.DeletePersonalDataAsync<int>(FromUser);

            Percentage = 95;
            PublishChanges();

            //_mailEraser.ClearUserMail(_userId);
            //await DeleteTalkStorage(storageFactory);

            if (_deleteProfile)
            {
                await DeleteUserProfile(userManager, userPhotoManager, messageService, messageTarget, userName);
            }

            await SendSuccessNotifyAsync(studioNotifyService, messageService, messageTarget, userName, wrapper);

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
            PublishChanges();
        }
    }

    public object Clone()
    {
        return MemberwiseClone();
    }

    private async Task<UsageSpaceWrapper> GetUsageSpace(WebItemManagerSecurity webItemManagerSecurity)
    {
        var usageSpaceWrapper = new UsageSpaceWrapper();

        var webItems = webItemManagerSecurity.GetItems(WebZoneType.All, ItemAvailableState.All);

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

                usageSpaceWrapper.DocsSpace = await manager.GetUserSpaceUsageAsync(FromUser);
            }

            if (item.ID == WebItemManager.MailProductID)
            {
                manager = item.Context.SpaceUsageStatManager as IUserSpaceUsage;
                if (manager == null)
                {
                    continue;
                }

                usageSpaceWrapper.MailSpace = await manager.GetUserSpaceUsageAsync(FromUser);
            }

            if (item.ID == WebItemManager.TalkProductID)
            {
                manager = item.Context.SpaceUsageStatManager as IUserSpaceUsage;
                if (manager == null)
                {
                    continue;
                }

                usageSpaceWrapper.TalkSpace = await manager.GetUserSpaceUsageAsync(FromUser);
            }
        }
        return usageSpaceWrapper;
    }

    private async Task DeleteUserProfile(UserManager userManager, UserPhotoManager userPhotoManager, MessageService messageService, MessageTarget messageTarget, string userName)
    {
        await userPhotoManager.RemovePhotoAsync(FromUser);
        await userManager.DeleteUserAsync(FromUser);

        if (_httpHeaders != null)
        {
            await messageService.SendHeadersMessageAsync(MessageAction.UserDeleted, messageTarget.Create(FromUser), _httpHeaders, userName);
        }
        else
        {
            await messageService.SendAsync(MessageAction.UserDeleted, messageTarget.Create(FromUser), userName);
        }
    }

    private async Task SendSuccessNotifyAsync(StudioNotifyService studioNotifyService, MessageService messageService, MessageTarget messageTarget, string userName, UsageSpaceWrapper wrapper)
    {
        if (_notify)
        {
            await studioNotifyService.SendMsgRemoveUserDataCompletedAsync(_currentUserId, User, userName, wrapper.DocsSpace, 0, 0, 0);
        }

        if (_httpHeaders != null)
        {
            await messageService.SendHeadersMessageAsync(MessageAction.UserDataRemoving, messageTarget.Create(FromUser), _httpHeaders, userName);
        }
        else
        {
            await messageService.SendAsync(MessageAction.UserDataRemoving, messageTarget.Create(FromUser), userName);
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
    MessageTarget MessageTarget,
    WebItemManagerSecurity WebItemManagerSecurity,
    UserFormatter UserFormatter,
    ILoggerProvider Options);

class UsageSpaceWrapper
{
    public long DocsSpace { get; set; }
    public long MailSpace { get; set; }
    public long TalkSpace { get; set; }
}

public static class RemoveProgressItemExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<RemoveProgressItemScope>();
    }
}
