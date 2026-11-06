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

[ApiEndpoint(Template = "ssov2")]
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
    /// Returns the SAML single sign-on (SSO) configuration of the portal. Anyone can call it without signing in.
    /// An anonymous caller gets only `hideAuthPage`, and every other field is empty. This flag says if the sign-in
    /// page hides the built-in login form. It is false if a cloud plan lacks SSO or SSO settings are hidden. A
    /// signed-in caller gets the full configuration and needs the EditPortalSettings permission. The portal owner
    /// and DocSpace admins have it. Other signed-in callers get 403. If a cloud plan lacks SSO or SSO settings are
    /// hidden, the call returns 402 to a signed-in caller. The call is read-only. An empty
    /// login label comes back as `Single Sign-on`. A null `enableSso` means that SSO is off. For a blank
    /// configuration, use `GET api/2.0/settings/ssov2/default`. For the values that the SAML fields accept, use
    /// `GET api/2.0/settings/ssov2/constants`.
    /// </remarks>
    /// <summary>
    /// Get the SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "The current portal SSO settings; an anonymous caller gets only the hidden-login-form flag", typeof(SsoSettingsDto))]
    [SwaggerResponse(402, "The caller is signed in, the portal is a cloud one, and its pricing plan has no single sign-on option or the SSO section is not enabled for it")]
    [SwaggerResponse(403, "The caller is signed in and has no portal-settings right")]
    [HttpGet("")]
    [AllowAnonymous, AllowNotPayment]
    public async Task<SsoSettingsDto> GetSsoSettingsV2()
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
            }.Map();
        }

        await CheckSsoPermissionsAsync();

        if (string.IsNullOrEmpty(settings.SpLoginLabel))
        {
            settings.SpLoginLabel = SsoSettingsV2.SSO_SP_LOGIN_LABEL;
        }

        return settings.Map();
    }

    /// <remarks>
    /// Returns the built-in SSO configuration that a portal starts from. It has empty identity provider fields,
    /// the usual SAML values, and SSO turned off. The values are the same for every portal and do not depend on the
    /// saved configuration. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins
    /// have. Other callers get 403. If the plan of a cloud portal does not include SSO or SSO settings are hidden,
    /// the call returns 402. The call is read-only. To create a configuration, fill in the identity provider entity
    /// ID, sign-in URL, and certificates of the result. Then send it to `POST api/2.0/settings/ssov2`. To reset the
    /// saved configuration to these values, use `DELETE api/2.0/settings/ssov2`.
    /// </remarks>
    /// <summary>
    /// Get the default SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2/default</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "The built-in SSO configuration a portal starts from", typeof(SsoSettingsDto))]
    [SwaggerResponse(402, "The portal is a cloud one, and its pricing plan has no single sign-on option or the SSO section is not enabled for it")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("default")]
    public async Task<SsoSettingsDto> GetDefaultSsoSettingsV2()
    {
        await CheckSsoPermissionsAsync();
        return settingsManager.GetDefault<SsoSettingsV2>().Map();
    }

    /// <remarks>
    /// Returns every value that the SAML fields of the SSO configuration accept, grouped by field. The groups are
    /// name ID formats, bindings, signing and encryption algorithms, and certificate uses. Any signed-in user can
    /// call it. The call is read-only. The values are SAML URNs and algorithm URIs, so copy them into the
    /// configuration as they are. The save call `POST api/2.0/settings/ssov2` stores these fields without checks.
    /// So a misspelled value fails only later, at sign-in. The list is the same for every portal and changes only
    /// with a new release, so you can cache it. For a configuration with the usual values, use
    /// `GET api/2.0/settings/ssov2/default`.
    /// </remarks>
    /// <summary>
    /// Get the SSO settings constants
    /// </summary>
    /// <path>api/2.0/settings/ssov2/constants</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "Every value the SAML fields accept: name ID formats, bindings, signing and encryption algorithms, and the service provider and identity provider certificate uses", typeof(SsoSettingsConstantsDto))]
    [HttpGet("constants")]
    public SsoSettingsConstantsDto GetSsoSettingsV2Constants()
    {
        return new SsoSettingsConstantsDto();
    }

    /// <remarks>
    /// Replaces the SAML SSO configuration of the portal with the JSON object in `serializeSettings`. It returns the
    /// saved configuration. The object is a full configuration, not a patch. Left-out fields become empty. Start
    /// from `GET api/2.0/settings/ssov2` or `GET api/2.0/settings/ssov2/default`. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. If a cloud plan lacks SSO
    /// or SSO settings are hidden, the call returns 402. The call changes data, and you can repeat it with the same
    /// body. It changes a `usersType` other than room admin (1), DocSpace admin (3), or user (4) to 4. It saves an
    /// empty login label as `Single Sign-on` and cuts a longer one to 100 characters. If `enableSso` is false or
    /// missing, the call unlinks every SSO account from the identity provider. These accounts become ordinary
    /// portal accounts. Turning SSO back on does not link them again. If `serializeSettings` is empty or the JSON
    /// `null`, the call returns 400. The call returns 500 if the JSON is invalid or lacks `idpSettings` or
    /// `fieldMapping`. It also returns 500 for an empty entity ID, sign-in URL, first name, last name, or email
    /// mapping. It also returns 500 if a sign-in or sign-out URL is not an absolute http or https address. Then the
    /// call saves nothing. For the values of the SAML fields, use `GET api/2.0/settings/ssov2/constants`.
    /// </remarks>
    /// <summary>
    /// Save the SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "The SSO settings as they were stored, with the login label and the user type normalised", typeof(SsoSettingsDto))]
    [SwaggerResponse(400, "The serialized settings are empty or do not contain an SSO configuration object")]
    [SwaggerResponse(402, "The portal is a cloud one, and its pricing plan has no single sign-on option or the SSO section is not enabled for it")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [SwaggerResponse(500, "The serialized settings are not valid JSON, have a value of the wrong type or lack the `idpSettings` or `fieldMapping` object, the IdP entity ID is empty, the SSO URL is missing or the SSO or SLO URL is not an absolute HTTP or HTTPS address, or the first name, last name or email attribute mapping is empty")]
    [HttpPost("")]
    public async Task<SsoSettingsDto> SaveSsoSettingsV2(SsoSettingsRequestsDto inDto)
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

        return settings.Map();
    }

    /// <remarks>
    /// Deletes the SAML SSO configuration of the portal and saves the built-in default one instead. It returns the
    /// saved configuration, which matches `GET api/2.0/settings/ssov2/default`. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. If the plan of a cloud
    /// portal does not include SSO or SSO settings are hidden, the call returns 402. The call cannot be undone, and
    /// you can repeat it. It turns SSO off and deletes the identity provider addresses, certificates, attribute
    /// mapping, and login label. It also unlinks every SSO account from the identity provider. These accounts
    /// become ordinary portal accounts and keep their data and contacts. To keep a copy, first save the result of
    /// `GET api/2.0/settings/ssov2`. To turn SSO off and keep the configuration, set `enableSso` to false in
    /// `POST api/2.0/settings/ssov2`.
    /// </remarks>
    /// <summary>
    /// Reset the SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "The default SSO configuration that is now in effect", typeof(SsoSettingsDto))]
    [SwaggerResponse(402, "The portal is a cloud one, and its pricing plan has no single sign-on option or the SSO section is not enabled for it")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpDelete("")]
    public async Task<SsoSettingsDto> ResetSsoSettingsV2()
    {
        await CheckSsoPermissionsAsync();

        var defaultSettings = settingsManager.GetDefault<SsoSettingsV2>();

        if (!await settingsManager.SaveAsync(defaultSettings))
        {
            throw new Exception(Resource.SsoSettingsCantSaveSettings);
        }

        await ConverSsoUsersToOrdinaryAsync();

        messageService.Send(MessageAction.SSODisabled);

        return defaultSettings.Map();
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