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
    /// Installs a web plugin from an uploaded package for the current portal or, with `personal=true`, for the
    /// calling user alone, and switches it on straight away. The package is one `multipart/form-data` file: a `.zip`
    /// archive holding a `config.json` manifest and a `plugin.js` entry point, 5 MB at most by default. The
    /// installation has to have web plugins and plugin uploading enabled; a portal plugin needs the portal-settings
    /// right, so a portal owner or administrator, and a personal one any member except a guest while personal plugins
    /// are enabled. Pass `system=true` to install a portal plugin for every portal, on standalone installations only.
    /// The call is mutating and not idempotent: a package whose name is already installed at the same level replaces
    /// the stored files and keeps the stored settings. The domains the manifest declares join the portal Content
    /// Security Policy for a portal plugin, and are allowed on the owner's pages alone for a personal one. It returns
    /// the installed plugin, enabled, with the `url` its script is served from. A portal holds up to 100 plugins and a
    /// member 10 personal ones by default; the name takes lower-case letters, digits, `_`, `.` and `-`, no other
    /// plugin of the same level may register the same `pluginName`, and a personal plugin may not take the `name` or
    /// `pluginName` of a portal or installation-wide plugin. A request a browser sends with the session cookie from a
    /// page of another origin is refused. List the plugins with `GET api/2.0/settings/webplugins`.
    /// </remarks>
    /// <summary>Add a web plugin</summary>
    /// <path>api/2.0/settings/webplugins</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The installed web plugin, enabled, with the `url` its script is served from", typeof(WebPluginDto))]
    [SwaggerResponse(400, "The uploaded package is missing, more than one file was sent, the package is of the wrong type, empty or too large, the archive or its manifest is rejected, a personal plugin takes the name of a portal or installation-wide plugin, `personal` is combined with `system`, or the portal or the caller already holds the maximum number of plugins")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or for a personal plugin is a guest or personal plugins are switched off; the request is not a form, a browser sent it with the session cookie from a page of another origin, web plugins or plugin uploads are switched off for the installation, `system` was requested outside a standalone installation, or the domains the plugin declares would push the Content Security Policy header over its size limit")]
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
    /// Lists the web plugins available to the caller: the plugins installed for the whole installation first, then the
    /// portal's own, with a portal plugin dropped when an installation-wide plugin already uses its name, then the
    /// caller's personal plugins whose `name` and `pluginName` no plugin above holds. Pass `personal=true` to list the
    /// caller's personal plugins alone, the ones left out of the full list for a taken name included. Any
    /// authenticated portal member may call it, no settings permission needed, and the installation has to have web
    /// plugins enabled in its configuration; `personal=true` is refused to a guest and while personal plugins are
    /// switched off. The call is read-only and idempotent. Pass `enabled=true` or `enabled=false` to keep only the
    /// plugins in that state. Each entry carries the manifest data with the state stored for that plugin: `enabled`,
    /// the `settings` string, `system` for an installation-wide plugin, `personal` for the caller's own, and the `url`
    /// and `cssUrl` a client loads it from. An empty list means nothing is installed, not that plugins are switched
    /// off, which is refused with 403 instead. A portal holds up to 100 plugins and a member 10 personal ones by
    /// default, and the list is not paginated. For one plugin by its manifest name use
    /// `GET api/2.0/settings/webplugins/{name}`.
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
    /// Returns one web plugin by its manifest name: a portal or installation-wide plugin, or with `personal=true` one
    /// of the caller's personal plugins, which the lookup without it does not reach. The `name` is the manifest name
    /// published in the `name` field of `GET api/2.0/settings/webplugins`, matched without regard to case; it is
    /// neither the localized display name nor the JavaScript object name in `pluginName`, so it cannot be taken from
    /// the title shown in the interface. Any authenticated portal member may call it, no settings permission needed,
    /// and the installation has to have web plugins enabled in its configuration; `personal=true` is refused to a
    /// guest and while personal plugins are switched off. The call is read-only and idempotent. The response carries
    /// the manifest data along with the state stored for that plugin: `enabled`, the `settings` string, `system`,
    /// `personal`, and the `url` and `cssUrl` a client loads it from. A name that is not installed at the level asked
    /// for is rejected as not found. Change the state of the plugin with `PUT api/2.0/settings/webplugins/{name}`.
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
    /// Switches a web plugin on or off and stores its settings string: a plugin of the current portal, or with
    /// `personal=true` one of the caller's personal plugins. The plugin has to be installed already, so upload its
    /// package with `POST api/2.0/settings/webplugins` first, and `name` is its manifest name as published by
    /// `GET api/2.0/settings/webplugins`, matched without regard to case. The installation has to have web plugins
    /// enabled; a portal plugin needs the portal-settings right, so a portal owner or administrator, and a personal one
    /// any member except a guest while personal plugins are enabled. The body replaces the stored state instead of
    /// merging into it, which makes the call idempotent; `settings` is required, so send `{}` when there is nothing to
    /// keep, and it is limited to 255 characters and stored encrypted. For a portal plugin the state is shared by the
    /// whole portal, switching it on adds the domains its manifest declares to the portal Content Security Policy and
    /// switching it off takes them away again, and the connected clients are notified. For a personal plugin the
    /// state is kept for the caller alone, the domains are allowed on the caller's pages alone, and the caller's other
    /// open pages pick the change up on their next load. Nothing is returned on success. A name that is not installed
    /// at the level asked for is rejected as not found.
    /// </remarks>
    /// <summary>Update a web plugin</summary>
    /// <path>api/2.0/settings/webplugins/{name}</path>
    [Tags("Settings / Webplugins")]
    [SwaggerResponse(200, "The state and the settings of the web plugin are saved")]
    [SwaggerResponse(400, "The request body cannot be read or has no `settings`, or `settings` is longer than 255 characters")]
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
    /// Picks up a web plugin whose files were replaced in storage without an upload, for example a new version of an
    /// installation-wide plugin put in place during a deployment. The `name` is the manifest name published by
    /// `GET api/2.0/settings/webplugins`, matched without regard to case. Editing the portal settings is required, so
    /// a portal owner or administrator, and the installation has to have web plugins enabled in its configuration.
    /// The server compares the version of the plugin it serves with the `version` of the manifest in storage and,
    /// only when they differ or the plugin appeared in or vanished from storage, re-reads the plugin list on every
    /// server: for all portals when the plugin is installation-wide, for this portal alone otherwise, and notifies
    /// the connected clients of this portal. The call is idempotent: with matching versions nothing changes, so a
    /// repeat is safe. The response is the plugin as served after the check. The stored state, the settings and the
    /// Content Security Policy of the portal are left as they are. Personal plugins are not covered: their files
    /// change through an upload alone. To install a new version use `POST api/2.0/settings/webplugins` instead.
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
    /// Removes a web plugin and deletes the files of its package from storage: a plugin of the current portal, or with
    /// `personal=true` one of the caller's personal plugins, leaving a portal plugin of the same name in place. The
    /// `name` is the manifest name published by `GET api/2.0/settings/webplugins`, matched without regard to case.
    /// The installation has to have web plugins enabled; a portal plugin needs the portal-settings right and plugin
    /// deletion enabled in the configuration, and an installation-wide one, whose `system` field is true, can be
    /// removed on standalone installations only, while a personal plugin is removed by its owner, any member except
    /// a guest, while personal plugins are enabled. The call is destructive and cannot be undone: the state and the
    /// settings stored for the plugin are dropped along with its files. For a portal plugin the domains its manifest
    /// declares are taken out of the portal Content Security Policy and the connected clients are notified. Getting
    /// the plugin back means uploading its package again with `POST api/2.0/settings/webplugins`. Nothing is returned
    /// on success, and a repeated call on a name that is no longer installed is rejected as not found. To keep a
    /// plugin installed but inactive, switch it off with `PUT api/2.0/settings/webplugins/{name}` instead.
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