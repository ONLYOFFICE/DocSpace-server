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

using Microsoft.OpenApi.Models;

namespace ASC.Api.Core.Extensions;

public static class OpenApiExtension
{
    public static IServiceCollection AddWebOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddOpenApi("common", c =>
        {
            var fullName = Assembly.GetEntryAssembly()?.FullName;
            
            if (fullName != null)
            {
                var assemblyName = fullName.Split(',').First().Split(".").Last();
                c.AddDocumentTransformer((document, _, _) =>
                {
                    document.Info = new()
                    {
                        Title = assemblyName,
                        Version = "v2",
                    };
                    return Task.CompletedTask;
                });
            }

            c.CreateSchemaReferenceId = NestedSchemaReferenceId.Fun;

            c.AddOperationTransformer<OpenApiContentTypesTransformer>();
            c.AddSchemaTransformer<OpenApiDescriptionSchemaTransformer>();
            c.AddOperationTransformer<OpenApiDescriptionSchemaTransformer>();
            c.AddDocumentTransformer<LowercaseDocumentTransformer>();
            c.AddDocumentTransformer((document, _, _) =>
            {
                document.Paths.Remove("/api/2.0/capabilities.json");
                return Task.CompletedTask;
            });
            c.AddDocumentTransformer<TagDescriptionsDocumentFilter>();
            c.AddDocumentTransformer<OpenApiResponseDescriptionTransformer>();
            c.AddOperationTransformer<OpenApiCustomOperationTransformer>();
            c.AddOperationTransformer<OpenApiOperationIdTransformer>();
            var serverUrls = configuration.GetSection("openApi:servers").Get<List<string>>() ?? [];
            var serverDescription = configuration.GetSection("openApi:serversDescription").Get<List<string>>() ?? [];

            c.AddDocumentTransformer((document, _, _) =>
            {
                for(var i = 0; i < serverUrls.Count; i++)
                {
                    document.Servers.Add(new OpenApiServer
                    {
                        Url = serverUrls[i],
                        Description = serverDescription.Count > i ? serverDescription[i] : null,
                    });
                }
                return Task.CompletedTask;
            });
            c.AddDocumentTransformer<SecurityDocumentTransformer>();

        });
    }

    public static IApplicationBuilder UseOpenApi(this IApplicationBuilder app)
    {
        var fullName = Assembly.GetEntryAssembly()?.FullName;
        if (fullName != null)
        {
            var assemblyName = fullName.Split(',').First();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapOpenApi($"openapi/{assemblyName.ToLower()}/{{documentName}}.json");
            });
        }

        return app;
    }

    public static IApplicationBuilder UseOpenApiUI(this IApplicationBuilder app)
    {
        var fullName = Assembly.GetEntryAssembly()?.FullName;
        if (fullName != null)
        {
            var assemblyName = fullName.Split(',').First();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapScalarApiReference($"scalar/{assemblyName.ToLower()}", options =>
                {
                    options
                        .WithTheme(ScalarTheme.Purple)
                        .WithTitle($"{assemblyName} API Documentation")
                        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                        .WithOpenApiRoutePattern($"openapi/{assemblyName.ToLower()}/{{documentName}}.json");
                });
            });
        }

        return app;
    }

}

public class SecurityDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var requirements = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["asc_auth_key"] = new()
            {
                Scheme = "asc_auth_key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Name = CookiesManager.AuthCookiesName
            }
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = requirements;

        foreach (var path in document.Paths) 
        {
            foreach(var operation in path.Value.Operations)
            {
                var tags = context.DescriptionGroups.SelectMany(r => r.Items).FirstOrDefault(r => r.HttpMethod != null && r.HttpMethod.Equals(operation.Key.ToString().ToUpper()) && ("/" + r.RelativePath).Equals(path.Key));
                if(tags == null)
                {
                    continue;
                }
                var allowAnonymous = tags.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().FirstOrDefault();
                if (allowAnonymous != null) 
                {
                    continue;
                }

                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "asc_auth_key", Type = ReferenceType.SecurityScheme } }] = new List<string> { "read", "write" }
                });
            }
        }
        return Task.CompletedTask;
    }
}

public static class NestedSchemaReferenceId
{
    private static readonly HashSet<string> _generatedSchemas = [];
    public static string Fun(JsonTypeInfo info)
    {
        var type = Nullable.GetUnderlyingType(info.Type) ?? info.Type;

        if (_primitiveTypes.Contains(type) || IsEnumerableOfPrimitive(type) || (type == typeof(object) && !type.IsGenericType))
        {
            return null;
        }

        if (info is { Kind: JsonTypeInfoKind.Enumerable } || type.IsArray)
        {
            return null;
        }

        if (info is { Kind: JsonTypeInfoKind.Dictionary })
        {
            return null;
        }

        var name = CustomSchemaId(type);

        if(name == "ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto")
        {
            return null;
        }

        if (!_generatedSchemas.Contains(name)) 
        { 
            _generatedSchemas.Add(name);
            return name;
        }
        return null;
    }

    private static bool IsEnumerableOfPrimitive(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.IsArray;
        }

        if (typeof(IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
            typeof(IAsyncEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
            typeof(ICollection<>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
            typeof(List<>).IsAssignableFrom(type.GetGenericTypeDefinition()) ||
            typeof(IDictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition()))
        {
            return true;
        }

        return type.IsArray;
    }
    private static string CustomSchemaId(Type type)
    {
        var name = type.FullName;
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }
        if (type.IsGenericType)
        {
            name = $"{name.Split('`')[0]}.{string.Join(".", type.GenericTypeArguments.Select(CustomSchemaId))}";
        }
        if (_primitiveTypes.Contains(type)) 
        {
            return type.Name;
        }

        name = name.Replace("+", "_");
        return name;
    }

    private static readonly List<Type> _primitiveTypes =
    [
        typeof(bool),
        typeof(byte),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(short),
        typeof(ushort),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(char),
        typeof(Uri),
        typeof(string),
        typeof(IFormFile),
        typeof(IFormFileCollection),
        typeof(PipeReader),
        typeof(Stream),
        typeof(JsonElement)
    ];
}