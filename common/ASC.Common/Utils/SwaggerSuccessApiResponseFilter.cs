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
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;
public class SwaggerSuccessApiResponseFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var schemas = swaggerDoc.Components.Schemas;
        var paths = swaggerDoc.Paths;

        foreach (var path in paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                var responses = operation.Value.Responses;
                foreach (var response in responses)
                {
                    if (response.Key == "200" && response.Value.Content != null)
                    {
                        foreach (var content in response.Value.Content)
                        {
                            var schema = content.Value.Schema;
                            WrapSchemaInSuccessApiResponse(schema, schemas);
                        }
                    }
                }
            }
        }
    }

    private static void WrapSchemaInSuccessApiResponse(OpenApiSchema schema, IDictionary<string, OpenApiSchema> schemas)
    {
        var originalSchemaRef = schema.Reference?.Id;

        var isPrimitive = schema.Type != null && schema.Type != "array";
        string responseSchemaKey;
        OpenApiSchema responseSchema = null;
        if (isPrimitive)
        {
            var typeName = GetPrimitiveTypeName(schema);
            responseSchemaKey = $"SuccessApiResponse{typeName}";
            var primitiveResponseProperty = new OpenApiSchema
            {
                Type = schema.Type
            };
            responseSchema = CreateSuccessApiResponseSchema(primitiveResponseProperty);
        }
        else if (schema.Type == "array")
        {
            originalSchemaRef = schema.Items.Reference?.Id;
            responseSchemaKey = $"SuccessApiResponseArray.{originalSchemaRef}";

            var arrayResponseProperty = new OpenApiSchema
            {
                Type = "array",
                Items = originalSchemaRef != null ? new OpenApiSchema { Reference = schema.Items.Reference } : new OpenApiSchema { Type = schema.Items.Type }
            };

            responseSchema = CreateSuccessApiResponseSchema(arrayResponseProperty);
        }
        else if (schema == null || (schema.Type == null && schema.Reference == null && schema.Items == null))
        {
            responseSchemaKey = "SuccessApiResponseObject";
            if (!schemas.ContainsKey(responseSchemaKey))
            {
                responseSchema = CreateSuccessApiResponseSchema(new OpenApiSchema { Type = "object" });
            }
        }
        else
        {
            responseSchemaKey = $"SuccessApiResponse.{originalSchemaRef}";
            var responseProperty = originalSchemaRef != null
            ? new OpenApiSchema { Reference = new OpenApiReference { Id = originalSchemaRef, Type = ReferenceType.Schema } }
            : schema;
            responseSchema = CreateSuccessApiResponseSchema(responseProperty);
        }
        if (!schemas.ContainsKey(responseSchemaKey))
        {
            schemas[responseSchemaKey] = responseSchema;
        }
        schema.Reference = new OpenApiReference { Id = responseSchemaKey, Type = ReferenceType.Schema };
        schema.Type = null;
        schema.Properties = null;
    }
    private static string GetPrimitiveTypeName(OpenApiSchema primitiveSchema)
    {
        return primitiveSchema.Type switch
        {
            "string" => "String",
            "boolean" => "Boolean",
            "integer" when primitiveSchema.Format == "int32" => "Int32",
            "integer" when primitiveSchema.Format == "int64" => "Int64",
            "number" when primitiveSchema.Format == "float" => "Float",
            "number" when primitiveSchema.Format == "double" => "Double",
            _ => "Unknown"
        };
    }
    private static OpenApiSchema CreateSuccessApiResponseSchema(OpenApiSchema responseProperty)
    {
        return new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                { "response", responseProperty },
                { "count", new OpenApiSchema { Type = "integer", Format = "int32" } },
                { "links", new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchema>
                            {
                                { "href", new OpenApiSchema { Type = "string" } },
                                { "action", new OpenApiSchema { Type = "string" } }
                            }
                        }
                    } },
                { "status", new OpenApiSchema { Type = "integer", Format = "int32" } },
                { "statusCode", new OpenApiSchema { Type = "integer", Format = "int32" } }
            }
        };
    }
}
