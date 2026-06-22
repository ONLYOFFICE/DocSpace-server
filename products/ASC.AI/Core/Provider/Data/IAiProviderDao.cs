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

namespace ASC.AI.Core.Provider.Data;

public interface IAiProviderDao
{
    Task<AiProvider> AddProviderAsync(
        int tenantId,
        string title,
        string url,
        string key,
        ProviderType type,
        string defaultModel,
        List<AiModelSettings>? modelSettings = null);

    Task<AiProvider?> GetProviderAsync(int tenantId, int id, bool forceSystemProvider = false, bool allowLegacyProvider = false);

    IAsyncEnumerable<AiProvider> GetProvidersAsync(int tenantId, int offset, int limit);

    Task<bool> CanDecryptSomeKeyAsync(int tenantId);

    Task<int> GetProvidersTotalCountAsync(int tenantId);

    Task<bool> IsProviderNameExistsAsync(int tenantId, string title, int excludedProviderId = 0);

    Task<AiProvider> UpdateProviderAsync(int tenantId, AiProvider provider, List<AiModelSettings>? modelSettings = null);

    Task DeleteProviders(int tenantId, HashSet<int> ids);

    Task<DefaultAiProviderSettings> SetDefaultProviderAsync(int tenantId, AiProvider provider, string defaultModel);

    Task<DefaultAiProviderSettings?> GetDefaultProviderAsync(int tenantId);

    Task<int?> GetFirstProviderIdAsync(int tenantId);

    Task<Dictionary<string, AiModelSettings>> GetModelSettingsAsync(int tenantId, int providerId, ProviderType type);
    Task<AiModelSettings?> GetModelSettingAsync(int tenantId, int providerId, string modelId);
}
