﻿// (c) Copyright Ascensio System SIA 2009-2025
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
            var docName = assemblyName.Split(".").Last();
            c.ResolveConflictingActions(a => a.First());
            c.CustomOperationIds(r =>
            {
                var actionName = r.ActionDescriptor.RouteValues["action"];

                return (char.ToLower(actionName[0]) + actionName.Substring(1));
            });

            c.CustomSchemaIds(CustomSchemaId);

            c.SwaggerDoc("common", new OpenApiInfo { Title = docName, Version = "v2" });
            c.SchemaFilter<SwaggerSchemaCustomFilter>();
            c.DocumentFilter<LowercaseDocumentFilter>();
            c.DocumentFilter<HideRouteDocumentFilter>("/api/2.0/capabilities.json");
            c.DocumentFilter<TagDescriptionsDocumentFilter>();
            c.OperationFilter<SwaggerCustomOperationFilter>();
            c.OperationFilter<ContentTypeOperationFilter>();
            c.DocumentFilter<SwaggerSuccessApiResponseFilter>();
            c.EnableAnnotations();

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
                Scheme = "basic",
                Description = "Enter your username and password"
            });

            // JWT Authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter 'Bearer {JWT Token}'"
            });

            // API Key Authentication
            c.AddSecurityDefinition("ApiKeyBearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "API Key",
                Description = "Authentication is determined by the 'Authorization' header"
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

    public static IApplicationBuilder UseOpenApi(this IApplicationBuilder app)
    {
        var assemblyName = Assembly.GetEntryAssembly().FullName.Split(',').First();

        app.UseSwagger(c =>
        {
            c.RouteTemplate = $"openapi/{assemblyName.ToLower()}/{{documentName}}.{{extension:regex(^(json|ya?ml)$)}}";
        });
        
        return app;
    }

    public static IApplicationBuilder UseOpenApiUI(this IApplicationBuilder app, Dictionary<string, string> endpoints)
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

    private static string CustomSchemaId(Type type)
    {
        var name = type.Name;

        if (string.IsNullOrEmpty(name))
        {
            return name;
        }
        
        if (type.IsGenericType)
        {
            name = name.Split('`')[0];

            var genericArgs = string.Join("", type.GenericTypeArguments.Select(CustomSchemaId));
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
                    },
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                        new List<string>()
                    },
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKeyBearer" } },
                        new List<string>()
                    },
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Basic" } },
                        new List<string>()
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

        private void ApplyAuthorizeAttribute(StringBuilder authorizationDescription, IEnumerable<string> policySelector, IEnumerable<string> schemaSelector, IEnumerable<string> rolesSelector)
        {
            if (policySelector.Any())
            {
                authorizationDescription.Append($" Policy: {string.Join(", ", policySelector)};");
            }
            if (schemaSelector.Any())
            {
                authorizationDescription.Append($" Schema: {string.Join(", ", schemaSelector)};");
            }
            if (rolesSelector.Any())
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
}