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

using ASC.AuditTrail.Models;

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public sealed class PortalDeactivateNotifyAction(StudioNotifyHelper studioNotifyHelper, DisplayUserSettingsHelper displayUserSettingsHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "portal_deactivate";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_deactivate, () => WebstudioNotifyPatternResource.pattern_portal_deactivate),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_portal_deactivate_tg)
        ];
    }
    
    public void Init(UserInfo user, string deactivateUrl, string activateUrl)
    {
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonDeactivatePortal", culture);
        var bestRegardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        Tags =
        [
            new TagValue(CommonTags.ActivateUrl, activateUrl),
            TagValues.OrangeButton(orangeButtonText, deactivateUrl),
            TagValues.TrulyYours(studioNotifyHelper, bestRegardsTxt),
            new TagValue(CommonTags.OwnerName, user.DisplayUserName(displayUserSettingsHelper))
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class PortalDeleteNotifyAction(StudioNotifyHelper studioNotifyHelper, DisplayUserSettingsHelper displayUserSettingsHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "portal_delete";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_delete, () => WebstudioNotifyPatternResource.pattern_portal_delete),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_portal_delete_tg)
        ];
    }
    
    public void Init(UserInfo user, string url, bool showAutoRenewText)
    {
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonDeactivatePortal", culture);
        var bestRegardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, url),
            TagValues.TrulyYours(studioNotifyHelper, bestRegardsTxt),
            new TagValue(CommonTags.AutoRenew, showAutoRenewText.ToString()),
            new TagValue(CommonTags.OwnerName, user.DisplayUserName(displayUserSettingsHelper))
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class PortalDeleteSuccessV1NotifyAction(StudioNotifyHelper studioNotifyHelper, DisplayUserSettingsHelper displayUserSettingsHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "portal_delete_success_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_delete_success_v1, () => WebstudioNotifyPatternResource.pattern_portal_delete_success_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo user, string url)
    {
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonLeaveFeedback", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, url),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue("URL1", externalResourceSettingsHelper.Common.GetRegionalFullEntry("legalterms", culture)),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("docspace_deactivated.gif")),
            new TagValue(CommonTags.OwnerName, user.DisplayUserName(displayUserSettingsHelper))
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class PortalDeletedToSupportNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper) : INotifyAction
{
    public string ID => "portal_deleted_to_support";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_deleted_to_support, () => WebstudioNotifyPatternResource.pattern_portal_deleted_to_support)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo user, string tenantDomain,  CustomerInfo customerInfo)
    {
        Tags =
        [
            new TagValue(CommonTags.PortalUrl, tenantDomain),
            new TagValue(CommonTags.UserEmail, user.Email),
            new TagValue(CommonTags.UserName, user.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(CommonTags.OwnerName, customerInfo?.Email),
            new TagValue(CommonTags.Footer, null),
            TagValues.WithoutUnsubscribe()
        ];
    }
}

[Scope]
public sealed class ProfileDeleteNotifyAction(CommonLinkUtility commonLinkUtility, AuthContext authContext, IUrlShortener urlShortener, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "profile_delete";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_profile_delete, () => WebstudioNotifyPatternResource.pattern_profile_delete)
        ];
    }
    
    public async Task Init(UserInfo user)
    {
        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.ProfileRemove, authContext.CurrentAccount.ID, authContext.CurrentAccount.ID);
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonRemoveProfile", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl)),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, user.GetCulture().Name)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class ProfileHasDeletedItselfNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility) : INotifyAction
{
    public string ID => "profile_has_deleted_itself";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_profile_has_deleted_itself, () => WebstudioNotifyPatternResource.pattern_profile_has_deleted_itself)
        ];
    }
    
    public async Task Init(UserInfo user, string culture)
    {
        var userName = user.DisplayUserName(displayUserSettingsHelper);
        var userLink = await GetUserProfileLinkAsync(user.Id);
        
        Tags =
        [
            new TagValue(CommonTags.FromUserName, userName),
            new TagValue(CommonTags.FromUserLink, userLink),
            new TagValue(CommonTags.Culture, culture)
        ];
    }
    
    private async Task<string> GetUserProfileLinkAsync(Guid userId)
    {
        return commonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(userId));
    }
}

public sealed class ReassignsCompletedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility) : INotifyAction
{
    public string ID => "reassigns_completed";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_reassigns_completed, () => WebstudioNotifyPatternResource.pattern_reassigns_completed)
        ];
    }
    
    public async Task Init(Guid recipientId, UserInfo fromUser, UserInfo toUser)
    {
        Tags =
        [
            new TagValue(CommonTags.UserName, await displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(CommonTags.FromUserName, fromUser.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(CommonTags.FromUserLink, await GetUserProfileLinkAsync(fromUser.Id)),
            new TagValue(CommonTags.ToUserName, toUser.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(CommonTags.ToUserLink, await GetUserProfileLinkAsync(toUser.Id))
        ];
    }
    
    private async Task<string> GetUserProfileLinkAsync(Guid userId)
    {
        return commonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(userId));
    }
}

public sealed class ReassignsFailedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility) : INotifyAction
{
    public string ID => "reassigns_failed";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_reassigns_failed, () => WebstudioNotifyPatternResource.pattern_reassigns_failed)
        ];
    }
    
    public async Task Init(Guid recipientId, UserInfo fromUser, UserInfo toUser, string message)
    {
        Tags =
        [
            new TagValue(CommonTags.UserName, await displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(CommonTags.FromUserName, fromUser.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(CommonTags.FromUserLink, await GetUserProfileLinkAsync(fromUser.Id)),
            new TagValue(CommonTags.ToUserName, toUser.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(CommonTags.ToUserLink, await GetUserProfileLinkAsync(toUser.Id)),
            new TagValue(CommonTags.Message, message)
        ];
    }
    
    private async Task<string> GetUserProfileLinkAsync(Guid userId)
    {
        return commonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(userId));
    }
}

public sealed class RemoveUserDataCompletedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility) : INotifyAction
{
    public string ID => "remove_user_data_completed";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_remove_user_data_completed, () => WebstudioNotifyPatternResource.pattern_remove_user_data_completed)
        ];
    }
    
    public async Task Init(Guid recipientId, UserInfo user, string fromUserName, long docsSpace, long crmSpace, long mailSpace, long talkSpace)
    {
        Tags =
        [
            new TagValue(CommonTags.UserName, await displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(CommonTags.FromUserName, fromUserName.HtmlEncode()),
            new TagValue(CommonTags.FromUserLink, await GetUserProfileLinkAsync(user.Id)),
            new TagValue("DocsSpace", FileSizeComment.FilesSizeToString(docsSpace)),
            new TagValue("CrmSpace", FileSizeComment.FilesSizeToString(crmSpace)),
            new TagValue("MailSpace", FileSizeComment.FilesSizeToString(mailSpace)),
            new TagValue("TalkSpace", FileSizeComment.FilesSizeToString(talkSpace))
        ];
    }
    
    private async Task<string> GetUserProfileLinkAsync(Guid userId)
    {
        return commonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(userId));
    }
}

public sealed class RemoveUserDataCompletedCustomModeNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility) : INotifyAction
{
    public string ID => "remove_user_data_completed_custom_mode";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => CustomModeResource.subject_remove_user_data_completed_custom_mode, () => CustomModeResource.pattern_remove_user_data_completed_custom_mode)
        ];
    }
    
    public async Task Init(Guid recipientId, UserInfo user, string fromUserName, long docsSpace, long crmSpace, long mailSpace, long talkSpace)
    {
        Tags =
        [
            new TagValue(CommonTags.UserName, await displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(CommonTags.FromUserName, fromUserName.HtmlEncode()),
            new TagValue(CommonTags.FromUserLink, await GetUserProfileLinkAsync(user.Id)),
            new TagValue("DocsSpace", FileSizeComment.FilesSizeToString(docsSpace)),
            new TagValue("CrmSpace", FileSizeComment.FilesSizeToString(crmSpace)),
            new TagValue("MailSpace", FileSizeComment.FilesSizeToString(mailSpace)),
            new TagValue("TalkSpace", FileSizeComment.FilesSizeToString(talkSpace))
        ];
    }
    
    private async Task<string> GetUserProfileLinkAsync(Guid userId)
    {
        return commonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(userId));
    }
}

[Scope]
public sealed class RemoveUserDataFailedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility) : INotifyAction
{
    public string ID => "remove_user_data_failed";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_remove_user_data_failed, () => WebstudioNotifyPatternResource.pattern_remove_user_data_failed)
        ];
    }
    
    public async Task Init(Guid recipientId, UserInfo user, string fromUserName, string message)
    {
        Tags =
        [
            new TagValue(CommonTags.UserName, await displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(CommonTags.FromUserName, fromUserName.HtmlEncode()),
            new TagValue(CommonTags.FromUserLink, await GetUserProfileLinkAsync(user.Id)),
            new TagValue(CommonTags.Message, message)
        ];
    }
    
    private async Task<string> GetUserProfileLinkAsync(Guid userId)
    {
        return commonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(userId));
    }
}

[Scope]
public sealed class ConfirmOwnerChangeNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper) : INotifyAction
{
    public string ID => "owner_confirm_change";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_confirm_owner_change, () => WebstudioNotifyPatternResource.pattern_confirm_owner_change)
        ];
    }
    
    public void Init(UserInfo owner, UserInfo newOwner, string confirmOwnerUpdateUrl)
    {        
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirmPortalOwnerUpdate", owner.GetCulture());
        
        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmOwnerUpdateUrl),
            new TagValue(CommonTags.UserName, newOwner.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(CommonTags.OwnerName, owner.DisplayUserName(displayUserSettingsHelper))
        ];
    }
}

[Scope]
public sealed class ActivateEmailNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : INotifyAction
{
    public string ID => "activate_email";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_activate_email, () => WebstudioNotifyPatternResource.pattern_activate_email)
        ];
    }
    
    public async Task Init(UserInfo user, string email)
    {        
        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(email, ConfirmType.EmailActivation, null, user.Id);
        var shortLink = await urlShortener.GetShortenLinkAsync(confirmationUrl);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonActivateEmail", GetCulture(user));
        
        Tags =
        [
            new TagValue(CommonTags.InviteLink, shortLink),
            new TagValue(CommonTags.Culture, user.GetCulture().Name),
            TagValues.OrangeButton(orangeButtonText, shortLink),
            new TagValue(CommonTags.UserDisplayName, (user.DisplayUserName(displayUserSettingsHelper) ?? string.Empty).Trim())
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class EmailChangeV115NotifyAction(CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : INotifyAction
{
    public string ID => "change_email_v115";
    
    public List<ITagValue> Tags { get; set; }
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_email_v115, () => WebstudioNotifyPatternResource.pattern_change_email_v115)
        ];
    }
    
    public async Task Init(UserInfo user, string email, DateTime auditEventDate)
    {        
        var postfix = auditEventDate.ToString("s", CultureInfo.InvariantCulture);

        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(email, ConfirmType.EmailChange, postfix, user.Id);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangeEmail", GetCulture(user));
        
        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl)),
            new TagValue(CommonTags.Culture, user.GetCulture().Name)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class UserMessageToAdminNotifyAction: INotifyAction 
{
    public string ID => "for_admin_notify";
    
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_for_admin_notify, () => WebstudioNotifyPatternResource.pattern_for_admin_notify),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_for_admin_notify_tg)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(string email, string message, string culture)
    {        
        List<ITagValue> tags =
        [
            new TagValue(CommonTags.Body, message),
            new TagValue(CommonTags.UserEmail, email)
        ];

        if (!string.IsNullOrEmpty(culture))
        {
            tags.Add(new TagValue(CommonTags.Culture, culture));
        }
        
        Tags = tags;
    }
};

public sealed class UserMessageToSalesNotifyAction: INotifyAction 
{
    public string ID => "for_sales_notify";
    
    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_for_sales_notify, () => WebstudioNotifyPatternResource.pattern_for_sales_notify)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(string email, string userName, string message)
    {        
        Tags = [
            
            new TagValue(CommonTags.Body, message),
            new TagValue(CommonTags.UserEmail, email),
            new TagValue(CommonTags.UserName, userName)
        ];
    }
};

public sealed class PasswordChangeV115NotifyAction(CommonLinkUtility commonLinkUtility, TenantManager tenantManager, IUrlShortener urlShortener) : INotifyAction
{
    public string ID => "change_password_v115";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_password_v115, () => WebstudioNotifyPatternResource.pattern_change_password_v115)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo userInfo, DateTime auditEventDate)
    {        
        var hash = auditEventDate.ToString("s", CultureInfo.InvariantCulture);

        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(userInfo.Email, ConfirmType.PasswordChange, hash, userInfo.Id);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangePassword", GetCulture(userInfo));
        
        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl))
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

[Scope]
public sealed class PasswordChangedNotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "password_changed";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_password_changed, () => WebstudioNotifyPatternResource.pattern_password_changed)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo userInfo, AuditEvent auditEvent)
    {        
        var cultureInfo = GetCulture(userInfo);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonOpenDocSpace", cultureInfo);
        var confirmationUrl = commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetConfirmationUrlRelative(userInfo.TenantId, userInfo.Email, ConfirmType.Auth, null, userInfo.Id));
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);

        var location = string.Empty;
        if (!string.IsNullOrEmpty(auditEvent.Country) || !string.IsNullOrEmpty(auditEvent.City))
        {
            location = auditEvent.Country + ", " + auditEvent.City;
        }
        
        Tags =
        [
            new TagValue(CommonTags.UserName, userInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.UserEmail, userInfo.Email),
            new TagValue(CommonTags.Date, auditEvent.Date.ToShortDateString() + " " + auditEvent.Date.ToShortTimeString()),
            new TagValue(CommonTags.Device, auditEvent.Platform),
            new TagValue(CommonTags.Location, location),
            new TagValue(CommonTags.Browser, auditEvent.Browser),
            new TagValue(CommonTags.IP, auditEvent.IP),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class PasswordSetNotifyAction(CommonLinkUtility commonLinkUtility, TenantManager tenantManager, IUrlShortener urlShortener) : INotifyAction
{
    public string ID => "set_password";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_set_password, () => WebstudioNotifyPatternResource.pattern_set_password)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo userInfo, DateTime auditEventDate)
    {        
        var hash = auditEventDate.ToString("s", CultureInfo.InvariantCulture);

        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(userInfo.Email, ConfirmType.PasswordChange, hash, userInfo.Id);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonSetPassword", GetCulture(userInfo));
        
        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl))
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class PhoneChangeNotifyAction(CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : INotifyAction
{
    public string ID => "change_phone";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_phone, () => WebstudioNotifyPatternResource.pattern_change_phone)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo userInfo)
    {        
        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(userInfo.Email.ToLower(), ConfirmType.PhoneActivation);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangePhone", GetCulture(userInfo));
        
        Tags = 
        [
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl))
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class TfaChangeNotifyAction(CommonLinkUtility commonLinkUtility, TenantManager tenantManager)  : INotifyAction
{
    public string ID => "change_tfa";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_tfa, () => WebstudioNotifyPatternResource.pattern_change_tfa)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo userInfo)
    {        
        var confirmationUrl = commonLinkUtility.GetFullAbsolutePath(string.Empty);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangeTfa", GetCulture(userInfo));
        
        Tags = 
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

[Scope]
public sealed class UserHasJoinNotifyAction  : INotifyAction
{
    public string ID => "user_has_join";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_has_join, () => WebstudioNotifyPatternResource.pattern_user_has_join),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_user_has_join_tg)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

[Scope]
public sealed class JoinUsersNotifyAction(CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener)  : INotifyAction
{
    public string ID => "join";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_join, () => WebstudioNotifyPatternResource.pattern_join),
            new JabberPattern(() => WebstudioNotifyPatternResource.pattern_join)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(string email, EmployeeType emplType, string culture)
    {        
        var inviteUrl = commonLinkUtility.GetConfirmationEmailUrl(email, ConfirmType.EmpInvite, (int)emplType + "trust") + $"&emplType={(int)emplType}";
        var shortLink = await urlShortener.GetShortenLinkAsync(inviteUrl);

        var orangeButtonText = WebstudioNotifyPatternResource.ButtonJoin;

        List<ITagValue> tags =
        [
            new TagValue(CommonTags.InviteLink, shortLink),
            TagValues.OrangeButton(orangeButtonText, shortLink)
        ];

        if (!string.IsNullOrEmpty(culture))
        {
            tags.Add(new TagValue(CommonTags.Culture, culture));
        }
        
        Tags = tags;
    }
}

public sealed class MigrationPortalStartNotifyAction : INotifyAction
{
    public string ID => "migration_start";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_start, () => WebstudioNotifyPatternResource.pattern_migration_start)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class MigrationPortalSuccessV115NotifyAction : INotifyAction
{
    public string ID => "migration_success_v115";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_success, () => WebstudioNotifyPatternResource.pattern_migration_success_v115)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class MigrationPortalErrorNotifyAction : INotifyAction
{
    public string ID => "migration_error";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_error, () => WebstudioNotifyPatternResource.pattern_migration_error)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class MigrationPortalServerFailureNotifyAction : INotifyAction
{
    public string ID => "migration_server_failure";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_error, () => WebstudioNotifyPatternResource.pattern_migration_server_failure)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class PortalRenameNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper) : INotifyAction
{
    public string ID => "portal_rename";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_rename, () => WebstudioNotifyPatternResource.pattern_portal_rename)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo u, string oldVirtualRootPath)
    {
        Tags = [
            new TagValue(CommonTags.PortalUrl, oldVirtualRootPath),
            new TagValue(CommonTags.UserDisplayName, u.DisplayUserName(displayUserSettingsHelper))
        ];
    }
}

public sealed class SaasGuestActivationV115NotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : INotifyAction
{
    public string ID => "saas_guest_activation_v115";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_guest_activation_v115, () => WebstudioNotifyPatternResource.pattern_saas_guest_activation_v115)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo newUserInfo)
    {        
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.ActivateUrl, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Footer, "social")
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

public sealed class EnterpriseGuestActivationV10NotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : INotifyAction
{
    public string ID => "enterprise_guest_activation_v10";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_guest_activation_v10, () => WebstudioNotifyPatternResource.pattern_enterprise_guest_activation_v10)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo newUserInfo)
    {        
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.ActivateUrl, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Footer, null)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

public sealed class EnterpriseWhitelabelGuestActivationV10NotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : INotifyAction
{
    public string ID => "enterprise_whitelabel_guest_activation_v10";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_guest_activation_v10, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_guest_activation_v10)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo newUserInfo)
    {        
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.ActivateUrl, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Footer, null)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

public sealed class OpensourceGuestActivationV11NotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : INotifyAction
{
    public string ID => "opensource_guest_activation_v11";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_guest_activation_v11, () => WebstudioNotifyPatternResource.pattern_opensource_guest_activation_v11)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo newUserInfo)
    {        
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.ActivateUrl, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Footer, "opensource")
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

public sealed class SaasGuestWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_guest_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_guest_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccessYouWebOffice", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        var url1 = externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("userguides", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue("IMG1", img1),
            new TagValue("IMG2", img2),
            new TagValue("IMG3", img3),
            new TagValue("IMG4", img4),
            new TagValue("URL1", url1),
            new TagValue(CommonTags.Footer, "social")
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }
}

public sealed class EnterpriseGuestWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "enterprise_guest_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_guest_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccessYourPortal", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        var url1 = externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("userguides", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue("IMG1", img1),
            new TagValue("IMG2", img2),
            new TagValue("IMG3", img3),
            new TagValue("IMG4", img4),
            new TagValue("URL1", url1),
            new TagValue(CommonTags.Footer, null)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }
}

public sealed class EnterpriseWhitelabelGuestWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "enterprise_whitelabel_guest_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_guest_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccessYourPortal", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        var url1 = externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("userguides", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue("IMG1", img1),
            new TagValue("IMG2", img2),
            new TagValue("IMG3", img3),
            new TagValue("IMG4", img4),
            new TagValue("URL1", url1),
            new TagValue(CommonTags.Footer, null)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }
}

public sealed class OpensourceGuestWelcomeV1NotifyAction (StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "opensource_guest_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_guest_welcome_v1, () => WebstudioNotifyPatternResource.pattern_opensource_guest_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccessYouWebOffice", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        var url1 = externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("userguides", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue("IMG1", img1),
            new TagValue("IMG2", img2),
            new TagValue("IMG3", img3),
            new TagValue("IMG4", img4),
            new TagValue("URL1", url1),
            new TagValue(CommonTags.Footer, "opensource")
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }
}

public sealed class SaasCustomModeRegDataNotifyAction : INotifyAction
{
    public string ID => "saas_custom_mode_reg_data";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => CustomModeResource.subject_saas_custom_mode_reg_data, () => CustomModeResource.pattern_saas_custom_mode_reg_data)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo u)
    {        
        Tags = [
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.UserLastName, u.LastName.HtmlEncode()),
            new TagValue(CommonTags.UserEmail, u.Email.HtmlEncode()),
            new TagValue(CommonTags.Phone, u.MobilePhone != null ? u.MobilePhone.HtmlEncode() : "-"),
            new TagValue(CommonTags.Date, u.CreateDate.ToShortDateString() + " " + u.CreateDate.ToShortTimeString()),
            new TagValue(CommonTags.Footer, null),
            TagValues.WithoutUnsubscribe()
        ];
    }
}

public sealed class StorageEncryptionStartNotifyAction : INotifyAction
{
    public string ID => "storage_encryption_start";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_encryption_start, () => WebstudioNotifyPatternResource.pattern_storage_encryption_start)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class StorageEncryptionSuccessNotifyAction : INotifyAction
{
    public string ID => "storage_encryption_success";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_encryption_success, () => WebstudioNotifyPatternResource.pattern_storage_encryption_success)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class StorageEncryptionErrorNotifyAction : INotifyAction
{
    public string ID => "storage_encryption_error";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_encryption_error, () => WebstudioNotifyPatternResource.pattern_storage_encryption_error)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class StorageDecryptionStartNotifyAction : INotifyAction
{
    public string ID => "storage_decryption_start";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_decryption_start, () => WebstudioNotifyPatternResource.pattern_storage_decryption_start)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class StorageDecryptionSuccessNotifyAction : INotifyAction
{
    public string ID => "storage_decryption_success";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_decryption_success, () => WebstudioNotifyPatternResource.pattern_storage_decryption_success)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class StorageDecryptionErrorNotifyAction : INotifyAction
{
    public string ID => "storage_decryption_error";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_decryption_error, () => WebstudioNotifyPatternResource.pattern_storage_decryption_error)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasRoomInviteNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_room_invite";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_room_invite, () => WebstudioNotifyPatternResource.pattern_saas_room_invite)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(string culture, string roomTitle, string confirmationUrl)
    {         
        var cultureInfo = string.IsNullOrEmpty(culture) ? GetCulture(null) : new CultureInfo(culture);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);
        
        Tags = [
            new TagValue(CommonTags.Message, roomTitle),
            new TagValue(CommonTags.InviteLink, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class SaasAgentInviteNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_agent_invite";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_agent_invite, () => WebstudioNotifyPatternResource.pattern_saas_agent_invite)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(string culture, string roomTitle, string confirmationUrl)
    {         
        var cultureInfo = string.IsNullOrEmpty(culture) ? GetCulture(null) : new CultureInfo(culture);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);
        
        Tags = [
            new TagValue(CommonTags.Message, roomTitle),
            new TagValue(CommonTags.InviteLink, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class SaasRoomInviteExistingUserNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_room_invite_existing_user";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_room_invite_existing_user, () => WebstudioNotifyPatternResource.pattern_saas_room_invite_existing_user)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo user, string roomTitle, string roomUrl)
    {         
        var cultureInfo = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonJoinRoom", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);
        
        Tags = [
            new TagValue(CommonTags.Message, roomTitle),
            new TagValue(CommonTags.InviteLink, roomUrl),
            TagValues.OrangeButton(orangeButtonText, roomUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class SaasAgentInviteExistingUserNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_agent_invite_existing_user";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_agent_invite_existing_user, () => WebstudioNotifyPatternResource.pattern_saas_agent_invite_existing_user)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo user, string roomTitle, string roomUrl)
    {         
        var cultureInfo = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonJoinAgent", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);
        
        Tags = [
            new TagValue(CommonTags.Message, roomTitle),
            new TagValue(CommonTags.InviteLink, roomUrl),
            TagValues.OrangeButton(orangeButtonText, roomUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

[Scope]
public sealed class SaasDocSpaceInviteNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_docspace_invite";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_docspace_invite, () => WebstudioNotifyPatternResource.pattern_saas_docspace_invite)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(string confirmationUrl, string culture = "")
    {        
        var cultureInfo = string.IsNullOrEmpty(culture) ? GetCulture(null) : new CultureInfo(culture);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);
        
        Tags = [
            
            new TagValue(CommonTags.InviteLink, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

[Scope]
public sealed class SaasDocSpaceRegistrationNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_docspace_registration";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_docspace_registration, () => WebstudioNotifyPatternResource.pattern_saas_docspace_registration)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(string confirmationUrl, string culture = "")
    {        
        var cultureInfo = string.IsNullOrEmpty(culture) ? GetCulture(null) : new CultureInfo(culture);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonRegister", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);
        
        Tags = [
            new TagValue(CommonTags.InviteLink, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class SaasAdminActivationV1NotifyAction : INotifyAction
{
    public string ID => "saas_admin_activation_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_activation_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class EnterpriseAdminActivationV1NotifyAction : INotifyAction
{
    public string ID => "enterprise_admin_activation_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_activation_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class EnterpriseWhitelabelAdminActivationV1NotifyAction : INotifyAction
{
    public string ID => "enterprise_whitelabel_admin_activation_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_admin_activation_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class OpensourceAdminActivationV1NotifyAction : INotifyAction
{
    public string ID => "opensource_admin_activation_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_opensource_admin_activation_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasAdminWelcomeV1NotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_admin_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {        
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonUpgrade", culture);
        
        Tags = [
            new TagValue(CommonTags.Footer, "common"),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.PricingPage, commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/portal-payments")),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/portal-payments")),
            TagValues.TrulyYours(studioNotifyHelper, WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", GetCulture(newUserInfo)), true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("discover_business_subscription.gif"))
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class EnterpriseAdminWelcomeV1NotifyAction : INotifyAction
{
    public string ID => "enterprise_admin_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class EnterpriseWhitelabelAdminWelcomeV1NotifyAction : INotifyAction
{
    public string ID => "enterprise_whitelabel_admin_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_admin_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class OpensourceAdminWelcomeV1NotifyAction : INotifyAction
{
    public string ID => "opensource_admin_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_opensource_admin_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class DocsTipsNotifyAction : INotifyAction
{
    public string ID => "docs_tips";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_docs_tips, () => WebstudioNotifyPatternResource.pattern_docs_tips)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasAdminTrialWarningAfterHalfYearV1NotifyAction : INotifyAction
{
    public string ID => "saas_admin_trial_warning_after_half_year_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_trial_warning_after_half_year_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_trial_warning_after_half_year_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasAdminStartupWarningAfterYearV1NotifyAction : INotifyAction
{
    public string ID => "saas_admin_startup_warning_after_year_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_startup_warning_after_year_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_startup_warning_after_year_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}


public sealed class SaasUserWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_user_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_user_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {        
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborate", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        var url1 = externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("userguides", culture);
        
        Tags = [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue("IMG1", img1),
            new TagValue("IMG2", img2),
            new TagValue("IMG3", img3),
            new TagValue("IMG4", img4),
            new TagValue("URL1", url1),
            new TagValue(CommonTags.Footer, "social")
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }
}

public sealed class EnterpriseUserWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "enterprise_user_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_user_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {        
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborate", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        var url1 = externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("userguides", culture);
        
        Tags = [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue("IMG1", img1),
            new TagValue("IMG2", img2),
            new TagValue("IMG3", img3),
            new TagValue("IMG4", img4),
            new TagValue("URL1", url1),
            new TagValue(CommonTags.Footer, null)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }
}

public sealed class EnterpriseWhitelabelUserWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "enterprise_whitelabel_user_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_user_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {        
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborate", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        var url1 = externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("userguides", culture);
        
        Tags = [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue("IMG1", img1),
            new TagValue("IMG2", img2),
            new TagValue("IMG3", img3),
            new TagValue("IMG4", img4),
            new TagValue("URL1", url1),
            new TagValue(CommonTags.Footer, null)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }
}

public sealed class EnterpriseWhitelabelUserWelcomeCustomModeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "enterprise_whitelabel_user_welcome_custom_mode_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_user_welcome_v3)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {        
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborate", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        var url1 = externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("userguides", culture);
        
        Tags = [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue("IMG1", img1),
            new TagValue("IMG2", img2),
            new TagValue("IMG3", img3),
            new TagValue("IMG4", img4),
            new TagValue("URL1", url1),
            new TagValue(CommonTags.Footer, null)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }
}

public sealed class OpensourceUserWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "opensource_user_welcome_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_opensource_user_welcome_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo newUserInfo)
    {        
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborate", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        var url1 = externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("userguides", culture);
        
        Tags = [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue("IMG1", img1),
            new TagValue("IMG2", img2),
            new TagValue("IMG3", img3),
            new TagValue("IMG4", img4),
            new TagValue("URL1", url1),
            new TagValue(CommonTags.Footer, "opensource")
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }
}

public sealed class SaasUserActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, IUrlShortener urlShortener, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "saas_user_activation_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_saas_user_activation_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo newUserInfo)
    {        
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.ActivateUrl, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Footer, "social")
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

public sealed class EnterpriseUserActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, IUrlShortener urlShortener, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "enterprise_user_activation_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_user_activation_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo newUserInfo)
    {        
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.ActivateUrl, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Footer, null)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

public sealed class EnterpriseWhitelabelUserActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, IUrlShortener urlShortener, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "enterprise_whitelabel_user_activation_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_whitelabel_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_whitelabel_user_activation_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo newUserInfo)
    {        
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.ActivateUrl, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Footer, null)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

public sealed class OpensourceUserActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, IUrlShortener urlShortener, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : INotifyAction
{
    public string ID => "opensource_user_activation_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_opensource_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_opensource_user_activation_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo newUserInfo)
    {        
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.ActivateUrl, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Footer, "opensource")
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
    
    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

public sealed class SaasAdminModulesV1NotifyAction : INotifyAction
{
    public string ID => "saas_admin_modules_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_modules_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_modules_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasAdminUserAppsTipsV1NotifyAction : INotifyAction
{
    public string ID => "saas_admin_user_apps_tips_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_user_apps_tips_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_user_apps_tips_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class EnterpriseAdminUserAppsTipsV1NotifyAction : INotifyAction
{
    public string ID => "enterprise_admin_user_apps_tips_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_user_apps_tips_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_user_apps_tips_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class RoomsActivityNotifyAction : INotifyAction
{
    public string ID => "rooms_activity";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_rooms_activity, () => WebstudioNotifyPatternResource.pattern_rooms_activity),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_rooms_activity)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SendWhatsNewNotifyAction : INotifyAction
{
    public string ID => "send_whats_new";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_send_whats_new, () => WebstudioNotifyPatternResource.pattern_send_whats_new),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_send_whats_new)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction : INotifyAction
{
    public string ID => "saas_owner_payment_warning_grace_period_before_activation";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_before_activation)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasOwnerPaymentWarningGracePeriodActivationNotifyAction : INotifyAction
{
    public string ID => "saas_owner_payment_warning_grace_period_activation";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_activation)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasOwnerPaymentWarningGracePeriodLastDayNotifyAction : INotifyAction
{
    public string ID => "saas_owner_payment_warning_grace_period_last_day";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_last_day, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_last_day)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction : INotifyAction
{
    public string ID => "saas_owner_payment_warning_grace_period_expired";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_expired, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_expired)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasAdminVideoGuidesNotifyAction : INotifyAction
{
    public string ID => "saas_video_guides_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_video_guides_v1, () => WebstudioNotifyPatternResource.pattern_saas_video_guides_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class SaasAdminIntegrationsNotifyAction : INotifyAction
{
    public string ID => "saas_admin_integrations_v1";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_integrations_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_integrations_v1)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class ZoomWelcomeNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "zoom_welcome";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_zoom_welcome, () => WebstudioNotifyPatternResource.pattern_zoom_welcome)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo u)
    {        
        var culture = GetCulture(u);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.Culture, culture.Name),
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class MigrationPersonalToDocspaceNotifyAction(CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "migration_personal_to_docspace";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_personal_to_docspace, () => WebstudioNotifyPatternResource.pattern_migration_personal_to_docspace)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public async Task Init(UserInfo userInfo, DateTime auditEventDate)
    {        
        var hash = auditEventDate.ToString("s", CultureInfo.InvariantCulture);

        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(userInfo.Email, ConfirmType.PasswordChange, hash, userInfo.Id);

        var cultureInfo = GetCulture(userInfo);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", cultureInfo);

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);
        
        Tags = [
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl)),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name),
            new TagValue(CommonTags.Footer, "social")
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class EnterpriseAdminPaymentWarningGracePeriodBeforeActivationNotifyAction : INotifyAction
{
    public string ID => "enterprise_admin_payment_warning_grace_period_before_activation";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_before_activation)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class EnterpriseAdminPaymentWarningGracePeriodActivationNotifyAction : INotifyAction
{
    public string ID => "enterprise_admin_payment_warning_grace_period_activation";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_activation)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class EnterpriseAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction : INotifyAction
{
    public string ID => "enterprise_admin_payment_warning_grace_period_before_expiration";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_before_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_before_expiration)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class EnterpriseAdminPaymentWarningGracePeriodExpirationNotifyAction : INotifyAction
{
    public string ID => "enterprise_admin_payment_warning_grace_period_expiration";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_expiration)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class EnterpriseAdminPaymentWarningLifetimeBeforeExpirationNotifyAction : INotifyAction
{
    public string ID => "enterprise_admin_payment_warning_lifetime_before_expiration";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_lifetime_before_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_lifetime_before_expiration)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class EnterpriseAdminPaymentWarningLifetimeExpirationNotifyAction : INotifyAction
{
    public string ID => "enterprise_admin_payment_warning_lifetime_expiration";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_lifetime_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_lifetime_expiration)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class DeveloperAdminPaymentWarningGracePeriodBeforeActivationNotifyAction : INotifyAction
{
    public string ID => "developer_admin_payment_warning_grace_period_before_activation";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_before_activation)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class DeveloperAdminPaymentWarningGracePeriodActivationNotifyAction : INotifyAction
{
    public string ID => "developer_admin_payment_warning_grace_period_activation";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_activation)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class DeveloperAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction : INotifyAction
{
    public string ID => "developer_admin_payment_warning_grace_period_before_expiration";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_before_expiration, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_before_expiration)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class DeveloperAdminPaymentWarningGracePeriodExpirationNotifyAction : INotifyAction
{
    public string ID => "developer_admin_payment_warning_grace_period_expiration";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_expiration, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_expiration)
        ];
    }

    public List<ITagValue> Tags { get; set; }
}

public sealed class UserTypeChangedNotifyAction(StudioNotifyHelper studioNotifyHelper, DisplayUserSettingsHelper displayUserSettingsHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "user_type_changed";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_type_changed, () => WebstudioNotifyPatternResource.pattern_user_type_changed)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo u, string userType)
    {        
        var culture = GetCulture(u);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue("UserType", userType),
            new TagValue("HelpCenterUrl", externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("accessrights", culture)),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class UserRoleChangedNotifyAction(ExternalResourceSettingsHelper externalResourceSettingsHelper, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "user_role_changed";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_role_changed, () => WebstudioNotifyPatternResource.pattern_user_role_changed)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo user, string roomTitle, string roomUrl, string userRole)
    {        
        var culture = GetCulture(user);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue("RoomTitle", roomTitle),
            new TagValue("RoomUrl", roomUrl),
            new TagValue("UserRole", userRole),
            new TagValue("HelpCenterUrl", externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("accessrights", culture)),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class UserAgentRoleChangedNotifyAction(ExternalResourceSettingsHelper externalResourceSettingsHelper, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "user_agent_role_changed";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_agent_role_changed, () => WebstudioNotifyPatternResource.pattern_user_agent_role_changed)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo user, string roomTitle, string roomUrl, string userRole)
    {        
        var culture = GetCulture(user);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue("RoomTitle", roomTitle),
            new TagValue("RoomUrl", roomUrl),
            new TagValue("UserRole", userRole),
            new TagValue("HelpCenterUrl", externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("accessrights", culture)),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class TopUpWalletErrorNotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "top_up_wallet_error";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_top_up_wallet_error, () => WebstudioNotifyPatternResource.pattern_top_up_wallet_error)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo user)
    {        
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGoToWalletSettings", GetCulture(user));
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/wallet")),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class RenewSubscriptionErrorNotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "renew_subscription_error";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_renew_subscription_error, () => WebstudioNotifyPatternResource.pattern_renew_subscription_error)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo user)
    {        
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGoToServices", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~/portal-settings/services")),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class ApiKeyExpiredNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : INotifyAction
{
    public string ID => "api_key_expired";

    public List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_api_key_expired, () => WebstudioNotifyPatternResource.pattern_api_key_expired)
        ];
    }

    public List<ITagValue> Tags { get; set; }
    
    public void Init(UserInfo user, string keyName)
    {        
        var culture = GetCulture(user);

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);
        
        Tags = [
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Message, keyName),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
    
    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? tenantManager.GetCurrentTenant(false)?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}

public sealed class AdminNotifyNotifyAction : INotifyAction
{
    public string ID => "admin_notify";

    public List<Pattern> Patterns
    {
        get =>
        [
            //new EmailPattern("admin_notify", () => WebstudioNotifyPatternResource.subject_admin_notify, () => WebstudioNotifyPatternResource.pattern_admin_notify)
        ];
    }

    public List<ITagValue> Tags { get; set; }
};

public sealed class PeriodicNotifyAction : INotifyAction
{
    public string ID => "periodic_notify";

    public List<Pattern> Patterns
    {
        get =>
        [
            //new EmailPattern("periodic_notify", () => WebstudioNotifyPatternResource.subject_periodic_notify, () => WebstudioNotifyPatternResource.pattern_periodic_notify)
        ];
    }

    public List<ITagValue> Tags { get; set; }
};