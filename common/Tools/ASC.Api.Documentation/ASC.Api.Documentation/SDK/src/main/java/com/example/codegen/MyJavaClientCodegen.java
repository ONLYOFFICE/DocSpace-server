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

import org.openapitools.codegen.languages.JavaClientCodegen;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.*;
import io.swagger.v3.oas.models.headers.*;
import static org.openapitools.codegen.utils.StringUtils.camelize;
import org.openapitools.codegen.*;
import org.openapitools.codegen.model.*;

import java.io.File;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.Map.Entry;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.stream.Collectors;
import java.util.*;

public class MyJavaClientCodegen extends JavaClientCodegen {

    /**
     * The call snippets of the reference pages instead of the SDK: one file, with a snippet per
     * operation, written into the folder the run was given with -o.
     */
    private boolean snippetsOnly;

    private Snippets snippets;
    
    public MyJavaClientCodegen() {
        super();
        this.templateDir = "templates/java";
        this.embeddedTemplateDir = "Java";
    }

    @Override
    public void processOpts() {
        super.processOpts();

        // Returns before the output folder is pointed at the SDK checkout (see CallSnippets.configure).
        snippetsOnly = CallSnippets.configure(this);
        if (snippetsOnly) {
            return;
        }

        this.outputFolder = "../../../../../sdk/docspace-api-sdk-java";

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
        supportingFiles.add(new SupportingFile("sample.mustache", "samples", "sample.java"));
        supportingFiles.add(new SupportingFile("auth/OpenIdAuth.mustache", this.authFolder, "OpenIdAuth.java"));
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
            snippets = new Snippets(openAPI, allModels, invokerPackage, modelPackage);
        }

        // The class of every section lives in a package of its own, as the SDK's sample imports it.
        OperationMap operations = objs.getOperations();
        String apiImport = apiPackage + "." + operations.get("x-folder") + "." + operations.get("x-classname");

        CallSnippets.attach(objs, operation -> snippets.attach(operation, apiImport));
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
            TagParts tagParts = tagMap.get(camelize(className));
            String baseClassName = tagParts.classPart + apiNameSuffix;
            String finalClassName = baseClassName;

            // if (usedClassNames.contains(finalClassName)) {
            //     finalClassName = camelize(tagParts.folderPart) + baseClassName;
            // }

            // usedClassNames.add(finalClassName);

            operationMap.put("x-folder", tagParts.folderPart);
            operationMap.put("x-classname", finalClassName);
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
    
    @Override
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

    @Override
    public ModelsMap postProcessModels(ModelsMap objs) {
        super.postProcessModels(objs);

        for (ModelMap mo : objs.getModels()) {
            CodegenModel model = mo.getModel();

            if ("ApiDateTime".equals(model.classname)) {
                model.vendorExtensions.put("isApiDateTime", true);

                for (CodegenProperty prop : model.vars) {
                    prop.isReadOnly = false;
                }
            }

            for (CodegenProperty prop : model.vars) {
                if ("version_Changed".equalsIgnoreCase(prop.baseName)) {
                    prop.name = "VersionChangedField";
                    prop.baseName = "versionChangedField";
                    prop.getter = "getVersionChangedField";
                    prop.setter = "setVersionChangedField";
                    prop.nameInCamelCase = "versionChangedField";
                    prop.nameInPascalCase = "VersionChangedField";
                    prop.nameInSnakeCase = "VERSION_CHANGED_FIELD";
                }
            }
            model.readWriteVars = model.vars.stream().filter(v -> !v.isReadOnly).collect(Collectors.toList());
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

        String baseFileName = toApiFilename(tagParts.classPart) + suffix;
        String fileName = baseFileName;
        // if (usedApiClassNames.contains(fileName)) {
        //     fileName = tagParts.folderPart + toApiFilename(tagParts.classPart) + suffix;
        // }
        // usedApiClassNames.add(fileName);

        return folderPath + File.separator + fileName;
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
        return "my-java";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Java client";
    }

    /**
     * Builds what the call snippet of one operation needs beyond the template, as Java: the arguments
     * of the call, the request body and its type, the imports, and the bearer scheme the token goes to.
     * <p>
     * The body is the minimal one, as for C# (see {@link MyCSharpClientCodegen}): a model gets its
     * required fields only, through its fluent setters, with the values the document states for them.
     * Java has neither default nor named arguments, so every parameter takes its place in the call, an
     * optional one as null.
     */
    private static final class Snippets {

        static final String ARGUMENTS = "x-snippet-arguments";
        static final String BODY = "x-snippet-body";
        static final String BODY_TYPE = "x-snippet-body-type";
        static final String IMPORTS = "x-snippet-imports";
        static final String BEARER = "x-snippet-bearer";

        /** Beyond this length the arguments are put one per line, and so are the setters of the body. */
        private static final int LINE_LENGTH = 60;

        private static final int MAX_DEPTH = 4;

        /** A line that continues a statement of main, which stands eight spaces in. */
        private static final String CONTINUATION = "\n                ";

        /** Where the parenthesis of arguments put one per line closes: back at the statement. */
        private static final String STATEMENT = "\n        ";

        private static final String FILE = "new File(\"file.docx\")";

        private static final String NIL_UUID = "new UUID(0L, 0L)";

        private static final Pattern UUID_TEXT =
                Pattern.compile("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        /** The value of a numeric enum member as the generator writes it: 1, 1L, 1.5f, new BigDecimal("1"). */
        private static final Pattern NUMBER_LITERAL =
                Pattern.compile("new BigDecimal\\(\"([^\"]*)\"\\)|(-?[0-9.]+(?:[eE][-+]?[0-9]+)?)[lLfFdD]?");

        private static final Pattern STRING_LITERAL = Pattern.compile("\"(?:\\\\.|[^\"\\\\])*\"");
        private static final Pattern COMMENT = Pattern.compile("//[^\n]*");
        private static final Pattern IDENTIFIER = Pattern.compile("[A-Za-z_][A-Za-z0-9_]*");

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
            List<String> values = new ArrayList<>();
            List<String> names = new ArrayList<>();
            String body = null;
            String bodyType = null;

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;
                names.add(parameter.paramName);

                if (parameter.isBodyParam) {
                    body = body(Shape.of(parameter), CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);
                    bodyType = parameter.dataType;
                    values.add("body");
                    continue;
                }

                if (!parameter.required && !parameter.isFile && !parameter.isBinary) {
                    values.add("null");
                    continue;
                }

                Object example = CallSnippets.plain(parameter.isFormParam
                        ? SpecExamples.formField(openAPI, operation, parameter.baseName)
                        : SpecExamples.parameter(openAPI, operation, parameter.baseName, SpecExamples.location(parameter)));

                values.add(value(Shape.of(parameter), example, parameter.baseName, where, 0));
            }

            String arguments = arguments(values, names);
            operation.vendorExtensions.put(ARGUMENTS, arguments);
            if (body != null) {
                operation.vendorExtensions.put(BODY, body);
                operation.vendorExtensions.put(BODY_TYPE, bodyType);
            }

            String bearer = bearer(operation);
            if (bearer != null) {
                operation.vendorExtensions.put(BEARER, bearer);
            }

            operation.vendorExtensions.put(IMPORTS,
                    imports(apiImport, bearer != null, arguments, body, bodyType, operation.returnType));
        }

        /**
         * The scheme the token goes to. The client has two bearer schemes, and its setBearerToken fills
         * only the first one it finds, so the snippet names the one the operation states.
         */
        private static String bearer(CodegenOperation operation) {
            if (operation.authMethods != null) {
                for (CodegenSecurity security : operation.authMethods) {
                    if (Boolean.TRUE.equals(security.isBasicBearer)) {
                        return security.name;
                    }
                }
            }

            return null;
        }

        /**
         * The imports: the client, the API class and the models, as the SDK's sample has them, then the
         * classes of the JDK the snippet names. What the snippet names is read from its code.
         */
        private String imports(String apiImport, boolean bearer, String... code) {
            Set<String> own = new TreeSet<>();
            own.add(invokerPackage + ".ApiClient");
            own.add(invokerPackage + ".Configuration");
            own.add(apiImport);
            if (bearer) {
                own.add(invokerPackage + ".auth.HttpBearerAuth");
            }

            Set<String> jdk = new TreeSet<>();
            for (String part : code) {
                if (part == null) {
                    continue;
                }

                String names = COMMENT.matcher(STRING_LITERAL.matcher(part).replaceAll("\"\"")).replaceAll("");
                Matcher identifier = IDENTIFIER.matcher(names);
                while (identifier.find()) {
                    String name = jdk(identifier.group());
                    if (name != null) {
                        jdk.add(name);
                    } else if (models.containsKey(identifier.group())) {
                        own.add(modelPackage + ".*");
                    }
                }
            }

            StringBuilder builder = new StringBuilder();
            for (String name : own) {
                builder.append("import ").append(name).append(";\n");
            }
            if (!jdk.isEmpty()) {
                builder.append('\n');
                for (String name : jdk) {
                    builder.append("import ").append(name).append(";\n");
                }
            }

            return builder.substring(0, builder.length() - 1);
        }

        /** The full name of a class of the JDK a snippet can name, by its simple name; null for any other. */
        private static String jdk(String name) {
            switch (name) {
                case "ArrayList":
                case "Arrays":
                case "HashMap":
                case "LinkedHashSet":
                case "List":
                case "Map":
                case "Set":
                case "UUID":
                    return "java.util." + name;
                case "LocalDate":
                case "OffsetDateTime":
                    return "java.time." + name;
                case "File":
                    return "java.io.File";
                case "BigDecimal":
                    return "java.math.BigDecimal";
                case "URI":
                    return "java.net.URI";
                default:
                    return null;
            }
        }

        /**
         * The request body. A model gets its required fields only, whatever the document states for
         * the body as a whole; an array or a scalar takes the example the document states for it.
         */
        private String body(Shape shape, Object example, String where) {
            CodegenModel model = shape.isArray ? null : model(shape);
            if (model == null || model.isEnum) {
                // A body parameter carries no item description, only the Java type.
                return shape.isArray && shape.items == null
                        ? typed(shape.dataType, example, "body", where, 0)
                        : value(shape, example, "body", where, 0);
            }

            String construction = "new " + model.classname + "()";
            List<String> setters = setters(model, where, 0);
            String line = construction + String.join("", setters);

            return setters.isEmpty() || line.length() <= LINE_LENGTH
                    ? line
                    : construction + CONTINUATION + String.join(CONTINUATION, setters);
        }

        private String construct(CodegenModel model, String where, int depth) {
            return "new " + model.classname + "()" + String.join("", setters(model, where, depth));
        }

        /**
         * The fluent setters of the model's required fields. A model composed with allOf is one class
         * with the fields of its parent among its own.
         */
        private List<String> setters(CodegenModel model, String where, int depth) {
            List<String> setters = new ArrayList<>();
            List<CodegenProperty> properties = model.readWriteVars != null ? model.readWriteVars : model.vars;

            if (CallSnippets.isComposed(model) && (properties == null || properties.isEmpty())) {
                // The SDK makes a oneOf or anyOf of plain values a class with no field to hold one of them.
                placeholders.add(where);
                return setters;
            }

            // A model can contain itself; past this depth it is built without its fields.
            if (depth >= MAX_DEPTH || properties == null) {
                return setters;
            }

            Set<String> seen = new HashSet<>();
            for (CodegenProperty property : properties) {
                if (!property.required || !seen.add(property.name)) {
                    continue;
                }

                Object example = CallSnippets.plain(SpecExamples.inheritedProperty(openAPI, model.name, property.baseName));
                setters.add("." + property.name + "(" + value(Shape.of(property, model.classname), example,
                        property.baseName, where + "." + property.baseName, depth + 1) + ")");
            }

            return setters;
        }

        /** A value of a type the generator names by its Java spelling only, as the body of an array. */
        private String typed(String type, Object example, String name, String where, int depth) {
            String itemType = type.startsWith("List<") || type.startsWith("Set<")
                    ? type.substring(type.indexOf('<') + 1, type.length() - 1)
                    : null;

            if (itemType != null) {
                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(typed(itemType, item, name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                }

                return collection(type, items);
            }

            if (type.startsWith("Map<")) {
                return "new HashMap<>()";
            }

            CodegenModel model = models.get(type);
            if (model == null) {
                return scalar(type, example, name, where);
            }

            if (model.isEnum) {
                Shape shape = new Shape();
                shape.complexType = type;
                return enumValue(shape, example, where);
            }

            return construct(model, where, depth);
        }

        private String value(Shape shape, Object example, String name, String where, int depth) {
            if (shape.isArray && shape.items != null && (shape.items.isFile || shape.items.isBinary)) {
                return "Arrays.asList(" + FILE + ")";
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
                        items.add(value(Shape.of(shape.items, shape.owner), item, name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                }

                return collection(shape.dataType, items);
            }

            // A free-form object is a map to Jackson, as is a map itself.
            if (shape.isMap || shape.isFreeFormObject) {
                return "new HashMap<>()";
            }

            CodegenModel model = model(shape);
            if (model != null) {
                return construct(model, where, depth);
            }

            return scalar(shape.dataType, example, name, where);
        }

        /** A list, or a set for an array of unique items, with the items given; empty when there are none. */
        private static String collection(String type, List<String> items) {
            boolean set = type != null && type.startsWith("Set<");
            if (items.isEmpty()) {
                return set ? "new LinkedHashSet<>()" : "new ArrayList<>()";
            }

            String list = "Arrays.asList(" + String.join(", ", items) + ")";
            return set ? "new LinkedHashSet<>(" + list + ")" : list;
        }

        /**
         * An enum member. An enum schema is an enum of the SDK, and so is an enum declared in place in a
         * model - a type nested in it; one declared in place in a parameter is only its underlying type,
         * so the member is its value.
         */
        private String enumValue(Shape shape, Object example, String where) {
            if (shape.isArray) {
                return null;
            }

            // A parameter typed by an enum schema names it in its type rather than as a model.
            CodegenModel model = model(shape);
            if (model == null && shape.dataType != null) {
                model = models.get(shape.dataType);
            }

            String type;
            Map<String, Object> allowableValues;
            if (model != null && model.isEnum) {
                type = model.classname;
                allowableValues = model.allowableValues;
            } else if (shape.isEnum) {
                String enumType = shape.datatypeWithEnum != null ? shape.datatypeWithEnum : shape.dataType;
                type = shape.owner == null ? null : shape.owner + "." + enumType;
                allowableValues = shape.allowableValues;
            } else {
                return null;
            }

            Object values = allowableValues == null ? null : allowableValues.get("enumVars");
            if (!(values instanceof List) || ((List<?>) values).isEmpty()) {
                return null;
            }

            Map<?, ?> chosen = member(allowableValues, example);
            if (chosen == null) {
                placeholders.add(where);
                chosen = (Map<?, ?>) ((List<?>) values).get(0);
            }

            return type == null ? String.valueOf(chosen.get("value")) : type + "." + chosen.get("name");
        }

        /**
         * The member of an enum the example names (see CallSnippets.member). The values of a numeric enum
         * come as the Java literals of their type, 1L or new BigDecimal("1"), so they are handed over as
         * the plain numbers the example states.
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
                Matcher number = NUMBER_LITERAL.matcher(String.valueOf(copy.get("value")));
                if (number.matches()) {
                    copy.put("value", number.group(1) != null ? number.group(1) : number.group(2));
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

            return null;
        }

        private CodegenModel model(Shape shape) {
            if (shape.complexType != null && models.containsKey(shape.complexType)) {
                return models.get(shape.complexType);
            }

            return shape.isModel ? models.get(shape.dataType) : null;
        }

        /** A value of a plain type, as the literal of the Java type it has in the SDK. */
        private String scalar(String type, Object example, String name, String where) {
            String text = example == null ? null : SpecExamples.text(example);

            if ("OffsetDateTime".equals(type)) {
                return dateTime(text, where);
            }
            if ("LocalDate".equals(type)) {
                return date(text, where);
            }
            if ("UUID".equals(type)) {
                return uuid(text, where);
            }

            if (text == null) {
                placeholders.add(where);
                return placeholder(type, name);
            }

            switch (type == null ? "" : type) {
                case "String":
                    return CallSnippets.quote(text);
                case "Integer":
                    return text;
                case "Long":
                    return text + "L";
                case "Float":
                    return text + "f";
                case "Double":
                    return text.matches("-?[0-9]+") ? text + ".0" : text;
                case "BigDecimal":
                    return "new BigDecimal(" + CallSnippets.quote(text) + ")";
                case "Boolean":
                    return text.toLowerCase(Locale.ROOT);
                case "URI":
                    return "URI.create(" + CallSnippets.quote(text) + ")";
                case "Object":
                    return literal(example);
                default:
                    return "null";
            }
        }

        private static String placeholder(String type, String name) {
            switch (type == null ? "" : type) {
                case "String":
                    return CallSnippets.quote(CallSnippets.placeholderName(name));
                case "Integer":
                    return "0";
                case "Long":
                    return "0L";
                case "Float":
                    return "0f";
                case "Double":
                    return "0.0";
                case "BigDecimal":
                    return "BigDecimal.ZERO";
                case "Boolean":
                    return "false";
                case "URI":
                    return "URI.create(" + CallSnippets.quote(CallSnippets.placeholderName(name)) + ")";
                default:
                    return "null";
            }
        }

        /** A date and time, parsed by the SDK's own type, OffsetDateTime, from the text the example states. */
        private String dateTime(String text, String where) {
            if (text != null) {
                try {
                    OffsetDateTime.parse(text);
                    return "OffsetDateTime.parse(" + CallSnippets.quote(text) + ")";
                } catch (DateTimeParseException notADateTime) {
                    try {
                        LocalDate.parse(text);
                        return "OffsetDateTime.parse(" + CallSnippets.quote(text + "T00:00:00Z") + ")";
                    } catch (DateTimeParseException notADate) {
                        // Falls through to the placeholder.
                    }
                }
            }

            placeholders.add(where);
            return "OffsetDateTime.now()";
        }

        private String date(String text, String where) {
            if (text != null) {
                String day = text.length() > 10 ? text.substring(0, 10) : text;
                try {
                    LocalDate.parse(day);
                    return "LocalDate.parse(" + CallSnippets.quote(day) + ")";
                } catch (DateTimeParseException notADate) {
                    // Falls through to the placeholder.
                }
            }

            placeholders.add(where);
            return "LocalDate.now()";
        }

        private String uuid(String text, String where) {
            // UUID.fromString refuses a text that is not one, so an example such as "1" counts as none.
            if (text == null || !UUID_TEXT.matcher(text).matches()) {
                placeholders.add(where);
                return NIL_UUID;
            }

            return "UUID.fromString(" + CallSnippets.quote(text) + ")";
        }

        /** An example of a value of any type, as the Java literal of what it holds when it is a plain one. */
        private static String literal(Object example) {
            if (example instanceof Boolean || example instanceof Number) {
                return SpecExamples.text(example);
            }
            if (example instanceof String) {
                return CallSnippets.quote((String) example);
            }

            return "null";
        }

        /**
         * The arguments on one line, or one per line, each named in a comment, once they get long or
         * any of them is a null that would say nothing on its own.
         */
        private static String arguments(List<String> values, List<String> names) {
            String line = String.join(", ", values);
            if (line.length() <= LINE_LENGTH && !values.contains("null")) {
                return line;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < values.size(); i++) {
                builder.append(CONTINUATION).append(values.get(i));
                if (i < values.size() - 1) {
                    builder.append(',');
                }
                builder.append(" // ").append(names.get(i));
            }

            return builder.append(STATEMENT).toString();
        }

        /** What the snippet needs to know about the type of a parameter or a property. */
        private static final class Shape {
            String dataType;
            String datatypeWithEnum;
            String complexType;
            String owner;
            boolean isEnum;
            boolean isArray;
            boolean isMap;
            boolean isFreeFormObject;
            boolean isModel;
            boolean isFile;
            Map<String, Object> allowableValues;
            CodegenProperty items;

            static Shape of(CodegenParameter parameter) {
                Shape shape = new Shape();
                shape.dataType = parameter.dataType;
                shape.datatypeWithEnum = parameter.datatypeWithEnum;
                shape.complexType = parameter.baseType;
                shape.isEnum = parameter.isEnum;
                shape.isArray = parameter.isArray;
                shape.isMap = parameter.isMap;
                shape.isFreeFormObject = parameter.isFreeFormObject;
                shape.isModel = parameter.isModel;
                shape.isFile = parameter.isFile || parameter.isBinary;
                shape.allowableValues = parameter.allowableValues;
                shape.items = parameter.items;
                return shape;
            }

            static Shape of(CodegenProperty property, String owner) {
                Shape shape = new Shape();
                shape.dataType = property.dataType;
                shape.datatypeWithEnum = property.datatypeWithEnum;
                shape.complexType = property.complexType;
                shape.owner = owner;
                shape.isEnum = property.isEnum;
                shape.isArray = property.isArray;
                shape.isMap = property.isMap;
                shape.isFreeFormObject = property.isFreeFormObject;
                shape.isModel = property.isModel;
                shape.isFile = property.isFile || property.isBinary;
                shape.allowableValues = property.allowableValues;
                shape.items = property.items;
                return shape;
            }
        }
    }
}