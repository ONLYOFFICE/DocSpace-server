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

using System.Text.Json;

namespace ASC.ElasticSearch.Core;

public class SearchSettings : ISettings<SearchSettings>
{
    public string Data { get; set; }
    
    public static Guid ID => new("{93784AB2-10B5-4C2F-9B36-F2662CCCF316}");

    internal List<SearchSettingsItem> Items
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            var parsed = JsonSerializer.Deserialize<List<SearchSettingsItem>>(Data ?? "");

            return field = parsed ?? [];
        }
        set;
    }

    public SearchSettings GetDefault()
    {
        return new SearchSettings();
    }

    public DateTime LastModified { get; set; }

    internal bool IsEnabled(string name)
    {
        var wrapper = Items.FirstOrDefault(r => r.ID == name);

        return wrapper is { Enabled: true };
    }
}

[Scope]
public class SearchSettingsHelper(TenantManager tenantManager,
    SettingsManager settingsManager,
    CoreBaseSettings coreBaseSettings,
    ICacheNotify<ReIndexAction> cacheNotify,
    IServiceProvider serviceProvider)
{
    internal IEnumerable<IFactoryIndexer> AllItems =>
        field ??= serviceProvider.GetService<IEnumerable<IFactoryIndexer>>();

    public async Task<List<SearchSettingsItem>> GetAllItemsAsync()
    {
        if (!coreBaseSettings.Standalone)
        {
            return [];
        }

        var settings = await settingsManager.LoadAsync<SearchSettings>();

        return AllItems.Select(r => new SearchSettingsItem
        {
            ID = r.IndexName,
            Enabled = settings.IsEnabled(r.IndexName),
            Title = r.SettingsTitle
        }).ToList();
    }

    public async Task SetAsync(List<SearchSettingsItem> items)
    {
        if (!coreBaseSettings.Standalone)
        {
            return;
        }

        var settings = await settingsManager.LoadAsync<SearchSettings>();

        var settingsItems = settings.Items;
        var toReIndex = settingsItems.Count == 0 ? items.Where(r => r.Enabled).ToList() : items.Where(item => settingsItems.Any(r => r.ID == item.ID && r.Enabled != item.Enabled)).ToList();

        settings.Items = items;
        settings.Data = JsonSerializer.Serialize(items);
        await settingsManager.SaveAsync(settings);

        var action = new ReIndexAction { Tenant = tenantManager.GetCurrentTenantId() };
        action.Names.AddRange(toReIndex.Select(r => r.ID).ToList());

        await cacheNotify.PublishAsync(action, CacheNotifyAction.Any);
    }

    public async Task<bool> CanIndexByContentAsync<T>() where T : class, ISearchItem
    {
        return await CanIndexByContentAsync(typeof(T));
    }

    public Task<bool> CanIndexByContentAsync(Type t)
    {
        if (!typeof(ISearchItemDocument).IsAssignableFrom(t))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);

        //if (Convert.ToBoolean(_configuration["core:search-by-content"] ?? "false"))
        //{
        //    return true;
        //}

        //if (!_coreBaseSettings.Standalone)
        //{
        //    return true;
        //}

        //var settings = _settingsManager.Load<SearchSettings>(tenantId);

        //return settings.IsEnabled(((ISearchItemDocument)_serviceProvider.GetService(t)).IndexName);
    }

    public async Task<bool> CanSearchByContentAsync<T>() where T : class, ISearchItem
    {
        return await CanSearchByContentAsync(typeof(T));
    }

    public async Task<bool> CanSearchByContentAsync(Type t)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        if (!await CanIndexByContentAsync(t))
        {
            return false;
        }

        if (coreBaseSettings.Standalone)
        {
            return true;
        }

        return (await tenantManager.GetTenantQuotaAsync(tenantId)).ContentSearch;
    }
}

public class SearchSettingsItem
{
    public string ID { get; init; }
    public bool Enabled { get; init; }
    public string Title { get; set; }
}