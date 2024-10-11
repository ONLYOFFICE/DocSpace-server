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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("iprestrictions")]
public class IpRestrictionsController(ApiContext apiContext,
        PermissionContext permissionContext,
        SettingsManager settingsManager,
        WebItemManager webItemManager,
        IPRestrictionsService iPRestrictionsService,
        IMemoryCache memoryCache,
        TenantManager tenantManager,
        IHttpContextAccessor httpContextAccessor)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns the IP portal restrictions.
    /// </summary>
    /// <short>Get the IP portal restrictions</short>
    /// <path>api/2.0/settings/iprestrictions</path>
    /// <collection>list</collection>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "List of IP restrictions parameters", typeof(IPRestriction))]
    [HttpGet("")]
    public async Task<IEnumerable<IPRestriction>> GetIpRestrictionsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var tenant = await tenantManager.GetCurrentTenantAsync();
        return await iPRestrictionsService.GetAsync(tenant.Id);
    }

    /// <summary>
    /// Saves the new portal IP restrictions specified in the request.
    /// </summary>
    /// <short>Save the IP restrictions</short>
    /// <path>api/2.0/settings/iprestrictions</path>
    /// <collection>list</collection>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "List of IP restrictions parameters", typeof(IpRestrictionBase))]
    [SwaggerResponse(400, "Exception in IpRestrictions")]
    [HttpPut("")]
    public async Task<IEnumerable<IpRestrictionBase>> SaveIpRestrictionsAsync(IpRestrictionsBaseRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        
        if (inDto.IpRestrictions.Any(r => !IPAddress.TryParse(r.Ip, out _)))
        {
            throw new ArgumentException(nameof(inDto.IpRestrictions));
        }
        
        var tenant = await tenantManager.GetCurrentTenantAsync();
        return await iPRestrictionsService.SaveAsync(inDto.IpRestrictions, tenant.Id);
    }

    /// <summary>
    /// Returns the IP restriction settings.
    /// </summary>
    /// <short>Get the IP restriction settings</short>
    /// <path>api/2.0/settings/iprestrictions/settings</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "IP restriction settings", typeof(IPRestrictionsSettings))]
    [HttpGet("settings")]
    public async Task<IPRestrictionsSettings> ReadIpRestrictionsSettingsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await settingsManager.LoadAsync<IPRestrictionsSettings>();
    }

    /// <summary>
    /// Updates the IP restriction settings with a parameter specified in the request.
    /// </summary>
    /// <short>Update the IP restriction settings</short>
    /// <path>api/2.0/settings/iprestrictions/settings</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "Updated IP restriction settings", typeof(IPRestrictionsSettings))]
    [HttpPut("settings")]
    public async Task<IPRestrictionsSettings> UpdateIpRestrictionsSettingsAsync(IpRestrictionsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = new IPRestrictionsSettings { Enable = inDto.Enable };
        await settingsManager.SaveAsync(settings);

        return settings;
    }
}