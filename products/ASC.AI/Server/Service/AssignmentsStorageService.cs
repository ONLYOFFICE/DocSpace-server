// (c) Copyright Ascensio System SIA 2009-2026
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
        await AssertUserHasAccessAsync(_writeTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        if (!await storage.CreateAsync(tenantManager.GetCurrentTenantId(), actionType, profileId, entryId))
        {
            throw new InvalidOperationException($"Assignment for action type '{actionType.ToStringFast()}' already exists");
        }
    }

    public async Task<Guid?> ReadByTypeAsync(ActionType actionType, string? entityId = null)
    {
        await AssertUserHasAccessAsync(_readTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        return await storage.ReadByTypeAsync(tenantManager.GetCurrentTenantId(), actionType, entryId);
    }

    public async Task<Dictionary<ActionType, Guid>> ReadAllAsync(string? entityId = null)
    {
        await AssertUserHasAccessAsync(_readTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        return await storage.ReadAllAsync(tenantManager.GetCurrentTenantId(), entryId);
    }

    public async Task UpdateAsync(ActionType actionType, Guid profileId, string? entityId = null)
    {
        await AssertUserHasAccessAsync(_writeTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

        if (!await storage.UpdateAsync(tenantManager.GetCurrentTenantId(), actionType, profileId, entryId))
        {
            throw new ItemNotFoundException($"Assignment for action type '{actionType.ToStringFast()}' was not found");
        }
    }

    public async Task UpsertManyAsync(IReadOnlyDictionary<ActionType, Guid> assignments, string? entityId = null)
    {
        await AssertUserHasAccessAsync(_writeTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

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
