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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public class StudioNotifyService
{
    private readonly StudioNotifyServiceHelper _client;

    public static string EMailSenderName { get { return ASC.Core.Configuration.Constants.NotifyEMailSenderSysName; } }

    private readonly UserManager _userManager;
    private readonly StudioNotifyHelper _studioNotifyHelper;
    private readonly TenantExtra _tenantExtra;
    private readonly AuthManager _authentication;
    private readonly AuthContext _authContext;
    private readonly TenantManager _tenantManager;
    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly CommonLinkUtility _commonLinkUtility;
    private readonly SetupInfo _setupInfo;
    private readonly DisplayUserSettingsHelper _displayUserSettingsHelper;
    private readonly SettingsManager _settingsManager;
    private readonly MessageService _messageService;
    private readonly MessageTarget _messageTarget;
    private readonly ILogger _log;

    public StudioNotifyService(
        UserManager userManager,
        StudioNotifyHelper studioNotifyHelper,
        StudioNotifyServiceHelper studioNotifyServiceHelper,
        TenantExtra tenantExtra,
        AuthManager authentication,
        AuthContext authContext,
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CommonLinkUtility commonLinkUtility,
        SetupInfo setupInfo,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        SettingsManager settingsManager,
        MessageService messageService,
        MessageTarget messageTarget,
        ILoggerProvider option)
    {
        _log = option.CreateLogger("ASC.Notify");
        _client = studioNotifyServiceHelper;
        _tenantExtra = tenantExtra;
        _authentication = authentication;
        _authContext = authContext;
        _tenantManager = tenantManager;
        _coreBaseSettings = coreBaseSettings;
        _commonLinkUtility = commonLinkUtility;
        _setupInfo = setupInfo;
        _displayUserSettingsHelper = displayUserSettingsHelper;
        _settingsManager = settingsManager;
        _messageService = messageService;
        _messageTarget = messageTarget;
        _userManager = userManager;
        _studioNotifyHelper = studioNotifyHelper;
    }

    public async Task SendMsgToAdminFromNotAuthUserAsync(string email, string message)
    {
        await _client.SendNoticeAsync(Actions.UserMessageToAdmin, new TagValue(Tags.Body, message), new TagValue(Tags.UserEmail, email));
    }

    public async Task SendMsgToSalesAsync(string email, string userName, string message)
    {
        var settings = await _settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>();

        await _client.SendNoticeToAsync(
            Actions.UserMessageToSales,
            await _studioNotifyHelper.RecipientFromEmailAsync(settings.SalesEmail, false),
            new[] { EMailSenderName },
            new TagValue(Tags.Body, message),
            new TagValue(Tags.UserEmail, email),
            new TagValue(Tags.UserName, userName));
    }

    #region User Password

    public async Task UserPasswordChangeAsync(UserInfo userInfo)
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

        var confirmationUrl = await _commonLinkUtility.GetConfirmationEmailUrlAsync(userInfo.Email, ConfirmType.PasswordChange, hash, userInfo.Id);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangePassword", GetCulture(userInfo));

        var action = _coreBaseSettings.Personal
                         ? (_coreBaseSettings.CustomMode ? Actions.PersonalCustomModeEmailChangeV115 : Actions.PersonalPasswordChangeV115)
                     : Actions.PasswordChangeV115;

        await _client.SendNoticeToAsync(
                action,
                    await _studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false),
                new[] { EMailSenderName },
                TagValues.OrangeButton(orangeButtonText, confirmationUrl));

        var displayUserName = userInfo.DisplayUserName(false, _displayUserSettingsHelper);

        await _messageService.SendAsync(MessageAction.UserSentPasswordChangeInstructions, _messageTarget.Create(userInfo.Id), auditEventDate, displayUserName);
    }

    #endregion

    #region User Email

    public async Task SendEmailChangeInstructionsAsync(UserInfo user, string email)
    {
        var confirmationUrl = await _commonLinkUtility.GetConfirmationEmailUrlAsync(email, ConfirmType.EmailChange, _authContext.CurrentAccount.ID, _authContext.CurrentAccount.ID);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangeEmail", GetCulture(user));

        var action = _coreBaseSettings.Personal
                         ? (_coreBaseSettings.CustomMode ? Actions.PersonalCustomModeEmailChangeV115 : Actions.PersonalEmailChangeV115)
                     : Actions.EmailChangeV115;

        await _client.SendNoticeToAsync(
                action,
                    await _studioNotifyHelper.RecipientFromEmailAsync(email, false),
                new[] { EMailSenderName },
                TagValues.OrangeButton(orangeButtonText, confirmationUrl),
                new TagValue(CommonTags.Culture, user.GetCulture().Name));
    }

    public async Task SendEmailActivationInstructionsAsync(UserInfo user, string email)
    {
        var confirmationUrl = await _commonLinkUtility.GetConfirmationEmailUrlAsync(email, ConfirmType.EmailActivation, null, user.Id);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonActivateEmail", GetCulture(user));

        await _client.SendNoticeToAsync(
                Actions.ActivateEmail,
                    await _studioNotifyHelper.RecipientFromEmailAsync(email, false),
                new[] { EMailSenderName },
                new TagValue(Tags.InviteLink, confirmationUrl),
                TagValues.OrangeButton(orangeButtonText, confirmationUrl),
                    new TagValue(Tags.UserDisplayName, (user.DisplayUserName(_displayUserSettingsHelper) ?? string.Empty).Trim()));
    }

    public async Task SendEmailRoomInviteAsync(string email, string roomTitle, string confirmationUrl, string culture = null)
    {
        var cultureInfo = string.IsNullOrEmpty(culture) ? CultureInfo.CurrentUICulture : new CultureInfo(culture);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);

        var tags = new List<ITagValue>
        { 
            new TagValue(Tags.Message, roomTitle),
            new TagValue(Tags.InviteLink, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        };

        await _client.SendNoticeToAsync(
            Actions.SaasRoomInvite,
                await _studioNotifyHelper.RecipientFromEmailAsync(email, false),
                new[] { EMailSenderName },
                tags.ToArray());
    }

    public async Task SendDocSpaceInviteAsync(string email, string confirmationUrl, string culture = "")
    {
        var cultureInfo = string.IsNullOrEmpty(culture) ? CultureInfo.CurrentUICulture : new CultureInfo(culture);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", cultureInfo);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", cultureInfo);

        var tags = new List<ITagValue>() 
        {
            new TagValue(Tags.InviteLink, confirmationUrl),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, _studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
            new TagValue(CommonTags.Culture, cultureInfo.Name)
        };

        await _client.SendNoticeToAsync(
            Actions.SaasDocSpaceInvite,
                await _studioNotifyHelper.RecipientFromEmailAsync(email, false),
                new[] { EMailSenderName },
                tags.ToArray());
    }

    #endregion

    public async Task SendMsgMobilePhoneChangeAsync(UserInfo userInfo)
    {
        var confirmationUrl = await _commonLinkUtility.GetConfirmationEmailUrlAsync(userInfo.Email.ToLower(), ConfirmType.PhoneActivation);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangePhone", GetCulture(userInfo));

        await _client.SendNoticeToAsync(
        Actions.PhoneChange,
           await _studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false),
        new[] { EMailSenderName },
        TagValues.OrangeButton(orangeButtonText, confirmationUrl));
    }

    public async Task SendMsgTfaResetAsync(UserInfo userInfo)
    {
        var confirmationUrl = _commonLinkUtility.GetFullAbsolutePath(string.Empty);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonChangeTfa", GetCulture(userInfo));

        await _client.SendNoticeToAsync(
        Actions.TfaChange,
           await _studioNotifyHelper.RecipientFromEmailAsync(userInfo.Email, false),
        new[] { EMailSenderName },
        TagValues.OrangeButton(orangeButtonText, confirmationUrl));
    }


    public async ValueTask UserHasJoinAsync()
    {
        if (!_coreBaseSettings.Personal)
        {
            await _client.SendNoticeAsync(Actions.UserHasJoin);
        }
    }

    public async Task SendJoinMsgAsync(string email, EmployeeType emplType)
    {
        var inviteUrl = await _commonLinkUtility.GetConfirmationEmailUrlAsync(email, ConfirmType.EmpInvite, (int)emplType)
                    + string.Format("&emplType={0}", (int)emplType);

        var orangeButtonText = WebstudioNotifyPatternResource.ButtonJoin;

        await _client.SendNoticeToAsync(
                Actions.JoinUsers,
                   await _studioNotifyHelper.RecipientFromEmailAsync(email, true),
                new[] { EMailSenderName },
                new TagValue(Tags.InviteLink, inviteUrl),
                TagValues.OrangeButton(orangeButtonText, inviteUrl));
    }

    public async Task UserInfoAddedAfterInviteAsync(UserInfo newUserInfo)
    {
        if (!_userManager.UserExists(newUserInfo))
        {
            return;
        }

        INotifyAction notifyAction;
        var footer = "social";

        if (_coreBaseSettings.Personal)
        {
            if (_coreBaseSettings.CustomMode)
            {
                notifyAction = Actions.PersonalCustomModeAfterRegistration1;
                footer = "personalCustomMode";
            }
            else
            {
                notifyAction = Actions.PersonalAfterRegistration1;
                footer = "personal";
            }
        }
        else if (_tenantExtra.Enterprise)
        {
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(_settingsManager);
            notifyAction = defaultRebranding
                               ? Actions.EnterpriseUserWelcomeV1
                                   : _coreBaseSettings.CustomMode
                                     ? Actions.EnterpriseWhitelabelUserWelcomeCustomModeV1
                                     : Actions.EnterpriseWhitelabelUserWelcomeV1;
            footer = null;
        }
        else if (_tenantExtra.Opensource)
        {
            notifyAction = Actions.OpensourceUserWelcomeV1;
            footer = "opensource";
        }
        else
        {
            notifyAction = Actions.SaasUserWelcomeV1;
        }

        var culture = GetCulture(newUserInfo);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborateDocSpace", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = _studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = _studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = _studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = _studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        await _client.SendNoticeToAsync(
        notifyAction,
           await _studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
        new[] { EMailSenderName },
        new TagValue(Tags.UserName, newUserInfo.FirstName.HtmlEncode()),
        new TagValue(Tags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, _commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours),
        new TagValue(CommonTags.TopGif, _studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
        new TagValue("IMG1", img1),
        new TagValue("IMG2", img2),
        new TagValue("IMG3", img3),
        new TagValue("IMG4", img4),
        new TagValue(CommonTags.Footer, footer));
    }

    public async Task GuestInfoAddedAfterInviteAsync(UserInfo newUserInfo)
    {
        if (!_userManager.UserExists(newUserInfo))
        {
            return;
        }

        INotifyAction notifyAction;
        var footer = "social";

        if (_tenantExtra.Enterprise)
        {
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(_settingsManager);
            notifyAction = defaultRebranding ? Actions.EnterpriseGuestWelcomeV1 : Actions.EnterpriseWhitelabelGuestWelcomeV1;
            footer = null;
        }
        else if (_tenantExtra.Opensource)
        {
            notifyAction = Actions.OpensourceGuestWelcomeV1;
            footer = "opensource";
        }
        else
        {
            notifyAction = Actions.SaasGuestWelcomeV1;
        }

        var culture = GetCulture(newUserInfo);
        var orangeButtonText = _tenantExtra.Enterprise
                              ? WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccessYourPortal", culture)
                              : WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccessYouWebOffice", culture);

        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        var img1 = _studioNotifyHelper.GetNotificationImageUrl("users.png");
        var img2 = _studioNotifyHelper.GetNotificationImageUrl("files.png");
        var img3 = _studioNotifyHelper.GetNotificationImageUrl("collaborate.png");
        var img4 = _studioNotifyHelper.GetNotificationImageUrl("chatgpt.png");

        await _client.SendNoticeToAsync(
        notifyAction,
           await _studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
        new[] { EMailSenderName },
        new TagValue(Tags.UserName, newUserInfo.FirstName.HtmlEncode()),
        new TagValue(Tags.MyStaffLink, GetMyStaffLink()),
            TagValues.OrangeButton(orangeButtonText, _commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/')),
            TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours),
        new TagValue(CommonTags.TopGif, _studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
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

        if (_tenantExtra.Enterprise)
        {
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(_settingsManager);
            notifyAction = defaultRebranding ? Actions.EnterpriseUserActivationV1 : Actions.EnterpriseWhitelabelUserActivationV1;
            footer = null;
        }
        else if (_tenantExtra.Opensource)
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

        await _client.SendNoticeToAsync(
        notifyAction,
           await _studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
        new[] { EMailSenderName },
        new TagValue(Tags.ActivateUrl, confirmationUrl),
        TagValues.OrangeButton(orangeButtonText, confirmationUrl), 
        TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours),
        new TagValue(CommonTags.TopGif, _studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
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

        if (_tenantExtra.Enterprise)
        {
            var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(_settingsManager);
            notifyAction = defaultRebranding ? Actions.EnterpriseGuestActivationV10 : Actions.EnterpriseWhitelabelGuestActivationV10;
            footer = null;
        }
        else if (_tenantExtra.Opensource)
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

        await _client.SendNoticeToAsync(
        notifyAction,
           await _studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
        new[] { EMailSenderName },
        new TagValue(Tags.ActivateUrl, confirmationUrl),
        TagValues.OrangeButton(orangeButtonText, confirmationUrl),
        TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours),
        new TagValue(CommonTags.TopGif, _studioNotifyHelper.GetNotificationImageUrl("join_docspace.gif")),
        new TagValue(Tags.UserName, newUserInfo.FirstName.HtmlEncode()),
        new TagValue(CommonTags.Footer, footer));
    }

    public async Task SendMsgProfileDeletionAsync(UserInfo user)
    {
        var confirmationUrl = await _commonLinkUtility.GetConfirmationEmailUrlAsync(user.Email, ConfirmType.ProfileRemove, _authContext.CurrentAccount.ID, _authContext.CurrentAccount.ID);
        var culture = GetCulture(user);
        var orangeButtonText = _coreBaseSettings.Personal ?
            WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture) :
            WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonRemoveProfile", culture);

        var action = _coreBaseSettings.Personal
                         ? (_coreBaseSettings.CustomMode ? Actions.PersonalCustomModeProfileDelete : Actions.PersonalProfileDelete)
                     : Actions.ProfileDelete;
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        await _client.SendNoticeToAsync(
        action,
           await _studioNotifyHelper.RecipientFromEmailAsync(user.Email, false),
        new[] { EMailSenderName },
        TagValues.OrangeButton(orangeButtonText, confirmationUrl),
        TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours),
        new TagValue(CommonTags.Culture, user.GetCulture().Name));
    }

    public async Task SendMsgReassignsCompletedAsync(Guid recipientId, UserInfo fromUser, UserInfo toUser)
    {
        await _client.SendNoticeToAsync(
        Actions.ReassignsCompleted,
            new[] { await _studioNotifyHelper.ToRecipientAsync(recipientId) },
            new[] { EMailSenderName },
            new TagValue(Tags.UserName, await _displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(Tags.FromUserName, fromUser.DisplayUserName(_displayUserSettingsHelper)),
            new TagValue(Tags.FromUserLink, await GetUserProfileLinkAsync(fromUser.Id)),
            new TagValue(Tags.ToUserName, toUser.DisplayUserName(_displayUserSettingsHelper)),
            new TagValue(Tags.ToUserLink, await GetUserProfileLinkAsync(toUser.Id)));
    }

    public async Task SendMsgReassignsFailedAsync(Guid recipientId, UserInfo fromUser, UserInfo toUser, string message)
    {
        await _client.SendNoticeToAsync(
        Actions.ReassignsFailed,
            new[] { await _studioNotifyHelper.ToRecipientAsync(recipientId) },
            new[] { EMailSenderName },
            new TagValue(Tags.UserName, await _displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(Tags.FromUserName, fromUser.DisplayUserName(_displayUserSettingsHelper)),
            new TagValue(Tags.FromUserLink, await GetUserProfileLinkAsync(fromUser.Id)),
            new TagValue(Tags.ToUserName, toUser.DisplayUserName(_displayUserSettingsHelper)),
            new TagValue(Tags.ToUserLink, await GetUserProfileLinkAsync(toUser.Id)),
            new TagValue(Tags.Message, message));
    }

    public async Task SendMsgRemoveUserDataCompletedAsync(Guid recipientId, UserInfo user, string fromUserName, long docsSpace, long crmSpace, long mailSpace, long talkSpace)
    {
        await _client.SendNoticeToAsync(
            _coreBaseSettings.CustomMode ? Actions.RemoveUserDataCompletedCustomMode : Actions.RemoveUserDataCompleted,
            new[] { await _studioNotifyHelper.ToRecipientAsync(recipientId) },
            new[] { EMailSenderName },
            new TagValue(Tags.UserName, await _displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(Tags.FromUserName, fromUserName.HtmlEncode()),
            new TagValue(Tags.FromUserLink, await GetUserProfileLinkAsync(user.Id)),
            new TagValue("DocsSpace", FileSizeComment.FilesSizeToString(docsSpace)),
            new TagValue("CrmSpace", FileSizeComment.FilesSizeToString(crmSpace)),
            new TagValue("MailSpace", FileSizeComment.FilesSizeToString(mailSpace)),
            new TagValue("TalkSpace", FileSizeComment.FilesSizeToString(talkSpace)));
    }

    public async Task SendMsgRemoveUserDataFailedAsync(Guid recipientId, UserInfo user, string fromUserName, string message)
    {
        await _client.SendNoticeToAsync(
        Actions.RemoveUserDataFailed,
            new[] { await _studioNotifyHelper.ToRecipientAsync(recipientId) },
            new[] { EMailSenderName },
            new TagValue(Tags.UserName, await _displayUserSettingsHelper.GetFullUserNameAsync(recipientId)),
            new TagValue(Tags.FromUserName, fromUserName.HtmlEncode()),
            new TagValue(Tags.FromUserLink, await GetUserProfileLinkAsync(user.Id)),
            new TagValue(Tags.Message, message));
    }

    public async Task SendAdminWelcomeAsync(UserInfo newUserInfo)
    {
        if (!_userManager.UserExists(newUserInfo))
        {
            return;
        }

        if (!newUserInfo.IsActive)
        {
            throw new ArgumentException("User is not activated yet!");
        }

        INotifyAction notifyAction;
        var tagValues = new List<ITagValue>();

        if (_tenantExtra.Enterprise)
        {
            return;
            //var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(_settingsManager);
            //notifyAction = defaultRebranding ? Actions.EnterpriseAdminWelcomeV1 : Actions.EnterpriseWhitelabelAdminWelcomeV1;
        }
        else if (_tenantExtra.Opensource)
        {
            return;
            //notifyAction = Actions.OpensourceAdminWelcomeV1;
            //tagValues.Add(new TagValue(CommonTags.Footer, "opensource"));
        }
        else
        {
            notifyAction = Actions.SaasAdminWelcomeV1;
            tagValues.Add(new TagValue(CommonTags.Footer, "common"));
        }

        tagValues.Add(new TagValue(Tags.UserName, newUserInfo.FirstName.HtmlEncode()));
        tagValues.Add(new TagValue(Tags.PricingPage, _commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/portal-payments")));
        tagValues.Add(TagValues.TrulyYours(_studioNotifyHelper, WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", GetCulture(newUserInfo))));
        tagValues.Add(new TagValue(CommonTags.TopGif, _studioNotifyHelper.GetNotificationImageUrl("discover_business_subscription.gif")));

        await _client.SendNoticeToAsync(
        notifyAction,
           await _studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, false),
        new[] { EMailSenderName },
        tagValues.ToArray());
    }

    #region Portal Deactivation & Deletion

    public async Task SendMsgPortalDeactivationAsync(Tenant t, string deactivateUrl, string activateUrl)
    {
        var u = await _userManager.GetUsersAsync(t.OwnerId);
        var culture = GetCulture(u);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonDeactivatePortal", culture);
        var bestReagardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        await _client.SendNoticeToAsync(
                Actions.PortalDeactivate,
                new IRecipient[] { u },
                new[] { EMailSenderName },
                new TagValue(Tags.ActivateUrl, activateUrl),
                TagValues.OrangeButton(orangeButtonText, deactivateUrl),
                TagValues.TrulyYours(_studioNotifyHelper, bestReagardsTxt),
                    new TagValue(Tags.OwnerName, u.DisplayUserName(_displayUserSettingsHelper)));
    }

    public async Task SendMsgPortalDeletionAsync(Tenant t, string url, bool showAutoRenewText)
    {
        var u = await _userManager.GetUsersAsync(t.OwnerId);
        var culture = GetCulture(u);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonDeletePortal", culture);
        var bestReagardsTxt = WebstudioNotifyPatternResource.ResourceManager.GetString("BestRegardsText", culture);

        await _client.SendNoticeToAsync(
                Actions.PortalDelete,
                new IRecipient[] { u },
                new[] { EMailSenderName },
                TagValues.OrangeButton(orangeButtonText, url),
                TagValues.TrulyYours(_studioNotifyHelper, bestReagardsTxt),
                new TagValue(Tags.AutoRenew, showAutoRenewText.ToString()),
                    new TagValue(Tags.OwnerName, u.DisplayUserName(_displayUserSettingsHelper)));
    }

    public async Task SendMsgPortalDeletionSuccessAsync(UserInfo owner, string url)
    {
        var culture = GetCulture(owner);
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonLeaveFeedback", culture);
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

        await _client.SendNoticeToAsync(
                Actions.PortalDeleteSuccessV1,
                new IRecipient[] { owner },
                new[] { EMailSenderName },
                TagValues.OrangeButton(orangeButtonText, url),
                TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours),
                new TagValue(CommonTags.TopGif, _studioNotifyHelper.GetNotificationImageUrl("docspace_deactivated.gif")),
                    new TagValue(Tags.OwnerName, owner.DisplayUserName(_displayUserSettingsHelper)));
    }

    #endregion

    public async Task SendMsgDnsChangeAsync(Tenant t, string confirmDnsUpdateUrl, string portalAddress, string portalDns)
    {
        var u = await _userManager.GetUsersAsync(t.OwnerId);

        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirmPortalAddressChange", GetCulture(u));

        await _client.SendNoticeToAsync(
                Actions.DnsChange,
                new IRecipient[] { u },
                new[] { EMailSenderName },
                new TagValue("ConfirmDnsUpdate", confirmDnsUpdateUrl),//TODO: Tag is deprecated and replaced by TagGreenButton
                TagValues.OrangeButton(orangeButtonText, confirmDnsUpdateUrl),
                new TagValue("PortalAddress", AddHttpToUrl(portalAddress)),
                new TagValue("PortalDns", AddHttpToUrl(portalDns ?? string.Empty)),
                    new TagValue(Tags.OwnerName, u.DisplayUserName(_displayUserSettingsHelper)));
    }

    public async Task SendMsgConfirmChangeOwnerAsync(UserInfo owner, UserInfo newOwner, string confirmOwnerUpdateUrl)
    {
        var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirmPortalOwnerUpdate", owner.GetCulture());

        await _client.SendNoticeToAsync(
        Actions.ConfirmOwnerChange,
        null,
        new IRecipient[] { owner },
        new[] { EMailSenderName },
        TagValues.OrangeButton(orangeButtonText, confirmOwnerUpdateUrl),
            new TagValue(Tags.UserName, newOwner.DisplayUserName(_displayUserSettingsHelper)),
            new TagValue(Tags.OwnerName, owner.DisplayUserName(_displayUserSettingsHelper)));
    }

    public async Task SendCongratulationsAsync(UserInfo u)
    {
        try
        {
            INotifyAction notifyAction;
            var footer = "common";

            if (_tenantExtra.Enterprise)
            {
                var defaultRebranding = await MailWhiteLabelSettings.IsDefaultAsync(_settingsManager);
                notifyAction = defaultRebranding ? Actions.EnterpriseAdminActivationV1 : Actions.EnterpriseWhitelabelAdminActivationV1;
                footer = null;
            }
            else if (_tenantExtra.Opensource)
            {
                notifyAction = Actions.OpensourceAdminActivationV1;
                footer = "opensource";
            }
            else
            {
                notifyAction = Actions.SaasAdminActivationV1;
            }

            var userId = u.Id;
            var confirmationUrl = await _commonLinkUtility.GetConfirmationEmailUrlAsync(u.Email, ConfirmType.EmailActivation, null, userId);

            await _settingsManager.SaveAsync(new FirstEmailConfirmSettings() { IsFirst = true });

            var culture = GetCulture(u);
            var orangeButtonText = WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfirmEmail", culture);
            var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

            await _client.SendNoticeToAsync(
            notifyAction,
               await _studioNotifyHelper.RecipientFromEmailAsync(u.Email, false),
            new[] { EMailSenderName },
            new TagValue(Tags.UserName, u.FirstName.HtmlEncode()),
            TagValues.OrangeButton(orangeButtonText, confirmationUrl),
            TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours),
            new TagValue(CommonTags.TopGif, _studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
            new TagValue(CommonTags.Footer, footer));
        }
        catch (Exception error)
        {
            _log.ErrorSendCongratulations(error);
        }
    }

    #region Personal

    public async Task SendInvitePersonalAsync(string email, string additionalMember = "")
    {
        var newUserInfo = await _userManager.GetUserByEmailAsync(email);
        if (_userManager.UserExists(newUserInfo))
        {
            return;
        }

        var lang = _coreBaseSettings.CustomMode
                       ? "ru-RU"
                       : CultureInfo.CurrentCulture.Name;

        var culture = _setupInfo.GetPersonalCulture(lang);

        var confirmUrl = await _commonLinkUtility.GetConfirmationEmailUrlAsync(email, ConfirmType.EmpInvite, (int)EmployeeType.RoomAdmin)
                     + "&emplType=" + (int)EmployeeType.RoomAdmin
                     + "&lang=" + culture.Key
                     + additionalMember;
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture.Value);

        await _client.SendNoticeToAsync(
            _coreBaseSettings.CustomMode ? Actions.PersonalCustomModeConfirmation : Actions.PersonalConfirmation,
           await _studioNotifyHelper.RecipientFromEmailAsync(email, false),
        new[] { EMailSenderName },
        new TagValue(Tags.InviteLink, confirmUrl),
            new TagValue(CommonTags.Footer, _coreBaseSettings.CustomMode ? "personalCustomMode" : "personal"),
        new TagValue(CommonTags.Culture, CultureInfo.CurrentUICulture.Name),
        TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours));
    }

    public async Task SendAlreadyExistAsync(string email)
    {
        var userInfo = await _userManager.GetUserByEmailAsync(email);
        if (!_userManager.UserExists(userInfo))
        {
            return;
        }

        var portalUrl = _commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/');

        var hash = (await _authentication.GetUserPasswordStampAsync(userInfo.Id)).ToString("s", CultureInfo.InvariantCulture);

        var linkToRecovery = await _commonLinkUtility.GetConfirmationEmailUrlAsync(userInfo.Email, ConfirmType.PasswordChange, hash, userInfo.Id);

        await _client.SendNoticeToAsync(
            _coreBaseSettings.CustomMode ? Actions.PersonalCustomModeAlreadyExist : Actions.PersonalAlreadyExist,
           await _studioNotifyHelper.RecipientFromEmailAsync(email, false),
        new[] { EMailSenderName },
        new TagValue(Tags.PortalUrl, portalUrl),
        new TagValue(Tags.LinkToRecovery, linkToRecovery),
            new TagValue(CommonTags.Footer, _coreBaseSettings.CustomMode ? "personalCustomMode" : "personal"),
        new TagValue(CommonTags.Culture, CultureInfo.CurrentUICulture.Name));
    }

    public async Task SendUserWelcomePersonalAsync(UserInfo newUserInfo)
    {
        var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", GetCulture(newUserInfo));
        await _client.SendNoticeToAsync(
            _coreBaseSettings.CustomMode ? Actions.PersonalCustomModeAfterRegistration1 : Actions.PersonalAfterRegistration1,
            await _studioNotifyHelper.RecipientFromEmailAsync(newUserInfo.Email, true),
        new[] { EMailSenderName },
            new TagValue(CommonTags.Footer, _coreBaseSettings.CustomMode ? "personalCustomMode" : "personal"),
            TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours));
    }

    #endregion

    #region Migration Portal

    public async Task PortalRenameNotifyAsync(Tenant tenant, string oldVirtualRootPath, string oldAlias)
    {
        var users = (await _userManager.GetUsersAsync())
                .Where(u => u.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated));

        try
        {
            _tenantManager.SetCurrentTenant(tenant);

            foreach (var u in users)
            {
                var culture = string.IsNullOrEmpty(u.CultureName) ? tenant.GetCulture() : u.GetCulture();
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                await _client.SendNoticeToAsync(
                    Actions.PortalRename,
                    new[] { await _studioNotifyHelper.ToRecipientAsync(u.Id) },
                    new[] { EMailSenderName },
                    _commonLinkUtility.GetFullAbsolutePath("").Replace(oldAlias, tenant.Alias),
                    new TagValue(Tags.PortalUrl, oldVirtualRootPath),
                    new TagValue(Tags.UserDisplayName, u.DisplayUserName(_displayUserSettingsHelper)));
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
        return _commonLinkUtility.GetFullAbsolutePath(_commonLinkUtility.GetMyStaff());
    }

    private async Task<string> GetUserProfileLinkAsync(Guid userId)
    {
        return _commonLinkUtility.GetFullAbsolutePath(await _commonLinkUtility.GetUserProfileAsync(userId));
    }

    private static string AddHttpToUrl(string url)
    {
        var httpPrefix = Uri.UriSchemeHttp + Uri.SchemeDelimiter;
        return !string.IsNullOrEmpty(url) && !url.StartsWith(httpPrefix) ? httpPrefix + url : url;
    }

    private async Task<string> GenerateActivationConfirmUrlAsync(UserInfo user)
    {
        var confirmUrl = await _commonLinkUtility.GetConfirmationEmailUrlAsync(user.Email, ConfirmType.Activation, user.Id, user.Id);

        return confirmUrl + $"&firstname={HttpUtility.UrlEncode(user.FirstName)}&lastname={HttpUtility.UrlEncode(user.LastName)}";
    }


    public async Task SendRegDataAsync(UserInfo u)
    {
        try
        {
            if (!_tenantExtra.Saas || !_coreBaseSettings.CustomMode)
            {
                return;
            }

            var settings = await _settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>();
            var salesEmail = settings.SalesEmail ?? _setupInfo.SalesEmail;

            if (string.IsNullOrEmpty(salesEmail))
            {
                return;
            }

            var recipient = new DirectRecipient(salesEmail, null, new[] { salesEmail }, false);

            await _client.SendNoticeToAsync(
            Actions.SaasCustomModeRegData,
            null,
            new IRecipient[] { recipient },
            new[] { EMailSenderName },
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
                    ? await _userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID)
                    : (await _userManager.GetUsersAsync()).Where(u => u.ActivationStatus.HasFlag(EmployeeActivationStatus.Activated));

        foreach (var u in users)
        {
            await _client.SendNoticeToAsync(
            action,
            null,
                new[] { await _studioNotifyHelper.ToRecipientAsync(u.Id) },
            new[] { EMailSenderName },
            new TagValue(Tags.UserName, u.FirstName.HtmlEncode()),
            new TagValue(Tags.PortalUrl, serverRootPath),
            new TagValue(Tags.ControlPanelUrl, GetControlPanelUrl(serverRootPath)));
        }
    }

    private string GetControlPanelUrl(string serverRootPath)
    {
        var controlPanelUrl = _setupInfo.ControlPanelUrl;

        if (string.IsNullOrEmpty(controlPanelUrl))
        {
            return string.Empty;
        }

        if (controlPanelUrl.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) ||
            controlPanelUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
        {
            return controlPanelUrl;
        }

        return serverRootPath + "/" + controlPanelUrl.TrimStart('~', '/').TrimEnd('/');
    }

    #endregion


    #region Zoom

    public async Task SendZoomWelcomeAsync(UserInfo u)
    {
        try
        {
            var culture = GetCulture(u);
            var txtTrulyYours = WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", culture);

            await _client.SendNoticeToAsync(
                Actions.ZoomWelcome,
                await _studioNotifyHelper.RecipientFromEmailAsync(u.Email, false),
                new[] { EMailSenderName },
                new TagValue(CommonTags.Culture, culture.Name),
                new TagValue(Tags.UserName, u.FirstName.HtmlEncode()),
                new TagValue(CommonTags.TopGif, _studioNotifyHelper.GetNotificationImageUrl("welcome.gif")),
                TagValues.TrulyYours(_studioNotifyHelper, txtTrulyYours));
        }
        catch (Exception error)
        {
            _log.ErrorSendCongratulations(error);
        }
    }

    #endregion

    private CultureInfo GetCulture(UserInfo user)
    {
        CultureInfo culture = null;

        if (!string.IsNullOrEmpty(user.CultureName))
        {
            culture = user.GetCulture();
        }

        if (culture == null)
        {
            culture = _tenantManager.GetCurrentTenant(false)?.GetCulture();
        }

        return culture;
    }
}
