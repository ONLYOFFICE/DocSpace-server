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

namespace ASC.Files.Core.EF;

public partial class FilesDbContext
{
    [PreCompileQuery]
    public Task<DbFilesShareLinkKey> ShareLinkKeyAsync(int tenantId, Guid linkId)
    {
        return ShareLinkKeyQueries.ShareLinkKeyAsync(this, tenantId, linkId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesShareLinkKey> ShareLinkKeysAsync(int tenantId, IEnumerable<Guid> linkIds)
    {
        return ShareLinkKeyQueries.ShareLinkKeysAsync(this, tenantId, linkIds);
    }

    [PreCompileQuery]
    public Task<int> DeleteShareLinkKeyAsync(int tenantId, Guid linkId)
    {
        return ShareLinkKeyQueries.DeleteShareLinkKeyAsync(this, tenantId, linkId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbFilesShareLinkSecretWrap> ShareLinkSecretWrapsOfUserAsync(int tenantId, IEnumerable<Guid> linkIds, Guid userId)
    {
        return ShareLinkKeyQueries.ShareLinkSecretWrapsOfUserAsync(this, tenantId, linkIds, userId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<Guid> ShareLinkSecretWrapUsersAsync(int tenantId, Guid linkId)
    {
        return ShareLinkKeyQueries.ShareLinkSecretWrapUsersAsync(this, tenantId, linkId);
    }

    [PreCompileQuery]
    public Task<int> DeleteShareLinkSecretWrapsAsync(int tenantId, Guid linkId)
    {
        return ShareLinkKeyQueries.DeleteShareLinkSecretWrapsAsync(this, tenantId, linkId);
    }

    [PreCompileQuery]
    public Task<int> DeleteShareLinkSecretWrapsOfUserAsync(int tenantId, IEnumerable<Guid> linkIds, Guid userId)
    {
        return ShareLinkKeyQueries.DeleteShareLinkSecretWrapsOfUserAsync(this, tenantId, linkIds, userId);
    }

    /// <summary>
    /// Deletes the key material of the external links of the given entries. Call it before their share records go: the
    /// share records are what tells which links belong to the entries.
    /// </summary>
    [PreCompileQuery]
    public Task<int> DeleteShareLinkKeysOfEntriesAsync(int tenantId, IEnumerable<int> entryIds, FileEntryType entryType)
    {
        return ShareLinkKeyQueries.DeleteShareLinkKeysOfEntriesAsync(this, tenantId, entryIds, entryType);
    }

    /// <summary>
    /// Deletes the wraps of the secrets of the external links of the given entries, before their share records go.
    /// </summary>
    [PreCompileQuery]
    public Task<int> DeleteShareLinkSecretWrapsOfEntriesAsync(int tenantId, IEnumerable<int> entryIds, FileEntryType entryType)
    {
        return ShareLinkKeyQueries.DeleteShareLinkSecretWrapsOfEntriesAsync(this, tenantId, entryIds, entryType);
    }
}

static file class ShareLinkKeyQueries
{
    public static readonly Func<FilesDbContext, int, Guid, Task<DbFilesShareLinkKey>> ShareLinkKeyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid linkId) =>
                ctx.ShareLinkKeys
                    .AsNoTracking()
                    .FirstOrDefault(r => r.TenantId == tenantId && r.LinkId == linkId));

    public static readonly Func<FilesDbContext, int, IEnumerable<Guid>, IAsyncEnumerable<DbFilesShareLinkKey>> ShareLinkKeysAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<Guid> linkIds) =>
                ctx.ShareLinkKeys
                    .AsNoTracking()
                    .Where(r => r.TenantId == tenantId && linkIds.Contains(r.LinkId)));

    public static readonly Func<FilesDbContext, int, Guid, Task<int>> DeleteShareLinkKeyAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid linkId) =>
                ctx.ShareLinkKeys
                    .Where(r => r.TenantId == tenantId && r.LinkId == linkId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<Guid>, Guid, IAsyncEnumerable<DbFilesShareLinkSecretWrap>> ShareLinkSecretWrapsOfUserAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<Guid> linkIds, Guid userId) =>
                ctx.ShareLinkSecretWraps
                    .AsNoTracking()
                    .Where(r => r.TenantId == tenantId && r.UserId == userId && linkIds.Contains(r.LinkId)));

    public static readonly Func<FilesDbContext, int, Guid, IAsyncEnumerable<Guid>> ShareLinkSecretWrapUsersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid linkId) =>
                ctx.ShareLinkSecretWraps
                    .Where(r => r.TenantId == tenantId && r.LinkId == linkId)
                    .Select(r => r.UserId));

    public static readonly Func<FilesDbContext, int, Guid, Task<int>> DeleteShareLinkSecretWrapsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid linkId) =>
                ctx.ShareLinkSecretWraps
                    .Where(r => r.TenantId == tenantId && r.LinkId == linkId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, FileEntryType, Task<int>> DeleteShareLinkKeysOfEntriesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> entryIds, FileEntryType entryType) =>
                ctx.ShareLinkKeys
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ctx.Security.Any(s =>
                        s.TenantId == tenantId &&
                        s.EntryType == entryType &&
                        entryIds.Contains(s.InternalEntryId) &&
                        s.Subject == r.LinkId &&
                        (s.SubjectType == SubjectType.ExternalLink || s.SubjectType == SubjectType.PrimaryExternalLink)))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<int>, FileEntryType, Task<int>> DeleteShareLinkSecretWrapsOfEntriesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<int> entryIds, FileEntryType entryType) =>
                ctx.ShareLinkSecretWraps
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => ctx.Security.Any(s =>
                        s.TenantId == tenantId &&
                        s.EntryType == entryType &&
                        entryIds.Contains(s.InternalEntryId) &&
                        s.Subject == r.LinkId &&
                        (s.SubjectType == SubjectType.ExternalLink || s.SubjectType == SubjectType.PrimaryExternalLink)))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<Guid>, Guid, Task<int>> DeleteShareLinkSecretWrapsOfUserAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<Guid> linkIds, Guid userId) =>
                ctx.ShareLinkSecretWraps
                    .Where(r => r.TenantId == tenantId && r.UserId == userId && linkIds.Contains(r.LinkId))
                    .ExecuteDelete());
}
