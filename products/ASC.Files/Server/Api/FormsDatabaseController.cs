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
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Returns read-only PostgreSQL connection details for the current portal's schema in the built-in forms
    /// database, which stores the submissions of every form filling room while the portal has no external database
    /// configured, whatever the room's send-to-database setting. The server must have the built-in forms database configured,
    /// otherwise the call is refused with 403. Only a portal administrator who can change portal settings may call
    /// it. The first call for a portal creates its schema and database accounts; every later call returns the same
    /// credentials, so the call is safe to repeat. The answer carries the host, port, database name, the read-only
    /// user and its password, and a ready connection string. That account can only read the tables of this
    /// portal's schema: one table per form version, named `form_{formId}_v{version}`, with one row per completed
    /// form. The operation does not configure an external database; that is done in the portal settings.
    /// </remarks>
    /// <summary>Get built-in forms database connection</summary>
    /// <path>api/2.0/files/builtindb/connectionstring</path>
    [Tags("Files / Built-in Database")]
    [SwaggerResponse(200, "Read-only connection details for the portal's schema in the built-in forms database", typeof(BuiltinDbConnectionDto))]
    [SwaggerResponse(403, "The caller cannot change portal settings, or the server has no built-in forms database configured")]
    [HttpGet("builtinDb/connectionString")]
    public async Task<BuiltinDbConnectionDto> GetBuiltinDbConnectionAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!provisioner.IsEnabled())
        {
            throw new InvalidOperationException("Built-in forms database is not configured.");
        }

        var credentials = await provisioner.GetOrProvisionAsync(tenantManager.GetCurrentTenantId());

        return credentials.Map();
    }
}
