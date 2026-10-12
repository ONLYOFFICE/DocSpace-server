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

import org.openapitools.codegen.languages.KotlinClientCodegen;
import org.openapitools.codegen.model.*;
import static org.openapitools.codegen.utils.StringUtils.camelize;
import org.openapitools.codegen.*;

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.*;
import io.swagger.v3.oas.models.headers.*;

import java.util.*;
import java.io.File;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.Map.Entry;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.stream.Collectors;

public class MyKotlinClientCodegen extends KotlinClientCodegen {
    protected String apiNamePrefix = "", apiNameSuffix = "Api";

    /**
     * The call snippets of the reference pages instead of the SDK: one file, with a snippet per
     * operation, written into the folder the run was given with -o.
     */
    private boolean snippetsOnly;

    private Snippets snippets;

    public MyKotlinClientCodegen() {
        super();
        this.templateDir = "templates/kotlin";
        this.embeddedTemplateDir = "kotlin-client";
    }

    @Override
    public void processOpts() {
        super.processOpts();

        // Returns before the output folder is pointed at the SDK checkout (see CallSnippets.configure).
        snippetsOnly = CallSnippets.configure(this);
        if (snippetsOnly) {
            return;
        }

        this.outputFolder = "../../../../../sdk/docspace-api-sdk-kotlin";

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

        supportingFiles.add(new SupportingFile("sample.mustache", "samples", "sample.kt"));
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
            snippets = new Snippets(openAPI, allModels, packageName + ".infrastructure", modelPackage);
        }

        // The interface of every section lives in a package of its own, as the SDK's sample imports it.
        OperationMap operations = objs.getOperations();
        String apiClass = String.valueOf(operations.get("x-classname"));
        String apiImport = apiPackage + "." + operations.get("x-folder") + "." + apiClass;

        // The Retrofit interface writes its paths without the leading slash, so the generator has taken it
        // off; the document, where the examples are looked up by path, keeps it.
        CallSnippets.attach(objs, operation -> {
            String path = operation.path;
            if (path != null && !path.startsWith("/")) {
                operation.path = "/" + path;
            }
            try {
                snippets.attach(operation, apiImport, apiClass);
            } finally {
                operation.path = path;
            }
        });
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
            operationMap.put("x-folder", tagParts.folderPart);
            operationMap.put("x-classname", tagParts.classPart + apiNameSuffix);
            boolean supportUseAt = false;
            boolean shouldSupportFields = false;
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
                    // Retrofit forbids @Body on @DELETE/@GET/@HEAD shorthands; such operations
                    // must use @HTTP(method = ..., hasBody = true) instead.
                    String httpMethod = op.httpMethod == null ? "" : op.httpMethod.toUpperCase(Locale.ROOT);
                    boolean bodyDisallowingMethod = httpMethod.equals("DELETE")
                        || httpMethod.equals("GET")
                        || httpMethod.equals("HEAD");
                    if (bodyDisallowingMethod && op.bodyParam != null) {
                        op.vendorExtensions.put("x-retrofit-http-with-body", true);
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
    public void postProcessParameter(CodegenParameter parameter) {
        super.postProcessParameter(parameter);
        if (Boolean.TRUE.equals(parameter.isArray)
                && Boolean.TRUE.equals(parameter.isQueryParam)
                && !Boolean.TRUE.equals(parameter.isCollectionFormatMulti)) {
            String itemType = (parameter.items != null) ? parameter.items.dataType : "Any";
            parameter.vendorExtensions.put("x-csv-collection", true);
            parameter.vendorExtensions.put("x-csv-item-type", itemType);
        }
    }

    @Override
    public ModelsMap postProcessModels(ModelsMap objs) {
        super.postProcessModels(objs);

        for (ModelMap mo : objs.getModels()) {
            CodegenModel model = mo.getModel();

            for (CodegenProperty prop : model.vars) {
                normalizeEnumBackingType(prop);
                if ("version_Changed".equalsIgnoreCase(prop.baseName)) {
                    prop.name = "versionChangedField";
                    prop.baseName = "versionChangedField";
                    prop.getter = "getVersionChangedField";
                    prop.setter = "setVersionChangedField";
                    prop.nameInCamelCase = "versionChangedField";
                    prop.nameInPascalCase = "VersionChangedField";
                    prop.nameInSnakeCase = "VERSION_CHANGED_FIELD";
                }
            }
            model.readWriteVars = model.vars.stream().filter(v -> !v.isReadOnly).collect(Collectors.toList());

            model.allVars = new ArrayList<>(model.vars);
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

    /**
     * Kotlin's `Enum` already declares `name`, `ordinal` and — through the generated enum class —
     * `value`, so a specification value that maps onto one of those names does not compile.
     */
    private static final Set<String> RESERVED_ENUM_MEMBERS =
        new HashSet<>(Arrays.asList("name", "ordinal", "value", "entries", "values", "valueOf"));

    @Override
    public String toEnumVarName(String value, String datatype) {
        String varName = super.toEnumVarName(value, datatype);
        if (varName != null && RESERVED_ENUM_MEMBERS.contains(varName)) {
            return varName + "Field";
        }
        return varName;
    }

    /**
     * The enum template quotes the literal for `@Json(name = ...)` but passes it to the enum
     * constant as it stands, so a `number` enum backed by `java.math.BigDecimal` — whose literal
     * `toEnumValue` has already quoted — is handed a `kotlin.String` where the constructor expects a
     * number and fails to compile. Declaring the backing type as `kotlin.String` would compile but
     * change the wire format: `ValueEnumJsonAdapterFactory` derives `isNumeric` from the runtime
     * type of `value`, so the property would be written back as the JSON string `"1"` instead of the
     * number `1` the specification declares. The literals are unquoted and the backing type retyped
     * to `kotlin.Long` instead — the shape every `integer` enum already generates.
     *
     * A fractional value has no such shape (the adapter writes every number through `toLong()` and
     * would truncate it), so those keep the `kotlin.String` backing type.
     */
    private void normalizeEnumBackingType(CodegenProperty prop) {
        if (prop == null || !prop.isEnum) {
            return;
        }
        CodegenProperty backing = prop;
        while (backing.items != null) {
            backing = backing.items;
        }
        if (backing.dataType == null || !backing.dataType.startsWith("java.math.")) {
            return;
        }
        List<Map<String, Object>> enumVars = enumVarsOf(prop.allowableValues);
        boolean integral = !enumVars.isEmpty();
        for (Map<String, Object> enumVar : enumVars) {
            if (!isIntegralLiteral(unquote(String.valueOf(enumVar.get("value"))))) {
                integral = false;
            }
        }
        if (!integral) {
            backing.dataType = "kotlin.String";
        } else {
            unquoteEnumValues(prop.allowableValues);
            if (backing != prop) {
                unquoteEnumValues(backing.allowableValues);
            }
            backing.dataType = "kotlin.Long";
        }
        if (prop.mostInnerItems != null) {
            prop.mostInnerItems.dataType = backing.dataType;
        }
    }

    @SuppressWarnings("unchecked")
    private static List<Map<String, Object>> enumVarsOf(Map<String, Object> allowableValues) {
        Object enumVars = allowableValues == null ? null : allowableValues.get("enumVars");
        return enumVars instanceof List ? (List<Map<String, Object>>) enumVars : Collections.emptyList();
    }

    private static void unquoteEnumValues(Map<String, Object> allowableValues) {
        for (Map<String, Object> enumVar : enumVarsOf(allowableValues)) {
            enumVar.put("value", unquote(String.valueOf(enumVar.get("value"))));
        }
    }

    private static String unquote(String value) {
        if (value.length() > 1 && value.startsWith("\"") && value.endsWith("\"")) {
            return value.substring(1, value.length() - 1);
        }
        return value;
    }

    private static boolean isIntegralLiteral(String value) {
        try {
            Long.parseLong(value);
            return true;
        } catch (NumberFormatException e) {
            return false;
        }
    }

    /**
     * When the specification declares more than one HTTP bearer scheme, the bearer import, the
     * bearer constructor and `setBearerToken`/`setOAuthCredentials` are emitted once per scheme,
     * which does not compile. Only the first bearer scheme carries the marker the template guards
     * those members with; every scheme still takes part in the auth-name mapping.
     */
    @Override
    public List<CodegenSecurity> fromSecurity(Map<String, io.swagger.v3.oas.models.security.SecurityScheme> securitySchemeMap) {
        List<CodegenSecurity> securities = super.fromSecurity(securitySchemeMap);
        if (securities == null) {
            return securities;
        }
        boolean bearerSeen = false;
        for (CodegenSecurity security : securities) {
            if (!Boolean.TRUE.equals(security.isBasicBearer)) {
                continue;
            }
            security.vendorExtensions.put("x-first-basic-bearer", !bearerSeen);
            bearerSeen = true;
        }
        return securities;
    }

    @Override
    public String getName() {
        return "my-kotlin";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Kotlin client";
    } 

    private final Map<String, TagParts> tagMap = new HashMap<>();

    @Override
    public String sanitizeTag(String tag) {
        String sanitized = super.sanitizeTag(tag);
        if (!tagMap.containsKey(sanitized)) {
            String[] parts = tag.split(" / ");
            String folderPart = parts[0];
            String classPart = (parts.length > 1) ? parts[1] : parts[0];

            String sanitizedFolder = camelize(sanitizeName(folderPart));
            String sanitizedClass = camelize(sanitizeName(classPart));

            if (folderPart.startsWith("AI")) {
                sanitizedClass = sanitizedFolder + sanitizedClass;
            }

            TagParts info = new TagParts(
                tag,
                sanitizedFolder,
                sanitizedClass
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

    /**
     * Builds what the call snippet of one operation needs beyond the template, as Kotlin: the named
     * arguments of the call, the request body, the imports, and the bearer scheme the client is given.
     * <p>
     * The body is the minimal one, as for C# (see {@link MyCSharpClientCodegen}): a model gets its
     * required fields only, as named arguments of its constructor, with the values the document states
     * for them. Every optional parameter of a call and every optional field of a model has a default in
     * the SDK, so it is left out.
     */
    private static final class Snippets {

        static final String ARGUMENTS = "x-snippet-arguments";
        static final String BODY = "x-snippet-body";
        static final String IMPORTS = "x-snippet-imports";
        static final String BEARER = "x-snippet-bearer";

        /** Beyond this length the arguments are put one per line, and so are those of the body's constructor. */
        private static final int LINE_LENGTH = 60;

        private static final int MAX_DEPTH = 4;

        /** An argument on a line of its own, inside a statement of main, which stands four spaces in. */
        private static final String ARGUMENT = "\n        ";

        /** Where the parenthesis of arguments put one per line closes: back at the statement. */
        private static final String STATEMENT = "\n    ";

        private static final String COLLECTIONS = "kotlin.collections.";

        private static final String NIL_UUID = "UUID(0L, 0L)";

        /** A value of any type, or a free-form object, as the empty JSON object; Any gives the map no types. */
        private static final String EMPTY_OBJECT = "emptyMap<String, Any>()";

        private static final Pattern UUID_TEXT =
                Pattern.compile("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        /** The value of a numeric enum member as the generator writes it: 1, 1L, 1.5f, java.math.BigDecimal("1"). */
        private static final Pattern NUMBER_LITERAL =
                Pattern.compile("(?:java\\.math\\.)?BigDecimal\\(\"([^\"]*)\"\\)|(-?[0-9.]+(?:[eE][-+]?[0-9]+)?)[lLfFdD]?");

        private static final Pattern STRING_LITERAL = Pattern.compile("\"(?:\\\\.|[^\"\\\\])*\"");

        /** A type the snippet names on its own: RoomType in RoomType.FillingFormsRoom, but not the member. */
        private static final Pattern TYPE = Pattern.compile("(?<![.\\w])[A-Z][A-Za-z0-9_]*");

        private final OpenAPI openAPI;
        private final String infrastructurePackage;
        private final String modelPackage;
        private final Map<String, CodegenModel> models;

        /** Values the document states no example for, as "operationId: name", for the run's log. */
        private final List<String> placeholders = new ArrayList<>();

        Snippets(OpenAPI openAPI, List<ModelMap> allModels, String infrastructurePackage, String modelPackage) {
            this.openAPI = openAPI;
            this.infrastructurePackage = infrastructurePackage;
            this.modelPackage = modelPackage;
            this.models = CallSnippets.models(allModels);
        }

        List<String> placeholders() {
            return placeholders;
        }

        void attach(CodegenOperation operation, String apiImport, String apiClass) {
            List<String> arguments = new ArrayList<>();
            String body = null;
            String firstPart = firstPart(operation);

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;

                if (parameter.isBodyParam) {
                    body = body(operation, Shape.of(parameter), CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);
                    arguments.add(parameter.paramName + " = body");
                    continue;
                }

                boolean part = parameter.isFormParam && parameter.baseName.equals(firstPart);
                if (!parameter.required && !parameter.isFile && !parameter.isBinary && !part) {
                    continue;
                }

                Object example = CallSnippets.plain(parameter.isFormParam
                        ? SpecExamples.formField(openAPI, operation, parameter.baseName)
                        : SpecExamples.parameter(openAPI, operation, parameter.baseName, SpecExamples.location(parameter)));

                // An enum declared in place in a query or header parameter is a type nested in the API
                // interface; in any other parameter it is only its underlying type.
                Shape shape = Shape.of(parameter);
                if (parameter.isEnum && !parameter.isContainer && (parameter.isQueryParam || parameter.isHeaderParam)) {
                    shape.enumType = apiClass + "." + parameter.enumName + operation.operationIdCamelCase;
                }

                arguments.add(parameter.paramName + " = " + value(shape, example, parameter.baseName, where, 0));
            }

            String call = arguments(arguments);
            operation.vendorExtensions.put(ARGUMENTS, call);
            if (body != null) {
                operation.vendorExtensions.put(BODY, body);
            }

            String bearer = bearer(operation);
            if (bearer != null) {
                operation.vendorExtensions.put(BEARER, bearer);
            }

            operation.vendorExtensions.put(IMPORTS, imports(apiImport, call, body));
        }

        /**
         * The form field a multipart call is given although it is optional, or null. The SDK builds the
         * body of the parts it is given and cannot build one of none, so a form whose every field is
         * optional gets its first one.
         */
        private static String firstPart(CodegenOperation operation) {
            if (!operation.isMultipart || operation.formParams == null || operation.formParams.isEmpty()) {
                return null;
            }

            for (CodegenParameter parameter : operation.formParams) {
                if (parameter.required || parameter.isFile || parameter.isBinary) {
                    return null;
                }
            }

            return operation.formParams.get(0).baseName;
        }

        /**
         * The bearer scheme of the operation, which the client is created with; null when it states
         * none. Every authorization of the client goes with every request it makes, so an operation
         * that takes no token gets a client without one.
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
         * The imports: the API interface, the client and the models, as the SDK's sample has them, then
         * the coroutine runner and the classes of the JDK and OkHttp the snippet names. What the snippet
         * names is read from its code.
         */
        private String imports(String apiImport, String... code) {
            Set<String> own = new TreeSet<>();
            own.add(apiImport);
            own.add(infrastructurePackage + ".ApiClient");

            Set<String> other = new TreeSet<>();
            other.add("kotlinx.coroutines.runBlocking");

            for (String part : code) {
                if (part == null) {
                    continue;
                }

                String names = STRING_LITERAL.matcher(part).replaceAll("\"\"");
                if (names.contains(".asRequestBody(")) {
                    other.add("okhttp3.RequestBody.Companion.asRequestBody");
                }

                Matcher type = TYPE.matcher(names);
                while (type.find()) {
                    String name = type.group();
                    if (models.containsKey(name)) {
                        own.add(modelPackage + "." + name);
                    } else if (library(name) != null) {
                        other.add(library(name));
                    }
                }
            }

            StringBuilder builder = new StringBuilder();
            for (String name : own) {
                builder.append("import ").append(name).append('\n');
            }
            builder.append('\n');
            for (String name : other) {
                builder.append("import ").append(name).append('\n');
            }

            return builder.substring(0, builder.length() - 1);
        }

        /** The full name of a class of the JDK or OkHttp a snippet can name, by its simple name; null for any other. */
        private static String library(String name) {
            switch (name) {
                case "UUID":
                    return "java.util.UUID";
                case "LocalDate":
                case "OffsetDateTime":
                    return "java.time." + name;
                case "File":
                    return "java.io.File";
                case "BigDecimal":
                    return "java.math.BigDecimal";
                case "URI":
                    return "java.net.URI";
                case "MultipartBody":
                    return "okhttp3.MultipartBody";
                default:
                    return null;
            }
        }

        /**
         * The request body. A model gets its required fields only, whatever the document states for
         * the body as a whole; an array or a scalar takes the example the document states for it.
         */
        private String body(CodegenOperation operation, Shape shape, Object example, String where) {
            CodegenModel model = shape.isArray ? null : model(shape);

            // A body that is an enum schema reaches the SDK as a string: the value of a member, chosen
            // as for any other enum.
            CodegenModel enumBody = model == null && "kotlin.String".equals(shape.dataType)
                    ? CallSnippets.enumBody(openAPI, operation, models)
                    : null;
            if (enumBody != null) {
                Map<?, ?> member = member(enumBody.allowableValues, example, where);
                return quote(CallSnippets.unquote(String.valueOf(member.get("value"))));
            }

            // Nothing around `val body =` gives an empty collection its type, so the type is written out.
            if (model == null && shape.dataType != null && shape.dataType.startsWith(COLLECTIONS)) {
                return typed(shape.dataType, example, "body", where, 0);
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

            // A model can contain itself; past this depth it is not built. Its required fields have no
            // default to fall back on, so the place is left to the reader.
            if (fields == null) {
                placeholders.add(where);
                return "TODO()";
            }

            return model.classname + "(" + String.join(", ", fields) + ")";
        }

        /**
         * The model's required fields, as named arguments of its constructor; null past the depth a
         * model with required fields is built to. A model composed with allOf is one class with the
         * fields of its parent among its own.
         */
        private List<String> fields(CodegenModel model, String where, int depth) {
            List<String> fields = new ArrayList<>();

            if (model.vars == null || model.vars.isEmpty()) {
                if (CallSnippets.isComposed(model)) {
                    // The SDK makes a oneOf or anyOf a class with no field to hold one of its variants.
                    placeholders.add(where);
                }
                return fields;
            }

            Set<String> seen = new HashSet<>();
            for (CodegenProperty property : model.vars) {
                if (!property.required || !seen.add(property.name)) {
                    continue;
                }

                if (depth >= MAX_DEPTH) {
                    return null;
                }

                Object example = CallSnippets.plain(SpecExamples.inheritedProperty(openAPI, model.name, property.baseName));
                fields.add(property.name + " = " + value(Shape.of(property, model.classname), example,
                        property.baseName, where + "." + property.baseName, depth + 1));
            }

            return fields;
        }

        /**
         * A value of a type the generator names by its Kotlin spelling only, as the body of an array.
         * An empty collection is written with its type: nothing around it gives one.
         */
        private String typed(String type, Object example, String name, String where, int depth) {
            String plainType = type.endsWith("?") ? type.substring(0, type.length() - 1) : type;

            if (plainType.startsWith(COLLECTIONS + "List<") || plainType.startsWith(COLLECTIONS + "Set<")) {
                String itemType = typeArguments(plainType);
                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(typed(itemType, item, name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                }

                return collection(plainType, items, true);
            }

            if (plainType.startsWith(COLLECTIONS + "Map<")) {
                return "emptyMap<" + simpleType(typeArguments(plainType)) + ">()";
            }

            CodegenModel model = models.get(plainType);
            if (model == null) {
                return scalar(plainType, example, name, where);
            }

            if (model.isEnum) {
                Shape shape = new Shape();
                shape.complexType = plainType;
                return enumValue(shape, example, where);
            }

            return construct(model, where, depth);
        }

        private String value(Shape shape, Object example, String name, String where, int depth) {
            if (shape.isFileList) {
                return "listOf(" + part(name) + ")";
            }

            if (shape.isFile) {
                return part(name);
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

                return collection(shape.dataType, items, false);
            }

            // A free-form object is a map to Moshi, as is a map itself; typed as Any, it says nothing of
            // the map's types.
            if (shape.isMap || shape.isFreeFormObject) {
                return shape.dataType != null && shape.dataType.startsWith(COLLECTIONS + "Map<") ? "emptyMap()" : EMPTY_OBJECT;
            }

            CodegenModel model = model(shape);
            if (model != null) {
                return construct(model, where, depth);
            }

            return scalar(shape.dataType, example, name, where);
        }

        /**
         * A list, or a set for an array of unique items, with the items given; empty when there are none,
         * with its item type when nothing else gives it one.
         */
        private static String collection(String type, List<String> items, boolean typed) {
            boolean set = type != null && type.startsWith(COLLECTIONS + "Set<");
            if (items.isEmpty()) {
                return (set ? "emptySet" : "emptyList") + (typed ? "<" + simpleType(typeArguments(type)) + ">" : "") + "()";
            }

            return (set ? "setOf(" : "listOf(") + String.join(", ", items) + ")";
        }

        /** A multipart part with a file, under the name of the form field it fills. */
        private static String part(String name) {
            return "MultipartBody.Part.createFormData(" + quote(name) + ", \"file.docx\", File(\"file.docx\").asRequestBody())";
        }

        /**
         * An enum member. An enum schema is an enum of the SDK, and so is an enum declared in place in a
         * model - a type nested in it - or in a query or header parameter, nested in the API interface;
         * in any other place it is only its underlying type, so the member is its value.
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
            Map<?, ?> chosen = member(allowableValues, example);
            if (chosen == null) {
                placeholders.add(where);
                chosen = (Map<?, ?>) ((List<?>) allowableValues.get("enumVars")).get(0);
            }

            return chosen;
        }

        /**
         * The member of an enum the example names (see CallSnippets.member). The values of a numeric enum
         * come as the Kotlin literals of their type, 1L or java.math.BigDecimal("1"), so they are handed
         * over as the plain numbers the example states.
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

        /** A value of a plain type, as the literal of the Kotlin type it has in the SDK. */
        private String scalar(String type, Object example, String name, String where) {
            String simple = type == null ? "" : simpleType(type).replace("?", "");
            String text = example == null ? null : SpecExamples.text(example);

            switch (simple) {
                case "OffsetDateTime":
                    return dateTime(text, where);
                case "LocalDate":
                    return date(text, where);
                case "UUID":
                    return uuid(text, where);
                case "Any":
                    if (example instanceof Boolean || example instanceof Number || example instanceof String) {
                        return literal(example);
                    }
                    if (example == null) {
                        placeholders.add(where);
                    }
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
                    return quote(text);
                case "Int":
                    return text;
                case "Long":
                    return text + "L";
                case "Float":
                    return text + "f";
                case "Double":
                    return text.matches("-?[0-9]+") ? text + ".0" : text;
                case "BigDecimal":
                    return "BigDecimal(" + quote(text) + ")";
                case "Boolean":
                    return text.toLowerCase(Locale.ROOT);
                case "URI":
                    return "URI.create(" + quote(text) + ")";
                default:
                    return "null";
            }
        }

        private static String placeholder(String type, String name) {
            switch (type) {
                case "String":
                    return quote(CallSnippets.placeholderName(name));
                case "Int":
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
                    return "URI.create(" + quote(CallSnippets.placeholderName(name)) + ")";
                default:
                    return "null";
            }
        }

        /** A date and time, parsed by the SDK's own type, OffsetDateTime, from the text the example states. */
        private String dateTime(String text, String where) {
            if (text != null) {
                try {
                    OffsetDateTime.parse(text);
                    return "OffsetDateTime.parse(" + quote(text) + ")";
                } catch (DateTimeParseException notADateTime) {
                    try {
                        LocalDate.parse(text);
                        return "OffsetDateTime.parse(" + quote(text + "T00:00:00Z") + ")";
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
                    return "LocalDate.parse(" + quote(day) + ")";
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

            return "UUID.fromString(" + quote(text) + ")";
        }

        /** An example of a value of any type that is a plain one, as the Kotlin literal of what it holds. */
        private static String literal(Object example) {
            return example instanceof String ? quote((String) example) : SpecExamples.text(example);
        }

        /** The arguments on one line, or one per line once they get long. */
        private static String arguments(List<String> arguments) {
            String line = String.join(", ", arguments);
            if (line.length() <= LINE_LENGTH) {
                return line;
            }

            return ARGUMENT + String.join("," + ARGUMENT, arguments) + STATEMENT;
        }

        /** The type arguments of a generic type, as they are written between its angle brackets. */
        private static String typeArguments(String type) {
            return type.substring(type.indexOf('<') + 1, type.lastIndexOf('>'));
        }

        /** A type as a snippet writes it: without the packages of the standard types, which it imports. */
        private static String simpleType(String type) {
            return type.replace(COLLECTIONS, "")
                    .replace("kotlin.", "")
                    .replace("java.util.", "")
                    .replace("java.time.", "")
                    .replace("java.math.", "")
                    .replace("java.net.", "");
        }

        /**
         * A string literal in double quotes. Besides what C# and Go escape, Kotlin reads a dollar sign as
         * the start of a template.
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
            String enumType;
            boolean isEnum;
            boolean isArray;
            boolean isMap;
            boolean isFreeFormObject;
            boolean isModel;
            boolean isFile;
            boolean isFileList;
            Integer minItems;
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
                shape.isFile = parameter.isFile || parameter.isBinary;
                // A list of files is one parameter, a list of parts, as the SDK declares it.
                shape.isFileList = (shape.isFile && parameter.isCollectionFormatMulti)
                        || (parameter.isArray && parameter.items != null && (parameter.items.isFile || parameter.items.isBinary));
                shape.minItems = parameter.minItems;
                shape.allowableValues = parameter.allowableValues;
                shape.items = parameter.items;
                return shape;
            }

            /** The shape of a property; an enum declared in place in it is a type nested in its model. */
            static Shape of(CodegenProperty property, String owner) {
                Shape shape = new Shape();
                shape.dataType = property.dataType;
                shape.complexType = property.complexType;
                shape.enumType = owner == null ? null : owner + "." + property.nameInPascalCase;
                shape.isEnum = property.isEnum;
                shape.isArray = property.isArray;
                shape.isMap = property.isMap;
                shape.isFreeFormObject = property.isFreeFormObject;
                shape.isModel = property.isModel;
                shape.isFile = property.isFile || property.isBinary;
                shape.isFileList = property.isArray && property.items != null && (property.items.isFile || property.items.isBinary);
                shape.minItems = property.minItems;
                shape.allowableValues = property.allowableValues;
                shape.items = property.items;
                return shape;
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