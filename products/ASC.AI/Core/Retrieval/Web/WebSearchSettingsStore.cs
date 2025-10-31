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
public class WebSearchSettingsStore(
    SettingsManager settingsManager,
    InstanceCrypto instanceCrypto,
    TenantManager tenantManager,
    AiGateway aiGateway)
{
    public async Task SetSettingsAsync(WebSearchSettings webSearchSettings)
    {
        var webSearchSettingsRaw = new WebSearchSettingsRaw
        {
            Enabled = webSearchSettings.Enabled, 
            Type = webSearchSettings.Type
        };

        if (webSearchSettings.Config != null)
        {
            var jsonConfig = JsonSerializer.Serialize(webSearchSettings.Config);
            webSearchSettingsRaw.Config = await instanceCrypto.EncryptAsync(jsonConfig);
        }
        
        await settingsManager.SaveAsync(webSearchSettingsRaw);
    }

    public async Task<WebSearchSettings> GetSettingsAsync()
    {
        var webSearchSettingsRaw = await settingsManager.LoadAsync<WebSearchSettingsRaw>();

        var webSearchSettings = new WebSearchSettings
        {
            Enabled = webSearchSettingsRaw.Enabled, 
            Type = webSearchSettingsRaw.Type
        };

        if (webSearchSettingsRaw.Config == null)
        {
            return webSearchSettings;
        }

        var jsonConfig = await instanceCrypto.DecryptAsync(webSearchSettingsRaw.Config);
        webSearchSettings.Config = JsonSerializer.Deserialize<EngineConfig>(jsonConfig);

        return webSearchSettings;
    }

    public async Task<bool> IsEnabledAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        
        if (aiGateway.Configured)
        {
            var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>(tenantId);
            return settings.EnabledServices != null && settings.EnabledServices.Contains(TenantWalletService.WebSearch);
        }
        
        var webSearchSettingsRaw = await settingsManager.LoadAsync<WebSearchSettingsRaw>(tenantId);
        
        return webSearchSettingsRaw.Enabled && webSearchSettingsRaw.Type != EngineType.None;
    }
}