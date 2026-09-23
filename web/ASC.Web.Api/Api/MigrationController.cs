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
    /// Lists the source products this installation can import a portal from, as the migrator names every other
    /// operation in this group expects. Nothing has to be called first, a DocSpace administrator is required as
    /// everywhere here, and the call is read-only and idempotent. The answer is a plain list of names such as
    /// `GoogleWorkspace`, `Nextcloud` or `Workspace`, never localized and ordered as the migrators are registered;
    /// pass one of them as `migratorName` to `POST api/2.0/migration/init/{migratorName}`, where the match ignores
    /// case. The list depends on the installation rather than on the portal, so it does not change while the portal
    /// runs, and a name that is not in it is not rejected by the operation that takes it - the queued job ends with
    /// the failure reported in `error` of `GET api/2.0/migration/status`.
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
    /// Queues a pass that reads the backup already uploaded for this portal with the migrator named in the path and
    /// reports what it holds - the users, the users that carry no email address, the users that exist on this portal
    /// already, the groups and the archives it could not open - so that the caller can choose what to import. Upload
    /// the backup first: `migrationFileUpload.ashx?Init=true` opens a new upload folder and drops the previous one,
    /// then every part of the archive is posted to the same handler with `Name` set to its file name; take
    /// `migratorName` from `GET api/2.0/migration/list`. A DocSpace administrator is required. The call only queues
    /// the job and answers at once with an empty body: poll `GET api/2.0/migration/status` until `isCompleted` is
    /// true, then read what was found from `parseResult` and any failure from `error`. Nothing is imported here and
    /// the portal is not changed - the parse result is the body to edit and send to
    /// `POST api/2.0/migration/migrate`. A portal runs one job at a time, so a call made while another parse or
    /// import is still running is ignored instead of reported, and a backup bigger than the portal's total storage
    /// quota ends the job with an error rather than failing this call.
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
    /// Returns how far the parse or the import queued for this portal has got and, once it stopped, what it produced
    /// - the one place where every other operation in this group reports what it did. Any of them may be polled from
    /// here as soon as it returns; a DocSpace administrator is required and the call is read-only and idempotent.
    /// `progress` is the share of the job that is done, from 0 to 100, and `isCompleted` turns true when the job
    /// stopped whether it succeeded or not, so read `error` as well: it stays empty while nothing went wrong and
    /// otherwise holds the message that ended the job. `parseResult` carries what the migrator has read so far -
    /// after a parse pass the users, groups and unreadable archives to edit and post to
    /// `POST api/2.0/migration/migrate`, and during an import also `successedUsers` and `failedUsers` - and its
    /// `operation` field, `parse` or `migration`, tells the two stages apart. The result is empty with status 200
    /// when the portal has no job at all, because none was ever started or because
    /// `POST api/2.0/migration/clear` or `POST api/2.0/migration/finish` has removed the last one; an empty answer is
    /// therefore not an error. Line-by-line detail behind the numbers is in `GET api/2.0/migration/logs`.
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
    /// Stops the parse pass queued for this portal and deletes the backup uploaded for it - the way back from a wrong
    /// archive or a wrong migrator name. Nothing has to be called first and a DocSpace administrator is required; the
    /// request is only queued, so the parse ends shortly after the call returns and
    /// `GET api/2.0/migration/status` stops reporting it. The call is destructive for the uploaded data: the whole
    /// upload folder is removed and the backup has to be sent to `migrationFileUpload.ashx` again before a new parse.
    /// It is idempotent - cancelling when nothing is running still answers 200 - and it undoes nothing that was
    /// already written to the portal. Only the parse stage is stopped, the job whose `parseResult.operation` is
    /// `parse`: an import started by `POST api/2.0/migration/migrate` keeps running, and a finished import is
    /// discarded with `POST api/2.0/migration/clear` instead.
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
    /// Discards a finished import and deletes the data uploaded for it, freeing the portal for the next one. Call it
    /// once `GET api/2.0/migration/status` reports `isCompleted` for a job whose `parseResult.operation` is
    /// `migration`; a DocSpace administrator is required. Only the queued job and the temporary upload folder go -
    /// the users, groups and files already imported stay in the portal - so the call destroys migration data alone,
    /// and it is idempotent: clearing twice, or with nothing to clear, still answers 200. Like the other write
    /// operations here it is only queued, and once it has run `GET api/2.0/migration/status` returns an empty result
    /// and `GET api/2.0/migration/logs` answers 404, so download the log before calling it. A parse that is still
    /// running is not affected - stop that with `POST api/2.0/migration/cancel` - and
    /// `POST api/2.0/migration/finish` performs the same clean-up itself, which makes this call unnecessary after it.
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
    /// Starts the import itself: the users, the groups and the files selected in the request body are created on this
    /// portal from the backup that the parse pass has read. Run `POST api/2.0/migration/init/{migratorName}` first
    /// and wait for `isCompleted` in `GET api/2.0/migration/status`, then send `parseResult` from that answer back
    /// here with `shouldImport` set on the users and groups to take and the `import...Files` flags set for the
    /// content to copy. A DocSpace administrator is required, and importing a user as `DocSpaceAdmin` additionally
    /// requires the caller to be the portal owner unless a user with that email is an administrator of this portal
    /// already, otherwise the whole call is rejected with 403 before anything is imported. The job is queued and the
    /// call answers with an empty body at once: watch `progress`, `successedUsers`, `failedUsers` and `error` in
    /// `GET api/2.0/migration/status` and read what each step did from `GET api/2.0/migration/logs`. The import
    /// writes to the portal and cannot be undone, and a repeat is no help: a call made while the job runs is ignored,
    /// and once the job has ended the uploaded backup is deleted, so a new call has nothing to read until the archive
    /// is uploaded and parsed again. When the import is done, close it with `POST api/2.0/migration/finish`, which can
    /// also mail the imported users their activation link.
    /// </remarks>
    /// <summary>
    /// Start migration
    /// </summary>
    /// <path>api/2.0/migration/migrate</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "The import has been queued; the response carries no content and the progress is read from `GET api/2.0/migration/status`")]
    [SwaggerResponse(400, "The request body is missing or could not be read as a parse result")]
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
    /// Downloads the log of the parse or import the portal currently holds - the step-by-step record behind the
    /// numbers and the single error message of `GET api/2.0/migration/status`, and the place where the reason for a
    /// skipped user or file is written. The portal has to hold such a job, started by
    /// `POST api/2.0/migration/init/{migratorName}` or `POST api/2.0/migration/migrate` and not yet removed by
    /// `POST api/2.0/migration/clear` or `POST api/2.0/migration/finish`, otherwise the call answers 404; a DocSpace
    /// administrator is required and the call is read-only and idempotent. The body is not JSON: it is
    /// `text/plain; charset=UTF-8` sent as an attachment named `migration.log`, one line per step with the progress
    /// it reported. Each job writes its own log, so this always returns the log of the job that
    /// `GET api/2.0/migration/status` describes, and while that job runs the file keeps growing - a call made early
    /// returns only the part written so far and may be repeated later for the rest.
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
    /// Closes a completed import: it can send every user the import created the activation email they need before
    /// they can sign in, and it then discards the job and the data uploaded for it. Call it once
    /// `GET api/2.0/migration/status` reports `isCompleted` for the import; a DocSpace administrator is required, and
    /// with `isSendWelcomeEmail` set to true the job must still be in the queue, so do not clear it first. That flag
    /// decides what happens to the imported people: true mails the activation link to each of them who has not
    /// activated their account yet and skips the ones that are already active, false ends the import quietly and
    /// leaves inviting them for later. The call writes to the portal and is not idempotent - the emails go out again
    /// on every call - while its second half repeats what `POST api/2.0/migration/clear` does, removing the finished
    /// job and the uploaded backup and leaving everything already imported in place. It answers with an empty body,
    /// after which `GET api/2.0/migration/status` returns an empty result and `GET api/2.0/migration/logs` answers
    /// 404, so download the log first.
    /// </remarks>
    /// <summary>
    /// Finish migration
    /// </summary>
    /// <path>api/2.0/migration/finish</path>
    [Tags("Migration")]
    [SwaggerResponse(200, "The activation emails have been sent if they were asked for and the clean-up has been queued. The response carries no content")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
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
