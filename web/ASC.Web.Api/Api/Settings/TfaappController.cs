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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Api.Controllers.Settings;

public class TfaappController(
    MessageService messageService,
    StudioNotifyService studioNotifyService,
    UserManager userManager,
    AuthContext authContext,
    CookiesManager cookiesManager,
    PermissionContext permissionContext,
    SettingsManager settingsManager,
    TfaManager tfaManager,
    WebItemManager webItemManager,
    CommonLinkUtility commonLinkUtility,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    StudioSmsNotificationSettingsHelper studioSmsNotificationSettingsHelper,
    TfaAppAuthSettingsHelper tfaAppAuthSettingsHelper,
    SmsProviderManager smsProviderManager,
    IFusionCache fusionCache,
    InstanceCrypto instanceCrypto,
    Signature signature,
    SecurityContext securityContext,
    TenantManager tenantManager,
    AuditEventsRepository auditEventsRepository,
    UserSocketManager userSocketManager)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Lists the two-factor authentication methods this portal offers, with the state of each one. The list carries
    /// at most two entries: `sms`, present only when the SMS method is enabled in the portal's configuration, and
    /// `app`, present only when the authenticator-application method is enabled there, so an empty list means neither
    /// method is offered here. Any authenticated member may call it, and what it returns is the portal-wide policy,
    /// not the caller's own linked credential. This is a read-only, idempotent call. For every entry `enabled` says
    /// whether that method is the current policy, `available` says whether it can actually be switched on (for `sms`
    /// that also requires a configured SMS provider), `trustedIps` lists the addresses and ranges exempt from the
    /// challenge, and `mandatoryUsers` and `mandatoryGroups` list the accounts that have to pass it even from a
    /// trusted address. Change the policy with `PUT api/2.0/settings/tfaapp`, and read the caller's own backup codes
    /// with `GET api/2.0/settings/tfaappcodes`.
    /// </remarks>
    /// <summary>Get the TFA settings</summary>
    /// <path>api/2.0/settings/tfaapp</path>
    /// <collection>list</collection>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "The TFA methods the portal offers, with the portal-wide state of each", typeof(IEnumerable<TfaSettingsDto>))]
    [HttpGet("tfaapp")]
    public async Task<IEnumerable<TfaSettingsDto>> GetTfaSettings()
    {
        var result = new List<TfaSettingsDto>();

        var smsVisible = studioSmsNotificationSettingsHelper.IsVisibleSettings;
        var smsEnable = smsVisible && smsProviderManager.Enabled();
        var tfaVisible = tfaAppAuthSettingsHelper.IsVisibleSettings;

        var tfaAppSettings = await settingsManager.LoadAsync<TfaAppAuthSettings>();
        var tfaSmsSettings = await settingsManager.LoadAsync<StudioSmsNotificationSettings>();

        if (smsVisible)
        {
            result.Add(new TfaSettingsDto
            {
                Enabled = tfaSmsSettings.EnableSetting && smsProviderManager.Enabled(),
                Id = "sms",
                Title = Resource.ButtonSmsEnable,
                Available = smsEnable,
                MandatoryUsers = tfaSmsSettings.MandatoryUsers,
                MandatoryGroups = tfaSmsSettings.MandatoryGroups,
                TrustedIps = tfaSmsSettings.TrustedIps
            });
        }

        if (tfaVisible)
        {
            result.Add(new TfaSettingsDto
            {
                Enabled = tfaAppSettings.EnableSetting,
                Id = "app",
                Title = Resource.ButtonTfaAppEnable,
                Available = true,
                MandatoryUsers = tfaAppSettings.MandatoryUsers,
                MandatoryGroups = tfaAppSettings.MandatoryGroups,
                TrustedIps = tfaAppSettings.TrustedIps
            });
        }

        return result;
    }

    /// <remarks>
    /// Verifies a two-factor authentication code for the account named in the confirmation link being used, and
    /// completes that account's pending TFA step. The call is reachable only with a confirmation token carrying the
    /// `TfaActivation` or `TfaAuth` role, issued by `GET api/2.0/settings/tfaapp/confirm` or by the login flow; an
    /// ordinary bearer token is refused. Both a code from the authenticator application and one of the account's
    /// unused backup codes are accepted, and a backup code is spent by the check. The call mutates state: it signs
    /// the account in, clears the confirmation cookie so the link cannot be replayed, and on the very first
    /// activation it generates the backup codes later returned by `GET api/2.0/settings/tfaappcodes`. Pass
    /// `session=true` to keep that sign-in for the browser session only instead of a persistent one. It answers
    /// `true` only for that first activation and `false` when an application was already linked. A wrong code is
    /// rejected as an invalid request, and further attempts are refused once the portal's login attempt limit is
    /// reached. The call also works while the portal's payment is overdue.
    /// </remarks>
    /// <summary>Validate the TFA code</summary>
    /// <path>api/2.0/settings/tfaapp/validate</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "`true` when the code completed a first activation and backup codes were generated, `false` when an application was already linked", typeof(bool))]
    [HttpPost("tfaapp/validate")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "TfaActivation,TfaAuth")]
    public async Task<bool> TfaValidateAuthCode(TfaValidateRequestsDto inDto)
    {
        await securityContext.AuthByClaimAsync();
        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        securityContext.Logout();

        var (result, _) = await tfaManager.ValidateAuthCodeAsync(user, inDto.Code, session: inDto.Session);
        await userSocketManager.UpdateUserAsync(userManager.GetUsers(authContext.CurrentAccount.ID));

        var request = QueryHelpers.ParseQuery(Request.Headers["confirm"]);
        var type = request.TryGetValue("type", out var value) ? (string)value : "";
        cookiesManager.ClearCookies(CookiesType.ConfirmKey, $"_{type}");

        return result;
    }

    /// <remarks>
    /// Returns the confirmation link the current user has to follow to pass the portal's two-factor authentication
    /// step, together with the confirmation cookie that link depends on. Any authenticated member may call it, always
    /// for their own account, and TFA has to be required for that account by the portal policy already, otherwise the
    /// response body is empty. Which link comes back depends on the method. With the SMS method it is a phone
    /// activation link while the account has no activated mobile number and a phone authorization link afterwards,
    /// and only `url` is filled in. With the authenticator-application method the response also carries `cookieName`
    /// and `cookieValue`, and the call mutates state by issuing a fresh confirmation key and setting that cookie; the
    /// link then points at activation while no application is linked, or after the previous link was reset, and at
    /// re-verification once one is linked. Hand the code obtained through that flow to
    /// `POST api/2.0/settings/tfaapp/validate`. The portal-wide policy behind all of this is read with
    /// `GET api/2.0/settings/tfaapp`.
    /// </remarks>
    /// <summary>Get TFA confirmation data</summary>
    /// <path>api/2.0/settings/tfaapp/confirm</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "The confirmation link for the caller's TFA step, with the cookie filled in for the authenticator method", typeof(TfaConfirmDataDto))]
    [HttpGet("tfaapp/confirm")]
    public async Task<TfaConfirmDataDto> GetTfaConfirmData()
    {
        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (studioSmsNotificationSettingsHelper.IsVisibleSettings && await studioSmsNotificationSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            var confirmType = string.IsNullOrEmpty(user.MobilePhone) ||
                            user.MobilePhoneActivationStatus == MobilePhoneActivationStatus.NotActivated
                                ? ConfirmType.PhoneActivation
                                : ConfirmType.PhoneAuth;

            return new TfaConfirmDataDto { Url = commonLinkUtility.GetConfirmationEmailUrl(user.Email, confirmType) };
        }

        if (tfaAppAuthSettingsHelper.IsVisibleSettings && await tfaAppAuthSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            var tfaExpired = await TfaAppUserSettings.TfaExpiredAndResetAsync(settingsManager, auditEventsRepository, user.Id);
            var confirmType = tfaExpired || !await TfaAppUserSettings.EnableForUserAsync(settingsManager, authContext.CurrentAccount.ID)
                ? ConfirmType.TfaActivation
                : ConfirmType.TfaAuth;

            var itemId = $"_{confirmType}";
            var (url, key) = commonLinkUtility.GetConfirmationUrlAndKey(user.Id, confirmType);
            await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, key, true, itemId);

            return new TfaConfirmDataDto
            {
                Url = url,
                CookieName = cookiesManager.GetConfirmCookiesName(itemId),
                CookieValue = key
            };
        }

        return null;
    }

    /// <remarks>
    /// Sets the portal-wide two-factor authentication policy: `type` `1` switches on the SMS method, `2` switches on
    /// the authenticator application, and `0` turns TFA off, as does any unknown value. The two methods are mutually
    /// exclusive, so switching one on switches the other off. The caller has to be the portal owner or a DocSpace
    /// administrator; other members are refused, and a request that names the owner's account in `id` or in
    /// `mandatoryUsers` is refused unless `id` carries the caller's own account. `trustedIps` takes single addresses,
    /// inclusive ranges and CIDR blocks, and an unparseable entry is rejected as an invalid request; accounts listed
    /// in `mandatoryUsers` or `mandatoryGroups` still have to pass the challenge even from a trusted address.
    /// Switching a method on is disruptive: it resets the portal's authentication cookies, so every session on the
    /// portal, the caller's own included, has to sign in again. The answer is `true` when a method was switched on
    /// and `false` when TFA was turned off. Use `PUT api/2.0/settings/tfaappwithlink` instead to receive the caller's
    /// own confirmation link in the same step.
    /// </remarks>
    /// <summary>Update the TFA settings</summary>
    /// <path>api/2.0/settings/tfaapp</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "`true` when the SMS or the authenticator method was switched on, `false` when TFA was turned off", typeof(bool))]
    [SwaggerResponse(405, "The requested method is not enabled on this portal, or the SMS method has no configured provider")]
    [HttpPut("tfaapp")]
    public async Task<bool> UpdateTfaSettings(TfaRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var ownerId = tenantManager.GetCurrentTenant().OwnerId;
        if ((inDto.Id == ownerId || (inDto.MandatoryUsers != null && inDto.MandatoryUsers.Contains(ownerId)))
            && inDto.Id != authContext.CurrentAccount.ID)
        {
            throw new InvalidOperationException(Resource.ErrorAccessDenied);
        }

        var result = false;

        MessageAction action;

        switch (inDto.Type)
        {
            case TfaRequestsDtoType.Sms:
                if (!await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync())
                {
                    throw new CustomHttpException(HttpStatusCode.MethodNotAllowed, Resource.SmsNotAvailable);
                }

                if (!smsProviderManager.Enabled())
                {
                    throw new CustomHttpException(HttpStatusCode.MethodNotAllowed, Resource.SmsNotAvailable);
                }

                var smsSettings = await settingsManager.LoadAsync<StudioSmsNotificationSettings>();
                SetSettingsProperty(smsSettings);
                await settingsManager.SaveAsync(smsSettings);

                action = MessageAction.TwoFactorAuthenticationEnabledBySms;

                if (await tfaAppAuthSettingsHelper.GetEnable())
                {
                    await tfaAppAuthSettingsHelper.SetEnable(false);
                }

                result = true;

                break;

            case TfaRequestsDtoType.App:
                if (!tfaAppAuthSettingsHelper.IsVisibleSettings)
                {
                    throw new CustomHttpException(HttpStatusCode.MethodNotAllowed, Resource.TfaAppNotAvailable);
                }

                var appSettings = await settingsManager.LoadAsync<TfaAppAuthSettings>();
                SetSettingsProperty(appSettings);
                await settingsManager.SaveAsync(appSettings);


                action = MessageAction.TwoFactorAuthenticationEnabledByTfaApp;

                if (await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync() && await studioSmsNotificationSettingsHelper.GetEnable())
                {
                    await studioSmsNotificationSettingsHelper.SetEnable(false);
                }

                result = true;

                break;

            default:
                if (await tfaAppAuthSettingsHelper.GetEnable())
                {
                    await tfaAppAuthSettingsHelper.SetEnable(false);
                }

                if (await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync() && await studioSmsNotificationSettingsHelper.GetEnable())
                {
                    await studioSmsNotificationSettingsHelper.SetEnable(false);
                }

                action = MessageAction.TwoFactorAuthenticationDisabled;

                break;
        }

        if (result)
        {
            await cookiesManager.ResetTenantCookieAsync();
        }

        messageService.Send(action);
        return result;

        void SetSettingsProperty<T>(TfaSettingsBase<T> settings) where T : class, ISettings<T>
        {
            settings.EnableSetting = true;
            settings.TrustedIps = inDto.TrustedIps ?? [];
            settings.MandatoryUsers = inDto.MandatoryUsers ?? [];
            settings.MandatoryGroups = inDto.MandatoryGroups ?? [];
        }
    }

    /// <remarks>
    /// Applies the same portal-wide two-factor authentication change as `PUT api/2.0/settings/tfaapp` and
    /// additionally returns the confirmation link the caller needs to pass the new challenge, so an administrator who
    /// has just switched TFA on can go straight to setting it up for themselves. The caller has to be the portal
    /// owner or a DocSpace administrator, and a request that names the owner's account in `id` or in `mandatoryUsers`
    /// is refused unless `id` carries the caller's own account. Every effect of the plain call applies here too: the
    /// methods are mutually exclusive, `type` `0` turns TFA off, `trustedIps` and the two mandatory lists behave the
    /// same way, and switching a method on resets the portal's authentication cookies, so all sessions have to sign
    /// in again. The answer is an empty string whenever there is no link to hand out: when the request turned TFA
    /// off, and when the caller is exempt from the challenge, most often because their own address is in the
    /// `trustedIps` list of that very request. The cookie the link depends on is not returned here, read it with
    /// `GET api/2.0/settings/tfaapp/confirm`.
    /// </remarks>
    /// <summary>Update TFA settings with a link</summary>
    /// <path>api/2.0/settings/tfaappwithlink</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "The caller's own confirmation link, or an empty string when TFA was turned off or the caller is exempt", typeof(string))]
    [SwaggerResponse(403, "The caller is neither the portal owner nor a DocSpace administrator, or is placing the owner under the policy")]
    [SwaggerResponse(405, "The requested method is not enabled on this portal, or the SMS method has no configured provider")]
    [HttpPut("tfaappwithlink")]
    public async Task<string> UpdateTfaSettingsLink(TfaRequestsDto inDto)
    {
        if (await UpdateTfaSettings(inDto))
        {
            // No confirmation data when the caller is exempt from the TFA they just switched on -
            // a trusted IP, most often their own address added in the very same request.
            var data = await GetTfaConfirmData();
            return data?.Url ?? string.Empty;
        }

        return string.Empty;
    }

    /// <remarks>
    /// Issues the secret the current user has to enter in an authenticator application before the
    /// authenticator-application method can be used, both as a scannable QR-code image and as a key for manual entry.
    /// The call is reachable only with a confirmation token carrying the `TfaActivation` role, obtained from
    /// `GET api/2.0/settings/tfaapp/confirm` or from the login flow; an ordinary bearer token is refused. The
    /// authenticator method has to be enabled on the portal and be its current policy, and the account must have no
    /// application linked yet: for an already-linked account the call answers 405, so reset the credential first with
    /// `PUT api/2.0/settings/tfaappnewapp`. Accounts flagged as outsiders are refused. Repeating the call is safe and
    /// hands back the same secret for the account, so the QR code and the manual key always describe one and the same
    /// credential. `qrCodeSetupImageUrl` is a base64 `data:` URL of a PNG image, and `account` is the label the
    /// application will show. Finish the setup by sending a code from the application to
    /// `POST api/2.0/settings/tfaapp/validate`.
    /// </remarks>
    /// <summary>Generate the TFA setup code</summary>
    /// <path>api/2.0/settings/tfaapp/setup</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "The account label, the manual entry key and the QR-code image for linking an authenticator application", typeof(TfaSetupCodeDto))]
    [SwaggerResponse(405, "The authenticator method is not enabled on this portal, or the account already has an application linked")]
    [HttpGet("tfaapp/setup")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "TfaActivation")]
    public async Task<TfaSetupCodeDto> TfaAppGenerateSetupCode()
    {
        await securityContext.AuthByClaimAsync();
        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (!tfaAppAuthSettingsHelper.IsVisibleSettings ||
            !(await settingsManager.LoadAsync<TfaAppAuthSettings>()).EnableSetting ||
            await TfaAppUserSettings.EnableForUserAsync(settingsManager, currentUser.Id))
        {
            throw new CustomHttpException(HttpStatusCode.MethodNotAllowed, Resource.TfaAppNotAvailable);
        }

        if (await userManager.IsOutsiderAsync(currentUser))
        {
            throw new InvalidOperationException("Not available.");
        }

        return TfaSetupCodeDto.FromSetupCode(await tfaManager.GenerateSetupCodeAsync(currentUser));
    }

    /// <remarks>
    /// Returns the one-time backup codes of the current user's authenticator-application credential, each with the
    /// flag that says whether it has been spent. A backup code is accepted in place of a code from the application
    /// when signing in, and every code works exactly once, so this list is what a member falls back on after losing
    /// access to their authenticator. Any authenticated member may call it, always for their own account: there is no
    /// way to read someone else's codes. The authenticator method has to be enabled on the portal and an application
    /// has to be linked to the account already, otherwise the call answers 405; link one through
    /// `GET api/2.0/settings/tfaapp/confirm` and `POST api/2.0/settings/tfaapp/validate`. Accounts flagged as
    /// outsiders are refused. This is a read-only, idempotent call: the codes are generated once, when the
    /// application is first linked, and the whole set is replaced by `PUT api/2.0/settings/tfaappnewcodes`. The
    /// default configuration issues five codes of six characters, and a portal may be configured for a different
    /// number and length.
    /// </remarks>
    /// <summary>Get the TFA backup codes</summary>
    /// <path>api/2.0/settings/tfaappcodes</path>
    /// <collection>list</collection>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "The caller's backup codes, each with the flag showing whether it has already been used", typeof(IEnumerable<TfaAppCodeDto>))]
    [SwaggerResponse(405, "The authenticator method is not enabled on this portal, or the caller has no application linked")]
    [HttpGet("tfaappcodes")]
    public async Task<IEnumerable<TfaAppCodeDto>> GetTfaAppCodes()
    {
        var currentUserId = authContext.CurrentAccount.ID;

        if (!tfaAppAuthSettingsHelper.IsVisibleSettings ||
            !(await settingsManager.LoadAsync<TfaAppAuthSettings>()).EnableSetting ||
            !await TfaAppUserSettings.EnableForUserAsync(settingsManager, currentUserId))
        {
            throw new CustomHttpException(HttpStatusCode.MethodNotAllowed, Resource.TfaAppNotAvailable);
        }

        if (await userManager.IsOutsiderAsync(currentUserId))
        {
            throw new InvalidOperationException("Not available.");
        }

        return (await settingsManager.LoadForCurrentUserAsync<TfaAppUserSettings>())
            .CodesSetting.Select(r => new TfaAppCodeDto
            {
                IsUsed =r.IsUsed,
                Code = r.GetEncryptedCode(instanceCrypto, signature)
            }).ToList();
    }

    /// <remarks>
    /// Replaces the current user's one-time backup codes with a freshly generated set and returns it. Use it once the
    /// previous codes have been spent or may have leaked: the whole old set stops being accepted the moment this call
    /// succeeds, so store the new codes before leaving the response. Any authenticated member may call it, always for
    /// their own account. The authenticator method has to be enabled on the portal and an application has to be
    /// linked to the account already, otherwise the call answers 405, and accounts flagged as outsiders are refused.
    /// The call mutates state and is not idempotent: every invocation issues another set and discards the one before
    /// it, so a retry after a timeout returns codes different from those the first attempt generated. The codes come
    /// back unused, five of them of six characters with the default configuration, and a portal may be configured for
    /// a different number and length. Read the current set without changing it through
    /// `GET api/2.0/settings/tfaappcodes`. The authenticator secret itself is untouched, so the linked application
    /// keeps working.
    /// </remarks>
    /// <summary>Regenerate the TFA backup codes</summary>
    /// <path>api/2.0/settings/tfaappnewcodes</path>
    /// <collection>list</collection>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "The newly generated backup codes, all unused, replacing the caller's previous set", typeof(IEnumerable<TfaAppCodeDto>))]
    [SwaggerResponse(405, "The authenticator method is not enabled on this portal, or the caller has no application linked")]
    [HttpPut("tfaappnewcodes")]
    public async Task<IEnumerable<TfaAppCodeDto>> UpdateTfaAppCodes()
    {
        var currentUserId = authContext.CurrentAccount.ID;
        var currentUser = await userManager.GetUsersAsync(currentUserId);

        if (!tfaAppAuthSettingsHelper.IsVisibleSettings || !await TfaAppUserSettings.EnableForUserAsync(settingsManager, currentUserId))
        {
            throw new CustomHttpException(HttpStatusCode.MethodNotAllowed, Resource.TfaAppNotAvailable);
        }

        if (await userManager.IsOutsiderAsync(currentUserId))
        {
            throw new InvalidOperationException("Not available.");
        }

        var codes = (await tfaManager.GenerateBackupCodesAsync())
            .Select(r => new TfaAppCodeDto
            {
                IsUsed =r.IsUsed,
                Code = r.GetEncryptedCode(instanceCrypto, signature)
            }).ToList();

        messageService.Send(MessageAction.UserConnectedTfaApp, MessageTarget.Create(currentUserId), currentUser.DisplayUserName(false, displayUserSettingsHelper));
        return codes;
    }

    /// <remarks>
    /// Detaches the authenticator application from an account, so that the account has to link a new one before it
    /// can sign in again. `id` has to name an existing account: an empty or unknown value is refused. Passing the
    /// caller's own ID resets their own credential and returns the activation link they should follow next; passing
    /// another member's ID is allowed for the portal owner only, and every other caller, a DocSpace administrator
    /// included, is refused. The account has to have an application linked and the authenticator method has to be
    /// enabled on the portal, otherwise the call answers 405. The call is destructive: the account's backup codes are
    /// dropped together with the credential and all of its sessions are signed out. For another member the portal
    /// also emails them that their TFA was reset, and the answer is then an empty string. The portal-wide policy is
    /// not touched, so TFA stays required and the account sets up an application again through
    /// `GET api/2.0/settings/tfaapp/confirm`; lift the requirement for everyone with `PUT api/2.0/settings/tfaapp`.
    /// </remarks>
    /// <summary>Unlink the TFA application</summary>
    /// <path>api/2.0/settings/tfaappnewapp</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "The activation link when the caller reset their own application, or an empty string when another member's was reset", typeof(string))]
    [SwaggerResponse(403, "The caller is not the portal owner, or the account cannot be resolved from `id`")]
    [SwaggerResponse(405, "The authenticator method is not enabled on this portal, or the account has no application linked")]
    [HttpPut("tfaappnewapp")]
    public async Task<string> UnlinkTfaApp(TfaRequestsDto inDto)
    {
        var id = inDto?.Id ?? Guid.Empty;
        var isMe = id.Equals(Guid.Empty) || id.Equals(authContext.CurrentAccount.ID);

        var user = await userManager.GetUsersAsync(id);

        if (user.Id == Constants.LostUser.Id)
        {
            throw new InvalidOperationException(Resource.ErrorAccessDenied);
        }

        if (!isMe && !await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser))
        {
            throw new InvalidOperationException(Resource.ErrorAccessDenied);
        }

        var tenant = tenantManager.GetCurrentTenant();
        if (!isMe && tenant.OwnerId != authContext.CurrentAccount.ID)
        {
            throw new InvalidOperationException(Resource.ErrorAccessDenied);
        }

        if (!tfaAppAuthSettingsHelper.IsVisibleSettings || !await TfaAppUserSettings.EnableForUserAsync(settingsManager, user.Id))
        {
            throw new CustomHttpException(HttpStatusCode.MethodNotAllowed, Resource.TfaAppNotAvailable);
        }

        if (await userManager.IsOutsiderAsync(user) || user.Status == EmployeeStatus.Terminated)
        {
            throw new InvalidOperationException("Not available.");
        }

        await TfaAppUserSettings.DisableForUserAsync(settingsManager, user.Id);
        messageService.Send(MessageAction.UserDisconnectedTfaApp, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));
        await userSocketManager.UpdateUserAsync(user);

        await cookiesManager.ResetUserCookieAsync(user.Id);
        if (isMe)
        {
            var (url, key) = commonLinkUtility.GetConfirmationUrlAndKey(user.Id, ConfirmType.TfaActivation);
            await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, key, true, $"_{ConfirmType.TfaActivation}");
            return url;
        }

        await studioNotifyService.SendMsgTfaResetAsync(user);
        return string.Empty;
    }
}
