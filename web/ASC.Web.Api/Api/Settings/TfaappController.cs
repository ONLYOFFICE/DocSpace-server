﻿// (c) Copyright Ascensio System SIA 2009-2024
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

public class TfaappController(
    MessageService messageService,
    StudioNotifyService studioNotifyService,
    ApiContext apiContext,
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
    IMemoryCache memoryCache,
    InstanceCrypto instanceCrypto,
    Signature signature,
    SecurityContext securityContext,
    IHttpContextAccessor httpContextAccessor,
    TenantManager tenantManager)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns the current two-factor authentication settings.
    /// </summary>
    /// <short>Get the TFA settings</short>
    ///<path>api/2.0/settings/tfaapp</path>
    ///<collection>list</collection>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "TFA settings", typeof(IEnumerable<TfaSettingsDto>))]
    [HttpGet("tfaapp")]
    public async Task<IEnumerable<TfaSettingsDto>> GetTfaSettingsAsync()
    {
        var result = new List<TfaSettingsDto>();

        var SmsVisible = studioSmsNotificationSettingsHelper.IsVisibleSettings;
        var SmsEnable = SmsVisible && smsProviderManager.Enabled();
        var TfaVisible = tfaAppAuthSettingsHelper.IsVisibleSettings;

        var tfaAppSettings = await settingsManager.LoadAsync<TfaAppAuthSettings>();
        var tfaSmsSettings = await settingsManager.LoadAsync<StudioSmsNotificationSettings>();

        if (SmsVisible)
        {
            result.Add(new TfaSettingsDto
            {
                Enabled = tfaSmsSettings.EnableSetting && smsProviderManager.Enabled(),
                Id = "sms",
                Title = Resource.ButtonSmsEnable,
                Avaliable = SmsEnable,
                MandatoryUsers = tfaSmsSettings.MandatoryUsers,
                MandatoryGroups = tfaSmsSettings.MandatoryGroups,
                TrustedIps = tfaSmsSettings.TrustedIps
            });
        }

        if (TfaVisible)
        {
            result.Add(new TfaSettingsDto
            {
                Enabled = tfaAppSettings.EnableSetting,
                Id = "app",
                Title = Resource.ButtonTfaAppEnable,
                Avaliable = true,
                MandatoryUsers = tfaAppSettings.MandatoryUsers,
                MandatoryGroups = tfaAppSettings.MandatoryGroups,
                TrustedIps = tfaAppSettings.TrustedIps
            });
        }

        return result;
    }

    /// <summary>
    /// Validates the two-factor authentication code specified in the request.
    /// </summary>
    /// <short>Validate the TFA code</short>
    ///<path>api/2.0/settings/tfaapp/validate</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "True if the code is valid", typeof(bool))]
    [HttpPost("tfaapp/validate")]
    [AllowNotPayment]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "TfaActivation,TfaAuth,Everyone")]
    public async Task<bool> TfaValidateAuthCodeAsync(TfaValidateRequestsDto inDto)
    {
        await ApiContext.AuthByClaimAsync();
        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);
        securityContext.Logout();

        var result = await tfaManager.ValidateAuthCodeAsync(user, inDto.Code);

        var request = QueryHelpers.ParseQuery(_httpContextAccessor.HttpContext.Request.Headers["confirm"]);
        var type = request.TryGetValue("type", out var value) ? (string)value : "";
        cookiesManager.ClearCookies(CookiesType.ConfirmKey, $"_{type}");

        return result;
    }

    /// <summary>
    /// Returns the confirmation email URL for authorization via SMS or TFA application.
    /// </summary>
    /// <short>Get confirmation email</short>
    ///<path>api/2.0/settings/tfaapp/confirm</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "Confirmation email URL", typeof(object))]
    [HttpGet("tfaapp/confirm")]
    public async Task<object> TfaConfirmUrlAsync()
    {
        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (studioSmsNotificationSettingsHelper.IsVisibleSettings && await studioSmsNotificationSettingsHelper.TfaEnabledForUserAsync(user.Id))// && smsConfirm.ToLower() != "true")
        {
            var confirmType = string.IsNullOrEmpty(user.MobilePhone) ||
                            user.MobilePhoneActivationStatus == MobilePhoneActivationStatus.NotActivated
                                ? ConfirmType.PhoneActivation
                                : ConfirmType.PhoneAuth;

            return commonLinkUtility.GetConfirmationEmailUrl(user.Email, confirmType);
        }

        if (tfaAppAuthSettingsHelper.IsVisibleSettings && await tfaAppAuthSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            var confirmType = await TfaAppUserSettings.EnableForUserAsync(settingsManager, authContext.CurrentAccount.ID)
                ? ConfirmType.TfaAuth
                : ConfirmType.TfaActivation;

            var (url, key) = commonLinkUtility.GetConfirmationUrlAndKey(user.Email, confirmType);
            await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, key, true, $"_{confirmType}");
            return url;
        }

        return string.Empty;
    }

    /// <summary>
    /// Updates the two-factor authentication settings with the parameters specified in the request.
    /// </summary>
    /// <short>Update the TFA settings</short>
    ///<path>api/2.0/settings/tfaapp</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "True if the operation is successful", typeof(bool))]
    [SwaggerResponse(405, "SMS settings are not available/TFA application settings are not available")]
    [HttpPut("tfaapp")]
    public async Task<bool> TfaSettingsAsync(TfaRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var result = false;

        MessageAction action;

        switch (inDto.Type)
        {
            case TfaRequestsDtoType.Sms:
                if (!await studioSmsNotificationSettingsHelper.IsVisibleAndAvailableSettingsAsync())
                {
                    throw new Exception(Resource.SmsNotAvailable);
                }

                if (!smsProviderManager.Enabled())
                {
                    throw new MethodAccessException();
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
                    throw new Exception(Resource.TfaAppNotAvailable);
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

    /// <summary>
    /// Returns the confirmation email URL for updating TFA settings.
    /// </summary>
    /// <short>Get confirmation email for updating TFA settings</short>
    /// <path>api/2.0/settings/tfaappwithlink</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "Confirmation email URL", typeof(object))]
    [HttpPut("tfaappwithlink")]
    public async Task<object> TfaSettingsLink(TfaRequestsDto inDto)
    {
        if (await TfaSettingsAsync(inDto))
        {
            return await TfaConfirmUrlAsync();
        }

        return string.Empty;
    }

    /// <summary>
    /// Generates the setup TFA code for the current user.
    /// </summary>
    /// <short>Generate setup code</short>
    /// <path>api/2.0/settings/tfaapp/setup</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "Setup code", typeof(SetupCode))]
    [SwaggerResponse(405, "TFA application settings are not available")]
    [HttpGet("tfaapp/setup")]
    [Authorize(AuthenticationSchemes = "confirm", Roles = "TfaActivation")]
    public async Task<SetupCode> TfaAppGenerateSetupCodeAsync()
    {
        await ApiContext.AuthByClaimAsync();
        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (!tfaAppAuthSettingsHelper.IsVisibleSettings ||
            !(await settingsManager.LoadAsync<TfaAppAuthSettings>()).EnableSetting ||
            await TfaAppUserSettings.EnableForUserAsync(settingsManager, currentUser.Id))
        {
            throw new Exception(Resource.TfaAppNotAvailable);
        }

        if (await userManager.IsOutsiderAsync(currentUser))
        {
            throw new NotSupportedException("Not available.");
        }

        return await tfaManager.GenerateSetupCodeAsync(currentUser);
    }

    /// <summary>
    /// Returns the two-factor authentication application codes.
    /// </summary>
    /// <short>Get the TFA codes</short>
    /// <path>api/2.0/settings/tfaappcodes</path>
    /// <collection>list</collection>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "List of TFA application codes", typeof(IEnumerable<object>))]
    [SwaggerResponse(405, "TFA application settings are not available")]
    [HttpGet("tfaappcodes")]
    public async Task<IEnumerable<object>> TfaAppGetCodesAsync()
    {
        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (!tfaAppAuthSettingsHelper.IsVisibleSettings ||
            !(await settingsManager.LoadAsync<TfaAppAuthSettings>()).EnableSetting ||
            !await TfaAppUserSettings.EnableForUserAsync(settingsManager, currentUser.Id))
        {
            throw new Exception(Resource.TfaAppNotAvailable);
        }

        if (await userManager.IsOutsiderAsync(currentUser))
        {
            throw new NotSupportedException("Not available.");
        }

        return (await settingsManager.LoadForCurrentUserAsync<TfaAppUserSettings>()).CodesSetting.Select(r => new { r.IsUsed, Code = r.GetEncryptedCode(instanceCrypto, signature) }).ToList();
    }

    /// <summary>
    /// Requests the new backup codes for the two-factor authentication application.
    /// </summary>
    /// <short>Update the TFA codes</short>
    /// <path>api/2.0/settings/tfaappnewcodes</path>
    /// <collection>list</collection>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "New backup codes", typeof(IEnumerable<object>))]
    [SwaggerResponse(405, "TFA application settings are not available")]
    [HttpPut("tfaappnewcodes")]
    public async Task<IEnumerable<object>> TfaAppRequestNewCodesAsync()
    {
        var currentUser = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (!tfaAppAuthSettingsHelper.IsVisibleSettings || !await TfaAppUserSettings.EnableForUserAsync(settingsManager, currentUser.Id))
        {
            throw new Exception(Resource.TfaAppNotAvailable);
        }

        if (await userManager.IsOutsiderAsync(currentUser))
        {
            throw new NotSupportedException("Not available.");
        }

        var codes = (await tfaManager.GenerateBackupCodesAsync()).Select(r => new { r.IsUsed, Code = r.GetEncryptedCode(instanceCrypto, signature) }).ToList();
        messageService.Send(MessageAction.UserConnectedTfaApp, MessageTarget.Create(currentUser.Id), currentUser.DisplayUserName(false, displayUserSettingsHelper));
        return codes;
    }

    /// <summary>
    /// Unlinks the current two-factor authentication application from the user account specified in the request.
    /// </summary>
    /// <short>Unlink the TFA application</short>
    /// <path>api/2.0/settings/tfaappnewapp</path>
    [Tags("Settings / TFA settings")]
    [SwaggerResponse(200, "Login URL", typeof(object))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(405, "TFA application settings are not available")]
    [HttpPut("tfaappnewapp")]
    public async Task<object> TfaAppNewAppAsync(TfaRequestsDto inDto)
    {
        var id = inDto?.Id ?? Guid.Empty;
        var isMe = id.Equals(Guid.Empty) || id.Equals(authContext.CurrentAccount.ID);

        var user = await userManager.GetUsersAsync(id);

        if (!isMe && !await permissionContext.CheckPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser))
        {
            throw new SecurityAccessDeniedException(Resource.ErrorAccessDenied);
        }

        var tenant = tenantManager.GetCurrentTenant();
        if (!isMe && tenant.OwnerId != authContext.CurrentAccount.ID)
        {
            throw new SecurityAccessDeniedException(Resource.ErrorAccessDenied);
        }

        if (!tfaAppAuthSettingsHelper.IsVisibleSettings || !await TfaAppUserSettings.EnableForUserAsync(settingsManager, user.Id))
        {
            throw new Exception(Resource.TfaAppNotAvailable);
        }

        if (await userManager.IsOutsiderAsync(user))
        {
            throw new NotSupportedException("Not available.");
        }

        await TfaAppUserSettings.DisableForUserAsync(settingsManager, user.Id);
        messageService.Send(MessageAction.UserDisconnectedTfaApp, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));

        await cookiesManager.ResetUserCookieAsync(user.Id);
        if (isMe)
        {
            var (url, key) = commonLinkUtility.GetConfirmationUrlAndKey(user.Email, ConfirmType.TfaActivation);
            await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, key, true, $"_{ConfirmType.TfaActivation}");
            return url;
        }

        await studioNotifyService.SendMsgTfaResetAsync(user);
        return string.Empty;
    }
}