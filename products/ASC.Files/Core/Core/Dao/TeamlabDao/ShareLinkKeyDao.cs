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

namespace ASC.Files.Core.Data;

/// <summary>
/// The key material of external links of items in private rooms, and everything stored for a link that must go with it.
/// </summary>
[Scope]
public class ShareLinkKeyDao(IDbContextFactory<FilesDbContext> dbContextFactory, TenantManager tenantManager)
{
    public async Task<DbFilesShareLinkKey> GetAsync(Guid linkId)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.ShareLinkKeyAsync(tenantManager.GetCurrentTenantId(), linkId);
    }

    /// <summary>
    /// The working external links of a private room: those of the room itself and of every folder and file in it that
    /// hold key material and have not expired.
    /// </summary>
    public async Task<List<ShareLinkInRoom>> GetWorkingLinksInRoomAsync(int roomId)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.ShareLinksInRoomAsync(tenantManager.GetCurrentTenantId(), roomId)
            .Where(r => r.Options is not { IsExpired: true })
            .ToListAsync();
    }

    /// <summary>
    /// Stores the key material of a link, replacing what the link had. The public key of an existing link is checked by
    /// the caller before this call.
    /// </summary>
    public async Task SaveAsync(Guid linkId, LinkKeyMaterial keyMaterial)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        await filesDbContext.AddOrUpdateAsync(context => context.ShareLinkKeys, new DbFilesShareLinkKey
        {
            TenantId = tenantManager.GetCurrentTenantId(),
            LinkId = linkId,
            PublicKey = keyMaterial.PublicKey,
            PrivateKeyEnc = keyMaterial.PrivateKeyEnc,
            ProofHash = keyMaterial.ProofHash,
            CreateOn = DateTime.UtcNow
        });

        await filesDbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Stores the wraps of a link's secret, replacing the wrap each named user had; the wraps of other users stay.
    /// </summary>
    public async Task SaveSecretWrapsAsync(Guid linkId, IEnumerable<LinkSecretWrap> wraps)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        foreach (var wrap in wraps)
        {
            await filesDbContext.AddOrUpdateAsync(context => context.ShareLinkSecretWraps, new DbFilesShareLinkSecretWrap
            {
                TenantId = tenantId,
                LinkId = linkId,
                UserId = wrap.UserId,
                PublicKeyId = wrap.PublicKeyId,
                Wrap = wrap.Wrap,
                CreateOn = DateTime.UtcNow
            });
        }

        await filesDbContext.SaveChangesAsync();
    }

    /// <summary>The wrap of a link's secret made for one user, or null.</summary>
    public async Task<DbFilesShareLinkSecretWrap> GetSecretWrapAsync(Guid linkId, Guid userId)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.ShareLinkSecretWrapsOfUserAsync(tenantManager.GetCurrentTenantId(), [linkId], userId).FirstOrDefaultAsync();
    }

    /// <summary>The users a link's secret is wrapped for.</summary>
    public async Task<HashSet<Guid>> GetSecretWrapUsersAsync(Guid linkId)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.ShareLinkSecretWrapUsersAsync(tenantManager.GetCurrentTenantId(), linkId).ToHashSetAsync();
    }

    /// <summary>
    /// Deletes the wraps of the secrets of every link of a private room, of its folders and files, made for the given
    /// users: those who stop managing the room's links. The links themselves stay.
    /// </summary>
    public async Task DeleteSecretWrapsInRoomAsync(int roomId, IEnumerable<Guid> userIds)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        var linkIds = await filesDbContext.ShareLinksInRoomAsync(tenantId, roomId).Select(r => r.LinkId).ToListAsync();
        if (linkIds.Count == 0)
        {
            return;
        }

        foreach (var userId in userIds)
        {
            await filesDbContext.DeleteShareLinkSecretWrapsOfUserAsync(tenantId, linkIds, userId);
        }
    }

    /// <summary>
    /// Deletes the wraps of link secrets made for one user in every private room: for an account that is disabled,
    /// deleted or made a guest, which manages no link any more. The links themselves stay.
    /// </summary>
    public async Task DeleteSecretWrapsOfUserAsync(Guid userId)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        await filesDbContext.DeleteShareLinkSecretWrapsOfUserEverywhereAsync(tenantManager.GetCurrentTenantId(), userId);
    }

    /// <summary>
    /// Deletes the key material of a link, the wraps of file keys made for it and the wraps of its secret, in one
    /// transaction.
    /// </summary>
    public async Task DeleteAsync(Guid linkId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var tr = await context.Database.BeginTransactionAsync();

            await context.DeleteShareLinkKeyAsync(tenantId, linkId);
            await context.DeleteShareLinkSecretWrapsAsync(tenantId, linkId);

            // A wrap of a file key made for a link is a file-key row with the link id in place of the user id.
            await context.DeleteAllFileKeysOfUserAsync(tenantId, linkId);

            await tr.CommitAsync();
        });
    }
}

/// <summary>An external link with key material on a private room or on an item in it.</summary>
public class ShareLinkInRoom
{
    public Guid LinkId { get; init; }

    /// <summary>The room, folder or file the link is set on.</summary>
    public int EntryId { get; init; }

    public FileEntryType EntryType { get; init; }

    public FileShareOptions Options { get; init; }

    public string PublicKey { get; init; }

    /// <summary>
    /// Whether the link covers a file: it is set on the file itself, or on a folder above it, the room included.
    /// </summary>
    public bool Covers(int fileId, IReadOnlySet<int> fileFolderIds)
    {
        return EntryType == FileEntryType.File ? EntryId == fileId : fileFolderIds.Contains(EntryId);
    }
}
