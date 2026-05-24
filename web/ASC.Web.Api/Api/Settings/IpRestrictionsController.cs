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

[DefaultRoute("iprestrictions")]
public class IpRestrictionsController(
    PermissionContext permissionContext,
    SettingsManager settingsManager,
    WebItemManager webItemManager,
    IPRestrictionsService iPRestrictionsService,
    IFusionCache fusionCache,
    MessageService messageService,
    TenantManager tenantManager)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns the IP portal restrictions.
    /// </remarks>
    /// <summary>Get the IP portal restrictions</summary>
    /// <path>api/2.0/settings/iprestrictions</path>
    /// <collection>list</collection>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "List of IP restrictions parameters", typeof(IEnumerable<IPRestriction>))]
    [HttpGet("")]
    public async Task<IEnumerable<IPRestriction>> GetIpRestrictions()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();
        var etagFromRequest = HttpContext.Request.Headers.IfNoneMatch;
        var result = await iPRestrictionsService.GetAsync(tenant.Id, etagFromRequest);

        return HttpContext.TryGetFromCache(await HttpContextExtension.CalculateEtagAsync(result.Select(r => r.Ip))) ? null : result;
    }

    /// <remarks>
    /// Updates the IP restrictions with the parameters specified in the request.
    /// </remarks>
    /// <summary>Update the IP restrictions</summary>
    /// <path>api/2.0/settings/iprestrictions</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "Updated IP restriction settings", typeof(IpRestrictionsDto))]
    [HttpPut("")]
    public async Task<IpRestrictionsDto> SaveIpRestrictions(IpRestrictionsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        inDto.IpRestrictions ??= new List<IpRestrictionBase>();
        var isEmpty = !inDto.IpRestrictions.Any();

        bool enable;
        if (!inDto.Enable.HasValue)
        {
            enable = !isEmpty;
        }
        else
        {
            enable = inDto.Enable.Value;
        }

        if (enable && isEmpty)
        {
            throw new ArgumentException(Resource.ErrorIpRestriction);
        }

        if (inDto.IpRestrictions.Any(r => !IPAddress.TryParse(r.Ip, out _)))
        {
            throw new ArgumentException(nameof(inDto.IpRestrictions));
        }

        var tenant = tenantManager.GetCurrentTenant();
        await iPRestrictionsService.SaveAsync(inDto.IpRestrictions, tenant.Id);

        var settings = new IPRestrictionsSettings { Enable = enable };
        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.IPRestrictionsSettingsUpdated);

        return inDto;
    }

    /// <remarks>
    /// Returns the IP restriction settings.
    /// </remarks>
    /// <summary>Get the IP restriction settings</summary>
    /// <path>api/2.0/settings/iprestrictions/settings</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "IP restriction settings", typeof(IPRestrictionsSettings))]
    [HttpGet("settings")]
    public async Task<IPRestrictionsSettings> ReadIpRestrictionsSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var settings = await settingsManager.LoadAsync<IPRestrictionsSettings>(HttpContext.GetIfModifiedSince());

        return HttpContext.TryGetFromCache(settings.LastModified) ? null : settings;
    }

    /// <remarks>
    /// Updates the IP restriction settings with the parameters specified in the request.
    /// </remarks>
    /// <summary>Update the IP restriction settings</summary>
    /// <path>api/2.0/settings/iprestrictions/settings</path>
    [Tags("Settings / IP restrictions")]
    [SwaggerResponse(200, "Updated IP restriction settings", typeof(IpRestrictionsDto))]
    [HttpPut("settings")]
    public async Task<IpRestrictionsDto> UpdateIpRestrictionsSettings(IpRestrictionsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        inDto.IpRestrictions ??= new List<IpRestrictionBase>();
        var isEmpty = !inDto.IpRestrictions.Any();

        bool enable;
        if (!inDto.Enable.HasValue)
        {
            enable = !isEmpty;
        }
        else
        {
            enable = inDto.Enable.Value;
        }

        if (enable && isEmpty)
        {
            throw new ArgumentException(Resource.ErrorIpRestriction);
        }

        if (inDto.IpRestrictions.Any(r => !IPAddress.TryParse(r.Ip, out _)))
        {
            throw new ArgumentException(nameof(inDto.IpRestrictions));
        }

        var tenant = tenantManager.GetCurrentTenant();
        await iPRestrictionsService.SaveAsync(inDto.IpRestrictions, tenant.Id);

        var settings = new IPRestrictionsSettings { Enable = enable };
        await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.IPRestrictionsSettingsUpdated);

        return inDto;
    }
}