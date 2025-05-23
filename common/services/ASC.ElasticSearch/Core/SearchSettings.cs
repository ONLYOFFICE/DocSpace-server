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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ASC.ElasticSearch.Core;

public class SearchSettings : ISettings<SearchSettings>
{
    public string Data { get; set; }

    [JsonIgnore]
    public Guid ID => new("{93784AB2-10B5-4C2F-9B36-F2662CCCF316}");
    internal List<SearchSettingsItem> Items
    {
        get
        {
            if (_items != null)
            {
                return _items;
            }

            var parsed = JsonSerializer.Deserialize<List<SearchSettingsItem>>(Data ?? "");

            return _items = parsed ?? [];
        }
        set
        {
            _items = value;
        }
    }

    private List<SearchSettingsItem> _items;

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
        _allItems ??= serviceProvider.GetService<IEnumerable<IFactoryIndexer>>();

    private IEnumerable<IFactoryIndexer> _allItems;

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
