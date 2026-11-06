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
    /// Returns the two-factor authentication methods the portal offers, with the portal-wide state of each. The list
    /// has at most two entries, `sms` and `app`. Each entry appears only if its method is available on the portal. An
    /// empty list means the portal offers neither method. Any signed-in user can call it. The call is read-only. It
    /// returns the portal policy, not the caller's own linked app. To change the policy, use
    /// `PUT api/2.0/settings/tfaapp`. To read your own backup codes, use `GET api/2.0/settings/tfaappcodes`.
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
    /// Checks a two-factor code for the account of the confirmation link. Then it completes the pending TFA step. Sign
    /// the request with the confirmation token of a `TfaActivation` or `TfaAuth` link. The sign-in flow or
    /// `GET api/2.0/settings/tfaapp/confirm` issues that link. With an ordinary sign-in token, the call returns 403. It
    /// accepts a code from the app or an unused backup code, which it spends. The call changes data. It signs the
    /// account in and clears the confirmation cookie, so the link cannot be used again. On the first activation, it
    /// also generates the backup codes. It returns `true` for the first activation, and `false` if an app was already
    /// linked. If the authenticator method is off on the portal, the call checks no code and returns `false`. If the
    /// code is wrong, the call returns 400. If the account exceeds the portal login attempt limit within a minute, the
    /// call returns 403. If `code` is empty or has only spaces, the call returns 500. The call works even when the
    /// portal payment is overdue.
    /// </remarks>
    /// <summary>Validate the TFA code</summary>
    /// <path>api/2.0/settings/tfaapp/validate</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "`true` when the code completed a first activation and backup codes were generated, `false` when an application was already linked", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read or has no `code`, or the code matches neither the authenticator application nor an unused backup code")]
    [SwaggerResponse(403, "The account the confirmation link was issued for has used up the portal's login attempt limit for TFA codes")]
    [SwaggerResponse(500, "The code is `null`, empty or consists of whitespace only")]
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
    /// Returns the confirmation link that the current user follows to pass the portal two-factor step. Any signed-in
    /// user can call it, always for their own account. If the portal policy does not require TFA from the caller, the
    /// response body is empty. With the SMS method, the call is read-only and fills only `url`. The link activates a
    /// phone while the account has no activated number. After that, it authorizes the phone. With the authenticator
    /// method, the call changes data. It issues a new confirmation key and sets it as a cookie. Each call replaces the
    /// previous key. The link activates an app while none is linked, and verifies a code once one is linked. If the
    /// portal last turned TFA off and the app was linked over a day ago, the call unlinks it. To send the code from
    /// that flow, use `POST api/2.0/settings/tfaapp/validate`. To read the portal policy, use
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
    /// Sets the portal-wide two-factor authentication policy. With `type` `1`, it switches on the SMS method, and with
    /// `2`, the authenticator app. Any other `type`, including `0`, turns TFA off. Switching one method on switches the
    /// other off. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other
    /// callers get 403. If `id` or `mandatoryUsers` names the portal owner while `id` is not the caller, the call
    /// returns 403. The call changes data and replaces the trusted and mandatory lists. Switching a method on signs out
    /// every session on the portal, including the caller's. A repeated call saves the same policy and signs everyone
    /// out again. It returns `true` if it switched a method on, and `false` if it turned TFA off. To also get the
    /// caller's own confirmation link, use `PUT api/2.0/settings/tfaappwithlink`. If the method is unavailable on the
    /// portal or SMS has no provider, the call returns 405. The call returns 400 for an unreadable `type` or a
    /// malformed ID. It also returns 400 if a `trustedIps` entry is not an address, range, or CIDR block. It works even
    /// when the portal payment is overdue.
    /// </remarks>
    /// <summary>Update the TFA settings</summary>
    /// <path>api/2.0/settings/tfaapp</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "`true` when the SMS or the authenticator method was switched on, `false` when TFA was turned off", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read, `type` is `null`, an empty string or a string that is neither a number nor built from the names `None`, `Sms` and `App`, or a `trustedIps` entry is not a single address, an inclusive range or a CIDR block")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or names the owner's account in `id` or `mandatoryUsers` while `id` is not the caller's own")]
    [SwaggerResponse(405, "The requested method is not enabled on this portal, or the SMS method has no configured provider")]
    [AllowNotPayment]
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
            case TfaType.Sms:
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

            case TfaType.App:
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
    /// Sets the portal-wide two-factor policy as `PUT api/2.0/settings/tfaapp` does. It also returns the caller's own
    /// confirmation link. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. If `id` or `mandatoryUsers` names the portal owner while `id` is not the caller, the call
    /// returns 403. Any `type` other than `1` and `2` turns TFA off. The call changes data. Switching a method on signs
    /// out every session on the portal. A repeated call saves the same policy and signs everyone out again. The call
    /// returns an empty string if it turned TFA off or the caller is exempt. Usually, the caller is exempt through
    /// their own address in `trustedIps`. For the authenticator method, the call sets the link's cookie but does not
    /// return its value. To get the value, use `GET api/2.0/settings/tfaapp/confirm`, which issues a new link. If the
    /// method is unavailable on the portal or SMS has no provider, the call returns 405. The call returns 400 for an
    /// unreadable `type` or a malformed ID. It also returns 400 if a `trustedIps` entry is not an address, range, or
    /// CIDR block.
    /// </remarks>
    /// <summary>Update TFA settings with a link</summary>
    /// <path>api/2.0/settings/tfaappwithlink</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "The caller's own confirmation link, or an empty string when TFA was turned off or the caller is exempt", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read, `type` is `null`, an empty string or a string that is neither a number nor built from the names `None`, `Sms` and `App`, or a `trustedIps` entry is not a single address, an inclusive range or a CIDR block")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or names the owner's account in `id` or `mandatoryUsers` while `id` is not the caller's own")]
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
    /// Returns the secret that the current user enters in an authenticator app, as a QR code and a key. Sign the
    /// request with the confirmation token of a `TfaActivation` link. The sign-in flow or
    /// `GET api/2.0/settings/tfaapp/confirm` issues that link. The call refuses an ordinary sign-in token. The
    /// authenticator method must be the current portal policy, and the account must have no app linked. Otherwise, the
    /// call returns 405. To replace a linked app, first unlink it with `PUT api/2.0/settings/tfaappnewapp`. The first
    /// call changes data and creates the secret. Later calls return the same secret until the app is unlinked. To
    /// finish the setup, send a code from the app to `POST api/2.0/settings/tfaapp/validate`. The call works even when
    /// the portal payment is overdue.
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
    /// Returns the backup codes of the current user's authenticator app, including the spent ones. At sign-in, each
    /// backup code can replace a code from the app once. Any signed-in user can call it, always for their own account.
    /// There is no way to read the codes of another account. If the authenticator method is unavailable or off on the
    /// portal, or no app is linked, the call returns 405. To link an app, use `GET api/2.0/settings/tfaapp/confirm` and
    /// then `POST api/2.0/settings/tfaapp/validate`. The call is read-only. The portal generates the codes when the app
    /// is first linked. To replace the whole set, use `PUT api/2.0/settings/tfaappnewcodes`. By default, there are five
    /// codes of six characters. The installation can set another number and length.
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
    /// Replaces the current user's backup codes with a new set and returns it. Use it after the old codes are spent or
    /// may have leaked. Any signed-in user can call it, always for their own account. If the authenticator method is
    /// unavailable on the portal or no app is linked, the call returns 405. The call changes data, and the old codes
    /// stop working immediately. Store the new codes before you leave the response. Do not repeat it. Each call creates
    /// another set and discards the previous one, so a retry returns different codes. The new codes are unused. By
    /// default, there are five codes of six characters. The installation can set another number and length. The secret
    /// of the linked app does not change, so the app keeps working. To read the current set without a change, use
    /// `GET api/2.0/settings/tfaappcodes`.
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
    /// Unlinks the authenticator app from an account. While the portal requires TFA, the account must link a new app at
    /// its next sign-in. To reset your own app, pass your own ID in `id`. Only the portal owner can reset the app of
    /// another account. Other callers get 403, including DocSpace admins. The call also returns 403 for a missing or
    /// unknown `id`, the empty GUID, or a terminated account. If the authenticator method is unavailable on the portal
    /// or the account has no app linked, the call returns 405. The call changes data and cannot be undone. It deletes
    /// the app and the backup codes of the account. It also signs out all its sessions. A repeated call returns 405.
    /// For your own account, the call returns the activation link to follow next and sets its confirmation cookie. For
    /// another account, the call returns an empty string and emails its user about the reset. The portal policy does
    /// not change. The account links a new app through `GET api/2.0/settings/tfaapp/confirm`. To turn TFA off for
    /// everyone, use `PUT api/2.0/settings/tfaapp`. The call returns 400 if a body field is unreadable, such as an `id`
    /// that is not a GUID.
    /// </remarks>
    /// <summary>Unlink the TFA application</summary>
    /// <path>api/2.0/settings/tfaappnewapp</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "The activation link when the caller reset their own application, or an empty string when another member's was reset", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read, `type` is `null`, an empty string or a string that is neither a number nor built from the names `None`, `Sms` and `App`, or a `trustedIps` entry is not a single address, an inclusive range or a CIDR block, although this call reads only `id`")]
    [SwaggerResponse(403, "The caller is not the portal owner, the account cannot be resolved from `id`, or the account is terminated")]
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
