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

using ASC.Core.Common.Data;

namespace ASC.Web.Api.Controllers;

/// <remarks>
/// Portal applications: the feature modules declared in the installation configuration, such as `ai-rooms` or
/// `docs-cloud`, together with the enabled flag and the JSON settings document that every portal stores for them.
/// Reading an application is open to any authenticated portal member, while changing one requires an administrator
/// allowed to edit the portal settings.
/// </remarks>
[Scope]
[ApiEndpoint("apps")]
public class AppsController(
    AppSettingsService appSettingsService,
    AppsSocketManager appsSocketManager,
    PermissionContext permissionContext,
    TenantManager tenantManager) : ControllerBase
{
    /// <remarks>
    /// Returns every portal application available on this installation, each with the state it has for the current
    /// portal: the feature modules the portal can turn on and configure, such as `ai-rooms` or `docs-cloud`. The set
    /// of applications and their initial enabled state come from the installation configuration and cannot be changed
    /// through the API; only the enabled flag and the settings document are stored per portal, by
    /// `PUT api/2.0/apps/{id}/enabled` and `PUT api/2.0/apps/{id}/settings`. Any authenticated portal member may read
    /// the list. The call is read-only and idempotent. The list follows the order of the configuration, and every item
    /// carries the application identifier, whether the application is enabled for the current portal, and the settings
    /// JSON document saved for it, which is empty while the portal has never saved one. An empty list means that no
    /// applications are configured on this installation, not that they are all disabled. There is neither paging nor
    /// filtering here: to read a single application use `GET api/2.0/apps/{id}`.
    /// </remarks>
    /// <summary>
    /// Get all apps
    /// </summary>
    /// <path>api/2.0/apps</path>
    /// <collection>list</collection>
    [Tags("Apps")]
    [SwaggerResponse(200, "The portal applications configured on this installation, each with the enabled state and the settings of the current portal", typeof(List<AppDto>))]
    [HttpGet]
    public async Task<List<AppDto>> GetAllAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var apps = await appSettingsService.GetAppsAsync(tenantId);
        return apps.Select(MapToDto).ToList();
    }

    /// <remarks>
    /// Returns one portal application by its identifier - one of the feature modules the portal can turn on, such as
    /// `ai-rooms` or `docs-cloud` - with the enabled state and the settings document stored for the current portal.
    /// The identifier must be an application declared in the installation configuration: take it
    /// from `GET api/2.0/apps`, because an unknown identifier is rejected instead of creating anything. Any
    /// authenticated portal member may read it. The call is read-only and idempotent. The result carries the
    /// identifier, the enabled flag of the current portal and the settings JSON document, which is empty while the
    /// portal has never saved settings for this application. An application that is not configured on this
    /// installation fails with 404, so this is also the way to find out whether an application exists here at all.
    /// Use `GET api/2.0/apps` to read all applications in one call, or `GET api/2.0/apps/{id}/settings` when only the
    /// settings document is needed.
    /// </remarks>
    /// <summary>
    /// Get an app
    /// </summary>
    /// <path>api/2.0/apps/{id}</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "The application with the enabled state and the settings of the current portal", typeof(AppDto))]
    [SwaggerResponse(404, "No application with this identifier is configured on this installation")]
    [HttpGet("{id}")]
    public async Task<AppDto> GetAsync(GetAppRequestDto inDto)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var app = await appSettingsService.GetAppAsync(tenantId, inDto.Id)
            ?? throw new ItemNotFoundException($"App '{inDto.Id}' not found");

        return MapToDto(app);
    }

    /// <remarks>
    /// Returns only the settings document of one portal application, such as `ai-rooms` or `docs-cloud`: the JSON
    /// that the current portal has saved for it through `PUT api/2.0/apps/{id}/settings`, with no wrapper around it.
    /// The identifier must be an application declared in the installation configuration, as listed by
    /// `GET api/2.0/apps`. Any authenticated portal member
    /// may read it. The call is read-only and idempotent. The document comes back exactly as it was saved: its shape
    /// is defined by the application itself and is not validated by the portal, and an empty result means that the
    /// portal has never saved settings for this application, so the application uses its own defaults. The enabled
    /// state is not part of the answer: read it from `GET api/2.0/apps/{id}`.
    /// </remarks>
    /// <summary>
    /// Get app settings
    /// </summary>
    /// <path>api/2.0/apps/{id}/settings</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "The settings document saved for the application, or an empty result if the portal has never saved one", typeof(JsonElement?))]
    [SwaggerResponse(404, "No application with this identifier is configured on this installation")]
    [HttpGet("{id}/settings")]
    public async Task<JsonElement?> GetSettingsAsync(GetAppRequestDto inDto)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var app = await appSettingsService.GetAppAsync(tenantId, inDto.Id)
            ?? throw new ItemNotFoundException($"App '{inDto.Id}' not found");

        return string.IsNullOrEmpty(app.Settings)
            ? null
            : JsonDocument.Parse(app.Settings).RootElement;
    }

    /// <remarks>
    /// Turns one portal application on or off for the current portal, and notifies the clients connected to the portal
    /// so that they can show or hide it without being reloaded. The identifier must be an application declared in the
    /// installation configuration, as listed by `GET api/2.0/apps`. The caller must be a portal administrator allowed
    /// to edit the portal settings. The call is mutating and idempotent: it stores the flag for this portal, overriding
    /// the default that the configuration gives the application, and repeating it with the same value changes nothing.
    /// Disabling an application does not delete its settings document, which stays saved and applies again as soon as
    /// the application is enabled. The response is the application in its new state, including that settings document.
    /// Only the enabled flag is affected here: to change the settings document use `PUT api/2.0/apps/{id}/settings`.
    /// </remarks>
    /// <summary>
    /// Enable or disable an app
    /// </summary>
    /// <path>api/2.0/apps/{id}/enabled</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "The application in its new state, with the saved settings document left untouched", typeof(AppDto))]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [SwaggerResponse(404, "No application with this identifier is configured on this installation")]
    [HttpPut("{id}/enabled")]
    public async Task<AppDto> SetEnabledAsync(SetAppEnabledRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenantId();
        var app = await appSettingsService.SetEnabledAsync(tenantId, inDto.Id, inDto.Body.Enabled);

        await appsSocketManager.ChangeAppEnabledAsync(app.Id, app.Enabled);

        return MapToDto(app);
    }

    /// <remarks>
    /// Stores the application-specific settings document of one portal application for the current portal. The
    /// identifier must be an application declared in the installation configuration, as listed by `GET api/2.0/apps`.
    /// The caller must be a portal administrator allowed to edit the portal settings. The call is mutating and
    /// idempotent, and it replaces the whole document instead of merging into it: read the current one with
    /// `GET api/2.0/apps/{id}/settings`, change it and send it back complete, or send `null` to drop the saved document
    /// and let the application fall back to its own defaults. Any valid JSON value is accepted, since the content is
    /// stored as it is and is interpreted by the application rather than by the portal, while a body that is not valid
    /// JSON fails with 400 and stores nothing. The response is the application in its new state, with the stored
    /// document echoed back. Unlike `PUT api/2.0/apps/{id}/enabled`, this operation sends no notification to the
    /// connected clients, which pick the new settings up on their next read.
    /// </remarks>
    /// <summary>
    /// Save app settings
    /// </summary>
    /// <path>api/2.0/apps/{id}/settings</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "The application in its new state, with the stored settings document", typeof(AppDto))]
    [SwaggerResponse(400, "The request body is not a valid JSON document, so no settings are stored")]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [SwaggerResponse(404, "No application with this identifier is configured on this installation")]
    [HttpPut("{id}/settings")]
    public async Task<AppDto> SetSettingsAsync(SetAppSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenantId();

        var settings = inDto.Body.Settings;
        var json = settings.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : settings.GetRawText();

        var app = await appSettingsService.SetSettingsAsync(tenantId, inDto.Id, json);

        return MapToDto(app);
    }

    private static AppDto MapToDto(AppItem app)
    {
        return new AppDto
        {
            Id = app.Id,
            Enabled = app.Enabled,
            Settings = string.IsNullOrEmpty(app.Settings)
                ? null
                : JsonDocument.Parse(app.Settings).RootElement
        };
    }
}
