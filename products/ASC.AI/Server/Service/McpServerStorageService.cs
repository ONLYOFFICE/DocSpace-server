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

using ASC.AI.Integration.McpServers;

using McpServer = ASC.AI.Integration.McpServers.McpServer;

namespace ASC.AI.Service;

[Scope]
public class McpServerStorageService(
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    McpServersStorage storage,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity)
{
    private static readonly EmployeeType[] _writeTypes = [EmployeeType.DocSpaceAdmin];
    private static readonly EmployeeType[] _readTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin];

    public async Task CreateAsync(string name, string config, string? entityId = null)
    {
        await AssertUserHasAccessAsync(_writeTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        if (!await storage.CreateAsync(tenantManager.GetCurrentTenantId(), name, config, entryId))
        {
            throw new InvalidOperationException($"MCP server with name '{name}' already exists");
        }
    }

    public async Task<McpServer> ReadByNameAsync(string name, string? entityId = null)
    {
        await AssertUserHasAccessAsync(_readTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        return await storage.ReadByNameAsync(tenantManager.GetCurrentTenantId(), name, entryId)
            ?? throw new ItemNotFoundException($"MCP server with name '{name}' was not found");
    }

    public async Task<IReadOnlyList<McpServer>> ReadAllAsync(string? entityId = null)
    {
        await AssertUserHasAccessAsync(_readTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        return await storage.ReadAllAsync(tenantManager.GetCurrentTenantId(), entryId);
    }

    public async Task UpdateAsync(string name, string config, string? entityId = null)
    {
        await AssertUserHasAccessAsync(_writeTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        if (!await storage.UpdateAsync(tenantManager.GetCurrentTenantId(), name, config, entryId))
        {
            throw new ItemNotFoundException($"MCP server with name '{name}' was not found");
        }
    }

    public async Task ReplaceAllAsync(IReadOnlyDictionary<string, string> servers, string? entityId = null)
    {
        await AssertUserHasAccessAsync(_writeTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        await storage.ReplaceAllAsync(tenantManager.GetCurrentTenantId(), servers, entryId);
    }

    public async Task DeleteAsync(string name, string? entityId = null)
    {
        await AssertUserHasAccessAsync(_writeTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        await storage.DeleteAsync(tenantManager.GetCurrentTenantId(), name, entryId);
    }
}
