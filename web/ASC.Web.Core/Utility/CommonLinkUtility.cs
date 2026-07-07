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

using NetEscapades.EnumGenerators;

namespace ASC.Web.Studio.Utility;

[EnumExtensions]
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
    IdentityServer = 23
}

[Scope]
public class CommonLinkUtility(
    IHttpContextAccessor httpContextAccessor,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        TenantManager tenantManager,
        UserManager userManager,
        InstanceCrypto instanceCrypto,
        EmailValidationKeyProvider emailValidationKeyProvider,
        ILoggerFactory loggerFactory,
        ExternalResourceSettingsHelper externalResourceSettingsHelper)
    : BaseCommonLinkUtility(httpContextAccessor, coreBaseSettings, coreSettings, tenantManager, loggerFactory)
{
    public const string ParamName_UserUserID = "uid";
    public const string AbsoluteAccountsPath = "/accounts/";
    public const string VirtualAccountsPath = "~/accounts/";

    public string Logout => ToAbsolute("~/auth.aspx") + "?t=logout";

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

    public string GetEmployees(EmployeeType employeeType = EmployeeType.User, EmployeeStatus empStatus = EmployeeStatus.Active)
    {
        return ToAbsolute(employeeType == EmployeeType.Guest ? "~/accounts/guests/filter" : "~/accounts/people/filter") +
               (empStatus == EmployeeStatus.Terminated ? $"?employeestatus={(int)EmployeeStatus.Terminated}" : string.Empty);
    }

    public string GetDepartment(Guid depId)
    {
        return depId != Guid.Empty ? ToAbsolute($"~/accounts/groups/{depId}/filter") : GetEmployees();
    }

    #region user profile link

    public async Task<string> GetUserProfileAsync(Guid userId)
    {
        if (userManager.IsSystemUser(userId))
        {
            return GetEmployees();
        }

        var user = await userManager.GetUsersAsync(userId);
        var employeeType = await userManager.GetUserTypeAsync(userId);
        var path = GetEmployees(employeeType);

        return $"{path}?search={HttpUtility.UrlEncode(user.Email?.ToLowerInvariant())}";
    }

    #endregion

    #region links to external resources

    public async Task<string> GetUserForumLinkAsync(SettingsManager settingsManager)
    {
        if (!(await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>()).UserForumEnabled)
        {
            return string.Empty;
        }

        var url = externalResourceSettingsHelper.Forum.GetDefaultRegionalDomain();

        return string.IsNullOrEmpty(url) ? string.Empty : url;
    }

    public async Task<string> GetHelpLinkAsync(SettingsManager settingsManager)
    {
        if (!(await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>()).HelpCenterEnabled)
        {
            return string.Empty;
        }

        var url = externalResourceSettingsHelper.Helpcenter.GetDefaultRegionalDomain();

        return string.IsNullOrEmpty(url) ? string.Empty : url;
    }

    public async Task<string> GetSupportLinkAsync(SettingsManager settingsManager)
    {
        if (!(await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>()).FeedbackAndSupportEnabled)
        {
            return string.Empty;
        }

        var url = externalResourceSettingsHelper.Support.GetDefaultRegionalDomain();

        return string.IsNullOrEmpty(url) ? string.Empty : url;
    }

    public string GetSiteLink()
    {
        var url = externalResourceSettingsHelper.Site.GetDefaultRegionalDomain();

        return string.IsNullOrEmpty(url) ? string.Empty : url;
    }

    public string GetSupportEmail()
    {
        var email = externalResourceSettingsHelper.Common.GetDefaultRegionalFullEntry("supportemail");

        return string.IsNullOrEmpty(email) ? string.Empty : email;
    }

    public string GetSalesEmail()
    {
        var email = externalResourceSettingsHelper.Common.GetDefaultRegionalFullEntry("paymentemail");

        return string.IsNullOrEmpty(email) ? string.Empty : email;
    }

    #endregion

    #region confirm links

    public string GetInvitationLink(string email, EmployeeType employeeType, Guid createdBy, string culture = null)
    {
        var tenant = _tenantManager.GetCurrentTenant();

        var link = GetConfirmationEmailUrl(email, ConfirmType.LinkInvite, employeeType.ToStringFast() + tenant.Alias, createdBy)
                   + $"&emplType={employeeType:d}";

        if (!string.IsNullOrEmpty(culture))
        {
            link += $"&culture={culture}";
        }

        return link;
    }

    public (string, string) GetConfirmationUrlAndKey(Guid userId, ConfirmType confirmType, object postfix = null)
    {
        var url = GetFullAbsolutePath($"confirm/{confirmType}?{GetTokenWithoutKey(userId, confirmType)}");

        var tenantId = _tenantManager.GetCurrentTenantId();
        var key = emailValidationKeyProvider.GetEmailKey(userId.ToString() + confirmType + (postfix ?? ""), tenantId);
        return (url, key);
    }

    public string GetConfirmationEmailUrl(string email, ConfirmType confirmType, object postfix = null, Guid userId = default)
    {
        return GetFullAbsolutePath(GetConfirmationUrlRelative(email, confirmType, postfix, userId));
    }

    public string GetConfirmationUrl(string key, ConfirmType confirmType, Guid userId = default)
    {
        return GetFullAbsolutePath(GetConfirmationUrlRelative(key, confirmType, userId));
    }

    public string GetConfirmationUrlRelative(string email, ConfirmType confirmType, object postfix = null, Guid userId = default)
    {
        return GetConfirmationUrlRelative(_tenantManager.GetCurrentTenantId(), email, confirmType, postfix, userId);
    }

    public string GetConfirmationUrlRelative(int tenantId, string email, ConfirmType confirmType, object postfix = null, Guid userId = default)
    {
        return $"confirm/{confirmType}?{GetToken(tenantId, email, confirmType, postfix, userId)}";
    }

    public string GetConfirmationUrlRelative(string key, ConfirmType confirmType, Guid userId = default)
    {
        return $"confirm/{confirmType}?type={confirmType}&key={key}&uid={userId}";
    }

    private string GetTokenWithoutKey(Guid userId, ConfirmType confirmType)
    {
        return $"type={confirmType}&uid={userId}";
    }

    public string GetToken(int tenantId, string email, ConfirmType confirmType, object postfix = null, Guid userId = default)
    {
        var validationKey = emailValidationKeyProvider.GetEmailKey(email + confirmType + (postfix ?? ""), tenantId);

        var link = $"type={confirmType}&key={validationKey}";

        if (!string.IsNullOrEmpty(email))
        {
            var encryptedEmail = instanceCrypto.Encrypt(email).Base64ToUrlSafe();
            link += $"&encemail={HttpUtility.UrlEncode(encryptedEmail)}";
        }

        if (userId != Guid.Empty)
        {
            link += $"&uid={userId}";
        }

        return link;
    }

    #endregion

}
