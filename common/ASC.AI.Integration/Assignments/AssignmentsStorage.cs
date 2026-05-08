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

namespace ASC.AI.Integration.Assignments;

[Scope]
public class AssignmentsStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory)
{
    public async Task<bool> CreateAsync(int tenantId, string actionType, Guid profileId, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var existing = entryId.HasValue
            ? await context.GetAssignmentByEntryAsync(tenantId, actionType, entryId.Value)
            : await context.GetAssignmentAsync(tenantId, actionType);

        if (existing != null)
        {
            return false;
        }

        var entity = new DbAssignment
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ActionType = actionType,
            ProfileId = profileId,
            EntryId = entryId,
            CreatedAt = DateTime.UtcNow
        };

        context.Assignments.Add(entity);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<Guid?> ReadByTypeAsync(int tenantId, string actionType, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = entryId.HasValue
            ? await context.GetAssignmentByEntryAsync(tenantId, actionType, entryId.Value)
            : await context.GetAssignmentAsync(tenantId, actionType);

        return entity?.ProfileId;
    }

    public async Task<Dictionary<string, Guid>> ReadAllAsync(int tenantId, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var assignments = entryId.HasValue
            ? context.GetAllAssignmentsByEntryAsync(tenantId, entryId.Value)
            : context.GetAllAssignmentsAsync(tenantId);

        return await assignments.ToDictionaryAsync(x => x.ActionType, x => x.ProfileId);
    }

    public async Task<bool> UpdateAsync(int tenantId, string actionType, Guid profileId, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var affected = entryId.HasValue
            ? await context.UpdateAssignmentProfileByEntryAsync(tenantId, actionType, entryId.Value, profileId)
            : await context.UpdateAssignmentProfileAsync(tenantId, actionType, profileId);

        return affected > 0;
    }

    public async Task UpsertManyAsync(int tenantId, IReadOnlyDictionary<string, Guid> assignments, int? entryId = null)
    {
        if (assignments.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

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
            await transaction.CommitAsync();
        });
    }

    public async Task DeleteAsync(int tenantId, string actionType, int? entryId = null)
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

    public async Task DeleteManyAsync(int tenantId, IReadOnlyCollection<string> actionTypes, int? entryId = null)
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
}
