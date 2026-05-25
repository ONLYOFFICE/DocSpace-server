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

using Microsoft.AspNetCore.RateLimiting;

namespace ASC.Api.Settings;

///<remarks>
/// SMTP settings API.
///</remarks>
[Scope]
[ApiController]
[DefaultRoute("smtp")]
[ControllerName("smtpsettings")]
public class SmtpSettingsController(
        PermissionContext permissionContext,
        CoreConfiguration coreConfiguration,
        CoreBaseSettings coreBaseSettings,
        SecurityContext securityContext,
        SmtpOperation smtpOperation,
        TenantManager tenantManager)
    : ControllerBase
{
    /// <remarks>
    /// Returns the current portal SMTP settings.
    /// </remarks>
    /// <summary>
    /// Get the SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "SMTP settings", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("")]
    public async Task<SmtpSettingsDto> GetSmtpSettings()
    {
        await CheckSmtpPermissionsAsync();

        var current = await coreConfiguration.GetDefaultSmtpSettingsAsync();

        if (current.IsDefaultSettings && !coreBaseSettings.Standalone)
        {
            current = SmtpSettings.Empty;
        }

        var settings = current.MapToDto();
        settings.CredentialsUserPassword = "";

        return settings;
    }

    /// <remarks>
    /// Saves the SMTP settings for the current portal.
    /// </remarks>
    /// <summary>
    /// Save the SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "SMTP settings", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPost("")]
    public async Task<SmtpSettingsDto> SaveSmtpSettings(SmtpSettingsDto inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);

        await CheckSmtpPermissionsAsync();

        //TODO: Add validation check



        var settingConfig = ToSmtpSettingsConfig(inDto);

        await coreConfiguration.SetSmtpSettingsAsync(settingConfig);

        var settings = settingConfig.MapToDto();
        settings.CredentialsUserPassword = "";

        return settings;
    }

    private SmtpSettings ToSmtpSettingsConfig(SmtpSettingsDto inDto)
    {
        var settingsConfig = new SmtpSettings(
            inDto.Host,
            inDto.Port ?? SmtpSettings.DefaultSmtpPort,
            inDto.SenderAddress,
            inDto.SenderDisplayName)
        {
            EnableSSL = inDto.EnableSSL,
            EnableAuth = inDto.EnableAuth
        };

        if (inDto.EnableAuth)
        {
            settingsConfig.SetCredentials(inDto.CredentialsUserName, inDto.CredentialsUserPassword);
        }

        return settingsConfig;
    }

    /// <remarks>
    /// Resets the SMTP settings of the current portal.
    /// </remarks>
    /// <summary>
    /// Reset the SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "Default SMTP settings", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpDelete("")]
    public async Task<SmtpSettingsDto> ResetSmtpSettings()
    {
        await CheckSmtpPermissionsAsync();

        if (!(await coreConfiguration.GetDefaultSmtpSettingsAsync()).IsDefaultSettings)
        {
            await coreConfiguration.SetSmtpSettingsAsync(null);
        }

        var current = await coreConfiguration.GetDefaultSmtpSettingsAsync();

        if (current.IsDefaultSettings && !coreBaseSettings.Standalone)
        {
            current = SmtpSettings.Empty;
        }

        var settings = current.MapToDto();
        settings.CredentialsUserPassword = "";
        return settings;
    }

    /// <remarks>
    /// Tests the SMTP settings for the current portal (sends test message to the user email).
    /// </remarks>
    /// <summary>
    /// Test the SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp/test</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "SMTP operation status", typeof(SmtpOperationStatusRequestsDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("test")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<SmtpOperationStatusRequestsDto> TestSmtpSettings()
    {
        await CheckSmtpPermissionsAsync();

        var settings = (await coreConfiguration.GetDefaultSmtpSettingsAsync()).MapToDto();

        var tenant = tenantManager.GetCurrentTenant();

        await smtpOperation.StartSmtpJob(settings, tenant, securityContext.CurrentAccount.ID);

        return await smtpOperation.GetStatus(tenant);
    }

    /// <remarks>
    /// Returns the status of the SMTP testing process.
    /// </remarks>
    /// <summary>
    /// Get the SMTP testing process status
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp/test/status</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "SMTP operation status", typeof(SmtpOperationStatusRequestsDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("test/status")]
    public async Task<SmtpOperationStatusRequestsDto> GetSmtpOperationStatus()
    {
        await CheckSmtpPermissionsAsync();

        return await smtpOperation.GetStatus(tenantManager.GetCurrentTenant());
    }

    private async Task CheckSmtpPermissionsAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        if (!SetupInfo.IsVisibleSettings(nameof(ManagementType.SmtpSettings)))
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }
}
