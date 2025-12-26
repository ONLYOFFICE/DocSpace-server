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

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public class StudioNotifyService(
    IServiceProvider serviceProvider,
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    StudioNotifyServiceHelper studioNotifyServiceHelper,
    TenantExtra tenantExtra,
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings,
    CommonLinkUtility commonLinkUtility,
    ExternalResourceSettingsHelper externalResourceSettingsHelper,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    UserInvitationLimitHelper userInvitationLimitHelper,
    SettingsManager settingsManager,
    MessageService messageService,
    IUrlShortener urlShortener,
    ILoggerProvider option)
{
    public static string EMailSenderName => Constants.NotifyEMailSenderSysName;

    private readonly ILogger _log = option.CreateLogger("ASC.Notify");

    public async Task SendMsgToAdminFromNotAuthUserAsync(string email, string message, string culture)
    {
        var userMessageToAdminNotifyAction = serviceProvider.GetService<UserMessageToAdminNotifyAction>();
        userMessageToAdminNotifyAction.Init(email, message,culture);
        
        await studioNotifyServiceHelper.SendNoticeAsync(userMessageToAdminNotifyAction);
    }

    public async Task SendMsgToSalesAsync(string email, string userName, string message)
    {
        var salesEmail = externalResourceSettingsHelper.Common.GetDefaultRegionalFullEntry("paymentemail");
        
        var userMessageToSalesNotifyAction = serviceProvider.GetService<UserMessageToSalesNotifyAction>();
        userMessageToSalesNotifyAction.Init(email, userName, message);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(userMessageToSalesNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(salesEmail, false), [EMailSenderName]);
    }

    #region User Password

    public async Task UserPasswordChangeAsync(UserInfo userInfo, bool initialPasswordAssignment)
    {
        var auditEventDate = DateTime.UtcNow;

        auditEventDate = new DateTime(
            auditEventDate.Year,
            auditEventDate.Month,
            auditEventDate.Day,
            auditEventDate.Hour,
            auditEventDate.Minute,
            auditEventDate.Second,
            0,
            DateTimeKind.Utc);
        
        var passwordChangeV115NotifyAction = serviceProvider.GetService<PasswordChangeV115NotifyAction>();
        await passwordChangeV115NotifyAction.Init(userInfo, auditEventDate);
        
        var passwordSetNotifyAction = serviceProvider.GetService<PasswordSetNotifyAction>();
        await passwordSetNotifyAction.Init(userInfo, auditEventDate);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(initialPasswordAssignment ? passwordSetNotifyAction : passwordChangeV115NotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false), [EMailSenderName]);

        var displayUserName = userInfo.DisplayUserName(false, displayUserSettingsHelper);

        messageService.Send(MessageAction.UserSentPasswordChangeInstructions, MessageTarget.Create(userInfo.Id), auditEventDate, displayUserName);
    }

    public async Task SendUserPasswordChangedAsync(UserInfo userInfo, AuditEvent auditEvent)
    {
        var passwordChangedNotifyAction = serviceProvider.GetService<PasswordChangedNotifyAction>();
        passwordChangedNotifyAction.Init(userInfo, auditEvent);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(passwordChangedNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false), [EMailSenderName]);
    }

    #endregion

    #region User Email

    public async Task SendEmailChangeInstructionsAsync(UserInfo user, string email)
    {
        var auditEventDate = DateTime.UtcNow;

        auditEventDate = new DateTime(
            auditEventDate.Year,
            auditEventDate.Month,
            auditEventDate.Day,
            auditEventDate.Hour,
            auditEventDate.Minute,
            auditEventDate.Second,
            0,
            DateTimeKind.Utc);
        
        var emailChangeV115NotifyAction = serviceProvider.GetService<EmailChangeV115NotifyAction>();
        await emailChangeV115NotifyAction.Init(user, email, auditEventDate);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(emailChangeV115NotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(email, false), [EMailSenderName]);

        var displayUserName = user.DisplayUserName(false, displayUserSettingsHelper);

        messageService.Send(MessageAction.UserSentEmailChangeInstructions, MessageTarget.Create(user.Id), auditEventDate, displayUserName);
    }

    public async Task SendEmailActivationInstructionsAsync(UserInfo user, string email)
    {
        var activateEmailNotifyAction = serviceProvider.GetService<ActivateEmailNotifyAction>();
        await activateEmailNotifyAction.Init(user, email);

        await studioNotifyServiceHelper.SendNoticeToAsync(activateEmailNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(email, false), [EMailSenderName]);
    }

    public async Task SendEmailRoomInviteAsync(
        string email, 
        string roomTitle, 
        string confirmationUrl,
        bool isAgent,
        string culture = null, 
        bool limitation = false)
    {
        var saasAgentInviteNotifyAction = serviceProvider.GetService<SaasAgentInviteNotifyAction>();
        saasAgentInviteNotifyAction.Init(culture, roomTitle, confirmationUrl);
        
        var saasRoomInviteNotifyAction = serviceProvider.GetService<SaasRoomInviteNotifyAction>();
        saasRoomInviteNotifyAction.Init(culture, roomTitle, confirmationUrl);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(isAgent ? saasAgentInviteNotifyAction : saasRoomInviteNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(email, false), [EMailSenderName]);

        if (limitation)
        {
            await userInvitationLimitHelper.ReduceLimit();
        }
    }

    public async Task SendEmailRoomInviteExistingUserAsync(UserInfo user, string roomTitle, string roomUrl, bool isAgent)
    {
        var saasAgentInviteExistingUserNotifyAction = serviceProvider.GetService<SaasAgentInviteExistingUserNotifyAction>();
        saasAgentInviteExistingUserNotifyAction.Init(user, roomTitle, roomUrl);
        
        var saasRoomInviteExistingUserNotifyAction = serviceProvider.GetService<SaasRoomInviteExistingUserNotifyAction>();
        saasRoomInviteExistingUserNotifyAction.Init(user, roomTitle, roomUrl);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(isAgent ? saasAgentInviteExistingUserNotifyAction : saasRoomInviteExistingUserNotifyAction, [user], [EMailSenderName]);
    }

    public async Task SendDocSpaceInviteAsync(string email, string confirmationUrl, string culture = "", bool limitation = false)
    {
        var saasDocSpaceInviteNotifyAction = serviceProvider.GetService<SaasDocSpaceInviteNotifyAction>();
        saasDocSpaceInviteNotifyAction.Init(confirmationUrl, culture);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(saasDocSpaceInviteNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(email, false), [EMailSenderName]);

        if (limitation)
        {
            await userInvitationLimitHelper.ReduceLimit();
        }
    }

    public async Task SendDocSpaceRegistration(string email, string confirmationUrl, string culture = "", bool limitation = false)
    {
        var saasDocSpaceRegistrationNotifyAction = serviceProvider.GetService<SaasDocSpaceRegistrationNotifyAction>();
        saasDocSpaceRegistrationNotifyAction.Init(confirmationUrl, culture);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(saasDocSpaceRegistrationNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(email, false), [EMailSenderName]);

        if (limitation)
        {
            await userInvitationLimitHelper.ReduceLimit();
        }
    }
    
    #endregion

    public async Task SendMsgMobilePhoneChangeAsync(UserInfo userInfo)
    {
        var phoneChangeNotifyAction = serviceProvider.GetService<PhoneChangeNotifyAction>();
        await phoneChangeNotifyAction.Init(userInfo);

        await studioNotifyServiceHelper.SendNoticeToAsync(phoneChangeNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false), [EMailSenderName]);
    }

    public async Task SendMsgTfaResetAsync(UserInfo userInfo)
    {
        var tfaChangeNotifyAction = serviceProvider.GetService<TfaChangeNotifyAction>();
        tfaChangeNotifyAction.Init(userInfo);

        await studioNotifyServiceHelper.SendNoticeToAsync(tfaChangeNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false), [EMailSenderName]);
    }

    public async ValueTask UserHasJoinAsync()
    {
        var userHasJoinNotifyAction = serviceProvider.GetService<UserHasJoinNotifyAction>();
        
        await studioNotifyServiceHelper.SendNoticeAsync(userHasJoinNotifyAction);
    }

    public async Task SendJoinMsgAsync(string email, EmployeeType emplType, string culture, bool limitation = false)
    {
        var joinUsersNotifyAction = serviceProvider.GetService<JoinUsersNotifyAction>();
        await joinUsersNotifyAction.Init(email, emplType, culture);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(joinUsersNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(email, false), [EMailSenderName]);

        if (limitation)
        {
            await userInvitationLimitHelper.ReduceLimit();
        }
    }

    public async Task UserInfoAddedAfterInviteAsync(UserInfo newUserInfo)
    {
        if (!userManager.UserExists(newUserInfo))
        {
            return;
        }

        INotifyAction notifyAction;

        if (tenantExtra.Enterprise)
        {
            var enterpriseUserWelcomeV1NotifyAction = serviceProvider.GetService<EnterpriseUserWelcomeV1NotifyAction>();
            enterpriseUserWelcomeV1NotifyAction.Init(newUserInfo);
            
            var enterpriseWhitelabelUserWelcomeCustomModeV1NotifyAction = serviceProvider.GetService<EnterpriseWhitelabelUserWelcomeCustomModeV1NotifyAction>();
            enterpriseWhitelabelUserWelcomeCustomModeV1NotifyAction.Init(newUserInfo);
            
            var enterpriseWhitelabelUserWelcomeV1NotifyAction = serviceProvider.GetService<EnterpriseWhitelabelUserWelcomeV1NotifyAction>();
            enterpriseWhitelabelUserWelcomeV1NotifyAction.Init(newUserInfo);
            
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(settingsManager);
            notifyAction = defaultRebranding
                               ? enterpriseUserWelcomeV1NotifyAction
                                   : coreBaseSettings.CustomMode
                                     ? enterpriseWhitelabelUserWelcomeCustomModeV1NotifyAction
                                     : enterpriseWhitelabelUserWelcomeV1NotifyAction;
        }
        else if (tenantExtra.Opensource)
        {
            var opensourceUserWelcomeV1NotifyAction = serviceProvider.GetService<OpensourceUserWelcomeV1NotifyAction>();
            opensourceUserWelcomeV1NotifyAction.Init(newUserInfo);
            
            notifyAction = opensourceUserWelcomeV1NotifyAction;
        }
        else
        {
            var saasUserWelcomeV1NotifyAction = serviceProvider.GetService<SaasUserWelcomeV1NotifyAction>();
            saasUserWelcomeV1NotifyAction.Init(newUserInfo);
            
            notifyAction = saasUserWelcomeV1NotifyAction;
        }
        
        await studioNotifyServiceHelper.SendNoticeToAsync(notifyAction, await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false), [EMailSenderName]);
    }

    public async Task GuestInfoAddedAfterInviteAsync(UserInfo newUserInfo)
    {
        if (!userManager.UserExists(newUserInfo))
        {
            return;
        }

        INotifyAction notifyAction;
        
        if (tenantExtra.Enterprise)
        {
            var enterpriseGuestWelcomeV1NotifyAction = serviceProvider.GetService<EnterpriseGuestWelcomeV1NotifyAction>();
            enterpriseGuestWelcomeV1NotifyAction.Init(newUserInfo);
            
            var enterpriseWhitelabelGuestWelcomeV1NotifyAction = serviceProvider.GetService<EnterpriseWhitelabelGuestWelcomeV1NotifyAction>();
            enterpriseWhitelabelGuestWelcomeV1NotifyAction.Init(newUserInfo);
            
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(settingsManager);
            notifyAction = defaultRebranding ? enterpriseGuestWelcomeV1NotifyAction : enterpriseWhitelabelGuestWelcomeV1NotifyAction;
        }
        else if (tenantExtra.Opensource)
        {
            var opensourceGuestWelcomeV1NotifyAction = serviceProvider.GetService<OpensourceGuestWelcomeV1NotifyAction>();
            opensourceGuestWelcomeV1NotifyAction.Init(newUserInfo);
            
            notifyAction = opensourceGuestWelcomeV1NotifyAction;
        }
        else
        {
            var saasGuestWelcomeV1NotifyAction = serviceProvider.GetService<SaasGuestWelcomeV1NotifyAction>();
            saasGuestWelcomeV1NotifyAction.Init(newUserInfo);
            
            notifyAction = saasGuestWelcomeV1NotifyAction;
        }
        
        await studioNotifyServiceHelper.SendNoticeToAsync(notifyAction, await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false), [EMailSenderName]);
    }

    public async Task UserInfoActivationAsync(UserInfo newUserInfo)
    {
        if (newUserInfo.IsActive)
        {
            throw new ArgumentException("User is already activated!");
        }

        INotifyAction notifyAction;

        if (tenantExtra.Enterprise)
        {            
            var enterpriseUserActivationV1NotifyAction = serviceProvider.GetService<EnterpriseUserActivationV1NotifyAction>();
            await enterpriseUserActivationV1NotifyAction.Init(newUserInfo);
            
            var enterpriseWhitelabelUserActivationV1NotifyAction = serviceProvider.GetService<EnterpriseWhitelabelUserActivationV1NotifyAction>();
            await enterpriseWhitelabelUserActivationV1NotifyAction.Init(newUserInfo);
            
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(settingsManager);
            notifyAction = defaultRebranding ? enterpriseUserActivationV1NotifyAction : enterpriseWhitelabelUserActivationV1NotifyAction;
        }
        else if (tenantExtra.Opensource)
        {
            var opensourceUserActivationV1NotifyAction = serviceProvider.GetService<OpensourceUserActivationV1NotifyAction>();
            await opensourceUserActivationV1NotifyAction.Init(newUserInfo);
            notifyAction = opensourceUserActivationV1NotifyAction;
        }
        else
        {            
            var saasUserActivationV1NotifyAction = serviceProvider.GetService<SaasUserActivationV1NotifyAction>();
            await saasUserActivationV1NotifyAction.Init(newUserInfo);
            notifyAction = saasUserActivationV1NotifyAction;
        }

        await studioNotifyServiceHelper.SendNoticeToAsync(notifyAction, await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false), [EMailSenderName]);
    }

    public async Task GuestInfoActivationAsync(UserInfo newUserInfo)
    {
        if (newUserInfo.IsActive)
        {
            throw new ArgumentException("User is already activated!");
        }

        INotifyAction notifyAction;
        
        if (tenantExtra.Enterprise)
        {
            var enterpriseGuestActivationV10NotifyAction = serviceProvider.GetService<EnterpriseGuestActivationV10NotifyAction>();
            await enterpriseGuestActivationV10NotifyAction.Init(newUserInfo);
            
            var enterpriseWhitelabelGuestActivationV10NotifyAction = serviceProvider.GetService<EnterpriseWhitelabelGuestActivationV10NotifyAction>();
            await enterpriseWhitelabelGuestActivationV10NotifyAction.Init(newUserInfo);

            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(settingsManager);
            notifyAction = defaultRebranding ? enterpriseGuestActivationV10NotifyAction : enterpriseWhitelabelGuestActivationV10NotifyAction;
        }
        else if (tenantExtra.Opensource)
        {            
            var opensourceGuestActivationV11NotifyAction = serviceProvider.GetService<OpensourceGuestActivationV11NotifyAction>();
            await opensourceGuestActivationV11NotifyAction.Init(newUserInfo);
            notifyAction = opensourceGuestActivationV11NotifyAction;
        }
        else
        {
            var saasGuestActivationV115NotifyAction = serviceProvider.GetService<SaasGuestActivationV115NotifyAction>();
            await saasGuestActivationV115NotifyAction.Init(newUserInfo);
            notifyAction = saasGuestActivationV115NotifyAction;
        }
        
        await studioNotifyServiceHelper.SendNoticeToAsync(notifyAction, await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false), [EMailSenderName]); 
    }

    public async Task SendMsgProfileDeletionAsync(UserInfo user)
    {
        var profileDeleteNotifyAction = serviceProvider.GetService<ProfileDeleteNotifyAction>();
        await profileDeleteNotifyAction.Init(user);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(profileDeleteNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(user.Email, false), [EMailSenderName]);
    }

    public async Task SendMsgProfileHasDeletedItselfAsync(UserInfo user)
    {
        var recipients = await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupAdmin.ID);

        foreach (var recipient in recipients)
        {
            var culture = GetCulture(recipient);
            
            var profileHasDeletedItselfNotifyAction = serviceProvider.GetService<ProfileHasDeletedItselfNotifyAction>();
            await profileHasDeletedItselfNotifyAction.Init(user, culture.Name);
            
            await studioNotifyServiceHelper.SendNoticeToAsync(profileHasDeletedItselfNotifyAction, [recipient], [EMailSenderName]);
        }
    }

    public async Task SendMsgReassignsCompletedAsync(Guid recipientId, UserInfo fromUser, UserInfo toUser)
    { 
        var reassignsCompletedNotifyAction = serviceProvider.GetService<ReassignsCompletedNotifyAction>();
        await reassignsCompletedNotifyAction.Init(recipientId, fromUser, toUser);

        await studioNotifyServiceHelper.SendNoticeToAsync(reassignsCompletedNotifyAction, [await studioNotifyHelper.ToRecipientAsync(recipientId)], [EMailSenderName]);
    }

    public async Task SendMsgReassignsFailedAsync(Guid recipientId, UserInfo fromUser, UserInfo toUser, string message)
    {        
        var reassignsFailedNotifyAction = serviceProvider.GetService<ReassignsFailedNotifyAction>();
        await reassignsFailedNotifyAction.Init(recipientId, fromUser, toUser, message);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(reassignsFailedNotifyAction, [await studioNotifyHelper.ToRecipientAsync(recipientId)], [EMailSenderName]);
    }

    public async Task SendMsgRemoveUserDataCompletedAsync(Guid recipientId, UserInfo user, string fromUserName, long docsSpace, long crmSpace, long mailSpace, long talkSpace)
    {
        var removeUserDataCompletedNotifyAction = serviceProvider.GetService<RemoveUserDataCompletedNotifyAction>();
        await removeUserDataCompletedNotifyAction.Init(recipientId, user, fromUserName, docsSpace, crmSpace, mailSpace, talkSpace);
        
        var removeUserDataCompletedCustomModeNotifyAction = serviceProvider.GetService<RemoveUserDataCompletedCustomModeNotifyAction>();
        await removeUserDataCompletedCustomModeNotifyAction.Init(recipientId, user, fromUserName, docsSpace, crmSpace, mailSpace, talkSpace);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(
            coreBaseSettings.CustomMode ? removeUserDataCompletedCustomModeNotifyAction : removeUserDataCompletedNotifyAction,
            [await studioNotifyHelper.ToRecipientAsync(recipientId)],
            [EMailSenderName]);
    }

    public async Task SendMsgRemoveUserDataFailedAsync(Guid recipientId, UserInfo user, string fromUserName, string message)
    {
        var removeUserDataFailedNotifyAction = serviceProvider.GetService<RemoveUserDataFailedNotifyAction>();
        await removeUserDataFailedNotifyAction.Init(recipientId, user, fromUserName, message);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(removeUserDataFailedNotifyAction, [await studioNotifyHelper.ToRecipientAsync(recipientId)], [EMailSenderName]);
    }

    public async Task SendAdminWelcomeAsync(UserInfo newUserInfo)
    {
        if (!userManager.UserExists(newUserInfo))
        {
            return;
        }

        if (!newUserInfo.IsActive)
        {
            throw new ArgumentException("User is not activated yet!");
        }
        
        if (tenantExtra.Enterprise)
        {
            return;
            //var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(_settingsManager);
            //notifyAction = defaultRebranding ? Actions.EnterpriseAdminWelcomeV1 : Actions.EnterpriseWhitelabelAdminWelcomeV1;
        }

        if (tenantExtra.Opensource)
        {
            return;
            //notifyAction = Actions.OpensourceAdminWelcomeV1;
            //tagValues.Add(new TagValue(CommonTags.Footer, "opensource"));
        }
        
        var saasAdminWelcomeV1NotifyAction = serviceProvider.GetService<SaasAdminWelcomeV1NotifyAction>();
        saasAdminWelcomeV1NotifyAction.Init(newUserInfo);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(saasAdminWelcomeV1NotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false), [EMailSenderName]);
    }

    public async Task SendMsgUserTypeChangedAsync(UserInfo u, string userType)
    {
        try
        {        
            var userTypeChangedNotifyAction = serviceProvider.GetService<UserTypeChangedNotifyAction>();
            userTypeChangedNotifyAction.Init(u, userType);
            
            await studioNotifyServiceHelper.SendNoticeToAsync(userTypeChangedNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(u.Email, false), [EMailSenderName]);
        }
        catch (Exception error)
        {
            _log.ErrorSendCongratulations(error);
        }
    }

    public async Task SendMsgUserRoleChangedAsync(UserInfo u, string roomTitle, string roomUrl, string userRole, bool isAgent = false)
    {
        try
        {
            var userAgentRoleChangedNotifyAction = serviceProvider.GetService<UserAgentRoleChangedNotifyAction>();
            userAgentRoleChangedNotifyAction.Init(u, roomTitle, roomUrl, userRole);
            
            var userRoleChangedNotifyAction = serviceProvider.GetService<UserRoleChangedNotifyAction>();
            userRoleChangedNotifyAction.Init(u, roomTitle, roomUrl, userRole);
            
            await studioNotifyServiceHelper.SendNoticeToAsync(isAgent ? userAgentRoleChangedNotifyAction : userRoleChangedNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(u.Email, false), [EMailSenderName]);
        }
        catch (Exception error)
        {
            _log.ErrorSendCongratulations(error);
        }
    }

    #region Portal Deactivation & Deletion

    public async Task SendMsgPortalDeactivationAsync(Tenant t, string deactivateUrl, string activateUrl)
    {
        var u = await userManager.GetUsersAsync(t.OwnerId);
        
        var portalDeactivateNotifyAction = serviceProvider.GetService<PortalDeactivateNotifyAction>();
        portalDeactivateNotifyAction.Init(u, deactivateUrl, activateUrl);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(portalDeactivateNotifyAction, [u], [EMailSenderName]);
    }

    public async Task SendMsgPortalDeletionAsync(Tenant t, string url, bool showAutoRenewText, bool checkActivation = true)
    {
        var u = await userManager.GetUsersAsync(t.OwnerId);

        var recipient = checkActivation ? [u] : await studioNotifyHelper.RecipientFromEmailAsync(u.Email, false);
        
        var portalDeleteNotifyAction = serviceProvider.GetService<PortalDeleteNotifyAction>();
        portalDeleteNotifyAction.Init(u, url, showAutoRenewText);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(portalDeleteNotifyAction, recipient, [EMailSenderName]);
    }

    public async Task SendMsgPortalDeletionSuccessAsync(UserInfo owner, string url)
    {
        var portalDeleteSuccessV1NotifyAction = serviceProvider.GetService<PortalDeleteSuccessV1NotifyAction>();
        portalDeleteSuccessV1NotifyAction.Init(owner, url);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(portalDeleteSuccessV1NotifyAction, [owner], [EMailSenderName]);
    }

    public async Task SendMsgPaidPortalDeletedToSupportAsync(string tenantDomain, UserInfo owner, CustomerInfo customerInfo)
    {
        var email = commonLinkUtility.GetSupportEmail();
        if (string.IsNullOrEmpty(email))
        {
            return;
        }
        
        var portalDeletedToSupportNotifyAction = serviceProvider.GetService<PortalDeletedToSupportNotifyAction>();
        portalDeletedToSupportNotifyAction.Init(owner, tenantDomain, customerInfo);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(portalDeletedToSupportNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(email, false), [EMailSenderName]);
    }

    #endregion

    public async Task SendMsgConfirmChangeOwnerAsync(UserInfo owner, UserInfo newOwner, string confirmOwnerUpdateUrl)
    {
        var confirmOwnerChangeNotifyAction = serviceProvider.GetService<ConfirmOwnerChangeNotifyAction>();
        confirmOwnerChangeNotifyAction.Init(owner, newOwner, confirmOwnerUpdateUrl);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(confirmOwnerChangeNotifyAction, null, [owner], [EMailSenderName]);
    }

    public async Task SendCongratulationsAsync(UserInfo u)
    {
        try
        {
            INotifyAction notifyAction;
            var footer = "common";

            if (tenantExtra.Enterprise)
            {
                var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(settingsManager);
                notifyAction = defaultRebranding ? actions.EnterpriseAdminActivationV1 : actions.EnterpriseWhitelabelAdminActivationV1;
                footer = null;
            }
            else if (tenantExtra.Opensource)
            {
                notifyAction = actions.OpensourceAdminActivationV1;
                footer = "opensource";
            }
            else
            {
                notifyAction = actions.SaasAdminActivationV1;
            }

            var culture = GetCulture(u);

            ITagValue orangeButton = new TagValue("OrangeButton", "");

            if (u.ActivationStatus != EmployeeActivationStatus.Activated)
            {
                var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(u.Email, ConfirmType.EmailActivation, null, u.Id);
                var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirm", culture);
                orangeButton = TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl));

                await settingsManager.SaveAsync(new FirstEmailConfirmSettings { IsFirst = true });
            }

            var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

            await studioNotifyServiceHelper.SendNoticeToAsync(
            notifyAction,
            await studioNotifyHelper.RecipientFromEmailAsync(u.Email, false),
            [EMailSenderName],
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            orangeButton,
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
            new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, footer));
        }
        catch (Exception error)
        {
            _log.ErrorSendCongratulations(error);
        }
    }

    #region Migration Portal

    public async Task PortalRenameNotifyAsync(Tenant tenant, string oldVirtualRootPath, string oldAlias)
    {
        var users = (await userManager.GetUsersAsync())
                .Where(u => u.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated));

        try
        {
            tenantManager.SetCurrentTenant(tenant);

            foreach (var u in users)
            {
                var culture = string.IsNullOrEmpty(u.CultureName) ? tenant.GetCulture() : u.GetCulture();
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                
                var portalRenameNotifyAction = serviceProvider.GetService<PortalRenameNotifyAction>();
                portalRenameNotifyAction.Init(u, oldVirtualRootPath);

                await studioNotifyServiceHelper.SendNoticeToAsync(
                    portalRenameNotifyAction,
                    [await studioNotifyHelper.ToRecipientAsync(u.Id)],
                    [EMailSenderName],
                    commonLinkUtility.GetFullAbsolutePath("").Replace(oldAlias, tenant.Alias));
            }
        }
        catch (Exception ex)
        {
            _log.ErrorPortalRenameNotify(ex);
        }
    }

    #endregion

    #region Helpers



    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }


    public async Task SendRegDataAsync(UserInfo u)
    {
        try
        {
            if (!tenantExtra.Saas || !coreBaseSettings.CustomMode)
            {
                return;
            }

            var salesEmail = externalResourceSettingsHelper.Common.GetDefaultRegionalFullEntry("paymentemail");

            if (string.IsNullOrEmpty(salesEmail))
            {
                return;
            }

            var recipient = new DirectRecipient(salesEmail, null, [salesEmail], false);
            
            var saasCustomModeRegDataNotifyAction = serviceProvider.GetService<SaasCustomModeRegDataNotifyAction>();
            saasCustomModeRegDataNotifyAction.Init(u);
            
            await studioNotifyServiceHelper.SendNoticeToAsync(saasCustomModeRegDataNotifyAction, [recipient], [EMailSenderName]);
        }
        catch (Exception error)
        {
            _log.ErrorSendRegData(error);
        }
    }

    #endregion

    #region Storage encryption

    public async Task SendStorageEncryptionStartAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(actions.StorageEncryptionStart, false, serverRootPath);
    }

    public async Task SendStorageEncryptionSuccessAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(actions.StorageEncryptionSuccess, false, serverRootPath);
    }

    public async Task SendStorageEncryptionErrorAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(actions.StorageEncryptionError, true, serverRootPath);
    }

    public async Task SendStorageDecryptionStartAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(actions.StorageDecryptionStart, false, serverRootPath);
    }

    public async Task SendStorageDecryptionSuccessAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(actions.StorageDecryptionSuccess, false, serverRootPath);
    }

    public async Task SendStorageDecryptionErrorAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(actions.StorageDecryptionError, true, serverRootPath);
    }

    private async Task SendStorageEncryptionNotifyAsync(INotifyAction action, bool notifyAdminsOnly, string serverRootPath)
    {
        var users = notifyAdminsOnly
                    ? await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupAdmin.ID)
                    : (await userManager.GetUsersAsync()).Where(u => u.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated));

        foreach (var u in users)
        {
            await studioNotifyServiceHelper.SendNoticeToAsync(
            action,
            null,
            [await studioNotifyHelper.ToRecipientAsync(u.Id)],
            [EMailSenderName],
            new TagValue(CommonTags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(CommonTags.PortalUrl, serverRootPath));
        }
    }

    #endregion


    #region Zoom

    public async Task SendZoomWelcomeAsync(UserInfo u, string portalUrl = null)
    {
        try
        {
            var zoomWelcomeNotifyAction = serviceProvider.GetService<ZoomWelcomeNotifyAction>();
            zoomWelcomeNotifyAction.Init(u);
            
            await studioNotifyServiceHelper.SendNoticeToAsync(
                zoomWelcomeNotifyAction,
                await studioNotifyHelper.RecipientFromEmailAsync(u.Email, false),
                [EMailSenderName],
                portalUrl ?? commonLinkUtility.GetFullAbsolutePath(""));
        }
        catch (Exception error)
        {
            _log.ErrorSendCongratulations(error);
        }
    }

    #endregion


    #region Wallet

    public async Task SendTopUpWalletErrorAsync(UserInfo payer, UserInfo owner)
    {
        var users = new[] { payer, owner }
            .Where(user => user != null && !string.IsNullOrEmpty(user.Email))
            .DistinctBy(user => user.Email);
        
        var topUpWalletErrorNotifyAction = serviceProvider.GetService<TopUpWalletErrorNotifyAction>();
        
        foreach (var user in users)
        {
            topUpWalletErrorNotifyAction.Init(user);
            
            await studioNotifyServiceHelper.SendNoticeToAsync(topUpWalletErrorNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(user.Email, false), [EMailSenderName]);
        }
    }

    public async Task SendRenewSubscriptionErrorAsync(UserInfo payer, UserInfo owner)
    {
        var users = new[] { payer, owner }
            .Where(user => user != null && !string.IsNullOrEmpty(user.Email))
            .DistinctBy(user => user.Email);
        
        var renewSubscriptionErrorNotifyAction = serviceProvider.GetService<RenewSubscriptionErrorNotifyAction>();
        
        foreach (var user in users)
        {
            renewSubscriptionErrorNotifyAction.Init(user);

            await studioNotifyServiceHelper.SendNoticeToAsync(renewSubscriptionErrorNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(user.Email, false), [EMailSenderName]);
        }
    }

    #endregion


    #region Migration Personal to Docspace

    public async Task MigrationPersonalToDocspaceAsync(UserInfo userInfo)
    {
        var auditEventDate = DateTime.UtcNow;

        auditEventDate = new DateTime(
            auditEventDate.Year,
            auditEventDate.Month,
            auditEventDate.Day,
            auditEventDate.Hour,
            auditEventDate.Minute,
            auditEventDate.Second,
            0,
            DateTimeKind.Utc);
        
        var migrationPersonalToDocspaceNotifyAction = serviceProvider.GetService<MigrationPersonalToDocspaceNotifyAction>();
        await migrationPersonalToDocspaceNotifyAction.Init(userInfo, auditEventDate);

        await studioNotifyServiceHelper.SendNoticeToAsync(migrationPersonalToDocspaceNotifyAction, await studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false), [EMailSenderName]);

        var displayUserName = userInfo.DisplayUserName(false, displayUserSettingsHelper);

        messageService.Send(MessageAction.UserSentPasswordChangeInstructions, MessageTarget.Create(userInfo.Id), auditEventDate, displayUserName);
    }

    #endregion


    #region API Keys

    public async Task SendApiKeyExpiredAsync(UserInfo userInfo, string keyName)
    {
        var apiKeyExpiredNotifyAction = serviceProvider.GetService<ApiKeyExpiredNotifyAction>();
        apiKeyExpiredNotifyAction.Init(userInfo, keyName);
        
        await studioNotifyServiceHelper.SendNoticeToAsync(apiKeyExpiredNotifyAction, [userInfo], [EMailSenderName]);
    }

    #endregion

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