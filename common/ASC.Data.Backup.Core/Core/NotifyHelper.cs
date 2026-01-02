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
    StudioNotifySource studioNotifySource,
    TenantManager tenantManager,
    WorkContext workContext,
    CommonLinkUtility commonLinkUtility,
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
        tenantManager.SetCurrentTenant(tenant);

        var client = workContext.RegisterClient(serviceProvider, studioNotifySource);

        var users = (notifyUsers
            ? await userManager.GetUsersAsync(EmployeeStatus.Active)
            : [await userManager.GetUsersAsync(tenant.OwnerId)])
            .Where(u => u.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated))
            .ToArray();

        if (users.Length == 0)
        {
            return;
        }
        
        var migrationPortalStartNotifyAction = serviceProvider.GetService<MigrationPortalStartNotifyAction>();
        migrationPortalStartNotifyAction.Init(targetRegion);

        await client.SendNoticeToAsync(
            migrationPortalStartNotifyAction,
            users.Cast<IRecipient>().ToArray(),
            [StudioNotifyService.EMailSenderName]);
        
    }

    public async Task SendAboutTransferCompleteAsync(Tenant tenant, string targetRegion, string targetAddress, bool notifyOnlyOwner, int toTenantId)
    {
        tenantManager.SetCurrentTenant(tenant);

        var client = workContext.RegisterClient(serviceProvider, studioNotifySource);

        var users = (!notifyOnlyOwner
            ? await userManager.GetUsersAsync(EmployeeStatus.Active)
            : [await userManager.GetUsersAsync(tenant.OwnerId)])
            .Where(u => u.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated))
            .ToArray();

        if (users.Length == 0)
        {
            return;
        }
        
        var migrationPortalSuccessV115NotifyAction = serviceProvider.GetService<MigrationPortalSuccessV115NotifyAction>();
        
        foreach (var user in users)
        {        
            await migrationPortalSuccessV115NotifyAction.Init(user, targetRegion, targetAddress, toTenantId);
            
            await client.SendNoticeToAsync(
                migrationPortalSuccessV115NotifyAction,
                [user],
                [StudioNotifyService.EMailSenderName]);
        }
    }

    public async Task SendAboutTransferErrorAsync(Tenant tenant, string targetRegion, string resultAddress, bool notifyOnlyOwner)
    {
        tenantManager.SetCurrentTenant(tenant);

        var client = workContext.RegisterClient(serviceProvider, studioNotifySource);

        var users = (!notifyOnlyOwner
            ? await userManager.GetUsersAsync(EmployeeStatus.Active)
            : [await userManager.GetUsersAsync(tenant.OwnerId)])
            .Where(u => u.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated))
            .ToArray();

        if (users.Length == 0)
        {
            return;
        }
        
        var migrationPortalErrorNotifyAction = serviceProvider.GetService<MigrationPortalErrorNotifyAction>();
        migrationPortalErrorNotifyAction.Init(targetRegion, resultAddress);
        
        var migrationPortalServerFailureNotifyAction = serviceProvider.GetService<MigrationPortalServerFailureNotifyAction>();
        migrationPortalServerFailureNotifyAction.Init(targetRegion, resultAddress);
        
        await client.SendNoticeToAsync(
            !string.IsNullOrEmpty(targetRegion) ? migrationPortalErrorNotifyAction : migrationPortalServerFailureNotifyAction,
            users.Cast<IRecipient>().ToArray(),
            [StudioNotifyService.EMailSenderName]);
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
}

[Scope]
public sealed class BackupCreatedNotifyAction(TenantManager tenantManager, DisplayUserSettingsHelper displayUserSettingsHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, StudioNotifyHelper studioNotifyHelper) : NotifyAction(tenantManager)
{
    public override string ID { get => "backup_created"; }

    public override List<Pattern> Patterns
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
public sealed class BackupFailedNotifyAction(TenantManager tenantManager, DisplayUserSettingsHelper displayUserSettingsHelper, StudioNotifyHelper studioNotifyHelper) : NotifyAction(tenantManager)
{
    public override string ID => "backup_failed";

    public override List<Pattern> Patterns
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

[Scope]
public sealed class ScheduledBackupFailedNotifyAction(TenantManager tenantManager, DisplayUserSettingsHelper displayUserSettingsHelper, StudioNotifyHelper studioNotifyHelper) : NotifyAction(tenantManager)
{
    public override string ID => "scheduled_backup_failed";

    public override List<Pattern> Patterns
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
public sealed class RestoreStartedNotifyAction(TenantManager tenantManager, StudioNotifyHelper studioNotifyHelper) : NotifyAction(tenantManager)
{
    public override string ID => "restore_started";

    public override List<Pattern> Patterns
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

[Scope]
public sealed class RestoreCompletedV115NotifyAction(TenantManager tenantManager, AuthManager authManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : NotifyAction(tenantManager)
{
    public override string ID => "restore_completed_v115";

    public override List<Pattern> Patterns
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

[Scope]
public sealed class MigrationPortalSuccessV115NotifyAction(TenantManager tenantManager, CommonLinkUtility commonLinkUtility, AuthManager authManager, StudioNotifyHelper studioNotifyHelper, TenantLogoManager tenantLogoManager) : NotifyAction(tenantManager)
{
    public override string ID => "migration_success_v115";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_success, () => WebstudioNotifyPatternResource.pattern_migration_success_v115)
        ];
    }
    
    public async Task Init(UserInfo user, string region, string url, int toTenantId)
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

        var newTenantId = toTenantId;
        var hash = (await authManager.GetUserPasswordStampAsync(user.Id)).ToString("s", CultureInfo.InvariantCulture);
        var confirmationUrl = url + "/" + commonLinkUtility.GetConfirmationUrlRelative(newTenantId, user.Email, ConfirmType.PasswordChange, hash, user.Id);
        var culture = user.GetCulture();

        var orangeButtonText = BackupResource.ResourceManager.GetString("ButtonSetPassword", culture);
        args.Add(TagValues.OrangeButton(orangeButtonText, confirmationUrl));

        var bestReagardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);
        args.Add(TagValues.TrulyYours(studioNotifyHelper, bestReagardsTxt));

        var logoArgs = await CreateLogoArgsAsync(culture);
        args.AddRange(logoArgs);
        
        Tags = args;
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