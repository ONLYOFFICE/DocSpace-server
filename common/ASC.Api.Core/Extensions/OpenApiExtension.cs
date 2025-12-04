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

using System.Xml.XPath;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;

public static class OpenApiExtension
{
    public static IServiceCollection AddOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddSwaggerGen(c =>
        {
            var assemblyName = Assembly.GetEntryAssembly().FullName.Split(',').First();
            c.ResolveConflictingActions(a => a.First());
            c.CustomOperationIds(r =>
            {
                return r.ActionDescriptor.RouteValues.TryGetValue("action", out var actionName)
                    ? char.ToLower(actionName[0]) + actionName.Substring(1)
                    : string.Empty;
            });

            c.CustomSchemaIds(CustomSchemaId);

            c.SwaggerDoc("common", new OpenApiInfo
            {
                Title = "Api",
                Version = "3.6.0",
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@onlyoffice.com",
                    Url = new Uri("https://helpdesk.onlyoffice.com/hc/en-us")
                }
            });
            c.SchemaFilter<SwaggerSchemaCustomFilter>();
            c.DocumentFilter<LowercaseDocumentFilter>();
            c.SchemaFilter<DerivedSchemaFilter>();
            c.DocumentFilter<HideRouteDocumentFilter>("/api/2.0/capabilities.json");
            c.OperationFilter<SwaggerOperationIdFilter>("api/2.0/files/recent", "getFolderRecent");
            c.DocumentFilter<TagDescriptionsDocumentFilter>();
            c.OperationFilter<SwaggerCustomOperationFilter>();
            c.OperationFilter<ContentTypeOperationFilter>();
            c.DocumentFilter<SwaggerSuccessApiResponseFilter>();
            c.EnableAnnotations();
            c.SchemaFilter<CustomInheritanceSchemaFilter>();

            var serverTemplate = configuration.GetValue<string>("openApi:server") ?? "";

            var defaultUrl = configuration.GetValue<string>("openApi:url:default") ?? "";
            var urlDescription = configuration.GetValue<string>("openApi:url:description") ?? "";

            c.AddServer(new OpenApiServer
            {
                Url = serverTemplate,
                Description = "Server configuration",
                Variables = new Dictionary<string, OpenApiServerVariable>
                {
                    ["baseUrl"] = new OpenApiServerVariable
                    {
                        Default = defaultUrl,
                        Description = urlDescription
                    }
                }
            });

            // ToDo: add security definitions
            c.AddSecurityDefinition(CookiesManager.AuthCookiesName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Name = CookiesManager.AuthCookiesName,
                Description = "Use Cookie authentication"
            });

            // Basic Authentication
            c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                In = ParameterLocation.Header,
                Scheme = "basic",
                Description = "Enter your username and password"
            });

            // JWT Authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                In = ParameterLocation.Header,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter 'Bearer {JWT Token}'"
            });

            // API Key Authentication
            c.AddSecurityDefinition("ApiKeyBearer", new OpenApiSecurityScheme
            {
                Name = "ApiKeyBearer",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Authentication is determined by the 'Authorization' header"
            });

            var authorizationUrl = configuration.GetValue<string>("openApi:oauth2:authorizationUrl");
            var tokenUrl = configuration.GetValue<string>("openApi:oauth2:tokenUrl");
            // OAuth2
            c.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                In = ParameterLocation.Header,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = string.IsNullOrEmpty(authorizationUrl) ? new Uri(string.Empty, UriKind.RelativeOrAbsolute) : new Uri(authorizationUrl),
                        TokenUrl = string.IsNullOrEmpty(tokenUrl) ? new Uri(string.Empty, UriKind.RelativeOrAbsolute) : new Uri(tokenUrl),
                        Scopes = new Dictionary<string, string>
                        {
                            { "read", "Read access to protected resources" },
                            { "write", "Write access to protected resources" }
                        }
                    }
                },
                Description = "OAuth2 flow with Authorization Code"
            });

            var openIdConnectUrl = configuration.GetValue<string>("openApi:openId:openIdConnectUrl");
            // OpenId Connect
            c.AddSecurityDefinition("OpenId", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OpenIdConnect,
                In = ParameterLocation.Header,
                OpenIdConnectUrl = string.IsNullOrEmpty(openIdConnectUrl) ? new Uri(string.Empty, UriKind.RelativeOrAbsolute) : new Uri(openIdConnectUrl),
                Description = "OpenID Connect authentication"
            });

            var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
            if (File.Exists(xmlPath))
            {
                var doc = new XPathDocument(xmlPath);

                c.IncludeXmlComments(() => doc);
                c.OperationFilter<XmlCustomTagFilter>(doc);

                c.OperationFilter<AllowAnonymousFilter>();
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var xmlFileName = $"{assembly.GetName().Name}.xml";
                var xmlPathOther = Path.Combine(AppContext.BaseDirectory, xmlFileName);

                if (File.Exists(xmlPathOther) && xmlPathOther != xmlPath)
                {
                    c.IncludeXmlComments(xmlPathOther);
                }
            }
        });
    }

    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseOpenApi()
        {
            var assemblyName = Assembly.GetEntryAssembly().FullName.Split(',').First();

            app.UseSwagger(c =>
            {
                c.RouteTemplate = $"openapi/{assemblyName.ToLower()}/{{documentName}}.{{extension:regex(^(json|ya?ml)$)}}";
            });

            return app;
        }

        public IApplicationBuilder UseOpenApiUI(Dictionary<string, string> endpoints)
        {
            app.UseSwaggerUI(o =>
            {
                o.RoutePrefix = "openapi";
                o.DocumentTitle = "DocSpace API";

                foreach (var (name, route) in endpoints)
                {
                    o.SwaggerEndpoint(route, name);
                }
            });

            app.UseEndpoints(endpointRouteBuilder =>
            {
                endpointRouteBuilder.MapSwagger();
            });

            return app;
        }
    }

    public static string CustomSchemaId(Type type)
    {
        var name = type.Name;

        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (type.IsGenericType)
        {
            name = name.Split('`')[0];
            var genericArgs = string.Join("", type.GetGenericArguments().Select(CustomSchemaId));
            name += genericArgs;
        }

        // Fix for nested classes
        name = name.Replace("+", "_");
        name = name.Replace("Int32", "Integer");
        return name;
    }

    private class AllowAnonymousFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var allowAnonymous = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .OfType<AllowAnonymousAttribute>();

            //var authorizeAttribute = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
            //    .Union (context.MethodInfo.GetCustomAttributes(true)) .OfType<AuthorizeAttribute>();


            if (allowAnonymous.Any())
            {
                operation.Security.Clear();
            }
            else
            {
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = CookiesManager.AuthCookiesName }
                        },
                        ["read", "write"]
                    }
                });
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                        new List<string>()
                    }
                });
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKeyBearer" } }, ["read", "write"]
                    }
                });
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Basic" } },
                        new List<string>()
                    }
                });
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OAuth2" } }, ["read", "write"]
                    }
                });
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OpenId" } }, []
                    }
                });

                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            }

            //if(authorizeAttribute.Any())
            //{
            //    var authorizationDescription = new StringBuilder(" (Auth:");
            //    var policySelector = authorizeAttribute.Where(a => !string.IsNullOrEmpty(a.Policy)).Select(a => a.Policy);
            //    var schemaSelector = authorizeAttribute.Where(a => !string.IsNullOrEmpty(a.AuthenticationSchemes)).Select(a => a.AuthenticationSchemes);
            //    var rolesSelector = authorizeAttribute.Where(a => !string.IsNullOrEmpty(a.Roles)).Select(a => a.Roles);
            //    ApplyAuthorizeAttribute(authorizationDescription, policySelector, schemaSelector, rolesSelector);
            //    operation.Summary += authorizationDescription.ToString().TrimEnd(';') + ")";
            //}
        }

        private void ApplyAuthorizeAttribute(StringBuilder authorizationDescription, List<string> policySelector, List<string> schemaSelector, List<string> rolesSelector)
        {
            if (policySelector.Count != 0)
            {
                authorizationDescription.Append($" Policy: {string.Join(", ", policySelector)};");
            }
            if (schemaSelector.Count != 0)
            {
                authorizationDescription.Append($" Schema: {string.Join(", ", schemaSelector)};");
            }
            if (rolesSelector.Count != 0)
            {
                authorizationDescription.Append($" Roles: {string.Join(", ", rolesSelector)};");
            }
        }
    }

    private class XmlCustomTagFilter(XPathDocument xmlDoc) : IOperationFilter
    {
        private readonly XPathNavigator _xmlNavigator = xmlDoc.CreateNavigator();

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.MethodInfo == null || context.MethodInfo.DeclaringType == null)
            {
                return;
            }

            var targetMethod = context.MethodInfo.DeclaringType.IsConstructedGenericType ?
                context.MethodInfo.GetUnderlyingGenericTypeMethod() :
                context.MethodInfo;

            if (targetMethod == null)
            {
                return;
            }

            ApplyMethodTags(operation, targetMethod);
        }

        private void ApplyMethodTags(OpenApiOperation operation, MethodInfo methodInfo)
        {
            var methodMemberName = XmlCommentsNodeNameHelper.GetMemberNameForMethod(methodInfo);
            var methodNode = _xmlNavigator.SelectSingleNode($"/doc/members/member[@name='{methodMemberName}']");

            var shortNode = methodNode?.SelectSingleNode("short");
            if (shortNode != null)
            {
                operation.AddExtension("x-shortName", new XmlShortNameExtension(XmlCommentsTextHelper.Humanize(shortNode.InnerXml)));
            }
        }
    }

    private class XmlShortNameExtension(string shortName) : IOpenApiExtension
    {
        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
        {
            writer.WriteValue(shortName);
        }
    }

    private class CustomInheritanceSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var type = context.Type;

            if (type.IsEnum || !type.IsClass || type == typeof(object) || IsSystemType(type))
            {
                return;
            }

            var baseType = type.BaseType;
            if (baseType == null || baseType == typeof(object) || IsSystemType(baseType) || baseType == typeof(BaseEntity))
            {
                return;
            }

            var baseProperties = GetAllBaseTypeProperties(type);
            if (baseProperties.Count == 0)
            {
                return;
            }

            var baseTypeSchema = context.SchemaGenerator.GenerateSchema(baseType, context.SchemaRepository);

            var schemaId = CustomSchemaId(baseType);

            context.SchemaRepository.Schemas.TryAdd(schemaId, baseTypeSchema);
            
            var baseSchemaRef = new OpenApiReference
            {
                Type = ReferenceType.Schema,
                Id = schemaId
            };

            var originalProperties = schema.Properties;
            var originalRequired = schema.Required;

            var derivedPropertiesSchema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>(),
                Required = new HashSet<string>()
            };

            foreach (var prop in originalProperties)
            {
                if (!baseProperties.Contains(prop.Key.ToLowerInvariant()))
                {
                    derivedPropertiesSchema.Properties.Add(prop.Key, prop.Value);
                    if (originalRequired?.Contains(prop.Key) == true)
                    {
                        derivedPropertiesSchema.Required.Add(prop.Key);
                    }
                }
            }

            schema.AllOf = new List<OpenApiSchema>
            {
                new OpenApiSchema { Reference = baseSchemaRef },
                derivedPropertiesSchema
            };

            schema.Properties = null;
            schema.Required = null;
            schema.Type = null;
        }

        private HashSet<string> GetAllBaseTypeProperties(Type type)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var currentType = type.BaseType;

            while (currentType != null && currentType != typeof(object))
            {
                foreach (var prop in currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var propName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);
                    result.Add(propName);
                }

                currentType = currentType.BaseType;
            }

            return result;
        }

        private static bool IsSystemType(Type type)
        {
            return type.Namespace != null && type.Namespace.StartsWith("System");
        }
    }
}