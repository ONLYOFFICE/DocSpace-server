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

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public abstract class DIAttribute : Attribute
{
    public abstract DIAttributeType DiAttributeType { get; }
    protected internal Type Implementation { get; }
    protected internal Type Service { get; }
    
    public Type[] GenericArguments { get; init; }

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

public class DIHelper
{
    private readonly Dictionary<DIAttributeType, List<string>> _services = new()
    {
        { DIAttributeType.Singleton, [] },
        { DIAttributeType.Scope, [] },
        { DIAttributeType.Transient, [] }
    };
    private readonly List<string> _added = [];
    private IServiceCollection _serviceCollection;

    readonly HashSet<string> _visited = [];
    
    public void Scan()
    {        
        AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
        {
            Scan(args.LoadedAssembly);
        };
        
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(r=> r.FullName))
        {
            Scan(assembly);
        }
    }
    
    private void Scan(Assembly assembly)
    {
        var assemblyName = assembly.GetName();
        if (!CheckAssemblyName(assemblyName) || _visited.Contains(assemblyName.FullName))
        {
            return;
        }
        
        _visited.Add(assembly.FullName);
        
        var types = assembly.GetTypes().Where(t => t.GetCustomAttributes<DIAttribute>().Any());
        
        foreach (var a in types)
        {
            TryAdd(a);
        }
        
        var references = assembly.GetReferencedAssemblies();
        foreach(var reference in references.Where(r => CheckAssemblyName(r) && !_visited.Contains(r.FullName)))
        {
            Assembly.Load(reference);
        }
    }

    private bool CheckAssemblyName(AssemblyName assembly)
    {
        var assemblyName = assembly.Name;
        return assemblyName != null && assemblyName.StartsWith("ASC.");
    }

    private void TryAdd(Type service, Type implementation = null, DIAttribute di = null)
    {
        Type serviceGenericTypeDefinition = null;
        
        if (service.IsGenericType)
        {
            serviceGenericTypeDefinition = service.GetGenericTypeDefinition();
        }

        if (serviceGenericTypeDefinition != null)
        {
            if (service.IsInterface && implementation == null &&
                (
                    serviceGenericTypeDefinition == typeof(IOptionsSnapshot<>) ||
                    serviceGenericTypeDefinition == typeof(IOptions<>) ||
                    serviceGenericTypeDefinition == typeof(IOptionsMonitor<>)
                ))
            {
                service = service.GetGenericArguments().FirstOrDefault();

                if (service == null)
                {
                    return;
                }
            }
            else if(service.IsGenericTypeDefinition)
            {
                var attributes = service.GetCustomAttributes<DIAttribute>();
                foreach (var attr in attributes)
                {
                    if (attr.GenericArguments == null || attr.GenericArguments.Length == 0)
                    {
                        continue;
                    }
                    
                    TryAdd(service.MakeGenericType(attr.GenericArguments), di: attr);
                }
                return;
            }
        }

        var serviceName = $"{service}{implementation}";

        if (_added.Contains(serviceName))
        {
            return;
        }

        _added.Add(serviceName);

        di ??= serviceGenericTypeDefinition != null && (
            serviceGenericTypeDefinition == typeof(IConfigureOptions<>) ||
            serviceGenericTypeDefinition == typeof(IPostConfigureOptions<>) ||
            serviceGenericTypeDefinition == typeof(IOptionsMonitor<>)
            ) && implementation != null ? 
            implementation.GetCustomAttributes<DIAttribute>().FirstOrDefault() : 
            service.GetCustomAttributes<DIAttribute>().FirstOrDefault();

            if (!service.IsInterface || implementation != null)
            {
                var isnew = implementation != null ? Register(di, service, implementation) : Register(di, service);
                if (!isnew)
                {
                    return;
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
                            Register(di, di.Service, service);
                            TryAdd(service);
                        }
                        else
                        {
                            Register(di, di.Service, di.Implementation);
                            Register(di, service);
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
                }
            }
        
    }

    public void Configure(IServiceCollection serviceCollection)
    {
        _serviceCollection = serviceCollection;
    }

    private bool Register(DIAttribute c, Type service, Type implementation = null)
    {
        if (service.IsSubclassOf(typeof(ControllerBase)) || service.GetInterfaces().Contains(typeof(IResourceFilter))
            || service.GetInterfaces().Contains(typeof(IDictionary<string, string>)))
        {
            return true;
        }

        var serviceName = $"{service}{implementation}";

        if (!_services[c.DiAttributeType].Contains(serviceName))
        {
            c.TryAdd(_serviceCollection, service, implementation);
            _services[c.DiAttributeType].Add(serviceName);

            return true;
        }

        return false;
    }
}