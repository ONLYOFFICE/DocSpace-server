﻿// (c) Copyright Ascensio System SIA 2010-2023
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

public class WebPluginsController : BaseSettingsController
{
    private readonly PermissionContext _permissionContext;
    private readonly WebPluginManager _webPluginManager;
    private readonly TenantManager _tenantManager;
    private readonly CspSettingsHelper _cspSettingsHelper;
    private readonly IMapper _mapper;

    public WebPluginsController(
        ApiContext apiContext,
        IMemoryCache memoryCache,
        WebItemManager webItemManager,
        IHttpContextAccessor httpContextAccessor,
        PermissionContext permissionContext,
        WebPluginManager webPluginManager,
        TenantManager tenantManager,
        CspSettingsHelper cspSettingsHelper,
        IMapper mapper) : base(apiContext, memoryCache, webItemManager, httpContextAccessor)
    {
        _permissionContext = permissionContext;
        _webPluginManager = webPluginManager;
        _tenantManager = tenantManager;
        _cspSettingsHelper = cspSettingsHelper;
        _mapper = mapper;
    }

    [HttpPost("webplugins")]
    public async Task<WebPluginDto> AddWebPluginFromFile(bool system)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (HttpContext.Request.Form.Files == null || HttpContext.Request.Form.Files.Count == 0)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginNoInputFile);
        }

        if (HttpContext.Request.Form.Files.Count > 1)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginToManyInputFiles);
        }

        var file = HttpContext.Request.Form.Files[0] ?? throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginNoInputFile);

        var tenant = await _tenantManager.GetCurrentTenantAsync();

        var webPlugin = await _webPluginManager.AddWebPluginFromFileAsync(tenant.Id, file, system);

        await ChangeCspSettings(webPlugin, webPlugin.Enabled);

        var outDto = _mapper.Map<WebPlugin, WebPluginDto>(webPlugin);

        return outDto;
    }

    [HttpGet("webplugins")]
    public async Task<IEnumerable<WebPluginDto>> GetWebPluginsAsync(bool? enabled = null)
    {
        var tenant = await _tenantManager.GetCurrentTenantAsync();

        var webPlugins = await _webPluginManager.GetWebPluginsAsync(tenant.Id);

        var outDto = _mapper.Map<List<WebPlugin>, List<WebPluginDto>>(webPlugins);

        if (enabled.HasValue)
        {
            outDto = outDto.Where(i => i.Enabled == enabled).ToList();
        }

        return outDto;
    }

    [HttpGet("webplugins/{name}")]
    public async Task<WebPluginDto> GetWebPluginAsync(string name)
    {
        var tenant = await _tenantManager.GetCurrentTenantAsync();

        var webPlugin = await _webPluginManager.GetWebPluginByNameAsync(tenant.Id, name);

        var outDto = _mapper.Map<WebPlugin, WebPluginDto>(webPlugin);

        return outDto;
    }

    [HttpPut("webplugins/{name}")]
    public async Task UpdateWebPluginAsync(string name, WebPluginRequestsDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = await _tenantManager.GetCurrentTenantAsync();

        var webPlugin = await _webPluginManager.UpdateWebPluginAsync(tenant.Id, name, inDto.Enabled, inDto.Settings);

        await ChangeCspSettings(webPlugin, inDto.Enabled);
    }

    [HttpDelete("webplugins/{name}")]
    public async Task DeleteWebPluginAsync(string name)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = await _tenantManager.GetCurrentTenantAsync();

        var webPlugin = await _webPluginManager.DeleteWebPluginAsync(tenant.Id, name);

        await ChangeCspSettings(webPlugin, false);
    }

    private async Task ChangeCspSettings(WebPlugin plugin, bool enabled)
    {
        if (string.IsNullOrEmpty(plugin.CspDomains))
        {
            return;
        }

        var settings = await _cspSettingsHelper.LoadAsync();

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

        _ = await _cspSettingsHelper.SaveAsync(currentDomains.Distinct(), settings.SetDefaultIfEmpty);
    }
}