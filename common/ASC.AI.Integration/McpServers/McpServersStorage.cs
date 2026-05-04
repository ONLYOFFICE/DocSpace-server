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
public class McpServersStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory)
{
    public async Task CreateAsync(int tenantId, string name, string config)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = new DbMcpServer
        {
            TenantId = tenantId,
            Name = name,
            Config = config,
            CreatedAt = DateTime.UtcNow
        };

        context.McpServers.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task<McpServer?> ReadByNameAsync(int tenantId, string name)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = await context.GetMcpServerAsync(tenantId, name);
        if (entity == null)
        {
            return null;
        }

        return new McpServer
        {
            Name = entity.Name,
            Config = entity.Config
        };
    }

    public async Task<Dictionary<string, string>> ReadAllAsync(int tenantId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.GetAllMcpServersAsync(tenantId)
            .ToDictionaryAsync(x => x.Name, x => x.Config);
    }

    public async Task<bool> UpdateAsync(int tenantId, string name, string config)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.UpdateMcpServerConfigAsync(tenantId, name, config) > 0;
    }

    public async Task ReplaceAllAsync(int tenantId, IReadOnlyDictionary<string, string> servers)
    {
        if (servers.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            var existingNames = await context.GetExistingMcpServerNamesAsync(tenantId, servers.Keys)
                .ToHashSetAsync();

            var now = DateTime.UtcNow;
            foreach (var (name, config) in servers)
            {
                if (existingNames.Contains(name))
                {
                    var entity = new DbMcpServer
                    {
                        TenantId = tenantId,
                        Name = name,
                        Config = config,
                        CreatedAt = default
                    };
                    context.McpServers.Attach(entity);
                    context.Entry(entity).Property(x => x.Config).IsModified = true;
                }
                else
                {
                    context.McpServers.Add(new DbMcpServer
                    {
                        TenantId = tenantId,
                        Name = name,
                        Config = config,
                        CreatedAt = now
                    });
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task DeleteAsync(int tenantId, string name)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            await context.DeleteMcpServerAsync(tenantId, name);
            await context.DeleteToolPrefsByServerTypeAsync(tenantId, name);

            await transaction.CommitAsync();
        });
    }
}
