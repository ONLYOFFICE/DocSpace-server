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

using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;
public class SwaggerSuccessApiResponseFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var schemas = swaggerDoc.Components.Schemas;
        var paths = swaggerDoc.Paths;
        // Wrapper name -> the payload it was generated for. Not a plain set of names: the name has to
        // be given back only to the payload it was minted for, see WrapperKey.
        var generated = new Dictionary<string, string>();

        foreach (var path in paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                var responses = operation.Value.Responses;
                foreach (var response in responses)
                {
                    // The runtime CustomResponseFilterAttribute wraps every ObjectResult regardless of its
                    // status code, so every documented success response with a body must be wrapped too.
                    if (response.Key.StartsWith('2') && response.Value.Content != null)
                    {
                        foreach (var content in response.Value.Content)
                        {
                            content.Value.Schema = WrapSchemaInSuccessApiResponse(content.Value.Schema, schemas, generated);
                        }
                    }
                }
            }
        }
    }

    private static IOpenApiSchema WrapSchemaInSuccessApiResponse(IOpenApiSchema schema, IDictionary<string, IOpenApiSchema> schemas, Dictionary<string, string> generated)
    {
         var originalSchemaRef = (schema as OpenApiSchemaReference)?.Reference.Id;

        var isPrimitive = schema.Type != null && schema.Type != JsonSchemaType.Array && schema.Type != JsonSchemaType.Object;
        string responseSchemaKey = null;
        OpenApiSchema responseSchema = null;
        if (isPrimitive)
        {
            var typeName = GetPrimitiveTypeName(schema);
            var primitiveDescriptions = GetPrimitiveDescriptions(typeName);
            if ((schema.Type & JsonSchemaType.Null) == JsonSchemaType.Null)
            {
                typeName += "Nullable";
            }
            responseSchemaKey = $"{typeName}Wrapper";
            var primitiveResponseProperty = new OpenApiSchema
            {
                Type = schema.Type
            };
            responseSchema = CreateSuccessApiResponseSchema(primitiveResponseProperty, primitiveDescriptions);
        }
        else if (schema.OneOf != null && schema.OneOf.Any(s => (s as OpenApiSchemaReference)?.Reference.Id != null))
        {
            var firstRefId = (schema.OneOf.FirstOrDefault(s => (s as OpenApiSchemaReference)?.Reference.Id != null) as OpenApiSchemaReference)?.Reference.Id;
            responseSchemaKey = WrapperKey(firstRefId, "Wrapper", schemas, generated);
            var responseProperty = new OpenApiSchema
            {
                OneOf = schema.OneOf
            };

            responseSchema = CreateSuccessApiResponseSchema(responseProperty, GetObjectDescriptions(firstRefId));
            //schema.OneOf = null;
        }
        else if (schema.Type == JsonSchemaType.Array)
        {
            originalSchemaRef = (schema.Items as OpenApiSchemaReference)?.Reference.Id;
            var schemaArray = schema.Items;
            OpenApiSchema arrayResponseProperty;
            (string Wrapper, string Response) arrayDescriptions;

            if (schema.OneOf != null && schema.OneOf.Any(s => s.Items != null))
            {
                var firstRefId = (schema.OneOf.FirstOrDefault(s => (s.Items as OpenApiSchemaReference)?.Reference.Id != null)?.Items as OpenApiSchemaReference)?.Reference.Id;
                responseSchemaKey = WrapperKey(firstRefId, "ArrayWrapper", schemas, generated);

                arrayResponseProperty = new OpenApiSchema
                {
                    Items = new OpenApiSchema
                    {
                        OneOf = schema.OneOf
                    }
                };

                arrayDescriptions = GetArrayDescriptions(firstRefId);

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
                arrayDescriptions = GenericDescriptions;
            }
            else if (schemaArray is { Type: JsonSchemaType.Array })
            {
                responseSchemaKey = "ArrayArrayWrapper";
                arrayResponseProperty = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = new OpenApiSchema { Type = JsonSchemaType.Array, Items = new OpenApiSchema { Type = schemaArray.Items.Type } }
                };
                arrayDescriptions = GenericDescriptions;
            }
            else
            {
                responseSchemaKey = originalSchemaRef == null
                    ? $"{schema.Items.Type.ToString().ToUpper()}ArrayWrapper"
                    : WrapperKey(originalSchemaRef, "ArrayWrapper", schemas, generated);
                arrayResponseProperty = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = originalSchemaRef != null ? new OpenApiSchemaReference((schema.Items as OpenApiSchemaReference)?.Reference.Id) : new OpenApiSchema { Type = schema.Items.Type }
                };
                arrayDescriptions = GetArrayDescriptions(originalSchemaRef);
            }

            responseSchema = CreateSuccessApiResponseSchema(arrayResponseProperty, arrayDescriptions);
        }
        else if (schema == null || (schema.Type == null && (schema is not OpenApiSchemaReference) && schema.Items == null))
        {
            responseSchemaKey = "ObjectWrapper";
            if (!schemas.ContainsKey(responseSchemaKey))
            {
                responseSchema = CreateSuccessApiResponseSchema(new OpenApiSchema { Type = JsonSchemaType.Object }, GenericDescriptions);
            }
        }
        else
        {
            if (originalSchemaRef != null)
            {
                responseSchemaKey = WrapperKey(originalSchemaRef, "Wrapper", schemas, generated);
            }

            var responseProperty = originalSchemaRef != null
            ? new OpenApiSchemaReference(originalSchemaRef)
            : schema;
            responseSchema = CreateSuccessApiResponseSchema(responseProperty, GetObjectDescriptions(originalSchemaRef));
        }

        if (responseSchemaKey != null)
        {
            // The fixed names ("ObjectWrapper", "Int32Wrapper", ...) own themselves: their key holds no
            // payload id, so WrapperKey never hands one of them to a schema whose name happens to end up
            // there. TryAdd, not Add: WrapperKey has already claimed the names it derived.
            if (schemas.TryAdd(responseSchemaKey, responseSchema))
            {
                generated.TryAdd(responseSchemaKey, responseSchemaKey);
            }

            // schema.Reference = new OpenApiSchemaReference(responseSchemaKey);
            //schema.Type = null;
            //schema.Properties = null;
            return new OpenApiSchemaReference(responseSchemaKey);
        }

        return responseSchema;
    }
    
    // The wrapper name normally drops the payload's `Dto` marker - `FileDto` becomes `FileWrapper`, the
    // flattened generic `FileDtoInteger` becomes `FileIntegerWrapper`. But `components/schemas` is shared
    // with real C# classes and several of them are already called `*Wrapper`
    // (`CompanyWhiteLabelSettingsWrapper`, `TenantWalletSettingsWrapper` - request bodies). The `TryAdd`
    // above then kept the stranger and the 200 response was documented as that unrelated type, which is
    // also why its own `*Dto` schema ended up referenced by nothing.
    // So a name is taken only when it is free, or when this filter minted it for the very same payload -
    // and that is what `generated` is checked for: it maps the name back to its payload, so two payloads
    // collapsing into one candidate (`FooDto` and `Foo` both want `FooWrapper`) get a wrapper each instead
    // of the second one silently inheriting the first one's payload. The candidates are derived from the
    // payload name alone, so the choice does not depend on the order the operations happen to be visited
    // in; when all three are taken by strangers the last one is numbered until it is free, rather than
    // returned as-is - returning it would be exactly the collision this method exists to avoid.
    private static string WrapperKey(string schemaId, string suffix, IDictionary<string, IOpenApiSchema> schemas, Dictionary<string, string> generated)
    {
        // A space cannot occur in a schema id, so a payload key can never collide with the fixed names,
        // which are registered under themselves.
        var payload = $"{schemaId} {suffix}";
        var stripped = StripDtoMarker(schemaId);

        for (var attempt = 0; ; attempt++)
        {
            var candidate = attempt switch
            {
                0 => stripped + suffix,
                1 => schemaId + suffix,
                2 => schemaId + "Response" + suffix,
                _ => $"{schemaId}Response{suffix}{attempt - 1}"
            };

            if (generated.TryGetValue(candidate, out var owner))
            {
                if (owner == payload)
                {
                    return candidate;
                }

                continue;
            }

            if (schemas.ContainsKey(candidate))
            {
                continue;
            }

            generated[candidate] = payload;

            return candidate;
        }
    }

    // `Dto` is a marker of a payload type name, not a word: it is cut where a type name can end - at the
    // end of the id, or in front of the next capital, as in the flattened generic `FileDtoInteger`. Never
    // at the start and never inside a word, so `DtoFileDto` becomes `DtoFile` and not `File`.
    private static string StripDtoMarker(string schemaId)
    {
        var stripped = schemaId;

        for (var i = stripped.Length - 3; i > 0; i--)
        {
            if (!stripped.AsSpan(i).StartsWith("Dto", StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 3 == stripped.Length || char.IsUpper(stripped[i + 3]))
            {
                stripped = stripped.Remove(i, 3);
            }
        }

        return stripped;
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

    // Texts used when the wrapped payload has no name to derive one from: an inline object, an array of
    // anonymous items, an array of arrays or an array of primitives.
    private const string GenericWrapperDescription = "The successful API response.";
    private const string GenericResponseDescription = "The response payload.";

    // A wrapper needs two descriptions: one for the envelope schema itself and one for its `response`
    // property. Both are derived from the same payload name, so they are produced together to keep them
    // from drifting apart.
    private static (string Wrapper, string Response) GenericDescriptions => (GenericWrapperDescription, GenericResponseDescription);

    private static (string Wrapper, string Response) GetObjectDescriptions(string schemaId)
    {
        return string.IsNullOrEmpty(schemaId)
            ? GenericDescriptions
            : ($"The successful API response containing the {schemaId} object.", $"The {schemaId} object returned by the operation.");
    }

    private static (string Wrapper, string Response) GetArrayDescriptions(string schemaId)
    {
        return string.IsNullOrEmpty(schemaId)
            ? GenericDescriptions
            : ($"The successful API response containing the list of {schemaId} objects.", $"The list of {schemaId} objects returned by the operation.");
    }

    private static (string Wrapper, string Response) GetPrimitiveDescriptions(string typeName)
    {
        return typeName == "Unknown"
            ? GenericDescriptions
            : ($"The successful API response containing the {typeName.ToLowerInvariant()} value.", $"The {typeName.ToLowerInvariant()} value returned by the operation.");
    }

    private static OpenApiSchema CreateSuccessApiResponseSchema(IOpenApiSchema responseProperty, (string Wrapper, string Response) descriptions)
    {
        // Both the wrapper and its `response` are described here: the ~200 generated wrapper schemas have
        // no C# class of their own, so this factory is the only place a description can be written once.
        // For a `$ref` payload the text lands on the reference itself and is emitted as a sibling of
        // `$ref` - valid in OAS 3.1 and the only form that reaches the generated SDK comment. It is set
        // unconditionally there on purpose: the getter falls through to the referenced schema, so an
        // "only if empty" check would read the target's description and never write the sibling - which
        // is the very gap this closes. An inline schema, in contrast, may be the response schema itself
        // and carry a real description already; that one is left alone.
        switch (responseProperty)
        {
            case OpenApiSchema inlineResponse when string.IsNullOrEmpty(inlineResponse.Description):
                inlineResponse.Description = descriptions.Response;
                break;
            case OpenApiSchemaReference referencedResponse:
                referencedResponse.Description = descriptions.Response;
                break;
        }

        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Description = descriptions.Wrapper,
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