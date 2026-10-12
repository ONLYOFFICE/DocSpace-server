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

import org.openapitools.codegen.languages.PhpClientCodegen;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.*;
import io.swagger.v3.oas.models.headers.*;
import static org.openapitools.codegen.utils.StringUtils.camelize;
import org.openapitools.codegen.*;
import org.openapitools.codegen.model.*;

import java.util.*;
import java.io.File;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.Map.Entry;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.stream.Collectors;

public class MyPHPClientCodegen extends PhpClientCodegen {
    
    /**
     * The call snippets of the reference pages instead of the SDK: one file, with a snippet per
     * operation, written into the folder the run was given with -o.
     */
    private boolean snippetsOnly;

    private Snippets snippets;

    public MyPHPClientCodegen() {
        super();
        this.templateDir = "templates/php";
        this.embeddedTemplateDir = "php";
    }

    @Override
    public void processOpts() {
        super.processOpts();

        // Returns before the output folder is pointed at the SDK checkout (see CallSnippets.configure).
        snippetsOnly = CallSnippets.configure(this);
        if (snippetsOnly) {
            return;
        }

        this.outputFolder = "../../../../../sdk/docspace-api-sdk-php";

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

        if(Boolean.TRUE.equals(additionalProperties.get("excludeTests")))
        {
            modelTestTemplateFiles.clear();
            apiTestTemplateFiles.clear();
        }

        supportingFiles.add(new SupportingFile(
            "AUTHORS.mustache", "", "AUTHORS.md"
        ));

        supportingFiles.add(new SupportingFile(
            "LICENSE.mustache", "", "LICENSE"
        ));

        supportingFiles.add(new SupportingFile(
            "CHANGELOG.mustache", "", "CHANGELOG.md"
        ));

        supportingFiles.add(new SupportingFile("sample.mustache", "samples", "main.php"));
    }

    // The third-party twin of a generic action (see ThirdPartyVariants): the string-id shape the document
    // carries as `x-thirdparty-variant`, exposed as a sibling method with the ThirdParty suffix.
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
     * (see ThirdPartyVariants) gets none: it is the same method, on the same page.
     */
    private void attachSnippets(OperationsMap objs, List<ModelMap> allModels) {
        if (snippets == null) {
            snippets = new Snippets(openAPI, allModels, invokerPackage, modelPackage);
        }

        // The API class of every section lives in a namespace of its own, as the SDK's sample imports it.
        OperationMap operations = objs.getOperations();
        String apiImport = apiPackage + "\\" + operations.get("x-folder") + "\\" + operations.get("x-classname");

        CallSnippets.attach(objs, operation -> snippets.attach(operation, apiImport));
    }

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        // PHP has neither overloads nor generics, so the twin stays attached: the docblocks carry
        // int|string and the union of the answers, and the response model is picked at run time
        // by the id's type (see ThirdPartyVariants.markUnions).
        ThirdPartyVariants.addAttachedImports(this, objs);

        super.postProcessOperationsWithModels(objs, allModels);
        for (CodegenOperation op : objs.getOperations().getOperation()) {
            ThirdPartyVariants.markUnions(op, (a, b) -> a + "|" + b);
        }

        if (objs != null && objs.getOperations() != null) {
            OperationMap operationMap = objs.getOperations();
            List<CodegenOperation> operationList = operationMap.getOperation();
            String className = operationMap.getClassname();
            if (className != null && className.endsWith(apiNameSuffix)) {
                className = className.substring(0, className.length() - 3);
            }
            TagParts tagParts = tagMap.get(camelize(className));
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
            TagParts tagParts = tagMap.get(camelize(className));

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
        return "my-php";
    }

    @Override
    public String getHelp() {
        return "Generates a custom PHP client";
    }

    /**
     * Builds what the call snippet of one operation needs beyond the template, as PHP: the named
     * arguments of the call, the request body, the imports, and whether the operation takes the token.
     * <p>
     * The body is the minimal one, as for C# (see {@link MyCSharpClientCodegen}): a model gets its
     * required fields only, through its setters, with the values the document states for them. Every
     * optional parameter of a call has a default in the SDK, so it is left out.
     */
    private static final class Snippets {

        static final String ARGUMENTS = "x-snippet-arguments";
        static final String BODY = "x-snippet-body";
        static final String IMPORTS = "x-snippet-imports";
        static final String BEARER = "x-snippet-bearer";

        /** Beyond this length the arguments are put one per line, and so are the setters of the body. */
        private static final int LINE_LENGTH = 60;

        private static final int MAX_DEPTH = 4;

        /** An argument or a setter on a line of its own, inside a statement of the script. */
        private static final String INDENT = "\n    ";

        private static final String FILE = "new SplFileObject(\"file.docx\")";

        private static final String NIL_UUID = "00000000-0000-0000-0000-000000000000";

        /**
         * A free-form object, a map or a value of any type, as the empty JSON object: the SDK writes an
         * empty PHP array as the empty JSON array.
         */
        private static final String EMPTY_OBJECT = "new stdClass()";

        private static final Pattern UUID_TEXT =
                Pattern.compile("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        private static final Pattern STRING_LITERAL = Pattern.compile("\"(?:\\\\.|[^\"\\\\])*\"");

        /** A class the snippet names: RoomType in RoomType::FILLING_FORMS_ROOM, but neither the constant nor a method. */
        private static final Pattern TYPE = Pattern.compile("(?<![\\w$:>\\\\])[A-Z][A-Za-z0-9_]*");

        private final OpenAPI openAPI;
        private final String invokerPackage;
        private final String modelPackage;
        private final Map<String, CodegenModel> models;

        /** Values the document states no example for, as "operationId: name", for the run's log. */
        private final List<String> placeholders = new ArrayList<>();

        Snippets(OpenAPI openAPI, List<ModelMap> allModels, String invokerPackage, String modelPackage) {
            this.openAPI = openAPI;
            this.invokerPackage = invokerPackage;
            this.modelPackage = modelPackage;
            this.models = CallSnippets.models(allModels);
        }

        List<String> placeholders() {
            return placeholders;
        }

        void attach(CodegenOperation operation, String apiImport) {
            List<String> arguments = new ArrayList<>();
            String body = null;

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;

                if (parameter.isBodyParam) {
                    body = body(operation, Shape.of(parameter), CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);
                    arguments.add(parameter.paramName + ": $body");
                    continue;
                }

                if (!parameter.required && !parameter.isFile && !parameter.isBinary) {
                    continue;
                }

                Object example = CallSnippets.plain(parameter.isFormParam
                        ? SpecExamples.formField(openAPI, operation, parameter.baseName)
                        : SpecExamples.parameter(openAPI, operation, parameter.baseName, SpecExamples.location(parameter)));

                arguments.add(parameter.paramName + ": " + value(Shape.of(parameter), example, parameter.baseName, where, 0));
            }

            String call = arguments(arguments);
            operation.vendorExtensions.put(ARGUMENTS, call);
            if (body != null) {
                operation.vendorExtensions.put(BODY, body);
            }

            if (takesToken(operation)) {
                operation.vendorExtensions.put(BEARER, true);
            }

            operation.vendorExtensions.put(IMPORTS, imports(apiImport, call, body));
        }

        /**
         * Whether the operation goes with the token. The configuration holds it for every call, and each
         * call sends it only when one of its schemes is a bearer one, so an operation without such a
         * scheme is shown without it.
         */
        private static boolean takesToken(CodegenOperation operation) {
            if (operation.authMethods != null) {
                for (CodegenSecurity security : operation.authMethods) {
                    if (Boolean.TRUE.equals(security.isBasicBearer) || Boolean.TRUE.equals(security.isOAuth)) {
                        return true;
                    }
                }
            }

            return false;
        }

        /**
         * The imports: the API class, the configuration and the models the snippet names, as the SDK's
         * sample has them. What the snippet names is read from its code; the classes of PHP itself need
         * no import in a script outside any namespace.
         */
        private String imports(String apiImport, String... code) {
            Set<String> names = new TreeSet<>();
            names.add(apiImport);
            names.add(invokerPackage + "\\Configuration");

            for (String part : code) {
                if (part == null) {
                    continue;
                }

                Matcher type = TYPE.matcher(STRING_LITERAL.matcher(part).replaceAll("\"\""));
                while (type.find()) {
                    if (models.containsKey(type.group())) {
                        names.add(modelPackage + "\\" + type.group());
                    }
                }
            }

            StringBuilder builder = new StringBuilder();
            for (String name : names) {
                builder.append("use ").append(name).append(";\n");
            }

            return builder.substring(0, builder.length() - 1);
        }

        /**
         * The request body. A model gets its required fields only, whatever the document states for
         * the body as a whole; an array or a scalar takes the example the document states for it.
         */
        private String body(CodegenOperation operation, Shape shape, Object example, String where) {
            CodegenModel model = shape.isArray ? null : model(shape);

            // A body that is an enum schema reaches the SDK as a string: the constant of a member,
            // chosen as for any other enum.
            if (model == null && "string".equals(shape.dataType)) {
                CodegenModel enumBody = CallSnippets.enumBody(openAPI, operation, models);
                if (enumBody != null) {
                    return enumConstant(enumBody, example, where);
                }
            }

            if (model == null || model.isEnum || CallSnippets.isComposed(model)) {
                return value(shape, example, "body", where, 0);
            }

            List<String> setters = setters(model, where, 0);
            if (setters.isEmpty()) {
                return "new " + model.classname + "()";
            }

            String line = "(new " + model.classname + "())->" + String.join("->", setters);
            return line.length() <= LINE_LENGTH
                    ? line
                    : "(new " + model.classname + "())" + INDENT + "->" + String.join(INDENT + "->", setters);
        }

        private String construct(CodegenModel model, String where, int depth) {
            List<String> setters = setters(model, where, depth);

            // A model can contain itself; past this depth it is not built, and the place is left to the reader.
            if (setters == null) {
                placeholders.add(where);
                return "new " + model.classname + "()";
            }

            return setters.isEmpty()
                    ? "new " + model.classname + "()"
                    : "(new " + model.classname + "())->" + String.join("->", setters);
        }

        /**
         * The model's required fields, as calls of their setters; null past the depth a model with
         * required fields is built to. A model composed with allOf has the setters of its parent too.
         */
        private List<String> setters(CodegenModel model, String where, int depth) {
            List<String> setters = new ArrayList<>();
            if (model.allVars == null) {
                return setters;
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
                setters.add(property.setter + "(" + value(Shape.of(property, model.classname), example,
                        property.baseName, where + "." + property.baseName, depth + 1) + ")");
            }

            return setters;
        }

        private String value(Shape shape, Object example, String name, String where, int depth) {
            if (shape.isFileList) {
                return "[" + FILE + "]";
            }

            if (shape.isFile) {
                return FILE;
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
                        items.add(value(shape.item(), item, name, where, depth + 1));
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

            if (shape.isFreeFormObject) {
                return EMPTY_OBJECT;
            }

            CodegenModel model = model(shape);
            if (model != null) {
                return CallSnippets.isComposed(model) ? composed(model, example, name, where, depth) : construct(model, where, depth);
            }

            return scalar(shape.dataType, shape.isUuid, example, name, where);
        }

        /**
         * A map, as the PHP array the SDK takes it as, with the entries of the example; one the document
         * requires entries in and gives no example for gets one standing in for them. The setter counts
         * the entries of such a map, so it has to be an array; an empty map is the empty object, which an
         * empty array would not make.
         */
        private String map(Shape shape, Object example, String name, String where, int depth) {
            List<String> entries = new ArrayList<>();
            if (example instanceof Map && shape.items != null) {
                for (Map.Entry<?, ?> entry : ((Map<?, ?>) example).entrySet()) {
                    entries.add(quote(String.valueOf(entry.getKey())) + " => "
                            + value(shape.item(), CallSnippets.plain(entry.getValue()), name, where, depth + 1));
                }
            }

            if (entries.isEmpty()) {
                if (shape.minProperties == null || shape.minProperties == 0 || shape.items == null) {
                    return EMPTY_OBJECT;
                }

                placeholders.add(where);
                entries.add(quote("YOUR_KEY") + " => " + value(shape.item(), null, name, where, depth + 1));
            }

            return "[" + String.join(", ", entries) + "]";
        }

        /**
         * A value of a oneOf or anyOf. The SDK's class for it holds no value, and the serializer writes
         * whatever it is given as it is, so the snippet gives the variant that fits the example in its
         * place, or the first one.
         */
        private String composed(CodegenModel model, Object example, String name, String where, int depth) {
            // A variant can be a oneOf of its own and lead back here, so it stops at the depth the models do.
            if (depth >= MAX_DEPTH) {
                placeholders.add(where);
                return "null";
            }

            List<String> variants = new ArrayList<>(model.oneOf != null && !model.oneOf.isEmpty() ? model.oneOf : model.anyOf);

            String chosen = null;
            for (String variant : variants) {
                if (fits(variant, example)) {
                    chosen = variant;
                    break;
                }
            }

            if (chosen == null) {
                chosen = variants.get(0);
                example = null;
            }

            return typed(chosen, example, name, where, depth + 1);
        }

        private boolean fits(String type, Object example) {
            if (example == null) {
                return false;
            }

            if (type.endsWith("[]")) {
                return example instanceof List;
            }

            switch (type) {
                case "string":
                    return example instanceof String;
                case "int":
                    return example instanceof Integer || example instanceof Long;
                case "float":
                    return example instanceof Number;
                case "bool":
                    return example instanceof Boolean;
                default:
                    CodegenModel model = models.get(simpleType(type));
                    if (model == null) {
                        return false;
                    }

                    // An enum fits only an example that names one of its members; any other is left to
                    // a variant beside it, such as a string.
                    return model.isEnum ? member(model.allowableValues, example) != null : example instanceof Map;
            }
        }

        /** A value of a type the generator names by its PHP spelling only, as the variants of a oneOf. */
        private String typed(String type, Object example, String name, String where, int depth) {
            if (type.endsWith("[]")) {
                String itemType = type.substring(0, type.length() - 2);

                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(typed(itemType, item, name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                }

                return "[" + String.join(", ", items) + "]";
            }

            if (type.startsWith("array<")) {
                return EMPTY_OBJECT;
            }

            CodegenModel model = models.get(simpleType(type));
            if (model == null) {
                return scalar(type, false, example, name, where);
            }

            if (model.isEnum) {
                return enumConstant(model, example, where);
            }

            return CallSnippets.isComposed(model) ? composed(model, example, name, where, depth) : construct(model, where, depth);
        }

        /**
         * An enum member. An enum schema is a class of constants in the SDK, and so is an enum declared
         * in place in a model, as constants of that model; in a parameter it is only its values.
         */
        private String enumValue(Shape shape, Object example, String where) {
            if (shape.isArray) {
                return null;
            }

            CodegenModel model = model(shape);
            if (model != null && model.isEnum) {
                return enumConstant(model, example, where);
            }

            Object values = shape.allowableValues == null ? null : shape.allowableValues.get("enumVars");
            if (!shape.isEnum || !(values instanceof List) || ((List<?>) values).isEmpty()) {
                return null;
            }

            Map<?, ?> chosen = member(shape.allowableValues, example, where);
            return shape.owner == null
                    ? literal(String.valueOf(chosen.get("value")))
                    : shape.owner + "::" + shape.enumName + "_" + chosen.get("name");
        }

        private String enumConstant(CodegenModel model, Object example, String where) {
            return model.classname + "::" + member(model.allowableValues, example, where).get("name");
        }

        /** The member of an enum the example names, or else the first one, which the log notes. */
        private Map<?, ?> member(Map<String, Object> allowableValues, Object example, String where) {
            Map<?, ?> chosen = member(allowableValues, example);
            if (chosen == null) {
                placeholders.add(where);
                chosen = (Map<?, ?>) ((List<?>) allowableValues.get("enumVars")).get(0);
            }

            return chosen;
        }

        /**
         * The member of an enum the example names (see CallSnippets.member). The PHP generator writes
         * the values of a string enum as PHP literals in single quotes, so they are handed over as the
         * plain text CallSnippets reads; and it writes the names of the members in capitals between
         * underscores, so an example that names a member as the document does, Active or
         * FillingFormsRoom, is matched against ACTIVE or FILLING_FORMS_ROOM without them.
         */
        private static Map<?, ?> member(Map<String, Object> allowableValues, Object example) {
            Object values = allowableValues == null ? null : allowableValues.get("enumVars");
            if (!(values instanceof List)) {
                return null;
            }

            List<?> members = (List<?>) values;
            List<Map<Object, Object>> plain = new ArrayList<>();
            for (Object item : members) {
                Map<Object, Object> copy = new HashMap<>((Map<?, ?>) item);
                String value = String.valueOf(copy.get("value"));
                if (isSingleQuoted(value)) {
                    copy.put("value", singleQuoted(value));
                }
                plain.add(copy);
            }

            Map<String, Object> enumVars = new HashMap<>();
            enumVars.put("enumVars", plain);

            Map<?, ?> found = CallSnippets.member(enumVars, example);
            for (int i = 0; i < plain.size(); i++) {
                if (plain.get(i) == found) {
                    return (Map<?, ?>) members.get(i);
                }
            }

            if (example != null) {
                String name = bareName(SpecExamples.text(example));
                for (Object item : members) {
                    if (name.equals(bareName(String.valueOf(((Map<?, ?>) item).get("name"))))) {
                        return (Map<?, ?>) item;
                    }
                }
            }

            return null;
        }

        private static String bareName(String name) {
            return name.replaceAll("[^A-Za-z0-9]", "").toLowerCase(Locale.ROOT);
        }

        /** The value of an enum member as the snippet writes it: a string in double quotes, as all its strings are. */
        private static String literal(String value) {
            return isSingleQuoted(value) ? quote(singleQuoted(value)) : value;
        }

        private static boolean isSingleQuoted(String value) {
            return value.length() >= 2 && value.startsWith("'") && value.endsWith("'");
        }

        /** The text of a PHP string literal in single quotes. */
        private static String singleQuoted(String value) {
            return value.substring(1, value.length() - 1).replace("\\'", "'").replace("\\\\", "\\");
        }

        private CodegenModel model(Shape shape) {
            CodegenModel model = models.get(simpleType(shape.complexType));
            if (model == null && !shape.isArray) {
                model = models.get(simpleType(shape.dataType));
            }

            return model;
        }

        /** A type without the namespace of the SDK's models: CreateRoomRequestDto for \OpenAPI\Client\Model\CreateRoomRequestDto. */
        private String simpleType(String type) {
            if (type == null) {
                return null;
            }

            String name = type.startsWith("\\") ? type.substring(1) : type;
            return name.startsWith(modelPackage + "\\") ? name.substring(modelPackage.length() + 1) : name;
        }

        /** A value of a plain type, as the PHP literal of the type it has in the SDK. */
        private String scalar(String type, boolean uuid, Object example, String name, String where) {
            String text = example == null ? null : SpecExamples.text(example);

            switch (type == null ? "" : type) {
                case "\\DateTime":
                    return dateTime(text, where);
                case "\\SplFileObject":
                    return FILE;
                case "mixed":
                case "object":
                    if (example instanceof Boolean || example instanceof Number || example instanceof String) {
                        return example instanceof String ? quote((String) example) : text;
                    }
                    if (example == null) {
                        placeholders.add(where);
                    }
                    return EMPTY_OBJECT;
                default:
                    break;
            }

            if (uuid) {
                return uuid(text, where);
            }

            if (text == null) {
                placeholders.add(where);
                return placeholder(type, name);
            }

            switch (type) {
                case "string":
                    return quote(text);
                case "int":
                case "float":
                    return text;
                case "bool":
                    return text.toLowerCase(Locale.ROOT);
                default:
                    return "null";
            }
        }

        private static String placeholder(String type, String name) {
            switch (type == null ? "" : type) {
                case "string":
                    return quote(CallSnippets.placeholderName(name));
                case "int":
                case "float":
                    return "0";
                case "bool":
                    return "false";
                default:
                    return "null";
            }
        }

        /** A date, or a date and time, as the SDK's own type, DateTime, made from the text the example states. */
        private String dateTime(String text, String where) {
            if (text != null) {
                try {
                    OffsetDateTime.parse(text);
                    return "new DateTime(" + quote(text) + ")";
                } catch (DateTimeParseException notADateTime) {
                    try {
                        LocalDate.parse(text);
                        return "new DateTime(" + quote(text) + ")";
                    } catch (DateTimeParseException notADate) {
                        // Falls through to the placeholder.
                    }
                }
            }

            placeholders.add(where);
            return "new DateTime()";
        }

        private String uuid(String text, String where) {
            // The SDK takes a uuid as a string and sends it as it is, so an example such as "1" counts as none.
            if (text == null || !UUID_TEXT.matcher(text).matches()) {
                placeholders.add(where);
                return quote(NIL_UUID);
            }

            return quote(text);
        }

        /** The arguments on one line, or one per line once they get long. */
        private static String arguments(List<String> arguments) {
            String line = String.join(", ", arguments);
            if (line.length() <= LINE_LENGTH) {
                return line;
            }

            return INDENT + String.join("," + INDENT, arguments) + "\n";
        }

        /**
         * A string literal in double quotes, as the SDK's sample writes them. Besides what C# and Go
         * escape, PHP reads a dollar sign in it as the start of a variable.
         */
        private static String quote(String text) {
            return "\"" + text
                    .replace("\\", "\\\\")
                    .replace("\"", "\\\"")
                    .replace("$", "\\$")
                    .replace("\r", "\\r")
                    .replace("\n", "\\n")
                    .replace("\t", "\\t") + "\"";
        }

        /** What the snippet needs to know about the type of a parameter or a property. */
        private static final class Shape {
            String dataType;
            String complexType;
            String owner;
            String enumName;
            boolean isEnum;
            boolean isArray;
            boolean isMap;
            boolean isFreeFormObject;
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
                shape.isUuid = parameter.isUuid;
                // A list of files is one parameter, an array of files, as the SDK declares it.
                shape.isFileList = parameter.isArray && (parameter.isFile || parameter.isBinary
                        || (parameter.items != null && (parameter.items.isFile || parameter.items.isBinary)));
                shape.isFile = !shape.isFileList && (parameter.isFile || parameter.isBinary);
                shape.minItems = parameter.minItems;
                shape.minProperties = parameter.getMinProperties();
                shape.allowableValues = parameter.allowableValues;
                shape.items = parameter.items;
                return shape;
            }

            /** The shape of a property; an enum declared in place in it is a set of constants of its model. */
            static Shape of(CodegenProperty property, String owner) {
                Shape shape = new Shape();
                shape.dataType = property.dataType;
                shape.complexType = property.complexType;
                shape.owner = owner;
                shape.enumName = property.enumName;
                shape.isEnum = property.isEnum;
                shape.isArray = property.isArray;
                shape.isMap = property.isMap;
                shape.isFreeFormObject = property.isFreeFormObject;
                shape.isUuid = property.isUuid;
                shape.isFileList = property.isArray && property.items != null && (property.items.isFile || property.items.isBinary);
                shape.isFile = !shape.isFileList && (property.isFile || property.isBinary);
                shape.minItems = property.minItems;
                shape.minProperties = property.getMinProperties();
                shape.allowableValues = property.allowableValues;
                shape.items = property.items;
                return shape;
            }

            /** The shape of an item of this array; an enum declared in place for the items is the array's. */
            Shape item() {
                Shape item = of(items, null);
                if (item.isEnum) {
                    item.owner = owner;
                    item.enumName = enumName;
                }
                return item;
            }
        }
    }
}