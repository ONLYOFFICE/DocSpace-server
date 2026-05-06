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
    AiConfiguration aiConfig,
    AiModelSettingsResolver modelSettingsResolver,
    UserManager userManager,
    IDistributedLockProvider distributedLockProvider,
    ModelClientFactory modelClientFactory,
    MessageService messageService,
    AiGateway aiGateway,
    ILogger<AiProviderService> logger)
{
    public async Task<AiProvider> AddProviderAsync(
        string? title,
        string? url,
        string key,
        ProviderType type,
        List<AiModelSettings>? modelSettings = null)
    {
        await ThrowIfNotAccessAsync();

        var settings = aiConfig.Get(type);
        if (settings == null)
        {
            throw new ArgumentException(ErrorMessages.IncorrectProvider);
        }

        ArgumentException.ThrowIfNullOrEmpty(title);
        ArgumentException.ThrowIfNullOrEmpty(key);

        url = string.IsNullOrEmpty(url) ? settings.Url : new Uri(url).ToString();

        ArgumentException.ThrowIfNullOrEmpty(url);

        if (type == ProviderType.OpenAiCompatible && modelSettings is not { Count: > 0 })
        {
            throw new ArgumentException("Model settings must be specified for OpenAI compatible providers.");
        }

        var client = modelClientFactory.Create(type, url, key);

        // The request for a list of models from OpenRouter is public and does not require a key,
        // so we'll use a different one to test it
        if (type is ProviderType.OpenRouter)
        {
            await client.PingAsync();
        }

        var models = await ExecuteProviderRequestAsync(type,
            async() => (await client.ListModelsAsync()).ToList());
        if (models.Count == 0)
        {
            throw new ArgumentException(ErrorMessages.NoModelsAvailable);
        }

        var mSettings = ProcessModelSettings(type, modelSettings);

        var firstModel = ApplySettings(type, models, [], hasModelSettings: true, mSettings).FirstOrDefault();
        if (firstModel == null)
        {
            throw new ArgumentException(ErrorMessages.NoModelsAvailable);
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProviderNameLockKey(tenantId)))
        {
            await ThrowIfProviderNameExistsAsync(tenantId, title);
            return await providerDao.AddProviderAsync(tenantId, title, url, key, type, firstModel.Id, mSettings);
        }
    }

    public async Task<AiProvider> UpdateProviderAsync(
        int id,
        string? title,
        string? url,
        string? key,
        List<AiModelSettings>? deltaSettings = null)
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

        var tenantId = tenantManager.GetCurrentTenantId();
        var mSettings = ProcessModelSettings(provider.Type, deltaSettings);

        if (needCheck || mSettings is { Count: > 0 })
        {
            var client = modelClientFactory.Create(provider.Type, provider.Url, provider.Key);

            if (mSettings is { Count: > 0 })
            {
                var currentSettings = providerDao.GetModelSettingsAsync(tenantId, provider.Id, provider.Type);
                var models = await ExecuteProviderRequestAsync(provider.Type,
                    async () => (await client.ListModelsAsync()).ToList());

                if (!ApplySettings(provider.Type, models, await currentSettings, provider.HasModelSettings, mSettings).Any())
                {
                    throw new ArgumentException(ErrorMessages.NoModelsAvailable);
                }
            }
            else
            {
                await ExecuteProviderRequestAsync(provider.Type, async () =>
                {
                    await client.PingAsync();
                    return true;
                });
            }
        }

        if (!titleChanged)
        {
            return await providerDao.UpdateProviderAsync(tenantId, provider, mSettings);
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetProviderNameLockKey(tenantId)))
        {
            await ThrowIfProviderNameExistsAsync(tenantId, provider.Title, provider.Id);
            return await providerDao.UpdateProviderAsync(tenantId, provider, mSettings);
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

    public async Task<IEnumerable<ProviderSettingsData>> GetAvailableProvidersAsync()
    {
        await ThrowIfNotAccessAsync();

        return aiConfig.GetAvailableProviders()
            .Where(x => x.Type != ProviderType.PortalAi)
            .OrderBy(x => x.Type == ProviderType.OpenAiCompatible)
            .ThenBy(x => x.Type.ToStringLowerFast(), StringComparer.Ordinal);
    }

    public async Task DeleteProvidersAsync(HashSet<int> ids)
    {
        await ThrowIfNotAccessAsync();

        await providerDao.DeleteProviders(tenantManager.GetCurrentTenantId(), ids);
    }

    public async Task<IEnumerable<ModelData>> GetActiveModelsAsync(int providerId)
    {
        var provider = await GetProviderAsync(providerId, forceSystemProvider: true);
        if (provider.NeedReset)
        {
            return [];
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var client = modelClientFactory.Create(provider.Type, provider.Url, provider.Key);
        var models = await ExecuteProviderRequestAsync(provider.Type, client.ListModelsAsync);

        Dictionary<string, AiChatPrice>? priceMap = null;
        CurrencyInfo? currency = null;
        if (provider.Type == ProviderType.PortalAi)
        {
            try
            {
                var prices = await aiGateway.GetPricesAsync();
                currency = prices.Currency;
                priceMap = prices.Chat.ToDictionary(p => p.Id, p => new AiChatPrice
                {
                    Prompt = p.Price.Prompt * 1_000_000,
                    Completion = p.Price.Completion * 1_000_000
                });
            }
            catch(Exception e)
            {
                logger.ErrorWithException(e);
            }
        }

        var dbSettings = await providerDao.GetModelSettingsAsync(tenantId, provider.Id, provider.Type);

        var result = new List<ModelData>();

        foreach (var m in models)
        {
            dbSettings.TryGetValue(m.Id, out var dbSetting);
            var resolved = modelSettingsResolver.Resolve(
                provider.Type,
                m.Id,
                dbSetting,
                provider.HasModelSettings,
                m.Alias,
                m.Capabilities);

            if (resolved is not { IsEnabled: true })
            {
                continue;
            }

            result.Add(new ModelData
            {
                Provider = provider,
                ModelId = m.Id,
                Alias = resolved.Alias,
                Capabilities = resolved.Capabilities,
                Price = priceMap?.GetValueOrDefault(m.Id),
                Currency = priceMap != null ? currency : null
            });
        }

        return result;
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

        var mSettings = await providerDao.GetModelSettingAsync(tenantId, providerId, defaultModel);

        var resolved = modelSettingsResolver.Resolve(provider.Type, defaultModel, mSettings, provider.HasModelSettings);
        if (!resolved.IsEnabled)
        {
            throw new ArgumentException(ErrorMessages.ModelDisabled);
        }

        var defaultProvider = await providerDao.SetDefaultProviderAsync(tenantId, provider, defaultModel);
        defaultProvider.ProviderTitle = provider.Title;

        var result = defaultProvider.Map(resolved.Alias);

        messageService.Send(MessageAction.AIDefaultProviderSet, MessageTarget.Create(result.ProviderId), provider.Title, defaultModel);

        return result;
    }

    public async Task<DefaultAiProvider?> GetDefaultProviderAsync()
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(ErrorMessages.ManageProviders);
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var defaultProvider = await providerDao.GetDefaultProviderAsync(tenantId);
        if (defaultProvider != null)
        {
            return defaultProvider.Map(ResolveModelAlias(defaultProvider));
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

        var mSettings = providerDao.GetModelSettingsAsync(tenantId, firstProvider.Id, firstProvider.Type);

        var client = modelClientFactory.Create(firstProvider.Type, firstProvider.Url, firstProvider.Key);
        IEnumerable<ModelInfo> models;

        try
        {
            models = await client.ListModelsAsync();
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            models = [];
        }

        var firstModel = ApplySettings(firstProvider.Type, models, await mSettings, firstProvider.HasModelSettings).FirstOrDefault();
        if (firstModel == null)
        {
            return null;
        }

        var result = await providerDao.SetDefaultProviderAsync(tenantId, firstProvider, firstModel.Id);

        return result.Map(ResolveModelAlias(result));

        string? ResolveModelAlias(DefaultAiProviderSettings settings)
        {
            var dbSettings = settings.ProviderType == ProviderType.PortalAi
                ? null
                : settings.DbModelSettings;

            return modelSettingsResolver.Resolve(
                settings.ProviderType,
                settings.DefaultModel,
                dbSettings,
                settings.HasModelSettings).Alias;
        }
    }

    public async Task<IEnumerable<ModelSettings>> GetModelsWithSettingsAsync(int providerId)
    {
        await ThrowIfNotAccessAsync();

        var provider = await GetProviderAsync(providerId, forceSystemProvider: true);
        if (provider.NeedReset)
        {
            return [];
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var client = modelClientFactory.Create(provider.Type, provider.Url, provider.Key);
        var models = await ExecuteProviderRequestAsync(provider.Type, client.ListModelsAsync);

        var dbSettings = await providerDao.GetModelSettingsAsync(tenantId, provider.Id, provider.Type);

        return BuildModelSettings(models, provider.Type, dbSettings, provider.HasModelSettings);
    }

    public async Task<IEnumerable<ModelSettings>> GetPreviewModelsAsync(ProviderType type, string? url, string key)
    {
        await ThrowIfNotAccessAsync();

        if (type == ProviderType.PortalAi)
        {
            throw new ArgumentException(ErrorMessages.IncorrectProvider);
        }

        var settings = aiConfig.Get(type);
        if (settings == null)
        {
            throw new ArgumentException(ErrorMessages.IncorrectProvider);
        }

        ArgumentException.ThrowIfNullOrEmpty(key);

        url = string.IsNullOrEmpty(url) ? settings.Url : new Uri(url).ToString();
        ArgumentException.ThrowIfNullOrEmpty(url);

        var client = modelClientFactory.Create(type, url, key);
        var models = await ExecuteProviderRequestAsync(type, client.ListModelsAsync);

        return BuildModelSettings(models, type, [], hasModelSettings: true);
    }

    public async Task<(AiProvider provider, ModelSettings resolved)> GetProviderContextAsync(int providerId, string modelId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var providerTask = GetProviderAsync(providerId);
        var settingTask = providerDao.GetModelSettingAsync(tenantId, providerId, modelId);

        await Task.WhenAll(providerTask, settingTask);

        var provider = await providerTask;
        var setting = await settingTask;

        var resolved = modelSettingsResolver.Resolve(provider.Type, modelId, setting, provider.HasModelSettings);

        return (provider, resolved);
    }

    private List<AiModelSettings>? ProcessModelSettings(ProviderType type, List<AiModelSettings>? changes)
    {
        if (changes is not { Count: > 0 } || type == ProviderType.PortalAi)
        {
            return null;
        }

        var result = new List<AiModelSettings>(changes.Count);

        foreach (var change in changes)
        {
            var recommended = aiConfig.GetModel(type, change.ModelId);
            if (recommended != null)
            {
                result.Add(new AiModelSettings
                {
                    ModelId = change.ModelId,
                    IsEnabled = change.IsEnabled
                });
            }
            else
            {
                result.Add(new AiModelSettings
                {
                    ModelId = change.ModelId,
                    IsEnabled = change.IsEnabled,
                    Alias = change.Alias?.Trim(),
                    Capabilities = change.Capabilities
                });
            }
        }

        return result;
    }

    private IEnumerable<ModelInfo> ApplySettings(
        ProviderType type,
        IEnumerable<ModelInfo> models,
        Dictionary<string, AiModelSettings> currentSettings,
        bool hasModelSettings,
        List<AiModelSettings>? newSettings = null)
    {
        if (newSettings is { Count: > 0 })
        {
            currentSettings = new Dictionary<string, AiModelSettings>(currentSettings);

            foreach (var s in newSettings)
            {
                currentSettings[s.ModelId] = s;
            }
        }

        foreach (var m in models)
        {
            currentSettings.TryGetValue(m.Id, out var setting);
            var resolved = modelSettingsResolver.Resolve(
                type,
                m.Id,
                setting,
                hasModelSettings,
                m.Alias,
                m.Capabilities);

            if (resolved is { IsEnabled: true })
            {
                yield return m;
            }
        }
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

    private IEnumerable<ModelSettings> BuildModelSettings(
        IEnumerable<ModelInfo> models,
        ProviderType type,
        Dictionary<string, AiModelSettings> dbSettings,
        bool hasModelSettings)
    {
        return models
            .Select(model =>
            {
                dbSettings.TryGetValue(model.Id, out var dbSetting);
                return modelSettingsResolver.Resolve(
                    type,
                    model.Id,
                    dbSetting,
                    hasModelSettings,
                    model.Alias,
                    model.Capabilities);
            })
            .OrderByDescending(x => x.IsRecommended)
            .ThenByDescending(x => x.IsEnabled);
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
