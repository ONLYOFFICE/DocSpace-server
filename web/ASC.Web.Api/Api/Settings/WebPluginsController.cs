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
    WebPluginMapper mapper,
    ILogger<WebPluginsController> logger)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Installs a web plugin from an uploaded package and switches it on, for the portal or, with `personal` set to
    /// true, for the caller alone. Send the package as `multipart/form-data`: a `.zip` archive with a `config.json`
    /// manifest and a `plugin.js` script. A portal plugin requires the EditPortalSettings permission, which the
    /// portal owner and DocSpace admins have. A personal plugin needs no permission, but a guest gets 403, and so
    /// does everyone while the installation turns off personal plugins. If the installation turns off web plugins or
    /// their upload, the call also returns 403. Set `system` to true to install a portal plugin for every portal, on a
    /// standalone installation only. The call changes data and can be repeated: a package with an installed name
    /// replaces its files and keeps its settings. The domains of a portal plugin go into the portal Content Security
    /// Policy, and if this makes the header too large, the call returns 403 but the plugin stays installed. The
    /// domains of a personal plugin apply to the caller's own pages only. The call returns 400 for a bad package or
    /// manifest, a `pluginName` another plugin uses, or a personal plugin that takes the `name` or `pluginName` of a
    /// portal plugin. A portal holds at most 100 plugins and a member 10 personal ones by default. A request that a
    /// browser sends with its credentials from a page of another origin returns 403. To list plugins, use
    /// `GET api/2.0/settings/webplugins`.
    /// </remarks>
    /// <summary>Add a web plugin</summary>
    /// <path>api/2.0/settings/webplugins</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The installed web plugin, enabled, with the `url` its script is served from", typeof(WebPluginDto))]
    [SwaggerResponse(400, "The uploaded package is missing, more than one file was sent, the package is of the wrong type, empty or too large, the archive or its manifest is rejected, a personal plugin takes the name of a portal or installation-wide plugin, `personal` is combined with `system`, or the portal or the caller already holds the maximum number of plugins")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or for a personal plugin is a guest or personal plugins are switched off; the request is not a form, a browser sent it with its credentials from a page of another origin, web plugins or plugin uploads are switched off for the installation, `system` was requested outside a standalone installation, or the domains the plugin declares would push the Content Security Policy header over its size limit")]
    [HttpPost("")]
    public async Task<WebPluginDto> AddWebPluginFromFile(WebPluginFromFileRequestDto inDto)
    {
        await DemandSameOriginRequestAsync();

        if (!inDto.Personal)
        {
            await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        }

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

        var webPlugin = await webPluginManager.AddWebPluginFromFileAsync(tenant.Id, file, inDto.System, inDto.Personal);

        messageService.Send(MessageAction.WebpluginUploaded, MessageTarget.Create($"{webPlugin.PluginName}{webPlugin.Version}"), webPlugin.Name);

        if (webPlugin.Personal)
        {
            await SyncPersonalCspCookieAsync(tenant.Id);
        }
        else
        {
            await ChangeCspSettings(webPlugin, webPlugin.Enabled);
        }

        var outDto = await mapper.ToDtoManual(webPlugin);

        return outDto;
    }

    /// <remarks>
    /// Returns the web plugins available to the caller. The installation-wide plugins come first, then the portal
    /// plugins, then the caller's personal plugins. The list does not show a portal plugin with the name of an
    /// installation-wide plugin, or a personal plugin whose `name` or `pluginName` a plugin above already uses. With
    /// `personal` set to true, the call returns only the caller's personal plugins, the hidden ones included. Any
    /// signed-in user can call it, but with `personal` a guest gets 403, and so does everyone while the installation
    /// turns off personal plugins. If the installation turns off web plugins, the call returns 403. The call changes
    /// no data; it sets or clears the `asc_personal_csp` cookie that `GET api/2.0/security/csp` reads. Each entry
    /// includes the `enabled` state and the `settings` stored for the plugin, and `personal` marks the caller's own.
    /// With `enabled` set, the call returns only the plugins in that state. An empty list means that there are no
    /// plugins of this kind. The list has no paging; a portal holds at most 100 plugins and a member 10 personal ones
    /// by default. To get one plugin, use `GET api/2.0/settings/webplugins/{name}`.
    /// </remarks>
    /// <summary>Get web plugins</summary>
    /// <path>api/2.0/settings/webplugins</path>
    /// <collection>list</collection>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The web plugins available to the caller, the installation-wide ones first and the personal ones last", typeof(IEnumerable<WebPluginDto>))]
    [SwaggerResponse(403, "Web plugins are switched off for the installation, or `personal=true` was sent by a guest or while personal plugins are switched off")]
    [HttpGet("")]
    public async Task<IEnumerable<WebPluginDto>> GetWebPlugins(GetWebPluginsRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var webPlugins = inDto.Personal
            ? await webPluginManager.GetPersonalWebPluginsAsync(tenant.Id)
            : await webPluginManager.GetAvailableWebPluginsAsync(tenant.Id);

        if (!inDto.Personal)
        {
            SetPersonalCspCookie(WebPluginManager.GetPersonalCspDomains(webPlugins).Count != 0);
        }

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
    /// Returns one web plugin by its manifest name. Without `personal`, the call searches the portal plugins,
    /// including the installation-wide ones. With `personal` set to true, it searches only the caller's personal
    /// plugins. Take the name from the `name` field of `GET api/2.0/settings/webplugins`. The call ignores the case of
    /// the name. Any signed-in user can call it, but with `personal` a guest gets 403, and so does everyone while the
    /// installation turns off personal plugins. If the installation turns off web plugins, the call returns 403. The
    /// call is read-only. The response includes the `enabled` state and the `settings` stored for the plugin. If no
    /// plugin has this name, the call returns 404. To change the state of the plugin, use
    /// `PUT api/2.0/settings/webplugins/{name}`.
    /// </remarks>
    /// <summary>Get a web plugin by name</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The requested web plugin with the state stored for it", typeof(WebPluginDto))]
    [SwaggerResponse(403, "Web plugins are switched off for the installation, or `personal=true` was sent by a guest or while personal plugins are switched off")]
    [SwaggerResponse(404, "No web plugin with this manifest name is installed at the level asked for")]
    [HttpGet("{name}")]
    public async Task<WebPluginDto> GetWebPlugin(WebPluginNameRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = inDto.Personal
            ? await webPluginManager.GetPersonalWebPluginByNameAsync(tenant.Id, inDto.Name)
            : await webPluginManager.GetWebPluginByNameAsync(tenant.Id, inDto.Name);

        var outDto = await mapper.ToDtoManual(webPlugin);

        return outDto;
    }

    /// <remarks>
    /// Switches an installed web plugin on or off and saves its settings string: a portal plugin, or with `personal`
    /// set to true one of the caller's personal plugins. To install a plugin, use `POST api/2.0/settings/webplugins`.
    /// Take the name from the `name` field of `GET api/2.0/settings/webplugins`. The call ignores the case of the name.
    /// A portal plugin requires the EditPortalSettings permission. A personal plugin needs no permission, but a guest
    /// gets 403, and so does everyone while the installation turns off personal plugins. If the installation turns
    /// off web plugins, the call also returns 403. The call changes data. The body replaces the stored state and
    /// settings, so you can repeat the call with the same body. Switching a portal plugin on adds the domains of its
    /// manifest to the portal Content Security Policy, and switching it off removes them. If the domains make the
    /// policy header too large, the call returns 403, but saves the new state. A personal plugin keeps its state for
    /// the caller alone, and the caller's other open pages see the change after a reload. The call returns 400 if the
    /// body has no `settings` or `settings` is longer than 4096 characters. If no plugin has this name, the call
    /// returns 404.
    /// </remarks>
    /// <summary>Update a web plugin</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The state and the settings of the web plugin are saved")]
    [SwaggerResponse(400, "The request body cannot be read or has no `settings`, or `settings` is longer than 4096 characters")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or for a personal plugin is a guest or personal plugins are switched off; web plugins are switched off for the installation, or switching the plugin on would push the Content Security Policy header over its size limit")]
    [SwaggerResponse(404, "No web plugin with this manifest name is installed at the level asked for")]
    [HttpPut("{name}")]
    public async Task UpdateWebPlugin(WebPluginRequestsDto inDto)
    {
        if (!inDto.Personal)
        {
            await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        }

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.UpdateWebPluginAsync(tenant.Id, inDto.Name, inDto.WebPlugin.Enabled, inDto.WebPlugin.Settings, inDto.Personal);

        messageService.Send(MessageAction.WebpluginUpdated, MessageTarget.Create($"{webPlugin.PluginName}{webPlugin.Version}"), webPlugin.Name);

        if (webPlugin.Personal)
        {
            await SyncPersonalCspCookieAsync(tenant.Id);
        }
        else
        {
            await ChangeCspSettings(webPlugin, inDto.WebPlugin.Enabled);
        }
    }

    /// <remarks>
    /// Reloads a web plugin whose files someone replaced in storage without an upload. For example, a deployment can
    /// put a new version of an installation-wide plugin in storage. Take the name from the `name` field of
    /// `GET api/2.0/settings/webplugins`. The call ignores the case of the name. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. If the installation turns
    /// off web plugins, the call also returns 403. The call compares the version it serves with the `version` of the
    /// manifest in storage. The server reloads the plugin list if they differ or the plugin appeared or disappeared.
    /// For an installation-wide plugin, it reloads the list of every portal. The call changes data only then, so you
    /// can repeat it. It does not change the stored state, the settings, or the Content Security Policy, and it does
    /// not reach personal plugins, which change only through an upload. The call returns the plugin as the portal
    /// serves it after the check. If no plugin has this name after the check, the call returns 404. To install a new
    /// version, use `POST api/2.0/settings/webplugins`.
    /// </remarks>
    /// <summary>Refresh a web plugin</summary>
    /// <path>api/2.0/settings/webplugins/{name}/refresh</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The web plugin as served after the version check", typeof(WebPluginDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right, or web plugins are switched off for the installation")]
    [SwaggerResponse(404, "No web plugin with this manifest name is available in the portal after the check")]
    [HttpPut("{name}/refresh")]
    public async Task<WebPluginDto> RefreshWebPlugin(RefreshWebPluginRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.RefreshWebPluginAsync(tenant.Id, inDto.Name);

        var outDto = await mapper.ToDtoManual(webPlugin);

        return outDto;
    }

    /// <remarks>
    /// Deletes a web plugin together with the files of its package: a portal plugin, or with `personal` set to true
    /// one of the caller's personal plugins, which leaves a portal plugin of the same name in place. Take the name
    /// from the `name` field of `GET api/2.0/settings/webplugins`. The call ignores the case of the name. A portal
    /// plugin requires the EditPortalSettings permission, and the installation has to allow plugin deletion. A
    /// personal plugin needs neither, but a guest gets 403, and so does everyone while the installation turns off
    /// personal plugins. If the installation turns off web plugins, the call returns 403. A plugin with `system` set
    /// to true is installation-wide. Only a standalone installation can delete it, and its deletion removes the
    /// plugin from every portal. The call cannot be undone. It deletes the stored state and settings of the plugin,
    /// and for a portal plugin it removes its domains from the portal Content Security Policy. To restore the plugin,
    /// upload its package again with `POST api/2.0/settings/webplugins`. Its old settings do not come back. If no
    /// plugin has this name or its files are missing in storage, the call returns 404. A repeated call returns 404.
    /// To keep the plugin but stop it, switch it off with `PUT api/2.0/settings/webplugins/{name}`.
    /// </remarks>
    /// <summary>Delete a web plugin</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The web plugin and the files of its package are removed")]
    [SwaggerResponse(403, "Web plugins are switched off; for a portal plugin plugin deletion is switched off, the caller has no portal-settings right, or the plugin is installation-wide outside a standalone installation; for a personal plugin the caller is a guest or personal plugins are switched off")]
    [SwaggerResponse(404, "No web plugin with this manifest name is installed at the level asked for, or the files of its package are missing from storage")]
    [HttpDelete("{name}")]
    public async Task DeleteWebPlugin(WebPluginNameRequestDto inDto)
    {
        if (!inDto.Personal)
        {
            await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        }

        var tenant = tenantManager.GetCurrentTenant();

        var webPlugin = await webPluginManager.DeleteWebPluginAsync(tenant.Id, inDto.Name, inDto.Personal);

        messageService.Send(MessageAction.WebpluginDeleted, MessageTarget.Create($"{webPlugin.PluginName}{webPlugin.Version}"), webPlugin.Name);

        if (webPlugin.Personal)
        {
            await SyncPersonalCspCookieAsync(tenant.Id);
        }
        else
        {
            await ChangeCspSettings(webPlugin, false);
        }
    }

    private async Task DemandSameOriginRequestAsync()
    {
        var ambientCredentials = Request.Cookies.ContainsKey(CookiesManager.AuthCookiesName) ||
                                 Request.Headers.Authorization.ToString().StartsWith("Basic ", StringComparison.OrdinalIgnoreCase);

        if (!ambientCredentials)
        {
            return;
        }

        var fetchSite = Request.Headers["Sec-Fetch-Site"].ToString();

        var sameOrigin = fetchSite.Length > 0
            ? fetchSite is "same-origin" or "none"
            : await IsPortalOriginAsync(Request.Headers.Origin.ToString());

        if (!sameOrigin)
        {
            throw new SecurityException("The request comes from another origin");
        }
    }

    private async Task<bool> IsPortalOriginAsync(string origin)
    {
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        if (originUri.Host.Equals(Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var originTenant = await tenantManager.GetTenantAsync(originUri.Host, false);

        return originTenant?.Id == tenantManager.GetCurrentTenantId();
    }

    private async Task SyncPersonalCspCookieAsync(int tenantId)
    {
        try
        {
            var personalCspDomains = await webPluginManager.GetPersonalCspDomainsAsync(tenantId);

            SetPersonalCspCookie(personalCspDomains.Count != 0);
        }
        catch (Exception e)
        {
            logger.WarningWithException("Failed to update the personal CSP cookie", e);
        }
    }

    private void SetPersonalCspCookie(bool hasPersonalCspDomains)
    {
        if (hasPersonalCspDomains)
        {
            Response.Cookies.Append(WebPluginManager.PersonalCspCookieName, "1", new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        }
        else if (Request.Cookies.ContainsKey(WebPluginManager.PersonalCspCookieName))
        {
            Response.Cookies.Delete(WebPluginManager.PersonalCspCookieName);
        }
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