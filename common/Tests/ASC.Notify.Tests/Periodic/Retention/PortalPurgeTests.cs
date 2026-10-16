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

namespace ASC.Notify.Tests.Periodic.Retention;

/// <summary>
/// The last step of removing a portal, the one that takes its rows away: the purge deletes the portal
/// row and lets the database take everything that belongs to it. A table whose rows stop that from
/// happening leaves the portal half removed - its files already gone from storage, the row stuck in
/// <c>RemovePending</c> - for the owner who asked and for the retention policy alike.
/// </summary>
public class PortalPurgeTests
{
    private static async ValueTask<LetterStackFixture> GetStackAsync()
    {
        return await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException(
                $"No stack in the test context. {nameof(LetterStackFixture)} is registered with "
                + "[assembly: AssemblyFixture] and starts before any letter test runs.");
    }

    [Fact]
    public async Task Purge_RemovesAPortalWhoseFilesCarryEncryptionKeys()
    {
        var stack = await GetStackAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var portal = await stack.CreatePortalAsync(cancellationToken);
        using var scope = stack.Host.CreateScope();

        var services = scope.ServiceProvider;
        await services.GetRequiredService<TenantManager>().SetCurrentTenantAsync(portal.TenantId);

        var filesDb = services.GetRequiredService<IDbContextFactory<FilesDbContext>>();

        // A key of an encrypted file, the way assigning a file-level key writes it.
        await using (var db = await filesDb.CreateDbContextAsync(cancellationToken))
        {
            db.DbFileKeys.Add(new DbFileKeys
            {
                TenantId = portal.TenantId,
                FileId = 1,
                UserId = portal.Owner.Id,
                PublicKeyId = Guid.NewGuid(),
                PrivateKeyEnc = "encrypted-private-key",
                CreateOn = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
        }

        // What RemovePortalOperation does once the storage is wiped.
        var tenantService = services.GetRequiredService<ITenantService>();

        await tenantService.PermanentlyRemoveTenantAsync(portal.TenantId);

        // Read from the database rather than the tenant cache, which is invalidated asynchronously.
        await using (var db = await services.GetRequiredService<IDbContextFactory<TenantDbContext>>().CreateDbContextAsync(cancellationToken))
        {
            (await db.Tenants.AnyAsync(t => t.Id == portal.TenantId, cancellationToken))
                .Should().BeFalse("the portal row is gone");
        }

        await using (var db = await filesDb.CreateDbContextAsync(cancellationToken))
        {
            (await db.DbFileKeys.AnyAsync(k => k.TenantId == portal.TenantId, cancellationToken))
                .Should().BeFalse("the keys of a removed portal go with it");
        }
    }
}
