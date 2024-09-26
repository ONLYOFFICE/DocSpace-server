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
    /// <category>TFA settings</category>
    /// <returns type="ASC.Web.Api.ApiModel.RequestsDto.TfaSettingsDto, ASC.Web.Api">TFA settings</returns>
    ///<path>api/2.0/settings/tfaapp</path>
    ///<httpMethod>GET</httpMethod>
    ///<collection>list</collection>
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
    /// <category>TFA settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.TfaValidateRequestsDto, ASC.Web.Api" name="inDto">TFA validation request parameters</param>
    /// <returns type="System.Boolean, System">True if the code is valid</returns>
    ///<path>api/2.0/settings/tfaapp/validate</path>
    ///<httpMethod>POST</httpMethod>
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
        var type = request.TryGetValue("type", out var value) ? value.FirstOrDefault() : "";
        cookiesManager.ClearCookies(CookiesType.ConfirmKey, $"_{type}");

        return result;
    }

    /// <summary>
    /// Returns the confirmation email URL for authorization via SMS or TFA application.
    /// </summary>
    /// <short>Get confirmation email</short>
    /// <category>TFA settings</category>
    /// <returns type="System.Object, System">Confirmation email URL</returns>
    ///<path>api/2.0/settings/tfaapp/confirm</path>
    ///<httpMethod>GET</httpMethod>
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

            return await commonLinkUtility.GetConfirmationEmailUrlAsync(user.Email, confirmType);
        }

        if (tfaAppAuthSettingsHelper.IsVisibleSettings && await tfaAppAuthSettingsHelper.TfaEnabledForUserAsync(user.Id))
        {
            var confirmType = await TfaAppUserSettings.EnableForUserAsync(settingsManager, authContext.CurrentAccount.ID)
                ? ConfirmType.TfaAuth
                : ConfirmType.TfaActivation;

            var (url, key) = await commonLinkUtility.GetConfirmationUrlAndKeyAsync(user.Email, confirmType);
            await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, key, true, $"_{confirmType}");
            return url;
        }

        return string.Empty;
    }

    /// <summary>
    /// Updates the two-factor authentication settings with the parameters specified in the request.
    /// </summary>
    /// <short>Update the TFA settings</short>
    /// <category>TFA settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.TfaRequestsDto, ASC.Web.Api" name="inDto">TFA settings request parameters</param>
    /// <returns type="System.Boolean, System">True if the operation is successful</returns>
    ///<path>api/2.0/settings/tfaapp</path>
    ///<httpMethod>PUT</httpMethod>
    [HttpPut("tfaapp")]
    public async Task<bool> TfaSettingsAsync(TfaRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var result = false;

        MessageAction action;

        switch (inDto.Type)
        {
            case "sms":
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

            case "app":
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

        await messageService.SendAsync(action);
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
    /// <category>TFA settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.TfaRequestsDto, ASC.Web.Api" name="inDto">TFA settings request parameters</param>
    /// <returns type="System.Object, System">Confirmation email URL</returns>
    /// <path>api/2.0/settings/tfaappwithlink</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <category>TFA settings</category>
    /// <returns type="Google.Authenticator.SetupCode, Google.Authenticator">Setup code</returns>
    /// <path>api/2.0/settings/tfaapp/setup</path>
    /// <httpMethod>GET</httpMethod>
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
    /// <category>TFA settings</category>
    /// <returns type="System.Object, System">List of TFA application codes</returns>
    /// <path>api/2.0/settings/tfaappcodes</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
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
    /// <category>TFA settings</category>
    /// <returns type="System.Object, System">New backup codes</returns>
    /// <path>api/2.0/settings/tfaappnewcodes</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
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
        await messageService.SendAsync(MessageAction.UserConnectedTfaApp, MessageTarget.Create(currentUser.Id), currentUser.DisplayUserName(false, displayUserSettingsHelper));
        return codes;
    }

    /// <summary>
    /// Unlinks the current two-factor authentication application from the user account specified in the request.
    /// </summary>
    /// <short>Unlink the TFA application</short>
    /// <category>TFA settings</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.TfaRequestsDto, ASC.Web.Api" name="inDto">TFA settings request parameters</param>
    /// <returns type="System.Object, System">Login URL</returns>
    /// <path>api/2.0/settings/tfaappnewapp</path>
    /// <httpMethod>PUT</httpMethod>
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

        var tenant = await tenantManager.GetCurrentTenantAsync();
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
        await messageService.SendAsync(MessageAction.UserDisconnectedTfaApp, MessageTarget.Create(user.Id), user.DisplayUserName(false, displayUserSettingsHelper));

        await cookiesManager.ResetUserCookieAsync(user.Id);
        if (isMe)
        {
            var (url, key) = await commonLinkUtility.GetConfirmationUrlAndKeyAsync(user.Email, ConfirmType.TfaActivation);
            await cookiesManager.SetCookiesAsync(CookiesType.ConfirmKey, key, true, $"_{ConfirmType.TfaActivation}");
            return url;
        }

        await studioNotifyService.SendMsgTfaResetAsync(user);
        return string.Empty;
    }
}