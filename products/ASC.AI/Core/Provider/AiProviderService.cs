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

namespace ASC.AI.Core.Provider;

[Scope]
public class AiProviderService(
    IAiProviderDao providerDao,
    TenantManager tenantManager,
    AuthContext authContext,
    AiConfiguration aiConfiguration,
    UserManager userManager,
    IDistributedLockProvider distributedLockProvider,
    ModelClientFactory modelClientFactory,
    MessageService messageService,
    AiGateway aiGateway,
    ILogger<AiProviderService> logger)
{
    public async Task<AiProvider> AddProviderAsync(string? title, string? url, string key, ProviderType type)
    {
        await ThrowIfNotAccessAsync();

        var settings = aiConfiguration.Get(type);
        if (settings == null)
        {
            throw new ArgumentException(ErrorMessages.IncorrectProvider);
        }

        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(key);

        url = string.IsNullOrEmpty(url) ? settings.Url : new Uri(url).ToString();
        
        ArgumentException.ThrowIfNullOrEmpty(url);

        var defaultModel = await ExecuteProviderRequestAsync(type, async () =>
        {
            var client = modelClientFactory.Create(type, url, key);
            var models = await GetFilteredModelsAsync(client, type);
            return models.FirstOrDefault()?.Id ?? throw new ArgumentException(ErrorMessages.NoModelsAvailable);
        });

        var tenantId = tenantManager.GetCurrentTenantId();

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProviderNameLockKey(tenantId)))
        {
            await ThrowIfProviderNameExistsAsync(tenantId, title);
            return await providerDao.AddProviderAsync(tenantId, title, url, key, type, defaultModel);
        }
    }
    
    public async Task<AiProvider> UpdateProviderAsync(int id, string? title, string? url, string? key)
    {
        await ThrowIfNotAccessAsync();

        var provider = await GetProviderAsync(id);
        var titleChanged = false;

        if (!string.IsNullOrEmpty(title) && !string.Equals(title, provider.Title, StringComparison.Ordinal))
        {
            provider.Title = title;
            titleChanged = true;
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
            await ExecuteProviderRequestAsync(provider.Type, async () =>
            {
                var client = modelClientFactory.Create(provider.Type, provider.Url, provider.Key);
                await client.PingAsync();
                return true;
            });
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        if (!titleChanged)
        {
            return await providerDao.UpdateProviderAsync(tenantId, provider);
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProviderNameLockKey(tenantId)))
        {
            await ThrowIfProviderNameExistsAsync(tenantId, provider.Title, provider.Id);
            return await providerDao.UpdateProviderAsync(tenantId, provider);
        }
    }

    public async IAsyncEnumerable<AiProvider> GetProvidersAsync(int offset, int limit)
    {
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        if (userType is not (EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin))
        {
            throw new SecurityException(ErrorMessages.ManageProviders);
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        await foreach (var provider in providerDao.GetProvidersAsync(tenantId, offset, limit))
        {
            yield return provider;
        }
    }

    public async Task<bool> NeedResetProvidersAsync()
    {
        var canDecryptSomeKey = await providerDao.CanDecryptSomeKeyAsync(tenantManager.GetCurrentTenantId());

        return !canDecryptSomeKey;
    }

    public async Task<int> GetProvidersTotalCountAsync()
    {
        return await providerDao.GetProvidersTotalCountAsync(tenantManager.GetCurrentTenantId());
    }

    public async Task<List<ProviderSettingsData>> GetAvailableProvidersAsync()
    {
        await ThrowIfNotAccessAsync();
        
        return aiConfiguration.GetAvailableProviders()
            .Where(x => x.Type != ProviderType.PortalAi)
            .ToList();
    }

    public async Task DeleteProvidersAsync(HashSet<int> ids)
    {
        await ThrowIfNotAccessAsync();

        await providerDao.DeleteProviders(tenantManager.GetCurrentTenantId(), ids);
    }

    public async Task<IEnumerable<ModelData>> GetModelsAsync(int providerId, Scope? scope)
    {
        var provider = await GetProviderAsync(providerId, forceSystemProvider: true);
        if (provider.NeedReset)
        {
            return [];
        }

        return await ExecuteProviderRequestAsync(provider.Type, async () =>
        {
            var client = modelClientFactory.Create(provider.Type, provider.Url, provider.Key);
            var models = await GetFilteredModelsAsync(client, provider.Type, scope);

            Dictionary<string, AiChatPrice>? priceMap = null;
            if (provider.Type == ProviderType.PortalAi)
            {
                try
                {
                    var prices = await aiGateway.GetPricesAsync();
                    priceMap = prices.Chat.ToDictionary(p => p.Id, p => new AiChatPrice
                    {
                        Prompt = p.Price.Prompt * 1_000_000,
                        Completion = p.Price.Completion * 1_000_000
                    });
                }
                catch(Exception e)
                {
                    // If prices can't be fetched, continue without them
                    logger.ErrorWithException(e);
                }
            }

            return models.Select(m => new ModelData
            {
                Provider = provider,
                ModelId = m.Id,
                Price = priceMap?.GetValueOrDefault(m.Id)
            });
        });
    }

    public async Task<AiProvider> GetProviderAsync(int providerId, bool forceSystemProvider = false)
    {
        var provider = await providerDao.GetProviderAsync(tenantManager.GetCurrentTenantId(), providerId, forceSystemProvider);

        return provider ?? throw new ItemNotFoundException(ErrorMessages.ProviderNotFound);
    }

    public async Task<DefaultAiProvider> SetDefaultProviderAsync(int providerId, string defaultModel)
    {
        await ThrowIfNotAccessAsync();

        var provider = await GetProviderAsync(providerId);

        var tenantId = tenantManager.GetCurrentTenantId();
        var result = await providerDao.SetDefaultProviderAsync(tenantId, provider, defaultModel);
        result.ProviderTitle = provider.Title;

        messageService.Send(MessageAction.AIDefaultProviderSet, MessageTarget.Create(result.ProviderId), provider.Title, defaultModel);

        return result;
    }

    public async Task<DefaultAiProvider?> GetDefaultProviderAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        
        var defaultProvider = await providerDao.GetDefaultProviderAsync(tenantId);
        if (defaultProvider != null)
        {
            return defaultProvider;
        }

        // Auto-set the first provider as default if none is set
        var firstProviderId = await providerDao.GetFirstProviderIdAsync(tenantId);
        if (firstProviderId == null)
        {
            return null;
        }

        var firstProvider = await providerDao.GetProviderAsync(tenantId, firstProviderId.Value, forceSystemProvider: true);
        if (firstProvider == null || firstProvider.NeedReset)
        {
            return null;
        }

        var defaultModel = await GetFirstAvailableModelAsync(firstProvider);
        if (defaultModel == null)
        {
            return null;
        }

        return await providerDao.SetDefaultProviderAsync(tenantId, firstProvider, defaultModel);

        async Task<string?> GetFirstAvailableModelAsync(AiProvider provider)
        {
            var supportedModels = aiConfiguration.GetSupportedModels(provider.Type);
            if (supportedModels is { Count: > 0 })
            {
                return supportedModels.First();
            }

            try
            {
                var client = modelClientFactory.Create(provider.Type, provider.Url, provider.Key);
                var models = await client.ListModelsAsync();
                return models.FirstOrDefault()?.Id;
            }
            catch
            {
                return null;
            }
        }
    }

    private async Task<IEnumerable<ModelInfo>> GetFilteredModelsAsync(IModelClient client, ProviderType type, Scope? scope = null)
    {
        var models = await client.ListModelsAsync(scope);

        var supported = aiConfiguration.GetSupportedModels(type);
        if (supported != null)
        {
            models = models.Where(m => supported.Contains(m.Id));
        }

        return models;
    }
    
    private async Task ThrowIfNotAccessAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(ErrorMessages.ManageProviders);       
        }
    }

    private async Task ThrowIfProviderNameExistsAsync(int tenantId, string title, int excludedProviderId = 0)
    {
        if (await providerDao.IsProviderNameExistsAsync(tenantId, title, excludedProviderId))
        {
            throw new ArgumentException(ErrorMessages.ProviderNameExists);
        }
    }

    private static string GetProviderNameLockKey(int tenantId)
    {
        return $"ai_provider_name_{tenantId}";
    }

    private static async Task<T> ExecuteProviderRequestAsync<T>(ProviderType providerType, Func<Task<T>> request)
    {
        try
        {
            return await request();
        }
        catch (HttpRequestException httpException)
        {
            if (providerType is ProviderType.XAi && httpException.StatusCode is HttpStatusCode.BadRequest 
                || httpException.StatusCode is HttpStatusCode.Unauthorized)
            {
                throw new ArgumentException(ErrorMessages.InvalidKey);
            }

            throw new ArgumentException(ErrorMessages.InvalidUrl);
        }
        catch (Exception)
        {
            throw new ArgumentException(ErrorMessages.InvalidKey);
        }
    }
}
