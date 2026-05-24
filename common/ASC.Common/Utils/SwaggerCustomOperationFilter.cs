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

namespace ASC.Api.Core.Extensions;

public class SwaggerCustomOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameter in operation.Parameters)
        {
            if (parameter.In == ParameterLocation.Query && parameter.Schema.Type == JsonSchemaType.Array)
            {
                (parameter as OpenApiParameter)?.Style = ParameterStyle.DeepObject;
                (parameter as OpenApiParameter)?.Explode = true;
            }

            // Remove duplicate example from parameter level if it exists in schema
            if (parameter is OpenApiParameter { Example: not null, Schema.Example: not null } openApiParameter)
            {
                openApiParameter.Example = null;
            }
        }
    }
}

public class ContentTypeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody != null)
        {
            var content = operation.RequestBody.Content
                .Where(c => c.Key != "text/json" && !c.Key.EndsWith("+json"))
                .ToDictionary(c => c.Key, c => c.Value);

            (operation.RequestBody as OpenApiRequestBody)?.Content = content;
        }

        if (operation.Responses != null)
        {
            foreach (var response in operation.Responses)
            {
                var content = response.Value.Content
                    .Where(c => c.Key.Equals("application/json"))
                    .ToDictionary(c => c.Key, c => c.Value);

                (response.Value as OpenApiResponse)?.Content = content;
            }
        }
    }
}

public class SwaggerOperationIdFilter : IOperationFilter
{
    private readonly string _route;
    private readonly string _newOperationId;
    private readonly string _shortName;

    public SwaggerOperationIdFilter(string route, string newOperationId, string shortName)
    {
        _route = route;
        _newOperationId = newOperationId;
        _shortName = shortName;
    }
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath.Equals(_route, StringComparison.OrdinalIgnoreCase) &&
            context.ApiDescription.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            operation.OperationId = _newOperationId;
            operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
            operation.Extensions["x-shortName"] = new JsonNodeExtension(_shortName);
        }
    }
}

public class PolymorphicFolderIdOperationFilter : IOperationFilter
{
    private static readonly HashSet<string> FolderIdParameterNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "folderId",
        "folderIds",
        "parentId",
        "rootFolderId",
        "destFolderId"
    };

    // Generic name "id" is also used as the folder identifier in some routes (e.g. "/folder/{id}/share",
    // "/folder/{id}/link", "/folder/{id}/links"). It is shared with non-folder entities (file, room, etc.),
    // so we only treat it as polymorphic when the route template contains the "/folder/{id}" segment.
    private const string GenericIdParameterName = "id";
    private const string FolderRouteSegment = "/folder/{id}";

    private const string FilesApiNamespacePrefix = "ASC.Files";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
        {
            return;
        }

        var declaringTypeNamespace = context.MethodInfo?.DeclaringType?.Namespace;
        if (declaringTypeNamespace == null
            || !declaringTypeNamespace.StartsWith(FilesApiNamespacePrefix, StringComparison.Ordinal))
        {
            return;
        }

        var routeTemplate = context.ApiDescription?.ActionDescriptor?.AttributeRouteInfo?.Template;

        foreach (var parameter in operation.Parameters)
        {
            if (parameter is not OpenApiParameter openApiParameter)
            {
                continue;
            }

            if (openApiParameter.In != ParameterLocation.Path && openApiParameter.In != ParameterLocation.Query)
            {
                continue;
            }

            if (string.IsNullOrEmpty(openApiParameter.Name)
                || !IsPolymorphicFolderParameter(openApiParameter.Name, routeTemplate))
            {
                continue;
            }

            if (openApiParameter.In == ParameterLocation.Path && HasInlineRouteConstraint(routeTemplate, openApiParameter.Name))
            {
                continue;
            }

            if (openApiParameter.Schema is not OpenApiSchema schema)
            {
                continue;
            }

            ApplyPolymorphicSchema(openApiParameter, schema);
        }
    }

    private static bool IsPolymorphicFolderParameter(string parameterName, string routeTemplate)
    {
        if (FolderIdParameterNames.Contains(parameterName))
        {
            return true;
        }

        if (string.Equals(parameterName, GenericIdParameterName, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(routeTemplate)
            && routeTemplate.Contains(FolderRouteSegment, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool HasInlineRouteConstraint(string routeTemplate, string parameterName)
    {
        if (string.IsNullOrEmpty(routeTemplate))
        {
            return false;
        }

        return routeTemplate.Contains("{" + parameterName + ":", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyPolymorphicSchema(OpenApiParameter parameter, OpenApiSchema schema)
    {
        if (schema.OneOf is { Count: > 0 } || schema.AnyOf is { Count: > 0 })
        {
            return;
        }

        if (schema.Type.HasValue && (schema.Type.Value & JsonSchemaType.Array) == JsonSchemaType.Array)
        {
            if (schema.Items is OpenApiSchema itemsSchema
                && itemsSchema.OneOf is not { Count: > 0 }
                && itemsSchema.AnyOf is not { Count: > 0 }
                && itemsSchema.Type.HasValue
                && (itemsSchema.Type.Value & JsonSchemaType.Integer) == JsonSchemaType.Integer)
            {
                schema.Items = new OpenApiSchema
                {
                    Description = itemsSchema.Description,
                    Extensions = itemsSchema.Extensions,
                    Example = itemsSchema.Example,
                    Deprecated = itemsSchema.Deprecated,
                    OneOf = FolderIdSchemaHelper.CreateIntStringSchemas()
                };

                FolderIdSchemaHelper.AppendDescription(parameter);
            }

            return;
        }

        if (!schema.Type.HasValue || (schema.Type.Value & JsonSchemaType.Integer) != JsonSchemaType.Integer)
        {
            return;
        }

        schema.OneOf = FolderIdSchemaHelper.CreateIntStringSchemas();
        schema.Type = null;
        schema.Format = null;

        FolderIdSchemaHelper.AppendDescription(parameter);
    }

}

/// <summary>
/// Mirrors <see cref="PolymorphicFolderIdOperationFilter"/> for body schemas: when a closed generic
/// DTO inside the Files API exposes a folder-identifier property whose original (open-generic) type
/// is a generic parameter <c>T</c>, the integer schema is rewritten as <c>oneOf: [integer, string]</c>
/// so internal and third-party folder IDs can be passed in request bodies (e.g. <c>SaveAsPdf.folderId</c>).
/// Non-generic DTOs are intentionally not scanned; add explicit support here if a non-generic body DTO
/// needs a polymorphic folder ID.
/// </summary>
public class PolymorphicFolderIdSchemaFilter : ISchemaFilter
{
    private static readonly HashSet<string> FolderIdPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "folderId",
        "folderIds",
        "parentId",
        "rootFolderId",
        "destFolderId"
    };

    private const string FilesApiRequestDtoNamespacePrefix = "ASC.Files.Core.ApiModels.RequestDto";

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema openApiSchema)
        {
            return;
        }

        var type = context.Type;
        if (type == null || !type.IsGenericType)
        {
            return;
        }

        if (type.Namespace == null || !type.Namespace.StartsWith(FilesApiRequestDtoNamespacePrefix, StringComparison.Ordinal))
        {
            return;
        }

        if (openApiSchema.Properties == null || openApiSchema.Properties.Count == 0)
        {
            return;
        }

        var openTypeDefinition = type.GetGenericTypeDefinition();

        foreach (var (propertyName, propertySchema) in openApiSchema.Properties.ToList())
        {
            if (!FolderIdPropertyNames.Contains(propertyName))
            {
                continue;
            }

            var openProperty = FindOpenGenericProperty(openTypeDefinition, propertyName);
            if (openProperty == null)
            {
                continue;
            }

            if (!IsGenericTypeParameterProperty(openProperty.PropertyType))
            {
                continue;
            }

            if (propertySchema is not OpenApiSchema concreteSchema)
            {
                continue;
            }

            RewriteSchema(concreteSchema);
        }
    }

    private static PropertyInfo FindOpenGenericProperty(Type openTypeDefinition, string camelCasePropertyName)
    {
        if (string.IsNullOrEmpty(camelCasePropertyName))
        {
            return null;
        }

        var pascalCase = char.ToUpperInvariant(camelCasePropertyName[0]) + camelCasePropertyName[1..];
        return openTypeDefinition
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => string.Equals(p.Name, pascalCase, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsGenericTypeParameterProperty(Type propertyType)
    {
        if (propertyType.IsGenericParameter)
        {
            return true;
        }

        // Cover collection-of-T cases such as List<T>, IEnumerable<T>.
        if (propertyType.IsGenericType
            && propertyType.GetGenericArguments().Any(a => a.IsGenericParameter))
        {
            return true;
        }

        if (propertyType.IsArray
            && propertyType.GetElementType() is { IsGenericParameter: true })
        {
            return true;
        }

        return false;
    }

    private static void RewriteSchema(OpenApiSchema propertySchema)
    {
        if (propertySchema.OneOf is { Count: > 0 } || propertySchema.AnyOf is { Count: > 0 })
        {
            return;
        }

        if (propertySchema.Type.HasValue
            && (propertySchema.Type.Value & JsonSchemaType.Array) == JsonSchemaType.Array
            && propertySchema.Items is OpenApiSchema itemsSchema
            && itemsSchema.OneOf is not { Count: > 0 }
            && itemsSchema.AnyOf is not { Count: > 0 }
            && itemsSchema.Type.HasValue
            && (itemsSchema.Type.Value & JsonSchemaType.Integer) == JsonSchemaType.Integer)
        {
            propertySchema.Items = new OpenApiSchema
            {
                Description = itemsSchema.Description,
                Extensions = itemsSchema.Extensions,
                Example = itemsSchema.Example,
                Deprecated = itemsSchema.Deprecated,
                OneOf = FolderIdSchemaHelper.CreateIntStringSchemas()
            };

            FolderIdSchemaHelper.AppendDescription(propertySchema);
            return;
        }

        if (!propertySchema.Type.HasValue
            || (propertySchema.Type.Value & JsonSchemaType.Integer) != JsonSchemaType.Integer)
        {
            return;
        }

        propertySchema.OneOf = FolderIdSchemaHelper.CreateIntStringSchemas();
        propertySchema.Type = null;
        propertySchema.Format = null;
        FolderIdSchemaHelper.AppendDescription(propertySchema);
    }
}

internal static class FolderIdSchemaHelper
{
    private const string PolymorphicDescription = "An internal DocSpace folder is identified by an integer; a third-party folder is identified by a string.";

    // Using oneOf (not anyOf) because a folder ID is exactly one type: either
    // an internal integer or a third-party string - never both simultaneously.
    internal static List<IOpenApiSchema> CreateIntStringSchemas()
    {
        return
        [
            new OpenApiSchema
            {
                Type = JsonSchemaType.Integer,
                Format = "int32",
                Description = "Internal DocSpace folder ID."
            },
            new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Description = "Third-party (e.g. Dropbox) folder ID."
            }
        ];
    }

    internal static void AppendDescription(OpenApiParameter parameter)
    {
        if (string.IsNullOrEmpty(parameter.Description))
        {
            parameter.Description = PolymorphicDescription;
        }
        else if (!parameter.Description.Contains(PolymorphicDescription, StringComparison.Ordinal))
        {
            parameter.Description = parameter.Description.TrimEnd() + " " + PolymorphicDescription;
        }
    }

    internal static void AppendDescription(OpenApiSchema schema)
    {
        if (string.IsNullOrEmpty(schema.Description))
        {
            schema.Description = PolymorphicDescription;
        }
        else if (!schema.Description.Contains(PolymorphicDescription, StringComparison.Ordinal))
        {
            schema.Description = schema.Description.TrimEnd() + " " + PolymorphicDescription;
        }
    }
}
