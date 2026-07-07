// (c) Copyright Ascensio System SIA 2009-2026
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

using ASC.Core.Common.Data;

namespace ASC.Web.Api.Controllers;

/// <remarks>
/// Portal applications API. Returns the list of apps registered in configuration with per-tenant state and JSON settings overrides.
/// </remarks>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("apps")]
public class AppsController(
    AppSettingsService appSettingsService,
    AppsSocketManager appsSocketManager,
    PermissionContext permissionContext,
    TenantManager tenantManager) : ControllerBase
{
    /// <summary>
    /// Get all apps
    /// </summary>
    /// <remarks>
    /// Returns the full list of portal applications declared in configuration, merged with per-tenant overrides
    /// (enabled state and JSON settings).
    /// </remarks>
    /// <path>api/2.0/apps</path>
    /// <collection>list</collection>
    [Tags("Apps")]
    [SwaggerResponse(200, "List of applications", typeof(List<AppDto>))]
    [HttpGet]
    public async Task<List<AppDto>> GetAllAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var apps = await appSettingsService.GetAppsAsync(tenantId);
        return apps.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Get a single app
    /// </summary>
    /// <remarks>
    /// Returns a single application by id with the per-tenant enabled state and settings JSON.
    /// </remarks>
    /// <path>api/2.0/apps/{id}</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "Application info", typeof(AppDto))]
    [SwaggerResponse(404, "Application not found")]
    [HttpGet("{id}")]
    public async Task<AppDto> GetAsync(GetAppRequestDto inDto)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var app = await appSettingsService.GetAppAsync(tenantId, inDto.Id)
            ?? throw new ItemNotFoundException($"App '{inDto.Id}' not found");

        return MapToDto(app);
    }

    /// <summary>
    /// Get app settings
    /// </summary>
    /// <remarks>
    /// Returns the JSON settings document saved for the specified application, or null if no overrides exist.
    /// </remarks>
    /// <path>api/2.0/apps/{id}/settings</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "Application settings JSON", typeof(JsonElement))]
    [SwaggerResponse(404, "Application not found")]
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

    /// <summary>
    /// Enable or disable an app
    /// </summary>
    /// <remarks>
    /// Toggles the enabled state of the application for the current tenant. Requires portal administrator permissions.
    /// </remarks>
    /// <path>api/2.0/apps/{id}/enabled</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "Updated application info", typeof(AppDto))]
    [SwaggerResponse(403, "You don't have enough permission to manage apps")]
    [SwaggerResponse(404, "Application not found")]
    [HttpPut("{id}/enabled")]
    public async Task<AppDto> SetEnabledAsync(SetAppEnabledRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenantId();
        var app = await appSettingsService.SetEnabledAsync(tenantId, inDto.Id, inDto.Body.Enabled);

        await appsSocketManager.ChangeAppEnabledAsync(app.Id, app.Enabled);

        return MapToDto(app);
    }

    /// <summary>
    /// Save app settings
    /// </summary>
    /// <remarks>
    /// Saves an arbitrary JSON settings document for the specified application for the current tenant.
    /// Requires portal administrator permissions.
    /// </remarks>
    /// <path>api/2.0/apps/{id}/settings</path>
    [Tags("Apps")]
    [SwaggerResponse(200, "Updated application info", typeof(AppDto))]
    [SwaggerResponse(400, "Settings is not valid JSON")]
    [SwaggerResponse(403, "You don't have enough permission to manage apps")]
    [SwaggerResponse(404, "Application not found")]
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
