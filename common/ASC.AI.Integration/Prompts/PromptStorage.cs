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

        await using var context = await dbContextFactory.CreateDbContextAsync();

        context.Prompts.Add(entity);
        await context.SaveChangesAsync();

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

        await using var context = await dbContextFactory.CreateDbContextAsync();

        context.Prompts.AddRange(entities);
        await context.SaveChangesAsync();

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
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var affected = await context.UpdatePromptAsync(tenantId, createdBy, id, name, text, folderId, DateTime.UtcNow);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int tenantId, Guid createdBy, Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var affected = await context.DeletePromptAsync(tenantId, createdBy, id);
        return affected > 0;
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
