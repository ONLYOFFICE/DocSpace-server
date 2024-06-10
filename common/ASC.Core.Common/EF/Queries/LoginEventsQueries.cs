// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.MessagingSystem.EF.Context;

public partial class MessagesContext
{
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, new int[0], PreCompileQuery.DefaultDateTime])]
    public IAsyncEnumerable<DbLoginEvent> LoginEventsAsync(int tenantId, Guid userId, IEnumerable<int> loginActions, DateTime date)
    {
        return Queries.LoginEventsAsync(this, tenantId, userId, loginActions, date);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> DeleteLoginEventsAsync(int tenantId, int loginEventId)
    {
        return Queries.DeleteLoginEventsAsync(this, tenantId, loginEventId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<DbLoginEvent> LoginEventsByUserIdAsync(int tenantId, Guid userId)
    {
        return Queries.LoginEventsByUserIdAsync(this, tenantId, userId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbLoginEvent> LoginEventsByTenantIdAsync(int tenantId)
    {
        return Queries.LoginEventsByTenantIdAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbLoginEvent> LoginEventsByIdAsync(int tenantId, int id)
    {
        return Queries.LoginEventsByIdAsync(this, tenantId, id);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbLoginEvent> LoginEventsExceptThisAsync(int tenantId, Guid userId, int loginEventId)
    {
        return Queries.LoginEventsExceptThisAsync(this, tenantId, userId, loginEventId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, (byte)0, 0, 0])]
    public IAsyncEnumerable<DbAuditEvent> GetAuditEventsByReferences(int tenantId, int entryId, byte entryType, int offset, int count)
    {
        return Queries.GetAuditEventsByReferences(this, tenantId, entryId, entryType, offset, count);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, (byte)0])]
    public Task<int> GetAuditEventsByReferencesTotalCount(int tenantId, int entryId, byte entryType)
    {
        return Queries.GetAuditEventsByReferencesTotalCount(this, tenantId, entryId, entryType);
    }
}

static file class Queries
{
    public static readonly Func<MessagesContext, int, Guid, IEnumerable<int>, DateTime, IAsyncEnumerable<DbLoginEvent>> LoginEventsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, Guid userId, IEnumerable<int> loginActions, DateTime date) =>
                ctx.LoginEvents
                    .Where(r => r.TenantId == tenantId
                                && r.UserId == userId
                                && loginActions.Contains(r.Action ?? 0)
                                && r.Date >= date
                                && r.Active)
                    .OrderByDescending(r => r.Id)
                    .AsQueryable());

    public static readonly Func<MessagesContext, int, int, Task<int>> DeleteLoginEventsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, int loginEventId) =>
                ctx.LoginEvents
                    .Where(r => r.TenantId == tenantId && r.Id == loginEventId)
                    .ExecuteUpdate(r => r.SetProperty(p => p.Active, false)));

    public static readonly Func<MessagesContext, int, Guid, IAsyncEnumerable<DbLoginEvent>> LoginEventsByUserIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, Guid userId) =>
                ctx.LoginEvents
                    .Where(r => r.TenantId == tenantId
                                && r.UserId == userId
                                && r.Active));

    public static readonly Func<MessagesContext, int, IAsyncEnumerable<DbLoginEvent>> LoginEventsByTenantIdAsync =
        Microsoft.EntityFrameworkCore. EF.CompileAsyncQuery((MessagesContext ctx, int tenantId) => 
            ctx.LoginEvents.Where(r => r.TenantId == tenantId && r.Active));

    public static readonly Func<MessagesContext, int, int, Task<DbLoginEvent>> LoginEventsByIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery((MessagesContext ctx, int tenantId, int id) => 
            ctx.LoginEvents.SingleOrDefault(e => e.TenantId == tenantId && e.Id == id));

    public static readonly Func<MessagesContext, int, Guid, int, IAsyncEnumerable<DbLoginEvent>> LoginEventsExceptThisAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, Guid userId, int loginEventId) =>
                ctx.LoginEvents
                    .Where(r => r.TenantId == tenantId
                                && r.UserId == userId
                                && r.Id != loginEventId
                                && r.Active));

    public static readonly Func<MessagesContext, int, int, byte, int, int, IAsyncEnumerable<DbAuditEvent>> GetAuditEventsByReferences =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, int entryId, byte entryType, int offset, int count) =>
                ctx.AuditEvents.Join(
                        ctx.FilesAuditReferences,
                        e => e.Id,
                        r => r.AuditEventId, (@event, reference) => new { @event, reference })
                    .Where(x => x.@event.TenantId == tenantId && x.reference.EntryId == entryId && x.reference.EntryType == entryType)
                    .OrderByDescending(x => x.@event.Date)
                    .Skip(offset)
                    .Take(count)
                    .Select(x => x.@event));

    public static readonly Func<MessagesContext, int, int, byte, Task<int>> GetAuditEventsByReferencesTotalCount =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (MessagesContext ctx, int tenantId, int entryId, byte entryType) =>
                ctx.AuditEvents
                    .Join(ctx.FilesAuditReferences,
                        e => e.Id,
                        r => r.AuditEventId, (@event, reference) => new { @event, reference })
                    .Count(x => x.@event.TenantId == tenantId && x.reference.EntryId == entryId && x.reference.EntryType == entryType));
}