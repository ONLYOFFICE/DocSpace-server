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

namespace ASC.AI.Integration.ToolPrefs;

[Scope]
public class ToolPrefsStorage(
    IDbContextFactory<AiIntegrationContext> dbContextFactory,
    IDistributedLockProvider distributedLockProvider)
{
    public async Task<IReadOnlyDictionary<string, ToolPreference>> ReadAllAsync(int tenantId, Guid createdBy, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var rows = entryId.HasValue
            ? context.GetAllToolPrefsByEntryAsync(tenantId, createdBy, entryId.Value)
            : context.GetAllToolPrefsAsync(tenantId, createdBy);

        return await rows.ToDictionaryAsync(x => x.ServerType, ToDomain);
    }

    public async Task UpsertAsync(int tenantId, Guid createdBy, IReadOnlyDictionary<string, ToolPreference> items, int? entryId = null)
    {
        if (items.Count == 0)
        {
            return;
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId, createdBy, entryId)))
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var strategy = dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var context = await dbContextFactory.CreateDbContextAsync();

                var existingByType = await (entryId.HasValue
                    ? context.GetToolPrefsByServerTypesAndEntryAsync(tenantId, createdBy, entryId.Value, items.Keys)
                    : context.GetToolPrefsByServerTypesAsync(tenantId, createdBy, items.Keys))
                .ToDictionaryAsync(x => x.ServerType, x => x.Id);

                var now = DateTime.UtcNow;
                foreach (var (serverType, item) in items)
                {
                    if (existingByType.TryGetValue(serverType, out var existingId))
                    {
                        var entity = new DbToolPreference
                        {
                            Id = existingId,
                            TenantId = tenantId,
                            CreatedBy = createdBy,
                            ServerType = serverType,
                            EntryId = entryId,
                            Disabled = item.Disabled,
                            AllowAlways = item.AllowAlways,
                            CreatedAt = default
                        };

                        context.ToolPrefs.Attach(entity);

                        if (item.Disabled != null)
                        {
                            context.Entry(entity).Property(x => x.Disabled).IsModified = true;
                        }

                        if (item.AllowAlways != null)
                        {
                            context.Entry(entity).Property(x => x.AllowAlways).IsModified = true;
                        }
                    }
                    else
                    {
                        context.ToolPrefs.Add(new DbToolPreference
                        {
                            Id = Guid.CreateVersion7(),
                            TenantId = tenantId,
                            CreatedBy = createdBy,
                            ServerType = serverType,
                            EntryId = entryId,
                            Disabled = item.Disabled,
                            AllowAlways = item.AllowAlways,
                            CreatedAt = now
                        });
                    }
                }

                await context.SaveChangesAsync();
            });
        }
    }

    private static ToolPreference ToDomain(DbToolPreference entity) => new()
    {
        Disabled = entity.Disabled,
        AllowAlways = entity.AllowAlways
    };

    private static string GetLockKey(int tenantId, Guid createdBy, int? entryId)
    {
        return entryId.HasValue
            ? $"ai_integration_tool_prefs_{tenantId}_{createdBy}_{entryId.Value}"
            : $"ai_integration_tool_prefs_{tenantId}_{createdBy}";
    }
}
