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

        await using var context = await dbContextFactory.CreateDbContextAsync();

        context.PromptFolders.Add(entity);
        await context.SaveChangesAsync();

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

        await using var context = await dbContextFactory.CreateDbContextAsync();

        context.PromptFolders.AddRange(entities);
        await context.SaveChangesAsync();

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
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var affected = await context.UpdatePromptFolderNameAsync(tenantId, createdBy, id, name, DateTime.UtcNow);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int tenantId, Guid createdBy, Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var affected = await context.DeletePromptFolderAsync(tenantId, createdBy, id);
        return affected > 0;
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
