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

import org.openapitools.codegen.languages.Swift6ClientCodegen;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.*;
import io.swagger.v3.oas.models.headers.*;
import static org.openapitools.codegen.utils.StringUtils.camelize;
import org.openapitools.codegen.*;
import org.openapitools.codegen.model.*;

import java.io.File;
import java.util.*;
import java.util.Map.Entry;
import java.util.stream.Collectors;

import io.swagger.v3.oas.models.OpenAPI;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeParseException;
import java.time.temporal.ChronoUnit;
import java.util.regex.Pattern;

public class MySwift6ClientCodegen extends Swift6ClientCodegen {
    protected String apiNamePrefix = "", apiNameSuffix = "Api";

    /**
     * The call snippets of the reference pages instead of the SDK: one file, with a snippet per
     * operation, written into the folder the run was given with -o.
     */
    private boolean snippetsOnly;

    private Snippets snippets;
    
    public MySwift6ClientCodegen() {
        super();
        this.templateDir = "templates/swift6";
        this.embeddedTemplateDir = "swift6";
    }

    @Override
    public void processOpts() {
        super.processOpts();

        // Returns before the output folder is pointed at the SDK checkout (see CallSnippets.configure).
        snippetsOnly = CallSnippets.configure(this);
        if (snippetsOnly) {
            return;
        }

        this.outputFolder = "../../../../../sdk/docspace-api-sdk-swift";

        if (openAPI.getServers() != null && !openAPI.getServers().isEmpty()) {
            Server server = openAPI.getServers().get(0);
            ServerVariables serverVars = server.getVariables();
            if (serverVars != null){
                ServerVariable baseUrlVar = serverVars.get("baseUrl");
                if(baseUrlVar != null && "".equals(baseUrlVar.getDefault())){
                    baseUrlVar.setDefault("http://localhost:8092/");
                }
            }
        }

        supportingFiles.removeIf(f -> f.getTemplateFile().equals("git_push.sh.mustache") || 
            f.getDestinationFilename().equals(".openapi-generator-ignore")
        );

        supportingFiles.add(new SupportingFile(
            "AUTHORS.mustache", "", "AUTHORS.md"
        ));

        supportingFiles.add(new SupportingFile(
            "LICENSE.mustache", "", "LICENSE"
        ));

        supportingFiles.add(new SupportingFile(
            "CHANGELOG.mustache", "", "CHANGELOG.md"
        ));
    }

    // The third-party twin of a generic action (see ThirdPartyVariants): the string-id shape the document
    // carries as `x-thirdparty-variant`, exposed as an overload of the same method.
    @Override
    public CodegenOperation fromOperation(String path, String httpMethod, Operation operation, List<Server> servers) {
        CodegenOperation op = super.fromOperation(path, httpMethod, operation, servers);
        ThirdPartyVariants.attach(this, op, path, httpMethod, operation, servers, ThirdPartyVariants.Naming.OVERLOAD);
        return op;
    }

    @Override
    public void postProcess() {
        super.postProcess();

        if (snippetsOnly) {
            if (snippets != null) {
                CallSnippets.report(snippets.placeholders());
            }
            return;
        }

        StaleOutput.delete(this);
        LineEndings.normalize(this);
    }

    /**
     * Gives every operation what its call snippet needs. The third-party twin of an operation
     * (see ThirdPartyVariants) gets none: it is an overload of the same method, on the same page.
     */
    private void attachSnippets(OperationsMap objs, List<ModelMap> allModels) {
        if (snippets == null) {
            snippets = new Snippets(openAPI, allModels);
        }

        String apiClass = String.valueOf(objs.getOperations().get("x-classname"));
        CallSnippets.attach(objs, operation -> snippets.attach(operation, apiClass));
    }

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        ThirdPartyVariants.insert(this, objs);

        super.postProcessOperationsWithModels(objs, allModels);

        if (objs != null && objs.getOperations() != null) {
            OperationMap operationMap = objs.getOperations();
            List<CodegenOperation> operationList = operationMap.getOperation();
            String className = operationMap.getClassname();
            if (className != null && className.endsWith(apiNameSuffix)) {
                className = className.substring(0, className.length() - 3);
            }
            String sanitizedTag = sanitizeTag(className);
            TagParts tagParts = tagMap.get(camelize(sanitizedTag));
            operationMap.put("x-folder", tagParts.folderPart);
            operationMap.put("x-classname", tagParts.classPart + apiNameSuffix);
            boolean shouldSupportFields = false;
            boolean supportUseAt = false;
            Map<String, Map<String, Object>> rateLimitHeaders = new LinkedHashMap<>();
            if (openAPI.getComponents() != null && openAPI.getComponents().getHeaders() != null) {
                Map<String, Header> componentHeaders = openAPI.getComponents().getHeaders();
                String[][] rateLimitHeaderDefs = {
                    {"X-RateLimit-Limit",     "x-rateLimitLimit"},
                    {"X-RateLimit-Remaining", "x-rateLimitRemaining"},
                    {"X-RateLimit-Reset",     "x-rateLimitReset"},
                    {"Retry-After",           "x-retryAfter"}
                };
                for (String[] def : rateLimitHeaderDefs) {
                    String name = def[0];
                    String vendorKey = def[1];
                    Header header = componentHeaders.get(name);
                    if (header != null) {
                        Map<String, Object> headerData = new LinkedHashMap<>();
                        headerData.put("name", name);
                        headerData.put("description", header.getDescription());
                        if (header.getSchema() != null && header.getSchema().getExample() != null) {
                            headerData.put("example", header.getSchema().getExample());
                        }
                        rateLimitHeaders.put(vendorKey, headerData);
                    }
                }
            }
            if (operationList != null) {
                for (CodegenOperation op : operationList) {
                    for (Map.Entry<String, Map<String, Object>> entry : rateLimitHeaders.entrySet()) {
                        op.vendorExtensions.put(entry.getKey(), entry.getValue());
                    }
                    if (op.operationId != null) {
                        String dashedId = toDashCase(op.operationId);
                        String seealsoUrl = "https://api.onlyoffice.com/docspace/api-backend/usage-api/" + dashedId + "/";
                        op.vendorExtensions.put("x-seealsoUrl", seealsoUrl);
                    }

                    if ("GET".equalsIgnoreCase(op.httpMethod)) {
                        boolean allAreQueryParams = op.allParams.stream()
                            .allMatch(p -> Boolean.TRUE.equals(p.isQueryParam));

                        boolean hasCountParam = op.allParams.stream()
                            .anyMatch(p -> "count".equals(p.baseName));

                        if (allAreQueryParams && hasCountParam) {
                            op.vendorExtensions.put("x-hasFieldsParam", true);
                            shouldSupportFields = true;
                        }
                    }
                    if ("GET".equalsIgnoreCase(op.httpMethod)
                        && "/api/2.0/files/recent".equals(op.path)) {

                        op.vendorExtensions.put("x-supportsUseAtMethod", true);
                        supportUseAt = true;
                    }
                    
                }
            }
            operationMap.put("x-supportsFields", shouldSupportFields);
            operationMap.put("x-supportsUseAt", supportUseAt);

            if (snippetsOnly) {
                attachSnippets(objs, allModels);
            }
        }

        return objs;
    }

    
    public Map<String, Object> postProcessSupportingFileData(Map<String, Object> objs) {
        super.postProcessSupportingFileData(objs);

        ApiInfoMap apiInfo = (ApiInfoMap) objs.get("apiInfo");
        Map<String, List<Map<String, Object>>> folderToApis = new LinkedHashMap<>();
        for (OperationsMap api : apiInfo.getApis()) {

            OperationMap operationMap = api.getOperations();
            String className = operationMap.getClassname();
            if (className != null && className.endsWith(apiNameSuffix)) {
                className = className.substring(0, className.length() - 3);
            }
            String sanitizedTag = sanitizeTag(className);
            TagParts tagParts = tagMap.get(camelize(sanitizedTag));

            String folder = tagParts.folderPart;
            String classname = tagParts.classPart + apiNameSuffix;
            api.put("x-folder", folder);
            api.put("x-classname", classname);

            folderToApis.computeIfAbsent(folder, k -> new ArrayList<>()).add(api);
        }

        List<Map<String, Object>> customApis = new ArrayList<>();
        for (Map.Entry<String, List<Map<String, Object>>> entry : folderToApis.entrySet()) {
            Map<String, Object> folderEntry = new HashMap<>();
            folderEntry.put("folder", entry.getKey());
            folderEntry.put("apis", entry.getValue());
            customApis.add(folderEntry);
        }

        objs.put("customApis", customApis);

        objs.put("x-authorizationUrl", "{{authBaseUrl}}/oauth2/authorize");
        objs.put("x-tokenUrl", "{{authBaseUrl}}/oauth2/token");
        objs.put("x-openIdConnectUrl", "{{authBaseUrl}}/.well-known/openid-configuration");

        String appName = (String) additionalProperties.get("appName");
        if (appName != null) {
            objs.put("appName", appName.toUpperCase());
        }

        return objs;
    }


    private final Map<String, TagParts> tagMap = new HashMap<>();

    @Override
    public String sanitizeTag(String tag) {
        String sanitized = super.sanitizeTag(tag);
        if (!tagMap.containsKey(sanitized)) {
            String[] parts = tag.split(" / ");
            String folderPart = parts[0];
            String classPart = (parts.length > 1) ? parts[1] : parts[0];

            TagParts info = new TagParts(
                tag,
                camelize(sanitizeName(folderPart)),
                camelize(sanitizeName(classPart))
            );

            tagMap.put(sanitized, info);
        }
        return sanitized;
    }

    private final Map<String, String> seenApiFilenames = new HashMap<String, String>();

    @Override
    public String apiFilename(String templateName, String tag) {
        String uniqueTag = uniqueCaseInsensitiveString(tag, seenApiFilenames);
        String suffix = apiTemplateFiles().get(templateName);
        TagParts tagParts = tagMap.get(camelize(uniqueTag));
        if (tagParts == null) {
            return apiFileFolder() + File.separator + toApiFilename(uniqueTag) + suffix;
        }

        String folderPath = apiFileFolder() + File.separator + tagParts.folderPart;
        String filename = toApiFilename(tagParts.classPart) + suffix;

        return folderPath + File.separator + filename;
    }

    private String uniqueCaseInsensitiveString(String value, Map<String, String> seenValues) {
        if (seenValues.keySet().contains(value)) {
            return seenValues.get(value);
        }

        Optional<Entry<String, String>> foundEntry = seenValues.entrySet().stream().filter(v -> v.getValue().toLowerCase(Locale.ROOT).equals(value.toLowerCase(Locale.ROOT))).findAny();
        if (foundEntry.isPresent()) {
            int counter = 0;
            String uniqueValue = value + "_" + counter;

            while (seenValues.values().stream().map(v -> v.toLowerCase(Locale.ROOT)).collect(Collectors.toList()).contains(uniqueValue.toLowerCase(Locale.ROOT))) {
                counter++;
                uniqueValue = value + "_" + counter;
            }

            seenValues.put(value, uniqueValue);
            return uniqueValue;
        }

        seenValues.put(value, value);
        return value;
    }

    private String toDashCase(String input) {
        return input.replaceAll("([a-z0-9])([A-Z])", "$1-$2")
                    .toLowerCase();
    }

    @Override
    public String getName() {
        return "my-swift6";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Swift6 client";
    }

    /**
     * Builds what the call snippet of one operation needs beyond the template, as Swift: the labelled
     * arguments of the call, the request body and the type it is declared with, whether Foundation is
     * imported, and whether the configuration carries the token.
     * <p>
     * The body is the minimal one, as for C# (see {@link MyCSharpClientCodegen}): a model gets its
     * required fields only, as labelled arguments of its initializer in the order it declares them,
     * with the values the document states for them. Every optional parameter of a call and every
     * optional field of a model has a default in the SDK, so it is left out.
     * <p>
     * The snippets are written by the names the SDK is generated with, but not compiled against it:
     * the SDK does not build as it stands.
     */
    private static final class Snippets {

        static final String ARGUMENTS = "x-snippet-arguments";
        static final String BODY = "x-snippet-body";
        static final String BODY_TYPE = "x-snippet-body-type";
        static final String FOUNDATION = "x-snippet-foundation";
        static final String BEARER = "x-snippet-bearer";

        /** Beyond this length the arguments of the body's initializer are put one per line. */
        private static final int LINE_LENGTH = 60;

        /** Beyond this length the call is put an argument per line: about what a code block of the page shows. */
        private static final int STATEMENT_LENGTH = 90;

        private static final int MAX_DEPTH = 4;

        /** An argument on a line of its own; the statements of main.swift stand at the margin. */
        private static final String ARGUMENT = "\n    ";

        /** Where the parenthesis of arguments put one per line closes: back at the statement. */
        private static final String STATEMENT = "\n";

        private static final String NIL_UUID = "UUID(uuidString: \"00000000-0000-0000-0000-000000000000\")!";

        /** A value of any type, or a free-form object, as the empty JSON object. */
        private static final String EMPTY_OBJECT = "[:]";

        private static final Pattern UUID_TEXT =
                Pattern.compile("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        private static final Pattern STRING_LITERAL = Pattern.compile("\"(?:\\\\.|[^\"\\\\])*\"");

        /** A type of Foundation a snippet names; the SDK does not export Foundation along with itself. */
        private static final Pattern FOUNDATION_TYPE =
                Pattern.compile("(?<![.\\w])(?:Date|UUID|URL|ISO8601DateFormatter)(?!\\w)");

        /** A date and time as ISO8601DateFormatter reads it by default: whole seconds and an offset. */
        private static final DateTimeFormatter DATE_TIME = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ssXXX");

        private final OpenAPI openAPI;
        private final Map<String, CodegenModel> models;

        /** Values the document states no example for, as "operationId: name", for the run's log. */
        private final List<String> placeholders = new ArrayList<>();

        Snippets(OpenAPI openAPI, List<ModelMap> allModels) {
            this.openAPI = openAPI;
            this.models = CallSnippets.models(allModels);
        }

        List<String> placeholders() {
            return placeholders;
        }

        void attach(CodegenOperation operation, String apiClass) {
            List<String> arguments = new ArrayList<>();
            String body = null;
            String bodyType = null;

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;

                if (parameter.isBodyParam) {
                    Shape shape = Shape.of(parameter);
                    body = body(operation, shape, CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);
                    bodyType = bodyType(shape);
                    arguments.add(parameter.paramName + ": body");
                    continue;
                }

                if (!parameter.required && !parameter.isFile && !parameter.isBinary) {
                    continue;
                }

                Object example = CallSnippets.plain(parameter.isFormParam
                        ? SpecExamples.formField(openAPI, operation, parameter.baseName)
                        : SpecExamples.parameter(openAPI, operation, parameter.baseName, SpecExamples.location(parameter)));

                // An enum declared in place in a parameter is a type nested in the API class.
                Shape shape = Shape.of(parameter);
                if (parameter.isEnum) {
                    shape.enumType = apiClass + "." + parameter.enumName + "_" + operation.operationId;
                }

                arguments.add(parameter.paramName + ": " + value(shape, example, parameter.baseName, where, 0));
            }

            // Every method takes the configuration last; the snippet's carries the portal and the token.
            arguments.add("apiConfiguration: config");

            // The call is a statement at the margin, so the whole line counts: what stands before the
            // arguments is long here, a type and a method name after `try await`.
            String statement = (operation.returnType != null ? "let result = " : "") + "try await "
                    + apiClass + "." + operation.operationId + "()";
            String call = arguments(arguments, statement.length());
            operation.vendorExtensions.put(ARGUMENTS, call);
            if (body != null) {
                operation.vendorExtensions.put(BODY, body);
                if (bodyType != null) {
                    operation.vendorExtensions.put(BODY_TYPE, bodyType);
                }
            }

            if (bearer(operation)) {
                operation.vendorExtensions.put(BEARER, true);
            }

            if (foundation(call, body, bodyType)) {
                operation.vendorExtensions.put(FOUNDATION, true);
            }
        }

        /**
         * Whether the operation states a bearer scheme. The SDK has no authorization of its own: the
         * token goes as a header of the configuration, with every request made with it, so an
         * operation that takes no token gets a configuration without one.
         */
        private static boolean bearer(CodegenOperation operation) {
            if (operation.authMethods != null) {
                for (CodegenSecurity security : operation.authMethods) {
                    if (Boolean.TRUE.equals(security.isBasicBearer)) {
                        return true;
                    }
                }
            }

            return false;
        }

        /** Whether the snippet names a type of Foundation, read from its code. */
        private static boolean foundation(String... code) {
            for (String part : code) {
                if (part != null && FOUNDATION_TYPE.matcher(STRING_LITERAL.matcher(part).replaceAll("\"\"")).find()) {
                    return true;
                }
            }

            return false;
        }

        private CodegenModel bodyModel(Shape shape) {
            return shape.isArray ? null : model(shape);
        }

        /**
         * The type `let body` is declared with, or null for a model, which its initializer names. A
         * literal alone would give a collection, a number or a JSON value a type of its own choosing.
         */
        private String bodyType(Shape shape) {
            CodegenModel model = bodyModel(shape);
            return model != null || shape.dataType == null ? null : plainType(shape.dataType);
        }

        /**
         * The request body. A model gets its required fields only, whatever the document states for
         * the body as a whole; an array or a scalar takes the example the document states for it.
         */
        private String body(CodegenOperation operation, Shape shape, Object example, String where) {
            CodegenModel model = bodyModel(shape);

            // A body that is an enum schema reaches the SDK as a string: the value of a member, chosen
            // as for any other enum.
            CodegenModel enumBody = model == null && "String".equals(plainType(shape.dataType))
                    ? CallSnippets.enumBody(openAPI, operation, models)
                    : null;
            if (enumBody != null) {
                Map<?, ?> member = member(enumBody.allowableValues, example, where);
                return CallSnippets.quote(CallSnippets.unquote(String.valueOf(member.get("value"))));
            }

            if (model == null || model.isEnum) {
                return value(shape, example, "body", where, 0);
            }

            List<String> fields = fields(model, where, 0);
            String line = model.classname + "(" + String.join(", ", fields) + ")";

            return fields.isEmpty() || line.length() <= LINE_LENGTH
                    ? line
                    : model.classname + "(" + ARGUMENT + String.join("," + ARGUMENT, fields) + STATEMENT + ")";
        }

        private String construct(CodegenModel model, String where, int depth) {
            List<String> fields = fields(model, where, depth);

            // A model can contain itself; past this depth it is not built, and the place is left to the
            // reader.
            if (fields == null) {
                placeholders.add(where);
                return model.classname + "()";
            }

            return model.classname + "(" + String.join(", ", fields) + ")";
        }

        /**
         * The model's required fields, as labelled arguments of its initializer in the order it declares
         * them; null past the depth a model with required fields is built to. A model composed with allOf
         * is one struct with the fields of its parent among its own.
         */
        private List<String> fields(CodegenModel model, String where, int depth) {
            List<String> fields = new ArrayList<>();

            if (model.allVars == null || model.allVars.isEmpty()) {
                if (CallSnippets.isComposed(model)) {
                    // The SDK makes a oneOf or anyOf a struct with no field to hold one of its variants.
                    placeholders.add(where);
                }
                return fields;
            }

            Set<String> seen = new HashSet<>();
            for (CodegenProperty property : model.allVars) {
                if (!property.required || !seen.add(property.name)) {
                    continue;
                }

                if (depth >= MAX_DEPTH) {
                    return null;
                }

                Object example = CallSnippets.plain(SpecExamples.inheritedProperty(openAPI, model.name, property.baseName));
                fields.add(property.name + ": " + value(Shape.of(property, model.classname), example,
                        property.baseName, where + "." + property.baseName, depth + 1));
            }

            return fields;
        }

        private String value(Shape shape, Object example, String name, String where, int depth) {
            if (shape.isFileList) {
                return "[" + file() + "]";
            }

            if (shape.isFile) {
                return file();
            }

            String enumValue = enumValue(shape, example, where);
            if (enumValue != null) {
                return enumValue;
            }

            if (shape.isArray) {
                if (shape.items == null) {
                    return typed(shape.dataType, example, name, where, depth);
                }

                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(value(shape.item(), CallSnippets.plain(item), name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    // A list the document requires items in gets one, standing in for them: an empty
                    // one would not make a valid request.
                    if (shape.minItems != null && shape.minItems > 0) {
                        items.add(value(shape.item(), null, name, where, depth + 1));
                    } else {
                        placeholders.add(where);
                    }
                }

                return "[" + String.join(", ", items) + "]";
            }

            if (shape.isMap) {
                return map(shape, example, name, where, depth);
            }

            // A free-form object is a dictionary of JSON values to the SDK.
            if (shape.isFreeFormObject) {
                return example instanceof Map ? literal(example) : EMPTY_OBJECT;
            }

            CodegenModel model = model(shape);
            if (model != null) {
                return construct(model, where, depth);
            }

            return scalar(shape.dataType, shape.isDate, example, name, where);
        }

        /**
         * A dictionary with the entries of the example; one the document requires entries in and gives
         * no example for gets one standing in for them, which the log notes.
         */
        private String map(Shape shape, Object example, String name, String where, int depth) {
            List<String> entries = new ArrayList<>();
            if (example instanceof Map) {
                for (Map.Entry<?, ?> entry : ((Map<?, ?>) example).entrySet()) {
                    entries.add(CallSnippets.quote(String.valueOf(entry.getKey())) + ": "
                            + entry(shape, CallSnippets.plain(entry.getValue()), name, where, depth));
                }
            }

            if (entries.isEmpty() && shape.minProperties != null && shape.minProperties > 0) {
                int logged = placeholders.size();
                entries.add(CallSnippets.quote("YOUR_KEY") + ": " + entry(shape, null, name, where, depth));
                if (placeholders.size() == logged) {
                    placeholders.add(where);
                }
            }

            return entries.isEmpty() ? EMPTY_OBJECT : "[" + String.join(", ", entries) + "]";
        }

        /** A value of a dictionary. A body parameter carries no item description, only the Swift type. */
        private String entry(Shape shape, Object example, String name, String where, int depth) {
            if (shape.items != null) {
                return value(shape.item(), example, name, where, depth + 1);
            }

            return typed(valueType(plainType(shape.dataType)), example, name, where, depth + 1);
        }

        /** A value of a type the generator names by its Swift spelling only: [Int], [String: Int], Set<String>. */
        private String typed(String type, Object example, String name, String where, int depth) {
            String plainType = plainType(type);

            if (plainType.startsWith("[") && valueType(plainType) != null) {
                List<String> entries = new ArrayList<>();
                if (example instanceof Map) {
                    for (Map.Entry<?, ?> entry : ((Map<?, ?>) example).entrySet()) {
                        entries.add(CallSnippets.quote(String.valueOf(entry.getKey())) + ": "
                                + typed(valueType(plainType), CallSnippets.plain(entry.getValue()), name, where, depth + 1));
                    }
                }
                return entries.isEmpty() ? EMPTY_OBJECT : "[" + String.join(", ", entries) + "]";
            }

            if (plainType.startsWith("[") || plainType.startsWith("Set<")) {
                String itemType = plainType.substring(plainType.indexOf(plainType.startsWith("[") ? '[' : '<') + 1,
                        plainType.length() - 1);
                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(typed(itemType, CallSnippets.plain(item), name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                }

                return "[" + String.join(", ", items) + "]";
            }

            CodegenModel model = models.get(plainType);
            if (model == null) {
                return scalar(plainType, false, example, name, where);
            }

            if (model.isEnum) {
                Shape shape = new Shape();
                shape.complexType = plainType;
                return enumValue(shape, example, where);
            }

            return construct(model, where, depth);
        }

        /** A file, read by the SDK from the path it is given. */
        private static String file() {
            return "URL(fileURLWithPath: \"file.docx\")";
        }

        /**
         * An enum member. An enum schema is an enum of the SDK, and so is an enum declared in place in a
         * model - a type nested in it - or in a parameter, nested in the API class.
         */
        private String enumValue(Shape shape, Object example, String where) {
            // An array or a dictionary of enum values is built a value at a time.
            if (shape.isArray || shape.isMap) {
                return null;
            }

            // A parameter typed by an enum schema names it in its type rather than as a model.
            CodegenModel model = model(shape);
            if (model == null && shape.dataType != null) {
                model = models.get(plainType(shape.dataType));
            }

            String type;
            Map<String, Object> allowableValues;
            if (model != null && model.isEnum) {
                type = model.classname;
                allowableValues = model.allowableValues;
            } else if (shape.isEnum) {
                type = shape.enumType;
                allowableValues = shape.allowableValues;
            } else {
                return null;
            }

            Object values = allowableValues == null ? null : allowableValues.get("enumVars");
            if (!(values instanceof List) || ((List<?>) values).isEmpty()) {
                return null;
            }

            Map<?, ?> chosen = member(allowableValues, example, where);
            return type == null ? String.valueOf(chosen.get("value")) : type + "." + chosen.get("name");
        }

        /** The member of an enum the example names, or else the first one, which the log notes. */
        private Map<?, ?> member(Map<String, Object> allowableValues, Object example, String where) {
            Map<?, ?> chosen = CallSnippets.member(allowableValues, example);
            if (chosen == null) {
                placeholders.add(where);
                chosen = (Map<?, ?>) ((List<?>) allowableValues.get("enumVars")).get(0);
            }

            return chosen;
        }

        private CodegenModel model(Shape shape) {
            if (shape.complexType != null && models.containsKey(shape.complexType)) {
                return models.get(shape.complexType);
            }

            return shape.isModel ? models.get(plainType(shape.dataType)) : null;
        }

        /**
         * A value of a plain type, as the literal of the Swift type it has in the SDK. Swift gives a
         * number literal the integer or floating-point type of the place it stands in.
         */
        private String scalar(String type, boolean isDate, Object example, String name, String where) {
            String simple = type == null ? "" : plainType(type);
            String text = example == null ? null : SpecExamples.text(example);

            switch (simple) {
                case "Date":
                    return isDate ? date(text, where) : dateTime(text, where);
                case "UUID":
                    return uuid(text, where);
                case "JSONValue":
                    if (example != null) {
                        return literal(example);
                    }
                    placeholders.add(where);
                    return EMPTY_OBJECT;
                default:
                    break;
            }

            if (text == null) {
                placeholders.add(where);
                return placeholder(simple, name);
            }

            switch (simple) {
                case "String":
                    return CallSnippets.quote(text);
                case "Int":
                case "Int32":
                case "Int64":
                case "Double":
                case "Float":
                case "Decimal":
                    return text;
                case "Bool":
                    return text.toLowerCase(Locale.ROOT);
                case "URL":
                    return "URL(string: " + CallSnippets.quote(text) + ")!";
                default:
                    return "nil";
            }
        }

        private static String placeholder(String type, String name) {
            switch (type) {
                case "String":
                    return CallSnippets.quote(CallSnippets.placeholderName(name));
                case "Int":
                case "Int32":
                case "Int64":
                case "Double":
                case "Float":
                case "Decimal":
                    return "0";
                case "Bool":
                    return "false";
                case "URL":
                    return "URL(string: " + CallSnippets.quote(CallSnippets.placeholderName(name)) + ")!";
                default:
                    return "nil";
            }
        }

        /**
         * A date and time, read by Foundation's ISO8601DateFormatter, which by default takes whole
         * seconds and an offset; a fraction of a second the example states is dropped.
         */
        private String dateTime(String text, String where) {
            if (text != null) {
                try {
                    return isoDate(OffsetDateTime.parse(text).truncatedTo(ChronoUnit.SECONDS).format(DATE_TIME));
                } catch (DateTimeParseException notADateTime) {
                    try {
                        LocalDate.parse(text);
                        return isoDate(text + "T00:00:00Z");
                    } catch (DateTimeParseException notADate) {
                        // Falls through to the placeholder.
                    }
                }
            }

            placeholders.add(where);
            return "Date()";
        }

        /** A date: the SDK holds it as a Date too, at midnight UTC. */
        private String date(String text, String where) {
            if (text != null) {
                String day = text.length() > 10 ? text.substring(0, 10) : text;
                try {
                    LocalDate.parse(day);
                    return isoDate(day + "T00:00:00Z");
                } catch (DateTimeParseException notADate) {
                    // Falls through to the placeholder.
                }
            }

            placeholders.add(where);
            return "Date()";
        }

        private static String isoDate(String text) {
            return "ISO8601DateFormatter().date(from: " + CallSnippets.quote(text) + ")!";
        }

        private String uuid(String text, String where) {
            // UUID(uuidString:) gives nil for a text that is not one, so an example such as "1" counts as none.
            if (text == null || !UUID_TEXT.matcher(text).matches()) {
                placeholders.add(where);
                return NIL_UUID;
            }

            return "UUID(uuidString: " + CallSnippets.quote(text) + ")!";
        }

        /** An example of a value of any type, as the literal of the JSON value it holds. */
        private static String literal(Object example) {
            if (example == null) {
                return "nil";
            }
            if (example instanceof String) {
                return CallSnippets.quote((String) example);
            }
            if (example instanceof List) {
                List<String> items = new ArrayList<>();
                for (Object item : (List<?>) example) {
                    items.add(literal(CallSnippets.plain(item)));
                }
                return "[" + String.join(", ", items) + "]";
            }
            if (example instanceof Map) {
                List<String> entries = new ArrayList<>();
                for (Map.Entry<?, ?> entry : ((Map<?, ?>) example).entrySet()) {
                    entries.add(CallSnippets.quote(String.valueOf(entry.getKey())) + ": "
                            + literal(CallSnippets.plain(entry.getValue())));
                }
                return entries.isEmpty() ? EMPTY_OBJECT : "[" + String.join(", ", entries) + "]";
            }

            return SpecExamples.text(example);
        }

        /** The arguments on one line, or one per line once the statement they stand in gets long. */
        private static String arguments(List<String> arguments, int around) {
            String line = String.join(", ", arguments);
            if (around + line.length() <= STATEMENT_LENGTH) {
                return line;
            }

            return ARGUMENT + String.join("," + ARGUMENT, arguments) + STATEMENT;
        }

        /** A type without the mark of an optional. */
        private static String plainType(String type) {
            return type != null && type.endsWith("?") ? type.substring(0, type.length() - 1) : type;
        }

        /** The value type of a dictionary type, [String: Int] giving Int; null for any other type. */
        private static String valueType(String type) {
            if (type == null || !type.startsWith("[") || !type.endsWith("]")) {
                return null;
            }

            int depth = 0;
            for (int i = 1; i < type.length() - 1; i++) {
                char c = type.charAt(i);
                if (c == '[' || c == '<') {
                    depth++;
                } else if (c == ']' || c == '>') {
                    depth--;
                } else if (c == ':' && depth == 0) {
                    return type.substring(i + 1, type.length() - 1).trim();
                }
            }

            return null;
        }

        /** What the snippet needs to know about the type of a parameter or a property. */
        private static final class Shape {
            String dataType;
            String complexType;
            String enumType;
            boolean isEnum;
            boolean isArray;
            boolean isMap;
            boolean isFreeFormObject;
            boolean isModel;
            boolean isDate;
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
                shape.isModel = parameter.isModel;
                shape.isDate = parameter.isDate;
                shape.isFile = parameter.isFile || parameter.isBinary;
                shape.isFileList = (shape.isFile && parameter.isCollectionFormatMulti)
                        || (parameter.isArray && parameter.items != null && (parameter.items.isFile || parameter.items.isBinary));
                shape.minItems = parameter.minItems;
                shape.minProperties = parameter.getMinProperties();
                shape.allowableValues = parameter.allowableValues;
                shape.items = parameter.items;
                return shape;
            }

            /** The shape of a property; an enum declared in place in it is a type nested in its model. */
            static Shape of(CodegenProperty property, String owner) {
                Shape shape = new Shape();
                shape.dataType = property.dataType;
                shape.complexType = property.complexType;
                shape.enumType = owner == null ? null : owner + "." + nestedEnum(property.datatypeWithEnum);
                shape.isEnum = property.isEnum;
                shape.isArray = property.isArray;
                shape.isMap = property.isMap;
                shape.isFreeFormObject = property.isFreeFormObject;
                shape.isModel = property.isModel;
                shape.isDate = property.isDate;
                shape.isFile = property.isFile || property.isBinary;
                shape.isFileList = property.isArray && property.items != null && (property.items.isFile || property.items.isBinary);
                shape.minItems = property.minItems;
                shape.minProperties = property.getMinProperties();
                shape.allowableValues = property.allowableValues;
                shape.items = property.items;
                return shape;
            }

            /** The enum type a property's type names: Role in Role, [Role] or [String: Role]. */
            private static String nestedEnum(String type) {
                String name = type == null ? "" : plainType(type);
                String value = valueType(name);
                if (value != null) {
                    name = value;
                }
                return name.replace("[", "").replace("]", "").replace("?", "").trim();
            }

            /** The shape of an item of this array; an enum declared in place for the items is the array's. */
            Shape item() {
                Shape item = of(items, null);
                if (item.isEnum) {
                    item.enumType = enumType;
                }
                return item;
            }
        }
    }
}