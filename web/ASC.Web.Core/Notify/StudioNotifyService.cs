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

using Constants = ASC.Core.Configuration.Constants;

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public class StudioNotifyService(
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    StudioNotifyServiceHelper studioNotifyServiceHelper,
    TenantExtra tenantExtra,
    AuthContext authContext,
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings,
    CommonLinkUtility commonLinkUtility,
    SetupInfo setupInfo,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    UserInvitationLimitHelper userInvitationLimitHelper,
    SettingsManager settingsManager,
    MessageService messageService,
    IUrlShortener urlShortener,
    ILoggerProvider option)
{
    public static string EMailSenderName { get { return Constants.NotifyEMailSenderSysName; } }

    private readonly ILogger _log = option.CreateLogger("ASC.Notify");

    public async Task SendMsgToAdminFromNotAuthUserAsync(string email, string message, string culture)
    {
        List<ITagValue> tags =
        [
            new TagValue(Tags.Body, message),
            new TagValue(Tags.UserEmail, email)
        ];

        if (!string.IsNullOrEmpty(culture))
        {
            tags.Add(new TagValue(CommonTags.Culture, culture));
        }

        await studioNotifyServiceHelper.SendNoticeAsync(Actions.UserMessageToAdmin, tags.ToArray());
    }

    public async Task SendMsgToSalesAsync(string email, string userName, string message)
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>();

        await studioNotifyServiceHelper.SendNoticeToAsync(
            Actions.UserMessageToSales,
            await studioNotifyHelper.RecipientFromEmailAsync(settings.SalesEmail, false),
            [EMailSenderName],
            new TagValue(Tags.Body, message),
            new TagValue(Tags.UserEmail, email),
            new TagValue(Tags.UserName, userName));
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

        var hash = auditEventDate.ToString("s", CultureInfo.InvariantCulture);

        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(userInfo.Email, ConfirmType.PasswordChange, hash, userInfo.Id);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString(initialPasswordAssignment ? "ButtonSetPassword" : "ButtonChangePassword", GetCulture(userInfo));

        var action = initialPasswordAssignment ? Actions.PasswordSet : Actions.PasswordChangeV115;

        await studioNotifyServiceHelper.SendNoticeToAsync(
                action,
                    await studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false),
                    [EMailSenderName],
                TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl)));

        var displayUserName = userInfo.DisplayUserName(false, displayUserSettingsHelper);

        messageService.Send(MessageAction.UserSentPasswordChangeInstructions, MessageTarget.Create(userInfo.Id), auditEventDate, displayUserName);
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

        var postfix = auditEventDate.ToString("s", CultureInfo.InvariantCulture);

        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(email, ConfirmType.EmailChange, postfix, user.Id);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangeEmail", GetCulture(user));

        var action = Actions.EmailChangeV115;

        await studioNotifyServiceHelper.SendNoticeToAsync(
                action,
                    await studioNotifyHelper.RecipientFromEmailAsync(email, false),
                    [EMailSenderName],
                TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl)),
                new TagValue(CommonTags.Culture, user.GetCulture().Name));

        var displayUserName = user.DisplayUserName(false, displayUserSettingsHelper);

        messageService.Send(MessageAction.UserSentEmailChangeInstructions, MessageTarget.Create(user.Id), auditEventDate, displayUserName);
    }

    public async Task SendEmailActivationInstructionsAsync(UserInfo user, string email)
    {
        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(email, ConfirmType.EmailActivation, null, user.Id);
        var shortLink  = await urlShortener.GetShortenLinkAsync(confirmationUrl);
        
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonActivateEmail", GetCulture(user));

        await studioNotifyServiceHelper.SendNoticeToAsync(
                Actions.ActivateEmail,
                    await studioNotifyHelper.RecipientFromEmailAsync(email, false),
                    [EMailSenderName],
                new TagValue(Tags.InviteLink, shortLink),
                new TagValue(CommonTags.Culture, user.GetCulture().Name),
                TagValues.OrangeButton(orangeButtonText, shortLink),
                    new TagValue(Tags.UserDisplayName, (user.DisplayUserName(displayUserSettingsHelper) ?? string.Empty).Trim()));
    }

    public async Task SendEmailRoomInviteAsync(string email, string roomTitle, string confirmationUrl, string culture = null, bool limitation = false)
    {
        var cultureInfo = string.IsNullOrEmpty(culture) ? (GetCulture(null)) : new CultureInfo(culture);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);

        var tags = new List<ITagValue>
        { 
            new TagValue(Tags.Message, roomTitle),
            new TagValue(Tags.InviteLink, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        };

        await studioNotifyServiceHelper.SendNoticeToAsync(
            Actions.SaasRoomInvite,
                await studioNotifyHelper.RecipientFromEmailAsync(email, false),
                [EMailSenderName],
                tags.ToArray());

        if (limitation)
        {
            await userInvitationLimitHelper.ReduceLimit();
        }
    }

    public async Task SendEmailRoomInviteExistingUserAsync(UserInfo user, string roomTitle, string roomUrl)
    {
        var cultureInfo = GetCulture(user);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonJoinRoom", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);

        var tags = new List<ITagValue>
        {
            new TagValue(Tags.Message, roomTitle),
            new TagValue(Tags.InviteLink, roomUrl),
            TagValues.OrangeButton(orangeButtonText, roomUrl),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        };

        await studioNotifyServiceHelper.SendNoticeToAsync(
            Actions.SaasRoomInviteExistingUser,
            [user],
            [EMailSenderName],
            tags.ToArray());
    }

    public async Task SendDocSpaceInviteAsync(string email, string confirmationUrl, string culture = "", bool limitation = false)
    {
        var cultureInfo = string.IsNullOrEmpty(culture) ? (GetCulture(null)) : new CultureInfo(culture);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);

        var tags = new List<ITagValue>
        {
                new TagValue(Tags.InviteLink, confirmationUrl),
                TagValues.OrangeButton(orangeButtonText, confirmationUrl),
                TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
                new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
                new TagValue(CommonTags.Culture, cultureInfo.Name)
        };

        await studioNotifyServiceHelper.SendNoticeToAsync(
            Actions.SaasDocSpaceInvite,
                await studioNotifyHelper.RecipientFromEmailAsync(email, false),
                [EMailSenderName],
                tags.ToArray());

        if (limitation)
        {
            await userInvitationLimitHelper.ReduceLimit();
        }
    }

    public async Task SendDocSpaceRegistration(string email, string confirmationUrl, string culture = "", bool limitation = false)
    {
        var cultureInfo = string.IsNullOrEmpty(culture) ? (GetCulture(null)) : new CultureInfo(culture);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonRegister", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);

        var tags = new List<ITagValue>
        {
                new TagValue(Tags.InviteLink, confirmationUrl),
                TagValues.OrangeButton(orangeButtonText, confirmationUrl),
                TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
                new TagValue(CommonTags.Culture, cultureInfo.Name)
        };

        await studioNotifyServiceHelper.SendNoticeToAsync(
            Actions.SaasDocSpaceRegistration,
                await studioNotifyHelper.RecipientFromEmailAsync(email, false),
                [EMailSenderName],
                tags.ToArray());

        if (limitation)
        {
            await userInvitationLimitHelper.ReduceLimit();
        }
    }
    #endregion

    public async Task SendMsgMobilePhoneChangeAsync(UserInfo userInfo)
    {
        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(userInfo.Email.ToLower(), ConfirmType.PhoneActivation);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangePhone", GetCulture(userInfo));

        await studioNotifyServiceHelper.SendNoticeToAsync(
        Actions.PhoneChange,
           await studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false),
           [EMailSenderName],
        TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl)));
    }

    public async Task SendMsgTfaResetAsync(UserInfo userInfo)
    {
        var confirmationUrl = commonLinkUtility.GetFullAbsolutePath(string.Empty);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangeTfa", GetCulture(userInfo));

        await studioNotifyServiceHelper.SendNoticeToAsync(
        Actions.TfaChange,
           await studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false),
           [EMailSenderName],
        TagValues.OrangeButton(orangeButtonText, confirmationUrl));
    }


    public async ValueTask UserHasJoinAsync()
    {
        await studioNotifyServiceHelper.SendNoticeAsync(Actions.UserHasJoin);
    }

    public async Task SendJoinMsgAsync(string email, EmployeeType emplType, string culture, bool limitation = false)
    {
        var inviteUrl = commonLinkUtility.GetConfirmationEmailUrl(email, ConfirmType.EmpInvite, (int)emplType + "trust") + $"&emplType={(int)emplType}";
        var shortLink = await urlShortener.GetShortenLinkAsync(inviteUrl);
        
        var orangeButtonText = WebstudioNotifyPatternResource.ButtonJoin;

        List<ITagValue> tags =
        [
            new TagValue(Tags.InviteLink, shortLink),
            TagValues.OrangeButton(orangeButtonText, shortLink)
        ];

        if (!string.IsNullOrEmpty(culture))
        {
            tags.Add(new TagValue(CommonTags.Culture, culture));
        }
        
        await studioNotifyServiceHelper.SendNoticeToAsync(
            Actions.JoinUsers,
            await studioNotifyHelper.RecipientFromEmailAsync(email, false),
            [EMailSenderName],
            tags.ToArray());
        
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
        var footer = "social";

        if (tenantExtra.Enterprise)
        {
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(settingsManager);
            notifyAction = defaultRebranding
                               ? Actions.EnterpriseUserWelcomeV1
                                   : coreBaseSettings.CustomMode
                                     ? Actions.EnterpriseWhitelabelUserWelcomeCustomModeV1
                                     : Actions.EnterpriseWhitelabelUserWelcomeV1;
            footer = null;
        }
        else if (tenantExtra.Opensource)
        {
            notifyAction = Actions.OpensourceUserWelcomeV1;
            footer = "opensource";
        }
        else
        {
            notifyAction = Actions.SaasUserWelcomeV1;
        }

        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborate", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        await studioNotifyServiceHelper.SendNoticeToAsync(
        notifyAction,
           await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
           [EMailSenderName],
        new TagValue(Tags.UserName, newUserInfo.FirstName.HtmlEncode()),
        new TagValue(Tags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
        new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
        new TagValue("IMG1", img1),
        new TagValue("IMG2", img2),
        new TagValue("IMG3", img3),
        new TagValue("IMG4", img4),
        new TagValue(CommonTags.Footer, footer));
    }

    public async Task GuestInfoAddedAfterInviteAsync(UserInfo newUserInfo)
    {
        if (!userManager.UserExists(newUserInfo))
        {
            return;
        }

        INotifyAction notifyAction;
        var footer = "social";

        if (tenantExtra.Enterprise)
        {
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(settingsManager);
            notifyAction = defaultRebranding ? Actions.EnterpriseGuestWelcomeV1 : Actions.EnterpriseWhitelabelGuestWelcomeV1;
            footer = null;
        }
        else if (tenantExtra.Opensource)
        {
            notifyAction = Actions.OpensourceGuestWelcomeV1;
            footer = "opensource";
        }
        else
        {
            notifyAction = Actions.SaasGuestWelcomeV1;
        }

        var culture = GetCulture(newUserInfo);
        var orangeButtonText = tenantExtra.Enterprise
                              ? WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccessYourPortal", culture)
                              : WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccessYouWebOffice", culture);

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        await studioNotifyServiceHelper.SendNoticeToAsync(
        notifyAction,
           await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
           [EMailSenderName],
        new TagValue(Tags.UserName, newUserInfo.FirstName.HtmlEncode()),
        new TagValue(Tags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
        new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
        new TagValue("IMG1", img1),
        new TagValue("IMG2", img2),
        new TagValue("IMG3", img3),
        new TagValue("IMG4", img4),
        new TagValue(CommonTags.Footer, footer));
    }

    public async Task UserInfoActivationAsync(UserInfo newUserInfo)
    {
        if (newUserInfo.IsActive)
        {
            throw new ArgumentException("User is already activated!");
        }

        INotifyAction notifyAction;
        var footer = "social";

        if (tenantExtra.Enterprise)
        {
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(settingsManager);
            notifyAction = defaultRebranding ? Actions.EnterpriseUserActivationV1 : Actions.EnterpriseWhitelabelUserActivationV1;
            footer = null;
        }
        else if (tenantExtra.Opensource)
        {
            notifyAction = Actions.OpensourceUserActivationV1;
            footer = "opensource";
        }
        else
        {
            notifyAction = Actions.SaasUserActivationV1;
        }

        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        await studioNotifyServiceHelper.SendNoticeToAsync(
        notifyAction,
           await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
           [EMailSenderName],
        new TagValue(Tags.ActivateUrl, confirmationUrl),
        TagValues.OrangeButton(orangeButtonText, confirmationUrl), 
        TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
        new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
        new TagValue(Tags.UserName, newUserInfo.FirstName.HtmlEncode()),
        new TagValue(CommonTags.Footer, footer));
    }

    public async Task GuestInfoActivationAsync(UserInfo newUserInfo)
    {
        if (newUserInfo.IsActive)
        {
            throw new ArgumentException("User is already activated!");
        }

        INotifyAction notifyAction;
        var footer = "social";

        if (tenantExtra.Enterprise)
        {
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(settingsManager);
            notifyAction = defaultRebranding ? Actions.EnterpriseGuestActivationV10 : Actions.EnterpriseWhitelabelGuestActivationV10;
            footer = null;
        }
        else if (tenantExtra.Opensource)
        {
            notifyAction = Actions.OpensourceGuestActivationV11;
            footer = "opensource";
        }
        else
        {
            notifyAction = Actions.SaasGuestActivationV115;
        }

        var confirmationUrl = await GenerateActivationConfirmUrlAsync(newUserInfo);
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        await studioNotifyServiceHelper.SendNoticeToAsync(
        notifyAction,
           await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
           [EMailSenderName],
        new TagValue(Tags.ActivateUrl, confirmationUrl),
        TagValues.OrangeButton(orangeButtonText, confirmationUrl),
        TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
        new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
        new TagValue(Tags.UserName, newUserInfo.FirstName.HtmlEncode()),
        new TagValue(CommonTags.Footer, footer));
    }

    public async Task SendMsgProfileDeletionAsync(UserInfo user)
    {
        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.ProfileRemove, authContext.CurrentAccount.ID, authContext.CurrentAccount.ID);
        var culture = GetCulture(user);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonRemoveProfile", culture);

        var action = Actions.ProfileDelete;
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        await studioNotifyServiceHelper.SendNoticeToAsync(
            action,
            await studioNotifyHelper.RecipientFromEmailAsync(user.Email, false),
            [EMailSenderName],
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl)),
            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, user.GetCulture().Name));
    }

    public async Task SendMsgProfileHasDeletedItselfAsync(UserInfo user)
    {
        var userName = user.DisplayUserName(displayUserSettingsHelper);
        var userLink = await GetUserProfileLinkAsync(user.Id);
        var recipients = await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupAdmin.ID);

        foreach (var recipient in recipients)
        {
            var culture = GetCulture(recipient);

            await studioNotifyServiceHelper.SendNoticeToAsync(
                Actions.ProfileHasDeletedItself,
                [recipient],
                [EMailSenderName],
                new TagValue(Tags.FromUserName, userName),
                new TagValue(Tags.FromUserLink, userLink),
                new TagValue(CommonTags.Culture, culture.Name));
        }
    }

    public async Task SendMsgReassignsCompletedAsync(Guid recipientId, UserInfo fromUser, UserInfo toUser)
    {
        await studioNotifyServiceHelper.SendNoticeToAsync(
        Actions.ReassignsCompleted,
        [await studioNotifyHelper.ToRecipientAsync(recipientId)],
        [EMailSenderName],
            new TagValue(Tags.UserName, await displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(Tags.FromUserName, fromUser.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(Tags.FromUserLink, await GetUserProfileLinkAsync(fromUser.Id)),
            new TagValue(Tags.ToUserName, toUser.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(Tags.ToUserLink, await GetUserProfileLinkAsync(toUser.Id)));
    }

    public async Task SendMsgReassignsFailedAsync(Guid recipientId, UserInfo fromUser, UserInfo toUser, string message)
    {
        await studioNotifyServiceHelper.SendNoticeToAsync(
        Actions.ReassignsFailed,
        [await studioNotifyHelper.ToRecipientAsync(recipientId)],
        [EMailSenderName],
            new TagValue(Tags.UserName, await displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(Tags.FromUserName, fromUser.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(Tags.FromUserLink, await GetUserProfileLinkAsync(fromUser.Id)),
            new TagValue(Tags.ToUserName, toUser.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(Tags.ToUserLink, await GetUserProfileLinkAsync(toUser.Id)),
            new TagValue(Tags.Message, message));
    }

    public async Task SendMsgRemoveUserDataCompletedAsync(Guid recipientId, UserInfo user, string fromUserName, long docsSpace, long crmSpace, long mailSpace, long talkSpace)
    {
        await studioNotifyServiceHelper.SendNoticeToAsync(
            coreBaseSettings.CustomMode ? Actions.RemoveUserDataCompletedCustomMode : Actions.RemoveUserDataCompleted,
            [await studioNotifyHelper.ToRecipientAsync(recipientId)],
            [EMailSenderName],
            new TagValue(Tags.UserName, await displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(Tags.FromUserName, fromUserName.HtmlEncode()),
            new TagValue(Tags.FromUserLink, await GetUserProfileLinkAsync(user.Id)),
            new TagValue("DocsSpace", FileSizeComment.FilesSizeToString(docsSpace)),
            new TagValue("CrmSpace", FileSizeComment.FilesSizeToString(crmSpace)),
            new TagValue("MailSpace", FileSizeComment.FilesSizeToString(mailSpace)),
            new TagValue("TalkSpace", FileSizeComment.FilesSizeToString(talkSpace)));
    }

    public async Task SendMsgRemoveUserDataFailedAsync(Guid recipientId, UserInfo user, string fromUserName, string message)
    {
        await studioNotifyServiceHelper.SendNoticeToAsync(
        Actions.RemoveUserDataFailed,
        [await studioNotifyHelper.ToRecipientAsync(recipientId)],
        [EMailSenderName],
            new TagValue(Tags.UserName, await displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(Tags.FromUserName, fromUserName.HtmlEncode()),
            new TagValue(Tags.FromUserLink, await GetUserProfileLinkAsync(user.Id)),
            new TagValue(Tags.Message, message));
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

        var tagValues = new List<ITagValue>();

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
        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonUpgrade", culture);
        var notifyAction = Actions.SaasAdminWelcomeV1;
        tagValues.Add(new TagValue(CommonTags.Footer, "common"));

        tagValues.Add(new TagValue(Tags.UserName, newUserInfo.FirstName.HtmlEncode()));
        tagValues.Add(new TagValue(Tags.PricingPage, commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/portal-payments")));
        tagValues.Add(TagValues.OrangeButton(orangeButtonText, commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/portal-payments")));
        tagValues.Add(TagValues.TrulyYours(studioNotifyHelper, WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", GetCulture(newUserInfo)), true));
        tagValues.Add(new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("discover_business_subscription.gif")));

        await studioNotifyServiceHelper.SendNoticeToAsync(
        notifyAction,
           await studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
           [EMailSenderName],
        tagValues.ToArray());
    }

    #region Portal Deactivation & Deletion

    public async Task SendMsgPortalDeactivationAsync(Tenant t, string deactivateUrl, string activateUrl)
    {
        var u = await userManager.GetUsersAsync(t.OwnerId);
        var culture = GetCulture(u);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonDeactivatePortal", culture);
        var bestReagardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        await studioNotifyServiceHelper.SendNoticeToAsync(
                Actions.PortalDeactivate,
                [u],
                [EMailSenderName],
                new TagValue(Tags.ActivateUrl, activateUrl),
                TagValues.OrangeButton(orangeButtonText, deactivateUrl),
                TagValues.TrulyYours(studioNotifyHelper, bestReagardsTxt),
                    new TagValue(Tags.OwnerName, u.DisplayUserName(displayUserSettingsHelper)));
    }

    public async Task SendMsgPortalDeletionAsync(Tenant t, string url, bool showAutoRenewText, bool checkActivation = true)
    {
        var u = await userManager.GetUsersAsync(t.OwnerId);
        var culture = GetCulture(u);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonDeletePortal", culture);
        var bestReagardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        var recipient = checkActivation ? [u] : await studioNotifyHelper.RecipientFromEmailAsync(u.Email, false);

        await studioNotifyServiceHelper.SendNoticeToAsync(
                Actions.PortalDelete,
                recipient,
                [EMailSenderName],
                TagValues.OrangeButton(orangeButtonText, url),
                TagValues.TrulyYours(studioNotifyHelper, bestReagardsTxt),
                new TagValue(Tags.AutoRenew, showAutoRenewText.ToString()),
                    new TagValue(Tags.OwnerName, u.DisplayUserName(displayUserSettingsHelper)));
    }

    public async Task SendMsgPortalDeletionSuccessAsync(UserInfo owner, string url)
    {
        var culture = GetCulture(owner);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonLeaveFeedback", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        await studioNotifyServiceHelper.SendNoticeToAsync(
                Actions.PortalDeleteSuccessV1,
                [owner],
                [EMailSenderName],
                TagValues.OrangeButton(orangeButtonText, url),
                TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours, true),
                new TagValue("URL1", setupInfo.LinksToExternalResources.GetValueOrDefault("legalterms")),
                new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("docspace_deactivated.gif")),
                new TagValue(Tags.OwnerName, owner.DisplayUserName(displayUserSettingsHelper)));
    }

    #endregion

    public async Task SendMsgConfirmChangeOwnerAsync(UserInfo owner, UserInfo newOwner, string confirmOwnerUpdateUrl)
    {
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirmPortalOwnerUpdate", owner.GetCulture());

        await studioNotifyServiceHelper.SendNoticeToAsync(
        Actions.ConfirmOwnerChange,
        null,
        [owner],
        [EMailSenderName],
        TagValues.OrangeButton(orangeButtonText, confirmOwnerUpdateUrl),
            new TagValue(Tags.UserName, newOwner.DisplayUserName(displayUserSettingsHelper)),
            new TagValue(Tags.OwnerName, owner.DisplayUserName(displayUserSettingsHelper)));
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
                notifyAction = defaultRebranding ? Actions.EnterpriseAdminActivationV1 : Actions.EnterpriseWhitelabelAdminActivationV1;
                footer = null;
            }
            else if (tenantExtra.Opensource)
            {
                notifyAction = Actions.OpensourceAdminActivationV1;
                footer = "opensource";
            }
            else
            {
                notifyAction = Actions.SaasAdminActivationV1;
            }

            var userId = u.Id;
            var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(u.Email, ConfirmType.EmailActivation, null, userId);

            await settingsManager.SaveAsync(new FirstEmailConfirmSettings { IsFirst = true });

            var culture = GetCulture(u);
            var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirm", culture);
            var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

            await studioNotifyServiceHelper.SendNoticeToAsync(
            notifyAction,
            await studioNotifyHelper.RecipientFromEmailAsync(u.Email, false),
            [EMailSenderName],
            new TagValue(Tags.UserName, u.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl)),
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

                await studioNotifyServiceHelper.SendNoticeToAsync(
                    Actions.PortalRename,
                    [await studioNotifyHelper.ToRecipientAsync(u.Id)],
                    [EMailSenderName],
                    commonLinkUtility.GetFullAbsolutePath("").Replace(oldAlias, tenant.Alias),
                    new TagValue(Tags.PortalUrl, oldVirtualRootPath),
                    new TagValue(Tags.UserDisplayName, u.DisplayUserName(displayUserSettingsHelper)));
            }
        }
        catch (Exception ex)
        {
            _log.ErrorPortalRenameNotify(ex);
        }
    }

    #endregion

    #region Helpers

    private string GetMyStaffLink()
    {
        return commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetMyStaff());
    }

    private async Task<string> GetUserProfileLinkAsync(Guid userId)
    {
        return commonLinkUtility.GetFullAbsolutePath(await commonLinkUtility.GetUserProfileAsync(userId));
    }


    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = commonLinkUtility.GetConfirmationEmailUrl(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return  await urlShortener.GetShortenLinkAsync(confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}");
    }


    public async Task SendRegDataAsync(UserInfo u)
    {
        try
        {
            if (!tenantExtra.Saas || !coreBaseSettings.CustomMode)
            {
                return;
            }

            var settings = await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>();
            var salesEmail = settings.SalesEmail;

            if (string.IsNullOrEmpty(salesEmail))
            {
                return;
            }

            var recipient = new DirectRecipient(salesEmail, null, [salesEmail], false);

            await studioNotifyServiceHelper.SendNoticeToAsync(
            Actions.SaasCustomModeRegData,
            null,
            [recipient],
            [EMailSenderName],
            new TagValue(Tags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(Tags.UserLastName, u.LastName.HtmlEncode()),
            new TagValue(Tags.UserEmail, u.Email.HtmlEncode()),
            new TagValue(Tags.Phone, u.MobilePhone != null ? u.MobilePhone.HtmlEncode() : "-"),
            new TagValue(Tags.Date, u.CreateDate.ToShortDateString() + " " + u.CreateDate.ToShortTimeString()),
            new TagValue(CommonTags.Footer, null),
            TagValues.WithoutUnsubscribe());
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
        await SendStorageEncryptionNotifyAsync(Actions.StorageEncryptionStart, false, serverRootPath);
    }

    public async Task SendStorageEncryptionSuccessAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(Actions.StorageEncryptionSuccess, false, serverRootPath);
    }

    public async Task SendStorageEncryptionErrorAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(Actions.StorageEncryptionError, true, serverRootPath);
    }

    public async Task SendStorageDecryptionStartAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(Actions.StorageDecryptionStart, false, serverRootPath);
    }

    public async Task SendStorageDecryptionSuccessAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(Actions.StorageDecryptionSuccess, false, serverRootPath);
    }

    public async Task SendStorageDecryptionErrorAsync(string serverRootPath)
    {
        await SendStorageEncryptionNotifyAsync(Actions.StorageDecryptionError, true, serverRootPath);
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
            new TagValue(Tags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(Tags.PortalUrl, serverRootPath));
        }
    }

    #endregion


    #region Zoom

    public async Task SendZoomWelcomeAsync(UserInfo u, string portalUrl = null)
    {
        try
        {
            var culture = GetCulture(u);
            var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

            await studioNotifyServiceHelper.SendNoticeToAsync(
                Actions.ZoomWelcome,
                await studioNotifyHelper.RecipientFromEmailAsync(u.Email, false),
                [EMailSenderName],
                portalUrl ??= commonLinkUtility.GetFullAbsolutePath(""),
                new TagValue(CommonTags.Culture, culture.Name),
                new TagValue(Tags.UserName, u.FirstName.HtmlEncode()),
                new TagValue(CommonTags.TopGif, studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
                TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours));
        }
        catch (Exception error)
        {
            _log.ErrorSendCongratulations(error);
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

        var hash = auditEventDate.ToString("s", CultureInfo.InvariantCulture);

        var confirmationUrl = commonLinkUtility.GetConfirmationEmailUrl(userInfo.Email, ConfirmType.PasswordChange, hash, userInfo.Id);

        var cultureInfo = GetCulture(userInfo);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", cultureInfo);

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);

        await studioNotifyServiceHelper.SendNoticeToAsync(
                Actions.MigrationPersonalToDocspace,
                await studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false),
                [EMailSenderName],
                TagValues.OrangeButton(orangeButtonText, await urlShortener.GetShortenLinkAsync(confirmationUrl)),
                TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours),
                new TagValue(CommonTags.Culture, cultureInfo.Name),
                new TagValue(CommonTags.Footer, "social"));

        var displayUserName = userInfo.DisplayUserName(false, displayUserSettingsHelper);

        messageService.Send(MessageAction.UserSentPasswordChangeInstructions, MessageTarget.Create(userInfo.Id), auditEventDate, displayUserName);
    }

    #endregion

    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (user != null && !string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        return culture ?? (tenantManager.GetCurrentTenant(false))?.GetCulture() ?? CultureInfo.CurrentUICulture;
    }
}
