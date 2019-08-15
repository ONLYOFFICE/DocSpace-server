/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ASC.Common.Caching;
using ASC.Common.Utils;
using ASC.Core.Tenants;
using Autofac;

namespace ASC.Core.Common.Configuration
{
    public class Consumer : IDictionary<string, string>
    {
        private static readonly ICacheNotify<ConsumerCacheItem> Cache = new KafkaCache<ConsumerCacheItem>();

        public bool CanSet { get; private set; }

        public int Order { get; private set; }

        public string Name { get; private set; }

        protected readonly Dictionary<string, string> Props;
        public IEnumerable<string> ManagedKeys
        {
            get { return Props.Select(r => r.Key).ToList(); }
        }

        protected readonly Dictionary<string, string> Additional;
        public virtual IEnumerable<string> AdditionalKeys
        {
            get { return Additional.Select(r => r.Key).ToList(); }
        }

        public ICollection<string> Keys { get { return AllProps.Keys; } }
        public ICollection<string> Values { get { return AllProps.Values; } }

        private Dictionary<string, string> AllProps
        {
            get
            {
                var result = Props.ToDictionary(item => item.Key, item => item.Value);

                foreach (var item in Additional.Where(item => !result.ContainsKey(item.Key)))
                {
                    result.Add(item.Key, item.Value);
                }

                return result;
            }
        }

        private static readonly bool OnlyDefault;

        public bool IsSet
        {
            get { return Props.Any() && !Props.All(r => string.IsNullOrEmpty(this[r.Key])); }
        }

        static Consumer()
        {
            OnlyDefault = ConfigurationManager.AppSettings["core:default-consumers"] == "true";
        }

        public Consumer()
        {
            Name = "";
            Order = int.MaxValue;
            Props = new Dictionary<string, string>();
            Additional = new Dictionary<string, string>();
        }

        public Consumer(string name, int order, Dictionary<string, string> additional)
        {
            Name = name;
            Order = order;
            Props = new Dictionary<string, string>();
            Additional = additional;
        }

        public Consumer(string name, int order, Dictionary<string, string> props, Dictionary<string, string> additional)
        {
            Name = name;
            Order = order;
            Props = props ?? new Dictionary<string, string>();
            Additional = additional ?? new Dictionary<string, string>();

            if (props != null && props.Any())
            {
                CanSet = props.All(r => string.IsNullOrEmpty(r.Value));
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return AllProps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
        {
        }

        public void Clear()
        {
            if (!CanSet)
            {
                throw new NotSupportedException("Key for read only.");
            }

            foreach (var providerProp in Props)
            {
                this[providerProp.Key] = null;
            }

            Cache.Publish(new ConsumerCacheItem() { Name = this.Name }, CacheNotifyAction.Remove);
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return AllProps.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return AllProps.Remove(item.Key);
        }

        public int Count { get { return AllProps.Count; } }

        public bool IsReadOnly { get { return true; } }

        public bool ContainsKey(string key)
        {
            return AllProps.ContainsKey(key);
        }

        public void Add(string key, string value)
        {
        }

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
            get { return Get(key); }
            set { Set(key, value); }
        }

        private string Get(string name)
        {
            string value = null;

            if (!OnlyDefault && CanSet)
            {
                var tenant = CoreContext.Configuration.Standalone
                                 ? Tenant.DEFAULT_TENANT
                                 : CoreContext.TenantManager.GetCurrentTenant().TenantId;

                value = CoreContext.Configuration.GetSetting(GetSettingsKey(name), tenant);
            }

            if (string.IsNullOrEmpty(value))
            {
                if (AllProps.ContainsKey(name))
                {
                    value = AllProps[name];
                }
            }

            return value;
        }

        private void Set(string name, string value)
        {
            if (!CanSet)
            {
                throw new NotSupportedException("Key for read only.");
            }

            if (!ManagedKeys.Contains(name))
            {
                if (Additional.ContainsKey(name))
                {
                    Additional[name] = value;
                }
                else
                {
                    Additional.Add(name, value);
                }
                return;
            }

            var tenant = CoreContext.Configuration.Standalone
                             ? Tenant.DEFAULT_TENANT
                             : CoreContext.TenantManager.GetCurrentTenant().TenantId;
            CoreContext.Configuration.SaveSetting(GetSettingsKey(name), value, tenant);
        }

        protected virtual string GetSettingsKey(string name)
        {
            return "AuthKey_" + name;
        }
    }

    public class DataStoreConsumer : Consumer, ICloneable
    {
        public Type HandlerType { get; private set; }
        public DataStoreConsumer Cdn { get; private set; }

        public const string HandlerTypeKey = "handlerType";
        public const string CdnKey = "cdn";

        public DataStoreConsumer()
        {

        }

        public DataStoreConsumer(string name, int order, Dictionary<string, string> additional)
            : base(name, order, additional)
        {
            Init(additional);
        }

        public DataStoreConsumer(string name, int order, Dictionary<string, string> props, Dictionary<string, string> additional)
            : base(name, order, props, additional)
        {
            Init(additional);
        }

        public override IEnumerable<string> AdditionalKeys
        {
            get { return base.AdditionalKeys.Where(r => r != HandlerTypeKey && r != "cdn").ToList(); }
        }

        protected override string GetSettingsKey(string name)
        {
            return base.GetSettingsKey(Name + name);
        }

        private void Init(IReadOnlyDictionary<string, string> additional)
        {
            if (additional == null || !additional.ContainsKey(HandlerTypeKey))
                throw new ArgumentException(HandlerTypeKey);

            HandlerType = Type.GetType(additional[HandlerTypeKey]);

            if (additional.ContainsKey(CdnKey))
            {
                Cdn = GetCdn(additional[CdnKey]);
            }
        }

        private DataStoreConsumer GetCdn(string cdn)
        {
            var fromConfig = ConsumerFactory.GetByName<Consumer>(cdn);

            var props = ManagedKeys.ToDictionary(prop => prop, prop => this[prop]);
            var additional = fromConfig.AdditionalKeys.ToDictionary(prop => prop, prop => fromConfig[prop]);
            additional.Add(HandlerTypeKey, HandlerType.AssemblyQualifiedName);

            return new DataStoreConsumer(fromConfig.Name, fromConfig.Order, props, additional);
        }

        public object Clone()
        {
            return new DataStoreConsumer(Name, Order, Props.ToDictionary(r => r.Key, r => r.Value), Additional.ToDictionary(r => r.Key, r => r.Value));
        }
    }

    public class ConsumerFactory
    {
        public static IEnumerable<Consumer> Consumers { get; private set; }

        private static IContainer Builder { get; set; }

        static ConsumerFactory()
        {
            var container = ConsumerConfigLoader.LoadConsumers("consumers");
            Builder = container.Build();
            Consumers = Builder.Resolve<IEnumerable<Consumer>>();
        }

        public static Consumer GetByName(string name)
        {
            object result;
            if (Builder.TryResolveNamed(name, typeof(Consumer), out result))
            {
                return (Consumer)result;
            }

            return new Consumer();
        }

        public static T GetByName<T>(string name) where T : Consumer, new()
        {
            object result;
            if (Builder.TryResolveNamed(name, typeof(T), out result))
            {
                return (T)result;
            }

            return new T();
        }

        public static T Get<T>() where T : Consumer, new()
        {
            T result;
            if (Builder.TryResolve(out result))
            {
                return result;
            }

            return new T();
        }

        public static IEnumerable<T> GetAll<T>() where T : Consumer, new()
        {
            return Builder.Resolve<IEnumerable<T>>();
        }
    }
}
