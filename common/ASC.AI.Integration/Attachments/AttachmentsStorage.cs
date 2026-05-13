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
