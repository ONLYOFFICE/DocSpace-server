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
    AiProviderDao providerDao,
    TenantManager tenantManager,
    AuthContext authContext,
    ProviderSettings providerSettings,
    UserManager userManager,
    ModelClientFactory modelClientFactory,
    AiGateway gateway,
    MessageService messageService)
{
    public async Task<AiProvider> AddProviderAsync(string? title, string? url, string key, ProviderType type)
    {
        await ThrowIfNotAccessAsync();

        var settings = providerSettings.Get(type);
        if (settings == null)
        {
            throw new ArgumentException(ErrorMessages.IncorrectProvider);
        }

        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(key);

        url = string.IsNullOrEmpty(url) ? settings.Url : new Uri(url).ToString();

        var defaultModel = await ExecuteProviderRequestAsync(async () =>
        {
            var client = modelClientFactory.Create(type, url, key);
            var models = await GetFilteredModelsAsync(client, type);
            return models.FirstOrDefault()?.Id ?? throw new ArgumentException(ErrorMessages.NoModelsAvailable);
        });

        var tenantId = tenantManager.GetCurrentTenantId();
        
        return await providerDao.AddProviderAsync(tenantId, title, url, key, type, defaultModel);
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
            await ExecuteProviderRequestAsync(async () =>
            {
                var client = modelClientFactory.Create(provider.Type, provider.Url, provider.Key);
                await client.PingAsync();
                return true;
            });
        }
        
        return await providerDao.UpdateProviderAsync(provider);
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
        
        return providerSettings.GetAvailableProviders().ToList();
    }

    public async Task DeleteProvidersAsync(HashSet<int> ids)
    {
        await ThrowIfNotAccessAsync();

        await providerDao.DeleteProviders(tenantManager.GetCurrentTenantId(), ids);
    }

    public async Task<IEnumerable<ModelData>> GetModelsAsync(int providerId, Scope? scope)
    {
        var provider = await GetProviderAsync(providerId);
        if (provider.NeedReset)
        {
            return [];
        }

        return await ExecuteProviderRequestAsync(async () => 
        { 
            var client = modelClientFactory.Create(provider.Type, provider.Url, provider.Key);
            var models = await GetFilteredModelsAsync(client, provider.Type, scope);

            return models.Select(m => new ModelData
            {
                Provider = provider,
                ModelId = m.Id
            });
        });
    }

    public async Task<AiProvider> GetProviderAsync(int providerId)
    {
        var provider = await providerDao.GetProviderAsync(tenantManager.GetCurrentTenantId(), providerId);

        return provider ?? throw new ItemNotFoundException(ErrorMessages.ProviderNotFound);
    }

    public async Task<DefaultAiProvider> SetDefaultProviderAsync(int providerId, string defaultModel)
    {
        await ThrowIfNotAccessAsync();

        var provider = await GetProviderAsync(providerId);

        var tenantId = tenantManager.GetCurrentTenantId();
        var result = await providerDao.SetDefaultProviderAsync(tenantId, providerId, defaultModel);
        result.ProviderTitle = provider.Title;

        messageService.Send(MessageAction.AIDefaultProviderSet, MessageTarget.Create(result.ProviderId), provider.Title, defaultModel);

        return result;
    }

    public async Task<DefaultAiProvider?> GetDefaultProviderAsync()
    {
        return await providerDao.GetDefaultProviderAsync(tenantManager.GetCurrentTenantId());
    }

    public async Task DeleteDefaultProviderAsync()
    {
        await ThrowIfNotAccessAsync();

        var deleted = await providerDao.DeleteDefaultProviderAsync(tenantManager.GetCurrentTenantId());

        if (deleted)
        {
            messageService.Send(MessageAction.AIDefaultProviderDeleted);
        }
    }

    private async Task<IEnumerable<ModelInfo>> GetFilteredModelsAsync(IModelClient client, ProviderType type, Scope? scope = null)
    {
        var models = await client.ListModelsAsync(scope);

        var supported = providerSettings.GetSupportedModels(type);
        if (supported != null)
        {
            models = models.Where(m => supported.Contains(m.Id));
        }

        return models;
    }
    
    private async Task ThrowIfNotAccessAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID) || gateway.Configured)
        {
            throw new SecurityException(ErrorMessages.ManageProviders);       
        }
    }

    private async Task<T> ExecuteProviderRequestAsync<T>(Func<Task<T>> request)
    {
        try
        {
            return await request();
        }
        catch (HttpRequestException httpException)
        {
            if (httpException.StatusCode is HttpStatusCode.Unauthorized)
            {
                throw new ArgumentException(ErrorMessages.InvalidKey);
            }

            throw new ArgumentException(ErrorMessages.InvalidUrl);
        }
    }
}