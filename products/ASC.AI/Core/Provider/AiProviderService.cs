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

namespace ASC.AI.Core.Provider;

[Scope]
public class AiProviderService(
    AiProviderDao providerDao, 
    TenantManager tenantManager, 
    AuthContext authContext,
    ProviderSettings providerSettings,
    UserManager userManager,
    ModelClientFactory modelClientFactory,
    ILogger<AiProviderService> logger,
    AiGateway gateway)
{
    public async Task<AiProvider> AddProviderAsync(string? title, string? url, string key, ProviderType type)
    {
        await ThrowIfNotAccessAsync();
        
        var settings = providerSettings.Get(type);
        if (settings == null)
        {
            throw new ArgumentException("Incorrect provider type");
        }
        
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
        
        url = string.IsNullOrEmpty(url) ? settings.Url : new Uri(url).ToString();
        
        await ThrowIfNotValidAsync(url, key, type);

        var provider = new AiProvider
        {
            Title = title,
            Url = url,
            Key = key,
            Type = type
        };
        
        return await providerDao.AddProviderAsync(tenantManager.GetCurrentTenantId(), provider);
    }
    
    public async Task<AiProvider> UpdateProviderAsync(int id, string? title, string? url, string? key)
    {
        await ThrowIfNotAccessAsync();

        var provider = await GetProviderAsync(id);

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
            await ThrowIfNotValidAsync(provider.Url, provider.Key, provider.Type);
        }
        
        return await providerDao.UpdateProviderAsync(provider);
    }

    public async IAsyncEnumerable<AiProvider> GetProvidersAsync(int offset, int limit)
    {
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        if (userType is not (EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin))
        {
            throw new SecurityException("Access denied");       
        }
        
        if (gateway.IsEnabled)
        {
            yield return new AiProvider
            {
                Id = gateway.ProviderId,
                Title = "DocSpace AI",
                Url = string.Empty,
                Key = string.Empty,
                Type = ProviderType.DocSpaceAi
            };
        }
        else
        {
            await foreach (var provider in providerDao.GetProvidersAsync(tenantManager.GetCurrentTenantId(), offset, limit))
            {
                yield return provider;
            }
        }
    }

    public async Task<int> GetProvidersTotalCountAsync()
    {
        if (gateway.IsEnabled)
        {
            return 1;
        }
        
        return await providerDao.GetProvidersTotalCountAsync(tenantManager.GetCurrentTenantId());
    }

    public async Task<List<ProviderSettingsData>> GetAvailableProvidersAsync()
    {
        await ThrowIfNotAccessAsync();
        
        return providerSettings.GetAvailableProviders().ToList();
    }

    public async Task DeleteProvidersAsync(List<int> ids)
    {
        await ThrowIfNotAccessAsync();
        
        await providerDao.DeleteProviders(tenantManager.GetCurrentTenantId(), ids);
    }

    public async Task<IEnumerable<ModelData>> GetModelsAsync(int? providerId, Scope? scope)
    {
        if (gateway.IsEnabled)
        {
            var provider = await GetProviderAsync(gateway.ProviderId);
            return await GetProviderModelsAsync(provider, scope);
        }
        
        if (providerId.HasValue)
        {
            var provider = await GetProviderAsync(providerId.Value);
            return await GetProviderModelsAsync(provider, scope);
        }
        
        var providers = await GetProvidersAsync(0, 10).ToListAsync();

        var tasks = providers.Select(p => 
            Task.Run(async () => 
            { 
                try
                {
                    return await GetProviderModelsAsync(p, scope);
                }
                catch (Exception ex)
                {
                    logger.ErrorWithException(ex);
                }

                return []; 
            }));

        var result = await Task.WhenAll(tasks);
        
        return result.SelectMany(x => x);
    }
    
    public async Task<AiProvider> GetProviderAsync(int providerId)
    {
        if (gateway.IsEnabled)
        {
            return new AiProvider
            {
                Id = gateway.ProviderId,
                Title = "DocSpace AI",
                Url = gateway.Url,
                Key = await gateway.GetKeyAsync(),
                Type = ProviderType.DocSpaceAi
            };
        }

        var provider = await providerDao.GetProviderAsync(tenantManager.GetCurrentTenantId(), providerId);
        return provider ?? throw new ItemNotFoundException("Provider not found");
    }
    
    private async Task<IEnumerable<ModelData>> GetProviderModelsAsync(AiProvider p, Scope? scope)
    {
        var client = modelClientFactory.Create(p.Type);
        var models = await client.GetModelsAsync(p.Url, p.Key, scope);

        return models.Select(m => new ModelData
        {
            Provider = p,
            ModelId = m.Id
        });
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
        var models = await modelClient.GetModelsAsync(url, key, null);
        if (models.Count == 0)
        {
            throw new ArgumentException("Invalid provider");
        }
    }
}