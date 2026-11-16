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
    /// database, used while the portal has no external database configured. Only a portal administrator who can change
    /// portal settings may call it, and a token scoped to reading files is not enough: it needs `files:write`. When the
    /// server has no built-in forms database the call is refused with 403. The
    /// read-only account works in a database of the portal's own, so it sees nothing of other portals: the first call
    /// starts moving the portal's tables there and answers 202 without a `response` field; repeat the call until it
    /// answers 200. Form filling goes on during the move, and a row that could not be written meanwhile is added to the
    /// new database from the search index. A failed move answers 503 with a `Retry-After` header until a new attempt may start. Repeated calls
    /// return the same database and credentials; new passwords are set only when the stored ones no longer sign in, as
    /// after a restored backup. The account only reads: one row per completed form, in tables named
    /// `form_{formId}_v{version}` after the first version they hold. Tables appear as forms are used; one unused for long
    /// may be dropped and is restored on its next use. An external database is configured in the portal settings instead.
    /// </remarks>
    /// <summary>Get built-in forms database connection</summary>
    /// <path>api/2.0/files/builtinDb/connectionString</path>
    [Tags("Files / Built-in Database")]
    [SwaggerResponse(200, "Read-only connection details for the portal's tables in the built-in forms database", typeof(BuiltinDbConnectionDto))]
    [SwaggerResponse(202, "The portal's own database is being prepared; repeat the call later")]
    [SwaggerResponse(403, "The caller cannot change portal settings, the token has no `files:write` scope, or the server has no built-in forms database configured")]
    [SwaggerResponse(503, "Moving the portal into its own database failed; repeat the call after the `Retry-After` seconds")]
    [HttpGet("builtinDb/connectionString")]
    public async Task<ActionResult<BuiltinDbConnectionDto>> GetBuiltinDbConnectionAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!provisioner.IsEnabled())
        {
            throw new InvalidOperationException("Built-in forms database is not configured.");
        }

        // A standalone server may hold several portals too, so every portal reads from a database of its own.
        var isolation = await provisioner.GetIsolatedAsync(tenantManager.GetCurrentTenantId());

        if (isolation.RetryAfter is { } retryAfter)
        {
            Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (isolation.Credentials == null)
        {
            return Accepted();
        }

        messageService.Send(MessageAction.BuiltinFormsDbAccessIssued);

        return isolation.Credentials.Map();
    }
}
