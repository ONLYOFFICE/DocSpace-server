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
    /// Returns the SMTP relay this portal sends its own mail through - host, port, sender identity and authentication
    /// flags - as it is stored for the portal. Nothing has to be called first; the caller needs the portal-settings
    /// right of a DocSpace administrator, and the SMTP settings section has to be enabled for the portal, otherwise
    /// the call is answered with 402. The call is read-only and safe to repeat. `isDefaultSettings` is true when the
    /// portal has no settings of its own and runs on the mail configuration of the installation: a standalone
    /// installation then shows those server-wide values, while a cloud portal is answered with an empty settings
    /// object instead, so an empty `host` together with `isDefaultSettings` true means nothing was ever saved here.
    /// `credentialsUserPassword` always comes back empty - the stored password cannot be read back, and a client that
    /// saves the settings again has to ask the user for it once more. `port` is the port that was saved, and settings
    /// saved without one are stored with `25`. To find out whether the returned relay actually accepts mail, queue a
    /// test with `GET api/2.0/smtpsettings/smtp/test`.
    /// </remarks>
    /// <summary>
    /// Get SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The SMTP settings stored for the portal, with an empty password and `isDefaultSettings` telling whether the configuration of the installation is in use", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
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
    /// Stores the SMTP relay that this portal will hand all of its own mail to, replacing whatever was saved before
    /// and taking the portal off the mail configuration of the installation. Nothing has to be called first; the
    /// caller needs the portal-settings right of a DocSpace administrator, and the SMTP settings section has to be
    /// enabled for the portal, otherwise the call is answered with 402. The call is mutating and idempotent - the
    /// same body saved twice leaves the same settings - and it applies to the next message the portal sends. The
    /// settings are stored unverified, no connection to `host` is attempted, so queue
    /// `GET api/2.0/smtpsettings/smtp/test` afterwards to find out whether they work. `host` and `senderAddress` must
    /// not be empty, `senderDisplayName` has to be present, and `enableAuth` true also requires `credentialsUserName`
    /// and `credentialsUserPassword`; a request that misses any of them is rejected and nothing is saved. `port`
    /// falls back to `25` when it is omitted, and `useNtlm` is accepted but not stored, so the saved settings always
    /// authenticate with a plain user name and password. The answer repeats the stored settings with the password
    /// emptied. Use `DELETE api/2.0/smtpsettings/smtp` to return to the configuration of the installation.
    /// </remarks>
    /// <summary>
    /// Save SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The SMTP settings now stored for the portal, with an empty password", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
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
    /// Deletes the SMTP settings of this portal and puts it back on the mail configuration of the installation, so
    /// the portal stops using the relay saved by `POST api/2.0/smtpsettings/smtp`. Nothing has to be called first;
    /// the caller needs the portal-settings right of a DocSpace administrator, and the SMTP settings section has to
    /// be enabled for the portal, otherwise the call is answered with 402. The call is destructive and cannot be
    /// undone - the host, the sender identity and the credentials are gone and have to be entered again - but it is
    /// idempotent, and on a portal that has no settings of its own it changes nothing. Portal mail itself keeps
    /// working as long as the installation has a relay of its own configured. The answer holds the settings that are
    /// in force after the reset, always with `isDefaultSettings` true: the server-wide values in a standalone
    /// installation, an empty settings object in a cloud portal, and an empty `credentialsUserPassword` in both. Read
    /// them back at any time with `GET api/2.0/smtpsettings/smtp`.
    /// </remarks>
    /// <summary>
    /// Reset SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The settings in force after the reset - the configuration of the installation, or an empty settings object in a cloud portal", typeof(SmtpSettingsDto))]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
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
    /// Queues a background job that sends a test message through the SMTP settings currently stored for the portal to
    /// the email address of the calling user, and returns the state of that job. Save the settings with
    /// `POST api/2.0/smtpsettings/smtp` first: the job always takes the stored settings and nothing can be passed to
    /// it here. The caller needs the portal-settings right of a DocSpace administrator, and the SMTP settings section
    /// has to be enabled for the portal, otherwise the call is answered with 402. The call is mutating, it sends
    /// mail, and it is rate-limited to five requests per fifteen minutes per user and path by default, answering 429
    /// above that; while a test is still running the same job is returned instead of a second one being started. The
    /// message has not been sent when the answer arrives: poll `GET api/2.0/smtpsettings/smtp/test/status` until
    /// `completed` is true, then read `error` - empty means the relay accepted the message, otherwise it carries the
    /// reason. `percents` climbs to 100 and `status` names the step reached, such as `Connect to host` or
    /// `Send test message`. An unreachable relay is reported in `error` after a 30-second connection timeout, not as
    /// a failed request.
    /// </remarks>
    /// <summary>
    /// Test SMTP settings
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp/test</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The state of the queued test message, to be polled until `completed` is true", typeof(SmtpOperationStatusRequestsDto))]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
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
    /// Returns the state of the test message that `GET api/2.0/smtpsettings/smtp/test` queued for this portal, and is
    /// the operation to poll while that test runs. A test has to be queued first; the caller needs the
    /// portal-settings right of a DocSpace administrator, and the SMTP settings section has to be enabled for the
    /// portal, otherwise the call is answered with 402. The call changes no settings, but it is not free of
    /// consequence: the first answer that reports `completed` true also discards the finished job, so a later call no
    /// longer knows about it - take `error` from that answer and keep it. An empty answer means the portal has no
    /// test on record, either because none was queued or because its result has already been read. While the job
    /// runs, `percents` climbs to 100 and `status` names the step reached, such as `Connect to host` or
    /// `Send test message`; `error` is empty until something fails and stays empty when the relay accepted the
    /// message. `id` identifies the queued job, of which a portal only ever has one.
    /// </remarks>
    /// <summary>
    /// Get SMTP test status
    /// </summary>
    /// <path>api/2.0/smtpsettings/smtp/test/status</path>
    [Tags("Security / SMTP settings")]
    [SwaggerResponse(200, "The state of the test message of the portal, or an empty answer when no test is on record", typeof(SmtpOperationStatusRequestsDto))]
    [SwaggerResponse(402, "The SMTP settings section is not enabled for this portal")]
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
