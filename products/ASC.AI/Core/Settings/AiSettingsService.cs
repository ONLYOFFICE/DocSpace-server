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

using ASC.AI.Core.Knowledge;

namespace ASC.AI.Core.Settings;

[Scope]
public class AiSettingsService(
    UserManager userManager,
    AuthContext authContext,
    AiSettingsStore aiSettingsStore,
    AiAccessibility accessibility,
    VectorizationGlobalSettings vectorizationGlobalSettings,
    EmbeddingProviderProbe embeddingProviderProbe,
    MessageService messageService,
    SettingsManager settingsManager,
    AiGateway gateway)
{
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
            default:
                {
                    ArgumentException.ThrowIfNullOrEmpty(key);

                    var url = type switch
                    {
                        EmbeddingProviderType.OpenAi => VectorizationGlobalSettings.OpenAiBaseUrl,
                        EmbeddingProviderType.OpenRouter => VectorizationGlobalSettings.OpenRouterBaseUrl,
                        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                    };

                    try
                    {
                        await embeddingProviderProbe.PingAsync(type, url, key);
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
        var aiStatus = await accessibility.GetStatusAsync();
        if (aiStatus.Enabled && aiStatus.GatewayEnabled)
        {
            return new AiSettings
            {
                VectorizationEnabled = true,
                AiReady = true,
                EmbeddingModel = vectorizationGlobalSettings.Model.Id,
                SystemAiEnabled = true,
                RecommendedModelForForms = aiSettingsStore.GetRecommendedModelForForms(),
            };
        }

        var vectorizationSettingsTask = aiSettingsStore.GetVectorizationSettingsAsync();
        var vectorizationEnabledTask = aiSettingsStore.IsVectorizationEnabledAsync();

        await Task.WhenAll(vectorizationSettingsTask, vectorizationEnabledTask);

        var vectorizationNeedReset = (await vectorizationSettingsTask).NeedReset;
        var vectorizationEnabled = !vectorizationNeedReset && (await vectorizationEnabledTask);

        return new AiSettings
        {
            VectorizationEnabled = vectorizationEnabled,
            VectorizationNeedReset = vectorizationNeedReset,
            AiReady = aiStatus.Enabled,
            EmbeddingModel = vectorizationGlobalSettings.Model.Id,
            SystemAiEnabled = aiStatus.GatewayEnabled,
            RecommendedModelForForms = aiSettingsStore.GetRecommendedModelForForms(),
        };
    }

    public async Task<AiUserSettings> GetAiUserSettingsAsync()
    {
        return await settingsManager.LoadForCurrentUserAsync<AiUserSettings>();
    }

    public async Task<AiUserSettings> SetAiUserSettingsAsync(bool chatRecommendedModelVisible)
    {
        var settings = new AiUserSettings
        {
            ChatRecommendedModelVisible = chatRecommendedModelVisible,
        };

        await settingsManager.SaveForCurrentUserAsync(settings);

        messageService.Send(MessageAction.UserUpdatedAiSettings);

        return settings;
    }

    private async Task ThrowIfNotAccess()
    {
        // Settings are managed externally when the gateway is configured — nobody can edit them.
        // Otherwise, only DocSpace admins have access.
        if (gateway.Configured || !await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(ErrorMessages.AiSettingsAccessDenied);
        }
    }
}
