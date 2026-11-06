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
    /// Installs a web plugin in the portal from an uploaded package and switches it on. Send the package as
    /// `multipart/form-data`. The archive holds a `config.json` manifest and a `plugin.js` script. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. If the
    /// installation turns off web plugins or their upload, the call also returns 403. Set `system` to true to
    /// install the plugin for every portal. If the installation is not standalone, `system` set to true returns 403.
    /// The call changes data. You can repeat it with the same package. A package with an installed name
    /// replaces its files and keeps its settings. The call adds the manifest domains to the portal Content Security
    /// Policy. If this makes the policy header too large, the call returns 403, but the plugin stays installed. The
    /// call returns 400 if the request has no file, several files, or a file that is not a `.zip`. It also returns
    /// 400 for an empty or too large file, or a bad archive or manifest. A manifest name has only lowercase letters,
    /// digits, `_`, `.`, and `-`. It also returns 400 if another plugin uses the same JavaScript object name,
    /// `pluginName`. A new plugin also returns 400 if the portal already has the maximum, 100 plugins by default. To
    /// list plugins, use `GET api/2.0/settings/webplugins`.
    /// </remarks>
    /// <summary>Add a web plugin</summary>
    /// <path>api/2.0/settings/webplugins</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The installed web plugin, enabled, with the `url` its script is served from", typeof(WebPluginDto))]
    [SwaggerResponse(400, "The uploaded package is missing, more than one file was sent, the package is of the wrong type, empty or too large, the archive or its manifest is rejected, or the portal already holds the maximum number of plugins")]
    [SwaggerResponse(403, "The caller has no portal-settings right, the request is not a form, web plugins or plugin uploads are switched off for the installation, `system` was requested outside a standalone installation, or the domains the plugin declares would push the Content Security Policy header over its size limit")]
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
    /// Returns the web plugins of the portal, including the plugins installed for the whole installation. The
    /// installation-wide plugins come first, then the portal plugins. The list does not show a portal plugin with
    /// the name of an installation-wide plugin. Any signed-in user can call it. If the installation turns off web
    /// plugins, the call returns 403. The call is read-only. Each entry includes the `enabled` state and the
    /// `settings` that the portal stored for the plugin. With `enabled` set, the call returns only the plugins in
    /// that state. An empty list means that the portal has no plugins of this kind. The list has no paging and at
    /// most 100 plugins by default. To get one plugin, use `GET api/2.0/settings/webplugins/{name}`.
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
    /// Returns one web plugin of the portal by its manifest name. The call searches the same plugins as
    /// `GET api/2.0/settings/webplugins`, including the installation-wide ones. Take the name from the `name` field
    /// of that list. The call ignores the case of the name. Any signed-in user can call it. If the installation
    /// turns off web plugins, the call returns 403. The call is read-only. The response includes the `enabled` state
    /// and the `settings` that the portal stored for the plugin. If no plugin has this name, the call returns 404. To
    /// change the state of the plugin, use `PUT api/2.0/settings/webplugins/{name}`.
    /// </remarks>
    /// <summary>Get a web plugin by name</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The requested web plugin with the state the portal stored for it", typeof(WebPluginDto))]
    [SwaggerResponse(403, "Web plugins are switched off for the installation")]
    [SwaggerResponse(404, "No web plugin with this manifest name is available in the portal")]
    [HttpGet("{name}")]
    public async Task<WebPluginDto> GetWebPlugin(WebPluginNameRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.GetWebPluginByNameAsync(tenant.Id, inDto.Name);

        var outDto = await mapper.ToDtoManual(webPlugin);

        return outDto;
    }

    /// <remarks>
    /// Switches an installed web plugin of the portal on or off and saves its settings string. To install a plugin,
    /// use `POST api/2.0/settings/webplugins`. Take the name from the `name` field of
    /// `GET api/2.0/settings/webplugins`. The call ignores the case of the name. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. If the installation turns
    /// off web plugins, the call also returns 403. The call changes data. The body replaces the stored state and
    /// settings, so you can repeat the call with the same body. Switching the plugin on adds the domains of its
    /// manifest to the portal Content Security Policy. Switching it off removes them. If the domains make the policy
    /// header too large, the call returns 403, but saves the new state. The call returns 400 if the body has no
    /// `settings` or `settings` is too long. If no plugin has this name, the call returns 404.
    /// </remarks>
    /// <summary>Update a web plugin</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The state and the settings of the web plugin are saved for the portal")]
    [SwaggerResponse(400, "The request body cannot be read or has no `settings`, or `settings` is longer than 255 characters")]
    [SwaggerResponse(403, "The caller has no portal-settings right, web plugins are switched off for the installation, or switching the plugin on would push the Content Security Policy header over its size limit")]
    [SwaggerResponse(404, "No web plugin with this manifest name is available in the portal")]
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
    /// Reloads a web plugin whose files someone replaced in storage without an upload. For example, a deployment can
    /// put a new version of an installation-wide plugin in storage. Take the name from the `name` field of
    /// `GET api/2.0/settings/webplugins`. The call ignores the case of the name. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. If the installation turns
    /// off web plugins, the call also returns 403. The call compares the version it serves with the `version` of the
    /// manifest in storage. The server reloads the plugin list if they differ or the plugin appeared or disappeared.
    /// For an installation-wide plugin, it reloads the list of every portal. The call changes data only then, so you
    /// can repeat it. It does not change the stored state, the settings, or the Content Security Policy. The call
    /// returns the plugin as the portal serves it after the check. If no plugin has this name after the check, the
    /// call returns 404. To install a new version, use `POST api/2.0/settings/webplugins`.
    /// </remarks>
    /// <summary>Refresh a web plugin</summary>
    /// <path>api/2.0/settings/webplugins/{name}/refresh</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The web plugin as served after the version check", typeof(WebPluginDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right, or web plugins are switched off for the installation")]
    [SwaggerResponse(404, "No web plugin with this manifest name is available in the portal after the check")]
    [HttpPut("{name}/refresh")]
    public async Task<WebPluginDto> RefreshWebPlugin(WebPluginNameRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.RefreshWebPluginAsync(tenant.Id, inDto.Name);

        var outDto = await mapper.ToDtoManual(webPlugin);

        return outDto;
    }

    /// <remarks>
    /// Deletes a web plugin from the portal together with the files of its package. Take the name from the `name`
    /// field of `GET api/2.0/settings/webplugins`. The call ignores the case of the name. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. If the
    /// installation turns off web plugins or their deletion, the call also returns 403. A plugin with `system` set to
    /// true is installation-wide. Only a standalone installation can delete it. Other installations get 403. Its
    /// deletion removes the plugin from every portal. The call cannot be undone. It deletes the stored state and
    /// settings of the plugin and removes its domains from the portal Content Security Policy. To restore the plugin,
    /// upload its package again with `POST api/2.0/settings/webplugins`. Its old settings do not come back. If no
    /// plugin has this name or its files are missing in storage, the call returns 404. A repeated call returns 404.
    /// To keep the plugin but stop it, switch it off with `PUT api/2.0/settings/webplugins/{name}`.
    /// </remarks>
    /// <summary>Delete a web plugin</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The web plugin and the files of its package are removed from the portal")]
    [SwaggerResponse(403, "Web plugins or plugin deletion are switched off, the caller has no portal-settings right, or the plugin is installation-wide outside a standalone installation")]
    [SwaggerResponse(404, "No web plugin with this manifest name is available in the portal, or the files of its package are missing from storage")]
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