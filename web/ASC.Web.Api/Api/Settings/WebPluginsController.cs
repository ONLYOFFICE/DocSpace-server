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
    /// 
    /// </summary>
    /// <param type="System.Boolean, System" name="system" example="true"></param>
    /// <returns></returns>
    /// <exception cref="CustomHttpException"></exception>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "Web plugin", typeof(WebPluginDto))]
    [HttpPost("")]
    public async Task<WebPluginDto> AddWebPluginFromFile(bool system)
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

        var tenant = await tenantManager.GetCurrentTenantAsync();

        var webPlugin = await webPluginManager.AddWebPluginFromFileAsync(tenant.Id, file, system);

        await ChangeCspSettings(webPlugin, webPlugin.Enabled);

        var outDto = mapper.Map<WebPlugin, WebPluginDto>(webPlugin);

        return outDto;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param type="System.Boolean, System" name="enabled" example="true"></param>
    /// <returns></returns>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "Web plugin", typeof(WebPluginDto))]
    [HttpGet("")]
    public async Task<IEnumerable<WebPluginDto>> GetWebPluginsAsync(bool? enabled = null)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();

        var webPlugins = await webPluginManager.GetWebPluginsAsync(tenant.Id);

        var outDto = mapper.Map<List<WebPlugin>, List<WebPluginDto>>(webPlugins);

        if (enabled.HasValue)
        {
            outDto = outDto.Where(i => i.Enabled == enabled).ToList();
        }

        return outDto;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param type="System.String, System" name="name" example="some text"></param>
    /// <returns></returns>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "Web plugin", typeof(WebPluginDto))]
    [HttpGet("{name}")]
    public async Task<WebPluginDto> GetWebPluginAsync(string name)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();

        var webPlugin = await webPluginManager.GetWebPluginByNameAsync(tenant.Id, name);

        var outDto = mapper.Map<WebPlugin, WebPluginDto>(webPlugin);

        return outDto;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param type="System.String, System" name="name" example="some text"></param>
    /// <param type="ASC.Web.Api.ApiModel.RequestsDto.WebPluginRequestsDto, ASC.Web.Api" name="inDto"></param>
    /// <returns></returns>
    [Tags("Settings / Webplugins")]
    [HttpPut("{name}")]
    public async Task UpdateWebPluginAsync(string name, WebPluginRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = await tenantManager.GetCurrentTenantAsync();

        var webPlugin = await webPluginManager.UpdateWebPluginAsync(tenant.Id, name, inDto.Enabled, inDto.Settings);

        await ChangeCspSettings(webPlugin, inDto.Enabled);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param type="System.String, System" name="name" example="some text"></param>
    /// <returns></returns>
    [Tags("Settings / Webplugins")]
    [HttpDelete("{name}")]
    public async Task DeleteWebPluginAsync(string name)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = await tenantManager.GetCurrentTenantAsync();

        var webPlugin = await webPluginManager.DeleteWebPluginAsync(tenant.Id, name);

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

        var currentDomains = settings.Domains?.ToList() ?? new List<string>();

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