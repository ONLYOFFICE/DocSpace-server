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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("ssov2")]
public class SsoController(
    TenantManager tenantManager,
    WebItemManager webItemManager,
    IFusionCache fusionCache,
    SettingsManager settingsManager,
    PermissionContext permissionContext,
    CoreBaseSettings coreBaseSettings,
    UserManager userManager,
    MessageService messageService,
    AuthContext authContext)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns the current portal SSO settings.
    /// </remarks>
    /// <summary>
    /// Get the SSO settings
    /// </summary>
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

    /// <remarks>
    /// Returns the default portal SSO settings.
    /// </remarks>
    /// <summary>
    /// Get the default SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2/default</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "Default SSO settings", typeof(SsoSettingsV2))]
    [HttpGet("default")]
    public async Task<SsoSettingsV2> GetDefaultSsoSettingsV2()
    {
        await CheckSsoPermissionsAsync();
        return settingsManager.GetDefault<SsoSettingsV2>();
    }

    /// <remarks>
    /// Returns the SSO settings constants.
    /// </remarks>
    /// <summary>
    /// Get the SSO settings constants
    /// </summary>
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

    /// <remarks>
    /// Saves the SSO settings for the current portal.
    /// </remarks>
    /// <summary>
    /// Save the SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "SSO settings", typeof(SsoSettingsV2))]
    [SwaggerResponse(400, "Settings could not be null")]
    [HttpPost("")]
    public async Task<SsoSettingsV2> SaveSsoSettingsV2(SsoSettingsRequestsDto inDto)
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

    /// <remarks>
    /// Resets the SSO settings of the current portal.
    /// </remarks>
    /// <summary>
    /// Reset the SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "Default SSO settings", typeof(SsoSettingsV2))]
    [HttpDelete("")]
    public async Task<SsoSettingsV2> ResetSsoSettingsV2()
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
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }

}