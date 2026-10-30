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
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.*;
import io.swagger.v3.oas.models.tags.*;

import java.math.BigDecimal;
import java.nio.charset.StandardCharsets;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.*;
import java.util.regex.Pattern;

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
    private static final String SECTION_TITLE = "sectionTitle";
    private static final String ENUM_DOC = "x-enum-doc";
    private static final String RETURN_MODEL_ANCHOR = "x-return-model-anchor";

    /**
     * The calls over HTTP of the example tabs instead of the pages: one file, with a call per
     * operation (see CallSnippets.configure). Such a call has no SDK to be generated from and lives
     * on these pages only, so it is rendered by the generator of the pages.
     */
    private boolean snippetsOnly;

    /**
     * The tab the calls are rendered for, as the command passes it: "javascript" has them written as
     * JavaScript fetch calls, any other as cURL commands.
     */
    private static final String SNIPPETS_TAB = "snippetsTab";

    /** The language the template marks the code block with. */
    private static final String SNIPPETS_FENCE = "snippetsFence";

    private Snippets snippets;

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
     * <p>
     * The break is written closed, because the pages are compiled as MDX, where an unclosed tag
     * is an error rather than a line break.
     */
    private static String tableText(String text) {
        if (text == null) {
            return null;
        }

        return text
                .replace("|", "\\|")
                .replaceAll("\\r\\n|\\r|\\n", "<br/>")
                .trim();
    }

    /**
     * A note's value, printed as a code span. These are literals the document states rather than
     * prose, and read as markup as soon as they are printed as prose: a domain regex such as
     * `[a-z0-9]([a-z0-9-])` is a Markdown link, and a JSON example is an MDX expression.
     * <p>
     * A line break becomes a space rather than a tag, because a tag inside a code span prints as
     * its own source.
     */
    private static String codeText(String text) {
        if (text == null) {
            return null;
        }

        String value = text
                .replaceAll("\\r\\n|\\r|\\n", " ")
                .replace("|", "\\|")
                .trim();

        // A code span is fenced by a longer run of backticks than any run inside it, and one that
        // starts or ends with a backtick needs a space to keep its own text off the fence.
        int longest = 0;
        int run = 0;

        for (int i = 0; i < value.length(); i++) {
            run = value.charAt(i) == '`' ? run + 1 : 0;
            longest = Math.max(longest, run);
        }

        String fence = "`".repeat(longest + 1);
        String padding = value.startsWith("`") || value.endsWith("`") ? " " : "";

        return fence + padding + value + padding + fence;
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
            notes.add("[example: " + codeText(example) + "]");
        }
        if (isStated(defaultValue)) {
            notes.add("[default to " + codeText(defaultValue) + "]");
        }
        Object rawValues = allowableValues == null ? null : allowableValues.get("values");
        if (rawValues instanceof List) {
            List<?> values = (List<?>) rawValues;
            if (!values.isEmpty()) {
                StringJoiner joiner = new StringJoiner(", ");
                for (Object value : values) {
                    joiner.add(codeText(String.valueOf(value)));
                }
                notes.add("[enum: " + joiner + "]");
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
            notes.add("[pattern: " + codeText(pattern) + "]");
        }
        if (nullable) {
            notes.add("[nullable]");
        }

        return String.join(" ", notes);
    }

    /**
     * The example a property actually states, read straight from the document (see SpecExamples:
     * the generator invents one where the document states none).
     */
    private String statedExample(String modelName, String propertyName) {
        return SpecExamples.text(SpecExamples.property(openAPI, modelName, propertyName));
    }

    /**
     * The example a parameter actually states, read straight from the document (see SpecExamples).
     */
    private String statedExample(CodegenOperation operation, String parameterName, String location) {
        return SpecExamples.text(SpecExamples.parameter(openAPI, operation, parameterName, location));
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
        return SpecExamples.location(parameter);
    }

    public MyMarkdownDocumentationCodegen() {
        super();
        this.templateDir = "templates/markdown";
        this.embeddedTemplateDir = "markdown-documentation";
    }

    @Override
    public void processOpts() {
        super.processOpts();

        snippetsOnly = CallSnippets.configure(this);
        if (snippetsOnly) {
            additionalProperties.put(SNIPPETS_FENCE, isFetch() ? "javascript" : "bash");
            return;
        }

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

            // The models are rendered once, into a document that is cut into a page per model:
            // a schema is exchanged by six endpoints on average, and printing it on each of them
            // buried the endpoint's own documentation under types the reader had already read.
            boolean modelsOnly = Boolean.parseBoolean(String.valueOf(additionalProperties.get("modelsOnly")));

            supportingFiles.add(new SupportingFile(
                    modelsOnly ? "models.mustache" : "service.mustache", "", documentName + ".md"));
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

    /**
     * The heading a group of operations is printed under. A document holds one section of the
     * API, so its groups are that section's sub-tags: "Files / Folders" prints as "Folders", and
     * a tag without sub-tags prints as it stands.
     * <p>
     * The generated class name is not used for this: "FilesFoldersApi" is this generator's own
     * bookkeeping, and the page is read by people who only ever see the tags.
     */
    private static String sectionTitle(OperationsMap results) {
        for (CodegenOperation operation : results.getOperations().getOperation()) {
            if (operation.tags == null) {
                continue;
            }

            for (Tag tag : operation.tags) {
                String name = tag.getName();

                if (name == null || name.isEmpty()) {
                    continue;
                }

                int separator = name.indexOf('/');

                return separator < 0 ? name.trim() : name.substring(separator + 1).trim();
            }
        }

        // An operation reaches a page only through a tag, so this is unreachable short of a
        // document that states none - and then the class name is all there is to head it with.
        return String.valueOf(results.get("classname"));
    }

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        OperationsMap results = super.postProcessOperationsWithModels(objs, allModels);

        // The commands need nothing of what the pages are marked up with below.
        if (snippetsOnly) {
            if (snippets == null) {
                snippets = new Snippets(openAPI, allModels,
                        String.valueOf(additionalProperties.get("snippetsServerUrl")), isFetch());
            }

            CallSnippets.attach(this, results, snippets::attach);
            return results;
        }

        results.put(SECTION_TITLE, sectionTitle(results));

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
            markOperation(operation);

            // The third-party twin is rendered inside the operation's own section, from the same marks.
            Object variant = operation.vendorExtensions.get(ThirdPartyVariants.VARIANT_OPERATION);
            if (variant instanceof CodegenOperation) {
                markOperation((CodegenOperation) variant);
            }

        }

        return results;
    }

    private boolean isFetch() {
        return "javascript".equals(String.valueOf(additionalProperties.get(SNIPPETS_TAB)));
    }

    @Override
    public void postProcess() {
        super.postProcess();

        if (snippetsOnly && snippets != null) {
            CallSnippets.report(snippets.placeholders());
        }
    }

    // The string-id twin of a generic action (see ThirdPartyVariants) stays attached to its operation: the
    // page shows it as a subsection of the operation rather than as an operation of its own.
    @Override
    public CodegenOperation fromOperation(String path, String httpMethod, Operation operation, List<Server> servers) {
        CodegenOperation op = super.fromOperation(path, httpMethod, operation, servers);
        ThirdPartyVariants.attach(this, op, path, httpMethod, operation, servers, ThirdPartyVariants.Naming.OVERLOAD);
        return op;
    }

    /**
     * Links and notes for the parameters, responses, authorization and return type of one operation.
     */
    private void markOperation(CodegenOperation operation) {
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

    /**
     * Builds the call over HTTP of one operation: the request as it goes over the wire, written as a
     * cURL command for a POSIX shell or as a JavaScript fetch call for Node. What the call carries
     * follows the call snippets of the languages (see {@link MyCSharpClientCodegen}): the required
     * parameters, the files and the minimal body - a model with its required fields only, with the
     * values the document states for them. Both forms are written from the same request, so the two
     * tabs send the same thing.
     * <p>
     * The call is assembled here rather than in the template: every part of it depends on the
     * operation, and the template has nothing around them to hold.
     */
    private static final class Snippets {

        static final String CODE = "x-snippet-code";

        private static final int MAX_DEPTH = 4;

        /** An option on a line of its own, under the first one. */
        private static final String NEXT_OPTION = " \\\n  ";

        /** The body stands on an option line; its fields go a level deeper. */
        private static final String BODY_INDENT = "  ";

        /** The tail of a fetch call: the response printed as it comes, as the old reference panel had it. */
        private static final String FETCH_TAIL = "\n"
                + "  .then((response) => response.text())\n"
                + "  .then((result) => console.log(result))\n"
                + "  .catch((error) => console.error(error));";

        private static final String NIL_UUID = "00000000-0000-0000-0000-000000000000";

        /** A shell computes no date for the command, so one without an example stands in fixed. */
        private static final String DATE_TIME = "2025-01-01T00:00:00Z";
        private static final String DATE = "2025-01-01";

        private static final String FILE = "file.docx";

        private static final Pattern UUID_TEXT =
                Pattern.compile("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        /** What a path segment and a query value may carry unencoded beyond the unreserved characters (RFC 3986). */
        private static final String PATH_SAFE = ":@";
        private static final String QUERY_SAFE = ":@/";

        /** Stands for a file: in a form it is uploaded, in a JSON body it is the name of one. */
        private static final Object FILE_VALUE = new Object();

        /** What enumValue returns for a type that is no enum. */
        private static final Object NOT_ENUM = new Object();

        private final OpenAPI openAPI;
        private final Map<String, CodegenModel> models;
        private final String serverUrl;
        private final boolean fetch;

        /** Values the document states no example for, as "operationId: name", for the run's log. */
        private final List<String> placeholders = new ArrayList<>();

        Snippets(OpenAPI openAPI, List<ModelMap> allModels, String serverUrl, boolean fetch) {
            this.openAPI = openAPI;
            this.models = CallSnippets.models(allModels);
            this.serverUrl = serverUrl;
            this.fetch = fetch;
        }

        List<String> placeholders() {
            return placeholders;
        }

        void attach(CodegenOperation operation) {
            Request request = request(operation);
            operation.vendorExtensions.put(CODE, fetch ? fetch(request) : curl(request));
        }

        /** What the call sends, whichever way it is written. */
        private Request request(CodegenOperation operation) {
            Request request = new Request();
            request.method = operation.httpMethod.toUpperCase(Locale.ROOT);
            request.streams = streams(operation);
            request.multipart = Boolean.TRUE.equals(operation.isMultipart);

            Field credential = credential(operation);
            if (credential != null) {
                request.fields.add(credential);
            }

            String path = operation.path;
            List<String> query = new ArrayList<>();

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;
                Shape shape = Shape.of(parameter);

                if (parameter.isBodyParam) {
                    request.body = body(operation, shape, CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);
                    continue;
                }

                if (!parameter.required && !shape.isFile && !shape.isFileList) {
                    continue;
                }

                if (parameter.isFormParam && (shape.isFile || shape.isFileList)) {
                    request.form.add(new Field(Field.FILE, parameter.baseName, FILE));
                    continue;
                }

                Object example = CallSnippets.plain(parameter.isFormParam
                        ? SpecExamples.formField(openAPI, operation, parameter.baseName)
                        : SpecExamples.parameter(openAPI, operation, parameter.baseName, SpecExamples.location(parameter)));
                Object value = value(shape, example, parameter.baseName, where, 0);

                if (parameter.isPathParam) {
                    path = path.replace("{" + parameter.baseName + "}", encode(text(value), PATH_SAFE));
                } else if (parameter.isQueryParam) {
                    query(query, parameter.baseName, value);
                } else if (parameter.isHeaderParam) {
                    request.fields.add(new Field(Field.HEADER, parameter.baseName, text(value)));
                } else if (parameter.isCookieParam) {
                    request.fields.add(new Field(Field.COOKIE, parameter.baseName, text(value)));
                } else if (parameter.isFormParam) {
                    request.form.add(new Field(Field.TEXT, parameter.baseName, text(value)));
                }
            }

            request.url = serverUrl + path + (query.isEmpty() ? "" : "?" + String.join("&", query));
            return request;
        }

        /** The request as a cURL command, an option per line. */
        private static String curl(Request request) {
            StringBuilder first = new StringBuilder("curl");
            if (request.streams) {
                // The response comes as a stream, which curl would otherwise hold back until it ends.
                first.append(" -N");
            }
            if (!"GET".equals(request.method)) {
                first.append(" -X ").append(request.method);
            }
            first.append(' ').append(shellQuote(request.url));

            List<String> lines = new ArrayList<>();
            lines.add(first.toString());
            for (Field field : request.fields) {
                lines.add(field.kind == Field.COOKIE
                        ? "-b " + shellQuote(field.name + "=" + field.value)
                        : "-H " + shellQuote(field.name + ": " + field.value));
            }
            if (request.body != null) {
                lines.add("-H \"Content-Type: application/json\"");
                lines.add("-d '" + json(request.body, BODY_INDENT).replace("'", "'\\''") + "'");
            }
            for (Field field : request.form) {
                lines.add(field.kind == Field.FILE
                        ? "-F " + shellQuote(field.name + "=@" + field.value)
                        : formField(request.multipart, field.name, field.value));
            }

            return String.join(NEXT_OPTION, lines);
        }

        /**
         * The request as a fetch call, in the form the reference panel of the earlier site version
         * wrote it: the headers, the body and the options in variables of their own, then the call.
         * It is written for Node: a file is read from the disk there, and the cookie an OAuth
         * endpoint is authorized with is a header Node sends, where a browser refuses to.
         */
        private static String fetch(Request request) {
            List<String> blocks = new ArrayList<>();

            boolean files = false;
            for (Field field : request.form) {
                files |= field.kind == Field.FILE;
            }
            if (files) {
                blocks.add("const fs = require(\"node:fs\");");
            }

            List<String> headers = new ArrayList<>();
            if (request.body != null) {
                headers.add(append("myHeaders", "Content-Type", "application/json"));
            }
            List<String> cookies = new ArrayList<>();
            for (Field field : request.fields) {
                if (field.kind == Field.COOKIE) {
                    cookies.add(field.name + "=" + field.value);
                } else {
                    headers.add(append("myHeaders", field.name, field.value));
                }
            }
            // Cookies travel in one header, the way a client joins them.
            if (!cookies.isEmpty()) {
                headers.add(append("myHeaders", "Cookie", String.join("; ", cookies)));
            }
            if (!headers.isEmpty()) {
                blocks.add("const myHeaders = new Headers();\n" + String.join("\n", headers));
            }

            String body = null;
            if (request.body != null) {
                body = "raw";
                blocks.add("const raw = JSON.stringify(" + json(request.body, "") + ");");
            } else if (!request.form.isEmpty()) {
                // A form needs no Content-Type of its own: fetch sets it, with the boundary of a multipart one.
                body = request.multipart ? "formdata" : "urlencoded";
                List<String> lines = new ArrayList<>();
                lines.add(request.multipart
                        ? "const formdata = new FormData();"
                        : "const urlencoded = new URLSearchParams();");
                for (Field field : request.form) {
                    lines.add(field.kind == Field.FILE
                            ? body + ".append(" + jsonString(field.name) + ", new Blob([fs.readFileSync("
                                    + jsonString(field.value) + ")]), " + jsonString(field.value) + ");"
                            : append(body, field.name, field.value));
                }
                blocks.add(String.join("\n", lines));
            }

            List<String> options = new ArrayList<>();
            options.add("  method: " + jsonString(request.method));
            if (!headers.isEmpty()) {
                options.add("  headers: myHeaders");
            }
            if (body != null) {
                options.add("  body: " + body);
            }
            options.add("  redirect: \"follow\"");
            blocks.add("const requestOptions = {\n" + String.join(",\n", options) + "\n};");

            blocks.add("fetch(" + jsonString(request.url) + ", requestOptions)" + FETCH_TAIL);

            return String.join("\n\n", blocks);
        }

        private static String append(String target, String name, String value) {
            return target + ".append(" + jsonString(name) + ", " + jsonString(value) + ");";
        }

        /**
         * The credentials the operation states, the way the document has them sent: the access token for
         * a bearer scheme or OAuth 2.0, which every other tab sends too, or else a key in a cookie or a
         * header. An operation that states none sends nothing.
         */
        private static Field credential(CodegenOperation operation) {
            if (operation.authMethods == null) {
                return null;
            }

            for (CodegenSecurity security : operation.authMethods) {
                if (Boolean.TRUE.equals(security.isBasicBearer) || Boolean.TRUE.equals(security.isOAuth)) {
                    return new Field(Field.HEADER, "Authorization", "Bearer YOUR_API_KEY");
                }
            }

            for (CodegenSecurity security : operation.authMethods) {
                if (Boolean.TRUE.equals(security.isApiKey) && security.keyParamName != null) {
                    String key = "YOUR_" + security.keyParamName.toUpperCase(Locale.ROOT).replaceAll("[^A-Z0-9]+", "_");
                    if (Boolean.TRUE.equals(security.isKeyInCookie)) {
                        return new Field(Field.COOKIE, security.keyParamName, key);
                    }
                    if (Boolean.TRUE.equals(security.isKeyInHeader)) {
                        return new Field(Field.HEADER, security.keyParamName, key);
                    }
                }
            }

            return null;
        }

        /** Whether the operation answers with a stream of events or of JSON lines. */
        private static boolean streams(CodegenOperation operation) {
            if (operation.produces != null) {
                for (Map<String, String> produced : operation.produces) {
                    String mediaType = String.valueOf(produced.get("mediaType")).toLowerCase(Locale.ROOT);
                    if (mediaType.contains("event-stream") || mediaType.contains("ndjson")) {
                        return true;
                    }
                }
            }

            return false;
        }

        /** The query of a list a value per item, as ASP.NET binds it: ids=1&amp;ids=2. */
        private static void query(List<String> query, String name, Object value) {
            if (value instanceof List) {
                for (Object item : (List<?>) value) {
                    query.add(encode(name, QUERY_SAFE) + "=" + encode(text(item), QUERY_SAFE));
                }
            } else if (value instanceof Map) {
                for (Map.Entry<?, ?> entry : ((Map<?, ?>) value).entrySet()) {
                    query.add(encode(name + "[" + entry.getKey() + "]", QUERY_SAFE) + "=" + encode(text(entry.getValue()), QUERY_SAFE));
                }
            } else {
                query.add(encode(name, QUERY_SAFE) + "=" + encode(text(value), QUERY_SAFE));
            }
        }

        /**
         * A field of a form body. A multipart field takes -F, or --form-string where curl would read the
         * value as a file or as its own field options; an urlencoded one is encoded by curl.
         */
        private static String formField(boolean multipart, String name, String value) {
            if (!multipart) {
                return "--data-urlencode " + shellQuote(name + "=" + value);
            }

            boolean literal = value.startsWith("@") || value.startsWith("<") || value.contains(";");
            return (literal ? "--form-string " : "-F ") + shellQuote(name + "=" + value);
        }

        /**
         * The request body. A model gets its required fields only, whatever the document states for the
         * body as a whole; an array or a scalar takes the example the document states for it.
         */
        private Object body(CodegenOperation operation, Shape shape, Object example, String where) {
            CodegenModel model = shape.isArray ? null : model(shape);

            // A body that is an enum schema the generator may hand over as its underlying type: the
            // value of a member, chosen as for any other enum.
            if (model == null && !shape.isArray && !shape.isMap) {
                CodegenModel enumBody = CallSnippets.enumBody(openAPI, operation, models);
                if (enumBody != null) {
                    return member(enumBody, enumBody.allowableValues, example, where);
                }
            }

            if (model == null || model.isEnum || CallSnippets.isComposed(model)) {
                return value(shape, example, "body", where, 0);
            }

            return construct(model, where, 0);
        }

        /**
         * A model as an object with its required fields; past the depth a model with required fields is
         * built to, an empty one, and the place is left to the reader.
         */
        private Object construct(CodegenModel model, String where, int depth) {
            Map<String, Object> fields = new LinkedHashMap<>();

            if (model.allVars == null) {
                return fields;
            }

            for (CodegenProperty property : model.allVars) {
                if (!property.required || fields.containsKey(property.baseName)) {
                    continue;
                }

                if (depth >= MAX_DEPTH) {
                    placeholders.add(where);
                    return new LinkedHashMap<String, Object>();
                }

                Object example = CallSnippets.plain(SpecExamples.inheritedProperty(openAPI, model.name, property.baseName));
                fields.put(property.baseName, value(Shape.of(property), example, property.baseName,
                        where + "." + property.baseName, depth + 1));
            }

            return fields;
        }

        private Object value(Shape shape, Object example, String name, String where, int depth) {
            if (shape.isFileList) {
                return Collections.singletonList(FILE_VALUE);
            }

            if (shape.isFile) {
                return FILE_VALUE;
            }

            Object enumValue = enumValue(shape, example, where);
            if (enumValue != NOT_ENUM) {
                return enumValue;
            }

            if (shape.isArray) {
                List<Object> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(item(shape, CallSnippets.plain(item), name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    // A list the document requires items in gets one, standing in for them: an empty one
                    // would not make a valid request.
                    if (shape.minItems != null && shape.minItems > 0) {
                        items.add(item(shape, null, name, where, depth + 1));
                    } else {
                        placeholders.add(where);
                    }
                }

                return items;
            }

            if (shape.isMap) {
                return map(shape, example, name, where, depth);
            }

            CodegenModel model = model(shape);
            if (model != null) {
                return CallSnippets.isComposed(model)
                        ? composed(model, example, name, where, depth)
                        : construct(model, where, depth);
            }

            // A free-form object, or a value of any type: the example as it stands. Without one, an empty
            // object; for a value of any type that is a stand-in the log notes.
            if (shape.isFreeFormObject || shape.isAnyType) {
                if (example != null) {
                    return example;
                }
                if (!shape.isFreeFormObject) {
                    placeholders.add(where);
                }
                return new LinkedHashMap<String, Object>();
            }

            return scalar(shape, example, name, where);
        }

        private Object item(Shape shape, Object example, String name, String where, int depth) {
            return shape.items == null ? scalar(new Shape(), example, name, where) : value(shape.item(), example, name, where, depth);
        }

        /**
         * A dictionary with the entries of the example; one the document requires entries in and gives no
         * example for gets one standing in for them, which the log notes.
         */
        private Object map(Shape shape, Object example, String name, String where, int depth) {
            Map<String, Object> entries = new LinkedHashMap<>();
            if (example instanceof Map) {
                for (Map.Entry<?, ?> entry : ((Map<?, ?>) example).entrySet()) {
                    entries.put(String.valueOf(entry.getKey()), entry(shape, CallSnippets.plain(entry.getValue()), name, where, depth));
                }
            }

            if (entries.isEmpty() && shape.minProperties != null && shape.minProperties > 0) {
                int logged = placeholders.size();
                entries.put("YOUR_KEY", entry(shape, null, name, where, depth));
                if (placeholders.size() == logged) {
                    placeholders.add(where);
                }
            }

            return entries;
        }

        /** A value of a dictionary; one without a description of its values is the example as it stands. */
        private Object entry(Shape shape, Object example, String name, String where, int depth) {
            if (shape.items != null) {
                return value(shape.item(), example, name, where, depth + 1);
            }

            return example != null ? example : new LinkedHashMap<String, Object>();
        }

        /**
         * A oneOf or anyOf: on the wire it is the value of one of its variants, the one
         * {@link CallSnippets#variant} picks.
         */
        private Object composed(CodegenModel model, Object example, String name, String where, int depth) {
            List<CodegenProperty> variants = new ArrayList<>();
            if (model.getComposedSchemas() != null) {
                if (model.getComposedSchemas().getOneOf() != null) {
                    variants.addAll(model.getComposedSchemas().getOneOf());
                }
                if (variants.isEmpty() && model.getComposedSchemas().getAnyOf() != null) {
                    variants.addAll(model.getComposedSchemas().getAnyOf());
                }
            }

            // A variant can be a oneOf of its own and lead back here, so it stops at the depth the models do.
            if (depth >= MAX_DEPTH || variants.isEmpty()) {
                placeholders.add(where);
                return new LinkedHashMap<String, Object>();
            }

            CallSnippets.Choice<CodegenProperty> choice = CallSnippets.variant(openAPI, model, variants, example,
                    (candidate, stated) -> fits(Shape.of(candidate), stated));

            return value(Shape.of(choice.variant), choice.example, name, where, depth + 1);
        }

        private boolean fits(Shape shape, Object example) {
            if (example == null) {
                return false;
            }

            if (shape.isArray) {
                return example instanceof List;
            }

            if (shape.isMap || shape.isFreeFormObject) {
                return example instanceof Map;
            }

            CodegenModel model = model(shape);
            if (model != null) {
                // An enum fits only an example that names one of its members; any other is left to a
                // variant beside it, such as a string.
                return model.isEnum ? named(model.allowableValues, enumNames(model), example) != null : example instanceof Map;
            }

            if (shape.isEnum) {
                return named(shape.allowableValues, null, example) != null;
            }

            if (shape.isAnyType) {
                return true;
            }

            if (shape.isString) {
                return example instanceof String;
            }

            if (shape.isInteger) {
                return example instanceof Number && new BigDecimal(String.valueOf(example)).stripTrailingZeros().scale() <= 0;
            }

            if (shape.isNumber) {
                return example instanceof Number;
            }

            if (shape.isBoolean) {
                return example instanceof Boolean;
            }

            return false;
        }

        /**
         * The value of an enum member, of an enum schema or of one declared in place; NOT_ENUM for any
         * other type. On the wire a member is its value, whatever the SDKs name it.
         */
        private Object enumValue(Shape shape, Object example, String where) {
            // An array or a dictionary of enum values is built a value at a time.
            if (shape.isArray || shape.isMap) {
                return NOT_ENUM;
            }

            CodegenModel model = model(shape);
            if (model != null && model.isEnum) {
                return member(model, model.allowableValues, example, where);
            }

            if (shape.isEnum && hasMembers(shape.allowableValues)) {
                return member(null, shape.allowableValues, example, where);
            }

            return NOT_ENUM;
        }

        private static boolean hasMembers(Map<String, Object> allowableValues) {
            Object values = allowableValues == null ? null : allowableValues.get("values");
            return values instanceof List && !((List<?>) values).isEmpty();
        }

        /** The member of an enum the example names, or else the first one, which the log notes. */
        private Object member(CodegenModel model, Map<String, Object> allowableValues, Object example, String where) {
            Object chosen = named(allowableValues, model == null ? null : enumNames(model), example);
            if (chosen == null) {
                placeholders.add(where);
                chosen = ((List<?>) allowableValues.get("values")).get(0);
            }

            return chosen;
        }

        /**
         * The value of the member the example names, by its value or by the name the document gives it;
         * null when it names none.
         */
        private static Object named(Map<String, Object> allowableValues, List<?> names, Object example) {
            Object values = allowableValues == null ? null : allowableValues.get("values");
            if (example == null || !(values instanceof List)) {
                return null;
            }

            String text = SpecExamples.text(example);
            List<?> members = (List<?>) values;
            for (int i = 0; i < members.size(); i++) {
                if (text.equals(String.valueOf(members.get(i)))
                        || (names != null && i < names.size() && text.equals(String.valueOf(names.get(i))))) {
                    return members.get(i);
                }
            }

            return null;
        }

        /**
         * The member names the document states for an enum schema in x-enum-varnames. The generator's
         * own names cannot be used: this generator names every member after the model (see enumVarNames).
         */
        private List<?> enumNames(CodegenModel model) {
            if (openAPI == null || openAPI.getComponents() == null || openAPI.getComponents().getSchemas() == null) {
                return null;
            }

            Schema<?> schema = openAPI.getComponents().getSchemas().get(model.name);
            Object names = schema == null || schema.getExtensions() == null ? null : schema.getExtensions().get("x-enum-varnames");
            return names instanceof List ? (List<?>) names : null;
        }

        private CodegenModel model(Shape shape) {
            if (shape.complexType != null && models.containsKey(shape.complexType)) {
                return models.get(shape.complexType);
            }

            // An enum schema in a path or a query the generator names by its type only.
            return shape.dataType != null ? models.get(shape.dataType) : null;
        }

        /**
         * A value of a plain type. A date, a date-time or a UUID the example does not hold in its form
         * counts as no example.
         */
        private Object scalar(Shape shape, Object example, String name, String where) {
            String text = example == null ? null : SpecExamples.text(example);

            if (shape.isDateTime) {
                if (text != null) {
                    try {
                        OffsetDateTime.parse(text);
                        return text;
                    } catch (DateTimeParseException notADateTime) {
                        // Falls through to the placeholder.
                    }
                }
                placeholders.add(where);
                return DATE_TIME;
            }

            if (shape.isDate) {
                if (text != null) {
                    String day = text.length() > 10 ? text.substring(0, 10) : text;
                    try {
                        LocalDate.parse(day);
                        return day;
                    } catch (DateTimeParseException notADate) {
                        // Falls through to the placeholder.
                    }
                }
                placeholders.add(where);
                return DATE;
            }

            if (shape.isUuid) {
                if (text == null || !UUID_TEXT.matcher(text).matches()) {
                    placeholders.add(where);
                    return NIL_UUID;
                }
                return text;
            }

            if (shape.isInteger || shape.isNumber) {
                if (example instanceof Number) {
                    return example;
                }
                if (text != null) {
                    try {
                        return new BigDecimal(text);
                    } catch (NumberFormatException notANumber) {
                        // Falls through to the placeholder.
                    }
                }
                placeholders.add(where);
                return 0;
            }

            if (shape.isBoolean) {
                if (example instanceof Boolean) {
                    return example;
                }
                if ("true".equalsIgnoreCase(text) || "false".equalsIgnoreCase(text)) {
                    return Boolean.parseBoolean(text);
                }
                placeholders.add(where);
                return false;
            }

            if (example instanceof List || example instanceof Map) {
                return example;
            }

            if (text == null) {
                placeholders.add(where);
                return CallSnippets.placeholderName(name);
            }

            return text;
        }

        /** A value as it is written into a path, a query, a header or a form field. */
        private static String text(Object value) {
            if (value == FILE_VALUE) {
                return FILE;
            }
            if (value instanceof String) {
                return (String) value;
            }
            if (value instanceof BigDecimal) {
                return ((BigDecimal) value).toPlainString();
            }
            if (value == null || value instanceof Number || value instanceof Boolean) {
                return String.valueOf(value);
            }

            return json(value, "");
        }

        /**
         * A value as JSON, an object or an array a member per line; indent is where the line the value
         * starts on stands.
         */
        private static String json(Object value, String indent) {
            if (value == null) {
                return "null";
            }
            if (value == FILE_VALUE) {
                return jsonString(FILE);
            }
            if (value instanceof String) {
                return jsonString((String) value);
            }
            if (value instanceof BigDecimal) {
                return ((BigDecimal) value).toPlainString();
            }
            if (value instanceof Number || value instanceof Boolean) {
                return String.valueOf(value);
            }

            String inner = indent + "  ";
            List<String> members = new ArrayList<>();

            if (value instanceof Map) {
                for (Map.Entry<?, ?> entry : ((Map<?, ?>) value).entrySet()) {
                    members.add(inner + jsonString(String.valueOf(entry.getKey())) + ": "
                            + json(CallSnippets.plain(entry.getValue()), inner));
                }
                return members.isEmpty() ? "{}" : "{\n" + String.join(",\n", members) + "\n" + indent + "}";
            }

            if (value instanceof List) {
                for (Object item : (List<?>) value) {
                    members.add(inner + json(CallSnippets.plain(item), inner));
                }
                return members.isEmpty() ? "[]" : "[\n" + String.join(",\n", members) + "\n" + indent + "]";
            }

            return jsonString(SpecExamples.text(value));
        }

        private static String jsonString(String text) {
            StringBuilder builder = new StringBuilder("\"");
            for (char c : text.toCharArray()) {
                switch (c) {
                    case '"':
                        builder.append("\\\"");
                        break;
                    case '\\':
                        builder.append("\\\\");
                        break;
                    case '\n':
                        builder.append("\\n");
                        break;
                    case '\r':
                        builder.append("\\r");
                        break;
                    case '\t':
                        builder.append("\\t");
                        break;
                    default:
                        if (c < 0x20) {
                            builder.append(String.format("\\u%04x", (int) c));
                        } else {
                            builder.append(c);
                        }
                }
            }
            return builder.append('"').toString();
        }

        /** Percent-encoding, as UTF-8: everything but the unreserved characters and those the place allows. */
        private static String encode(String text, String safe) {
            StringBuilder builder = new StringBuilder();
            for (byte b : text.getBytes(StandardCharsets.UTF_8)) {
                char c = (char) (b & 0xFF);
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')
                        || c == '-' || c == '.' || c == '_' || c == '~' || safe.indexOf(c) >= 0) {
                    builder.append(c);
                } else {
                    builder.append('%').append(String.format("%02X", b & 0xFF));
                }
            }
            return builder.toString();
        }

        /**
         * An argument of the shell: in double quotes, with what the shell expands in them escaped; in
         * single quotes where it holds a !, which an interactive shell expands even inside double quotes.
         */
        private static String shellQuote(String text) {
            if (text.indexOf('!') >= 0) {
                return "'" + text.replace("'", "'\\''") + "'";
            }

            return "\"" + text
                    .replace("\\", "\\\\")
                    .replace("\"", "\\\"")
                    .replace("$", "\\$")
                    .replace("`", "\\`") + "\"";
        }

        /** The request a call sends: what the cURL command and the fetch call are both written from. */
        private static final class Request {
            String method;
            String url;
            boolean streams;
            boolean multipart;

            /** The headers and cookies, the credentials first, then the parameters in their order. */
            final List<Field> fields = new ArrayList<>();

            /** The JSON body, as the value it is written from; null when there is none. */
            Object body;

            /** The fields of a form body, in the order of the parameters. */
            final List<Field> form = new ArrayList<>();
        }

        /** A header, a cookie or a form field of the request; a file field holds the file's name. */
        private static final class Field {
            static final int HEADER = 0;
            static final int COOKIE = 1;
            static final int TEXT = 2;
            static final int FILE = 3;

            final int kind;
            final String name;
            final String value;

            Field(int kind, String name, String value) {
                this.kind = kind;
                this.name = name;
                this.value = value;
            }
        }

        /** What the call needs to know about the type of a parameter or a property. */
        private static final class Shape {
            String dataType;
            String complexType;
            boolean isEnum;
            boolean isArray;
            boolean isMap;
            boolean isFreeFormObject;
            boolean isAnyType;
            boolean isString;
            boolean isInteger;
            boolean isNumber;
            boolean isBoolean;
            boolean isDate;
            boolean isDateTime;
            boolean isUuid;
            boolean isFile;
            boolean isFileList;
            Integer minItems;
            Integer minProperties;
            Map<String, Object> allowableValues;
            CodegenProperty items;

            static Shape of(CodegenParameter parameter) {
                Shape shape = new Shape();
                shape.dataType = parameter.dataType;
                shape.complexType = parameter.baseType;
                shape.isEnum = parameter.isEnum;
                shape.isArray = parameter.isArray;
                shape.isMap = parameter.isMap;
                shape.isFreeFormObject = parameter.isFreeFormObject;
                shape.isAnyType = parameter.isAnyType;
                shape.isString = parameter.isString;
                shape.isInteger = parameter.isInteger || parameter.isLong || parameter.isShort;
                shape.isNumber = parameter.isNumber || parameter.isFloat || parameter.isDouble || parameter.isDecimal;
                shape.isBoolean = parameter.isBoolean;
                shape.isDate = parameter.isDate;
                shape.isDateTime = parameter.isDateTime;
                shape.isUuid = parameter.isUuid;
                shape.isFile = parameter.isFile || parameter.isBinary;
                shape.isFileList = (shape.isFile && parameter.isCollectionFormatMulti)
                        || (parameter.isArray && parameter.items != null && (parameter.items.isFile || parameter.items.isBinary));
                if (shape.isFileList) {
                    shape.isFile = false;
                }
                shape.minItems = parameter.minItems;
                shape.minProperties = parameter.getMinProperties();
                shape.allowableValues = parameter.allowableValues;
                shape.items = parameter.items;
                return shape;
            }

            static Shape of(CodegenProperty property) {
                Shape shape = new Shape();
                shape.dataType = property.dataType;
                shape.complexType = property.complexType;
                shape.isEnum = property.isEnum;
                shape.isArray = property.isArray;
                shape.isMap = property.isMap;
                shape.isFreeFormObject = property.isFreeFormObject;
                shape.isAnyType = property.isAnyType;
                shape.isString = property.isString;
                shape.isInteger = property.isInteger || property.isLong || property.isShort;
                shape.isNumber = property.isNumber || property.isFloat || property.isDouble || property.isDecimal;
                shape.isBoolean = property.isBoolean;
                shape.isDate = property.isDate;
                shape.isDateTime = property.isDateTime;
                shape.isUuid = property.isUuid;
                shape.isFile = property.isFile || property.isBinary;
                shape.isFileList = property.isArray && property.items != null && (property.items.isFile || property.items.isBinary);
                if (shape.isFileList) {
                    shape.isFile = false;
                }
                shape.minItems = property.minItems;
                shape.minProperties = property.getMinProperties();
                shape.allowableValues = property.allowableValues;
                shape.items = property.items;
                return shape;
            }

            /** The shape of an item of this array. */
            Shape item() {
                return of(items);
            }
        }
    }
}
