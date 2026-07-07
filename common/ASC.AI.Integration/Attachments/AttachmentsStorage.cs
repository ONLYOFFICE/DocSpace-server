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

namespace ASC.AI.Integration.Attachments;

[Scope]
public class AttachmentsStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory)
{
    public async Task<List<Attachment>> CreateManyAsync(int tenantId, IReadOnlyList<CreateAttachmentParams> data)
    {
        if (data.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var entities = data.Select(p => new DbAttachment
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Kind = p.Kind,
            Title = p.Title,
            Content = p.Content,
            EntryId = p.EntryId,
            ThirdpartyEntryId = p.ThirdpartyEntryId,
            CreatedAt = now
        }).ToArray();

        await using var context = await dbContextFactory.CreateDbContextAsync();

        context.Attachments.AddRange(entities);
        await context.SaveChangesAsync();

        return entities.Select(ToDomainEntity).ToList();
    }

    public async Task<Attachment?> ReadByIdAsync(int tenantId, Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = await context.GetAttachmentAsync(tenantId, id);
        return entity == null ? null : ToDomainEntity(entity);
    }

    public async Task<List<Attachment>> ReadManyByIdsAsync(int tenantId, HashSet<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.GetAttachmentsByIdsAsync(tenantId, ids)
            .Select(ToDomainEntity)
            .ToListAsync();
    }

    public async Task UpdateManyAsync(int tenantId, HashSet<Guid> ids, Guid messageId)
    {
        if (ids.Count == 0)
        {
            return;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.UpdateAttachmentBindingsByIdsAsync(tenantId, ids, messageId);
    }

    public async Task DeleteAsync(int tenantId, Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.DeleteAttachmentAsync(tenantId, id);
    }

    public async Task DeleteManyAsync(int tenantId, HashSet<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }

        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.DeleteAttachmentsByIdsAsync(tenantId, ids);
    }

    private static Attachment ToDomainEntity(DbAttachment entity)
    {
        return new Attachment
        {
            Id = entity.Id,
            Kind = entity.Kind,
            Title = entity.Title,
            Content = entity.Content,
            MessageId = entity.MessageId,
            EntryId = entity.EntryId,
            ThirdpartyEntryId = entity.ThirdpartyEntryId,
            CreatedAt = entity.CreatedAt
        };
    }
}
