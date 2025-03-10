// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Api.Settings;

///<summary>
/// SMTP settings API.
///</summary>
[Scope]
[ApiController]
[DefaultRoute("smtp")]
[ControllerName("smtpsettings")]
public class SmtpSettingsController(
        PermissionContext permissionContext,
        CoreConfiguration coreConfiguration,
        CoreBaseSettings coreBaseSettings,
        IMapper mapper,
        SecurityContext securityContext,
        SmtpOperation smtpOperation,
        TenantManager tenantManager)
    : ControllerBase
{
    /// <summary>
    /// Returns the current portal SMTP settings.
    /// </summary>
    /// <short>
    /// Get the SMTP settings
    /// </short>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "SMTP settings", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("")]
    public async Task<SmtpSettingsDto> GetSmtpSettingsAsync()
    {
        await CheckSmtpPermissionsAsync();

        var current = await coreConfiguration.GetDefaultSmtpSettingsAsync();

        if (current.IsDefaultSettings && !coreBaseSettings.Standalone)
        {
            current = SmtpSettings.Empty;
        }

        var settings = mapper.Map<SmtpSettings, SmtpSettingsDto>(current);
        settings.CredentialsUserPassword = "";

        return settings;
    }

    /// <summary>
    /// Saves the SMTP settings for the current portal.
    /// </summary>
    /// <short>
    /// Save the SMTP settings
    /// </short>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "SMTP settings", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpPost("")]
    public async Task<SmtpSettingsDto> SaveSmtpSettingsAsync(SmtpSettingsDto inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);
        
        await CheckSmtpPermissionsAsync();

        //TODO: Add validation check

        

        var settingConfig = ToSmtpSettingsConfig(inDto);

        await coreConfiguration.SetSmtpSettingsAsync(settingConfig);

        var settings = mapper.Map<SmtpSettings, SmtpSettingsDto>(settingConfig);
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

    /// <summary>
    /// Resets the SMTP settings of the current portal.
    /// </summary>
    /// <short>
    /// Reset the SMTP settings
    /// </short>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "Default SMTP settings", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpDelete("")]
    public async Task<SmtpSettingsDto> ResetSmtpSettingsAsync()
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

        var settings = mapper.Map<SmtpSettings, SmtpSettingsDto>(current);
        settings.CredentialsUserPassword = "";

        return settings;
    }

    /// <summary>
    /// Tests the SMTP settings for the current portal (sends test message to the user email).
    /// </summary>
    /// <short>
    /// Test the SMTP settings
    /// </short>
    /// <path>api/2.0/smtpsettings/smtp/test</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "SMTP operation status", typeof(SmtpOperationStatusRequestsDto))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("test")]
    public async Task<SmtpOperationStatusRequestsDto> TestSmtpSettings()
    {
        await CheckSmtpPermissionsAsync();

        var settings = mapper.Map<SmtpSettings, SmtpSettingsDto>(await coreConfiguration.GetDefaultSmtpSettingsAsync());

        var tenant = tenantManager.GetCurrentTenant();

        await smtpOperation.StartSmtpJob(settings, tenant, securityContext.CurrentAccount.ID);

        return await smtpOperation.GetStatus(tenant);
    }

    /// <summary>
    /// Returns the SMTP test process status.
    /// </summary>
    /// <short>
    /// Get the SMTP test process status
    /// </short>
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
            throw new BillingException(Resource.ErrorNotAllowedOption, "Smtp");
        }
    }
}