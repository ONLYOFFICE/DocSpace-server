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

namespace ASC.AI.Core.Settings;

[Scope]
public class AiSettingsService(
    UserManager userManager,
    AuthContext authContext,
    AiSettingsStore aiSettingsStore,
    AiAccessibility accessibility,
    AiProviderService providerService,
    VectorizationGlobalSettings vectorizationGlobalSettings,
    SystemMcpConfig systemMcpConfig,
    ModelClientFactory modelClientFactory,
    MessageService messageService,
    SettingsManager settingsManager)
{
    public async Task<WebSearchSettings> SetWebSearchSettingsAsync(bool enabled, EngineType type, string? key)
    {
        await ThrowIfNotAccess();
    
        var settings = await aiSettingsStore.GetWebSearchSettingsAsync();
        settings.Enabled = enabled;
        
        var typeChanged = settings.Type != type;
        settings.Type = type;

        var set = false;

        switch (type)
        {
            case EngineType.Exa:
                if (typeChanged || !string.IsNullOrEmpty(key))
                {
                    ArgumentException.ThrowIfNullOrEmpty(key);
                    settings.Config = new ExaConfig
                    {
                        ApiKey = key
                    };

                    set = true;
                }
                break;
            case EngineType.PortalAi:
                settings.Config = null;
                set = true;
                break;
            case EngineType.None:
                settings.Config = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    
        await aiSettingsStore.SetWebSearchSettingsAsync(settings);

        settings.NeedReset = false;

        if (set)
        {
            messageService.Send(MessageAction.SetWebSearchSettings, type.ToStringFast());
        }
        else
        {
            messageService.Send(MessageAction.ResetWebSearchSettings);
        }
    
        return settings;
    }

    public async Task<WebSearchSettings> GetWebSearchSettingsAsync()
    {
        await ThrowIfNotAccess();
        
        return await aiSettingsStore.GetWebSearchSettingsAsync();
    }
    
    public async Task<VectorizationSettings> SetVectorizationSettingsAsync(EmbeddingProviderType type, string? key)
    {
        await ThrowIfNotAccess();

        var set = false;
        var settings = await aiSettingsStore.GetVectorizationSettingsAsync();
        
        switch (type)
        {
            case EmbeddingProviderType.None:
                settings.Type = type;
                settings.Key = null;
                break;
            case EmbeddingProviderType.PortalAi:
                settings.Type = type;
                settings.Key = null;
                set = true;
                break;
            default:
                {
                    ArgumentException.ThrowIfNullOrEmpty(key);

                    var url = type switch
                    {
                        EmbeddingProviderType.OpenAi => VectorizationGlobalSettings.OpenAiBaseUrl,
                        EmbeddingProviderType.OpenRouter => VectorizationGlobalSettings.OpenRouterBaseUrl,
                        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                    };

                    var client = modelClientFactory.Create(type, url, key);

                    try
                    {
                        await client.PingAsync();
                    }
                    catch (HttpRequestException httpException)
                    {
                        if (httpException.StatusCode is HttpStatusCode.Unauthorized)
                        {
                            throw new ArgumentException(ErrorMessages.InvalidKey);
                        }

                        throw;
                    }

                    settings.Type = type;
                    settings.Key = key;

                    set = true;
                    break;
                }
        }

        await aiSettingsStore.SetVectorizationSettingsAsync(settings);

        settings.NeedReset = false;

        if (set)
        {
            messageService.Send(MessageAction.SetVectorizationSettings, type.ToStringFast());
        }
        else
        {
            messageService.Send(MessageAction.ResetVectorizationSettings);
        }

        return settings;
    }
    
    public async Task<VectorizationSettings> GetVectorizationSettingsAsync()
    {
        await ThrowIfNotAccess();
        
        return await aiSettingsStore.GetVectorizationSettingsAsync();
    }

    public async Task<AiSettings> GetAiSettingsAsync()
    {
        var webSearchSettingsTask = aiSettingsStore.GetWebSearchSettingsAsync();
        var webSearchEnabledTask = aiSettingsStore.IsWebSearchEnabledAsync();

        var vectorizationSettingsTask = aiSettingsStore.GetVectorizationSettingsAsync();
        var vectorizationEnabledTask = aiSettingsStore.IsVectorizationEnabledAsync();

        var needResetProvidersTask = providerService.NeedResetProvidersAsync();
        var aiStatusTask = accessibility.GetStatusAsync();
        var userSettingsTask = settingsManager.LoadForCurrentUserAsync<AiUserSettings>();

        var docSpaceMcpServer = systemMcpConfig.Servers.Values.FirstOrDefault(
            x => x.Type == ServerType.DocSpace);

        await Task.WhenAll(
            webSearchSettingsTask,
            webSearchEnabledTask,
            vectorizationSettingsTask,
            vectorizationEnabledTask,
            needResetProvidersTask,
            aiStatusTask,
            userSettingsTask
            );

        var webSearchNeedReset = (await webSearchSettingsTask).NeedReset;
        var webSearchEnabled = !webSearchNeedReset && (await webSearchEnabledTask);

        var vectorizationNeedReset = (await vectorizationSettingsTask).NeedReset;
        var vectorizationEnabled = !vectorizationNeedReset && (await vectorizationEnabledTask);

        var aiStatus = await aiStatusTask;
        var needResetProviders = await needResetProvidersTask;
        var aiReady = (aiStatus.GatewayEnabled || !needResetProviders) && aiStatus.Enabled;

        var userSettings = await userSettingsTask;

        return new AiSettings
        {
            WebSearchEnabled = webSearchEnabled,
            WebSearchNeedReset = webSearchNeedReset,
            VectorizationEnabled = vectorizationEnabled,
            VectorizationNeedReset = vectorizationNeedReset,
            AiReady = aiReady,
            AiReadyNeedReset = needResetProviders,
            EmbeddingModel = vectorizationGlobalSettings.Model.Id,
            ModelAliases = aiSettingsStore.GetModelAliases(),
            PortalMcpServerId = docSpaceMcpServer?.Id,
            SystemAiEnabled = aiStatus.GatewayEnabled,
            ChatRecomendedModelVisible = userSettings.ChatRecomendedModelVisible,
            RecomendedModelForForms = aiSettingsStore.GetRecomendedModelForForms(),
        };
    }

    public async Task<AiUserSettings> SetAiUserSettingsAsync(bool chatRecomendedModelVisible)
    {
        var settings = new AiUserSettings
        {
            ChatRecomendedModelVisible = chatRecomendedModelVisible,
        };

        await settingsManager.SaveForCurrentUserAsync(settings);

        return settings;
    }

    private async Task ThrowIfNotAccess()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(ErrorMessages.AiSettingsAccessDenied);
        }
    }
}
