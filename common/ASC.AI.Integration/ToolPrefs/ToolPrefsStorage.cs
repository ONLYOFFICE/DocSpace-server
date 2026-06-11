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
