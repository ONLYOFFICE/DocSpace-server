// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Api.Migration;

/// <summary>
/// Migration API.
/// </summary>
/// <name>migration</name>
[DefaultRoute]
[ApiController]
[ControllerName("migration")]
public class MigrationController(
    UserManager userManager,
    AuthContext authContext,
    StudioNotifyService studioNotifyService,
    MigrationCore migrationCore,
    MigrationLogger migrationLogger) : ControllerBase
{
    /// <summary>
    /// Returns a list of available migrations.
    /// </summary>
    /// <short>
    /// Get migrations
    /// </short>
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

    /// <summary>
    /// Uploads and initializes a migration with a migrator name specified in the request.
    /// </summary>
    /// <short>
    /// Upload and initialize migration
    /// </short>
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

    /// <summary>
    /// Returns the migration status.
    /// </summary>
    /// <short>
    /// Get migration status
    /// </short>
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

    /// <summary>
    /// Cancels the migration.
    /// </summary>
    /// <short>
    /// Cancel migration
    /// </short>
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

    /// <summary>
    /// Clears the migration.
    /// </summary>
    /// <short>
    /// Clear migration
    /// </short>
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

    /// <summary>
    /// Starts the migration process.
    /// </summary>
    /// <short>
    /// Start migration
    /// </short>
    /// <path>api/2.0/migration/migrate</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("migrate")]
    public async Task StartMigration(MigrationApiInfo info)
    {
        await DemandPermissionAsync();

        await migrationCore.StartAsync(info);
    }

    /// <summary>
    /// Returns the migration logs.
    /// </summary>
    /// <short>
    /// Get migration logs
    /// </short>
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

    /// <summary>
    /// Finishes the migration process.
    /// </summary>
    /// <short>
    /// Finish migration
    /// </short>
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
