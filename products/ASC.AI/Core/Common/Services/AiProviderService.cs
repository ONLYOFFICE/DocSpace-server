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

namespace ASC.AI.Core.Common.Services;

[Scope]
public class AiProviderService(
    AiProviderDao providerDao, 
    TenantManager tenantManager, 
    UserManager userManager, 
    AuthContext authContext,
    ProviderSettings providerSettings)
{
    public async Task<AiProvider> AddProviderAsync(string? title, string? url, string key, ProviderType type)
    {
        await ThrowIfNotAccessAsync();
        
        var settings = providerSettings.Get(type);
        if (settings == null)
        {
            throw new ArgumentException("Provider not supported");
        }
        
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key can not be empty");
        }

        Uri? uri;

        if (!string.IsNullOrEmpty(settings.Url))
        {
            uri = null;
        }
        else if (!string.IsNullOrEmpty(url))
        {
            uri = new Uri(url);
        }
        else
        {
            throw new ArgumentException("Provider must have url");
        }

        var provider = new AiProvider
        {
            Title = !string.IsNullOrEmpty(title) ? title : settings.Name,
            Url = uri,
            Key = key,
            Type = type
        };
        
        return await providerDao.AddProviderAsync(tenantManager.GetCurrentTenantId(), provider);
    }
    
    public async Task<AiProvider> UpdateProviderAsync(int id, string? title, string? url, string? key)
    {
        await ThrowIfNotAccessAsync();

        var provider = await providerDao.GetProviderAsync(tenantManager.GetCurrentTenantId(), id);
        if (provider == null)
        {
            throw new ItemNotFoundException("Provider not found");
        }

        if (!string.IsNullOrEmpty(title))
        {
            provider.Title = title;
        }

        if (provider.Type == ProviderType.OpenAiCompatible && !string.IsNullOrEmpty(url))
        {
            provider.Url = new Uri(url);
        }

        if (!string.IsNullOrEmpty(key))
        {
            provider.Key = key;
        }
        
        return await providerDao.UpdateProviderAsync(provider);
    }
    
    public async IAsyncEnumerable<AiProvider> GetProvidersAsync(int offset, int limit)
    {
        await ThrowIfNotAccessAsync();

        await foreach (var provider in providerDao.GetProvidersAsync(tenantManager.GetCurrentTenantId(), offset, limit))
        {
            yield return provider;
        }
    }

    public async Task<int> GetProvidersTotalCountAsync()
    {
        await ThrowIfNotAccessAsync();

        return await providerDao.GetProvidersTotalCountAsync(tenantManager.GetCurrentTenantId());
    }

    public async Task DeleteProvidersAsync(List<int> ids)
    {
        await ThrowIfNotAccessAsync();
        
        await providerDao.DeleteProviders(tenantManager.GetCurrentTenantId(), ids);
    }
    
    private async Task ThrowIfNotAccessAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException("Access denied");       
        }
    }
}