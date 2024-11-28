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

[DefaultRoute("greetingsettings")]
public class GreetingSettingsController(TenantInfoSettingsHelper tenantInfoSettingsHelper,
        MessageService messageService,
        ApiContext apiContext,
        TenantManager tenantManager,
        PermissionContext permissionContext,
        WebItemManager webItemManager,
        IMemoryCache memoryCache,
        CoreBaseSettings coreBaseSettings,
        IHttpContextAccessor httpContextAccessor)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Returns the greeting settings for the current portal.
    /// </summary>
    /// <short>Get greeting settings</short>
    /// <path>api/2.0/settings/greetingsettings</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "Greeting settings: tenant name", typeof(object))]
    [HttpGet("")]
    public object GetGreetingSettings()
    {
        var tenant = tenantManager.GetCurrentTenant();
        return tenant.Name == "" ? Resource.PortalName : tenant.Name;
    }

    /// <summary>
    /// Checks if the greeting settings of the current portal are set to default or not.
    /// </summary>
    /// <short>Check the default greeting settings</short>
    /// <path>api/2.0/settings/greetingsettings/isdefault</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "Boolean value: true if the greeting settings of the current portal are set to default", typeof(bool))]
    [HttpGet("isdefault")]
    public bool IsDefault()
    {
        var tenant = tenantManager.GetCurrentTenant();
        return tenant.Name == "";
    }

    /// <summary>
    /// Saves the greeting settings specified in the request to the current portal.
    /// </summary>
    /// <short>Save the greeting settings</short>
    /// <path>api/2.0/settings/greetingsettings</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "Message about saving greeting settings successfully", typeof(object))]
    [HttpPost("")]
    public async Task<object> SaveGreetingSettingsAsync(GreetingSettingsRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        if (!coreBaseSettings.Standalone)
        {
            var quota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
            if (quota.Free || quota.Trial)
            {
                tenantManager.ValidateTenantName(inDto.Title);
            }
        }

        tenant.Name = inDto.Title;
        await tenantManager.SaveTenantAsync(tenant);

        messageService.Send(MessageAction.GreetingSettingsUpdated);

        return Resource.SuccessfullySaveGreetingSettingsMessage;
    }

    /// <summary>
    /// Restores the current portal greeting settings.
    /// </summary>
    /// <short>Restore the greeting settings</short>
    /// <path>api/2.0/settings/greetingsettings/restore</path>
    [Tags("Settings / Greeting settings")]
    [SwaggerResponse(200, "Greeting settings: tenant name", typeof(object))]
    [HttpPost("restore")]
    public async Task<object> RestoreGreetingSettingsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantInfoSettingsHelper.RestoreDefaultTenantNameAsync();

        var tenant = tenantManager.GetCurrentTenant();
        
        return tenant.Name == "" ? Resource.PortalName : tenant.Name;
    }
}
