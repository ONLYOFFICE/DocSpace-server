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

namespace ASC.AI.Integration.McpServers;

[Scope]
public class McpServersStorage(
    IDbContextFactory<AiIntegrationContext> dbContextFactory,
    IDistributedLockProvider distributedLockProvider,
    InstanceCrypto crypto)
{
    public async Task<bool> CreateAsync(int tenantId, string name, string config, int? entryId = null)
    {
        var encryptedConfig = await EncryptConfigAsync(config);

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId, entryId)))
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var strategy = dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var context = await dbContextFactory.CreateDbContextAsync();

                var existing = entryId.HasValue
                    ? await context.GetMcpServerByEntryAsync(tenantId, name, entryId.Value)
                    : await context.GetMcpServerAsync(tenantId, name);

                if (existing != null)
                {
                    return false;
                }

                context.McpServers.Add(new DbMcpServer
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenantId,
                    Name = name,
                    Config = encryptedConfig,
                    EntryId = entryId,
                    CreatedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();

                return true;
            });
        }
    }

    public async Task<McpServer?> ReadByNameAsync(int tenantId, string name, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = entryId.HasValue
            ? await context.GetMcpServerByEntryAsync(tenantId, name, entryId.Value)
            : await context.GetMcpServerAsync(tenantId, name);

        return entity == null ? null : await ToDomainAsync(entity);
    }

    public async Task<IReadOnlyList<McpServer>> ReadAllAsync(int tenantId, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var servers = entryId.HasValue
            ? context.GetAllMcpServersByEntryAsync(tenantId, entryId.Value)
            : context.GetAllMcpServersAsync(tenantId);

        var result = new List<McpServer>();
        await foreach (var entity in servers)
        {
            result.Add(await ToDomainAsync(entity));
        }

        return result;
    }

    public async Task<bool> UpdateAsync(int tenantId, string name, string config, int? entryId = null)
    {
        var encryptedConfig = await EncryptConfigAsync(config);

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId, entryId)))
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var affected = entryId.HasValue
                ? await context.UpdateMcpServerConfigByEntryAsync(tenantId, name, entryId.Value, encryptedConfig)
                : await context.UpdateMcpServerConfigAsync(tenantId, name, encryptedConfig);

            return affected > 0;
        }
    }

    public async Task ReplaceAllAsync(int tenantId, IReadOnlyDictionary<string, string> servers, int? entryId = null)
    {
        var encryptedServers = new Dictionary<string, string>(servers.Count);
        foreach (var (name, config) in servers)
        {
            encryptedServers[name] = await EncryptConfigAsync(config);
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId, entryId)))
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var strategy = dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var context = await dbContextFactory.CreateDbContextAsync();
                await using var transaction = await context.Database.BeginTransactionAsync();

                if (entryId.HasValue)
                {
                    await context.DeleteAllMcpServersByEntryAsync(tenantId, entryId.Value);
                }
                else
                {
                    await context.DeleteAllMcpServersAsync(tenantId);
                }

                var now = DateTime.UtcNow;
                context.McpServers.AddRange(encryptedServers.Select(x => new DbMcpServer
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenantId,
                    Name = x.Key,
                    Config = x.Value,
                    EntryId = entryId,
                    CreatedAt = now
                }));

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }
    }

    public async Task DeleteAsync(int tenantId, string name, int? entryId = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            if (entryId.HasValue)
            {
                await context.DeleteMcpServerByEntryAsync(tenantId, name, entryId.Value);
                await context.DeleteToolPrefsByServerTypeAndEntryAsync(tenantId, name, entryId.Value);
            }
            else
            {
                await context.DeleteMcpServerAsync(tenantId, name);
                await context.DeleteToolPrefsByServerTypeAsync(tenantId, name);
            }

            await transaction.CommitAsync();
        });
    }

    private async Task<McpServer> ToDomainAsync(DbMcpServer entity) => new()
    {
        Name = entity.Name,
        Config = await DecryptConfigAsync(entity.Config)
    };

    private async Task<string> EncryptConfigAsync(string config)
    {
        return string.IsNullOrEmpty(config) ? config : await crypto.EncryptAsync(config);
    }

    private async Task<string> DecryptConfigAsync(string config)
    {
        if (string.IsNullOrEmpty(config))
        {
            return config;
        }

        try
        {
            return await crypto.DecryptAsync(config);
        }
        catch (CryptographicException)
        {
            return string.Empty;
        }
    }

    private static string GetLockKey(int tenantId, int? entryId)
    {
        return entryId.HasValue
            ? $"ai_integration_mcp_servers_{tenantId}_{entryId.Value}"
            : $"ai_integration_mcp_servers_{tenantId}";
    }
}
