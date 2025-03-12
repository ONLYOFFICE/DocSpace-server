// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Core.Common.Settings;

[Scope]
public class SettingsManager(
    IServiceProvider serviceProvider,
    ILogger<SettingsManager> logger,
    AuthContext authContext,
    TenantManager tenantManager,
    IDbContextFactory<WebstudioDbContext> dbContextFactory,
    IFusionCache fusionCache)
{
    private static readonly TimeSpan _expirationTimeout = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions  _options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };
    
    public async Task ClearCacheAsync<T>() where T : class, ISettings<T>
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var settings = await LoadAsync<T>(tenantId, Guid.Empty);
        var key = $"{settings.ID}{tenantId}{Guid.Empty}";

        await fusionCache.RemoveAsync(key);
    }

    public T GetDefault<T>() where T : class, ISettings<T>
    {
        var settingsInstance = ActivatorUtilities.CreateInstance<T>(serviceProvider);
        return settingsInstance.GetDefault();
    }

    public async Task<T> LoadAsync<T>(DateTime? lastModified = null) where T : class, ISettings<T>
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return await LoadAsync<T>(tenantId, Guid.Empty, lastModified);
    }
    
    public async Task<T> LoadAsync<T>(Guid userId) where T : class, ISettings<T>
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return await LoadAsync<T>(tenantId, userId);
    }

    public async Task<T> LoadAsync<T>(UserInfo user) where T : class, ISettings<T>
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return await LoadAsync<T>(tenantId, user.Id);
    }

    public Task<T> LoadAsync<T>(int tenantId) where T : class, ISettings<T>
    {
        return LoadAsync<T>(tenantId, Guid.Empty);
    }

    public Task<T> LoadForDefaultTenantAsync<T>() where T : class, ISettings<T>
    {
        return LoadAsync<T>(Tenant.DefaultTenant);
    }

    public Task<T> LoadForCurrentUserAsync<T>() where T : class, ISettings<T>
    {
        return LoadAsync<T>(authContext.CurrentAccount.ID);
    }

    public async Task<bool> SaveAsync<T>(T data) where T : class, ISettings<T>
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return await SaveAsync(data, tenantId, Guid.Empty);
    }
    
    public async Task<bool> SaveAsync<T>(T data, Guid userId) where T : class, ISettings<T>
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return await SaveAsync(data, tenantId, userId);
    }

    public async Task<bool> SaveAsync<T>(T data, UserInfo user) where T : class, ISettings<T>
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return await SaveAsync(data, tenantId, user.Id);
    }

    public Task<bool> SaveAsync<T>(T data, int tenantId) where T : class, ISettings<T>
    {
        return SaveAsync(data, tenantId, Guid.Empty);
    }
    
    public Task<bool> SaveForDefaultTenantAsync<T>(T data) where T : class, ISettings<T>
    {
        return SaveAsync(data, Tenant.DefaultTenant);
    }

    public Task<bool> SaveForCurrentUserAsync<T>(T data) where T : class, ISettings<T>
    {
        return SaveAsync(data, authContext.CurrentAccount.ID);
    }

    public async Task<bool> ManageAsync<T>(Action<T> action) where T : class, ISettings<T>
    {
        var settings = await LoadAsync<T>();
        action(settings);
        return await SaveAsync(settings);
    }

    internal async Task<T> LoadAsync<T>(int tenantId, Guid userId, DateTime? lastModified = null) where T : class, ISettings<T>
    {
        var def = GetDefault<T>();
        var key = def.ID.ToString() + tenantId + userId;

        return await fusionCache.GetOrSetAsync<T>(key, async (ctx, token) =>
        {
            if (lastModified.HasValue && ctx is { HasStaleValue: true, HasLastModified: true } && ctx.LastModified >= lastModified.Value)
            {
                return ctx.NotModified();
            }
            
            await using var context = await dbContextFactory.CreateDbContextAsync(token);
            var result = await context.WebStudioSettingsAsync(tenantId, def.ID, userId);
            var settings = def;
            def.LastModified = DateTime.UtcNow;
            
            if (result != null)
            {
                settings = Deserialize<T>(result.Data);
                settings.LastModified = result.LastModified;
            }
            
            return ctx.Modified(settings, lastModified: settings.LastModified);
        },
        opt => opt.SetDuration(_expirationTimeout).SetFailSafe(true));
    }
    

    private async Task<bool> SaveAsync<T>(T settings, int tenantId, Guid userId) where T : class, ISettings<T>
    {
        ArgumentNullException.ThrowIfNull(settings);

        await using var context = await dbContextFactory.CreateDbContextAsync();

        try
        {
            var key = settings.ID.ToString() + tenantId + userId;
            var data = Serialize(settings);
            var def = GetDefault<T>();

            var defaultData = Serialize(def);

            if (data.SequenceEqual(defaultData))
            {
                var s = await context.WebStudioSettingsAsync(tenantId, settings.ID, userId);

                if (s != null)
                {
                    context.WebstudioSettings.Remove(s);
                }
            }
            else
            {
                var s = new DbWebstudioSettings
                {
                    Id = settings.ID,
                    UserId = userId,
                    TenantId = tenantId,
                    Data = data,
                    LastModified = DateTime.UtcNow
                };

                await context.AddOrUpdateAsync(q => q.WebstudioSettings, s);
            }

            await context.SaveChangesAsync();
            
            await fusionCache.RemoveAsync(key);

            return true;
        }
        catch (Exception ex)
        {
            logger.ErrorSaveSettingsFor(ex);

            return false;
        }
    }

    private T Deserialize<T>(string data)
    {
        return JsonSerializer.Deserialize<T>(data, _options);
    }

    private string Serialize<T>(T settings) where T : class, ISettings<T>
    {
        var temp = settings.LastModified;
        settings.LastModified = default;
        var result = JsonSerializer.Serialize(settings, _options);
        settings.LastModified = temp;
        return result;
    }
}