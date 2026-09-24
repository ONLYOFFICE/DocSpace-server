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

[ApiEndpoint(Template = "webplugins")]
public class WebPluginsController(
    IFusionCache fusionCache,
    WebItemManager webItemManager,
    PermissionContext permissionContext,
    WebPluginManager webPluginManager,
    TenantManager tenantManager,
    MessageService messageService,
    CspSettingsHelper cspSettingsHelper,
    WebPluginMapper mapper)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Installs a web plugin into the current portal from an uploaded package, and switches the plugin on straight
    /// away. The package is sent as `multipart/form-data` with exactly one file: a `.zip` archive holding a
    /// `config.json` manifest and a `plugin.js` entry point, under the configured size cap of 5 MB by default.
    /// Editing the portal settings is required, so a portal owner or administrator, and the installation has to have
    /// web plugins and plugin uploading enabled in its configuration. Pass `system=true` to install the plugin for
    /// every portal of the installation, which is accepted on standalone installations only. The call is mutating and
    /// not idempotent: a package whose manifest name is already installed replaces the stored files and keeps the
    /// settings saved for that name, and the domains the manifest declares are added to the portal Content Security
    /// Policy. It returns the freshly installed plugin, enabled, with the `url` its script is served from. A portal
    /// holds up to 100 plugins by default, the manifest name has to be lower-case letters, digits, `_`, `.` or `-`,
    /// and the package is rejected when another installed plugin registers the same JavaScript object under a
    /// different name. List what is installed with `GET api/2.0/settings/webplugins`.
    /// </remarks>
    /// <summary>Add a web plugin</summary>
    /// <path>api/2.0/settings/webplugins</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The installed web plugin, enabled, with the `url` its script is served from", typeof(WebPluginDto))]
    [SwaggerResponse(400, "The uploaded package is missing, of the wrong type, too large, or its manifest is rejected")]
    [SwaggerResponse(403, "Web plugins or plugin uploads are switched off for the installation, or `system` was requested outside a standalone installation")]
    [HttpPost("")]
    public async Task<WebPluginDto> AddWebPluginFromFile(WebPluginFromFileRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (HttpContext.Request.Form.Files == null || HttpContext.Request.Form.Files.Count == 0)
        {
            throw new ArgumentException(Resource.ErrorWebPluginNoInputFile);
        }

        if (HttpContext.Request.Form.Files.Count > 1)
        {
            throw new ArgumentException(Resource.ErrorWebPluginToManyInputFiles);
        }

        var file = HttpContext.Request.Form.Files[0] ?? throw new ArgumentException(Resource.ErrorWebPluginNoInputFile);

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.AddWebPluginFromFileAsync(tenant.Id, file, inDto.System);

        messageService.Send(MessageAction.WebpluginUploaded, MessageTarget.Create($"{webPlugin.PluginName}{webPlugin.Version}"), webPlugin.Name);

        await ChangeCspSettings(webPlugin, webPlugin.Enabled);

        var outDto = await mapper.ToDtoManual(webPlugin);

        return outDto;
    }

    /// <remarks>
    /// Lists the web plugins available in the current portal: the plugins installed for the whole installation first,
    /// then the portal's own, with a portal plugin dropped when an installation-wide plugin already uses its name.
    /// Any authenticated portal member may call it, no settings permission needed, and the installation has to have
    /// web plugins enabled in its configuration. The call is read-only and idempotent. Pass `enabled=true` or
    /// `enabled=false` to keep only the plugins in that state, and leave the parameter out to get every plugin. Each
    /// entry carries the manifest data together with the state the portal stored for that plugin: `enabled`, the
    /// `settings` string, `system` for an installation-wide plugin, and the `url` and `cssUrl` a client loads the
    /// plugin from. An empty list means nothing is installed for this portal, not that plugins are switched off,
    /// which is refused with 403 instead. The list is capped at the configured maximum, 100 plugins by default, and
    /// is not paginated. For one plugin by its manifest name use `GET api/2.0/settings/webplugins/{name}`.
    /// </remarks>
    /// <summary>Get web plugins</summary>
    /// <path>api/2.0/settings/webplugins</path>
    /// <collection>list</collection>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The web plugins available in the portal, the installation-wide ones first", typeof(IEnumerable<WebPluginDto>))]
    [SwaggerResponse(403, "Web plugins are switched off for the installation")]
    [HttpGet("")]
    public async Task<IEnumerable<WebPluginDto>> GetWebPlugins(GetWebPluginsRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var webPlugins = await webPluginManager.GetWebPluginsAsync(tenant.Id);

        List<WebPluginDto> outDto = [];
        foreach (var webPlugin in webPlugins)
        {
            outDto.Add(await mapper.ToDtoManual(webPlugin));
        }

        if (inDto.Enabled.HasValue)
        {
            outDto = outDto.Where(i => i.Enabled == inDto.Enabled).ToList();
        }

        return outDto;
    }

    /// <remarks>
    /// Returns one web plugin of the current portal by its manifest name, looked up over the same set as
    /// `GET api/2.0/settings/webplugins`: the installation-wide plugins plus the portal's own. The `name` is the
    /// manifest name published in the `name` field of that list, matched without regard to case; it is neither the
    /// localized display name nor the JavaScript object name in `pluginName`, so it cannot be taken from the title
    /// shown in the interface. Any authenticated portal member may call it, no settings permission needed, and the
    /// installation has to have web plugins enabled in its configuration. The call is read-only and idempotent. The
    /// response carries the manifest data along with the state the portal stored for that plugin: `enabled`, the
    /// `settings` string, `system`, and the `url` and `cssUrl` a client loads it from. A name that is not installed
    /// is rejected as not found, and 403 means web plugins are switched off for the installation. Change the state of
    /// the plugin with `PUT api/2.0/settings/webplugins/{name}`.
    /// </remarks>
    /// <summary>Get a web plugin by name</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The requested web plugin with the state the portal stored for it", typeof(WebPluginDto))]
    [SwaggerResponse(403, "Web plugins are switched off for the installation")]
    [HttpGet("{name}")]
    public async Task<WebPluginDto> GetWebPlugin(WebPluginNameRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.GetWebPluginByNameAsync(tenant.Id, inDto.Name);

        var outDto = await mapper.ToDtoManual(webPlugin);

        return outDto;
    }

    /// <remarks>
    /// Switches a web plugin of the current portal on or off and stores the settings string the portal keeps for it.
    /// The plugin has to be installed already, so upload its package with `POST api/2.0/settings/webplugins` first,
    /// and `name` is its manifest name as published by `GET api/2.0/settings/webplugins`, matched without regard to
    /// case. Editing the portal settings is required, so a portal owner or administrator, and the installation has to
    /// have web plugins enabled in its configuration. The body replaces the stored state instead of merging into it,
    /// which makes the call idempotent; `settings` is required, so send `{}` when there is nothing to keep, and it is
    /// limited to 255 characters and stored encrypted for this portal alone. Switching the plugin on adds the domains
    /// its manifest declares to the portal Content Security Policy and switching it off takes them away again, and
    /// the connected clients are notified of the new state. Nothing is returned on success. A name that is not
    /// installed is rejected as not found, and 403 means web plugins are switched off or the caller may not edit the
    /// portal settings.
    /// </remarks>
    /// <summary>Update a web plugin</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The state and the settings of the web plugin are saved for the portal")]
    [SwaggerResponse(403, "Web plugins are switched off for the installation, or the caller may not edit the portal settings")]
    [HttpPut("{name}")]
    public async Task UpdateWebPlugin(WebPluginRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.UpdateWebPluginAsync(tenant.Id, inDto.Name, inDto.WebPlugin.Enabled, inDto.WebPlugin.Settings);

        messageService.Send(MessageAction.WebpluginUpdated, MessageTarget.Create($"{webPlugin.PluginName}{webPlugin.Version}"), webPlugin.Name);

        await ChangeCspSettings(webPlugin, inDto.WebPlugin.Enabled);
    }

    /// <remarks>
    /// Removes a web plugin from the current portal and deletes the files of its package from storage. The `name` is
    /// the manifest name published by `GET api/2.0/settings/webplugins`, matched without regard to case. Editing the
    /// portal settings is required, so a portal owner or administrator, and the installation has to have web plugins
    /// and plugin deletion enabled in its configuration. An installation-wide plugin, the one whose `system` field is
    /// true, can be removed on standalone installations only. The call is destructive and cannot be undone: the state
    /// and the settings stored for the plugin are dropped along with its files, the domains its manifest declares are
    /// taken out of the portal Content Security Policy, and the connected clients are notified. Getting the plugin
    /// back means uploading its package again with `POST api/2.0/settings/webplugins`, and the settings it had are
    /// gone. Nothing is returned on success, and a repeated call on a name that is no longer installed is rejected as
    /// not found instead of answered as success. To keep a plugin installed but inactive, switch it off with
    /// `PUT api/2.0/settings/webplugins/{name}` instead.
    /// </remarks>
    /// <summary>Delete a web plugin</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The web plugin and the files of its package are removed from the portal")]
    [SwaggerResponse(403, "Web plugins or plugin deletion are switched off, the caller may not edit the portal settings, or the plugin is installation-wide outside a standalone installation")]
    [HttpDelete("{name}")]
    public async Task DeleteWebPlugin(WebPluginNameRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.DeleteWebPluginAsync(tenant.Id, inDto.Name);

        messageService.Send(MessageAction.WebpluginDeleted, MessageTarget.Create($"{webPlugin.PluginName}{webPlugin.Version}"), webPlugin.Name);

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