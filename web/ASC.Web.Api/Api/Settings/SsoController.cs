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
    /// Returns the SAML Single Sign-On configuration of the current portal: the identity provider endpoints and
    /// certificates, the service provider certificates, the attribute mapping, the login button label and the user
    /// type new SSO accounts get. Anonymous callers are accepted, but an unauthenticated one receives only
    /// `hideAuthPage`, which tells the sign-in page whether the built-in login form has to be hidden; every other
    /// field stays empty, so read the full configuration with an authenticated request. An authenticated caller needs
    /// the permission to edit portal settings, which in practice means the portal owner or a DocSpace admin, and the
    /// portal plan has to include Single Sign-On, otherwise the call is refused. The operation only reads and is safe
    /// to repeat. When the login label was never set, the response carries the built-in `Single Sign-on` instead of
    /// an empty string, and `enableSso` is null until the settings are saved for the first time. Use
    /// `GET api/2.0/settings/ssov2/default` for a blank configuration to start from, and
    /// `GET api/2.0/settings/ssov2/constants` for the values the SAML fields accept.
    /// </remarks>
    /// <summary>
    /// Get the SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "The current portal SSO settings; an anonymous caller gets only the hidden-login-form flag", typeof(SsoSettingsV2))]
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
    /// Returns the built-in SSO configuration a portal starts from: empty identity provider and service provider
    /// sections with the stock SAML settings already filled in (HTTP-POST binding, transient name ID format, RSA-SHA1
    /// signing, AES-128 encryption), the default attribute mapping of `givenName`, `sn` and `mail`, the
    /// `Single Sign-on` login label, new accounts typed as user, and SSO switched off. Use it as the template for a
    /// new configuration: fill in the identity provider entity ID, sign-in URL and certificates, then send the result
    /// to `POST api/2.0/settings/ssov2`. The values are the same for every portal and do not depend on what is
    /// currently saved, nothing is written, and the call is safe to repeat. The caller needs the permission to edit
    /// portal settings, which in practice means the portal owner or a DocSpace admin, and the portal plan has to
    /// include Single Sign-On. This operation changes nothing by itself: to actually discard the configuration in
    /// use, call `DELETE api/2.0/settings/ssov2`, and to read what is configured now, call
    /// `GET api/2.0/settings/ssov2`.
    /// </remarks>
    /// <summary>
    /// Get the default SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2/default</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "The built-in SSO configuration a portal starts from", typeof(SsoSettingsV2))]
    [HttpGet("default")]
    public async Task<SsoSettingsV2> GetDefaultSsoSettingsV2()
    {
        await CheckSsoPermissionsAsync();
        return settingsManager.GetDefault<SsoSettingsV2>();
    }

    /// <remarks>
    /// Returns every literal value the SAML fields of the SSO configuration accept, grouped by the field it belongs
    /// to: name ID formats, request bindings, signing and encryption algorithms, and what a service provider or
    /// identity provider certificate can be used for. The values are the SAML URNs and algorithm URIs themselves, so
    /// they can be written into the configuration exactly as they come back; picking one from the matching group is
    /// the point, because `POST api/2.0/settings/ssov2` stores these fields as they are given and a misspelled value
    /// therefore surfaces only later, as a failing sign-in. The list is a fixed part of the product: it is the same
    /// for every portal, does not depend on the saved settings and does not change between calls within a release, so
    /// it can be cached. The operation only reads, is safe to repeat and needs nothing beyond an authenticated
    /// caller. Use it together with `GET api/2.0/settings/ssov2/default`, which already has the usual values set.
    /// </remarks>
    /// <summary>
    /// Get the SSO settings constants
    /// </summary>
    /// <path>api/2.0/settings/ssov2/constants</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "Every value the SAML fields accept: name ID formats, bindings, signing and encryption algorithms, and the service provider and identity provider certificate uses", typeof(SsoSettingsV2ConstantsDto))]
    [HttpGet("constants")]
    public SsoSettingsV2ConstantsDto GetSsoSettingsV2Constants()
    {
        return new SsoSettingsV2ConstantsDto();
    }

    /// <remarks>
    /// Replaces the whole SAML Single Sign-On configuration of the current portal with the one passed as a JSON
    /// object in `serializeSettings`, and returns the configuration as it was stored. The payload is a complete
    /// configuration rather than a patch: fields left out are stored empty, so send back a changed copy of
    /// `GET api/2.0/settings/ssov2`, or start from `GET api/2.0/settings/ssov2/default`. The identity provider entity
    /// ID and sign-in URL are required, the sign-in and sign-out URLs have to be absolute http or https addresses,
    /// and the attribute mapping has to name the fields for first name, last name and email; otherwise nothing is
    /// saved. The caller has to be allowed to edit portal settings (portal owner or DocSpace admin), and the portal
    /// plan has to include Single Sign-On. Some values are normalised on the way in: a `usersType` other than 1 (room
    /// admin), 3 (DocSpace admin) or 4 (user) becomes 4, an empty login label becomes `Single Sign-on`, and a longer
    /// one is cut to 100 characters. Saving with SSO switched off unlinks every existing SSO account and turns it
    /// into an ordinary one; switching SSO back on later does not restore those links. The change is recorded in the
    /// audit trail.
    /// </remarks>
    /// <summary>
    /// Save the SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "The SSO settings as they were stored, with the login label and the user type normalised", typeof(SsoSettingsV2))]
    [SwaggerResponse(400, "The serialized settings are empty or do not contain an SSO configuration object")]
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
    /// Discards the SAML Single Sign-On configuration of the current portal, stores the built-in default one in its
    /// place and returns what was stored, which is the same content as `GET api/2.0/settings/ssov2/default`. This is
    /// destructive and cannot be undone through the API: the identity provider addresses, both certificate sets, the
    /// attribute mapping and the login label are gone and SSO is left switched off, so keep a copy of
    /// `GET api/2.0/settings/ssov2` first if the configuration may be needed again. Every account that signed in
    /// through SSO is unlinked and becomes an ordinary account that keeps its data but authenticates with portal
    /// credentials from then on, and its external contacts are converted the same way. Repeating the call is
    /// harmless, as the second one stores the same defaults again. The caller needs the permission to edit portal
    /// settings, which in practice means the portal owner or a DocSpace admin, and the portal plan has to include
    /// Single Sign-On, otherwise the call is refused. To switch SSO off while keeping the configuration, send it back
    /// to `POST api/2.0/settings/ssov2` with SSO disabled instead. The reset is recorded in the audit trail.
    /// </remarks>
    /// <summary>
    /// Reset the SSO settings
    /// </summary>
    /// <path>api/2.0/settings/ssov2</path>
    [Tags("Settings / SSO")]
    [SwaggerResponse(200, "The default SSO configuration that is now in effect", typeof(SsoSettingsV2))]
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