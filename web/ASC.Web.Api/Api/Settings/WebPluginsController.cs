// (c) Copyright Ascensio System SIA 2010-2022
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
        var tenant = await _tenantManager.GetCurrentTenantAsync();
        var tenantId = system ? Tenant.DefaultTenant : tenant.Id;

        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (HttpContext.Request.Form.Files == null || HttpContext.Request.Form.Files.Count == 0)
        {
            throw new ArgumentException("No input file");
        }

        if (HttpContext.Request.Form.Files.Count > 1)
        {
            throw new ArgumentException("To many input files");
        }

        var file = HttpContext.Request.Form.Files[0] ?? throw new ArgumentException("Input file is null");

        var plugin = await _webPluginManager.AddWebPluginFromFileAsync(tenantId, file);

        await ChangeCspSettings(plugin, plugin.Enabled);

        var outDto = _mapper.Map<DbWebPlugin, WebPluginDto>(plugin);

        var urlTemplate = await _webPluginManager.GetPluginUrlTemplateAsync(tenantId);

        outDto.Url = string.Format(urlTemplate, outDto.Name);

        return outDto;
    }

    [HttpGet("webplugins")]
    public async Task<IEnumerable<WebPluginDto>> GetWebPluginsAsync(bool? enabled = null)
    {
        var tenant = await _tenantManager.GetCurrentTenantAsync();

        var plugins = new List<DbWebPlugin>();

        plugins.AddRange(await _webPluginManager.GetSystemWebPluginsAsync());

        plugins.AddRange(await _webPluginManager.GetWebPluginsAsync(tenant.Id));

        var outDto = _mapper.Map<List<DbWebPlugin>, List<WebPluginDto>>(plugins);

        if (enabled.HasValue)
        {
            outDto = outDto.Where(i => i.Enabled == enabled).ToList();
        }

        if (outDto.Any())
        {
            string urlTemplate = null;
            string systemUrlTemplate = null;

            foreach (var dto in outDto)
            {
                if (dto.System && systemUrlTemplate == null)
                {
                    systemUrlTemplate = await _webPluginManager.GetPluginUrlTemplateAsync(Tenant.DefaultTenant);
                }

                if (!dto.System && urlTemplate == null)
                {
                    urlTemplate = await _webPluginManager.GetPluginUrlTemplateAsync(tenant.Id);
                }

                dto.Url = string.Format(dto.System ? systemUrlTemplate : urlTemplate, dto.Name);
            }
        }

        return outDto;
    }

    [HttpGet("webplugins/{id}")]
    public async Task<WebPluginDto> GetWebPluginByIdAsync(int id)
    {
        var tenant = await _tenantManager.GetCurrentTenantAsync();
        var plugin = await _webPluginManager.GetWebPluginByIdAsync(tenant.Id, id);

        var outDto = _mapper.Map<DbWebPlugin, WebPluginDto>(plugin);

        if (outDto != null)
        {
            var urlTemplate = await _webPluginManager.GetPluginUrlTemplateAsync(tenant.Id);

            outDto.Url = string.Format(urlTemplate, outDto.Name);
        }

        return outDto;
    }

    [HttpPut("webplugins/{id}")]
    public async Task UpdateWebPluginAsync(int id, WebPluginRequestsDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = await _tenantManager.GetCurrentTenantAsync();

        var plugin = await _webPluginManager.UpdateWebPluginAsync(tenant.Id, id, inDto.Enabled);

        await ChangeCspSettings(plugin, inDto.Enabled);
    }

    [HttpDelete("webplugins/{id}")]
    public async Task DeleteWebPluginAsync(int id)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = await _tenantManager.GetCurrentTenantAsync();

        var plugin = await _webPluginManager.DeleteWebPluginAsync(tenant.Id, id);

        await ChangeCspSettings(plugin, false);
    }


    [HttpGet("webplugins/system/{name}")]
    public async Task<WebPluginDto> GetSystemWebPluginByNameAsync(string name)
    {
        var plugin = await _webPluginManager.GetSystemWebPluginAsync(name);

        var outDto = _mapper.Map<DbWebPlugin, WebPluginDto>(plugin);

        if (outDto != null)
        {
            var urlTemplate = await _webPluginManager.GetPluginUrlTemplateAsync(Tenant.DefaultTenant);

            outDto.Url = string.Format(urlTemplate, outDto.Name);
        }

        return outDto;
    }

    [HttpPut("webplugins/system/{name}")]
    public async Task UpdateSystemWebPluginAsync(string name, WebPluginRequestsDto inDto)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var plugin = await _webPluginManager.UpdateSystemWebPluginAsync(name, inDto.Enabled);

        await ChangeCspSettings(plugin, inDto.Enabled);
    }

    [HttpDelete("webplugins/system/{name}")]
    public async Task DeleteSystemWebPluginAsync(string name)
    {
        await _permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var plugin = await _webPluginManager.DeleteSystemWebPluginAsync(name);

        await ChangeCspSettings(plugin, false);
    }

    private async Task ChangeCspSettings(DbWebPlugin plugin, bool enabled)
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