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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("ssov2")]
public class SsoController(TenantManager tenantManager,
        ApiContext apiContext,
        WebItemManager webItemManager,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor,
        SettingsManager settingsManager,
        PermissionContext permissionContext,
        CoreBaseSettings coreBaseSettings,
        UserManager userManager,
        MessageService messageService,
        AuthContext authContext)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns the current portal SSO settings.
    /// </summary>
    /// <short>
    /// Get the SSO settings
    /// </short>
    /// <path>api/2.0/settings/ssov2</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "SSO settings", typeof(SsoSettingsV2))]
    [HttpGet("")]
    [AllowAnonymous, AllowNotPayment]
    public async Task<SsoSettingsV2> GetSsoSettingsV2()
    {
        var settings = await settingsManager.LoadAsync<SsoSettingsV2>();

        if (!authContext.IsAuthenticated)
        {
            bool hideAuthPage;
            try
            {
                await CheckSsoPermissionsAsync(true);
                hideAuthPage = settings.HideAuthPage;
            }
            catch (BillingException)
            {
                hideAuthPage = false;
            }

            return new SsoSettingsV2
            {
                HideAuthPage = hideAuthPage
            };
        }

        await CheckSsoPermissionsAsync();

        if (string.IsNullOrEmpty(settings.SpLoginLabel))
        {
            settings.SpLoginLabel = SsoSettingsV2.SSO_SP_LOGIN_LABEL;
        }

        return settings;
    }

    /// <summary>
    /// Returns the default portal SSO settings.
    /// </summary>
    /// <short>
    /// Get the default SSO settings
    /// </short>
    /// <path>api/2.0/settings/ssov2/default</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "Default SSO settings", typeof(SsoSettingsV2))]
    [HttpGet("default")]
    public async Task<SsoSettingsV2> GetDefaultSsoSettingsV2Async()
    {
        await CheckSsoPermissionsAsync();
        return settingsManager.GetDefault<SsoSettingsV2>();
    }

    /// <summary>
    /// Returns the SSO settings constants.
    /// </summary>
    /// <short>
    /// Get the SSO settings constants
    /// </short>
    /// <path>api/2.0/settings/ssov2/constants</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "The SSO settings constants: SSO name ID format type, SSO binding type, SSO signing algorithm type, SSO SP certificate action type, SSO IDP certificate action type", typeof(object))]
    [HttpGet("constants")]
    public object GetSsoSettingsV2Constants()
    {
        return new
        {
            SsoNameIdFormatType = new SsoNameIdFormatType(),
            SsoBindingType = new SsoBindingType(),
            SsoSigningAlgorithmType = new SsoSigningAlgorithmType(),
            SsoEncryptAlgorithmType = new SsoEncryptAlgorithmType(),
            SsoSpCertificateActionType = new SsoSpCertificateActionType(),
            SsoIdpCertificateActionType = new SsoIdpCertificateActionType()
        };
    }

    /// <summary>
    /// Saves the SSO settings for the current portal.
    /// </summary>
    /// <short>
    /// Save the SSO settings
    /// </short>
    /// <path>api/2.0/settings/ssov2</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "SSO settings", typeof(SsoSettingsV2))]
    [SwaggerResponse(400, "Settings could not be null")]
    [HttpPost("")]
    public async Task<SsoSettingsV2> SaveSsoSettingsV2Async(SsoSettingsRequestsDto inDto)
    {
        await CheckSsoPermissionsAsync();

        var serializeSettings = inDto.SerializeSettings;

        if (string.IsNullOrEmpty(serializeSettings))
        {
            throw new ArgumentException(Resource.SsoSettingsCouldNotBeNull);
        }

        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        var settings = JsonSerializer.Deserialize<SsoSettingsV2>(serializeSettings, options);

        if (settings == null)
        {
            throw new ArgumentException(Resource.SsoSettingsCouldNotBeNull);
        }

        if (string.IsNullOrWhiteSpace(settings.IdpSettings.EntityId))
        {
            throw new Exception(Resource.SsoSettingsInvalidEntityId);
        }

        if (string.IsNullOrWhiteSpace(settings.IdpSettings.SsoUrl) || !CheckUri(settings.IdpSettings.SsoUrl))
        {
            throw new Exception(string.Format(Resource.SsoSettingsInvalidBinding, "SSO " + settings.IdpSettings.SsoBinding));
        }

        if (!string.IsNullOrWhiteSpace(settings.IdpSettings.SloUrl) && !CheckUri(settings.IdpSettings.SloUrl))
        {
            throw new Exception(string.Format(Resource.SsoSettingsInvalidBinding, "SLO " + settings.IdpSettings.SloBinding));
        }

        if (string.IsNullOrWhiteSpace(settings.FieldMapping.FirstName) ||
            string.IsNullOrWhiteSpace(settings.FieldMapping.LastName) ||
            string.IsNullOrWhiteSpace(settings.FieldMapping.Email))
        {
            throw new Exception(Resource.SsoSettingsInvalidMapping);
        }

        if ((EmployeeType)settings.UsersType is not (EmployeeType.User or EmployeeType.RoomAdmin or EmployeeType.DocSpaceAdmin))
        {
            settings.UsersType = (int)EmployeeType.User;
        }

        if (string.IsNullOrEmpty(settings.SpLoginLabel))
        {
            settings.SpLoginLabel = SsoSettingsV2.SSO_SP_LOGIN_LABEL;
        }
        else if (settings.SpLoginLabel.Length > 100)
        {
            settings.SpLoginLabel = settings.SpLoginLabel[..100];
        }

        if (!await settingsManager.SaveAsync(settings))
        {
            throw new Exception(Resource.SsoSettingsCantSaveSettings);
        }

        var enableSso = settings.EnableSso.GetValueOrDefault();
        if (!enableSso)
        {
            await ConverSsoUsersToOrdinaryAsync();
        }

        var messageAction = enableSso ? MessageAction.SSOEnabled : MessageAction.SSODisabled;

        messageService.Send(messageAction);

        return settings;
    }

    /// <summary>
    /// Resets the SSO settings of the current portal.
    /// </summary>
    /// <short>
    /// Reset the SSO settings
    /// </short>
    /// <path>api/2.0/settings/ssov2</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "Default SSO settings", typeof(SsoSettingsV2))]
    [HttpDelete("")]
    public async Task<SsoSettingsV2> ResetSsoSettingsV2Async()
    {
        await CheckSsoPermissionsAsync();

        var defaultSettings = settingsManager.GetDefault<SsoSettingsV2>();

        if (!await settingsManager.SaveAsync(defaultSettings))
        {
            throw new Exception(Resource.SsoSettingsCantSaveSettings);
        }

        await ConverSsoUsersToOrdinaryAsync();

        messageService.Send(MessageAction.SSODisabled);

        return defaultSettings;
    }

    private async Task ConverSsoUsersToOrdinaryAsync()
    {
        var ssoUsers = (await userManager.GetUsersAsync()).Where(u => u.IsSSO()).ToList();

        if (ssoUsers.Count == 0)
        {
            return;
        }

        foreach (var existingSsoUser in ssoUsers)
        {
            existingSsoUser.SsoNameId = null;
            existingSsoUser.SsoSessionId = null;

            existingSsoUser.ConvertExternalContactsToOrdinary();

            await userManager.UpdateUserInfoAsync(existingSsoUser);
        }
    }

    private static bool CheckUri(string uriName)
    {
        return Uri.TryCreate(uriName, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private async Task CheckSsoPermissionsAsync(bool allowAnonymous = false)
    {
        if (!allowAnonymous)
        {
            await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        }

        if (!coreBaseSettings.Standalone
            && (!SetupInfo.IsVisibleSettings(ManagementType.SingleSignOnSettings.ToStringFast())
                || !(await tenantManager.GetCurrentTenantQuotaAsync()).Sso))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption, "Sso");
        }
    }

}