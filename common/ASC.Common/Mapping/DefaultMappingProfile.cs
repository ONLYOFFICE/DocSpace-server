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

namespace ASC.Common.Mapping;

public class DefaultMappingProfile : Profile
{
    public DefaultMappingProfile()
    {
        Array.ForEach(AppDomain.CurrentDomain.GetAssemblies(), ApplyMappingsFromAssembly);
        ApplyPrimitiveMappers();
    }

    private void ApplyMappingsFromAssembly(Assembly assembly)
    {
        if (!assembly.GetName().Name.StartsWith("ASC."))
        {
            return;
        }

        var mapFromType = typeof(IMapFrom<>);

        var mappingMethodName = nameof(IMapFrom<object>.Mapping);

        var types = assembly.GetExportedTypes().Where(t => t.GetInterfaces().Any(r => HasInterface(r, mapFromType))).ToList();

        var argumentTypes = new[] { typeof(Profile) };

        foreach (var type in types)
        {
            if (type.ContainsGenericParameters)
            {
                throw new Exception("Denied for using type with generic parameters.");
            }

            var instance = Activator.CreateInstance(type);

            var methodInfo = type.GetMethod(mappingMethodName);

            if (methodInfo != null)
            {
                methodInfo.Invoke(instance, [this]);
            }
            else
            {
                var interfaces = type.GetInterfaces().Where(r => HasInterface(r, mapFromType)).ToList();

                if (interfaces.Count <= 0)
                {
                    continue;
                }

                foreach (var interfaceMethodInfo in interfaces.Select(@interface => @interface.GetMethod(mappingMethodName, argumentTypes)))
                {
                    interfaceMethodInfo?.Invoke(instance, [this]);
                }
            }
        }
    }

    private void ApplyPrimitiveMappers()
    {
        CreateMap<long, DateTime>().ReverseMap()
            .ConvertUsing<TimeConverter>();
    }
    
    internal static bool HasInterface(Type t, Type mapFromType) => t.IsGenericType && t.GetGenericTypeDefinition() == mapFromType;
}

public class WarmupMappingStartupTask(ILogger<WarmupMappingStartupTask> logger, IMapper mapper) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x =>
        {
            var name = x.GetName().Name;
            return !string.IsNullOrEmpty(name) && name.StartsWith("ASC.");
        });
        
        var mapFromType = typeof(IMapFrom<>);
        
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetExportedTypes().Where(t => t.GetInterfaces().Any(r => DefaultMappingProfile.HasInterface(r, mapFromType))).ToList();
        
            foreach (var type in types)
            {
                if (type.ContainsGenericParameters)
                {
                    throw new Exception("Denied for using type with generic parameters.");
                }
        
                var instance = Activator.CreateInstance(type);
                
                var interfaces = type.GetInterfaces().Where(r => DefaultMappingProfile.HasInterface(r, mapFromType)).ToList();
                
                foreach (var i in interfaces)
                {
                    var mapSource = i.GetGenericArguments().FirstOrDefault();
    
                    if (mapSource != null)
                    {
                        try
                        {
                            mapper.Map(Activator.CreateInstance(mapSource), instance);
                        }
                        catch (Exception e)
                        {
                            logger.LogTrace(e, mapSource.FullName);
                        }
                    }
                }
            }
        }
        var configuration = new MapperConfiguration(_ => {});
        configuration.CompileMappings();
        
        return Task.CompletedTask;
    }
}