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

namespace ASC.AI.Integration.Assignments;

[Scope]
public class AssignmentsStorage(
    IDbContextFactory<AiIntegrationContext> dbContextFactory,
    IDistributedLockProvider distributedLockProvider)
{
    public async Task<bool> CreateAsync(int tenantId, ActionType actionType, Guid profileId, int? entryId = null)
    {
        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId, entryId)))
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var strategy = dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var context = await dbContextFactory.CreateDbContextAsync();

                var existing = entryId.HasValue
                    ? await context.GetAssignmentByEntryAsync(tenantId, actionType, entryId.Value)
                    : await context.GetAssignmentAsync(tenantId, actionType);

                if (existing != null)
                {
                    return false;
                }

                context.Assignments.Add(new DbAssignment
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenantId,
                    ActionType = actionType,
                    ProfileId = profileId,
                    EntryId = entryId,
                    CreatedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();

                return true;
            });
        }
    }

    public async Task<Guid?> ReadByTypeAsync(int tenantId, ActionType actionType, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = entryId.HasValue
            ? await context.GetAssignmentByEntryAsync(tenantId, actionType, entryId.Value)
            : await context.GetAssignmentAsync(tenantId, actionType);

        return entity?.ProfileId;
    }

    public async Task<Dictionary<ActionType, Guid>> ReadAllAsync(int tenantId, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var assignments = entryId.HasValue
            ? context.GetAllAssignmentsByEntryAsync(tenantId, entryId.Value)
            : context.GetAllAssignmentsAsync(tenantId);

        return await assignments.ToDictionaryAsync(x => x.ActionType, x => x.ProfileId);
    }

    public async Task<bool> UpdateAsync(int tenantId, ActionType actionType, Guid profileId, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var affected = entryId.HasValue
            ? await context.UpdateAssignmentProfileByEntryAsync(tenantId, actionType, entryId.Value, profileId)
            : await context.UpdateAssignmentProfileAsync(tenantId, actionType, profileId);

        return affected > 0;
    }

    public async Task UpsertManyAsync(int tenantId, IReadOnlyDictionary<ActionType, Guid> assignments, int? entryId = null)
    {
        if (assignments.Count == 0)
        {
            return;
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId, entryId)))
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var strategy = dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var context = await dbContextFactory.CreateDbContextAsync();

                var existingByType = await (entryId.HasValue
                        ? context.GetAssignmentsByTypesAndEntryAsync(tenantId, entryId.Value, assignments.Keys)
                        : context.GetAssignmentsByTypesAsync(tenantId, assignments.Keys))
                    .ToDictionaryAsync(x => x.ActionType, x => x.Id);

                var now = DateTime.UtcNow;
                foreach (var (actionType, profileId) in assignments)
                {
                    if (existingByType.TryGetValue(actionType, out var existingId))
                    {
                        var entity = new DbAssignment
                        {
                            Id = existingId,
                            TenantId = tenantId,
                            ActionType = actionType,
                            EntryId = entryId,
                            ProfileId = profileId,
                            CreatedAt = default
                        };
                        context.Assignments.Attach(entity);
                        context.Entry(entity).Property(x => x.ProfileId).IsModified = true;
                    }
                    else
                    {
                        context.Assignments.Add(new DbAssignment
                        {
                            Id = Guid.CreateVersion7(),
                            TenantId = tenantId,
                            ActionType = actionType,
                            ProfileId = profileId,
                            EntryId = entryId,
                            CreatedAt = now
                        });
                    }
                }

                await context.SaveChangesAsync();
            });
        }
    }

    public async Task DeleteAsync(int tenantId, ActionType actionType, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        if (entryId.HasValue)
        {
            await context.DeleteAssignmentByEntryAsync(tenantId, actionType, entryId.Value);
        }
        else
        {
            await context.DeleteAssignmentAsync(tenantId, actionType);
        }
    }

    public async Task DeleteManyAsync(int tenantId, IReadOnlyCollection<ActionType> actionTypes, int? entryId = null)
    {
        if (actionTypes.Count == 0)
        {
            return;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync();

        if (entryId.HasValue)
        {
            await context.DeleteAssignmentsByTypesAndEntryAsync(tenantId, entryId.Value, actionTypes);
        }
        else
        {
            await context.DeleteAssignmentsByTypesAsync(tenantId, actionTypes);
        }
    }

    private static string GetLockKey(int tenantId, int? entryId)
    {
        return entryId.HasValue
            ? $"ai_integration_assignments_{tenantId}_{entryId.Value}"
            : $"ai_integration_assignments_{tenantId}";
    }
}
