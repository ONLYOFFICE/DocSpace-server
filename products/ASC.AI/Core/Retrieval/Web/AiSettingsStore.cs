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

namespace ASC.AI.Core.Retrieval.Web;

[Scope]
public class AiSettingsStore(
    SettingsManager settingsManager,
    InstanceCrypto instanceCrypto,
    TenantManager tenantManager,
    AiGateway aiGateway)
{
    public async Task SetWebSearchSettingsAsync(WebSearchSettings webSearchSettings)
    {
        var encryptedSettings = new EncryptedWebSearchSettings
        {
            Enabled = webSearchSettings.Enabled, 
            Type = webSearchSettings.Type
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

        var webSearchSettings = new WebSearchSettings
        {
            Enabled = webSearchSettingsRaw.Enabled, 
            Type = webSearchSettingsRaw.Type
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
        
        if (aiGateway.Configured)
        {
            var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>(tenantId);
            return settings.EnabledServices != null && settings.EnabledServices.Contains(TenantWalletService.WebSearch);
        }
        
        var webSearchSettingsRaw = await settingsManager.LoadAsync<EncryptedWebSearchSettings>(tenantId);
        
        return webSearchSettingsRaw.Enabled && webSearchSettingsRaw.Type != EngineType.None;
    }
    
    public async Task SetVectorizationSettingsAsync(VectorizationSettings vectorizationSettings)
    {
        var settings = new EncryptedVectorizationSettings
        {
            ProviderType = vectorizationSettings.Type
        };

        if (vectorizationSettings.Type != EmbeddingProviderType.None)
        {
            settings.Key = await instanceCrypto.EncryptAsync(vectorizationSettings.Key);
        }
        
        await settingsManager.SaveAsync(settings);
    }
    
    public async Task<VectorizationSettings> GetVectorizationSettingsAsync()
    {
        var settings = await settingsManager.LoadAsync<EncryptedVectorizationSettings>();
        if (settings.ProviderType == EmbeddingProviderType.None)
        {
            return new VectorizationSettings {
                
                Type = settings.ProviderType, 
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
            Type = settings.ProviderType,
            Key = key,
            NeedReset = reset
        };
    }
    
    public async Task<bool> IsVectorizationEnabledAsync()
    {
        var settings = await settingsManager.LoadAsync<EncryptedVectorizationSettings>();
        return settings.ProviderType != EmbeddingProviderType.None;
    }
}