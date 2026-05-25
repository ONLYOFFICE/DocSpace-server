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

[ApiExplorerSettings(IgnoreApi = true)]
[DefaultRoute("version")]
public class VersionController(
    PermissionContext permissionContext,
    TenantManager tenantManager,
    WebItemManager webItemManager,
    BuildVersion buildVersion,
    IFusionCache fusionCache)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns the current portal build version.
    /// </remarks>
    /// <summary>Get the current build version</summary>
    /// <path>api/2.0/settings/version/build</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Settings / Versions")]
    [SwaggerResponse(200, "Current product versions", typeof(BuildVersion))]
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("build")]
    public async Task<BuildVersion> GetBuildVersions()
    {
        return await buildVersion.GetCurrentBuildVersionAsync();
    }

    /// <remarks>
    /// Returns a list of the available portal versions, including the current version.
    /// </remarks>
    /// <summary>
    /// Get the portal versions
    /// </summary>
    /// <path>api/2.0/settings/version</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Versions")]
    [SwaggerResponse(200, "List of availibe portal versions including the current version", typeof(TenantVersionDto))]
    [HttpGet("")]
    public async Task<TenantVersionDto> GetVersions()
    {
        var tenant = tenantManager.GetCurrentTenant();
        return new TenantVersionDto(tenant.Version, await tenantManager.GetTenantVersionsAsync());
    }

    /// <remarks>
    /// Sets a version with the ID specified in the request to the current tenant.
    /// </remarks>
    /// <summary>
    /// Change the portal version
    /// </summary>
    /// <path>api/2.0/settings/version</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Versions")]
    [SwaggerResponse(200, "List of availibe portal versions including the current version", typeof(TenantVersionDto))]
    [HttpPut("")]
    public async Task<TenantVersionDto> SetVersion(SettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        (await tenantManager.GetTenantVersionsAsync()).FirstOrDefault(r => r.Id == inDto.VersionId).NotFoundIfNull();

        var tenant = tenantManager.GetCurrentTenant();
        await tenantManager.SetTenantVersionAsync(tenant, inDto.VersionId);

        return await GetVersions();
    }
}