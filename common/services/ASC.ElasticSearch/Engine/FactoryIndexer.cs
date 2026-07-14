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

namespace ASC.ElasticSearch;

[Singleton]
public class FactoryIndexerHelper
{
    public DateTime LastIndexed { get; set; }
    public string Indexing { get; set; }

    public FactoryIndexerHelper(ICacheNotify<IndexAction> cacheNotify)
    {
        cacheNotify.Subscribe(a =>
        {
            if (a.LastIndexed != 0)
            {
                LastIndexed = new DateTime(a.LastIndexed);
            }
            Indexing = a.Indexing;
        }, CacheNotifyAction.Any);
    }
}

public interface IFactoryIndexer
{
    string IndexName { get; }
    string SettingsTitle { get; }
    Task IndexAllAsync();
    Task ReIndexAsync();
    Task DeleteAsync(int tenantId, bool immediately = true);
}

public abstract class FactoryIndexer<T>(ILoggerFactory loggerFactory,
        TenantManager tenantManager,
        SearchSettingsHelper searchSettingsHelper,
        FactoryIndexer factoryIndexer,
        BaseIndexer<T> baseIndexer,
        IServiceProvider serviceProvider)
    : IFactoryIndexer
    where T : class, ISearchItem
{
    protected ILogger Logger { get; } = loggerFactory.CreateLogger("ASC.Indexer");
    public string IndexName => _indexer.IndexName;
    public virtual string SettingsTitle => string.Empty;

    protected readonly BaseIndexer<T> _indexer = baseIndexer;

    public async Task<(bool, IReadOnlyCollection<T>)> TrySelectAsync(Expression<Func<Selector<T>, Selector<T>>> expression)
    {
        IReadOnlyCollection<T> result;
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync() || !_indexer.CheckExist(t))
        {
            return (false, []);
        }

        try
        {
            result = await _indexer.SelectAsync(expression);
        }
        catch (Exception e)
        {
            Logger.ErrorSelect(e);

            return (false, []);
        }

        return (true, result);
    }

    public async Task<(bool Success, long Count)> TryCountAsync(Expression<Func<Selector<T>, Selector<T>>> expression)
    {
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync() || !_indexer.CheckExist(t))
        {
            return (false, 0);
        }

        try
        {
            var count = await _indexer.CountAsync(expression);

            return (true, count);
        }
        catch (Exception e)
        {
            Logger.ErrorSelect(e);

            return (false, 0);
        }
    }

    public async Task<(bool, List<int>)> TrySelectIdsAsync(Expression<Func<Selector<T>, Selector<T>>> expression)
    {
        List<int> result;
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync() || !_indexer.CheckExist(t))
        {
            result = [];

            return (false, result);
        }

        try
        {
            result = (await _indexer.SelectAsync(expression, true)).Select(r => r.Id).ToList();
        }
        catch (Exception e)
        {
            Logger.ErrorSelect(e);
            result = [];

            return (false, result);
        }

        return (true, result);
    }

    public virtual async Task<bool> CanIndexByContentAsync(T t)
    {
        return await SupportAsync() && await searchSettingsHelper.CanIndexByContentAsync<T>();
    }

    public async Task<bool> Index(T data, bool immediately = true)
    {
        if (!await SupportAsync())
        {
            return false;
        }

        try
        {
            await _indexer.IndexAsync(data, immediately);

            return true;
        }
        catch (Exception e)
        {
            Logger.ErrorIndex(e);
        }

        return false;
    }

    public async Task Index(List<T> data, bool immediately = true, int retry = 0)
    {
        if (!await SupportAsync() || data.Count == 0)
        {
            return;
        }

        try
        {
            await _indexer.IndexAsync(data, immediately);
        }
        catch (OpenSearchClientException e)
        {
            Logger.ErrorIndex(e);

            if (e.Response != null)
            {
                Logger.Error(e.Response.HttpStatusCode.ToString());

                if (e.Response.HttpStatusCode is 413 or 403 or 408)
                {
                    foreach (var r in data.Where(r => r != null))
                    {
                        await Index(r, immediately);
                    }
                }
                else if (e.Response.HttpStatusCode is 429 or 502)
                {
                    await Task.Delay(60000);
                    if (retry < 10)
                    {
                        await Index(data.Where(r => r != null).ToList(), immediately, retry + 1);
                        return;
                    }

                    throw;
                }
            }
        }
        catch (AggregateException e) //OpenSearchClientException
        {
            if (e.InnerExceptions.Count == 0)
            {
                throw;
            }

            var inner = e.InnerExceptions.OfType<OpenSearchClientException>().FirstOrDefault();


            if (inner != null)
            {
                Logger.ErrorInner(inner);

                if (inner.Response.HttpStatusCode is 413 or 403)
                {
                    Logger.Error(inner.Response.HttpStatusCode.ToString());
                    foreach (var r in data.Where(r => r != null))
                    {
                        await Index(r, immediately);
                    }
                }
                else if (inner.Response.HttpStatusCode is 429 or 502)
                {
                    await Task.Delay(60000);
                    if (retry < 10)
                    {
                        await Index(data.Where(r => r != null).ToList(), immediately, retry + 1);
                        return;
                    }

                    throw;
                }
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<bool> UpdateAsync(T data, UpdateAction action, Expression<Func<T, IList>> field, bool immediately = true)
    {
        if (!await SupportAsync())
        {
            return false;
        }

        try
        {
            await _indexer.UpdateAsync(data, action, field, immediately);

            return true;
        }
        catch (Exception e)
        {
            Logger.ErrorUpdate(e);

            return false;
        }
    }


    public async Task UpdateAsync(T data, Expression<Func<Selector<T>, Selector<T>>> expression, bool immediately = true, params Expression<Func<T, object>>[] fields)
    {
        if (!await SupportAsync())
        {
            return;
        }

        try
        {
            var tenant = tenantManager.GetCurrentTenantId();
            await _indexer.Update(data, expression, tenant, immediately, fields);
        }
        catch (Exception e)
        {
            Logger.ErrorUpdate(e);
        }
    }


    public async Task IndexAsync(T data, bool immediately = true)
    {
        if (!await SupportAsync())
        {
            return;
        }

        try
        {
            await _indexer.IndexAsync(data, immediately);
        }
        catch (Exception e)
        {
            Logger.ErrorIndex(e);
        }
    }

    public async Task<bool> UpdateAsync(T data, bool immediately = true, params Expression<Func<T, object>>[] fields)
    {
        if (!await SupportAsync())
        {
            return false;
        }

        try
        {
            await _indexer.UpdateAsync(data, immediately, fields);

            return true;
        }
        catch (Exception e)
        {
            Logger.ErrorUpdate(e);

            return false;
        }
    }

    public async Task<bool> DeleteAsync(Expression<Func<Selector<T>, Selector<T>>> expression, bool immediately = true)
    {
        if (!await SupportAsync())
        {
            return false;
        }

        var tenant = tenantManager.GetCurrentTenantId();

        try
        {
            await _indexer.DeleteAsync(expression, tenant, immediately);

            return true;
        }
        catch (Exception e)
        {
            Logger.ErrorDelete(e);

            return false;
        }
    }

    public async Task DeleteAsync(int tenantId, bool immediately = true)
    {
        if (!await SupportAsync())
        {
            return;
        }

        try
        {
            await _indexer.DeleteAsync(r => r, tenantId, immediately);
        }
        catch (Exception e)
        {
            Logger.ErrorDelete(e);
        }
    }


    public async Task RefreshAsync()
    {
        if (!await SupportAsync())
        {
            return;
        }

        await _indexer.RefreshAsync();
    }

    public virtual Task IndexAllAsync()
    {
        return Task.CompletedTask;
    }

    public async Task ReIndexAsync()
    {
        await _indexer.ReIndexAsync();
    }

    public async Task<bool> SupportAsync()
    {
        return await factoryIndexer.CheckStateAsync();
    }
}

[Scope]
public class FactoryIndexer
{
    public ILogger Log { get; }

    private readonly ICache _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly FactoryIndexerHelper _factoryIndexerHelper;
    private readonly Client _client;
    private readonly CoreBaseSettings _coreBaseSettings;

    public FactoryIndexer(
        IServiceProvider serviceProvider,
        FactoryIndexerHelper factoryIndexerHelper,
        Client client,
        ILoggerFactory loggerFactory,
        CoreBaseSettings coreBaseSettings,
        ICache cache)
    {
        _cache = cache;
        _serviceProvider = serviceProvider;
        _factoryIndexerHelper = factoryIndexerHelper;
        _client = client;
        _coreBaseSettings = coreBaseSettings;

        try
        {
            Log = loggerFactory.CreateLogger("ASC.Indexer");
        }
        catch (Exception e)
        {
            Log.CriticalFactoryIndexer(e);
        }
    }

    public bool CheckState(bool cacheState = true)
    {
        const string key = "elasticsearch";

        if (cacheState)
        {
            var cacheValue = _cache.Get<string>(key);
            if (!string.IsNullOrEmpty(cacheValue))
            {
                return Convert.ToBoolean(cacheValue);
            }
        }

        var cacheTime = DateTime.UtcNow.AddMinutes(15);

        try
        {
            var isValid = _client.Ping();

            if (cacheState)
            {
                _cache.Insert(key, isValid.ToString(CultureInfo.InvariantCulture).ToLower(), cacheTime);
            }

            return isValid;
        }
        catch (Exception e)
        {
            if (cacheState)
            {
                _cache.Insert(key, "false", cacheTime);
            }

            Log.ErrorPingFalse(e);

            return false;
        }
    }

    public async ValueTask<bool> CheckStateAsync(bool cacheState = true)
    {
        const string key = "elasticsearch";

        if (cacheState)
        {
            var cacheValue = _cache.Get<string>(key);
            if (!string.IsNullOrEmpty(cacheValue))
            {
                return Convert.ToBoolean(cacheValue);
            }
        }

        var cacheTime = DateTime.UtcNow.AddMinutes(15);

        try
        {
            if (_client.Instance == null)
            {
                if (cacheState)
                {
                    _cache.Insert(key, "false", cacheTime);
                }

                Log.DebugCheckStatePing("Client instance is null");

                return false;
            }

            var result = await _client.Instance.PingAsync(new PingRequest());

            var isValid = result.IsValid;

            Log.DebugCheckStatePing(result.DebugInformation);

            if (cacheState)
            {
                _cache.Insert(key, isValid.ToString(CultureInfo.InvariantCulture).ToLower(), cacheTime);
            }

            return isValid;
        }
        catch (Exception e)
        {
            if (cacheState)
            {
                _cache.Insert(key, "false", cacheTime);
            }

            Log.ErrorPingFalse(e);

            return false;
        }
    }

    public object GetState(TenantUtil tenantUtil)
    {
        State state = null;
        IEnumerable<object> indices = null;
        Dictionary<string, long> count = null;

        if (!_coreBaseSettings.Standalone)
        {
            return new
            {
                state,
                indices,
                status = CheckState()
            };
        }

        state = new State
        {
            Indexing = _factoryIndexerHelper.Indexing,
            LastIndexed = _factoryIndexerHelper.LastIndexed != DateTime.MinValue ? _factoryIndexerHelper.LastIndexed : null
        };

        if (state.LastIndexed.HasValue)
        {
            state.LastIndexed = tenantUtil.DateTimeFromUtc(state.LastIndexed.Value);
        }

        indices = _client.Instance.Cat.Indices(new CatIndicesRequest { SortByColumns = ["index"] }).Records
            .Select(r => new
            {
                r.Index,
                Count = count.GetValueOrDefault(r.Index, 0),
                DocsCount = _client.Instance.Count(new CountRequest(r.Index)).Count,
                r.StoreSize
            })
            .Where(r => r.Count > 0);

        return new
        {
            state,
            indices,
            status = CheckState()
        };
    }

    public async Task ReindexAsync(string name)
    {
        if (!_coreBaseSettings.Standalone)
        {
            return;
        }

        var generic = typeof(BaseIndexer<>);
        var indexers = _serviceProvider.GetService<IEnumerable<IFactoryIndexer>>()
            .Where(r => string.IsNullOrEmpty(name) || r.IndexName == name)
            .Select(r => (IFactoryIndexer)Activator.CreateInstance(generic.MakeGenericType(r.GetType()), r));

        foreach (var indexer in indexers)
        {
            await indexer.ReIndexAsync();
        }
    }
}
