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

/// <remarks>
/// The portal's outgoing mail relay: the SMTP server this portal hands its notifications, invitations and
/// confirmation links to, and the test that proves the relay accepts them. A portal that has never saved settings of
/// its own runs on the mail configuration of the installation - `isDefaultSettings` reports that state, and a cloud
/// portal is never shown the server-wide values behind it. Every operation here needs the portal-settings right of a
/// DocSpace administrator and the SMTP settings section enabled for the portal, otherwise the call is answered with
/// 402. Saving settings does not check them: store them with `POST api/2.0/smtpsettings/smtp`, queue a test message
/// with `GET api/2.0/smtpsettings/smtp/test` and poll `GET api/2.0/smtpsettings/smtp/test/status` until `completed`
/// is true to learn whether the relay works. `DELETE api/2.0/smtpsettings/smtp` puts the portal back on the
/// configuration of the installation. A stored password is never returned by any operation here.
/// </remarks>
[Scope]
[ApiEndpoint("smtpsettings", "smtp")]
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
    /// Returns the SMTP settings that the portal sends its mail through. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. If the SMTP settings section is turned
    /// off for the portal, the call returns 402. The call is read-only. If `isDefaultSettings` is true, the portal has
    /// no settings of its own and uses those of the installation. A standalone installation then returns its values,
    /// and a cloud portal returns empty fields. The password in `credentialsUserPassword` is always empty. So ask the
    /// user for it before you save the settings again. To check that the mail server accepts mail, use
    /// `GET api/2.0/smtpsettings/smtp/test`.
    /// </remarks>
    /// <summary>
    /// Get SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The SMTP settings stored for the portal, with an empty password and `isDefaultSettings` telling whether the configuration of the installation is in use", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
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
    /// Saves the SMTP settings of the portal and replaces the saved ones or the settings of the installation.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. If the SMTP settings section is turned off for the portal, the call returns 402. The call changes
    /// data. You can repeat it with the same body. The portal uses the new settings for its next email. The call does not
    /// connect to `host` or check the settings. To check them, use `GET api/2.0/smtpsettings/smtp/test` after this
    /// call. If `port` is omitted, the portal saves `25`. The call does not save `useNtlm`, so the portal never signs
    /// in with NTLM. It returns the saved settings with an empty password. The call returns 400 if `host` or
    /// `senderAddress` is empty, or `senderDisplayName` is missing. It also returns 400 if `enableAuth` is true and
    /// `credentialsUserName` or `credentialsUserPassword` is empty. It also returns 400 if a text field is too long or
    /// `port` is out of range. Nothing is saved after a 400. To return to the settings of the installation, use
    /// `DELETE api/2.0/smtpsettings/smtp`.
    /// </remarks>
    /// <summary>
    /// Save SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The SMTP settings now stored for the portal, with an empty password", typeof(SmtpSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read, `host` or `senderAddress` is empty, `senderDisplayName` is missing, `enableAuth` is true without `credentialsUserName` or `credentialsUserPassword`, a text field is longer than 255 characters, or `port` is outside 1-65535")]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
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
    /// Deletes the SMTP settings of the portal. After this call, the portal uses the mail settings of the
    /// installation. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. If the SMTP settings section is turned off for the portal, the call returns 402. The
    /// call cannot be undone. To use the deleted settings again, save them with `POST api/2.0/smtpsettings/smtp`.
    /// You can repeat the call. If the portal has no settings of its own, it changes nothing. The call returns the
    /// settings now in use, with `isDefaultSettings` true. A standalone installation returns its values, and a cloud
    /// portal returns empty fields. The password is always empty.
    /// </remarks>
    /// <summary>
    /// Reset SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The settings in force after the reset - the configuration of the installation, or an empty settings object in a cloud portal", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
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
    /// Starts a job that sends a test email through the saved SMTP settings of the portal. The portal sends it to the
    /// email address of the caller. First, save the settings with `POST api/2.0/smtpsettings/smtp`. Without them, the
    /// job uses the settings of the installation. Requires the EditPortalSettings permission, which the portal owner
    /// and DocSpace admins have. Other callers get 403. If the SMTP settings section is turned off for the portal, the
    /// call returns 402. The call queues work and sends an email. While a test runs, a repeated call returns the same
    /// job. The call returns before the email is sent. Repeat `GET api/2.0/smtpsettings/smtp/test/status` until
    /// `completed` is true. Then an empty `error` means that the mail server accepted the email. Otherwise, `error`
    /// holds the reason. A failed test does not make the request fail. By default, the limit is 5 calls in 15 minutes
    /// per user. More calls return 429.
    /// </remarks>
    /// <summary>
    /// Test SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp/test</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The state of the queued test message, to be polled until `completed` is true", typeof(SmtpOperationStatusDto))]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("test")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task<SmtpOperationStatusDto> TestSmtpSettings()
    {
        await CheckSmtpPermissionsAsync();

        var settings = (await coreConfiguration.GetDefaultSmtpSettingsAsync()).MapToDto();

        var tenant = tenantManager.GetCurrentTenant();

        await smtpOperation.StartSmtpJob(settings, tenant, securityContext.CurrentAccount.ID);

        return await smtpOperation.GetStatus(tenant);
    }

    /// <remarks>
    /// Returns the state of the SMTP test email that `GET api/2.0/smtpsettings/smtp/test` started for the portal.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. If the SMTP settings section is turned off for the portal, the call returns 402. This is the call to
    /// poll. Repeat it until `completed` is true. The call changes data only once. The first answer with `completed`
    /// true deletes the finished job. So keep `error` from that answer. If `error` is empty, the mail server accepted the email. An empty answer
    /// means that no test was started, or its result was already returned.
    /// </remarks>
    /// <summary>
    /// Get SMTP test status
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp/test/status</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The state of the test message of the portal, or an empty answer when no test is on record", typeof(SmtpOperationStatusDto))]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("test/status")]
    public async Task<SmtpOperationStatusDto> GetSmtpOperationStatus()
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
