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

namespace ASC.Web.Studio.Utility;

public enum ManagementType
{
    General = 0,
    Customization = 1,
    ProductsAndInstruments = 2,
    PortalSecurity = 3,
    AccessRights = 4,
    Backup = 5,
    LoginHistory = 6,
    AuditTrail = 7,
    LdapSettings = 8,
    ThirdPartyAuthorization = 9,
    SmtpSettings = 10,
    Statistic = 11,
    Monitoring = 12,
    SingleSignOnSettings = 13,
    Migration = 14,
    DeletionPortal = 15,
    HelpCenter = 16,
    DocService = 17,
    FullTextSearch = 18,
    WhiteLabel = 19,
    MailService = 20,
    Storage = 21,
    PrivacyRoom = 22
}

[Scope]
public class CommonLinkUtility(IHttpContextAccessor httpContextAccessor,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        TenantManager tenantManager,
        UserManager userManager,
        EmailValidationKeyProvider emailValidationKeyProvider,
        ILoggerProvider options)
    : BaseCommonLinkUtility(httpContextAccessor, coreBaseSettings, coreSettings, tenantManager, options)
{
    public const string ParamName_UserUserID = "uid";
    public const string AbsoluteAccountsPath = "/accounts/";
    public const string VirtualAccountsPath = "~/accounts/";

    public CommonLinkUtility(
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        TenantManager tenantManager,
        UserManager userManager,
        EmailValidationKeyProvider emailValidationKeyProvider,
        ILoggerProvider options) :
        this(null, coreBaseSettings, coreSettings, tenantManager, userManager, emailValidationKeyProvider, options)
    {
    }

    public string Logout
    {
        get { return ToAbsolute("~/auth.aspx") + "?t=logout"; }
    }

    public string GetDefault()
    {
        return VirtualRoot;
    }

    public string GetMyStaff()
    {
        return ToAbsolute("~/profile");
    }

    public string GetUnsubscribe()
    {
        return ToAbsolute("~/profile/notifications");
    }

    public string GetEmployees(EmployeeStatus empStatus = EmployeeStatus.Active)
    {
        return ToAbsolute("~/products/people/") +
               (empStatus == EmployeeStatus.Terminated ? "#type=disabled" : string.Empty);
    }

    public string GetDepartment(Guid depId)
    {
        return depId != Guid.Empty ? ToAbsolute("~/products/people/#group=") + depId : GetEmployees();
    }

    #region user profile link

    public async Task<string> GetUserProfileAsync(Guid userId, bool absolute = true)
    {
        if (!await userManager.UserExistsAsync(userId))
        {
            return GetEmployees();
        }
        
        var queryParams = userId != Guid.Empty ? await GetUserParamsPairAsync(userId) : HttpUtility.UrlEncode(userId.ToString().ToLowerInvariant());

        var url = absolute ? ToAbsolute(VirtualAccountsPath) : AbsoluteAccountsPath;
        url += "view/";
        url += queryParams;

        return url;
    }

    #endregion

    private async Task<string> GetUserParamsPairAsync(Guid userID)
    {
        return GetUserParamsPair(await userManager.GetUsersAsync(userID));
    }

    public string GetUserParamsPair(UserInfo user)
    {
        if (user == null || string.IsNullOrEmpty(user.UserName) || !userManager.UserExists(user))
        {
            return "";
        }

        return HttpUtility.UrlEncode(user.UserName.ToLowerInvariant());
    }

    public async Task<string> GetUserForumLinkAsync(SettingsManager settingsManager, AdditionalWhiteLabelSettingsHelperInit additionalWhiteLabelSettingsHelper, bool inCurrentCulture = true)
    {
        if (!(await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>()).UserForumEnabled)
        {
            return string.Empty;
        }

        var url = additionalWhiteLabelSettingsHelper.DefaultUserForumUrl;

        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        return GetRegionalUrl(url, inCurrentCulture ? CultureInfo.CurrentCulture.TwoLetterISOLanguageName : null);
    }
    
    #region Help Centr

    public async Task<string> GetHelpLinkAsync(SettingsManager settingsManager, AdditionalWhiteLabelSettingsHelperInit additionalWhiteLabelSettingsHelper, bool inCurrentCulture = true)
    {
        if (!(await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>()).HelpCenterEnabled)
        {
            return string.Empty;
        }

        var url = additionalWhiteLabelSettingsHelper.DefaultHelpCenterUrl;

        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        return GetRegionalUrl(url, inCurrentCulture ? CultureInfo.CurrentCulture.TwoLetterISOLanguageName : null);
    }

    public async Task<string> GetSupportLinkAsync(SettingsManager settingsManager, AdditionalWhiteLabelSettingsHelperInit additionalWhiteLabelSettingsHelper, bool inCurrentCulture = true)
    {
        if (!(await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>()).FeedbackAndSupportEnabled)
        {
            return string.Empty;
        }

        var url = additionalWhiteLabelSettingsHelper.DefaultFeedbackAndSupportUrl;

        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        return GetRegionalUrl(url, inCurrentCulture ? CultureInfo.CurrentCulture.TwoLetterISOLanguageName : null);
    }

    public string GetSiteLink(MailWhiteLabelSettingsHelper mailWhiteLabelSettingsHelper)
    {
        var url = mailWhiteLabelSettingsHelper.DefaultMailSiteUrl;

        return string.IsNullOrEmpty(url) ? string.Empty : url;
    }

    public string GetSupportEmail(MailWhiteLabelSettingsHelper mailWhiteLabelSettingsHelper)
    {
        var url = mailWhiteLabelSettingsHelper.DefaultMailSupportEmail;

        return string.IsNullOrEmpty(url) ? string.Empty : url;
    }

    public string GetSalesEmail(AdditionalWhiteLabelSettingsHelperInit additionalWhiteLabelSettingsHelper)
    {
        var mail = additionalWhiteLabelSettingsHelper.DefaultMailSalesEmail;

        return string.IsNullOrEmpty(mail) ? string.Empty : mail;
    }

    public async Task<string> GetFeedbackAndSupportLink(SettingsManager settingsManager, bool inCurrentCulture = true)
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>();

        return !settings.FeedbackAndSupportEnabled || string.IsNullOrEmpty(settings.FeedbackAndSupportUrl)
            ? string.Empty
            : GetRegionalUrl(settings.FeedbackAndSupportUrl, inCurrentCulture ? CultureInfo.CurrentCulture.TwoLetterISOLanguageName : null);
    }

    #endregion

    #region confirm links

    public async Task<(string, string)> GetConfirmationUrlAndKeyAsync(string email, ConfirmType confirmType, object postfix = null, Guid userId = default)
    {
        var url = GetFullAbsolutePath($"confirm/{confirmType}?{GetTokenWithoutKey(email, confirmType, userId)}");

        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var key = emailValidationKeyProvider.GetEmailKey(tenantId, email + confirmType + (postfix ?? ""));
        return (url, key);
    }

    public async Task<string> GetConfirmationEmailUrlAsync(string email, ConfirmType confirmType, object postfix = null, Guid userId = default)
    {
        return GetFullAbsolutePath(await GetConfirmationUrlRelativeAsync(email, confirmType, postfix, userId));
    }

    public string GetConfirmationUrl(string key, ConfirmType confirmType, Guid userId = default)
    {
        return GetFullAbsolutePath(GetConfirmationUrlRelative(key, confirmType, userId));
    }

    public async Task<string> GetConfirmationUrlRelativeAsync(string email, ConfirmType confirmType, object postfix = null, Guid userId = default)
    {
        return GetConfirmationUrlRelative(await _tenantManager.GetCurrentTenantIdAsync(), email, confirmType, postfix, userId);
    }

    public string GetConfirmationUrlRelative(int tenantId, string email, ConfirmType confirmType, object postfix = null, Guid userId = default)
    {
        return $"confirm/{confirmType}?{GetToken(tenantId, email, confirmType, postfix, userId)}";
    }

    public string GetConfirmationUrlRelative(string key, ConfirmType confirmType, Guid userId = default)
    {
        return $"confirm/{confirmType}?type={confirmType}&key={key}&uid={userId}";
    }

    public string GetTokenWithoutKey(string email, ConfirmType confirmType, Guid userId = default)
    {
        var link = $"type={confirmType}";

        if (!string.IsNullOrEmpty(email))
        {
            link += $"&email={HttpUtility.UrlEncode(email)}";
        }

        if (userId != Guid.Empty)
        {
            link += $"&uid={userId}";
        }

        return link;
    }

    public string GetToken(int tenantId, string email, ConfirmType confirmType, object postfix = null, Guid userId = default)
    {
        var validationKey = emailValidationKeyProvider.GetEmailKey(tenantId, email + confirmType + (postfix ?? ""));

        var link = $"type={confirmType}&key={validationKey}";

        if (!string.IsNullOrEmpty(email))
        {
            link += $"&email={HttpUtility.UrlEncode(email)}";
        }

        if (userId != Guid.Empty)
        {
            link += $"&uid={userId}";
        }

        return link;
    }

    #endregion

}
