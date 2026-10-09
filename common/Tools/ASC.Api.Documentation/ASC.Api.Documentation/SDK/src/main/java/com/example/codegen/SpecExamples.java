/*
 * (c) Copyright Ascensio System SIA 2026
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package com.example.codegen;

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.PathItem;
import io.swagger.v3.oas.models.media.Content;
import io.swagger.v3.oas.models.media.MediaType;
import io.swagger.v3.oas.models.media.Schema;
import io.swagger.v3.oas.models.parameters.Parameter;
import io.swagger.v3.oas.models.parameters.RequestBody;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.CodegenParameter;
import org.openapitools.codegen.utils.ModelUtils;

import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;
import java.util.List;
import java.util.Locale;
import java.util.Map;

/**
 * The examples the document actually states, read straight from it.
 * <p>
 * The generators' own example fields cannot be used: when the document states no example a
 * generator invents one (56 for integers, and so on), and nothing distinguishes the two once they
 * are in that field. Every generator that prints an example - the reference pages, the call
 * snippets of the languages - reads it from here, so they all agree on what the document states.
 */
final class SpecExamples {

    private SpecExamples() {
    }

    /**
     * The example a property of a declared schema states, or null. Only the schema's own
     * properties are looked at.
     */
    static Object property(OpenAPI openAPI, String schemaName, String propertyName) {
        Schema<?> schema = schema(openAPI, schemaName);
        if (schema == null || schema.getProperties() == null) {
            return null;
        }

        Object property = schema.getProperties().get(propertyName);
        if (!(property instanceof Schema)) {
            return null;
        }

        return schemaExample((Schema<?>) property);
    }

    /**
     * The example a property states, looked up through the schemas an allOf schema is composed of
     * when the schema does not declare the property itself.
     */
    static Object inheritedProperty(OpenAPI openAPI, String schemaName, String propertyName) {
        return inheritedProperty(openAPI, schema(openAPI, schemaName), propertyName, 0);
    }

    private static Object inheritedProperty(OpenAPI openAPI, Schema<?> schema, String propertyName, int depth) {
        if (schema == null || depth > 8) {
            return null;
        }

        if (schema.get$ref() != null) {
            return inheritedProperty(openAPI, schema(openAPI, ModelUtils.getSimpleRef(schema.get$ref())), propertyName, depth + 1);
        }

        if (schema.getProperties() != null && schema.getProperties().get(propertyName) instanceof Schema) {
            return schemaExample((Schema<?>) schema.getProperties().get(propertyName));
        }

        if (schema.getAllOf() != null) {
            for (Object part : schema.getAllOf()) {
                if (part instanceof Schema) {
                    Object example = inheritedProperty(openAPI, (Schema<?>) part, propertyName, depth + 1);
                    if (example != null) {
                        return example;
                    }
                }
            }
        }

        return null;
    }

    /**
     * The example a schema states, in either spelling.
     * <p>
     * OpenAPI 3.1 replaced the single `example` with an `examples` array, and the documents the
     * services emit use the array form. Reading `example` alone leaves the pages with no examples
     * at all, which looks exactly like a document that states none.
     */
    static Object schemaExample(Schema<?> schema) {
        if (schema == null) {
            return null;
        }

        if (schema.getExample() != null) {
            return schema.getExample();
        }

        List<?> examples = schema.getExamples();
        if (examples != null) {
            for (Object example : examples) {
                if (example != null) {
                    return example;
                }
            }
        }

        return null;
    }

    /**
     * The example a parameter states, or null.
     * <p>
     * Matched on name and location together: an operation may carry two parameters of the same
     * name in different places - `tagName` in the path and in the query, for one - and matching
     * on the name alone would hand one parameter's example to the other.
     */
    static Object parameter(OpenAPI openAPI, CodegenOperation operation, String parameterName, String location) {
        Operation raw = rawOperation(openAPI, operation);
        if (raw == null || raw.getParameters() == null) {
            return null;
        }

        // The third-party twin shares the path and the method of its operation, so the lookup below would
        // hand it the portal parameter's example ("1" for an id that is "sbox-42" there). What the twin
        // changes is stated in the extension, and that is where its examples are.
        if (operation.vendorExtensions.containsKey(ThirdPartyVariants.IS_VARIANT)) {
            Object variantExample = variantParameter(raw, parameterName, location);
            if (variantExample != null) {
                return variantExample;
            }
        }

        for (Parameter parameter : raw.getParameters()) {
            if (!parameterName.equals(parameter.getName())) {
                continue;
            }
            if (location != null && !location.isEmpty() && !location.equals(parameter.getIn())) {
                continue;
            }

            if (parameter.getExample() != null) {
                return parameter.getExample();
            }

            Object schemaExample = schemaExample(parameter.getSchema());
            if (schemaExample != null) {
                return schemaExample;
            }
        }

        return null;
    }

    /**
     * The example a field of a form body states: a property of the schema the multipart or
     * urlencoded content declares.
     */
    static Object formField(OpenAPI openAPI, CodegenOperation operation, String fieldName) {
        Operation raw = rawOperation(openAPI, operation);
        if (raw == null || raw.getRequestBody() == null) {
            return null;
        }

        Content content = raw.getRequestBody().getContent();
        if (content == null) {
            return null;
        }

        for (Map.Entry<String, MediaType> entry : content.entrySet()) {
            String mediaType = entry.getKey().toLowerCase(Locale.ROOT);
            if (!mediaType.startsWith("multipart/") && !mediaType.equals("application/x-www-form-urlencoded")) {
                continue;
            }

            Schema<?> schema = entry.getValue().getSchema();
            if (schema != null && schema.get$ref() != null) {
                schema = schema(openAPI, ModelUtils.getSimpleRef(schema.get$ref()));
            }

            Object example = inheritedProperty(openAPI, schema, fieldName, 0);
            if (example != null) {
                return example;
            }
        }

        return null;
    }

    /**
     * The example the schema of a JSON request body states, or null: the one written next to the
     * schema in the operation, or else the one stated by the schema it refers to.
     */
    static Object requestBody(OpenAPI openAPI, CodegenOperation operation) {
        Operation raw = rawOperation(openAPI, operation);
        if (raw == null || raw.getRequestBody() == null) {
            return null;
        }

        // The generator moves request bodies under components and leaves a reference in their place.
        RequestBody requestBody = ModelUtils.getReferencedRequestBody(openAPI, raw.getRequestBody());
        if (requestBody == null || requestBody.getContent() == null) {
            return null;
        }

        for (Map.Entry<String, MediaType> entry : requestBody.getContent().entrySet()) {
            Schema<?> schema = entry.getValue().getSchema();
            if (!entry.getKey().toLowerCase(Locale.ROOT).contains("json") || schema == null) {
                continue;
            }

            Object example = schemaExample(schema);
            return example != null ? example : schemaExample(ModelUtils.getReferencedSchema(openAPI, schema));
        }

        return null;
    }

    /**
     * The example the third-party twin of an operation states for a parameter it changes, read from the
     * {@code x-thirdparty-variant} extension; null when the twin leaves that parameter as it is.
     */
    private static Object variantParameter(Operation raw, String parameterName, String location) {
        Object extension = raw.getExtensions() == null ? null : raw.getExtensions().get(ThirdPartyVariants.EXTENSION);
        if (!(extension instanceof Map)) {
            return null;
        }

        Object parameters = ((Map<?, ?>) extension).get("parameters");
        if (!(parameters instanceof List)) {
            return null;
        }

        for (Object item : (List<?>) parameters) {
            if (!(item instanceof Map)) {
                continue;
            }

            Map<?, ?> parameter = (Map<?, ?>) item;
            if (!parameterName.equals(parameter.get("name"))) {
                continue;
            }
            if (location != null && !location.isEmpty() && !location.equals(parameter.get("in"))) {
                continue;
            }

            if (parameter.get("example") != null) {
                return parameter.get("example");
            }

            if (parameter.get("schema") instanceof Map) {
                Map<?, ?> schema = (Map<?, ?>) parameter.get("schema");
                if (schema.get("example") != null) {
                    return schema.get("example");
                }
                if (schema.get("examples") instanceof List) {
                    for (Object example : (List<?>) schema.get("examples")) {
                        if (example != null) {
                            return example;
                        }
                    }
                }
            }
        }

        return null;
    }

    /**
     * Renders an example value. Date-times arrive already parsed, and their toString() drops
     * zero seconds - "2025-01-01T00:00Z" instead of the "2025-01-01T00:00:00Z" the document
     * spells out - so they are formatted back to full ISO-8601.
     */
    static String text(Object example) {
        if (example == null) {
            return null;
        }

        if (example instanceof OffsetDateTime) {
            return ((OffsetDateTime) example).format(DateTimeFormatter.ISO_OFFSET_DATE_TIME);
        }

        return String.valueOf(example);
    }

    /** Where a parameter travels: the `in` field of the OpenAPI document. */
    static String location(CodegenParameter parameter) {
        if (parameter.isPathParam) {
            return "path";
        }
        if (parameter.isQueryParam) {
            return "query";
        }
        if (parameter.isHeaderParam) {
            return "header";
        }
        if (parameter.isCookieParam) {
            return "cookie";
        }
        if (parameter.isBodyParam) {
            return "body";
        }
        if (parameter.isFormParam) {
            return "form";
        }

        return "";
    }

    private static Schema<?> schema(OpenAPI openAPI, String name) {
        if (openAPI == null || name == null || openAPI.getComponents() == null || openAPI.getComponents().getSchemas() == null) {
            return null;
        }

        return openAPI.getComponents().getSchemas().get(name);
    }

    private static Operation rawOperation(OpenAPI openAPI, CodegenOperation operation) {
        if (openAPI == null || openAPI.getPaths() == null || operation.httpMethod == null) {
            return null;
        }

        PathItem pathItem = openAPI.getPaths().get(operation.path);
        if (pathItem == null) {
            return null;
        }

        return pathItem.readOperationsMap()
                .get(PathItem.HttpMethod.valueOf(operation.httpMethod.toUpperCase(Locale.ROOT)));
    }
}
