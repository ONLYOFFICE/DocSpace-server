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

[DefaultRoute("webplugins")]
public class WebPluginsController(ApiContext apiContext,
        IMemoryCache memoryCache,
        WebItemManager webItemManager,
        IHttpContextAccessor httpContextAccessor,
        PermissionContext permissionContext,
        WebPluginManager webPluginManager,
        TenantManager tenantManager,
        CspSettingsHelper cspSettingsHelper,
        IMapper mapper)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    /// <summary>
    /// Adds web plugins from file
    /// </summary>
    /// <path>api/2.0/settings/webplugins</path>
    /// <exception cref="CustomHttpException"></exception>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "Web plugin", typeof(WebPluginDto))]
    [SwaggerResponse(400, "bad request")]
    [SwaggerResponse(403, "Plugins disabled")]
    [HttpPost("")]
    public async Task<WebPluginDto> AddWebPluginFromFile(WebPluginFromFileRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (HttpContext.Request.Form.Files == null || HttpContext.Request.Form.Files.Count == 0)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginNoInputFile);
        }

        if (HttpContext.Request.Form.Files.Count > 1)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginToManyInputFiles);
        }

        var file = HttpContext.Request.Form.Files[0] ?? throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginNoInputFile);

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.AddWebPluginFromFileAsync(tenant.Id, file, inDto.System);

        await ChangeCspSettings(webPlugin, webPlugin.Enabled);

        var outDto = mapper.Map<WebPlugin, WebPluginDto>(webPlugin);

        return outDto;
    }

    /// <summary>
    /// Gets web plugins
    /// </summary>
    /// <path>api/2.0/settings/webplugins</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "Web plugin", typeof(IEnumerable<WebPluginDto>))]
    [SwaggerResponse(403, "Plugins disabled")]
    [HttpGet("")]
    public async Task<IEnumerable<WebPluginDto>> GetWebPluginsAsync(GetWebPluginsRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var webPlugins = await webPluginManager.GetWebPluginsAsync(tenant.Id);

        var outDto = mapper.Map<List<WebPlugin>, List<WebPluginDto>>(webPlugins);

        if (inDto.Enabled.HasValue)
        {
            outDto = outDto.Where(i => i.Enabled == inDto.Enabled).ToList();
        }

        return outDto;
    }

    /// <summary>
    /// Gets web plugins by name specified in request
    /// </summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "Web plugin", typeof(WebPluginDto))]
    [SwaggerResponse(403, "Plugins disabled")]
    [HttpGet("{name}")]
    public async Task<WebPluginDto> GetWebPluginAsync(WebPluginNameRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.GetWebPluginByNameAsync(tenant.Id, inDto.Name);

        var outDto = mapper.Map<WebPlugin, WebPluginDto>(webPlugin);

        return outDto;
    }

    /// <summary>
    /// Updates web plugins
    /// </summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "Plugins disabled")]
    [HttpPut("{name}")]
    public async Task UpdateWebPluginAsync(WebPluginRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.UpdateWebPluginAsync(tenant.Id, inDto.Name, inDto.WebPlugin.Enabled, inDto.WebPlugin.Settings);

        await ChangeCspSettings(webPlugin, inDto.WebPlugin.Enabled);
    }

    /// <summary>
    /// Deletes web plugins by name specified in request
    /// </summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "Plugins disabled")]
    [HttpDelete("{name}")]
    public async Task DeleteWebPluginAsync(WebPluginNameRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.DeleteWebPluginAsync(tenant.Id, inDto.Name);

        await ChangeCspSettings(webPlugin, false);
    }

    private async Task ChangeCspSettings(WebPlugin plugin, bool enabled)
    {
        if (string.IsNullOrEmpty(plugin.CspDomains))
        {
            return;
        }

        var settings = await cspSettingsHelper.LoadAsync();

        var domains = plugin.CspDomains.Split(',');

        var currentDomains = settings.Domains?.ToList() ?? [];

        if (enabled)
        {
            currentDomains.AddRange(domains);
        }
        else
        {
            _ = currentDomains.RemoveAll(x => domains.Contains(x));
        }

        _ = await cspSettingsHelper.SaveAsync(currentDomains.Distinct());
    }
}