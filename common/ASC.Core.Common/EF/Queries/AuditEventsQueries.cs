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

namespace ASC.MessagingSystem.EF.Context;

public partial class MessagesContext
{
    [PreCompileQuery]
    public IAsyncEnumerable<int> AuditEventTenantsOlderThanAsync(DateTime threshold)
    {
        return AuditEventsQueries.AuditEventTenantsOlderThanAsync(this, threshold);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<AuditEventKey> AuditEventsToClearAsync(int tenantId, DateTime threshold, DateTime? afterDate, int afterId, int count)
    {
        return AuditEventsQueries.AuditEventsToClearAsync(this, tenantId, threshold, afterDate, afterId, count);
    }

    [PreCompileQuery]
    public Task<int> DeleteAuditEventsToClearAsync(int tenantId, IEnumerable<int> ids)
    {
        return AuditEventsQueries.DeleteAuditEventsToClearAsync(this, tenantId, ids);
    }
}

public record AuditEventKey(int Id, DateTime Date);

static file class AuditEventsQueries
{
    public static readonly Func<MessagesContext, DateTime, IAsyncEnumerable<int>> AuditEventTenantsOlderThanAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (MessagesContext ctx, DateTime threshold) =>
                ctx.AuditEvents
                    .GroupBy(r => r.TenantId)
                    .Where(r => r.Min(a => a.Date) < threshold)
                    .Select(r => r.Key));

    // The events older than the threshold that are not part of the history of a file, folder or room: an event is
    // kept while files_audit_reference points at it, and the history is cleared together with its entry. The
    // events are read in (date, id) order resuming after the last one of the previous batch, so the history events
    // that stay behind are passed over once per run instead of once per batch.
    public static readonly Func<MessagesContext, int, DateTime, DateTime?, int, int, IAsyncEnumerable<AuditEventKey>> AuditEventsToClearAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, DateTime threshold, DateTime? afterDate, int afterId, int count) =>
                ctx.AuditEvents
                    .Where(r => r.TenantId == tenantId && r.Date < threshold)
                    .Where(r => afterDate == null || r.Date > afterDate || (r.Date == afterDate && r.Id > afterId))
                    .Where(r => !ctx.FilesAuditReferences.Any(a => a.AuditEventId == r.Id))
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.Id)
                    .Take(count)
                    .Select(r => new AuditEventKey(r.Id, r.Date)));

    // The reference check is repeated, because files_audit_reference cascades from audit_events: deleting an event
    // that became part of a history after it was read would silently remove it from that history.
    public static readonly Func<MessagesContext, int, IEnumerable<int>, Task<int>> DeleteAuditEventsToClearAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, IEnumerable<int> ids) =>
                ctx.AuditEvents
                    .Where(r => r.TenantId == tenantId && ids.Contains(r.Id))
                    .Where(r => !ctx.FilesAuditReferences.Any(a => a.AuditEventId == r.Id))
                    .ExecuteDelete());
}
