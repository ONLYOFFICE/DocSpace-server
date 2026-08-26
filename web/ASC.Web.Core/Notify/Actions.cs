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

using ASC.AuditTrail.Models;

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public sealed class PortalDeactivateNotifyAction(StudioNotifyHelper studioNotifyHelper, DisplayUserSettingsHelper displayUserSettingsHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "portal_deactivate";

    public override List<Pattern> Patterns
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
}

[Scope]
public sealed class PortalDeleteNotifyAction(StudioNotifyHelper studioNotifyHelper, DisplayUserSettingsHelper displayUserSettingsHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "portal_delete";

    public override List<Pattern> Patterns
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
}

[Scope]
public sealed class PortalDeleteSuccessV1NotifyAction(StudioNotifyHelper studioNotifyHelper, DisplayUserSettingsHelper displayUserSettingsHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "portal_delete_success_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_delete_success_v1, () => WebstudioNotifyPatternResource.pattern_portal_delete_success_v1)
        ];
    }

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
}

[Scope]
public sealed class PortalDeletedToSupportNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "portal_deleted_to_support";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_deleted_to_support, () => WebstudioNotifyPatternResource.pattern_portal_deleted_to_support)
        ];
    }

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
public sealed class ProfileDeleteNotifyAction(CommonLinkUtility commonLinkUtility, AuthContext authContext, IUrlShortener urlShortener, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "profile_delete";

    public override List<Pattern> Patterns
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
}

[Scope]
public sealed class ProfileHasDeletedItselfNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "profile_has_deleted_itself";

    public override List<Pattern> Patterns
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

[Scope]
public sealed class ReassignsCompletedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "reassigns_completed";

    public override List<Pattern> Patterns
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

[Scope]
public sealed class ReassignsFailedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "reassigns_failed";

    public override List<Pattern> Patterns
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

[Scope]
public sealed class RemoveUserDataCompletedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "remove_user_data_completed";

    public override List<Pattern> Patterns
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

[Scope]
public sealed class RemoveUserDataCompletedCustomModeNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "remove_user_data_completed_custom_mode";

    public override List<Pattern> Patterns
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
public sealed class RemoveUserDataFailedNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "remove_user_data_failed";

    public override List<Pattern> Patterns
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
public sealed class ConfirmOwnerChangeNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "owner_confirm_change";

    public override List<Pattern> Patterns
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
public sealed class ActivateEmailNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "activate_email";

    public override List<Pattern> Patterns
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
}

[Scope]
public sealed class EmailChangeV115NotifyAction(CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "change_email_v115";

    public override List<Pattern> Patterns
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
}

[Scope]
public sealed class UserMessageToAdminNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "for_admin_notify";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_for_admin_notify, () => WebstudioNotifyPatternResource.pattern_for_admin_notify),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_for_admin_notify_tg)
        ];
    }

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
}

[Scope]
public sealed class UserMessageToSalesNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "for_sales_notify";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_for_sales_notify, () => WebstudioNotifyPatternResource.pattern_for_sales_notify)
        ];
    }

    public void Init(string email, string userName, string message)
    {
        Tags = [

            new TagValue(CommonTags.Body, message),
            new TagValue(CommonTags.UserEmail, email),
            new TagValue(CommonTags.UserName, userName)
        ];
    }
}

[Scope]
public sealed class PasswordChangeV115NotifyAction(CommonLinkUtility commonLinkUtility, TenantManager tenantManager, IUrlShortener urlShortener) : NotifyAction(tenantManager)
{
    public override string ID => "change_password_v115";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_password_v115, () => WebstudioNotifyPatternResource.pattern_change_password_v115)
        ];
    }

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
}

[Scope]
public sealed class PasswordChangedNotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "password_changed";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_password_changed, () => WebstudioNotifyPatternResource.pattern_password_changed)
        ];
    }

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
}

[Scope]
public sealed class PasswordSetNotifyAction(CommonLinkUtility commonLinkUtility, TenantManager tenantManager, IUrlShortener urlShortener) : NotifyAction(tenantManager)
{
    public override string ID => "set_password";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_set_password, () => WebstudioNotifyPatternResource.pattern_set_password)
        ];
    }

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
}

[Scope]
public sealed class PhoneChangeNotifyAction(CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "change_phone";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_phone, () => WebstudioNotifyPatternResource.pattern_change_phone)
        ];
    }

    public async Task Init(UserInfo userInfo)
    {
        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(userInfo.Email.ToLower(), ConfirmType.PhoneActivation);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangePhone", GetCulture(userInfo));

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl))
        ];
    }
}

[Scope]
public sealed class TfaChangeNotifyAction(CommonLinkUtility commonLinkUtility, TenantManager tenantManager)  : NotifyAction(tenantManager)
{
    public override string ID => "change_tfa";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_change_tfa, () => WebstudioNotifyPatternResource.pattern_change_tfa)
        ];
    }

    public void Init(UserInfo userInfo)
    {
        var confirmationUrl = commonLinkUtility.GetFullAbsolutePath(string.Empty);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangeTfa", GetCulture(userInfo));

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl)
        ];
    }
}

[Scope]
public sealed class UserHasJoinNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "user_has_join";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_has_join, () => WebstudioNotifyPatternResource.pattern_user_has_join),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_user_has_join_tg)
        ];
    }
}

[Scope]
public sealed class JoinUsersNotifyAction(CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager)  : NotifyAction(tenantManager)
{
    public override string ID => "join";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_join, () => WebstudioNotifyPatternResource.pattern_join),
            new JabberPattern(() => WebstudioNotifyPatternResource.pattern_join)
        ];
    }

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

[Scope]
public sealed class MigrationPortalStartNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "migration_start";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_start, () => WebstudioNotifyPatternResource.pattern_migration_start)
        ];
    }

    public void Init(string region)
    {
        Tags =
        [
            new TagValue(CommonTags.RegionName, TransferResourceHelper.GetRegionDescription(region)),
            new TagValue(CommonTags.PortalUrl, string.Empty)
        ];
    }
}

[Scope]
public sealed class MigrationPortalErrorNotifyAction(CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "migration_error";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_error, () => WebstudioNotifyPatternResource.pattern_migration_error)
        ];
    }

    public void Init(string region, string url)
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

        Tags = args;
    }
}

[Scope]
public sealed class MigrationPortalServerFailureNotifyAction(CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "migration_server_failure";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_migration_error, () => WebstudioNotifyPatternResource.pattern_migration_server_failure)
        ];
    }

    public void Init(string region, string url)
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

        Tags = args;
    }
}

[Scope]
public sealed class PortalRenameNotifyAction(DisplayUserSettingsHelper displayUserSettingsHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "portal_rename";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_portal_rename, () => WebstudioNotifyPatternResource.pattern_portal_rename)
        ];
    }

    public void Init(UserInfo u, string oldVirtualRootPath)
    {
        Tags = [
            new TagValue(CommonTags.PortalUrl, oldVirtualRootPath),
            new TagValue(CommonTags.UserDisplayName, u.DisplayUserName(displayUserSettingsHelper))
        ];
    }
}

[Scope]
public sealed class SaasGuestActivationV115NotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : NotifyAction(tenantManager)
{
    public override string ID => "saas_guest_activation_v115";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_user_activation_v1)
        ];
    }

    public async Task Init(UserInfo newUserInfo)
    {
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Footer, "social")
        ];
    }

    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

[Scope]
public sealed class EnterpriseGuestActivationV10NotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_guest_activation_v10";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_user_activation_v1)
        ];
    }

    public async Task Init(UserInfo newUserInfo)
    {
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Footer, null)
        ];
    }

    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

[Scope]
public sealed class EnterpriseWhitelabelGuestActivationV10NotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_whitelabel_guest_activation_v10";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_user_activation_v1)
        ];
    }

    public async Task Init(UserInfo newUserInfo)
    {
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Footer, null)
        ];
    }

    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

[Scope]
public sealed class OpensourceGuestActivationV11NotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener) : NotifyAction(tenantManager)
{
    public override string ID => "opensource_guest_activation_v11";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_user_activation_v1)
        ];
    }

    public async Task Init(UserInfo newUserInfo)
    {
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Footer, "opensource")
        ];
    }

    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

[Scope]
public sealed class SaasGuestWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_guest_welcome_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_user_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, "social")
        ];
    }
}

[Scope]
public sealed class EnterpriseGuestWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_guest_welcome_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_user_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, null)
        ];
    }
}

[Scope]
public sealed class EnterpriseWhitelabelGuestWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_whitelabel_guest_welcome_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_user_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, null)
        ];
    }
}

[Scope]
public sealed class OpensourceGuestWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "opensource_guest_welcome_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_user_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, "opensource")
        ];
    }
}

[Scope]
public sealed class SaasCustomModeRegDataNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_custom_mode_reg_data";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => CustomModeResource.subject_saas_custom_mode_reg_data, () => CustomModeResource.pattern_saas_custom_mode_reg_data)
        ];
    }

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

[Scope]
public sealed class StorageEncryptionStartNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "storage_encryption_start";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_encryption_start, () => WebstudioNotifyPatternResource.pattern_storage_encryption_start)
        ];
    }

    public void Init(UserInfo u, string serverRootPath)
    {
        Tags = [
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.PortalUrl, serverRootPath)
        ];
    }
}

[Scope]
public sealed class StorageEncryptionSuccessNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "storage_encryption_success";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_encryption_success, () => WebstudioNotifyPatternResource.pattern_storage_encryption_success)
        ];
    }

    public void Init(UserInfo u, string serverRootPath)
    {
        Tags = [
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.PortalUrl, serverRootPath)
        ];
    }
}

[Scope]
public sealed class StorageEncryptionErrorNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "storage_encryption_error";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_encryption_error, () => WebstudioNotifyPatternResource.pattern_storage_encryption_error)
        ];
    }

    public void Init(UserInfo u, string serverRootPath)
    {
        Tags = [
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.PortalUrl, serverRootPath)
        ];
    }
}

[Scope]
public sealed class StorageDecryptionStartNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "storage_decryption_start";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_decryption_start, () => WebstudioNotifyPatternResource.pattern_storage_decryption_start)
        ];
    }

    public void Init(UserInfo u, string serverRootPath)
    {
        Tags = [
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.PortalUrl, serverRootPath)
        ];
    }
}

[Scope]
public sealed class StorageDecryptionSuccessNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "storage_decryption_success";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_decryption_success, () => WebstudioNotifyPatternResource.pattern_storage_decryption_success)
        ];
    }

    public void Init(UserInfo u, string serverRootPath)
    {
        Tags = [
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.PortalUrl, serverRootPath)
        ];
    }
}

[Scope]
public sealed class StorageDecryptionErrorNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "storage_decryption_error";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_storage_decryption_error, () => WebstudioNotifyPatternResource.pattern_storage_decryption_error)
        ];
    }

    public void Init(UserInfo u, string serverRootPath)
    {
        Tags = [
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.PortalUrl, serverRootPath)
        ];
    }
}

[Scope]
public sealed class SaasRoomInviteNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_room_invite";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_room_invite, () => WebstudioNotifyPatternResource.pattern_saas_room_invite)
        ];
    }

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
}

[Scope]
public sealed class SaasAgentInviteNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_agent_invite";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_agent_invite, () => WebstudioNotifyPatternResource.pattern_saas_agent_invite)
        ];
    }

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
}

[Scope]
public sealed class SaasRoomInviteExistingUserNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_room_invite_existing_user";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_room_invite_existing_user, () => WebstudioNotifyPatternResource.pattern_saas_room_invite_existing_user)
        ];
    }

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
}

[Scope]
public sealed class SaasAgentInviteExistingUserNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_agent_invite_existing_user";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_agent_invite_existing_user, () => WebstudioNotifyPatternResource.pattern_saas_agent_invite_existing_user)
        ];
    }

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
}

[Scope]
public sealed class SaasDocSpaceInviteNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_docspace_invite";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_docspace_invite, () => WebstudioNotifyPatternResource.pattern_saas_docspace_invite)
        ];
    }

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
}

[Scope]
public sealed class SaasDocSpaceRegistrationNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_docspace_registration";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_docspace_registration, () => WebstudioNotifyPatternResource.pattern_saas_docspace_registration)
        ];
    }

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
}

[Scope]
public sealed class SaasAdminActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_admin_activation_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_activation_v1)
        ];
    }

    public async Task Init(UserInfo u, DateTime? auditPasswordChangeEventDate)
    {
        var culture = GetCulture(u);

        ITagValue orangeButton = new TagValue("OrangeButton", "");
        ITagValue orangeButtonPwd = new TagValue("OrangeButtonPwd", "");

        if (u.ActivationStatus != EmployeeActivationStatus.Activated)
        {
            var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(u.Email, ConfirmType.EmailActivation, null, u.Id);
            var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirm", culture);
            orangeButton = TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl));
        }
        else if (auditPasswordChangeEventDate.HasValue)
        {
            var hash = auditPasswordChangeEventDate.Value.ToString("s", CultureInfo.InvariantCulture);

            var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(u.Email, ConfirmType.PasswordChange, hash, u.Id);
            var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangePassword", culture);
            orangeButtonPwd = TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl), "OrangeButtonPwd");
        }

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags = [
            new TagValue(CommonTags.UserEmail, u.Email),
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            orangeButton,
            orangeButtonPwd,
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, "common"),
            new TagValue(CommonTags.Culture, culture.Name)
        ];
    }
}

[Scope]
public sealed class EnterpriseAdminActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_admin_activation_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_admin_activation_v1)
        ];
    }

    public async Task Init(UserInfo u)
    {
        var culture = GetCulture(u);

        ITagValue orangeButton = new TagValue("OrangeButton", "");

        if (u.ActivationStatus != EmployeeActivationStatus.Activated)
        {
            var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(u.Email, ConfirmType.EmailActivation, null, u.Id);
            var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirm", culture);
            orangeButton = TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl));
        }

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserEmail, u.Email),
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            orangeButton,
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, null),
            new TagValue(CommonTags.Culture, culture.Name)
        ];
    }
}

[Scope]
public sealed class EnterpriseWhitelabelAdminActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_whitelabel_admin_activation_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_admin_activation_v1)
        ];
    }

    public async Task Init(UserInfo u)
    {
        var culture = GetCulture(u);

        ITagValue orangeButton = new TagValue("OrangeButton", "");

        if (u.ActivationStatus != EmployeeActivationStatus.Activated)
        {
            var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(u.Email, ConfirmType.EmailActivation, null, u.Id);
            var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirm", culture);
            orangeButton = TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl));
        }

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserEmail, u.Email),
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            orangeButton,
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, null),
            new TagValue(CommonTags.Culture, culture.Name)
        ];
    }
}

[Scope]
public sealed class OpensourceAdminActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, IUrlShortener urlShortener, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "opensource_admin_activation_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_admin_activation_v1, () => WebstudioNotifyPatternResource.pattern_admin_activation_v1)
        ];
    }

    public async Task Init(UserInfo u)
    {
        var culture = GetCulture(u);

        ITagValue orangeButton = new TagValue("OrangeButton", "");

        if (u.ActivationStatus != EmployeeActivationStatus.Activated)
        {
            var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(u.Email, ConfirmType.EmailActivation, null, u.Id);
            var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirm", culture);
            orangeButton = TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl));
        }

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserEmail, u.Email),
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            orangeButton,
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, "opensource"),
            new TagValue(CommonTags.Culture, culture.Name)
        ];
    }
}

[Scope]
public sealed class SaasAdminWelcomeV1NotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_admin_welcome_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_welcome_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonUpgrade", culture);

        Tags = [
            new TagValue(CommonTags.Footer, "common"),
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            new TagValue(CommonTags.PricingPage, commonLinkUtility.GetFullAbsolutePath("~/billing/overview")),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~/billing/overview")),
            TagValues.TrulyYours(studioNotifyHelper, WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", GetCulture(newUserInfo)), true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("discover_business_subscription.gif"))
        ];
    }
}




/// <summary>Six months after a paid tariff lapsed: the last word before the portal is deleted.</summary>
[Scope]
public sealed class SaasAdminWarningAfterHalfYearV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_warning_after_half_year_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_warning_after_half_year_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_warning_after_half_year_v1)
        ];
    }

    protected override bool ToOwner => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(!context.Quota.Free && context.Tariff.State == TariffState.NotPaid
            && context.DueDateIsNotMax && context.DueDate.AddMonths(6) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonLeaveFeedback", culture), externalResources.Site.GetRegionalFullEntry("registrationcanceled", culture)));
        tags.Add(new TagValue("URL1", externalResources.Common.GetRegionalFullEntry("legalterms", culture)));
        tags.Add(new TagValue(CommonTags.TopGif, NotifyHelper.GetNotificationImageUrl("docspace_deleted.gif")));

        return Task.CompletedTask;
    }
}

/// <summary>Three months after a paid tariff lapsed: the portal is still there, but not for long.</summary>
[Scope]
public sealed class SaasAdminWarningAfterThreeMonthsV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_warning_after_three_months_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_warning_after_three_months_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_warning_after_three_months_v1)
        ];
    }

    protected override bool ToOwner => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(!context.Quota.Free && context.Tariff.State == TariffState.NotPaid
            && context.DueDateIsNotMax && context.DueDate.AddMonths(3) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonLogIn", culture), commonLinkUtility.GetFullAbsolutePath("~/dashboard")));
        tags.Add(new TagValue("URL1", externalResources.Common.GetRegionalFullEntry("legalterms", culture)));
        tags.Add(new TagValue(CommonTags.TopGif, NotifyHelper.GetNotificationImageUrl("docspace_deleted.gif")));

        return Task.CompletedTask;
    }
}

/// <summary>A free portal nobody has touched for six months: the last warning, a week before it is deleted.</summary>
[Scope]
public sealed class SaasAdminStartupWarningAfterHalfYearV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_startup_warning_after_half_year_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_startup_warning_after_half_year_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_startup_warning_after_half_year_v1)
        ];
    }

    protected override bool ToOwner => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override async Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        if (!context.Quota.Free || context.NowDate < context.UnusedPortalNotifyFrom || !context.IsCreationAnniversary())
        {
            return false;
        }

        var lastActivity = await context.GetLastActivityDateAsync();

        return lastActivity.AddMonths(6) <= context.NowDate && lastActivity.AddMonths(7) > context.NowDate;
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonLeaveFeedback", culture), externalResources.Site.GetRegionalFullEntry("registrationcanceled", culture)));
        tags.Add(new TagValue("URL1", externalResources.Common.GetRegionalFullEntry("legalterms", culture)));
        tags.Add(new TagValue(CommonTags.TopGif, NotifyHelper.GetNotificationImageUrl("docspace_deleted.gif")));

        return Task.CompletedTask;
    }
}


[Scope]
public sealed class SaasUserWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_user_welcome_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_user_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, "social")
        ];
    }
}

[Scope]
public sealed class EnterpriseUserWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_user_welcome_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_user_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, null)
        ];
    }
}

[Scope]
public sealed class EnterpriseWhitelabelUserWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_whitelabel_user_welcome_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_user_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, null)
        ];
    }
}

[Scope]
public sealed class EnterpriseWhitelabelUserWelcomeCustomModeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_whitelabel_user_welcome_custom_mode_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_user_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, null)
        ];
    }
}

[Scope]
public sealed class OpensourceUserWelcomeV1NotifyAction(StudioNotifyHelper studioNotifyHelper, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "opensource_user_welcome_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_welcome_v1, () => WebstudioNotifyPatternResource.pattern_user_welcome_v1)
        ];
    }

    public void Init(UserInfo newUserInfo)
    {
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, newUserInfo.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, "opensource")
        ];
    }
}

[Scope]
public sealed class SaasUserActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, IUrlShortener urlShortener, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "saas_user_activation_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_user_activation_v1)
        ];
    }

    public async Task Init(UserInfo newUserInfo)
    {
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Footer, "social")
        ];
    }

    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

[Scope]
public sealed class EnterpriseUserActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, IUrlShortener urlShortener, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_user_activation_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_user_activation_v1)
        ];
    }

    public async Task Init(UserInfo newUserInfo)
    {
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Footer, null)
        ];
    }

    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

[Scope]
public sealed class EnterpriseWhitelabelUserActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, IUrlShortener urlShortener, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "enterprise_whitelabel_user_activation_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_user_activation_v1)
        ];
    }

    public async Task Init(UserInfo newUserInfo)
    {
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Footer, null)
        ];
    }

    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

[Scope]
public sealed class OpensourceUserActivationV1NotifyAction(StudioNotifyHelper studioNotifyHelper, IUrlShortener urlShortener, CommonLinkUtility commonLinkUtility, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "opensource_user_activation_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_activation_v1, () => WebstudioNotifyPatternResource.pattern_user_activation_v1)
        ];
    }

    public async Task Init(UserInfo newUserInfo)
    {
        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Footer, "opensource")
        ];
    }

    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }
}

/// <summary>
/// A letter the daily tariff job may send. Each subclass answers three questions about itself — when it
/// goes out, to whom, and what it says — and the base owns everything those answers have in common.
///
/// Before this, all three lived in one <c>else if</c> chain in <c>StudioPeriodicNotify</c> that filled
/// forty shared locals and poured them into a thirty-eight parameter <c>Init</c>, so every letter
/// carried the union of every tag any letter might want.
/// </summary>
public abstract class BasePeriodicNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager manager) : NotifyAction(manager)
{
    /// <summary>
    /// The image helper, for the letters that carry a top picture or app icons. Exposed because it is
    /// the tenant that decides where notification images live.
    /// </summary>
    protected StudioNotifyHelper NotifyHelper { get; } = studioNotifyHelper;

    /// <summary>Billing, for the letters that quote how long the grace period lasts.</summary>
    protected ITariffService TariffService { get; } = tariffService;

    /// <summary>Is today this letter's day for this portal?</summary>
    /// <remarks>
    /// Letters judge themselves independently, so a predicate must carry every condition its old branch
    /// inherited from the <c>if</c> it was nested in — the tariff state, the free quota, the trial.
    /// </remarks>
    public abstract Task<bool> ShouldSendAsync(PeriodicLetterContext context);

    protected virtual bool ToAdmins => false;

    protected virtual bool ToUsers => false;

    protected virtual bool ToGuests => false;

    /// <summary>Whether the portal owner is added to the recipients, on top of the groups above.</summary>
    protected virtual bool ToOwner => false;

    /// <summary>Whether whoever pays the bills is added, when billing knows of someone.</summary>
    protected virtual bool ToPayer => false;

    /// <summary>
    /// Whether the recipient's "Tips and Tricks" subscription is honoured. Payment notices go out
    /// whatever the recipient has switched off, which is why this is false by default.
    /// </summary>
    protected virtual bool RequiresSubscription => false;

    /// <summary>
    /// The tags this letter needs on top of <see cref="BuildCommonTagsAsync"/> — its buttons, links and
    /// images. Only the tags its own pattern references: an unused tag is dead weight, and a missing one
    /// leaves a raw <c>$URL1</c> in front of the reader.
    /// </summary>
    protected abstract Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags);

    /// <summary>
    /// Delivers the letter to everyone who should get it. Shared by every periodic letter: the recipient
    /// groups, the owner and payer, the subscription filter, the recipient's culture, and the tags.
    /// </summary>
    public async Task SendAsync(PeriodicLetterContext context, INotifyClient client, string senderName)
    {
        foreach (var user in await GetRecipientsAsync(context))
        {
            if (RequiresSubscription && !await NotifyHelper.IsSubscribedToNotifyAsync(user, periodicNotifyAction))
            {
                continue;
            }

            // Carried on the letter as the Culture tag rather than set on the thread. Nothing here
            // renders: SendNoticeToAsync queues the request, and NotifyEngine re-establishes the culture
            // from NotifyRequest.GetCulture - which reads that very tag - before it resolves the pattern.
            // The tags below take their culture as an argument, and so does everything they call.
            var culture = string.IsNullOrEmpty(user.CultureName) ? context.Tenant.GetCulture() : user.GetCulture();

            Tags = await BuildTagsAsync(context, user, culture);

            await client.SendNoticeToAsync(this, user, senderName);
        }
    }

    /// <summary>
    /// Everything this letter substitutes for one recipient: the tags every periodic letter carries plus
    /// the ones the subclass adds. Public because it is also the only honest way to ask a letter what it
    /// would say without sending it - a letter test renders exactly these tags instead of restating them.
    /// </summary>
    public async Task<List<ITagValue>> BuildTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture)
    {
        var tags = await BuildCommonTagsAsync(context, user, culture);

        await AddTagsAsync(context, user, culture, tags);

        return tags;
    }

    /// <summary>
    /// Who gets this letter: the groups the subclass asked for, plus the owner and the payer when it
    /// wants them. Both extras are appended without duplicates - the owner is usually an admin already.
    /// </summary>
    private async Task<IEnumerable<UserInfo>> GetRecipientsAsync(PeriodicLetterContext context)
    {
        var users = await NotifyHelper.GetRecipientsAsync(ToAdmins, ToUsers, ToGuests);

        if (ToOwner)
        {
            users = users.Append(await userManager.GetUsersAsync(context.Tenant.OwnerId)).DistinctBy(u => u.Id);
        }

        if (ToPayer)
        {
            var customerInfo = await TariffService.GetCustomerInfoAsync(context.Tenant.Id);
            var payer = await userManager.GetUserByEmailAsync(customerInfo?.Email);

            if (payer.Id != ASC.Core.Users.Constants.LostUser.Id && users.All(u => u.Id != payer.Id))
            {
                users = users.Concat([payer]);
            }
        }

        return users;
    }

    /// <summary>
    /// What every periodic letter carries: the culture, who it greets, the signature and the footer
    /// flavour. The last two are read by the styler rather than by the pattern text, which is why every
    /// letter needs them whether or not it mentions them.
    /// </summary>
    protected virtual async Task<List<ITagValue>> BuildCommonTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture)
    {
        return
        [
            new TagValue(CommonTags.Culture, culture.Name),
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            TagValues.TrulyYours(NotifyHelper, Resource("TrulyYoursText", culture), TrulyYoursAsTableRow),
            new TagValue(CommonTags.Footer, await userManager.IsDocSpaceAdminAsync(user) ? "common" : "social")
        ];
    }

    /// <summary>
    /// Whether the signature is a table row of its own. True for the HTML letters, false for the plain
    /// textile ones, where it follows the last paragraph.
    /// </summary>
    protected virtual bool TrulyYoursAsTableRow => false;

    /// <summary>A caption or a line of text from the letter resources, in the recipient's culture.</summary>
    protected static string Resource(string key, CultureInfo culture)
    {
        return WebstudioNotifyPatternResource.ResourceManager.GetString(key, culture);
    }
}

/// <summary>Four days after registration: the paid add-ons.</summary>
[Scope]
public sealed class SaasAdminAddonsV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_addons_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_addons_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_addons_v1)
        ];
    }

    protected override bool ToAdmins => true;
    protected override bool ToOwner => true;
    protected override bool RequiresSubscription => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.CreatedDate.AddDays(4) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonGetStarted", culture), commonLinkUtility.GetFullAbsolutePath("~/billing/overview")));
        tags.Add(new TagValue("URL1", commonLinkUtility.GetFullAbsolutePath("~/billing/overview")));
        tags.Add(new TagValue("URL2", commonLinkUtility.GetFullAbsolutePath("~/billing/wallet")));

        return Task.CompletedTask;
    }
}

/// <summary>Seven days after registration: the AI agents.</summary>
[Scope]
public sealed class SaasAdminAiAgentsV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_ai_agents_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_ai_agents_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_ai_agents_v1)
        ];
    }

    protected override bool ToAdmins => true;
    protected override bool ToOwner => true;
    protected override bool RequiresSubscription => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.CreatedDate.AddDays(7) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonActivateAiFeatures", culture), commonLinkUtility.GetFullAbsolutePath("~/portal-settings/ai-settings/ai-models")));

        return Task.CompletedTask;
    }
}

/// <summary>Three days after registration: settings worth configuring.</summary>
[Scope]
public sealed class SaasAdminConfigureV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_configure_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_configure_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_configure_v1)
        ];
    }

    protected override bool ToAdmins => true;
    protected override bool ToOwner => true;
    protected override bool RequiresSubscription => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.CreatedDate.AddDays(3) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonConfigureRightNow", culture), commonLinkUtility.GetFullAbsolutePath("~/portal-settings")));
        tags.Add(new TagValue(CommonTags.TopGif, NotifyHelper.GetNotificationImageUrl("configure_docspace.gif")));
        tags.Add(new TagValue("URL1", externalResources.Helpcenter.GetRegionalDomain(culture)));
        tags.Add(new TagValue("URL2", commonLinkUtility.GetFullAbsolutePath("~/billing/tariff-plan")));

        return Task.CompletedTask;
    }
}

/// <summary>Ten days after registration: the developer tools.</summary>
[Scope]
public sealed class SaasAdminDeveloperToolsV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_developer_tools_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_developer_tools_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_developer_tools_v1)
        ];
    }

    protected override bool ToAdmins => true;
    protected override bool ToOwner => true;
    protected override bool RequiresSubscription => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.CreatedDate.AddDays(10) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonGetStarted", culture), commonLinkUtility.GetFullAbsolutePath("~/developer-tools/overview")));
        tags.Add(new TagValue("URL1", externalResources.Site.GetRegionalFullEntry("allconnectors", culture)));
        tags.Add(new TagValue("URL2", externalResources.Api.GetRegionalDomain(culture)));

        return Task.CompletedTask;
    }
}

/// <summary>Two days after registration: four apps worth knowing about.</summary>
[Scope]
public sealed class SaasAdminHandyAppsV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_handy_apps_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_handy_apps_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_handy_apps_v1)
        ];
    }

    protected override bool ToAdmins => true;
    protected override bool ToOwner => true;
    protected override bool RequiresSubscription => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.CreatedDate.AddDays(2) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonGoToDocSpace", culture), commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')));

        return Task.CompletedTask;
    }
}

/// <summary>A free portal nobody has touched for three months: the first of two warnings before it is
/// deleted.</summary>
[Scope]
public sealed class SaasAdminStartupWarningAfterThreeMonthsV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_startup_warning_after_three_months_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_startup_warning_after_three_months_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_startup_warning_after_three_months_v1)
        ];
    }

    protected override bool ToOwner => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override async Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        if (!context.Quota.Free || context.NowDate < context.UnusedPortalNotifyFrom || !context.IsCreationAnniversary())
        {
            return false;
        }

        var lastActivity = await context.GetLastActivityDateAsync();

        return lastActivity.AddMonths(3) <= context.NowDate && lastActivity.AddMonths(4) > context.NowDate;
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonLogIn", culture), commonLinkUtility.GetFullAbsolutePath("~/dashboard")));
        tags.Add(new TagValue(CommonTags.TopGif, NotifyHelper.GetNotificationImageUrl("docspace_deleted.gif")));

        return Task.CompletedTask;
    }
}

/// <summary>Fourteen days after registration: the desktop and mobile apps. Goes to everyone who works
/// in the portal, not just its administrators.</summary>
[Scope]
public sealed class SaasAdminUserAppsTipsV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_admin_user_apps_tips_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_admin_user_apps_tips_v1, () => WebstudioNotifyPatternResource.pattern_saas_admin_user_apps_tips_v1)
        ];
    }

    protected override bool ToAdmins => true;
    protected override bool ToUsers => true;
    protected override bool RequiresSubscription => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.CreatedDate.AddDays(14) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(new TagValue(CommonTags.TopGif, NotifyHelper.GetNotificationImageUrl("free_apps.gif")));
        tags.Add(new TagValue("IMG1", NotifyHelper.GetNotificationImageUrl("windows.png")));
        tags.Add(new TagValue("IMG2", NotifyHelper.GetNotificationImageUrl("apple.png")));
        tags.Add(new TagValue("IMG3", NotifyHelper.GetNotificationImageUrl("linux.png")));
        tags.Add(new TagValue("IMG4", NotifyHelper.GetNotificationImageUrl("android.png")));
        tags.Add(new TagValue("URL1", externalResources.Site.GetRegionalFullEntry("downloaddesktop", culture)));
        tags.Add(new TagValue("URL2", externalResources.Site.GetRegionalFullEntry("downloadmobile", culture)));

        return Task.CompletedTask;
    }
}

/// <summary>Fourteen days into an Enterprise trial: the desktop and mobile apps. Only on a portal that
/// still carries our branding — a white-labelled one must not advertise our apps.</summary>
[Scope]
public sealed class EnterpriseAdminUserAppsTipsV1NotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "enterprise_admin_user_apps_tips_v1";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_user_apps_tips_v1, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_user_apps_tips_v1)
        ];
    }

    protected override bool ToAdmins => true;
    protected override bool ToUsers => true;
    protected override bool RequiresSubscription => true;
    protected override bool TrulyYoursAsTableRow => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Quota.Trial && context.DefaultRebranding
            && context.CreatedDate.AddDays(14) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(new TagValue(CommonTags.TopGif, NotifyHelper.GetNotificationImageUrl("free_apps.gif")));
        tags.Add(new TagValue("IMG1", NotifyHelper.GetNotificationImageUrl("windows.png")));
        tags.Add(new TagValue("IMG2", NotifyHelper.GetNotificationImageUrl("apple.png")));
        tags.Add(new TagValue("IMG3", NotifyHelper.GetNotificationImageUrl("linux.png")));
        tags.Add(new TagValue("IMG4", NotifyHelper.GetNotificationImageUrl("android.png")));
        tags.Add(new TagValue("URL1", externalResources.Site.GetRegionalFullEntry("downloaddesktop", culture)));
        tags.Add(new TagValue("URL2", externalResources.Site.GetRegionalFullEntry("downloadmobile", culture)));

        return Task.CompletedTask;
    }
}

[Scope]
public sealed class RoomsActivityNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "rooms_activity";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_rooms_activity, () => WebstudioNotifyPatternResource.pattern_rooms_activity),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_rooms_activity)
        ];
    }

    public void Init(DateTime scheduleDate, WhatsNewType whatsNewType, HashSet<string> userActivities)
    {
        Tags = [
                new TagValue(CommonTags.Activities, userActivities),
                new TagValue(CommonTags.Date, DateToString(scheduleDate, whatsNewType)),
                new TagValue(CommonTags.Priority, 1)
        ];
    }

    private static string DateToString(DateTime d, WhatsNewType type)
    {
        d = type == WhatsNewType.DailyFeed ? d.AddDays(-1) : d.AddHours(-1);

        return d.ConvertNumerals("M");
    }
}

[Scope]
public sealed class SendWhatsNewNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "send_whats_new";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_send_whats_new, () => WebstudioNotifyPatternResource.pattern_send_whats_new),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_send_whats_new)
        ];
    }

    public void Init(DateTime scheduleDate, WhatsNewType whatsNewType, HashSet<string> userActivities)
    {
        Tags = [
            new TagValue(CommonTags.Activities, userActivities),
            new TagValue(CommonTags.Date, DateToString(scheduleDate, whatsNewType)),
            new TagValue(CommonTags.Priority, 1)
        ];
    }

    private static string DateToString(DateTime d, WhatsNewType type)
    {
        d = type == WhatsNewType.DailyFeed ? d.AddDays(-1) : d.AddHours(-1);

        return d.ConvertNumerals("M");
    }
}

/// <summary>Three days before the paid period ends.</summary>
[Scope]
public sealed class SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_owner_payment_warning_grace_period_before_activation";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_before_activation)
        ];
    }

    protected override bool ToOwner => true;
    protected override bool ToPayer => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(!context.Quota.Free && context.Tariff.State >= TariffState.Paid
            && context.DueDateIsNotMax && context.DueDate.AddDays(-3) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(new TagValue("URL1", commonLinkUtility.GetFullAbsolutePath("~/billing/payment-method")));

        return Task.CompletedTask;
    }
}

/// <summary>The day after the paid period ended and the grace period began.</summary>
[Scope]
public sealed class SaasOwnerPaymentWarningGracePeriodActivationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_owner_payment_warning_grace_period_activation";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_activation)
        ];
    }

    protected override bool ToOwner => true;
    protected override bool ToPayer => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(!context.Quota.Free && context.Tariff.State >= TariffState.Paid
            && context.DueDateIsNotMax && context.DueDate.AddDays(1) == context.NowDate
            && context.DelayDueDateIsNotMax);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonVisitBillingSection", culture), commonLinkUtility.GetFullAbsolutePath("~/billing/overview")));

        tags.Add(new TagValue(CommonTags.PaymentDelay, TariffService.GetPaymentDelay()));

        return Task.CompletedTask;
    }
}

/// <summary>The last day of the grace period.</summary>
[Scope]
public sealed class SaasOwnerPaymentWarningGracePeriodLastDayNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_owner_payment_warning_grace_period_last_day";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_last_day, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_last_day)
        ];
    }

    protected override bool ToOwner => true;
    protected override bool ToPayer => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(!context.Quota.Free && context.Tariff.State == TariffState.Delay
            && context.DelayDueDateIsNotMax && context.DelayDueDate.AddDays(-1) == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonVisitBillingSection", culture), commonLinkUtility.GetFullAbsolutePath("~/billing/overview")));

        tags.Add(new TagValue(CommonTags.PaymentDelay, TariffService.GetPaymentDelay()));

        return Task.CompletedTask;
    }
}

/// <summary>The day the grace period runs out.</summary>
[Scope]
public sealed class SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    CommonLinkUtility commonLinkUtility,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "saas_owner_payment_warning_grace_period_expired";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_saas_owner_payment_warning_grace_period_expired, () => WebstudioNotifyPatternResource.pattern_saas_owner_payment_warning_grace_period_expired)
        ];
    }

    protected override bool ToOwner => true;
    protected override bool ToPayer => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(!context.Quota.Free && context.Tariff.State == TariffState.Delay
            && context.DelayDueDateIsNotMax && context.DelayDueDate == context.NowDate);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonVisitBillingSection", culture), commonLinkUtility.GetFullAbsolutePath("~/billing/overview")));

        tags.Add(new TagValue(CommonTags.PaymentDelay, TariffService.GetPaymentDelay()));

        return Task.CompletedTask;
    }
}

[Scope]
public sealed class ZoomWelcomeNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "zoom_welcome";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_zoom_welcome, () => WebstudioNotifyPatternResource.pattern_zoom_welcome)
        ];
    }

    public void Init(UserInfo u)
    {
        var culture = GetCulture(u);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.Culture, culture.Name),
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}


/// <summary>A week before an Enterprise licence expires.</summary>
[Scope]
public sealed class EnterpriseAdminPaymentWarningGracePeriodBeforeActivationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "enterprise_admin_payment_warning_grace_period_before_activation";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_before_activation)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Paid && context.DueDate.AddDays(-7) == context.NowDate
            && !context.Quota.Lifetime && !context.Quota.Customization);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_expire_7_days"));

        return Task.CompletedTask;
    }
}

/// <summary>The day an Enterprise licence expires and the grace period begins.</summary>
[Scope]
public sealed class EnterpriseAdminPaymentWarningGracePeriodActivationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "enterprise_admin_payment_warning_grace_period_activation";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_activation)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Paid && context.DueDate == context.NowDate
            && !context.Quota.Lifetime && !context.Quota.Customization);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_grace_period"));

        tags.Add(new TagValue(CommonTags.PaymentDelay, TariffService.GetPaymentDelay()));

        return Task.CompletedTask;
    }
}

/// <summary>A week before the grace period of an Enterprise licence runs out.</summary>
[Scope]
public sealed class EnterpriseAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "enterprise_admin_payment_warning_grace_period_before_expiration";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_before_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_before_expiration)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Delay && context.DelayDueDate.AddDays(-7) == context.NowDate
            && !context.Quota.Customization);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_grace_period_expire_soon"));

        return Task.CompletedTask;
    }
}

/// <summary>The day the grace period of an Enterprise licence runs out.</summary>
[Scope]
public sealed class EnterpriseAdminPaymentWarningGracePeriodExpirationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "enterprise_admin_payment_warning_grace_period_expiration";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_grace_period_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_grace_period_expiration)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Delay && context.DelayDueDate == context.NowDate
            && !context.Quota.Customization);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_no_available"));

        return Task.CompletedTask;
    }
}

/// <summary>A week before a lifetime licence stops being supported.</summary>
[Scope]
public sealed class EnterpriseAdminPaymentWarningLifetimeBeforeExpirationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "enterprise_admin_payment_warning_lifetime_before_expiration";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_lifetime_before_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_lifetime_before_expiration)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Paid && context.DueDate.AddDays(-7) == context.NowDate
            && context.Quota.Lifetime);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_expire_7_days"));

        return Task.CompletedTask;
    }
}

/// <summary>The day support for a lifetime licence ends.</summary>
[Scope]
public sealed class EnterpriseAdminPaymentWarningLifetimeExpirationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "enterprise_admin_payment_warning_lifetime_expiration";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_enterprise_admin_payment_warning_lifetime_expiration, () => WebstudioNotifyPatternResource.pattern_enterprise_admin_payment_warning_lifetime_expiration)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Paid && context.DueDate == context.NowDate
            && context.Quota.Lifetime);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_grace_period"));

        return Task.CompletedTask;
    }
}

/// <summary>A week before a Developer licence expires.</summary>
[Scope]
public sealed class DeveloperAdminPaymentWarningGracePeriodBeforeActivationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "developer_admin_payment_warning_grace_period_before_activation";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_before_activation, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_before_activation)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Paid && context.DueDate.AddDays(-7) == context.NowDate
            && !context.Quota.Lifetime && context.Quota.Customization);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_expire_7_days"));

        return Task.CompletedTask;
    }
}

/// <summary>The day a Developer licence expires and the grace period begins.</summary>
[Scope]
public sealed class DeveloperAdminPaymentWarningGracePeriodActivationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "developer_admin_payment_warning_grace_period_activation";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_activation, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_activation)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Paid && context.DueDate == context.NowDate
            && !context.Quota.Lifetime && context.Quota.Customization);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_grace_period"));

        tags.Add(new TagValue(CommonTags.PaymentDelay, TariffService.GetPaymentDelay()));

        return Task.CompletedTask;
    }
}

/// <summary>A week before the grace period of a Developer licence runs out.</summary>
[Scope]
public sealed class DeveloperAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "developer_admin_payment_warning_grace_period_before_expiration";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_before_expiration, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_before_expiration)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Delay && context.DelayDueDate.AddDays(-7) == context.NowDate
            && context.Quota.Customization);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_grace_period_expire_soon"));

        return Task.CompletedTask;
    }
}

/// <summary>The day the grace period of a Developer licence runs out.</summary>
[Scope]
public sealed class DeveloperAdminPaymentWarningGracePeriodExpirationNotifyAction(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    ExternalResourceSettingsHelper externalResources,
    PeriodicNotifyAction periodicNotifyAction,
    TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "developer_admin_payment_warning_grace_period_expiration";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_developer_admin_payment_warning_grace_period_expiration, () => WebstudioNotifyPatternResource.pattern_developer_admin_payment_warning_grace_period_expiration)
        ];
    }

    protected override bool ToAdmins => true;

    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
    {
        return Task.FromResult(context.Tariff.State == TariffState.Delay && context.DelayDueDate == context.NowDate
            && context.Quota.Customization);
    }

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonPurchaseNow", culture),
            externalResources.Site.GetRegionalFullEntry("docspaceprices", culture)
            + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_no_available"));

        return Task.CompletedTask;
    }
}

[Scope]
public sealed class UserTypeChangedNotifyAction(StudioNotifyHelper studioNotifyHelper, ExternalResourceSettingsHelper externalResourceSettingsHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "user_type_changed";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_type_changed, () => WebstudioNotifyPatternResource.pattern_user_type_changed)
        ];
    }

    public void Init(UserInfo u, string userType)
    {
        var culture = GetCulture(u);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue("UserType", userType),
            new TagValue("HelpCenterUrl", externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("accessrights", culture)),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}

[Scope]
public sealed class UserRoleChangedNotifyAction(ExternalResourceSettingsHelper externalResourceSettingsHelper, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "user_role_changed";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_role_changed, () => WebstudioNotifyPatternResource.pattern_user_role_changed)
        ];
    }

    public void Init(UserInfo user, string roomTitle, string roomUrl, string userRole)
    {
        var culture = GetCulture(user);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue("RoomTitle", roomTitle),
            new TagValue("RoomUrl", roomUrl),
            new TagValue("UserRole", userRole),
            new TagValue("HelpCenterUrl", externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("accessrights", culture)),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}

[Scope]
public sealed class UserAgentRoleChangedNotifyAction(ExternalResourceSettingsHelper externalResourceSettingsHelper, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "user_agent_role_changed";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_user_agent_role_changed, () => WebstudioNotifyPatternResource.pattern_user_agent_role_changed)
        ];
    }

    public void Init(UserInfo user, string roomTitle, string roomUrl, string userRole)
    {
        var culture = GetCulture(user);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue("RoomTitle", roomTitle),
            new TagValue("RoomUrl", roomUrl),
            new TagValue("UserRole", userRole),
            new TagValue("HelpCenterUrl", externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("accessrights", culture)),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}

[Scope]
public sealed class TopUpWalletErrorNotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "top_up_wallet_error";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_top_up_wallet_error, () => WebstudioNotifyPatternResource.pattern_top_up_wallet_error),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_top_up_wallet_error)
        ];
    }

    public void Init(UserInfo user)
    {
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGoToWalletSettings", GetCulture(user));
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~/billing/wallet")),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}

[Scope]
public sealed class WalletAutoTopUpUnavailableNotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "wallet_auto_top_up_unavailable";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_wallet_auto_top_up_unavailable, () => WebstudioNotifyPatternResource.pattern_wallet_auto_top_up_unavailable),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_wallet_auto_top_up_unavailable)
        ];
    }

    public void Init(UserInfo user)
    {
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGoToWalletSettings", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~/billing/wallet")),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}

[Scope]
public sealed class LowWalletBalanceNotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "low_wallet_balance";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_low_wallet_balance, () => WebstudioNotifyPatternResource.pattern_low_wallet_balance),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_low_wallet_balance)
        ];
    }

    public void Init(UserInfo user)
    {
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGoToWalletSettings", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~/billing/wallet")),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}

[Scope]
public sealed class UpcomingSubscriptionPaymentNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "upcoming_subscription_payment";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_upcoming_subscription_payment, () => WebstudioNotifyPatternResource.pattern_upcoming_subscription_payment)
        ];
    }

    // subscriptionName gives the subscriptions the upcoming payment covers, joined with a comma and
    // localized for the recipient - e.g. "Additional disk storage, Docs Connect". It lands in the
    // subject as well, so it must stay plain text.
    public void Init(UserInfo user, Func<CultureInfo, string> subscriptionName)
    {
        var culture = GetCulture(user);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Culture, culture.Name),
            new TagValue("SubscriptionName", subscriptionName(culture)),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}

[Scope]
public sealed class RenewSubscriptionErrorNotifyAction(CommonLinkUtility commonLinkUtility, StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "renew_subscription_error";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_renew_subscription_error, () => WebstudioNotifyPatternResource.pattern_renew_subscription_error),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_renew_subscription_error)
        ];
    }

    public void Init(UserInfo user)
    {
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonVisitBillingSection", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~/billing/overview")),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}

[Scope]
public sealed class ApiKeyExpiredNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "api_key_expired";

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_api_key_expired, () => WebstudioNotifyPatternResource.pattern_api_key_expired)
        ];
    }

    public void Init(UserInfo user, string keyName)
    {
        var culture = GetCulture(user);

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        Tags =
        [
            new TagValue(CommonTags.UserName, user.FirstName.HtmlEncode()),
            new TagValue(CommonTags.Message, keyName),
            new TagValue(CommonTags.Culture, culture.Name),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours)
        ];
    }
}

[Scope]
public sealed class AdminNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "admin_notify";

    public override List<Pattern> Patterns
    {
        get =>
        [
            //new EmailPattern("admin_notify", () => WebstudioNotifyPatternResource.subject_admin_notify, () => WebstudioNotifyPatternResource.pattern_admin_notify)
        ];
    }
}

[Scope]
public sealed class SelfProfileUpdatedNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "self_profile_updated";

    public override List<Pattern> Patterns =>
    [
        new EmailPattern(() => WebstudioNotifyPatternResource.subject_self_profile_updated, () => WebstudioNotifyPatternResource.pattern_self_profile_updated),
        new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_self_profile_updated_tg)
    ];
}

[Scope]
public sealed class PeriodicNotifyAction(TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "periodic_notify";

    public override List<Pattern> Patterns
    {
        get =>
        [
            //new EmailPattern("periodic_notify", () => WebstudioNotifyPatternResource.subject_periodic_notify, () => WebstudioNotifyPatternResource.pattern_periodic_notify)
        ];
    }
}
