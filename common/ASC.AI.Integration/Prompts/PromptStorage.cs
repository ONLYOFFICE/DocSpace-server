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

namespace ASC.AI.Integration.Prompts;

[Scope]
public class PromptStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory)
{
    public async Task<Prompt> CreateAsync(int tenantId, Guid createdBy, string name, string text, Guid? folderId)
    {
        var now = DateTime.UtcNow;
        var entity = new DbPrompt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CreatedBy = createdBy,
            Name = name,
            Text = text,
            FolderId = folderId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            context.Prompts.Add(entity);
            await context.SaveChangesAsync();
        });

        return ToDomainEntity(entity);
    }

    public async Task<IEnumerable<Prompt>> CreateManyAsync(int tenantId, Guid createdBy, IReadOnlyList<PromptCreateData> prompts)
    {
        if (prompts.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var entities = prompts.Select(p => new DbPrompt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CreatedBy = createdBy,
            Name = p.Name,
            Text = p.Text,
            FolderId = p.FolderId,
            CreatedAt = now,
            UpdatedAt = now
        }).ToArray();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            context.Prompts.AddRange(entities);
            await context.SaveChangesAsync();
        });

        return entities.Select(ToDomainEntity);
    }

    public async Task<Prompt?> ReadByIdAsync(int tenantId, Guid createdBy, Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = await context.GetPromptAsync(tenantId, createdBy, id);
        return entity == null ? null : ToDomainEntity(entity);
    }

    public async Task<IEnumerable<Prompt>> ReadAllAsync(int tenantId, Guid createdBy)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.GetAllPromptsAsync(tenantId, createdBy)
            .Select(ToDomainEntity)
            .ToListAsync();
    }

    public async Task<IEnumerable<Prompt>> ReadByFolderIdAsync(int tenantId, Guid createdBy, Guid? folderId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.GetPromptsByFolderAsync(tenantId, createdBy, folderId)
            .Select(ToDomainEntity)
            .ToListAsync();
    }

    public async Task<bool> UpdateAsync(int tenantId, Guid createdBy, Guid id, string name, string text, Guid? folderId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var affected = await context.UpdatePromptAsync(tenantId, createdBy, id, name, text, folderId, DateTime.UtcNow);
            return affected > 0;
        });
    }

    public async Task<bool> DeleteAsync(int tenantId, Guid createdBy, Guid id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var affected = await context.DeletePromptAsync(tenantId, createdBy, id);
            return affected > 0;
        });
    }

    private static Prompt ToDomainEntity(DbPrompt entity)
    {
        return new Prompt
        {
            Id = entity.Id,
            CreatedBy = entity.CreatedBy,
            Name = entity.Name,
            Text = entity.Text,
            FolderId = entity.FolderId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
