// (c) Copyright Ascensio System SIA 2009-2026
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

using Microsoft.OpenApi;

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
                            content.Value.Schema = WrapSchemaInSuccessApiResponse(content.Value.Schema, schemas);
                        }
                    }
                }
            }
        }
    }

    private static IOpenApiSchema WrapSchemaInSuccessApiResponse(IOpenApiSchema schema, IDictionary<string, IOpenApiSchema> schemas)
    {
         var originalSchemaRef = (schema as OpenApiSchemaReference)?.Reference.Id;

        var isPrimitive = schema.Type != null && schema.Type != JsonSchemaType.Array && schema.Type != JsonSchemaType.Object;
        string responseSchemaKey = null;
        OpenApiSchema responseSchema = null;
        if (isPrimitive)
        {
            var typeName = GetPrimitiveTypeName(schema);
            if ((schema.Type & JsonSchemaType.Null) == JsonSchemaType.Null)
            {
                typeName += "Nullable";
            }
            responseSchemaKey = $"{typeName}Wrapper";
            var primitiveResponseProperty = new OpenApiSchema
            {
                Type = schema.Type
            };
            responseSchema = CreateSuccessApiResponseSchema(primitiveResponseProperty);
        }
        else if (schema.OneOf != null && schema.OneOf.Any(s => (s as OpenApiSchemaReference)?.Reference.Id != null))
        {
            var firstRefId = (schema.OneOf.FirstOrDefault(s => (s as OpenApiSchemaReference)?.Reference.Id != null) as OpenApiSchemaReference)?.Reference.Id;
            responseSchemaKey = firstRefId.Contains("Dto") ? firstRefId.Replace("Dto", "") + "Wrapper"
                : firstRefId + "Wrapper";
            var responseProperty = new OpenApiSchema
            {
                OneOf = schema.OneOf
            };

            responseSchema = CreateSuccessApiResponseSchema(responseProperty);
            //schema.OneOf = null;
        }
        else if (schema.Type == JsonSchemaType.Array)
        {
            originalSchemaRef = (schema.Items as OpenApiSchemaReference)?.Reference.Id;
            var schemaArray = schema.Items;
            OpenApiSchema arrayResponseProperty;

            if (schema.OneOf != null && schema.OneOf.Any(s => s.Items != null))
            {
                var firstRefId = (schema.OneOf.FirstOrDefault(s => (s.Items as OpenApiSchemaReference)?.Reference.Id != null)?.Items as OpenApiSchemaReference)?.Reference.Id;
                responseSchemaKey = firstRefId.Contains("Dto") ? firstRefId.Replace("Dto", "") + "ArrayWrapper" : firstRefId + "ArrayWrapper";

                arrayResponseProperty = new OpenApiSchema
                {
                    Items = new OpenApiSchema
                    {
                        OneOf = schema.OneOf
                    }
                };

                responseSchema = CreateSuccessApiResponseSchema(arrayResponseProperty);

                //schema.OneOf = null;
            }
            else if (schemaArray.Type == null && (schemaArray is not OpenApiSchemaReference) && schemaArray.Items == null)
            {
                responseSchemaKey = "ObjectArrayWrapper";
                arrayResponseProperty = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchema { Type = JsonSchemaType.Object }
                };
            }
            else if (schemaArray is { Type: JsonSchemaType.Array })
            {
                responseSchemaKey = "ArrayArrayWrapper";
                arrayResponseProperty = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchema { Type = JsonSchemaType.Array, Items = new OpenApiSchema { Type = schemaArray.Items.Type } }
                };
            }
            else
            {
                responseSchemaKey = originalSchemaRef == null ? $"{schema.Items.Type.ToString().ToUpper()}ArrayWrapper"
                    : originalSchemaRef.Contains("Dto") ? originalSchemaRef.Replace("Dto", "") + "ArrayWrapper" : originalSchemaRef + "ArrayWrapper";
                arrayResponseProperty = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = originalSchemaRef != null ? new OpenApiSchemaReference((schema.Items as OpenApiSchemaReference)?.Reference.Id) : new OpenApiSchema { Type = schema.Items.Type }
                };
            }

            responseSchema = CreateSuccessApiResponseSchema(arrayResponseProperty);
        }
        else if (schema == null || (schema.Type == null && (schema is not OpenApiSchemaReference) && schema.Items == null))
        {
            responseSchemaKey = "ObjectWrapper";
            if (!schemas.ContainsKey(responseSchemaKey))
            {
                responseSchema = CreateSuccessApiResponseSchema(new OpenApiSchema { Type = JsonSchemaType.Object });
            }
        }
        else
        {
            if (originalSchemaRef != null)
            {
                responseSchemaKey = originalSchemaRef.Contains("Dto") ? originalSchemaRef.Replace("Dto", "") + "Wrapper" : originalSchemaRef + "Wrapper";
            }

            var responseProperty = originalSchemaRef != null
            ? new OpenApiSchemaReference(originalSchemaRef)
            : schema;
            responseSchema = CreateSuccessApiResponseSchema(responseProperty);
        }

        if (responseSchemaKey != null)
        {
            schemas.TryAdd(responseSchemaKey, responseSchema);
            // schema.Reference = new OpenApiSchemaReference(responseSchemaKey);
            //schema.Type = null;
            //schema.Properties = null;
            return new OpenApiSchemaReference(responseSchemaKey);
        }

        return responseSchema;
    }
    
    private static string GetPrimitiveTypeName(IOpenApiSchema primitiveSchema)
    {
        if ((primitiveSchema.Type & JsonSchemaType.String) == JsonSchemaType.String)
        {
            return "String";
        }
        
        if ((primitiveSchema.Type & JsonSchemaType.Boolean) == JsonSchemaType.Boolean)
        {
            return "Boolean";
        }
        
        if ((primitiveSchema.Type & JsonSchemaType.Integer) == JsonSchemaType.Integer && primitiveSchema.Format == "int32")
        {
            return "Int32";
        }
        
        if ((primitiveSchema.Type & JsonSchemaType.Integer) == JsonSchemaType.Integer && primitiveSchema.Format == "int64")
        {
            return "Int64";
        }
        
        if ((primitiveSchema.Type & JsonSchemaType.Number) == JsonSchemaType.Number && primitiveSchema.Format == "float")
        {
            return "Float";
        }
        
        if ((primitiveSchema.Type & JsonSchemaType.Number) == JsonSchemaType.Number && primitiveSchema.Format == "double")
        {
            return "Double";
        }

        return "Unknown";
    }
    private static OpenApiSchema CreateSuccessApiResponseSchema(IOpenApiSchema responseProperty)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                { "response", responseProperty },
                { "count", new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Description = "The total number of items in the response"  } },
                { "links", new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Description = "List of links related to the response",
                        Items = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>
                            {
                                { "href", new OpenApiSchema { Type = JsonSchemaType.String, Description = "URL of the link" } },
                                { "action", new OpenApiSchema { Type = JsonSchemaType.String, Description = "Action associated with the link" } }
                            }
                        }
                    } },
                { "status", new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Description = "HTTP status code of the response"  } },
                { "statusCode", new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32", Description = "HTTP status code of the response (duplicate of status)"  } }
            }
        };
    }
}