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
/// Importing a portal from another product: the source products this installation can read, the parse pass that
/// inspects an uploaded backup, the import itself, its progress and its log, and the clean-up that ends it. Every
/// operation acts on the portal of the caller and is open to DocSpace administrators only. The backup is not sent
/// through these operations - it is uploaded to `migrationFileUpload.ashx` first - and the operations that start,
/// cancel or clear an import only queue the request, so what each of them did is read from
/// `GET api/2.0/migration/status`. A portal runs one job at a time, in this order: parse with
/// `POST api/2.0/migration/init/{migratorName}`, import with `POST api/2.0/migration/migrate`, close with
/// `POST api/2.0/migration/finish`.
/// </remarks>
/// <name>migration</name>
[ApiEndpoint("migration")]
public class MigrationController(
    TenantManager tenantManager,
    UserManager userManager,
    AuthContext authContext,
    StudioNotifyService studioNotifyService,
    MigrationCore migrationCore,
    MigrationLogger migrationLogger) : ControllerBase
{
    /// <remarks>
    /// Returns the names of the source products this installation can import a portal from. Pass a name as
    /// `migratorName` to `POST api/2.0/migration/init/{migratorName}`, which ignores case. Requires a DocSpace admin.
    /// Other callers get 403. The call is read-only. The names are not localized, for example `GoogleWorkspace` or
    /// `Nextcloud`. The list keeps the order in which the installation registers its migrators. It depends on the
    /// installation, not on the portal. The parse call does not reject a name outside this list. Instead, its job
    /// ends with an error in `GET api/2.0/migration/status`.
    /// </remarks>
    /// <summary>
    /// Get available migrators
    /// </summary>
    /// <path>api/2.0/migration/list</path>
    /// <collection>list</collection>
    [Tags("Migration")]
    [SwaggerResponse(200, "The names of the migrators this installation can import from, in registration order", typeof(string[]))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpGet("list")]
    public async Task<string[]> ListMigrations()
    {
        await DemandPermissionAsync();
        return migrationCore.GetAvailableMigrations();
    }

    /// <remarks>
    /// Starts a job that reads the uploaded backup of the portal with the migrator named in the path. The job lists
    /// the users, the groups, and the archives it cannot open. Users without an email and users that already exist
    /// come in lists of their own. First, call `migrationFileUpload.ashx` with `Init=true`. This opens a new upload
    /// folder and deletes the previous one. Then post each part of the archive to it with `Name` set to the file
    /// name. Take `migratorName` from `GET api/2.0/migration/list`. Requires a DocSpace admin. Other callers get 403.
    /// The call only queues the job and returns an empty body immediately. Repeat `GET api/2.0/migration/status`
    /// until `isCompleted` is true. Then read the result from `parseResult` and any failure from `error`. The job
    /// changes nothing on the portal. To import, edit `parseResult` and send it to `POST api/2.0/migration/migrate`.
    /// A portal runs one job at a time. While a parse or an import runs, the call does nothing and reports nothing.
    /// After the job ends, a repeated call starts a new parse. A failed parse deletes the backup, so upload it again
    /// first. If the backup is larger than the storage quota of the portal, the job ends with an error.
    /// </remarks>
    /// <summary>
    /// Parse migration archive
    /// </summary>
    /// <path>api/2.0/migration/init/{migratorName}</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "The parse job has been queued; the response carries no content and the result is read from `GET api/2.0/migration/status`")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpPost("init/{migratorName}")]
    public async Task UploadAndInitializeMigration(MigratorNameRequestDto inDto)
    {
        await DemandPermissionAsync();

        await migrationCore.StartParseAsync(inDto.MigratorName);
    }

    /// <remarks>
    /// Returns the progress and the result of the parse or import job of the portal. The other operations of this
    /// group only queue work, so their outcome appears here. Requires a DocSpace admin. Other callers get 403. The
    /// call is read-only. This is the call to poll. Repeat it until `isCompleted` is true. A failed job also sets
    /// `isCompleted` to true, so then check `error`. If the portal has no job, the call returns 200 with an empty
    /// result. This happens before the first parse and after `POST api/2.0/migration/finish`. To read each step of
    /// the job, use `GET api/2.0/migration/logs`.
    /// </remarks>
    /// <summary>
    /// Get migration status
    /// </summary>
    /// <path>api/2.0/migration/status</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "The state of the parse or import queued for the portal, or an empty result when the portal has no job", typeof(MigrationStatusDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
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
    /// Stops the parse job of the portal and deletes the uploaded backup. Use it after a wrong archive or a wrong
    /// migrator name. Requires a DocSpace admin. Other callers get 403. The call only queues the request and returns
    /// an empty body. Soon after, `GET api/2.0/migration/status` no longer shows the parse. You cannot restore the
    /// deleted backup. Before a new parse, upload it to `migrationFileUpload.ashx` again. You can repeat the call. With
    /// no parse job, it still returns 200 and deletes the upload folder. The call does not stop an import. It still
    /// deletes the backup that the import reads. To delete a finished import, use `POST api/2.0/migration/clear`.
    /// </remarks>
    /// <summary>
    /// Cancel migration
    /// </summary>
    /// <path>api/2.0/migration/cancel</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "The cancellation has been queued; the parse stops and the uploaded backup is deleted. The response carries no content")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpPost("cancel")]
    public async Task CancelMigration()
    {
        await DemandPermissionAsync();

        await migrationCore.StopAsync();
    }

    /// <remarks>
    /// Deletes the finished import job of the portal and the backup uploaded for it. Call it after
    /// `GET api/2.0/migration/status` reports `isCompleted` for a job whose `parseResult.operation` is `migration`.
    /// Requires a DocSpace admin. Other callers get 403. The users, groups, and files already imported stay on the
    /// portal. The call only queues the request and returns an empty body. You can repeat it. With nothing to clear,
    /// it still returns 200. After it runs, `GET api/2.0/migration/status` returns an empty result. Also,
    /// `GET api/2.0/migration/logs` returns 404, so download the log first. The call does not stop a running parse or
    /// import. It still deletes the backup they read. To stop a parse, use `POST api/2.0/migration/cancel`. You do
    /// not need this call after `POST api/2.0/migration/finish`, which does the same clean-up.
    /// </remarks>
    /// <summary>
    /// Clear migration
    /// </summary>
    /// <path>api/2.0/migration/clear</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "The clean-up has been queued; the finished import is dropped and the uploaded data deleted. The response carries no content")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpPost("clear")]
    public async Task ClearMigration()
    {
        await DemandPermissionAsync();

        await migrationCore.ClearAsync();
    }

    /// <remarks>
    /// Starts the import of the users, groups, and files selected in the body from the parsed backup. First, call
    /// `POST api/2.0/migration/init/{migratorName}` and wait until `isCompleted` is true. Then send `parseResult`
    /// from `GET api/2.0/migration/status` back here. Mark the users and groups to import with `shouldImport`, and
    /// the content with the `import` flags. Requires a DocSpace admin. Other callers get 403. Only the portal owner
    /// can import a user as `DocSpaceAdmin` who is not a DocSpace admin of this portal yet. If another admin tries
    /// this, the call returns 403 and imports nothing. The call only queues the job and returns an empty body
    /// immediately. Repeat `GET api/2.0/migration/status` until `isCompleted` is true. There, `successedUsers`,
    /// `failedUsers`, and `error` show the result. The log in `GET api/2.0/migration/logs` has a line for each step.
    /// The import changes the portal and cannot be undone. Do not repeat the call. While the job runs, a repeated
    /// call does nothing. After the job ends, the portal deletes the backup, so upload and parse it again first. The
    /// call returns 400 for a missing or unreadable body, an unknown `userType`, or an empty or invalid `email`.
    /// When the import ends, close it with `POST api/2.0/migration/finish`. That call can also email the imported
    /// users their activation links.
    /// </remarks>
    /// <summary>
    /// Start migration
    /// </summary>
    /// <path>api/2.0/migration/migrate</path>
    [RaisesWebhook(WebhookTrigger.RoomCreated, When = "for every room the queued import creates")]
    [RaisesWebhook(WebhookTrigger.FolderCreated, When = "for every folder the import creates, including one per imported user for their files")]
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "for every room the import moves to its original owner, a room admin or DocSpace admin")]
    [Tags("Migration")]
    [SwaggerResponse(200, "The import has been queued; the response carries no content and the progress is read from `GET api/2.0/migration/status`")]
    [SwaggerResponse(400, "The request body is missing or could not be read as a parse result, including an unknown `userType` name, or a user in `users`, `withoutEmailUsers` or `existUsers` has an `email` that is empty or not a valid address")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or is not the portal owner and asked to import a user as `DocSpaceAdmin` who is not an administrator of this portal yet")]
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
    /// Returns the log of the parse or import job of the portal as a file. The log has a timestamped line for each
    /// step, including why the job skipped a user or file. Requires a DocSpace admin. Other callers get 403. The
    /// call is read-only. The body is not JSON. It is `text/plain; charset=UTF-8`, sent as the attachment
    /// `migration.log`. Each job has its own log, and the call returns the log of the job that
    /// `GET api/2.0/migration/status` shows. The portal saves the log only when the job ends. So wait until
    /// `isCompleted` is true, since the call fails while the job runs. If the portal has no job, the call
    /// returns 404. This happens after `POST api/2.0/migration/clear` or `POST api/2.0/migration/finish`.
    /// </remarks>
    /// <summary>
    /// Get migration logs
    /// </summary>
    /// <path>api/2.0/migration/logs</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "The log of the current job as a `text/plain` attachment named `migration.log`")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [SwaggerResponse(404, "The portal holds no parse or import whose log could be returned")]
    [HttpGet("logs")]
    public async Task GetMigrationLogs()
    {
        await DemandPermissionAsync();

        var status = await migrationCore.GetStatusAsync();
        if (status == null)
        {
            throw new ItemNotFoundException(MigrationResource.MigrationProgressException);
        }
        migrationLogger.Init(status.LogName);
        await using var stream = await migrationLogger.GetStreamAsync();

        Response.Headers.Append("Content-Disposition", ContentDispositionUtil.GetHeaderValue("migration.log"));
        Response.ContentType = "text/plain; charset=UTF-8";
        Response.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);

        await stream.CopyToAsync(Response.Body);
    }

    /// <remarks>
    /// Closes a finished import and can email the imported users their activation links. Call it after
    /// `GET api/2.0/migration/status` reports `isCompleted` for the import. Requires a DocSpace admin. Other callers
    /// get 403. If `isSendWelcomeEmail` is true, the call emails each imported user who is not active yet. With
    /// `false`, it sends nothing, and you can invite these users later. Then it queues the same clean-up as
    /// `POST api/2.0/migration/clear`. The clean-up deletes the job and the uploaded backup, and the imported data
    /// stays. The call returns an empty body. After it, `GET api/2.0/migration/status` returns an empty result. Also,
    /// `GET api/2.0/migration/logs` returns 404, so download the log first. You can repeat the call with `false`.
    /// With `true`, a repeat can send the emails again or return 500. The call returns 500 if `isSendWelcomeEmail`
    /// is true and the import is missing, still running, or stopped before creating users. It returns 400 if the body
    /// has no `isSendWelcomeEmail`.
    /// </remarks>
    /// <summary>
    /// Finish migration
    /// </summary>
    /// <path>api/2.0/migration/finish</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "The activation emails have been sent if they were asked for and the clean-up has been queued. The response carries no content")]
    [SwaggerResponse(400, "The request body cannot be read or has no `isSendWelcomeEmail`")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [SwaggerResponse(500, "`isSendWelcomeEmail` is true and the portal holds no import that has reached the user step: no job at all, only a parse pass, or an import still running or stopped before that step")]
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
