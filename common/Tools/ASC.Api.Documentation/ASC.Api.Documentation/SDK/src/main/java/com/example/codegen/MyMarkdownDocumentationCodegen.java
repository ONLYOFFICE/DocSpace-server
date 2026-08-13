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

import org.openapitools.codegen.languages.MarkdownDocumentationCodegen;
import org.openapitools.codegen.model.*;
import org.openapitools.codegen.*;
import io.swagger.v3.oas.models.*;
import io.swagger.v3.oas.models.media.*;
import io.swagger.v3.oas.models.parameters.*;
import io.swagger.v3.oas.models.security.*;
import io.swagger.v3.oas.models.servers.*;

import java.time.*;
import java.time.format.*;
import java.util.*;

public class MyMarkdownDocumentationCodegen extends MarkdownDocumentationCodegen {

    /**
     * Class name to in-document anchor, for every model that gets a section of its own. Types
     * absent from this map get no link: the stock templates point every non-primitive type at a
     * page without checking, which yields dead links for primitives and empty ones for inline
     * enums. The anchor follows the section heading, which for inline schemas is not the class
     * name, so the two cannot be derived independently.
     */
    private final Map<String, String> documentedModels = new HashMap<>();

    private static final String DECLARED_SCHEMA = "x-declared-schema";

    private static final String MODEL_DOC = "x-model-doc";
    private static final String RETURN_MODEL_DOC = "x-return-model-doc";

    private static final String ANCHOR = "x-anchor";
    private static final String PARAM_IN = "x-param-in";
    private static final String NOTES = "x-notes";
    private static final String SCOPES = "x-scopes";
    private static final String TITLE = "x-title";
    private static final String MODEL_ANCHOR = "x-model-anchor";
    private static final String HAS_PROPERTIES = "x-has-properties";
    private static final String ENUM_DOC = "x-enum-doc";
    private static final String RETURN_MODEL_ANCHOR = "x-return-model-anchor";

    /**
     * Fragment for a heading, following the slug rule GitHub and VS Code share: lower case,
     * punctuation dropped, spaces turned into hyphens. Anchors have to be derived from the
     * heading text rather than written as &lt;a name&gt; tags, because editors and forges
     * resolve in-document links against headings only.
     */
    private static String slug(String heading) {
        StringBuilder builder = new StringBuilder(heading.length());

        for (char ch : heading.toLowerCase(Locale.ROOT).toCharArray()) {
            if (Character.isLetterOrDigit(ch) || ch == '_' || ch == '-') {
                builder.append(ch);
            } else if (ch == ' ') {
                builder.append('-');
            }
        }

        return builder.toString();
    }

    /**
     * Models get their own prefix so an operation and a model of the same name cannot claim the
     * same fragment - `updateFile` and `UpdateFile` collide otherwise, and the renderer then
     * silently renumbers one of them.
     */
    private static String modelAnchor(String title) {
        return "model-" + slug(title);
    }

    /**
     * Heading for a model page.
     * <p>
     * Schemas the document declares keep their name. Schemas defined inline - in a property, a
     * request body or a response - have no name of their own, so openapi-generator synthesizes one
     * by joining the context with underscores: `AppDto_settings`, `getPortalPrices_200_response`,
     * `BaseBatchRequestDto_allOf_fileIds`. Those read as noise on a documentation page, so the
     * convention is unwound into something a reader can place.
     */
    private static String modelTitle(CodegenModel model) {
        if (model.vendorExtensions.containsKey(DECLARED_SCHEMA) || !model.name.contains("_")) {
            return model.classname;
        }

        String title = model.name.replace("_allOf_", "_");

        title = title.replaceAll("_(\\d{3})_response", " $1 response");

        if (title.endsWith("_request")) {
            title = title.substring(0, title.length() - "_request".length()) + " request body";
        }

        int items = 0;
        while (title.endsWith("_inner")) {
            title = title.substring(0, title.length() - "_inner".length());
            items++;
        }

        title = title.replace('_', '.');

        for (int i = 0; i < items; i++) {
            title = title + " item";
        }

        return title;
    }

    /**
     * Descriptions end up inside Markdown tables, where an unescaped pipe starts a new cell and
     * a newline ends the row - either one silently mangles the table from that point on.
     */
    private static String tableText(String text) {
        if (text == null) {
            return null;
        }

        return text.replace("|", "\\|")
                .replaceAll("\\r\\n|\\r|\\n", "<br>")
                .trim();
    }

    /**
     * Builds the Notes cell from what the document actually states. Assembled here rather than in
     * the template so that placeholder values never reach the page: the generator fills
     * defaultValue with the literal string "null" for anything without a default, and printing
     * that would claim a default the document does not have.
     */
    private static String notes(
            boolean required,
            String example,
            String defaultValue,
            Map<String, Object> allowableValues,
            String minimum,
            String maximum,
            Integer minLength,
            Integer maxLength,
            String pattern,
            boolean nullable) {
        List<String> notes = new ArrayList<>();

        notes.add(required ? "[required]" : "[optional]");

        if (isStated(example)) {
            notes.add("[example: " + tableText(example) + "]");
        }
        if (isStated(defaultValue)) {
            notes.add("[default to " + tableText(defaultValue) + "]");
        }
        Object rawValues = allowableValues == null ? null : allowableValues.get("values");
        if (rawValues instanceof List) {
            List<?> values = (List<?>) rawValues;
            if (!values.isEmpty()) {
                StringJoiner joiner = new StringJoiner(", ");
                for (Object value : values) {
                    joiner.add(String.valueOf(value));
                }
                notes.add("[enum: " + tableText(joiner.toString()) + "]");
            }
        }
        if (isStated(minimum)) {
            notes.add("[min: " + minimum + "]");
        }
        if (isStated(maximum)) {
            notes.add("[max: " + maximum + "]");
        }
        if (minLength != null) {
            notes.add("[minLength: " + minLength + "]");
        }
        if (maxLength != null) {
            notes.add("[maxLength: " + maxLength + "]");
        }
        if (isStated(pattern)) {
            notes.add("[pattern: " + tableText(pattern) + "]");
        }
        if (nullable) {
            notes.add("[nullable]");
        }

        return String.join(" ", notes);
    }

    /**
     * The example a property actually states, read straight from the document.
     * <p>
     * CodegenProperty.example cannot be used: when the document states no example the generator
     * invents one (56 for integers, and so on), and the page would then present a fabricated
     * value as documentation. Nothing distinguishes the two once they are in that field.
     */
    private String statedExample(String modelName, String propertyName) {
        if (openAPI == null || openAPI.getComponents() == null || openAPI.getComponents().getSchemas() == null) {
            return null;
        }

        Schema<?> schema = openAPI.getComponents().getSchemas().get(modelName);
        if (schema == null || schema.getProperties() == null) {
            return null;
        }

        Object property = schema.getProperties().get(propertyName);
        if (!(property instanceof Schema)) {
            return null;
        }

        return exampleText(schemaExample((Schema<?>) property));
    }

    /**
     * The example a schema states, in either spelling.
     * <p>
     * OpenAPI 3.1 replaced the single `example` with an `examples` array, and the documents the
     * services emit use the array form. Reading `example` alone leaves the pages with no examples
     * at all, which looks exactly like a document that states none.
     */
    private static Object schemaExample(Schema<?> schema) {
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
     * The example a parameter actually states, read straight from the document.
     * <p>
     * Matched on name and location together: an operation may carry two parameters of the same
     * name in different places - `tagName` in the path and in the query, for one - and matching
     * on the name alone would hand one parameter's example to the other.
     */
    private String statedExample(CodegenOperation operation, String parameterName, String location) {
        if (openAPI == null || openAPI.getPaths() == null || operation.httpMethod == null) {
            return null;
        }

        PathItem pathItem = openAPI.getPaths().get(operation.path);
        if (pathItem == null) {
            return null;
        }

        Operation raw = pathItem.readOperationsMap()
                .get(PathItem.HttpMethod.valueOf(operation.httpMethod.toUpperCase(Locale.ROOT)));
        if (raw == null || raw.getParameters() == null) {
            return null;
        }

        for (Parameter parameter : raw.getParameters()) {
            if (!parameterName.equals(parameter.getName())) {
                continue;
            }
            if (location != null && !location.isEmpty() && !location.equals(parameter.getIn())) {
                continue;
            }

            if (parameter.getExample() != null) {
                return exampleText(parameter.getExample());
            }

            Object schemaExample = schemaExample(parameter.getSchema());
            if (schemaExample != null) {
                return exampleText(schemaExample);
            }
        }

        return null;
    }

    /**
     * Renders an example value. Date-times arrive already parsed, and their toString() drops
     * zero seconds - "2025-01-01T00:00Z" instead of the "2025-01-01T00:00:00Z" the document
     * spells out - so they are formatted back to full ISO-8601.
     */
    private static String exampleText(Object example) {
        if (example == null) {
            return null;
        }

        if (example instanceof OffsetDateTime) {
            return ((OffsetDateTime) example).format(DateTimeFormatter.ISO_OFFSET_DATE_TIME);
        }

        return String.valueOf(example);
    }

    /** Scopes the operation requires from this scheme, empty when it requires none. */
    private static String scopeList(CodegenSecurity security) {
        if (security.scopes == null || security.scopes.isEmpty()) {
            return "";
        }

        StringJoiner joiner = new StringJoiner(", ");
        for (Map<String, Object> scope : security.scopes) {
            Object name = scope.get("scope");
            if (name != null) {
                joiner.add(String.valueOf(name));
            }
        }

        return joiner.toString();
    }

    /** "null" is the generator's placeholder for absent, not a value the document states. */
    private static boolean isStated(String value) {
        return value != null && !value.isEmpty() && !"null".equals(value);
    }

    /** Where a parameter travels: the `in` field of the OpenAPI document. */
    private static String parameterLocation(CodegenParameter parameter) {
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

    public MyMarkdownDocumentationCodegen() {
        super();
        this.templateDir = "templates/markdown";
        this.embeddedTemplateDir = "markdown-documentation";
    }

    @Override
    public void processOpts() {
        super.processOpts();

        // One document per service instead of a page per tag and per model. The name comes
        // from the split sub-document the run was given, so it matches the json/*.json layout.
        Object documentName = additionalProperties.get("documentName");
        if (documentName != null && !documentName.toString().isEmpty()) {
            // This generator emits its pages through apiTemplateFiles/modelTemplateFiles - the
            // *DocTemplateFiles maps it inherits stay empty, which is also why the apiDocs and
            // modelDocs global properties have no effect here. Clearing these stops the per-tag
            // and per-model pages while operations and models still reach the supporting file.
            apiTemplateFiles.clear();
            modelTemplateFiles.clear();
            supportingFiles.clear();
            supportingFiles.add(new SupportingFile("service.mustache", "", documentName + ".md"));
        }

        // The page heading and the server URL are not fixed up here: they are written into the
        // sub-document before the generator is started (see GenerateMarkdownDocsCommand). They
        // cannot travel as --additional-properties, because openapi-generator-cli is an npm shim
        // that re-spawns java through a shell, and a value containing spaces does not survive it.
    }

    @Override
    public Map<String, ModelsMap> postProcessAllModels(Map<String, ModelsMap> objs) {
        Map<String, ModelsMap> results = super.postProcessAllModels(objs);

        // Titles first: a cross-link has to point at the heading the page ends up with, and for
        // inline schemas that heading is not the class name.
        for (ModelsMap models : results.values()) {
            for (ModelMap modelMap : models.getModels()) {
                CodegenModel model = modelMap.getModel();
                String title = modelTitle(model);
                model.vendorExtensions.put(TITLE, title);
                documentedModels.put(model.classname, modelAnchor(title));
            }
        }

        for (ModelsMap models : results.values()) {
            for (ModelMap modelMap : models.getModels()) {
                CodegenModel model = modelMap.getModel();
                model.vendorExtensions.put(ANCHOR, documentedModels.get(model.classname));

                // Enums and other property-less schemas would otherwise be given a table header
                // with no rows under it - an empty frame that reads as missing documentation
                // rather than as a model that has no properties to list.
                model.vendorExtensions.put(HAS_PROPERTIES, hasProperties(model));

                markEnumValues(model);

                markProperties(model.name, model.vars);
                markProperties(model.parent, model.parentVars);
                markProperties(model.name, model.allVars);
            }
        }

        return results;
    }

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        OperationsMap results = super.postProcessOperationsWithModels(objs, allModels);

        if (allModels != null) {
            for (ModelMap modelMap : allModels) {
                CodegenModel model = modelMap.getModel();
                if (!documentedModels.containsKey(model.classname)) {
                    String title = modelTitle(model);
                    model.vendorExtensions.put(TITLE, title);
                    documentedModels.put(model.classname, modelAnchor(title));
                }
            }
        }

        for (CodegenOperation operation : results.getOperations().getOperation()) {
            operation.vendorExtensions.put(ANCHOR, slug(operation.operationId));

            if (operation.allParams != null) {
                for (CodegenParameter parameter : operation.allParams) {
                    String location = parameterLocation(parameter);
                    parameter.description = tableText(parameter.description);
                    parameter.vendorExtensions.put(PARAM_IN, location);
                    parameter.vendorExtensions.put(NOTES, notes(
                            parameter.required,
                            statedExample(operation, parameter.baseName, location),
                            parameter.defaultValue,
                            parameter.allowableValues,
                            parameter.minimum,
                            parameter.maximum,
                            parameter.minLength,
                            parameter.maxLength,
                            parameter.pattern,
                            parameter.isNullable));

                    if (isDocumented(parameter.baseType)) {
                        parameter.vendorExtensions.put(MODEL_DOC, parameter.baseType);
                        parameter.vendorExtensions.put(MODEL_ANCHOR, documentedModels.get(parameter.baseType));
                    }
                }
            }

            if (operation.responses != null) {
                for (CodegenResponse response : operation.responses) {
                    response.message = tableText(response.message);

                    if (isDocumented(response.baseType)) {
                        response.vendorExtensions.put(MODEL_ANCHOR, documentedModels.get(response.baseType));
                    }
                }
            }

            if (operation.authMethods != null) {
                for (CodegenSecurity security : operation.authMethods) {
                    security.vendorExtensions.put(ANCHOR, slug(security.name));

                    // Absent rather than empty: the template engine treats "" as present and
                    // would render a bare "(scopes: )" for schemes that require none.
                    String scopes = scopeList(security);
                    if (!scopes.isEmpty()) {
                        security.vendorExtensions.put(SCOPES, scopes);
                    } else {
                        security.vendorExtensions.remove(SCOPES);
                    }
                }
            }

            if (isDocumented(operation.returnBaseType)) {
                operation.vendorExtensions.put(RETURN_MODEL_DOC, operation.returnBaseType);
                operation.vendorExtensions.put(RETURN_MODEL_ANCHOR, documentedModels.get(operation.returnBaseType));
            }
        }

        return results;
    }

    @Override
    public List<CodegenSecurity> fromSecurity(Map<String, SecurityScheme> securitySchemes) {
        List<CodegenSecurity> securities = super.fromSecurity(securitySchemes);

        if (securities != null) {
            for (CodegenSecurity security : securities) {
                security.vendorExtensions.put(ANCHOR, slug(security.name));
            }
        }

        return securities;
    }

    /**
     * Whether the model has a row to put in its properties table. Inherited properties count:
     * the template prints those too, so a model that only carries a parent's fields still has
     * something to show.
     */
    private static boolean hasProperties(CodegenModel model) {
        if (model.vars != null && !model.vars.isEmpty()) {
            return true;
        }

        return model.parent != null && model.parentVars != null && !model.parentVars.isEmpty();
    }

    /**
     * Turns an enum model into a list the page can print: the wire value, what it means, and the
     * constant name the SDKs expose for it.
     * <p>
     * Without this an enum shows nothing but its raw description, because an enum has no
     * properties and the properties table is all the section otherwise contains.
     */
    private void markEnumValues(CodegenModel model) {
        if (model.allowableValues == null
                || !(model.allowableValues.get("values") instanceof List)
                || ((List<?>) model.allowableValues.get("values")).isEmpty()) {
            return;
        }

        List<?> values = (List<?>) model.allowableValues.get("values");
        List<String> names = enumVarNames(model, values.size());
        List<String> labels = parseEnumLabels(model.description, values);

        List<Map<String, String>> entries = new ArrayList<>();

        for (int i = 0; i < values.size(); i++) {
            Map<String, String> entry = new HashMap<>();
            entry.put("enumValue", String.valueOf(values.get(i)));

            if (labels != null) {
                entry.put("enumLabel", labels.get(i));
            }

            String name = names == null ? null : names.get(i);
            if (name != null && !name.isEmpty()) {
                entry.put("enumConstant", name);
            }

            entries.add(entry);
        }

        Map<String, Object> enumDoc = new HashMap<>();
        enumDoc.put("values", entries);
        model.vendorExtensions.put(ENUM_DOC, enumDoc);

        // The bracketed description is the same list written as prose. Once the list is rendered
        // in full, repeating it above it is noise - but only then. A few schemas state "[]", a
        // label list with nothing in it, which is noise either way.
        if (labels != null || "[]".equals(model.description == null ? null : model.description.trim())) {
            model.description = null;
        }
    }

    /**
     * The constant names the document states in `x-enum-varnames`, read straight from the schema.
     * <p>
     * Neither `enumVars` nor the model's own copy of the extension can be used: this generator
     * inherits `toEnumVarName` from the Markdown codegen, which names every member after the
     * model, and the generator writes that back - so both would label all six members of RoomType
     * "RoomType", and would invent names for enums that state none at all.
     */
    private List<String> enumVarNames(CodegenModel model, int expected) {
        if (openAPI == null || openAPI.getComponents() == null || openAPI.getComponents().getSchemas() == null) {
            return null;
        }

        Schema<?> schema = openAPI.getComponents().getSchemas().get(model.name);
        if (schema == null || schema.getExtensions() == null
                || !(schema.getExtensions().get("x-enum-varnames") instanceof List)) {
            return null;
        }

        List<?> varNames = (List<?>) schema.getExtensions().get("x-enum-varnames");
        if (varNames.size() != expected) {
            return null;
        }

        List<String> names = new ArrayList<>();

        for (Object varName : varNames) {
            names.add(varName == null ? null : String.valueOf(varName));
        }

        return names;
    }

    /**
     * The labels the document hides in the description: `[1 - Form filling room, 2 - ...]`.
     * <p>
     * Deliberately all-or-nothing. The format is not a contract - a label containing ", " splits
     * into the wrong number of entries, and some descriptions are ordinary prose - so the parse is
     * accepted only when every entry lines up with the value it claims to describe. Anything else
     * falls back to printing the values alone, which is worse but never wrong.
     */
    private static List<String> parseEnumLabels(String description, List<?> values) {
        if (description == null) {
            return null;
        }

        String text = description.trim();
        if (text.length() < 2 || text.charAt(0) != '[' || text.charAt(text.length() - 1) != ']') {
            return null;
        }

        String[] parts = text.substring(1, text.length() - 1).split(", ");
        if (parts.length != values.size()) {
            return null;
        }

        List<String> labels = new ArrayList<>();

        for (int i = 0; i < parts.length; i++) {
            int separator = parts[i].indexOf(" - ");
            if (separator < 0 || !parts[i].substring(0, separator).equals(String.valueOf(values.get(i)))) {
                return null;
            }

            labels.add(parts[i].substring(separator + 3));
        }

        return labels;
    }

    private void markProperties(String modelName, List<CodegenProperty> properties) {
        if (properties == null) {
            return;
        }

        for (CodegenProperty property : properties) {
            property.description = tableText(property.description);
            property.vendorExtensions.put(NOTES, notes(
                    property.required,
                    modelName == null ? null : statedExample(modelName, property.baseName),
                    property.defaultValue,
                    property.allowableValues,
                    property.minimum,
                    property.maximum,
                    property.minLength,
                    property.maxLength,
                    property.pattern,
                    property.isNullable));

            if (isDocumented(property.complexType)) {
                property.vendorExtensions.put(MODEL_DOC, property.complexType);
                property.vendorExtensions.put(MODEL_ANCHOR, documentedModels.get(property.complexType));
            }
        }
    }

    private boolean isDocumented(String type) {
        return type != null && !type.isEmpty() && documentedModels.containsKey(type);
    }

    @Override
    public String getName() {
        return "my-markdown";
    }

    @Override
    public String getHelp() {
        return "Generates custom Markdown API documentation.";
    }
}
