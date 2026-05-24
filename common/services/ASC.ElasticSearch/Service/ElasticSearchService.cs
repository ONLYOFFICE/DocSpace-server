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

namespace ASC.ElasticSearch.Service;

[Singleton]
public class ElasticSearchService(IServiceProvider serviceProvider, ICacheNotify<ReIndexAction> cacheNotify)
{
    public void Subscribe()
    {
        cacheNotify.Subscribe(a =>
        {
            ReIndex(a.Names.ToList(), a.Tenant);
        }, CacheNotifyAction.Any);
    }

    public bool Support(string table)
    {
        return serviceProvider.GetService<IEnumerable<IFactoryIndexer>>().Any(r => r.IndexName == table);
    }

    private void ReIndex(List<string> toReIndex, int tenant)
    {
        var allItems = serviceProvider.GetService<IEnumerable<IFactoryIndexer>>().ToList();
        var tasks = new List<Task>(toReIndex.Count);

        foreach (var item in toReIndex)
        {
            var index = allItems.Find(r => r.IndexName == item);
            if (index == null)
            {
                continue;
            }

            var generic = typeof(BaseIndexer<>);
            var instance = (IIndexer)Activator.CreateInstance(generic.MakeGenericType(index.GetType()), index);
            tasks.Add(instance.ReIndexAsync());
        }

        if (tasks.Count == 0)
        {
            return;
        }

        Task.WhenAll(tasks).ContinueWith(async _ =>
        {
            using var scope = serviceProvider.CreateScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();
            await tenantManager.SetCurrentTenantAsync(tenant);
            await settingsManager.ClearCacheAsync<SearchSettings>();
        });
    }
    //public State GetState()
    //{
    //    return new State
    //    {
    //        Indexing = Launcher.Indexing,
    //        LastIndexed = Launcher.LastIndexed
    //    };
    //}
}