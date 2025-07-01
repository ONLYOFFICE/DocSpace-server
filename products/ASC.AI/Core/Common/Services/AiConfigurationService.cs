// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.AI.Core.Common.Services;

[Scope]
public class AiConfigurationService(
    AiConfigurationDao configurationDao, 
    AiProviderDao providerDao, 
    TenantManager tenantManager, 
    AuthContext authContext,
    ProviderSettings providerSettings,
    UserManager userManager,
    ModelClientFactory modelClientFactory)
{
    public async Task<AiProvider> AddProviderAsync(string? title, string? url, string key, ProviderType type)
    {
        await ThrowIfNotAccessAsync();
        
        var settings = providerSettings.Get(type);
        if (settings == null || settings.Internal)
        {
            throw new ArgumentException("Provider not supported");
        }
        
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        Uri? uri;

        switch (type)
        {
            case ProviderType.OpenAiCompatible:
                ArgumentException.ThrowIfNullOrEmpty(url, nameof(url));
                uri = new Uri(url);
                await ThrowIfNotValidAsync(url, key, type);
                break;
            case ProviderType.OpenAi:
            case ProviderType.TogetherAi:
                uri = null;
                await ThrowIfNotValidAsync(settings.Url!, key, type);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        var provider = new AiProvider
        {
            Title = !string.IsNullOrEmpty(title) ? title : settings.Name,
            Url = uri?.ToString(),
            Key = key,
            Type = type
        };
        
        return await providerDao.AddProviderAsync(tenantManager.GetCurrentTenantId(), provider);
    }
    
    public async Task<AiProvider> UpdateProviderAsync(int id, string? title, string? url, string? key)
    {
        await ThrowIfNotAccessAsync();

        var provider = await providerDao.GetProviderAsync(tenantManager.GetCurrentTenantId(), id);
        if (provider == null)
        {
            throw new ItemNotFoundException("Provider not found");
        }

        if (!string.IsNullOrEmpty(title))
        {
            provider.Title = title;
        }

        var needCheck = false;

        if (provider.Type == ProviderType.OpenAiCompatible && !string.IsNullOrEmpty(url))
        {
            provider.Url = new Uri(url).ToString();
            needCheck = true;
        }

        if (!string.IsNullOrEmpty(key))
        {
            provider.Key = key;
            needCheck = true;
        }

        if (needCheck)
        {
            var endpoint = provider.Url ?? providerSettings.Get(provider.Type)?.Url;
            await ThrowIfNotValidAsync(endpoint!, provider.Key, provider.Type);
        }
        
        return await providerDao.UpdateProviderAsync(provider);
    }
    
    public async IAsyncEnumerable<AiProvider> GetProvidersAsync(int offset, int limit)
    {
        await ThrowIfNotAccessAsync();

        await foreach (var provider in providerDao.GetProvidersAsync(tenantManager.GetCurrentTenantId(), offset, limit))
        {
            provider.Url ??= ResolveEndpoint(provider.Type);

            yield return provider;
        }
    }

    public async Task<int> GetProvidersTotalCountAsync()
    {
        await ThrowIfNotAccessAsync();

        return await providerDao.GetProvidersTotalCountAsync(tenantManager.GetCurrentTenantId());
    }

    public async Task DeleteProvidersAsync(List<int> ids)
    {
        await ThrowIfNotAccessAsync();
        
        await providerDao.DeleteProviders(tenantManager.GetCurrentTenantId(), ids);
    }
    
    public async Task<ModelConfiguration> SetConfigurationAsync(int providerId, ConfigurationScope scope, RunParameters parameters)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        
        var provider = await providerDao.GetProviderAsync(tenantId, providerId);
        if (provider == null)
        {
            throw new ItemNotFoundException("Provider not found");
        }

        var userId = authContext.CurrentAccount.ID;

        var settings = await configurationDao.GetConfigurationAsync(tenantId, userId, scope);
        if (settings == null)
        {
            return await configurationDao.AddConfigurationAsync(tenantId, providerId, userId, scope, parameters);
        }

        settings.ProviderId = providerId;
        settings.Parameters = parameters;

        return await configurationDao.UpdateConfigurationAsync(settings);
    }
    
    public async Task<ModelConfiguration> GetConfigurationAsync(ConfigurationScope scope)
    {
        var settings = await configurationDao.GetConfigurationAsync(tenantManager.GetCurrentTenantId(), 
            authContext.CurrentAccount.ID, scope);
        if (settings == null)
        {
            throw new ItemNotFoundException("Model configuration not found");
        }

        return settings;
    }

    public async Task<RunConfiguration> GetRunConfigurationAsync(ConfigurationScope scope)
    {
        var config = await configurationDao.GetRunConfiguration(
            tenantManager.GetCurrentTenantId(), authContext.CurrentAccount.ID, scope);
        
        if (config == null)
        { 
            throw new ItemNotFoundException("Model configuration not found");
        }
        
        config.Url ??= ResolveEndpoint(config.ProviderType);

        return config;
    }
    
    private string? ResolveEndpoint(ProviderType type)
    {
        var settings = providerSettings.Get(type);
        return settings?.Url;
    }
    
    private async Task ThrowIfNotAccessAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException("Access denied");       
        }
    }

    private async Task ThrowIfNotValidAsync(string url, string key, ProviderType type)
    {
        var modelClient = modelClientFactory.Create(type);
        var models = await modelClient.GetModelsAsync(url, key);
        if (models.Count == 0)
        {
            throw new ArgumentException("Invalid provider");
        }
    }
}