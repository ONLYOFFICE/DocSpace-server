using System.Reflection.Metadata;

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
        if (schema.Type == "array")
        {
            originalSchemaRef = schema.Items.Reference?.Id;
        }
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
        else if(schema == null || (schema.Type == null && schema.Reference == null && schema.Items == null))
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
