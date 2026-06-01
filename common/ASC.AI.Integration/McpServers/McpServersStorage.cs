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
        if (servers.Count == 0)
        {
            return;
        }

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

                var existingByName = await (entryId.HasValue
                        ? context.GetMcpServersByNamesAndEntryAsync(tenantId, entryId.Value, encryptedServers.Keys)
                        : context.GetMcpServersByNamesAsync(tenantId, encryptedServers.Keys))
                    .ToDictionaryAsync(x => x.Name, x => x.Id);

                var now = DateTime.UtcNow;
                foreach (var (name, config) in encryptedServers)
                {
                    if (existingByName.TryGetValue(name, out var existingId))
                    {
                        var entity = new DbMcpServer
                        {
                            Id = existingId,
                            TenantId = tenantId,
                            Name = name,
                            Config = config,
                            EntryId = entryId,
                            CreatedAt = default
                        };
                        context.McpServers.Attach(entity);
                        context.Entry(entity).Property(x => x.Config).IsModified = true;
                    }
                    else
                    {
                        context.McpServers.Add(new DbMcpServer
                        {
                            Id = Guid.CreateVersion7(),
                            TenantId = tenantId,
                            Name = name,
                            Config = config,
                            EntryId = entryId,
                            CreatedAt = now
                        });
                    }
                }

                await context.SaveChangesAsync();
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
