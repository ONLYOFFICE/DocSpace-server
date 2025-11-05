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

namespace ASC.AI.Core.Settings;

[Scope]
public class AiSettingsService(
    UserManager userManager,
    AuthContext authContext,
    AiSettingsStore aiSettingsStore,
    AiAccessibility accessibility,
    AiGateway aiGateway)
{
    public async Task<WebSearchSettings> SetWebSearchSettingsAsync(bool enabled, EngineType type, string? key)
    {
        await ThrowIfNotAccess();
    
        var settings = await aiSettingsStore.GetWebSearchSettingsAsync();
        settings.Enabled = enabled;
        
        var typeChanged = settings.Type != type;
        settings.Type = type;

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
                }
                break;
            
            case EngineType.None:
                settings.Config = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    
        await aiSettingsStore.SetWebSearchSettingsAsync(settings);
    
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

        var settings = await aiSettingsStore.GetVectorizationSettingsAsync();
        if (type == EmbeddingProviderType.None)
        {
            settings.Type = type;
            settings.Key = null;
        }
        else
        {
            settings.Type = type;
            settings.Key = key;
        }

        await aiSettingsStore.SetVectorizationSettingsAsync(settings);

        return settings;
    }
    
    public async Task<VectorizationSettings> GetVectorizationSettingsAsync()
    {
        await ThrowIfNotAccess();
        
        return await aiSettingsStore.GetVectorizationSettingsAsync();
    }

    public async Task<AiSettings> GetAiSettingsAsync()
    {
        var webSearchTask = aiSettingsStore.IsWebSearchEnabledAsync();
        var vectorizationTask = aiSettingsStore.IsVectorizationEnabledAsync();
        var aiReadyTask = accessibility.IsAiEnabledAsync();
        
        await Task.WhenAll(webSearchTask, vectorizationTask, aiReadyTask);
        
        return new AiSettings
        {
            WebSearchEnabled = await webSearchTask,
            VectorizationEnabled = await vectorizationTask,
            AiReady = await aiReadyTask
        };
    }
    
    private async Task ThrowIfNotAccess()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID) || aiGateway.Configured)
        {
            throw new SecurityException();
        }
    }
}