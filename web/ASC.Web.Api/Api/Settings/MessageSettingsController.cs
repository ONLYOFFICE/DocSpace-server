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

using Microsoft.AspNetCore.RateLimiting;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Api.Controllers.Settings;

public class MessageSettingsController(
    AuthContext authContext,
    SetupInfo setupInfo,
    MessageService messageService,
    StudioNotifyService studioNotifyService,
    UserManager userManager,
    TenantExtra tenantExtra,
    PermissionContext permissionContext,
    SettingsManager settingsManager,
    WebItemManager webItemManager,
    CustomNamingPeople customNamingPeople,
    IFusionCache fusionCache,
    IHttpContextAccessor httpContextAccessor,
    TenantManager tenantManager,
    CookiesManager cookiesManager,
    BruteForceLoginManager bruteForceLoginManager,
    CountPaidUserChecker countPaidUserChecker)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Turns on or off the contact form of the sign-in page for visitors who cannot get into the portal. The form
    /// sends their message to the DocSpace admins. Requires the EditPortalSettings permission, which the portal owner
    /// and DocSpace admins have. Other callers get 403. Send `turnOn` as `true` to show the form, or `false` to hide
    /// it. The change covers the whole portal and applies to the next sign-in page. The call changes data, and you
    /// can repeat it with the same body. It returns a localized confirmation message, not the stored flag. To read
    /// the flag without signing in, use `GET api/2.0/settings` and its `enableAdmMess` field. That field is also
    /// `true` while the portal's payment has lapsed, even if the form is turned off here. The form sends messages
    /// with `POST api/2.0/settings/sendadmmail`. This setting does not change the notifications that admins get as
    /// portal members. To subscribe to those, use `POST api/2.0/settings/notification`.
    /// </remarks>
    /// <summary>
    /// Enable or disable administrator messages
    /// </summary>
    /// <path>api/2.0/settings/messagesettings</path>
    [Tags("Settings / Messages")]
    [SwaggerResponse(200, "A localized message confirming that the administrator message setting has been saved", typeof(string))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpPost("messagesettings")]
    public async Task<string> EnableAdminMessageSettings(TurnOnAdminMessageSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await settingsManager.SaveAsync(new StudioAdminMessageSettings { Enable = inDto.TurnOn });

        messageService.Send(MessageAction.AdministratorMessageSettingsUpdated);

        return Resource.SuccessfullySaveSettingsMessage;
    }

    /// <remarks>
    /// Returns how long a sign-in session of the portal stays valid. The answer has `lifeTime` in minutes and the
    /// `enabled` flag that turns this limit on. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. The call is read-only. The pair is for the whole portal, not for
    /// the calling user. A portal that nobody has configured returns `lifeTime` 1440, one day, with `enabled` false.
    /// Read the two fields together. While `enabled` is false, the portal ignores the number and a session lasts a
    /// year. If `lifeTime` is 0 and `enabled` is true, sessions never expire on their own. If the installation hides
    /// the cookie section, the call returns the default pair instead of the stored one. The `cookieSettingsEnabled`
    /// field of `GET api/2.0/settings` has the same flag without the number. To change the pair, use
    /// `PUT api/2.0/settings/cookiesettings`.
    /// </remarks>
    /// <summary>
    /// Get the cookie lifetime settings
    /// </summary>
    /// <path>api/2.0/settings/cookiesettings</path>
    [Tags("Settings / Cookies")]
    [SwaggerResponse(200, "The authentication session lifetime of the portal in minutes together with the flag that says whether that limit is applied", typeof(CookieSettingsDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("cookiesettings")]
    public async Task<CookieSettingsDto> GetCookieSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var result = await cookiesManager.GetLifeTimeAsync();
        return new CookieSettingsDto
        {
            Enabled = result.Enabled,
            LifeTime = result.LifeTime
        };
    }

    /// <remarks>
    /// Changes how long a sign-in session of the portal stays valid. Send the limit in minutes as `lifeTime`, and turn
    /// it on with `enabled`. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins
    /// have. Other callers get 403. If the installation hides the cookie section, the call saves nothing and
    /// returns 402. The call changes a `lifeTime` over 9999 to 9999. A value of 0 or less clears the number. With
    /// `enabled` true, sessions then never expire on their own. Any positive `lifeTime` ends every session issued
    /// before the call. If `enabled` is also true, the portal closes their connections. The call signs the caller in
    /// again and returns a new session cookie. Use that cookie for later calls. The call changes data, and each
    /// repeat with a positive `lifeTime` ends all sessions again. It returns a localized confirmation message. To read
    /// the stored pair, use `GET api/2.0/settings/cookiesettings`.
    /// </remarks>
    /// <summary>
    /// Update the cookie lifetime settings
    /// </summary>
    /// <path>api/2.0/settings/cookiesettings</path>
    [Tags("Settings / Cookies")]
    [SwaggerResponse(200, "A localized message confirming that the session lifetime has been saved", typeof(string))]
    [SwaggerResponse(402, "The installation hides the cookie lifetime section, or the portal's payment has lapsed")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpPut("cookiesettings")]
    public async Task<string> UpdateCookieSettings(CookieSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!SetupInfo.IsVisibleSettings("CookieSettings"))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }

        await cookiesManager.SetLifeTimeAsync(inDto.LifeTime, inDto.Enabled);

        messageService.Send(MessageAction.CookieSettingsUpdated);

        return Resource.SuccessfullySaveSettingsMessage;
    }

    /// <remarks>
    /// Sends a message to the DocSpace admins from someone who cannot get into the portal. This is the contact form
    /// of the sign-in page. First, turn the form on with `POST api/2.0/settings/messagesettings`. While the portal's
    /// payment has lapsed, the form works without that. If the form is turned off, the call returns 500. The
    /// `enableAdmMess` field of `GET api/2.0/settings` shows whether the form works. Anyone can call it without
    /// signing in. The call returns 400 if `email` is not a valid address or `message` is empty without its markup.
    /// If the caller is not signed in and the installation has a CAPTCHA, `recaptchaResponse` must carry a solved
    /// challenge. If it is missing or not accepted, the call returns 403. The call queues the letter to the admins
    /// subscribed to admin notifications. So a success means that the portal accepted the message, not that anyone
    /// read it. It returns a localized confirmation message. Do not repeat it, since each call sends another letter.
    /// The portal limits attempts per network address. More attempts return 429.
    /// </remarks>
    /// <summary>
    /// Send a message to the administrator
    /// </summary>
    /// <path>api/2.0/settings/sendadmmail</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Messages")]
    [SwaggerResponse(200, "A localized message confirming that the message has been queued for the portal administrators", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read or has no `email` or `message`, the email address is malformed or longer than 255 characters, or the message is longer than 255 characters or empty once its markup is stripped")]
    [SwaggerResponse(403, "The caller is not signed in, the installation has a CAPTCHA configured, and `recaptchaResponse` is missing or not accepted")]
    [SwaggerResponse(429, "Too many contact attempts came from the same address within the rate-limit window")]
    [SwaggerResponse(500, "The contact form is switched off and the portal's payment has not lapsed")]
    [AllowAnonymous, AllowNotPayment]
    [HttpPost("sendadmmail")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<string> SendAdminMail(AdminMessageSettingsRequestsDto inDto)
    {
        var studioAdminMessageSettings = await settingsManager.LoadAsync<StudioAdminMessageSettings>();
        var enableAdmMess = studioAdminMessageSettings.Enable || await tenantExtra.IsNotPaidAsync();

        if (!enableAdmMess)
        {
            throw new MethodAccessException("Method not available");
        }

        if (!inDto.Email.TestEmailRegex())
        {
            throw new ArgumentException(Resource.ErrorNotCorrectEmail);
        }

        var message = HtmlUtil.ToPlainText(inDto.Message);

        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentException(Resource.ErrorEmptyMessage);
        }

        if (!authContext.IsAuthenticated && (!string.IsNullOrEmpty(setupInfo.HcaptchaPublicKey) || !string.IsNullOrEmpty(setupInfo.RecaptchaPublicKey)))
        {
            var requestIp = MessageSettings.GetIP(httpContextAccessor.HttpContext?.Request);
            var secretEmail = SetupInfo.IsSecretEmail(inDto.Email);

            var recaptchaPassed = secretEmail || await bruteForceLoginManager.CheckRecaptchaAsync(inDto.RecaptchaType, inDto.RecaptchaResponse, requestIp);

            if (!recaptchaPassed)
            {
                throw new RecaptchaException(Resource.RecaptchaInvalid);
            }
        }

        await studioNotifyService.SendMsgToAdminFromNotAuthUserAsync(inDto.Email, message, inDto.Culture);
        messageService.Send(MessageAction.ContactAdminMailSent);

        return Resource.AdminMessageSent;
    }

    /// <remarks>
    /// Sends an email with a join link to the address in the request. This is the self-registration behind the
    /// register link of the sign-in page. First, the portal needs a trusted-domain policy, which
    /// `POST api/2.0/settings/maildomainsettings` saves. Without one, the call returns 405 to every caller. The
    /// `enabledJoin` field of `GET api/2.0/settings` shows whether the portal has such a policy. Anyone can call it
    /// without signing in. The call returns 400 if `email` is not a valid ASCII address or already belongs to a
    /// member. It also returns 400 if the address is outside the trusted domains of the policy. The invitee becomes a
    /// member only after following the link. The trusted-domain settings make the new member a user or a room admin.
    /// When the portal's paid places are taken, the new member is always a user. If the installation limits
    /// invitations, each sent email spends one of `invitationLimit`. A success returns a localized confirmation
    /// message. Do not repeat the call. Each call sends another email. After 11 calls from one network address,
    /// each within two minutes of the last, the call returns 500.
    /// </remarks>
    /// <summary>
    /// Send an invitation email
    /// </summary>
    /// <path>api/2.0/settings/sendjoininvite</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Messages")]
    [SwaggerResponse(200, "A localized message confirming that the invitation with the join link has been sent", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read or has no `email`, the email address is malformed, internationalized or longer than 255 characters, lies outside the trusted domains, or already belongs to a member of the portal")]
    [SwaggerResponse(403, "The portal is not accepting requests while it is being restored, transferred or encrypted")]
    [SwaggerResponse(405, "The portal publishes no trusted-domain policy, so it has nothing to join")]
    [SwaggerResponse(500, "Eleven invitation requests from the same network address have already been counted, each less than two minutes after the one before")]
    [AllowAnonymous]
    [HttpPost("sendjoininvite")]
    public async Task<string> SendJoinInviteMail(AdminMessageBaseSettingsRequestsDto inDto)
    {
        try
        {
            var tenant = tenantManager.GetCurrentTenant();
            var email = inDto.Email;

            // Joining is what this endpoint serves - the "Register" link the login page shows an
            // anonymous visitor - so it exists only while the portal publishes a trusted-domain
            // policy, the same condition SettingsDto.EnabledJoin reports to that page. Saying so with
            // 405 rather than letting an unmapped MethodAccessException surface as 500.
            if (!(
                (tenant.TrustedDomainsType == TenantTrustedDomainsType.Custom &&
                tenant.TrustedDomains.Count > 0) ||
                tenant.TrustedDomainsType == TenantTrustedDomainsType.All))
            {
                throw new CustomHttpException(HttpStatusCode.MethodNotAllowed, "Method not available");
            }

            if (!email.TestEmailRegex() || email.TestEmailPunyCode())
            {
                throw new ArgumentException(Resource.ErrorNotCorrectEmail);
            }

            await CheckCache("sendjoininvite");

            var user = await userManager.GetUserByEmailAsync(email);
            if (!user.Id.Equals(Constants.LostUser.Id))
            {
                throw new ArgumentException(await customNamingPeople.Substitute<Resource>("ErrorEmailAlreadyExists"));
            }

            var trustedDomainSettings = await settingsManager.LoadAsync<StudioTrustedDomainSettings>();
            var employeeType = trustedDomainSettings.InviteAsUsers ? EmployeeType.User : EmployeeType.RoomAdmin;
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
                employeeType = EmployeeType.User;
            }

            switch (tenant.TrustedDomainsType)
            {
                case TenantTrustedDomainsType.Custom:
                    {
                        var address = new MailAddress(email);
                        if (tenant.TrustedDomains.Any(d => address.Address.EndsWith("@" + d.Replace("*", ""), StringComparison.InvariantCultureIgnoreCase)))
                        {
                            await studioNotifyService.SendJoinMsgAsync(email, employeeType, inDto.Culture, true);
                            messageService.Send(MessageInitiator.System, MessageAction.SentInviteInstructions, email);
                            return Resource.FinishInviteJoinEmailMessage;
                        }

                        throw new ArgumentException(Resource.ErrorEmailDomainNotAllowed);
                    }
                case TenantTrustedDomainsType.All:
                    {
                        await studioNotifyService.SendJoinMsgAsync(email, employeeType, inDto.Culture, true);
                        messageService.Send(MessageInitiator.System, MessageAction.SentInviteInstructions, email);
                        return Resource.FinishInviteJoinEmailMessage;
                    }
                default:
                    throw new ArgumentException(Resource.ErrorNotCorrectEmail);
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException(Resource.ErrorNotCorrectEmail);
        }
    }
}