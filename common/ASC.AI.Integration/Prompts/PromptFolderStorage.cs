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
public class PromptFolderStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory)
{
    public async Task<PromptFolder> CreateAsync(int tenantId, Guid createdBy, string name)
    {
        var now = DateTime.UtcNow;
        var entity = new DbPromptFolder
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CreatedBy = createdBy,
            Name = name,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            context.PromptFolders.Add(entity);
            await context.SaveChangesAsync();
        });

        return ToDomainEntity(entity);
    }

    public async Task<IReadOnlyList<PromptFolder>> CreateManyAsync(int tenantId, Guid createdBy, IReadOnlyList<string> names)
    {
        if (names.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var entities = names.Select(name => new DbPromptFolder
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            CreatedBy = createdBy,
            Name = name,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            context.PromptFolders.AddRange(entities);
            await context.SaveChangesAsync();
        });

        return entities.Select(ToDomainEntity).ToList();
    }

    public async Task<PromptFolder?> ReadByIdAsync(int tenantId, Guid createdBy, Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = await context.GetPromptFolderAsync(tenantId, createdBy, id);
        return entity == null ? null : ToDomainEntity(entity);
    }

    public async Task<List<PromptFolder>> ReadAllAsync(int tenantId, Guid createdBy)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.GetAllPromptFoldersAsync(tenantId, createdBy)
            .Select(ToDomainEntity)
            .ToListAsync();
    }

    public async Task<bool> UpdateNameAsync(int tenantId, Guid createdBy, Guid id, string name)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var affected = await context.UpdatePromptFolderNameAsync(tenantId, createdBy, id, name, DateTime.UtcNow);
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
            var affected = await context.DeletePromptFolderAsync(tenantId, createdBy, id);
            return affected > 0;
        });
    }

    private static PromptFolder ToDomainEntity(DbPromptFolder entity)
    {
        return new PromptFolder
        {
            Id = entity.Id,
            CreatedBy = entity.CreatedBy,
            Name = entity.Name,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
