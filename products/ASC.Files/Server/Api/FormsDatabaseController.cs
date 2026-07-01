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
    /// Returns a read-only PostgreSQL connection string scoped to the current portal's form data schema.
    /// Only portal administrators can access this endpoint.
    /// </remarks>
    /// <summary>Get built-in database connection string</summary>
    /// <path>api/2.0/files/builtin-db/connection-string</path>
    [Tags("Files / Built-in Database")]
    [SwaggerResponse(200, "Read-only PostgreSQL connection details for the portal's form data", typeof(BuiltinDbConnectionDto))]
    [SwaggerResponse(402, "Built-in database feature is not configured")]
    [HttpGet("builtin-db/connection-string")]
    public async Task<BuiltinDbConnectionDto> GetConnectionStringAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!provisioner.IsEnabled())
        {
            throw new InvalidOperationException("Built-in forms database is not configured.");
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var credentials = await provisioner.GetOrProvisionAsync(tenantId);

        return new BuiltinDbConnectionDto
        {
            Host = credentials.Host,
            Port = credentials.Port,
            Database = credentials.Database,
            User = credentials.RoUser,
            Password = credentials.RoPassword,
            ConnectionString = credentials.RoConnectionString
        };
    }
}
