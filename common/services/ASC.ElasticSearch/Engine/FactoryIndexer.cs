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

public abstract class FactoryIndexer<T>(ILoggerProvider options,
        TenantManager tenantManager,
        SearchSettingsHelper searchSettingsHelper,
        FactoryIndexer factoryIndexer,
        BaseIndexer<T> baseIndexer,
        IServiceProvider serviceProvider,
        ICache cache)
    : IFactoryIndexer
    where T : class, ISearchItem
{
    protected ILogger Logger { get; } = options.CreateLogger("ASC.Indexer");
    public string IndexName { get => _indexer.IndexName; }
    public virtual string SettingsTitle => string.Empty;

    protected readonly BaseIndexer<T> _indexer = baseIndexer;

    private static readonly TaskScheduler _scheduler = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, 10).ConcurrentScheduler;

    public async Task<(bool, IReadOnlyCollection<T>)> TrySelectAsync(Expression<Func<Selector<T>, Selector<T>>> expression)
    {
        IReadOnlyCollection<T> result;
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync(t) || !_indexer.CheckExist(t))
        {
            result = new List<T>();

            return (false, result);
        }

        try
        {
            result = await _indexer.SelectAsync(expression);
        }
        catch (Exception e)
        {
            Logger.ErrorSelect(e);
            result = new List<T>();

            return (false, result);
        }

        return (true, result);
    }

    public async Task<(bool, List<int>)> TrySelectIdsAsync(Expression<Func<Selector<T>, Selector<T>>> expression)
    {
        List<int> result;
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync(t) || !_indexer.CheckExist(t))
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

    public async Task<(bool, List<int>, long)> TrySelectIdsWithTotalAsync(Expression<Func<Selector<T>, Selector<T>>> expression)
    {
        List<int> result;
        long total;
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync(t) || !_indexer.CheckExist(t))
        {
            result = [];
            total = 0;

            return (false, result, total);
        }

        try
        {
            (var r, total) = _indexer.SelectWithTotal(expression, true);
            result = r.Select(entry => entry.Id).ToList();
        }
        catch (Exception e)
        {
            Logger.ErrorSelect(e);
            total = 0;
            result = [];

            return (false, result, total);
        }

        return (true, result, total);
    }

    public virtual async Task<bool> CanIndexByContentAsync(T t)
    {
        return await SupportAsync(t) && await searchSettingsHelper.CanIndexByContentAsync<T>();
    }

    public async Task<bool> Index(T data, bool immediately = true)
    {
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync(t))
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
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync(t) || data.Count == 0)
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
                else if (e.Response.HttpStatusCode == 429)
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
                else if (inner.Response.HttpStatusCode == 429)
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

    public void Update(T data, bool immediately = true, params Expression<Func<T, object>>[] fields)
    {
        var t = serviceProvider.GetService<T>();
        if (!Support(t))
        {
            return;
        }

        try
        {
            _indexer.Update(data, immediately, fields);
        }
        catch (Exception e)
        {
            Logger.ErrorUpdate(e);
        }
    }

    public void Update(T data, UpdateAction action, Expression<Func<T, IList>> field, bool immediately = true)
    {
        var t = serviceProvider.GetService<T>();
        if (!Support(t))
        {
            return;
        }

        try
        {
            _indexer.Update(data, action, field, immediately);
        }
        catch (Exception e)
        {
            Logger.ErrorUpdate(e);
        }
    }

    public Task<bool> UpdateAsync(T data, UpdateAction action, Expression<Func<T, IList>> field, bool immediately = true)
    {
        var t = serviceProvider.GetService<T>();

        return !Support(t)
            ? Task.FromResult(false)
            : QueueAsync(() => _indexer.Update(data, action, field, immediately));
    }


    public async Task UpdateAsync(T data, Expression<Func<Selector<T>, Selector<T>>> expression, bool immediately = true, params Expression<Func<T, object>>[] fields)
    {
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync(t))
        {
            return;
        }

        try
        {
            var tenant = tenantManager.GetCurrentTenantId();
            _indexer.Update(data, expression, tenant, immediately, fields);
        }
        catch (Exception e)
        {
            Logger.ErrorUpdate(e);
        }
    }

    public async Task UpdateAsync(T data, Expression<Func<Selector<T>, Selector<T>>> expression, UpdateAction action, Expression<Func<T, IList>> fields, bool immediately = true)
    {
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync(t))
        {
            return;
        }

        try
        {
            var tenant = tenantManager.GetCurrentTenantId();
            _indexer.Update(data, expression, tenant, action, fields, immediately);
        }
        catch (Exception e)
        {
            Logger.ErrorUpdate(e);
        }
    }

    public void Delete(T data, bool immediately = true)
    {
        var t = serviceProvider.GetService<T>();
        if (!Support(t))
        {
            return;
        }

        try
        {
            _indexer.Delete(data, immediately);
        }
        catch (Exception e)
        {
            Logger.ErrorDelete(e);
        }
    }

    public Task IndexAsync(T data, bool immediately = true)
    {
        var t = serviceProvider.GetService<T>();

        if (Support(t))
        {
            return QueueAsync(() => _indexer.IndexAsync(data, immediately));
        }

        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(T data, bool immediately = true, params Expression<Func<T, object>>[] fields)
    {
        var t = serviceProvider.GetService<T>();

        return !Support(t)
            ? Task.FromResult(false)
            : QueueAsync(() => _indexer.Update(data, immediately, fields));
    }

    public Task<bool> DeleteAsync(T data, bool immediately = true)
    {
        var t = serviceProvider.GetService<T>();

        return !Support(t)
            ? Task.FromResult(false)
            : QueueAsync(() => _indexer.Delete(data, immediately));
    }

    public async Task<bool> DeleteAsync(Expression<Func<Selector<T>, Selector<T>>> expression, bool immediately = true)
    {
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync(t))
        {
            return false;
        }

        var tenant = tenantManager.GetCurrentTenantId();

        return await QueueAsync(() => _indexer.Delete(expression, tenant, immediately));
    }

    public async Task DeleteAsync(int tenantId, bool immediately = true)
    {
        var t = serviceProvider.GetService<T>();
        if (!await SupportAsync(t))
        {
            return;
        }
        
        await QueueAsync(() => _indexer.Delete(r => r, tenantId, immediately));
    }

    public void Flush()
    {
        var t = serviceProvider.GetService<T>();
        if (!Support(t))
        {
            return;
        }

        _indexer.Flush();
    }

    public void Refresh()
    {
        var t = serviceProvider.GetService<T>();
        if (!Support(t))
        {
            return;
        }

        _indexer.Refresh();
    }

    public virtual Task IndexAllAsync()
    {
        return Task.CompletedTask;
    }

    public async Task ReIndexAsync()
    {
        await _indexer.ReIndexAsync();
    }

    public bool Support(T t)
    {
        if (!factoryIndexer.CheckState())
        {
            return false;
        }

        var cacheTime = DateTime.UtcNow.AddMinutes(15);
        var key = "elasticsearch " + t.IndexName;
        try
        {
            var cacheValue = cache.Get<string>(key);
            if (!string.IsNullOrEmpty(cacheValue))
            {
                return Convert.ToBoolean(cacheValue);
            }

            //TODO:
            //var service = new Service.Service();

            //var result = service.Support(t.IndexName);

            //Cache.Insert(key, result.ToString(CultureInfo.InvariantCulture).ToLower(), cacheTime);

            return true;
        }
        catch (Exception e)
        {
            cache.Insert(key, "false", cacheTime);
            Logger.ErrorFactoryIndexerCheckState(e);

            return false;
        }
    }

    public async Task<bool> SupportAsync(T t)
    {
        return await factoryIndexer.CheckStateAsync();
    }

    private Task<bool> QueueAsync(Action actionData)
    {
        var task = new Task<bool>(() =>
        {
            try
            {
                actionData();

                return true;
            }
            catch (AggregateException agg)
            {
                foreach (var e in agg.InnerExceptions)
                {
                    Logger.ErrorQueue(e);
                }

                throw;
            }
        }, TaskCreationOptions.LongRunning);

        task.ConfigureAwait(false);
        task.Start(_scheduler);

        return task;
    }

    private Task QueueAsync(Func<Task> actionData)
    {
        var task = new Task(async () =>
        {
            try
            {
                await actionData();
            }
            catch (Exception e)
            {
                Logger.ErrorQueue(e);
            }
        }, TaskCreationOptions.LongRunning);

        task.ConfigureAwait(false);
        task.Start(_scheduler);

        return task;
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
        ILoggerProvider options,
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
            Log = options.CreateLogger("ASC.Indexer");
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
