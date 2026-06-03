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

namespace ASC.AI.Core.Retrieval.Web;

[Scope]
public class AiSettingsStore(
    SettingsManager settingsManager,
    InstanceCrypto instanceCrypto,
    TenantManager tenantManager,
    AiConfiguration aiConfiguration,
    AiGateway aiGateway)
{
    public async Task SetWebSearchSettingsAsync(WebSearchSettings webSearchSettings)
    {
        var encryptedSettings = new EncryptedWebSearchSettings
        {
            Enabled = webSearchSettings.Enabled,
            Type = webSearchSettings.Type,
            IsConfigured = true
        };

        if (webSearchSettings.Config != null)
        {
            var jsonConfig = JsonSerializer.Serialize(webSearchSettings.Config);
            encryptedSettings.Config = await instanceCrypto.EncryptAsync(jsonConfig);
        }
        
        await settingsManager.SaveAsync(encryptedSettings);
    }

    public async Task<WebSearchSettings> GetWebSearchSettingsAsync()
    {
        var webSearchSettingsRaw = await settingsManager.LoadAsync<EncryptedWebSearchSettings>();
        var type = await NormalizeSystemTypeAsync(
            webSearchSettingsRaw.Type,
            EngineType.None,
            EngineType.PortalAi,
            webSearchSettingsRaw.IsConfigured);

        var webSearchSettings = new WebSearchSettings 
        { 
            Enabled = type != EngineType.None
                && (webSearchSettingsRaw.Enabled || !webSearchSettingsRaw.IsConfigured),
            Type = type
        };

        if (webSearchSettingsRaw.Config == null)
        {
            return webSearchSettings;
        }

        var jsonConfig = string.Empty;
        try
        {
            jsonConfig = await instanceCrypto.DecryptAsync(webSearchSettingsRaw.Config);
        }
        catch (CryptographicException)
        {
            webSearchSettings.NeedReset = true;
        }

        if (!webSearchSettings.NeedReset)
        {
            webSearchSettings.Config = JsonSerializer.Deserialize<EngineConfig>(jsonConfig);
        }

        return webSearchSettings;
    }

    public async Task<bool> IsWebSearchEnabledAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var webSearchSettingsRaw = await settingsManager.LoadAsync<EncryptedWebSearchSettings>(tenantId);
        var type = await NormalizeSystemTypeAsync(
            webSearchSettingsRaw.Type,
            EngineType.None,
            EngineType.PortalAi,
            webSearchSettingsRaw.IsConfigured);

        return type != EngineType.None
            && (webSearchSettingsRaw.Enabled || !webSearchSettingsRaw.IsConfigured);
    }
    
    public async Task SetVectorizationSettingsAsync(VectorizationSettings vectorizationSettings)
    {
        var settings = new EncryptedVectorizationSettings
        {
            ProviderType = vectorizationSettings.Type,
            IsConfigured = true
        };

        if (vectorizationSettings.Type is not EmbeddingProviderType.None and not EmbeddingProviderType.PortalAi)
        {
            settings.Key = await instanceCrypto.EncryptAsync(vectorizationSettings.Key);
        }
        
        await settingsManager.SaveAsync(settings);
    }
    
    public async Task<VectorizationSettings> GetVectorizationSettingsAsync()
    {
        var settings = await settingsManager.LoadAsync<EncryptedVectorizationSettings>();
        var providerType = await NormalizeSystemTypeAsync(
            settings.ProviderType,
            EmbeddingProviderType.None,
            EmbeddingProviderType.PortalAi,
            settings.IsConfigured);

        if (providerType is EmbeddingProviderType.None or EmbeddingProviderType.PortalAi)
        {
            return new VectorizationSettings
            {
                Type = providerType,
                Key = null
            };
        }

        string? key = null;
        var reset = false;

        try
        {
            key = await instanceCrypto.DecryptAsync(settings.Key);
        }
        catch (CryptographicException)
        {
            reset = true;
        }

        return new VectorizationSettings
        {
            Type = providerType,
            Key = key,
            NeedReset = reset
        };
    }
    
    public async Task<bool> IsVectorizationEnabledAsync()
    {
        var settings = await settingsManager.LoadAsync<EncryptedVectorizationSettings>();
        return await NormalizeSystemTypeAsync(
            settings.ProviderType,
            EmbeddingProviderType.None,
            EmbeddingProviderType.PortalAi,
            settings.IsConfigured) != EmbeddingProviderType.None;
    }

    public IReadOnlyDictionary<string, string> GetModelAliases()
    {
        return aiConfiguration.GetModelAliases();
    }

    public string? GetRecomendedModelForForms()
    {
        return aiConfiguration.RecomendedModelForForms;
    }

    private async Task<T> NormalizeSystemTypeAsync<T>(T type, T noneType, T systemType, bool isConfigured)
        where T : struct, Enum
    {
        var gatewayEnabled = await aiGateway.IsEnabledAsync();

        if (type.Equals(systemType))
        {
            return gatewayEnabled ? type : noneType;
        }

        return !isConfigured && type.Equals(noneType) && gatewayEnabled
            ? systemType
            : type;
    }
}
