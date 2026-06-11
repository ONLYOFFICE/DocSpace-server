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

namespace ASC.AI.Service;

[Scope]
public class AssignmentsStorageService(
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    AssignmentsStorage storage,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity)
{
    private static readonly EmployeeType[] _writeTypes = [EmployeeType.DocSpaceAdmin];
    private static readonly EmployeeType[] _readTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin];

    public async Task CreateAsync(ActionType actionType, Guid profileId, string? entityId = null)
    {
        var entryId = await AssertUserHasAccessAsync(_writeTypes, entityId);

        if (!await storage.CreateAsync(tenantManager.GetCurrentTenantId(), actionType, profileId, entryId))
        {
            throw new InvalidOperationException($"Assignment for action type '{actionType.ToStringFast()}' already exists");
        }
    }

    public async Task<Guid?> ReadByTypeAsync(ActionType actionType, string? entityId = null)
    {
        var entryId = await AssertUserHasAccessAsync(_readTypes, entityId);

        return await storage.ReadByTypeAsync(tenantManager.GetCurrentTenantId(), actionType, entryId);
    }

    public async Task<Dictionary<ActionType, Guid>> ReadAllAsync(string? entityId = null)
    {
        var entryId = await AssertUserHasAccessAsync(_readTypes, entityId);

        return await storage.ReadAllAsync(tenantManager.GetCurrentTenantId(), entryId);
    }

    public async Task UpdateAsync(ActionType actionType, Guid profileId, string? entityId = null)
    {
        var entryId = await AssertUserHasAccessAsync(_writeTypes, entityId);

        if (!await storage.UpdateAsync(tenantManager.GetCurrentTenantId(), actionType, profileId, entryId))
        {
            throw new ItemNotFoundException($"Assignment for action type '{actionType.ToStringFast()}' was not found");
        }
    }

    public async Task UpsertManyAsync(IReadOnlyDictionary<ActionType, Guid> assignments, string? entityId = null)
    {
        var entryId = await AssertUserHasAccessAsync(_writeTypes, entityId);

        await storage.UpsertManyAsync(tenantManager.GetCurrentTenantId(), assignments, entryId);
    }

    public async Task DeleteAsync(ActionType actionType)
    {
        await AssertUserHasAccessAsync(_writeTypes);

        await storage.DeleteAsync(tenantManager.GetCurrentTenantId(), actionType);
    }

    public async Task DeleteManyAsync(IReadOnlyCollection<ActionType> actionTypes)
    {
        await AssertUserHasAccessAsync(_writeTypes);

        await storage.DeleteManyAsync(tenantManager.GetCurrentTenantId(), actionTypes);
    }
}
