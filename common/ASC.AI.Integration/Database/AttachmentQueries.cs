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
    public Task<DbAttachment?> GetAttachmentAsync(int tenantId, Guid id)
    {
        return AttachmentQueriesContainer.GetAttachmentAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbAttachment> GetAttachmentsByIdsAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return AttachmentQueriesContainer.GetAttachmentsByIdsAsync(this, tenantId, ids);
    }

    [PreCompileQuery]
    public Task<int> UpdateAttachmentBindingsByIdsAsync(int tenantId, IEnumerable<Guid> ids, Guid messageId)
    {
        return AttachmentQueriesContainer.UpdateAttachmentBindingsByIdsAsync(this, tenantId, ids, messageId);
    }

    [PreCompileQuery]
    public Task<int> DeleteAttachmentAsync(int tenantId, Guid id)
    {
        return AttachmentQueriesContainer.DeleteAttachmentAsync(this, tenantId, id);
    }

    [PreCompileQuery]
    public Task<int> DeleteAttachmentsByIdsAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return AttachmentQueriesContainer.DeleteAttachmentsByIdsAsync(this, tenantId, ids);
    }
}

static file class AttachmentQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, Guid, Task<DbAttachment?>> GetAttachmentAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id) =>
                ctx.Attachments.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));

    public static readonly Func<AiIntegrationContext, int, IEnumerable<Guid>, IAsyncEnumerable<DbAttachment>> GetAttachmentsByIdsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.Attachments
                    .Where(x => x.TenantId == tenantId && ids.Contains(x.Id)));

    public static readonly Func<AiIntegrationContext, int, IEnumerable<Guid>, Guid, Task<int>> UpdateAttachmentBindingsByIdsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<Guid> ids, Guid messageId) =>
                ctx.Attachments
                    .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
                    .ExecuteUpdate(x => x
                        .SetProperty(y => y.MessageId, messageId)));

    public static readonly Func<AiIntegrationContext, int, Guid, Task<int>> DeleteAttachmentAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid id) =>
                ctx.Attachments
                    .Where(x => x.TenantId == tenantId && x.Id == id)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, IEnumerable<Guid>, Task<int>> DeleteAttachmentsByIdsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.Attachments
                    .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
                    .ExecuteDelete());
}
