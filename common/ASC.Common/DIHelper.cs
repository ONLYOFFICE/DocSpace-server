// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Common;

public enum DIAttributeType
{
    Singleton,
    Scope,
    Transient
}

public class TransientAttribute : DIAttribute
{
    public override DIAttributeType DiAttributeType => DIAttributeType.Transient;

    public TransientAttribute() { }

    public TransientAttribute(Type service) : base(service) { }

    public TransientAttribute(Type service, Type implementation) : base(service, implementation) { }

    public override void TryAdd(IServiceCollection services, Type service, Type implementation = null)
    {
        if (implementation != null)
        {
            services.AddTransient(service, implementation);
        }
        else
        {
            services.AddTransient(service);
        }
    }
}

public class ScopeAttribute : DIAttribute
{
    public override DIAttributeType DiAttributeType => DIAttributeType.Scope;

    public ScopeAttribute() { }

    public ScopeAttribute(Type service) : base(service) { }

    public ScopeAttribute(Type service, Type implementation) : base(service, implementation) { }

    public override void TryAdd(IServiceCollection services, Type service, Type implementation = null)
    {
        if (implementation != null)
        {
            services.AddScoped(service, implementation);
        }
        else
        {
            services.AddScoped(service);
        }
    }
}

public class SingletonAttribute : DIAttribute
{
    public override DIAttributeType DiAttributeType => DIAttributeType.Singleton;

    public SingletonAttribute() { }

    public SingletonAttribute(Type service) : base(service) { }

    public SingletonAttribute(Type service, Type implementation) : base(service, implementation) { }

    public override void TryAdd(IServiceCollection services, Type service, Type implementation = null)
    {
        if (implementation != null)
        {
            services.AddSingleton(service, implementation);
        }
        else
        {
            services.AddSingleton(service);
        }
    }
}

public abstract class DIAttribute : Attribute
{
    public abstract DIAttributeType DiAttributeType { get; }
    protected internal Type Implementation { get; }
    protected internal Type Service { get; }
    public Type Additional { get; init; }

    protected DIAttribute() { }

    protected DIAttribute(Type service)
    {
        Service = service;
    }

    protected DIAttribute(Type service, Type implementation)
    {
        Implementation = implementation;
        Service = service;
    }

    public abstract void TryAdd(IServiceCollection services, Type service, Type implementation = null);
}

public class DIHelper()
{
    private readonly Dictionary<DIAttributeType, List<string>> _services = new()
    {
        { DIAttributeType.Singleton, [] },
        { DIAttributeType.Scope, [] },
        { DIAttributeType.Transient, [] }
    };
    private readonly List<string> _added = [];
    private readonly List<string> _configured = [];
    public IServiceCollection ServiceCollection { get; private set; }

    public DIHelper(IServiceCollection serviceCollection) : this()
    {
        ServiceCollection = serviceCollection;
    }

    public void AddControllers()
    {
        foreach (var a in Assembly.GetEntryAssembly().GetTypes().Where(r => r.IsAssignableTo<ControllerBase>() && !r.IsAbstract))
        {
            _ = TryAdd(a);
        }
    }

    public bool TryAdd<TService>() where TService : class
    {
        return TryAdd(typeof(TService));
    }

    public bool TryAdd<TService, TImplementation>() where TService : class
    {
        return TryAdd(typeof(TService), typeof(TImplementation));
    }

    public bool TryAdd(Type service, Type implementation = null)
    {
        Type serviceGenericTypeDefinition = null;

        if (service.IsGenericType)
        {
            serviceGenericTypeDefinition = service.GetGenericTypeDefinition();
        }

        if (service.IsInterface && serviceGenericTypeDefinition != null && implementation == null &&
            (
            serviceGenericTypeDefinition == typeof(IOptionsSnapshot<>) ||
            serviceGenericTypeDefinition == typeof(IOptions<>) ||
            serviceGenericTypeDefinition == typeof(IOptionsMonitor<>)
            ))
        {
            service = service.GetGenericArguments().FirstOrDefault();

            if (service == null)
            {
                return false;
            }
        }

        var serviceName = $"{service}{implementation}";

        if (_added.Contains(serviceName))
        {
            return false;
        }

        _added.Add(serviceName);

        var di = serviceGenericTypeDefinition != null && (
            serviceGenericTypeDefinition == typeof(IConfigureOptions<>) ||
            serviceGenericTypeDefinition == typeof(IPostConfigureOptions<>) ||
            serviceGenericTypeDefinition == typeof(IOptionsMonitor<>)
            ) && implementation != null ? implementation.GetCustomAttribute<DIAttribute>() : service.GetCustomAttribute<DIAttribute>();

        var isnew = false;

        if (di != null)
        {
            if (di.Additional != null)
            {
                var m = di.Additional.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
                m.Invoke(null, [this]);
            }

            if (!service.IsInterface || implementation != null)
            {
                isnew = implementation != null ? Register(service, implementation) : Register(service);
                if (!isnew)
                {
                    return false;
                }
            }

            if (service.IsInterface && implementation == null || !service.IsInterface)
            {
                if (di.Service != null)
                {
                    var a = di.Service.GetInterfaces().FirstOrDefault(x =>
                    {
                        Type xGenericTypeDefinition = null;

                        if (x.IsGenericType)
                        {
                            xGenericTypeDefinition = x.GetGenericTypeDefinition();
                        }

                        return
                        xGenericTypeDefinition != null && (
                        xGenericTypeDefinition == typeof(IConfigureOptions<>) ||
                        xGenericTypeDefinition == typeof(IPostConfigureOptions<>) ||
                        xGenericTypeDefinition == typeof(IOptionsMonitor<>));
                    });

                    if (a != null)
                    {
                        if (!a.ContainsGenericParameters)
                        {
                            var b = a.GetGenericArguments();

                            foreach (var g in b)
                            {
                                if (g != service)
                                {
                                    TryAdd(g);

                                    if (service.IsInterface && di.Implementation == null)
                                    {
                                        TryAdd(service, g);
                                    }
                                }
                            }

                            TryAdd(a, di.Service);
                        }
                        else
                        {
                            Type c;
                            var a1 = a.GetGenericTypeDefinition();
                            var b = a.GetGenericArguments().FirstOrDefault();

                            if (b is { IsGenericType: true })
                            {
                                var b1 = b.GetGenericTypeDefinition().MakeGenericType(service.GetGenericArguments());

                                TryAdd(b1);
                                c = a1.MakeGenericType(b1);
                            }
                            else
                            {
                                c = a1.MakeGenericType(service.GetGenericArguments());
                            }

                            TryAdd(c, di.Service.MakeGenericType(service.GetGenericArguments()));
                            //a, di.Service
                        }
                    }
                    else
                    {
                        if (di.Implementation == null)
                        {
                            isnew = Register(service, di.Service);
                            TryAdd(di.Service);
                        }
                        else
                        {
                            Register(di.Service);
                        }
                    }
                }

                if (di.Implementation != null)
                {
                    var a = di.Implementation.GetInterfaces().FirstOrDefault(x =>
                    {

                        Type xGenericTypeDefinition = null;

                        if (x.IsGenericType)
                        {
                            xGenericTypeDefinition = x.GetGenericTypeDefinition();
                        }

                        return
                        xGenericTypeDefinition != null &&
                        (
                        xGenericTypeDefinition == typeof(IConfigureOptions<>) ||
                        xGenericTypeDefinition == typeof(IPostConfigureOptions<>) ||
                        xGenericTypeDefinition == typeof(IOptionsMonitor<>));
                    });

                    if (a != null)
                    {
                        if (!a.ContainsGenericParameters)
                        {
                            var b = a.GetGenericArguments();

                            foreach (var g in b)
                            {
                                if (g != service)
                                {
                                    //TryAdd(g);
                                    if (service.IsInterface && implementation == null)
                                    {
                                        TryAdd(service, g);
                                    }
                                }
                            }

                            TryAdd(a, di.Implementation);
                        }
                        else
                        {
                            Type c;
                            var a1 = a.GetGenericTypeDefinition();
                            var b = a.GetGenericArguments().FirstOrDefault();

                            if (b is { IsGenericType: true })
                            {
                                var b1 = b.GetGenericTypeDefinition().MakeGenericType(service.GetGenericArguments());

                                TryAdd(b1);
                                c = a1.MakeGenericType(b1);
                            }
                            else
                            {
                                c = a1.MakeGenericType(service.GetGenericArguments());
                            }

                            TryAdd(c, di.Implementation.MakeGenericType(service.GetGenericArguments()));
                            //a, di.Service
                        }
                    }

                    else
                    {
                        isnew = TryAdd(service, di.Implementation);
                    }
                }
            }
        }

        if (isnew)
        {
            ConstructorInfo[] props = null;

            if (!service.IsInterface)
            {
                props = service.GetConstructors();
            }
            else if (implementation != null)
            {
                props = implementation.GetConstructors();
            }
            else if (di.Service != null)
            {
                props = di.Service.GetConstructors();
            }

            if (props != null)
            {
                var par = props.SelectMany(r => r.GetParameters()).Distinct();

                foreach (var p1 in par)
                {
                    TryAdd(p1.ParameterType);
                }
            }
        }

        return isnew;
    }

    public DIHelper TryAddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class
    {
        var serviceName = $"{typeof(TService)}";

        if (!_services[DIAttributeType.Singleton].Contains(serviceName))
        {
            _services[DIAttributeType.Singleton].Add(serviceName);
            ServiceCollection.TryAddSingleton(implementationFactory);
        }

        return this;
    }

    public void Configure(IServiceCollection serviceCollection)
    {
        ServiceCollection = serviceCollection;
    }

    public DIHelper Configure<TOptions>(string name, Action<TOptions> configureOptions) where TOptions : class
    {
        var serviceName = $"{typeof(TOptions)}{name}";

        if (!_configured.Contains(serviceName))
        {
            _configured.Add(serviceName);
            ServiceCollection.Configure(name, configureOptions);
        }

        return this;
    }

    private bool Register(Type service, Type implementation = null)
    {
        if (service.IsSubclassOf(typeof(ControllerBase)) || service.GetInterfaces().Contains(typeof(IResourceFilter))
            || service.GetInterfaces().Contains(typeof(IDictionary<string, string>)))
        {
            return true;
        }

        Type serviceGenericTypeDefinition = null;

        if (service.IsGenericType)
        {
            serviceGenericTypeDefinition = service.GetGenericTypeDefinition();
        }

        var c = serviceGenericTypeDefinition != null && (
            serviceGenericTypeDefinition == typeof(IConfigureOptions<>) ||
            serviceGenericTypeDefinition == typeof(IPostConfigureOptions<>) ||
            serviceGenericTypeDefinition == typeof(IOptionsMonitor<>)
            ) && implementation != null ? implementation.GetCustomAttribute<DIAttribute>() : service.GetCustomAttribute<DIAttribute>();

        var serviceName = $"{service}{implementation}";

        if (!_services[c.DiAttributeType].Contains(serviceName))
        {
            c.TryAdd(ServiceCollection, service, implementation);
            _services[c.DiAttributeType].Add(serviceName);

            return true;
        }

        return false;
    }
}