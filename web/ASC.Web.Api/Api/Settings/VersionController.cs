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

/// <visible>false</visible>
[DefaultRoute("version")]
public class VersionController(PermissionContext permissionContext,
        ApiContext apiContext,
        TenantManager tenantManager,
        WebItemManager webItemManager,
        BuildVersion buildVersion,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns the current build version.
    /// </summary>
    /// <short>Get the current build version</short>
    /// <category>Versions</category>
    /// <path>api/2.0/settings/version/build</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <returns type="ASC.Api.Settings.BuildVersion, ASC.Web.Api">Current product versions</returns>
    /// <visible>false</visible>
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("build")]
    public async Task<BuildVersion> GetBuildVersionsAsync()
    {
        return await buildVersion.GetCurrentBuildVersionAsync();
    }

    /// <summary>
    /// Returns a list of the available portal versions including the current version.
    /// </summary>
    /// <short>
    /// Get the portal versions
    /// </short>
    /// <category>Versions</category>
    /// <path>api/2.0/settings/version</path>
    /// <httpMethod>GET</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.TenantVersionDto, ASC.Web.Api">List of availibe portal versions including the current version</returns>
    /// <visible>false</visible>
    [HttpGet("")]
    public async Task<TenantVersionDto> GetVersionsAsync()
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        return new TenantVersionDto(tenant.Version, await tenantManager.GetTenantVersionsAsync());
    }

    /// <summary>
    /// Sets a version with the ID specified in the request to the current tenant.
    /// </summary>
    /// <short>
    /// Change the portal version
    /// </short>
    /// <category>Versions</category>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.SettingsRequestsDto, ASC.Web.Api" name="inDto">Settings request parameters</param>
    /// <path>api/2.0/settings/version</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns type="ASC.Web.Api.ApiModel.ResponseDto.TenantVersionDto, ASC.Web.Api">List of availibe portal versions including the current version</returns>
    /// <visible>false</visible>
    [HttpPut("")]
    public async Task<TenantVersionDto> SetVersionAsync(SettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        (await tenantManager.GetTenantVersionsAsync()).FirstOrDefault(r => r.Id == inDto.VersionId).NotFoundIfNull();
        
        var tenant = await tenantManager.GetCurrentTenantAsync();
        await tenantManager.SetTenantVersionAsync(tenant, inDto.VersionId);

        return await GetVersionsAsync();
    }
}
