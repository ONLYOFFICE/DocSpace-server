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

public class AppInfo
{
    public string Id { get; set; }
    public bool Enabled { get; set; }
    public string Settings { get; set; }
}

[Scope]
public class AppSettingsService(
    IDbContextFactory<WebstudioDbContext> dbContextFactory,
    IConfiguration configuration)
{
    private List<AppItem> GetDefaults()
    {
        var defaults = configuration.GetSection("apps").Get<List<AppItem>>();
        return defaults ?? [];
    }

    public async Task<List<AppInfo>> GetAppsAsync(int tenantId)
    {
        var defaults = GetDefaults();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var overrides = await dbContext.AppSettings
            .Where(x => x.TenantId == tenantId)
            .ToDictionaryAsync(x => x.Id);

        var result = new List<AppInfo>(defaults.Count);

        foreach (var d in defaults)
        {
            overrides.TryGetValue(d.Id, out var o);

            result.Add(new AppInfo
            {
                Id = d.Id,
                Enabled = o?.Enabled ?? d.Enabled,
                Settings = o?.Settings
            });
        }

        return result;
    }

    public async Task<AppInfo> GetAppAsync(int tenantId, string id)
    {
        var defaults = GetDefaults();
        var d = defaults.FirstOrDefault(x => x.Id == id);
        if (d == null)
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var o = await dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id);

        return new AppInfo
        {
            Id = d.Id,
            Enabled = o?.Enabled ?? d.Enabled,
            Settings = o?.Settings
        };
    }

    public async Task<string> GetSettingsAsync(int tenantId, string id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.AppSettings
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => x.Settings)
            .FirstOrDefaultAsync();
    }

    public async Task<AppInfo> SetEnabledAsync(int tenantId, string id, bool enabled)
    {
        var defaults = GetDefaults();
        _ = defaults.FirstOrDefault(x => x.Id == id)
            ?? throw new ItemNotFoundException($"App '{id}' not found");

        await UpsertAsync(tenantId, id, e =>
        {
            e.Enabled = enabled;
        });

        return await GetAppAsync(tenantId, id);
    }

    public async Task<AppInfo> SetSettingsAsync(int tenantId, string id, string settingsJson)
    {
        var defaults = GetDefaults();
        _ = defaults.FirstOrDefault(x => x.Id == id)
            ?? throw new ItemNotFoundException($"App '{id}' not found");

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

        await UpsertAsync(tenantId, id, e =>
        {
            e.Settings = settingsJson;
        });

        return await GetAppAsync(tenantId, id);
    }

    private async Task UpsertAsync(int tenantId, string id, Action<DbAppSettings> mutate)
    {
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
                LastModified = now
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
    }
}
