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
    public async Task CreateAsync(int tenantId, string actionType, int profileId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = new DbAssignment
        {
            TenantId = tenantId,
            ActionType = actionType,
            ProfileId = profileId,
            CreatedAt = DateTime.UtcNow
        };

        context.Assignments.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task<int?> ReadByTypeAsync(int tenantId, string actionType)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = await context.GetAssignmentAsync(tenantId, actionType);
        return entity?.ProfileId;
    }

    public async Task<Dictionary<string, int>> ReadAllAsync(int tenantId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var result = new Dictionary<string, int>();
        await foreach (var entity in context.GetAllAssignmentsAsync(tenantId))
        {
            result[entity.ActionType] = entity.ProfileId;
        }

        return result;
    }

    public async Task<int> UpdateAsync(int tenantId, string actionType, int profileId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.UpdateAssignmentProfileAsync(tenantId, actionType, profileId);
    }

    public async Task UpsertManyAsync(int tenantId, IReadOnlyDictionary<string, int> assignments)
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

            var actionTypes = assignments.Keys.ToArray();
            var existing = new Dictionary<string, DbAssignment>(actionTypes.Length);
            await foreach (var entity in context.GetAssignmentsByTypesAsync(tenantId, actionTypes))
            {
                existing[entity.ActionType] = entity;
            }

            var now = DateTime.UtcNow;
            foreach (var (actionType, profileId) in assignments)
            {
                if (existing.TryGetValue(actionType, out var entity))
                {
                    if (entity.ProfileId != profileId)
                    {
                        entity.ProfileId = profileId;
                    }
                }
                else
                {
                    context.Assignments.Add(new DbAssignment
                    {
                        TenantId = tenantId,
                        ActionType = actionType,
                        ProfileId = profileId,
                        CreatedAt = now
                    });
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task DeleteAsync(int tenantId, string actionType)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.DeleteAssignmentAsync(tenantId, actionType);
    }

    public async Task DeleteManyAsync(int tenantId, IReadOnlyCollection<string> actionTypes)
    {
        if (actionTypes.Count == 0)
        {
            return;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.DeleteAssignmentsByTypesAsync(tenantId, actionTypes);
    }
}
