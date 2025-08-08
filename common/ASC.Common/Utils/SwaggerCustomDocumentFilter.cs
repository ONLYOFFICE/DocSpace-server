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

using System.Web.Services.Description;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;

public class HideRouteDocumentFilter(string routeToHide) : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        document.Paths.Remove(routeToHide);
    }
}

public class OneOfResponseFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var responceAttribute = method.GetCustomAttributes<SwaggerResponseAttribute>().Where(attr => attr.StatusCode == 200 && attr.Type != null).ToList();
        if (responceAttribute.Count > 1)
        {
            var OneOfSchema = responceAttribute.Select(attr => context.SchemaGenerator.GenerateSchema(attr.Type!, context.SchemaRepository)).ToList();

            if (operation.Responses.TryGetValue("200", out var response))
            {
                foreach (var content in response.Content)
                {
                    var schema = content.Value.Schema;
                    if (schema.Type == "array")
                    {
                        content.Value.Schema = new OpenApiSchema
                        {
                            OneOf = OneOfSchema,
                            Type = "array"
                        };
                    }
                    else
                    {
                        content.Value.Schema = new OpenApiSchema
                        {
                            OneOf = OneOfSchema
                        };
                    }
                }
            }
        }
    }
}

public class DerivedSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var baseType = context.Type;
        var derivedTypes = baseType.GetCustomAttributes<JsonDerivedTypeAttribute>(true).Select(attr => attr.DerivedType).Where(t => t != null).Distinct().ToList();
        if (derivedTypes.Count > 0)
        {

            schema.Extensions.Add("x-derived", new OpenApiBoolean(true));
            var derivedArray = new OpenApiArray();
            foreach (var type in derivedTypes)
            {
                var schemaId = CustomSchemaId(type);
                derivedArray.Add(new OpenApiString(schemaId));
            }

            schema.Extensions.Add("x-derived-types", derivedArray);
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
                if (!segments[i].StartsWith("{") && !segments[i].EndsWith("}"))
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
        { "Tariff", "Operations for working with tariff" },
        { "Backup", "Operations for working with backup" },
        { "Files / Files", "Operations for working with files." },
        { "Files / Folders", "Operations for working with folders." },
        { "Files / Operations", "Operations for performing actions on files and folders." },
        { "Files / Quota", "Operations for working with room quota limit." },
        { "Rooms", "Operations for working with rooms." },
        { "Files / Settings", "Operations for working with file settings." },
        { "Files / Third-party integration", "Operations for working with third-party integrations." },
        { "Files / Sharing", "Operations for working with sharing."},
        { "Group", "Operations for working with groups." },
        { "Group / Rooms", "Operations for getting groups with access rights to a room." },
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
        { "People / Guests", "Operations for workig with gursts" },
        { "Authentication", "Operations for authenticating users." },
        { "Capabilities", "Operations for getting information about portal capabilities." },
        { "Migration", "Operations for performing migration." },
        { "Modules", "Operations for getting information about portal modules." },
        { "ThirdParty", "Operations for working with third-party." },
        { "Portal / Quota", "Operations for getting information about portal quota." },
        { "Portal / Settings", "Operations for getting information about portal settings." },
        { "Portal / Users", "Operations for getting information about portal users." },
        { "Portal / Payment", "Operations for getting information about payment."},
        { "Security / Active connections", "Operations for working with active connections." },
        { "Security / Audit trail data", "Operations for working with audit trail data." },
        { "Security / CSP", "Operations for working with CSP." },
        { "Security / OAuth2", "Operations for working with OAuth2." },
        { "Security / Firebase", "Operations for working with Firebase." },
        { "Security / Login history", "Operations for getting login history." },
        { "Security / SMTP settings", "Operations for working with SMTP settings." },
        { "Settings / Authorization", "Operations for working with authorization settings." },
        { "Security / Access to DevTools", "Operations for working with acess to devtools."},
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
        { "Api keys", "Operations for working with api keys." }
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
                openApiTag.Extensions.Add("x-displayName", new OpenApiString(displayName));

                return openApiTag;
            }).ToList();

        var groupTag = customTags
            .Where(tag => _tagDescriptions.ContainsKey(tag))
            .GroupBy(tag => tag.Split(" / ")[0])
            .ToDictionary(group => group.Key, group => group.ToList());

        var tagGroups = new OpenApiArray();
        foreach (var group in groupTag)
        {
            var groupObject = new OpenApiObject();
            var tagsArray = new OpenApiArray();

            foreach(var tag in group.Value)
            {
                tagsArray.Add(new OpenApiString(tag));
            }

            groupObject["name"] = new OpenApiString(group.Key);
            groupObject["tags"] = tagsArray;
            tagGroups.Add(groupObject);
        }

        swaggerDoc.Extensions["x-tagGroups"] = tagGroups;
    }
}