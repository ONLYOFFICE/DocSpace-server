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

namespace ASC.Web.Api.Controllers.Settings;

public class MessageSettingsController(MessageService messageService,
        StudioNotifyService studioNotifyService,
        ApiContext apiContext,
        UserManager userManager,
        TenantExtra tenantExtra,
        PermissionContext permissionContext,
        SettingsManager settingsManager,
        WebItemManager webItemManager,
        CustomNamingPeople customNamingPeople,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor,
        TenantManager tenantManager,
        CookiesManager cookiesManager,
        CountPaidUserChecker countPaidUserChecker)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Displays the contact form on the "Sign In" page, allowing users to send a message to the DocSpace administrator in case they encounter any issues while accessing DocSpace.
    /// </summary>
    /// <short>
    /// Enable the administrator message settings
    /// </short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.AdminMessageSettingsRequestsDto, ASC.Web.Api" name="inDto">Request parameters for administrator message settings</param>
    /// <category>Messages</category>
    /// <returns type="System.Object, System">Message about the result of saving new settings</returns>
    /// <path>api/2.0/settings/messagesettings</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("messagesettings")]
    public async Task<object> EnableAdminMessageSettingsAsync(AdminMessageSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await settingsManager.SaveAsync(new StudioAdminMessageSettings { Enable = inDto.TurnOn });

        await messageService.SendAsync(MessageAction.AdministratorMessageSettingsUpdated);

        return Resource.SuccessfullySaveSettingsMessage;
    }

    /// <summary>
    /// Returns the cookies lifetime value in minutes.
    /// </summary>
    /// <short>
    /// Get cookies lifetime
    /// </short>
    /// <category>Cookies</category>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.CookieSettingsDto, ASC.Web.Api">Lifetime value in minutes</returns>
    /// <path>api/2.0/settings/cookiesettings</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("cookiesettings")]
    public async Task<CookieSettingsDto> GetCookieSettings()
    {
        var result = await cookiesManager.GetLifeTimeAsync(await tenantManager.GetCurrentTenantIdAsync());
        return new CookieSettingsDto
        {
            Enabled = result.Enabled,
            LifeTime = result.LifeTime
        };
    }

    /// <summary>
    /// Updates the cookies lifetime value in minutes.
    /// </summary>
    /// <short>
    /// Update cookies lifetime
    /// </short>
    /// <category>Cookies</category>
    /// <param type="ASC.Web.Api.Models.CookieSettingsRequestsDto, ASC.Web.Api" name="inDto">Cookies settings request parameters</param>
    /// <returns type="System.Object, System">Message about the result of saving new settings</returns>
    /// <path>api/2.0/settings/cookiesettings</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("cookiesettings")]
    public async Task<object> UpdateCookieSettings(CookieSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!SetupInfo.IsVisibleSettings("CookieSettings"))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "CookieSettings");
        }

        await cookiesManager.SetLifeTimeAsync(inDto.LifeTime, inDto.Enabled);

        await messageService.SendAsync(MessageAction.CookieSettingsUpdated);

        return Resource.SuccessfullySaveSettingsMessage;
    }

    /// <summary>
    /// Sends a message to the administrator email when unauthorized users encounter issues accessing DocSpace.
    /// </summary>
    /// <short>
    /// Send a message to the administrator
    /// </short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.AdminMessageSettingsRequestsDto, ASC.Web.Api" name="inDto">Request parameters for administrator message settings</param>
    /// <category>Messages</category>
    /// <returns type="System.Object, System">Message about the result of sending a message</returns>
    /// <path>api/2.0/settings/sendadmmail</path>
    /// <httpMethod>POST</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous, AllowNotPayment]
    [HttpPost("sendadmmail")]
    public async Task<object> SendAdmMailAsync(AdminMessageSettingsRequestsDto inDto)
    {
        var studioAdminMessageSettings = await settingsManager.LoadAsync<StudioAdminMessageSettings>();
        var enableAdmMess = studioAdminMessageSettings.Enable || (await tenantExtra.IsNotPaidAsync());

        if (!enableAdmMess)
        {
            throw new MethodAccessException("Method not available");
        }

        if (!inDto.Email.TestEmailRegex())
        {
            throw new Exception(Resource.ErrorNotCorrectEmail);
        }

        var message = HtmlUtil.ToPlainText(inDto.Message);

        if (string.IsNullOrEmpty(message))
        {
            throw new Exception(Resource.ErrorEmptyMessage);
        }

        CheckCache("sendadmmail");

        await studioNotifyService.SendMsgToAdminFromNotAuthUserAsync(inDto.Email, message);
        await messageService.SendAsync(MessageAction.ContactAdminMailSent);

        return Resource.AdminMessageSent;
    }

    /// <summary>
    /// Sends an invitation email with a link to the DocSpace.
    /// </summary>
    /// <short>
    /// Sends an invitation email
    /// </short>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.AdminMessageSettingsRequestsDto, ASC.Web.Api" name="inDto">Request parameters for administrator message settings</param>
    /// <category>Messages</category>
    /// <returns type="System.Object, System">Message about sending a link to confirm joining the DocSpace</returns>
    /// <path>api/2.0/settings/sendjoininvite</path>
    /// <httpMethod>POST</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [HttpPost("sendjoininvite")]
    public async Task<object> SendJoinInviteMail(AdminMessageSettingsRequestsDto inDto)
    {
        try
        {
            var tenant = await tenantManager.GetCurrentTenantAsync();
            var email = inDto.Email;
            if (!(
                (tenant.TrustedDomainsType == TenantTrustedDomainsType.Custom &&
                tenant.TrustedDomains.Count > 0) ||
                tenant.TrustedDomainsType == TenantTrustedDomainsType.All))
            {
                throw new MethodAccessException("Method not available");
            }

            if (!email.TestEmailRegex())
            {
                throw new Exception(Resource.ErrorNotCorrectEmail);
            }

            CheckCache("sendjoininvite");

            var user = await userManager.GetUserByEmailAsync(email);
            if (!user.Id.Equals(Constants.LostUser.Id))
            {
                throw new Exception(customNamingPeople.Substitute<Resource>("ErrorEmailAlreadyExists"));
            }

            var trustedDomainSettings = await settingsManager.LoadAsync<StudioTrustedDomainSettings>();
            var emplType = trustedDomainSettings.InviteAsUsers ? EmployeeType.User : EmployeeType.RoomAdmin;
            var enableInviteUsers = true;
            try
            {
                await countPaidUserChecker.CheckAppend();
            }
            catch (Exception)
            {
                enableInviteUsers = false;
            }

            if (!enableInviteUsers)
            {
                emplType = EmployeeType.User;
            }

            switch (tenant.TrustedDomainsType)
            {
                case TenantTrustedDomainsType.Custom:
                    {
                        var address = new MailAddress(email);
                        if (tenant.TrustedDomains.Any(d => address.Address.EndsWith("@" + d.Replace("*", ""), StringComparison.InvariantCultureIgnoreCase)))
                        {
                            await studioNotifyService.SendJoinMsgAsync(email, emplType);
                            await messageService.SendAsync(MessageInitiator.System, MessageAction.SentInviteInstructions, email);
                            return Resource.FinishInviteJoinEmailMessage;
                        }

                        throw new Exception(Resource.ErrorEmailDomainNotAllowed);
                    }
                case TenantTrustedDomainsType.All:
                    {
                        await studioNotifyService.SendJoinMsgAsync(email, emplType);
                        await messageService.SendAsync(MessageInitiator.System, MessageAction.SentInviteInstructions, email);
                        return Resource.FinishInviteJoinEmailMessage;
                    }
                default:
                    throw new Exception(Resource.ErrorNotCorrectEmail);
            }
        }
        catch (FormatException)
        {
            throw new Exception(Resource.ErrorNotCorrectEmail);
        }
    }
}
