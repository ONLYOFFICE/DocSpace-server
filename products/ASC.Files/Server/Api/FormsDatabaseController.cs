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

namespace ASC.Files.Api;

public class FormsDatabaseController(
    FormsDbProvisioningService provisioner,
    TenantManager tenantManager,
    PermissionContext permissionContext,
    MessageService messageService,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Returns read-only PostgreSQL connection details for the current portal's form submissions in the built-in forms
    /// database. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. An API key or OAuth token also needs the `files:write` scope. The account reads the
    /// submissions of every room of the portal, including rooms the caller cannot open. The details lead to a database
    /// that holds the tables of this portal only. The first call starts moving the tables there and returns 202 without
    /// a `response` field. Repeat the call until it returns 200. Forms can be filled during the move. A submission made
    /// during the move may appear in the new database later. If the move fails, the call returns 503 with a
    /// `Retry-After` header in seconds. After three failed moves in a row, the next move starts a day later. A repeated
    /// call returns the same database and credentials. The password changes whenever the account has to be set up
    /// again, for example after a restored backup. If the password stops working, or to end the open sessions of the
    /// account, use `PUT api/2.0/files/builtinDb/password`. It sets a new password. The account cannot change data.
    /// Each table holds one row per completed form. A table is named `form_{formId}_v{version}` after the first form
    /// version it holds. A table appears when its form is first used. A table unused for the idle period of the
    /// server may be deleted. The period is 30 days unless the administrator of the installation changed it. Its form
    /// creates the table again on next use. If
    /// the server has no built-in forms database, the call returns 403. To send the submissions to a database of your
    /// own, use `POST api/2.0/settings/authservice`.
    /// </remarks>
    /// <summary>Get built-in forms database connection</summary>
    /// <path>api/2.0/files/builtinDb/connectionString</path>
    [Tags("Files / Built-in database")]
    [SwaggerResponse(200, "Read-only connection details for the portal's tables in the built-in forms database", typeof(BuiltinDbConnectionDto))]
    [SwaggerResponse(202, "The portal's own database is being prepared; repeat the call later")]
    [SwaggerResponse(403, "The caller cannot change portal settings, the token has no `files:write` scope, or the server has no built-in forms database configured")]
    [SwaggerResponse(503, "Moving the portal into its own database failed; repeat the call after the `Retry-After` seconds")]
    [HttpGet("builtinDb/connectionString")]
    public async Task<ActionResult<BuiltinDbConnectionDto>> GetBuiltinDbConnectionAsync()
    {
        await DemandAccessAsync();

        // A standalone server may hold several portals too, so every portal reads from a database of its own.
        var isolation = await provisioner.GetIsolatedAsync(tenantManager.GetCurrentTenantId());
        if (isolation.Credentials == null)
        {
            return NotReady(isolation);
        }

        messageService.Send(MessageAction.BuiltinFormsDbAccessIssued);

        return isolation.Credentials.Map();
    }

    /// <remarks>
    /// Sets a new password for the read-only PostgreSQL account of the current portal in the built-in forms database.
    /// The call also ends the open sessions of the account. So an app signed in with the old password loses its
    /// connection. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other
    /// callers get 403. An API key or OAuth token also needs the `files:write` scope. The call changes data. You can
    /// repeat it, and each call sets another password. If the sessions cannot be ended, the call returns 500 although the
    /// new password is set. Then repeat the call. It returns the connection details of
    /// `GET api/2.0/files/builtinDb/connectionString` with the new password. If the portal is not yet in its own
    /// database, the call starts the move and returns 202. Then no password changes, and the body has no `response`
    /// field.
    /// Repeat the call until it returns 200. If the move fails, the call returns 503 with a `Retry-After` header in
    /// seconds. If the server has no built-in forms database, the call returns 403.
    /// </remarks>
    /// <summary>Reset built-in forms database password</summary>
    /// <path>api/2.0/files/builtinDb/password</path>
    [Tags("Files / Built-in database")]
    [SwaggerResponse(200, "Read-only connection details for the portal's tables with the new password", typeof(BuiltinDbConnectionDto))]
    [SwaggerResponse(202, "The portal's own database is being prepared and no password was changed; repeat the call later")]
    [SwaggerResponse(403, "The caller cannot change portal settings, the token has no `files:write` scope, or the server has no built-in forms database configured")]
    [SwaggerResponse(500, "The new password is set, but the open sessions of the account could not be ended; repeat the call")]
    [SwaggerResponse(503, "Moving the portal into its own database failed; repeat the call after the `Retry-After` seconds")]
    [HttpPut("builtinDb/password")]
    public async Task<ActionResult<BuiltinDbConnectionDto>> ResetBuiltinDbPasswordAsync()
    {
        await DemandAccessAsync();

        var isolation = await provisioner.ResetReadOnlyPasswordAsync(tenantManager.GetCurrentTenantId());
        if (isolation.Credentials == null)
        {
            return NotReady(isolation);
        }

        messageService.Send(MessageAction.BuiltinFormsDbPasswordReset);

        return isolation.Credentials.Map();
    }

    private async Task DemandAccessAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!provisioner.IsEnabled())
        {
            throw new InvalidOperationException("Built-in forms database is not configured.");
        }
    }

    // The portal's own database is not ready: 503 with the wait after a failed move, otherwise 202.
    private ActionResult NotReady(FormsDbIsolation isolation)
    {
        if (isolation.RetryAfter is { } retryAfter)
        {
            Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        return Accepted();
    }
}
