// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Core.Common.Configuration;

public class Consumer() : IDictionary<string, string>
{
    public bool CanSet { get; private set; }
    public int Order { get; private set; }
    public string Name { get; private set; }
    
    protected readonly Dictionary<string, string> _props = new();
    public IEnumerable<string> ManagedKeys => _props.Select(r => r.Key);

    protected readonly Dictionary<string, string> _additional = new();
    public virtual IEnumerable<string> AdditionalKeys => _additional.Select(r => r.Key);

    public ICollection<string> Keys => AllProps.Keys;
    public ICollection<string> Values => AllProps.Values;

    private Dictionary<string, string> AllProps
    {
        get
        {
            var result = _props.ToDictionary(item => item.Key, item => item.Value);

            foreach (var item in _additional.Where(item => !result.ContainsKey(item.Key)))
            {
                result.Add(item.Key, item.Value);
            }

            return result;
        }
    }

    private readonly bool _onlyDefault;

    protected internal readonly TenantManager TenantManager;
    protected internal readonly CoreBaseSettings CoreBaseSettings;
    protected internal readonly CoreSettings CoreSettings;
    protected internal readonly ConsumerFactory ConsumerFactory;
    protected internal readonly IConfiguration Configuration;
    protected internal readonly ICacheNotify<ConsumerCacheItem> Cache;

    public async Task<bool> GetIsSetAsync()
    {
        if (_props.Count == 0)
        {
            return false;
        }

        foreach (var r in _props)
        {
            if (string.IsNullOrEmpty(await GetAsync(r.Key)))
            {
                return false;
            }
        }
        
        return true;
    }

    public Consumer(
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory) : this()
    {
        TenantManager = tenantManager;
        CoreBaseSettings = coreBaseSettings;
        CoreSettings = coreSettings;
        Configuration = configuration;
        Cache = cache;
        ConsumerFactory = consumerFactory;
        _onlyDefault = configuration["core:default-consumers"] == "true";
        Name = "";
        Order = int.MaxValue;
    }

    public Consumer(
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        string name, int order, Dictionary<string, string> additional)
        : this(tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory)
    {
        Name = name;
        Order = order;
        _props = new Dictionary<string, string>();
        _additional = additional;
    }

    public Consumer(
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        string name, int order, Dictionary<string, string> props, Dictionary<string, string> additional = null)
        : this(tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory)
    {
        Name = name;
        Order = order;
        _props = props ?? new Dictionary<string, string>();
        _additional = additional ?? new Dictionary<string, string>();

        if (props is { Count: > 0 })
        {
            CanSet = props.All(r => string.IsNullOrEmpty(r.Value));
        }
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return AllProps.GetEnumerator();
    }
    

    public void Add(KeyValuePair<string, string> item) { }
    public void Clear()
    {
        ClearAsync().Wait();
    }

    public async Task ClearAsync()
    {
        if (!CanSet)
        {
            throw new NotSupportedException("Key for read only. Consumer " + Name);
        }

        foreach (var providerProp in _props)
        {
            await SetAsync(providerProp.Key,  null);
        }

        await Cache.PublishAsync(new ConsumerCacheItem { Name = this.Name }, CacheNotifyAction.Remove);
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
        return AllProps.Contains(item);
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) { }

    public bool Remove(KeyValuePair<string, string> item)
    {
        return AllProps.Remove(item.Key);
    }

    public int Count => AllProps.Count;

    public bool IsReadOnly => true;

    public bool ContainsKey(string key)
    {
        return AllProps.ContainsKey(key);
    }

    public void Add(string key, string value) { }

    public bool Remove(string key)
    {
        return false;
    }

    public bool TryGetValue(string key, out string value)
    {
        return AllProps.TryGetValue(key, out value);
    }

    public string this[string key]
    {
        get => GetAsync(key).Result;        //TODO
        set => SetAsync(key, value).Wait(); //TODO
    }

    public async Task<string> GetAsync(string name)
    {
        string value = null;

        if (!_onlyDefault && CanSet)
        {
            var tenant = CoreBaseSettings.Standalone
                             ? Tenant.DefaultTenant
                             : TenantManager.GetCurrentTenantId();

            value = await CoreSettings.GetSettingAsync(GetSettingsKey(name), tenant);
        }

        if (string.IsNullOrEmpty(value))
        {
            AllProps.TryGetValue(name, out value);
        }

        return value;
    }

    public async Task SetAsync(string name, string value)
    {
        if (!ManagedKeys.Contains(name))
        {
            _additional[name] = value;

            return;
        }

        if (!CanSet)
        {
            throw new NotSupportedException("Key for read only. Key " + name);
        }

        var tenant = CoreBaseSettings.Standalone
                         ? Tenant.DefaultTenant
                         : TenantManager.GetCurrentTenantId();
        await CoreSettings.SaveSettingAsync(GetSettingsKey(name), value, tenant);
    }

    protected virtual string GetSettingsKey(string name)
    {
        return "AuthKey_" + name;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class DataStoreConsumer : Consumer, ICloneable
{
    public Type HandlerType { get; private set; }
    public DataStoreConsumer Cdn { get; private set; }

    private const string HandlerTypeKey = "handlerType";
    private const string CdnKey = "cdn";

    public DataStoreConsumer()
    { }

    public DataStoreConsumer(
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory)
        : base(tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory)
    {

    }

    public DataStoreConsumer(
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        string name, int order, Dictionary<string, string> additional)
        : base(tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, additional)
    {
        InitAsync(additional).Wait();
    }

    public DataStoreConsumer(
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        string name, int order, Dictionary<string, string> props, Dictionary<string, string> additional)
        : base(tenantManager, coreBaseSettings, coreSettings, configuration, cache, consumerFactory, name, order, props, additional)
    {
        InitAsync(additional).Wait();
    }

    public override IEnumerable<string> AdditionalKeys => base.AdditionalKeys.Where(r => r != HandlerTypeKey && r != "cdn");

    protected override string GetSettingsKey(string name)
    {
        return base.GetSettingsKey(Name + name);
    }

    private async Task InitAsync(IReadOnlyDictionary<string, string> additional)
    {
        if (additional == null || !additional.TryGetValue(HandlerTypeKey, out var handler))
        {
            throw new ArgumentException(HandlerTypeKey);
        }

        HandlerType = Type.GetType(handler);

        if (additional.TryGetValue(CdnKey, out var value))
        {
            Cdn = await GetCdnAsync(value);
        }
    }

    private async Task<DataStoreConsumer> GetCdnAsync(string cdn)
    {
        var fromConfig = ConsumerFactory.GetByKey<Consumer>(cdn);
        if (string.IsNullOrEmpty(fromConfig.Name))
        {
            return null;
        }

        Dictionary<string, string> props = new ();
        foreach (var prop in ManagedKeys)
        {
            props.Add(prop, await GetAsync(prop));
        }
        
        Dictionary<string, string> additional = new();
        foreach (var prop in fromConfig.AdditionalKeys)
        {
            props.Add(prop, await fromConfig.GetAsync(prop));
        }
        additional.Add(HandlerTypeKey, HandlerType.AssemblyQualifiedName);

        return new DataStoreConsumer(fromConfig.TenantManager, fromConfig.CoreBaseSettings, fromConfig.CoreSettings, fromConfig.Configuration, fromConfig.Cache, fromConfig.ConsumerFactory, fromConfig.Name, fromConfig.Order, props, additional);
    }

    public object Clone()
    {
        return new DataStoreConsumer(TenantManager, CoreBaseSettings, CoreSettings, Configuration, Cache, ConsumerFactory, Name, Order, _props.ToDictionary(r => r.Key, r => r.Value), _additional.ToDictionary(r => r.Key, r => r.Value));
    }
}

[Scope]
public class ConsumerFactory : IDisposable
{
    public ILifetimeScope Builder { get; set; }

    public ConsumerFactory(IContainer builder)
    {
        Builder = builder.BeginLifetimeScope();
    }

    public ConsumerFactory(ILifetimeScope builder)
    {
        Builder = builder;
    }

    public Consumer GetByKey(string key)
    {
        if (Builder.TryResolveKeyed(key, typeof(Consumer), out var result))
        {
            return (Consumer)result;
        }

        return new Consumer();
    }

    public T GetByKey<T>(string key) where T : Consumer, new()
    {
        if (Builder.TryResolveKeyed(key, typeof(T), out var result))
        {
            return (T)result;
        }

        return new T();
    }

    public T Get<T>() where T : Consumer, new()
    {
        if (Builder.TryResolve(out T result))
        {
            return result;
        }

        return new T();
    }

    public IEnumerable<T> GetAll<T>() where T : Consumer, new()
    {
        return Builder.Resolve<IEnumerable<T>>();
    }

    public void Dispose()
    {
        Builder.Dispose();
    }
}
