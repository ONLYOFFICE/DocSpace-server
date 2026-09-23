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
    /// Switches on or off the contact form the sign-in page offers a visitor who cannot get into the portal, and
    /// which delivers their message to the portal administrators. The caller needs the portal-settings right of a
    /// DocSpace administrator - the portal owner and a DocSpace administrator qualify, any other member is refused.
    /// Send the new state as `turnOn`: `true` publishes the form, `false` hides it. The change covers the whole
    /// portal, applies to the next sign-in page without a restart, is recorded in the audit trail, and repeating the
    /// call with the same value leaves the portal as it is. What comes back is a localized confirmation message
    /// rather than the stored flag - read the flag as `enableAdmMess` from `GET api/2.0/settings`, which needs no
    /// token. That flag is also forced on while the portal's payment has lapsed, so it can report `true` on a portal
    /// where the form was switched off here. The form itself posts to `POST api/2.0/settings/sendadmmail` and this
    /// setting gates nothing else: the notifications administrators receive as portal members are subscribed
    /// separately with `POST api/2.0/settings/notification`.
    /// </remarks>
    /// <summary>
    /// Enable or disable administrator messages
    /// </summary>
    /// <path>api/2.0/settings/messagesettings</path>
    [Tags("Settings / Messages")]
    [SwaggerResponse(200, "A localized message confirming that the administrator message setting has been saved", typeof(string))]
    [HttpPost("messagesettings")]
    public async Task<string> EnableAdminMessageSettings(TurnOnAdminMessageSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await settingsManager.SaveAsync(new StudioAdminMessageSettings { Enable = inDto.TurnOn });

        messageService.Send(MessageAction.AdministratorMessageSettingsUpdated);

        return Resource.SuccessfullySaveSettingsMessage;
    }

    /// <remarks>
    /// Returns how long an authentication session of this portal stays valid: `lifeTime` in minutes together with the
    /// `enabled` flag that says whether that limit is applied at all. The caller needs the portal-settings right of a
    /// DocSpace administrator - the portal owner and a DocSpace administrator qualify, any other member is refused -
    /// and the call is read-only. The pair describes the whole portal rather than the calling user, and it is never
    /// empty: a portal nobody has configured answers `lifeTime` 1440, one day, with `enabled` false. Read the two
    /// fields together, because the number alone does not say how long a session lasts - while `enabled` is false the
    /// stored number is ignored and an issued session is honoured for a year, and `lifeTime` 0 with `enabled` true
    /// means a session that never expires on its own. On an installation whose configuration hides the cookie section
    /// the built-in default pair comes back instead of the stored one. `GET api/2.0/settings` carries the same flag
    /// as `cookieSettingsEnabled` without the number; change the pair with `PUT api/2.0/settings/cookiesettings`.
    /// </remarks>
    /// <summary>
    /// Get the cookie lifetime settings
    /// </summary>
    /// <path>api/2.0/settings/cookiesettings</path>
    [Tags("Settings / Cookies")]
    [SwaggerResponse(200, "The authentication session lifetime of the portal in minutes together with the flag that says whether that limit is applied", typeof(CookieSettingsDto))]
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
    /// Stores how long an authentication session of this portal stays valid: `lifeTime` in minutes together with the
    /// `enabled` flag that switches the limit on. The caller needs the portal-settings right of a DocSpace
    /// administrator - the portal owner and a DocSpace administrator qualify, any other member is refused - and on an
    /// installation whose configuration hides the cookie section nothing is stored and the call is answered with 402.
    /// A `lifeTime` above 9999 minutes is not rejected but clamped to 9999, while 0 or less clears the number
    /// instead, which with `enabled` true leaves sessions that never expire on their own. Any positive `lifeTime`
    /// raises the session version of the portal: every session issued before the call stops being accepted, and with
    /// `enabled` true the connections behind them are dropped as well. The caller is signed in again inside the same
    /// call and gets a fresh session cookie in the response, so a client that keeps sending the token it held before
    /// this call is the one locked out. The change is recorded in the audit trail. What comes back is a localized
    /// confirmation message; read the stored pair with `GET api/2.0/settings/cookiesettings`.
    /// </remarks>
    /// <summary>
    /// Update the cookie lifetime settings
    /// </summary>
    /// <path>api/2.0/settings/cookiesettings</path>
    [Tags("Settings / Cookies")]
    [SwaggerResponse(200, "A localized message confirming that the session lifetime has been saved", typeof(string))]
    [SwaggerResponse(402, "The installation hides the cookie lifetime section, or the portal's payment has lapsed")]
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
    /// Sends a message from someone who cannot get into the portal to its administrators - the contact form the
    /// sign-in page offers unauthenticated visitors. No token is needed. The form has to be published first with
    /// `POST api/2.0/settings/messagesettings` unless the portal's payment has lapsed, otherwise nothing is sent;
    /// `enableAdmMess` in `GET api/2.0/settings` reports whether the call is worth making. `email` is the address the
    /// administrators answer to and has to be a real address, and `message` is reduced to plain text first, so a body
    /// carrying nothing but markup counts as empty - either fault is refused with 400. When the caller is not signed
    /// in and this installation has a CAPTCHA configured, `recaptchaResponse` has to carry a solved challenge of the
    /// `recaptchaType` that `GET api/2.0/settings` publishes together with the site key, and a missing or stale
    /// answer refuses the call. `culture` picks the language of the letter. Delivery is queued and reaches the
    /// administrators subscribed to administrator notifications, so a confirmed call means accepted rather than read,
    /// and the answer is a localized confirmation. Attempts are rate limited per address and per operation, and
    /// further ones are refused with 429.
    /// </remarks>
    /// <summary>
    /// Send a message to the administrator
    /// </summary>
    /// <path>api/2.0/settings/sendadmmail</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Messages")]
    [SwaggerResponse(200, "A localized message confirming that the message has been queued for the portal administrators", typeof(string))]
    [SwaggerResponse(400, "The email address is malformed, or the message is empty once its markup is stripped")]
    [SwaggerResponse(429, "Too many contact attempts came from the same address within the rate-limit window")]
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
    /// Sends an invitation email with a join link to the address in the request - the self-registration the sign-in
    /// page's register link performs. No token is needed. The portal has to publish a trusted-domain policy first,
    /// saved with `POST api/2.0/settings/maildomainsettings`: without one there is nothing to join and every caller
    /// alike is answered with 405 - the same condition `GET api/2.0/settings` reports as `enabledJoin`. `email` has
    /// to be a real address written in ASCII rather than an internationalized one, must not already belong to a
    /// portal member, and, when the policy names domains rather than accepting all of them, has to end with one of
    /// them - each of those faults is refused with 400. `culture` picks the language of the letter. The invitation is
    /// not an account: the invitee becomes a member only after following the link, and the role it grants, user or
    /// room administrator, follows the trusted-domain settings and drops to user once the portal's paid places are
    /// taken. Where the installation caps invitations, an accepted call spends one of those counted by
    /// `invitationLimit`, and only about a dozen calls from one address in two minutes are accepted. What comes back
    /// is a localized confirmation.
    /// </remarks>
    /// <summary>
    /// Send an invitation email
    /// </summary>
    /// <path>api/2.0/settings/sendjoininvite</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Messages")]
    [SwaggerResponse(200, "A localized message confirming that the invitation with the join link has been sent", typeof(string))]
    [SwaggerResponse(400, "The email address is malformed or internationalized, lies outside the trusted domains, or already belongs to a member of the portal")]
    [SwaggerResponse(403, "The portal is not accepting requests while it is being restored, transferred or encrypted")]
    [SwaggerResponse(405, "The portal publishes no trusted-domain policy, so it has nothing to join")]
    [SwaggerResponse(429, "Too many invitation requests came from the same network address")]
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