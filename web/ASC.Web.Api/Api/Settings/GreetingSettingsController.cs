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

[DefaultRoute("greetingsettings")]
public class GreetingSettingsController(
    TenantInfoSettingsHelper tenantInfoSettingsHelper,
    MessageService messageService,
    TenantManager tenantManager,
    PermissionContext permissionContext,
    WebItemManager webItemManager,
    IFusionCache fusionCache,
    CoreBaseSettings coreBaseSettings)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Returns the greeting settings for the current portal.
    /// </remarks>
    /// <summary>Get greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "Greeting settings: tenant name", typeof(object))]
    [HttpGet("")]
    public object GetGreetingSettings()
    {
        var tenant = tenantManager.GetCurrentTenant();
        return tenant.Name == "" ? Resource.PortalName : tenant.Name;
    }

    /// <remarks>
    /// Checks if the greeting settings of the current portal are set to default or not.
    /// </remarks>
    /// <summary>Check the default greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings/isdefault</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "Boolean value: true if the greeting settings of the current portal are set to default", typeof(bool))]
    [HttpGet("isdefault")]
    public bool GetIsDefaultGreetingSettings()
    {
        var tenant = tenantManager.GetCurrentTenant();
        return tenant.Name == "";
    }

    /// <remarks>
    /// Saves the greeting settings specified in the request to the current portal.
    /// </remarks>
    /// <summary>Save the greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "Message about saving greeting settings successfully", typeof(string))]
    [HttpPost("")]
    public async Task<string> SaveGreetingSettings(GreetingSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        if (!coreBaseSettings.Standalone)
        {
            var quota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
            if (quota.Free || quota.Trial)
            {
                try
                {
                    tenantManager.ValidateTenantName(inDto.Title);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message, nameof(inDto.Title));
                }
            }
        }

        tenant.Name = inDto.Title;
        await tenantManager.SaveTenantAsync(tenant);

        messageService.Send(MessageAction.GreetingSettingsUpdated);

        return Resource.SuccessfullySaveGreetingSettingsMessage;
    }

    /// <remarks>
    /// Restores the current portal greeting settings.
    /// </remarks>
    /// <summary>Restore the greeting settings</summary>
    /// <path>api/2.0/settings/greetingsettings/restore</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "Greeting settings: tenant name", typeof(string))]
    [HttpPost("restore")]
    public async Task<string> RestoreGreetingSettings()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantInfoSettingsHelper.RestoreDefaultTenantNameAsync();

        var tenant = tenantManager.GetCurrentTenant();

        return tenant.Name == "" ? Resource.PortalName : tenant.Name;
    }
}