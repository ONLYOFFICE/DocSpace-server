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
public class AiConfigurationService(
    AiSettingsDao settingsDao, 
    AiProviderDao providerDao, 
    TenantManager tenantManager, 
    AuthContext authContext)
{
    public async Task<ModelConfiguration> SetConfigurationAsync(int providerId, ConfigurationScope scope, RunParameters parameters)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        
        var provider = await providerDao.GetProviderAsync(tenantId, providerId);
        if (provider == null)
        {
            throw new ItemNotFoundException("Provider not found");
        }

        var userId = authContext.CurrentAccount.ID;

        var settings = await settingsDao.GetSettingsAsync(tenantId, userId, scope);
        if (settings == null)
        {
            return await settingsDao.AddSettingsAsync(tenantId, providerId, userId, scope, parameters);
        }

        settings.ProviderId = providerId;
        settings.Parameters = parameters;

        return await settingsDao.UpdateSettingsAsync(settings);
    }
    
    public async Task<ModelConfiguration> GetConfigurationAsync(ConfigurationScope scope)
    {
        var settings = await settingsDao.GetSettingsAsync(
            tenantManager.GetCurrentTenantId(), authContext.CurrentAccount.ID, scope);
        if (settings == null)
        {
            throw new ItemNotFoundException("Model configuration not found");
        }

        return settings;
    }

    public async Task<RunConfiguration> GetRunConfigurationAsync(ConfigurationScope scope)
    {
        var settings = await settingsDao.GetExecutionSettingsAsync(
            tenantManager.GetCurrentTenantId(), authContext.CurrentAccount.ID, scope);
        if (settings == null)
        {
            throw new ItemNotFoundException("Model configuration not found");
        }
        
        return settings;
    }
}