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

namespace ASC.AI.Integration.Database;

public partial class AiIntegrationContext
{
    [PreCompileQuery]
    public Task<DbAttachment?> GetAttachmentAsync(int tenantId, Guid createdBy, Guid id)
    {
        return AttachmentQueriesContainer.GetAttachmentAsync(this, tenantId, createdBy, id);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbAttachment> GetAttachmentsByIdsAsync(int tenantId, Guid createdBy, IEnumerable<Guid> ids)
    {
        return AttachmentQueriesContainer.GetAttachmentsByIdsAsync(this, tenantId, createdBy, ids);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbAttachment> GetUnboundAttachmentsByEntryIdsAsync(int tenantId, Guid createdBy, IEnumerable<int> entryIds)
    {
        return AttachmentQueriesContainer.GetUnboundAttachmentsByEntryIdsAsync(this, tenantId, createdBy, entryIds);
    }

    [PreCompileQuery]
    public Task<int> SetAttachmentContentAsync(int tenantId, Guid createdBy, Guid id, string content)
    {
        return AttachmentQueriesContainer.SetAttachmentContentAsync(this, tenantId, createdBy, id, content);
    }

    [PreCompileQuery]
    public Task<int> UpdateAttachmentBindingsByIdsAsync(int tenantId, Guid createdBy, IEnumerable<Guid> ids, Guid messageId)
    {
        return AttachmentQueriesContainer.UpdateAttachmentBindingsByIdsAsync(this, tenantId, createdBy, ids, messageId);
    }

    [PreCompileQuery]
    public Task<int> AgeUnboundAttachmentsAsync(int tenantId, Guid createdBy, IEnumerable<Guid> ids)
    {
        return AttachmentQueriesContainer.AgeUnboundAttachmentsAsync(this, tenantId, createdBy, ids);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<OrphanAttachment> GetOrphanAttachmentsAsync(DateTime cutoffDate, DateTime createdAt, Guid id, int take)
    {
        return AttachmentQueriesContainer.GetOrphanAttachmentsAsync(this, cutoffDate, createdAt, id, take);
    }

    [PreCompileQuery]
    public Task<int> DeleteAttachmentsAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return AttachmentQueriesContainer.DeleteAttachmentsAsync(this, tenantId, ids);
    }
}

static file class AttachmentQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, Guid, Guid, Task<DbAttachment?>> GetAttachmentAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, Guid id) =>
                ctx.Attachments.FirstOrDefault(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.Id == id));

    public static readonly Func<AiIntegrationContext, int, Guid, IEnumerable<Guid>, IAsyncEnumerable<DbAttachment>> GetAttachmentsByIdsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, IEnumerable<Guid> ids) =>
                ctx.Attachments
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && ids.Contains(x.Id)));

    public static readonly Func<AiIntegrationContext, int, Guid, IEnumerable<int>, IAsyncEnumerable<DbAttachment>> GetUnboundAttachmentsByEntryIdsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, IEnumerable<int> entryIds) =>
                ctx.Attachments
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.MessageId == null &&
                                x.EntryId != null && entryIds.Contains(x.EntryId.Value)));

    public static readonly Func<AiIntegrationContext, int, Guid, Guid, string, Task<int>> SetAttachmentContentAsync =
        (AiIntegrationContext ctx, int tenantId, Guid createdBy, Guid id, string content) =>
            ctx.Attachments
                .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.Id == id && x.MessageId == null && x.Content == null)
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.Content, content));

    public static readonly Func<AiIntegrationContext, int, Guid, IEnumerable<Guid>, Guid, Task<int>> UpdateAttachmentBindingsByIdsAsync =
        (AiIntegrationContext ctx, int tenantId, Guid createdBy, IEnumerable<Guid> ids, Guid messageId) =>
            ctx.Attachments
                .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && ids.Contains(x.Id))
                .ExecuteUpdateAsync(x => x
                    .SetProperty(y => y.MessageId, messageId));

    public static readonly Func<AiIntegrationContext, int, Guid, IEnumerable<Guid>, Task<int>> AgeUnboundAttachmentsAsync =
        (AiIntegrationContext ctx, int tenantId, Guid createdBy, IEnumerable<Guid> ids) =>
            ctx.Attachments
                .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.MessageId == null && ids.Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(y => y.CreatedAt, AttachmentsStorage.DeletedMark));

    public static readonly Func<AiIntegrationContext, DateTime, DateTime, Guid, int, IAsyncEnumerable<OrphanAttachment>> GetOrphanAttachmentsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, DateTime cutoffDate, DateTime createdAt, Guid id, int take) =>
                ctx.Attachments
                    .Where(x => x.MessageId == null && x.CreatedAt <= cutoffDate
                                && (x.CreatedAt > createdAt
                                    || (x.CreatedAt == createdAt && x.Id.CompareTo(id) > 0)))
                    .OrderBy(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .Take(take)
                    .Select(x => new OrphanAttachment { TenantId = x.TenantId, Id = x.Id, CreatedAt = x.CreatedAt }));

    public static readonly Func<AiIntegrationContext, int, IEnumerable<Guid>, Task<int>> DeleteAttachmentsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.Attachments
                    .Where(x => x.TenantId == tenantId && x.MessageId == null && ids.Contains(x.Id))
                    .ExecuteDelete());
}
