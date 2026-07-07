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

namespace ASC.Core.Common.Data;

[Scope]
public class AppSettingsService(
    IDbContextFactory<WebstudioDbContext> dbContextFactory,
    CoreBaseSettings coreBaseSettings)
{
    public async Task<List<AppItem>> GetAppsAsync(int tenantId)
    {
        var defaults = coreBaseSettings.DefaultApps;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var overrides = await dbContext.AppSettings
            .Where(x => x.TenantId == tenantId)
            .ToDictionaryAsync(x => x.Id);

        var result = new List<AppItem>(defaults.Count);

        foreach (var d in defaults)
        {
            overrides.TryGetValue(d.Id, out var o);

            result.Add(new AppItem
            {
                Id = d.Id,
                Enabled = o?.Enabled ?? d.Enabled,
                Settings = o?.Settings
            });
        }

        return result;
    }

    public async Task<AppItem> GetAppAsync(int tenantId, string id)
    {
        var defaults = coreBaseSettings.DefaultApps;
        var d = defaults.FirstOrDefault(x => x.Id == id);
        if (d == null)
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var o = await dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id);

        return new AppItem
        {
            Id = d.Id,
            Enabled = o?.Enabled ?? d.Enabled,
            Settings = o?.Settings
        };
    }


    public async Task<AppItem> SetEnabledAsync(int tenantId, string id, bool enabled)
    {
        var defaults = coreBaseSettings.DefaultApps;
        _ = defaults.FirstOrDefault(x => x.Id == id)
            ?? throw new ItemNotFoundException($"App '{id}' not found");

        var result = await UpsertAsync(tenantId, id, e =>
        {
            e.Enabled = enabled;
        });

        return new AppItem
        {
            Id = result.Id,
            Enabled = result.Enabled,
            Settings = result.Settings
        };
    }

    public async Task<AppItem> SetSettingsAsync(int tenantId, string id, string settingsJson)
    {
        var defaults = coreBaseSettings.DefaultApps;
        _ = defaults.FirstOrDefault(x => x.Id == id) ?? throw new ItemNotFoundException($"App '{id}' not found");

        if (!string.IsNullOrEmpty(settingsJson))
        {
            try
            {
                using var _ = JsonDocument.Parse(settingsJson);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Settings must be valid JSON", nameof(settingsJson), ex);
            }
        }

        var result = await UpsertAsync(tenantId, id, e =>
        {
            e.Settings = settingsJson;
        });

        return new AppItem
        {
            Id = result.Id,
            Enabled = result.Enabled,
            Settings = result.Settings
        };
    }

    private async Task<DbAppSettings> UpsertAsync(int tenantId, string id, Action<DbAppSettings> mutate)
    {
        var defaults = coreBaseSettings.DefaultApps;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = await dbContext.AppSettings
            .AsTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id);

        var now = DateTime.UtcNow;

        if (entity == null)
        {
            entity = new DbAppSettings
            {
                TenantId = tenantId,
                Id = id,
                LastModified = now,
                Enabled = defaults.FirstOrDefault(x => x.Id == id)?.Enabled ?? false
            };
            mutate(entity);
            await dbContext.AppSettings.AddAsync(entity);
        }
        else
        {
            mutate(entity);
            entity.LastModified = now;
        }

        await dbContext.SaveChangesAsync();
        return entity;
    }
}

