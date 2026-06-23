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

namespace ASC.Web.Api.Controllers;

/// <remarks>
/// Portal capabilities API.
/// </remarks>
/// <name>capabilities</name>
[DefaultRoute, Route("api/2.0/capabilities.json")]
[ApiController]
[AllowAnonymous]
[ControllerName("capabilities")]
public class CapabilitiesController(CoreBaseSettings coreBaseSettings,
        TenantManager tenantManager,
        ProviderManager providerManager,
        SettingsManager settingsManager,
        ILogger<CapabilitiesController> logger,
        CommonLinkUtility commonLinkUtility,
        GeolocationHelper geolocationHelper)
    : ControllerBase
{
    private readonly ILogger _log = logger;


    ///<remarks>
    /// Returns the information about portal capabilities.
    ///</remarks>
    ///<summary>
    /// Get portal capabilities
    ///</summary>
    ///<path>api/2.0/capabilities</path>
    [Tags("Capabilities")]
    [SwaggerResponse(200, "Portal capabilities", typeof(CapabilitiesDto))]
    [HttpGet] //NOTE: this method doesn't requires auth!!!  //NOTE: this method doesn't check payment!!!
    [AllowNotPayment]
    public async Task<CapabilitiesDto> GetPortalCapabilities()
    {
        var quota = await tenantManager.GetTenantQuotaAsync(tenantManager.GetCurrentTenantId());
        var result = new CapabilitiesDto
        {
            LdapEnabled = false,
            OauthEnabled = coreBaseSettings.Standalone || quota.Oauth,
            Providers = [],
            SsoLabel = string.Empty,
            SsoUrl = string.Empty,
            IdentityServerEnabled = false
        };

        try
        {
            if (coreBaseSettings.Standalone
                    || SetupInfo.IsVisibleSettings(ManagementType.LdapSettings.ToStringFast())
                        && quota.Ldap)
            {
                var settings = await settingsManager.LoadAsync<LdapSettings>();
                var currentDomainSettings = await settingsManager.LoadAsync<LdapCurrentDomain>();
                result.LdapEnabled = settings.EnableLdapAuthentication;
                result.LdapDomain = currentDomainSettings.CurrentDomain;
            }
        }
        catch (Exception ex)
        {
            _log.ErrorWithException(ex);
        }

        try
        {
            if (result.OauthEnabled)
            {
                var geoInfoKey = (await geolocationHelper.GetIPGeolocationFromHttpContextAsync()).Key;

                result.Providers = ProviderManager.GetSortedAuthProviders(geoInfoKey).Where(loginProvider =>
                {
                    if (loginProvider is ProviderConstants.Facebook or ProviderConstants.AppleId
                                                                    && coreBaseSettings.Standalone && HttpContext.Request.MobileApp())
                    {
                        return false;
                    }
                    var provider = providerManager.GetLoginProvider(loginProvider);
                    return provider is { IsEnabled: true };
                })
                .ToList();
            }
        }
        catch (Exception ex)
        {
            _log.ErrorWithException(ex);
        }

        try
        {
            if (coreBaseSettings.Standalone
                    || SetupInfo.IsVisibleSettings(ManagementType.SingleSignOnSettings.ToStringFast())
                        && quota.Sso)
            {
                var settings = await settingsManager.LoadAsync<SsoSettingsV2>();

                if (settings.EnableSso.GetValueOrDefault())
                {
                    result.SsoUrl = commonLinkUtility.GetFullAbsolutePath("/sso/login"); //TODO: get it from config
                    result.SsoLabel = settings.SpLoginLabel;
                }
            }
        }
        catch (Exception ex)
        {
            _log.ErrorWithException(ex);
        }

        if (SetupInfo.IsVisibleSettings(ManagementType.IdentityServer.ToStringFast()))
        {
            result.IdentityServerEnabled = true;
        }

        return result;
    }
}