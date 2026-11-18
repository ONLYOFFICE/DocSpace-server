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
    MessageService messageService,
    PermissionContext permissionContext,
    TenantManager tenantManager) : ControllerBase
{
    /// <remarks>
    /// Returns all portal applications of the installation, each with its state on the current portal. An application
    /// is a feature module that the portal can turn on and configure, such as `ai-rooms` or `docs-cloud`. The
    /// installation configuration sets the list and the default state of each application. The API cannot add or
    /// remove applications. Any signed-in user can call it. The call is read-only. The list follows the order of the
    /// configuration, without paging or a total. The `settings` field is empty until the portal saves settings for
    /// the application. An empty list means that the installation has no applications, not that all are turned off. To
    /// read one application, use `GET api/2.0/apps/{id}`. To change the state or the settings, use
    /// `PUT api/2.0/apps/{id}/enabled` or `PUT api/2.0/apps/{id}/settings`.
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
    /// Returns one portal application with its state on the current portal. An application is a feature module that
    /// the portal can turn on and configure, such as `ai-rooms` or `docs-cloud`. Take the identifier from
    /// `GET api/2.0/apps`. It is case-sensitive. Any signed-in user can call it. The call is read-only. The `settings`
    /// field is empty until the portal saves settings for the application. To read only the settings, use
    /// `GET api/2.0/apps/{id}/settings`. If the installation has no application with this identifier, the call
    /// returns 404.
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
    /// Returns only the settings of one portal application on the current portal. The answer is the saved JSON value
    /// itself, not an application object. Take the identifier from `GET api/2.0/apps`. It is case-sensitive. Any
    /// signed-in user can call it. The call is read-only. The portal returns the value as it was saved and never checks
    /// its shape. An empty result means that the portal has no saved settings for the application. Then the application
    /// uses its own defaults. To read the enabled state, use `GET api/2.0/apps/{id}`. If the installation has no
    /// application with this identifier, the call returns 404.
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

        return ParseSettings(app.Settings);
    }

    /// <remarks>
    /// Turns one portal application on or off for the current portal. Take the identifier from `GET api/2.0/apps`. It
    /// is case-sensitive. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. The call changes data. It saves the flag for this portal instead of the default from the
    /// configuration. You can repeat it with the same body. Turning an application off keeps its saved settings. They
    /// apply again when you turn it on. The call returns the application in its new state, with its settings. To
    /// change the settings, use `PUT api/2.0/apps/{id}/settings`. The call returns 400 if the body is not valid JSON or
    /// has no `enabled` field. If the installation has no application with this identifier, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Enable or disable an app
    /// </summary>
    /// <path>api/2.0/apps/{id}/enabled</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "The application in its new state, with the saved settings document left untouched", typeof(AppDto))]
    [SwaggerResponse(400, "The request body is not a valid JSON document or has no enabled field, so the state is not changed")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [SwaggerResponse(404, "No application with this identifier is configured on this installation")]
    [HttpPut("{id}/enabled")]
    public async Task<AppDto> SetEnabledAsync(SetAppEnabledRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenantId();
        var app = await appSettingsService.SetEnabledAsync(tenantId, inDto.Id, inDto.Body.Enabled);

        messageService.Send(app.Enabled ? MessageAction.AppEnabled : MessageAction.AppDisabled, app.Id);

        await appsSocketManager.ChangeAppEnabledAsync(app.Id, app.Enabled);

        return MapToDto(app);
    }

    /// <remarks>
    /// Saves the settings of one portal application for the current portal. Take the identifier from
    /// `GET api/2.0/apps`. It is case-sensitive. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. The call changes data, and you can repeat it with the same body. It
    /// replaces the whole saved value and does not merge into it. So first read the current value with
    /// `GET api/2.0/apps/{id}/settings`, change it, and send it complete. The portal accepts any JSON value and never
    /// checks its shape. If `settings` is `null` or missing, the call deletes the saved value. Then the application
    /// uses its own defaults. The call returns the application in its new state, with the saved settings. It does not
    /// change the enabled state. To turn the application on or off, use `PUT api/2.0/apps/{id}/enabled`. The call
    /// returns 400 if the body is not valid JSON. If the installation has no application with this identifier, the
    /// call returns 404.
    /// </remarks>
    /// <summary>
    /// Save app settings
    /// </summary>
    /// <path>api/2.0/apps/{id}/settings</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "The application in its new state, with the stored settings document", typeof(AppDto))]
    [SwaggerResponse(400, "The request body is not a valid JSON document, so no settings are stored")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
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
            Settings = ParseSettings(app.Settings)
        };
    }

    // JsonElement.Parse, not JsonDocument.Parse(...).RootElement: the document rents its buffer from the array pool
    // and has to be disposed, while the element handed out here must outlive it. JsonElement.Parse returns a
    // self-contained element instead.
    private static JsonElement? ParseSettings(string settings)
    {
        return string.IsNullOrEmpty(settings) ? null : JsonElement.Parse(settings);
    }
}
