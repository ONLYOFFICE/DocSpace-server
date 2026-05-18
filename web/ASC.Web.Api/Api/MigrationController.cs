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

namespace ASC.Api.Migration;

/// <remarks>
/// Migration API.
/// </remarks>
/// <name>migration</name>
[DefaultRoute]
[ApiController]
[ControllerName("migration")]
public class MigrationController(
    TenantManager tenantManager,
    UserManager userManager,
    AuthContext authContext,
    StudioNotifyService studioNotifyService,
    MigrationCore migrationCore,
    MigrationLogger migrationLogger) : ControllerBase
{
    /// <remarks>
    /// Returns a list of available migrations.
    /// </remarks>
    /// <summary>
    /// Get migrations
    /// </summary>
    /// <path>api/2.0/migration/list</path>
    /// <collection>list</collection>
    [Tags("Migration")]
    [SwaggerResponse(200, "Ok", typeof(string[]))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("list")]
    public async Task<string[]> ListMigrations()
    {
        await DemandPermissionAsync();
        return migrationCore.GetAvailableMigrations();
    }

    /// <remarks>
    /// Uploads and initializes a migration with a migrator name specified in the request.
    /// </remarks>
    /// <summary>
    /// Upload and initialize migration
    /// </summary>
    /// <path>api/2.0/migration/init/{migratorName}</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("init/{migratorName}")]
    public async Task UploadAndInitializeMigration(MigratorNameRequestDto inDto)
    {
        await DemandPermissionAsync();

        await migrationCore.StartParseAsync(inDto.MigratorName);
    }

    /// <remarks>
    /// Returns the migration status.
    /// </remarks>
    /// <summary>
    /// Get migration status
    /// </summary>
    /// <path>api/2.0/migration/status</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "Ok", typeof(MigrationStatusDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("status")]
    public async Task<MigrationStatusDto> GetMigrationStatus()
    {
        await DemandPermissionAsync();
        try
        {
            var status = await migrationCore.GetStatusAsync();
            if (status != null)
            {
                var result = new MigrationStatusDto
                {
                    Progress = status.Percentage,
                    Error = status.Exception != null ? status.Exception.Message : "",
                    IsCompleted = status.IsCompleted,
                    ParseResult = status.MigrationApiInfo
                };
                return result;
            }
        }
        catch
        {

        }
        return null;
    }

    /// <remarks>
    /// Cancels the migration.
    /// </remarks>
    /// <summary>
    /// Cancel migration
    /// </summary>
    /// <path>api/2.0/migration/cancel</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("cancel")]
    public async Task CancelMigration()
    {
        await DemandPermissionAsync();

        await migrationCore.StopAsync();
    }

    /// <remarks>
    /// Clears the migration.
    /// </remarks>
    /// <summary>
    /// Clear migration
    /// </summary>
    /// <path>api/2.0/migration/clear</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("clear")]
    public async Task ClearMigration()
    {
        await DemandPermissionAsync();

        await migrationCore.ClearAsync();
    }

    /// <remarks>
    /// Starts the migration process.
    /// </remarks>
    /// <summary>
    /// Start migration
    /// </summary>
    /// <path>api/2.0/migration/migrate</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("migrate")]
    public async Task StartMigration(MigrationApiInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        await DemandPermissionAsync();

        var tenant = tenantManager.GetCurrentTenant();
        var user = await userManager.GetUsersAsync(authContext.CurrentAccount.ID);

        if (user.IsOwner(tenant))
        {
            await migrationCore.StartAsync(info);
            return;
        }

        var adminEmailsToImport = (info.Users ?? [])
            .Where(u => u.ShouldImport && u.UserType == EmployeeType.DocSpaceAdmin)
            .Select(x => x.Email)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (adminEmailsToImport.Count > 0)
        {
            var admins = (await userManager.GetUsersAsync(EmployeeStatus.All, EmployeeType.DocSpaceAdmin))
                .Select(x => x.Email)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!adminEmailsToImport.IsSubsetOf(admins))
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        await migrationCore.StartAsync(info);
    }

    /// <remarks>
    /// Returns the migration logs.
    /// </remarks>
    /// <summary>
    /// Get migration logs
    /// </summary>
    /// <path>api/2.0/migration/logs</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("logs")]
    public async Task GetMigrationLogs()
    {
        await DemandPermissionAsync();

        var status = await migrationCore.GetStatusAsync();
        if (status == null)
        {
            throw new Exception(MigrationResource.MigrationProgressException);
        }
        migrationLogger.Init(status.LogName);
        await using var stream = await migrationLogger.GetStreamAsync();

        Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue("migration.log"));
        Response.ContentType = "text/plain; charset=UTF-8";
        Response.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);

        await stream.CopyToAsync(Response.Body);
    }

    /// <remarks>
    /// Finishes the migration process.
    /// </remarks>
    /// <summary>
    /// Finish migration
    /// </summary>
    /// <path>api/2.0/migration/finish</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("finish")]
    public async Task FinishMigration(FinishDto inDto)
    {
        await DemandPermissionAsync();

        if (inDto.IsSendWelcomeEmail)
        {
            var status = await migrationCore.GetStatusAsync();
            if (status == null)
            {
                throw new Exception(MigrationResource.MigrationProgressException);
            }
            var emails = status.ImportedUsers;
            foreach (var email in emails)
            {
                var u = await userManager.GetUserByEmailAsync(email);
                if (u.IsActive)
                {
                    continue;
                }
                await studioNotifyService.UserInfoActivationAsync(u);
            }
        }
        await migrationCore.ClearAsync();
    }

    private async Task DemandPermissionAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }
    }
}