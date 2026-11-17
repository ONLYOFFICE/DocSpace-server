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
