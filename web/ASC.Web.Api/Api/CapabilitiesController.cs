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

namespace ASC.Web.Api.Controllers;

/// <summary>
/// Portal capabilities API.
/// </summary>
/// <name>capabilities</name>
[DefaultRoute, Route("api/2.0/capabilities.json")]
[ApiController]
[AllowAnonymous]
public class CapabilitiesController(CoreBaseSettings coreBaseSettings,
        TenantManager tenantManager,
        ProviderManager providerManager,
        SettingsManager settingsManager,
        ILogger<CapabilitiesController> logger,
        CommonLinkUtility commonLinkUtility)
    : ControllerBase
{
    private readonly ILogger _log = logger;


    ///<summary>
    ///Returns the information about portal capabilities.
    ///</summary>
    ///<short>
    ///Get portal capabilities
    ///</short>
    ///<returns type="ASC.Web.Api.ApiModel.ResponseDto.CapabilitiesDto, ASC.Web.Api">Portal capabilities</returns>
    ///<path>api/2.0/capabilities</path>
    ///<httpMethod>GET</httpMethod>
    ///<requiresAuthorization>false</requiresAuthorization>
    [HttpGet] //NOTE: this method doesn't requires auth!!!  //NOTE: this method doesn't check payment!!!
    [AllowNotPayment]
    public async Task<CapabilitiesDto> GetPortalCapabilitiesAsync()
    {
        var quota = await tenantManager.GetTenantQuotaAsync(await tenantManager.GetCurrentTenantIdAsync());
        var result = new CapabilitiesDto
        {
            LdapEnabled = false,
            OauthEnabled = coreBaseSettings.Standalone || quota.Oauth,
            Providers = new List<string>(0),
            SsoLabel = string.Empty,
            SsoUrl = string.Empty
        };

        try
        {
            if (coreBaseSettings.Standalone
                    || SetupInfo.IsVisibleSettings(ManagementType.LdapSettings.ToString())
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
                result.Providers = ProviderManager.AuthProviders.Where(loginProvider =>
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
                    || SetupInfo.IsVisibleSettings(ManagementType.SingleSignOnSettings.ToString())
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

        return result;
    }
}