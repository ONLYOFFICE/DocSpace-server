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

using System.Text.Json.Nodes;

using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;

public class HideRouteDocumentFilter(string routeToHide) : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        document.Paths.Remove(routeToHide);
    }
}

public class ErrorResponseFilter(IOptions<RateLimiterSettings> rateLimiterOptions) : IDocumentFilter
{
    private readonly RateLimiterSettings _settings = rateLimiterOptions.Value;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Components.Headers ??= new Dictionary<string, IOpenApiHeader>();
        swaggerDoc.Components.Headers["X-RateLimit-Limit"] = new OpenApiHeader
        {
            Description = $"Sliding window rate limit: {_settings.SlidingWindowLimit} requests per minute per user/IP.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = _settings.SlidingWindowLimit },
        };
        swaggerDoc.Components.Headers["X-RateLimit-Remaining"] = new OpenApiHeader
        {
            Description = $"Number of requests remaining in the current sliding window ({_settings.SlidingWindowLimit} req/min). " +
                          $"Concurrent limits also apply: {_settings.ConcurrentGetLimit} parallel GET requests, " +
                          $"{_settings.DefaultConcurrencyWriteRequests} parallel POST/PUT requests.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 1 },
        };
        swaggerDoc.Components.Headers["X-RateLimit-Reset"] = new OpenApiHeader
        {
            Description = "Unix timestamp (seconds) when the current sliding window rate limit resets.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 1750000000 },
        };
        swaggerDoc.Components.Headers["Retry-After"] = new OpenApiHeader
        {
            Description = $"Seconds to wait before retrying. " +
                          $"Up to 60s for the sliding window ({_settings.SlidingWindowLimit} req/min), up to 86400s for the daily POST/PUT limit ({_settings.DailyWriteLimit}/day).",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 30 },
        };

        foreach (var path in swaggerDoc.Paths.Values)
        {
            foreach (var operation in path.Operations.Values)
            {
                if (operation.Responses.TryGetValue("200", out var okResponse) && okResponse is OpenApiResponse concreteOkResponse)
                {
                    concreteOkResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
                    concreteOkResponse.Headers.TryAdd("X-RateLimit-Limit", new OpenApiHeaderReference("X-RateLimit-Limit", swaggerDoc));
                    concreteOkResponse.Headers.TryAdd("X-RateLimit-Remaining", new OpenApiHeaderReference("X-RateLimit-Remaining", swaggerDoc));
                    concreteOkResponse.Headers.TryAdd("X-RateLimit-Reset", new OpenApiHeaderReference("X-RateLimit-Reset", swaggerDoc));
                }

                operation.Responses.TryAdd("429", new OpenApiResponse
                {
                    Description = "Too Many Requests.",
                    Headers = new Dictionary<string, IOpenApiHeader>
                    {
                        ["Retry-After"] = new OpenApiHeaderReference("Retry-After", swaggerDoc)
                    }
                });
                operation.Responses.TryAdd("502", new OpenApiResponse
                {
                    Description = "Bad Gateway. Returned by the reverse proxy, response body may be HTML and not JSON."
                });
                operation.Responses.TryAdd("503", new OpenApiResponse
                {
                    Description = "Service Unavailable. Returned by the reverse proxy, response body may be HTML and not JSON."
                });
            }
        }
    }
}

public class DerivedSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if(schema is not OpenApiSchema openApiSchema)
        {
            return;
        }

        var baseType = context.Type;
        var derivedTypes = baseType.GetCustomAttributes<JsonDerivedTypeAttribute>(true).Select(attr => attr.DerivedType).Distinct().ToList();
        if (derivedTypes.Count > 0)
        {
            openApiSchema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
            openApiSchema.Extensions.Add("x-derived", new JsonNodeExtension(true));
            var derivedArray = new JsonArray();
            foreach (var type in derivedTypes)
            {
                var schemaId = CustomSchemaId(type);
                derivedArray.Add(schemaId);
            }

            if (derivedArray.Any())
            {
                openApiSchema.Extensions["x-derived-types"] = new JsonNodeExtension(derivedArray);
            }
        }
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
        name = name.Replace("+", "_");
        name = name.Replace("Int32", "Integer");
        return name;
    }
}

public class LowercaseDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = new OpenApiPaths();
        foreach (var (key, value) in swaggerDoc.Paths)
        {
            var segments = key.Split('/');

            for (var i = 0; i < segments.Length; i++)
            {
                if (!segments[i].StartsWith('{') && !segments[i].EndsWith("}"))
                {
                    segments[i] = segments[i].ToLowerInvariant();
                }
            }

            var lowerCaseKey = string.Join("/", segments);
            paths.Add(lowerCaseKey, value);
        }

        swaggerDoc.Paths = paths;
    }
}

public class TagDescriptionsDocumentFilter : IDocumentFilter
{
    private readonly Dictionary<string, string> _tagDescriptions = new()
    {
        { "People", "Operations for working with people" },
        { "Portal", "Operations for working with portal" },
        { "Settings", "Operations for working with settings" },
        { "Backup", "Operations for working with backup" },
        { "Files / Files", "Operations for working with files." },
        { "Files / Folders", "Operations for working with folders." },
        { "Files / Operations", "Operations for performing actions on files and folders." },
        { "Files / Quota", "Operations for working with room quota limit." },
        { "Rooms", "Operations for working with rooms." },
        { "Rooms / Groups", "Operations for managing groups within rooms." },
        { "Files / Settings", "Operations for working with file settings." },
        { "Files / Third-party integration", "Operations for working with third-party integrations." },
        { "Files / Sharing", "Operations for working with sharing."},
        { "Group", "Operations for working with groups." },
        { "Group / Rooms", "Operations for getting groups with access rights to a room." },
        { "Group / Search", "Operations for searching groups." },
        { "People / Contacts", "Operations for working with user contacts." },
        { "People / Password", "Operations for working with user passwords." },
        { "People / Photos", "Operations for working with user photos." },
        { "People / Profiles", "Operations  for working with user profiles." },
        { "People / Quota", "Operations for working with user quotas." },
        { "People / Search", "Operations for searching users." },
        { "People / Theme", "Operations for working with portal theme." },
        { "People / Third-party accounts", "Operations for working with third-party accounts." },
        { "People / User data", "Operations for working with user data." },
        { "People / User status", "Operations for working with user status." },
        { "People / User type", "Operations for working with user types." },
        { "People / Guests", "Operations for workig with guests" },
        { "Authentication", "Operations for authenticating users." },
        { "Capabilities", "Operations for getting information about portal capabilities." },
        { "Migration", "Operations for performing migration." },
        { "ThirdParty", "Operations for working with third-party." },
        { "Portal / Quota", "Operations for getting information about portal quota." },
        { "Portal / Settings", "Operations for getting information about portal settings." },
        { "Portal / Users", "Operations for getting information about portal users." },
        { "Portal / Payment", "Operations for getting information about payment."},
        { "Portal / Guests", "Operations for managing guest users in the portal." },
        { "Security / Active connections", "Operations for working with active connections." },
        { "Security / Audit trail data", "Operations for working with audit trail data." },
        { "Security / CSP", "Operations for working with CSP." },
        { "Security / OAuth2", "Operations for working with OAuth2." },
        { "Security / Firebase", "Operations for working with Firebase." },
        { "Security / Login history", "Operations for getting login history." },
        { "Security / SMTP settings", "Operations for working with SMTP settings." },
        { "Settings / Authorization", "Operations for working with authorization settings." },
        { "Security / Access to DevTools", "Operations for working with acess to devtools."},
        { "Security / Banners visibility", "Operations for managing visibility of security banners." },
        { "Settings / Common settings", "Operations for working with common settings." },
        { "Settings / Cookies", "Operations for working with cookies settings." },
        { "Settings / Custom Navigation", "Operations for working with custom navigation settings." },
        { "Settings / Encryption", "Operations for working with encryption settings." },
        { "Settings / Greeting settings", "Operations for working with greeting settings." },
        { "Settings / IP restrictions", "Operations for working with IP restriction settings." },
        { "Settings / License", "Operations for working with license settings." },
        { "Settings / Login settings", "Operations for working with login settings." },
        { "Settings / Messages", "Operations for working with message settings." },
        { "Settings / Notifications", "Operations for working with notification settings." },
        { "Settings / Owner", "Operations for working with owner settings." },
        { "Settings / Quota", "Operations for working with quota settings." },
        { "Settings / Rebranding", "Operations for working with rebranding settings." },
        { "Settings / Security", "Operations for working with security settings." },
        { "Settings / SSO", "Operations for working with SSO settings." },
        { "Settings / Statistics", "Operations for working with statistics settings." },
        { "Settings / Storage", "Operations for working with storage settings." },
        { "Settings / Team templates", "Operations for working with team template settings." },
        { "Settings / TFA settings", "Operations for working with TFA settings." },
        { "Settings / Tips", "Operations for working with tip settings." },
        { "Settings / Webhooks", "Operations for working with webhook settings." },
        { "Settings / Webplugins", "Operations for working with webplugin settings." },
        { "Settings / Access to DevTools", "Operations for working with acess to devtools." },
        { "Settings / Versions", "Operations for working with versions settings." },
        { "Settings / LDAP", "Operations for working with LDAP settings." },
        { "Settings / Banners visibility", "Operations for managing visibility of settings banners." },
        { "Settings / Telegram", "Operations for managing Telegram integration." },
        { "Api keys", "Operations for working with api keys." },
        { "AI / Agents", "Operations for working with AI agents." },
        { "AI / Chat", "Operations for working with AI chat." },
        { "AI / Messages", "Operations for working with AI messages." },
        { "AI / Providers", "Operations for working with AI providers." },
        { "AI / Settings", "Operations for working with AI settings." },
        { "AI / MCP", "Operations for working with MCP servers." },
        { "AI / Vectorization", "Operations for working with vectorization." }
    };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var customTags = new HashSet<string>();

        foreach (var path in swaggerDoc.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                foreach (var tag in operation.Value.Tags)
                {
                    customTags.Add(tag.Name);
                }
            }
        }

        swaggerDoc.Tags = customTags
            .Where(tag => _tagDescriptions.ContainsKey(tag))
            .Select(tag =>
            {
                var tagParts = tag.Split(" / ");
                var displayName = tagParts.Length > 1 ? tagParts[1] : tagParts[0];

                var openApiTag = new OpenApiTag
                {
                    Name = tag,
                    Description = _tagDescriptions[tag]
                };
                openApiTag.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                openApiTag.Extensions.Add("x-displayName", new JsonNodeExtension(displayName));

                return openApiTag;
            }).ToHashSet();

        var groupTag = customTags
            .Where(tag => _tagDescriptions.ContainsKey(tag))
            .GroupBy(tag => tag.Split(" / ")[0])
            .ToDictionary(group => group.Key, group => group.ToList());

        var tagGroups = new JsonArray();
        foreach (var group in groupTag)
        {
            var groupObject = new JsonObject();
            var tagsArray = new JsonArray();

            foreach (var tag in group.Value)
            {
                tagsArray.Add(tag);
            }

            groupObject["name"] = group.Key;
            groupObject["tags"] = tagsArray;
            tagGroups.Add(groupObject);
        }

        swaggerDoc.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        swaggerDoc.Extensions["x-tagGroups"] = new JsonNodeExtension(tagGroups);
    }
}