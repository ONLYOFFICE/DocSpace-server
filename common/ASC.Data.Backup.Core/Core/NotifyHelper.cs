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

using ASC.Core.Common.WhiteLabel;
using ASC.Notify.Recipients;
using ASC.Web.Core.Utility;

namespace ASC.Data.Backup;

[Scope]
public class NotifyHelper(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    StudioNotifySource studioNotifySource,
    TenantManager tenantManager,
    AuthManager authManager,
    WorkContext workContext,
    CommonLinkUtility commonLinkUtility,
    TenantLogoManager tenantLogoManager,
    IServiceProvider serviceProvider)
{
    public void SetServerBaseUri(string uri)
    {
        if (!string.IsNullOrEmpty(uri))
        {
            commonLinkUtility.ServerUri = uri;
        }
    }

    public async Task SendAboutTransferStartAsync(Tenant tenant, string targetRegion, bool notifyUsers)
    {
        await MigrationNotifyAsync(tenant, actions.MigrationPortalStart, targetRegion, string.Empty, notifyUsers);
    }

    public async Task SendAboutTransferCompleteAsync(Tenant tenant, string targetRegion, string targetAddress, bool notifyOnlyOwner, int toTenantId)
    {
        await MigrationNotifyAsync(tenant, actions.MigrationPortalSuccessV115, targetRegion, targetAddress, !notifyOnlyOwner, toTenantId);
    }

    public async Task SendAboutTransferErrorAsync(Tenant tenant, string targetRegion, string resultAddress, bool notifyOnlyOwner)
    {
        await MigrationNotifyAsync(tenant, !string.IsNullOrEmpty(targetRegion) ? actions.MigrationPortalError : actions.MigrationPortalServerFailure, targetRegion, resultAddress, !notifyOnlyOwner);
    }

    public async Task SendAboutBackupCompletedAsync(int tenantId, Guid userId)
    {
        await tenantManager.SetCurrentTenantAsync(tenantId);

        var user = await userManager.GetUsersAsync(userId);

        var client = workContext.RegisterClient(serviceProvider, studioNotifySource);
        var action = serviceProvider.GetService<BackupCreatedNotifyAction>();
        action.Init(user);
        
        await client.SendNoticeToAsync(action, user, StudioNotifyService.EMailSenderName);
    }

    public async Task SendAboutBackupFailedAsync(int tenantId, Guid userId, string errorMessage)
    {
        await tenantManager.SetCurrentTenantAsync(tenantId);

        var admins = userId != Guid.Empty
            ? [await userManager.GetUsersAsync(userId)]
            : await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupAdmin.ID, EmployeeStatus.Active);

        var client = workContext.RegisterClient(serviceProvider, studioNotifySource);
        var backupFailedNotifyAction = serviceProvider.GetService<BackupFailedNotifyAction>();
        
        foreach (var user in admins.Where(r => r.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated)))
        {
            backupFailedNotifyAction.Init(user, errorMessage);
            await client.SendNoticeToAsync(backupFailedNotifyAction, user, StudioNotifyService.EMailSenderName);
        }
    }

    public async Task SendAboutScheduledBackupFailedAsync(int tenantId, string errorMessage)
    {
        await tenantManager.SetCurrentTenantAsync(tenantId);

        var admins = await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupAdmin.ID, EmployeeStatus.Active);

        var client = workContext.RegisterClient(serviceProvider, studioNotifySource);

        foreach (var user in admins.Where(r => r.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated)))
        {
            var scheduledBackupFailedNotifyAction = serviceProvider.GetService<ScheduledBackupFailedNotifyAction>();
            scheduledBackupFailedNotifyAction.Init(user, errorMessage);
            
            await client.SendNoticeToAsync(scheduledBackupFailedNotifyAction, user, StudioNotifyService.EMailSenderName);
        }
    }

    public async Task SendAboutRestoreStartedAsync(Tenant tenant, bool notifyAllUsers)
    {
        tenantManager.SetCurrentTenant(tenant);

        var client = workContext.RegisterClient(serviceProvider, studioNotifySource);

        var users = notifyAllUsers
                ? await userManager.GetUsersAsync(EmployeeStatus.Active)
                : [await userManager.GetUsersAsync(tenant.OwnerId)];

        foreach (var user in users.Where(r => r.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated)))
        {
            var restoreStartedNotifyAction = serviceProvider.GetService<RestoreStartedNotifyAction>();
            restoreStartedNotifyAction.Init(user);
            
            await client.SendNoticeToAsync(restoreStartedNotifyAction, user, StudioNotifyService.EMailSenderName);
        }
    }

    public async Task SendAboutRestoreCompletedAsync(Tenant tenant, bool notifyAllUsers)
    {
        tenantManager.SetCurrentTenant(tenant);

        var client = workContext.RegisterClient(serviceProvider, studioNotifySource);

        var users = notifyAllUsers
            ? await userManager.GetUsersAsync(EmployeeStatus.Active)
            : [await userManager.GetUsersAsync(tenant.OwnerId)];

        foreach (var user in users.Where(r => r.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated)))
        {
            var restoreCompletedV115NotifyAction = serviceProvider.GetService<RestoreCompletedV115NotifyAction>();
            await restoreCompletedV115NotifyAction.InitAsync(user);

            await client.SendNoticeToAsync(restoreCompletedV115NotifyAction, user, StudioNotifyService.EMailSenderName);
        }
    }

    private async Task MigrationNotifyAsync(Tenant tenant, INotifyAction action, string region, string url, bool notify, int? toTenantId = null)
    {
        tenantManager.SetCurrentTenant(tenant);

        var client = workContext.RegisterClient(serviceProvider, studioNotifySource);

        var users = (notify
            ? await userManager.GetUsersAsync(EmployeeStatus.Active)
            : [await userManager.GetUsersAsync(tenant.OwnerId)])
            .Where(u => u.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated))
            .ToArray();

        if (users.Length != 0)
        {
            var args = CreateArgsAsync(region, url);
            if (action.Equals(actions.MigrationPortalSuccessV115))
            {
                foreach (var user in users)
                {
                    var currentArgs = new List<ITagValue>(args);

                    var newTenantId = toTenantId ?? tenant.Id;
                    var hash = (await authManager.GetUserPasswordStampAsync(user.Id)).ToString("s", CultureInfo.InvariantCulture);
                    var confirmationUrl = url + "/" + commonLinkUtility.GetConfirmationUrlRelative(newTenantId, user.Email, ConfirmType.PasswordChange, hash, user.Id);
                    var culture = user.GetCulture();

                    var orangeButtonText = BackupResource.ResourceManager.GetString("ButtonSetPassword", culture);
                    currentArgs.Add(TagValues.OrangeButton(orangeButtonText, confirmationUrl));

                    var bestReagardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);
                    currentArgs.Add(TagValues.TrulyYours(studioNotifyHelper, bestReagardsTxt));

                    var logoArgs = await CreateLogoArgsAsync(culture);
                    currentArgs.AddRange(logoArgs);

                    await client.SendNoticeToAsync(
                        action,
                        [user],
                        [StudioNotifyService.EMailSenderName],
                        currentArgs.ToArray());
                }
            }
            else
            {
                await client.SendNoticeToAsync(
                    action,
                    users.Cast<IRecipient>().ToArray(),
                    [StudioNotifyService.EMailSenderName],
                    args.ToArray());
            }
        }
    }

    private List<ITagValue> CreateArgsAsync(string region, string url)
    {
        var args = new List<ITagValue>
        {
                        new TagValue(CommonTags.RegionName, TransferResourceHelper.GetRegionDescription(region)),
                        new TagValue(CommonTags.PortalUrl, url)
                    };

        if (!string.IsNullOrEmpty(url))
        {
            args.Add(new TagValue(CommonTags.VirtualRootPath, url));
            args.Add(new TagValue(CommonTags.ProfileUrl, url + commonLinkUtility.GetMyStaff()));
        }

        return args;
    }

    private async Task<List<ITagValue>> CreateLogoArgsAsync(CultureInfo cultureInfo)
    {
        var args = new List<ITagValue>();

        var attachment = await tenantLogoManager.GetMailLogoAsAttachmentAsync(cultureInfo);

        if (attachment != null)
        {
            args.Add(new TagValue(CommonTags.LetterLogo, "cid:" + attachment.ContentId));
            args.Add(new TagValue(CommonTags.EmbeddedAttachments, new[] { attachment }));
        }

        return args;
    }
}

[Scope]
public sealed class BackupCreatedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, StudioNotifyHelper studioNotifyHelper) : INotifyAction
{
    public string ID { get => "backup_created"; }
    public List<ITagValue> Tags { get; set; }

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_backup_created, () => WebstudioNotifyPatternResource.pattern_backup_created),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_backup_created_tg)
        ];
    }

    public void Init(UserInfo user)
    {
        var culture = user.GetCulture();

        var bestRegardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        Tags =
        [
            new TagValue(CommonTags.OwnerName, user.DisplayUserName(displayUserSettingsHelper)),
            new TagValue("URL1", externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("creatingbackup", culture)),
            TagValues.TrulyYours(studioNotifyHelper, bestRegardsTxt)
        ];
    }
}

[Scope]
public sealed class BackupFailedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, StudioNotifyHelper studioNotifyHelper) : INotifyAction
{
    public string ID => "backup_failed";
    
    public List<ITagValue> Tags { get; set; }
    
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_backup_failed, () => WebstudioNotifyPatternResource.pattern_backup_failed),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_backup_failed_tg)
        ];
    }
    
    public void Init(UserInfo user, string errorMessage)
    {
        var culture = user.GetCulture();

        var bestRegardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        Tags =
        [
            new TagValue(CommonTags.Culture, culture.Name),
            new TagValue(CommonTags.UserName, user.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(CommonTags.Message, errorMessage),
            TagValues.TrulyYours(studioNotifyHelper, bestRegardsTxt)
        ];
    }
}

public sealed class ScheduledBackupFailedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, StudioNotifyHelper studioNotifyHelper) : INotifyAction
{
    public string ID => "scheduled_backup_failed";

    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_scheduled_backup_failed, () => WebstudioNotifyPatternResource.pattern_scheduled_backup_failed),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_scheduled_backup_failed_tg)
        ];
    }
    
    public void Init(UserInfo user, string errorMessage)
    {
        var culture = user.GetCulture();

        var bestRegardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        Tags =
        [
            new TagValue(CommonTags.Culture, culture.Name),
            new TagValue(CommonTags.UserName, user.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(CommonTags.Message, errorMessage),
            TagValues.TrulyYours(studioNotifyHelper, bestRegardsTxt)
        ];
    }
}

[Scope]
public sealed class RestoreStartedNotifyAction(StudioNotifyHelper studioNotifyHelper) : INotifyAction
{
    public string ID => "restore_started";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_restore_started, () => WebstudioNotifyPatternResource.pattern_restore_started),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_restore_started)
        ];
    }
    
    public void Init(UserInfo user)
    {
        var culture = user.GetCulture();

        var bestRegardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        Tags =
        [
            TagValues.TrulyYours(studioNotifyHelper, bestRegardsTxt)
        ];
    }
}

public sealed class RestoreCompletedV115NotifyAction(AuthManager authManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : INotifyAction
{
    public string ID => "restore_completed_v115";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_restore_completed, () => WebstudioNotifyPatternResource.pattern_restore_completed_v115),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_restore_completed_v115)
        ];
    }
    
    public async Task InitAsync(UserInfo user)
    {
        var hash = (await authManager.GetUserPasswordStampAsync(user.Id)).ToString("s", CultureInfo.InvariantCulture);
        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.PasswordChange, hash, user.Id);

        var orangeButtonText = BackupResource.ResourceManager.GetString("ButtonSetPassword", user.GetCulture());

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl))
        ];
    }
}