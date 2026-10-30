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

import java.io.File;
import java.util.*;
import java.util.stream.Collectors;
import java.util.Map.Entry;

import org.openapitools.codegen.model.*;
import org.openapitools.codegen.languages.TypeScriptAxiosClientCodegen;
import static org.openapitools.codegen.utils.StringUtils.camelize;
import org.openapitools.codegen.*;
import org.openapitools.codegen.utils.ModelUtils;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.*;
import io.swagger.v3.oas.models.headers.*;
import io.swagger.v3.oas.models.media.Schema;

import io.swagger.v3.oas.models.OpenAPI;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class MyTypeScriptAxiosClientCodegen extends TypeScriptAxiosClientCodegen {

    protected String apiDocPath = "docs/";
    protected String modelDocPath = "docs/";
    public static final String BASE_URL = "baseURL";

    /**
     * The call snippets of the reference pages instead of the SDK: one file, with a snippet per
     * operation, written into the folder the run was given with -o.
     */
    private boolean snippetsOnly;

    private Snippets snippets;

    public MyTypeScriptAxiosClientCodegen() {
        super();
        this.templateDir = "templates/typescript-axios";
        this.embeddedTemplateDir = "typescript-axios";

        additionalProperties.put("apiDocPath", apiDocPath);
        additionalProperties.put("modelDocPath", modelDocPath);
        modelDocTemplateFiles.put("model_doc.mustache", ".md");
        apiDocTemplateFiles.put("api_doc.mustache", ".md");

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
    
    @Override
    public void processOpts() {
        super.processOpts();

        // Returns before the output folder is pointed at the SDK checkout (see CallSnippets.configure).
        snippetsOnly = CallSnippets.configure(this);
        if (snippetsOnly) {
            return;
        }

        Boolean example = (Boolean) additionalProperties.get("example");

        if(Boolean.TRUE.equals(example))
        {
            supportingFiles.add(new SupportingFile(
                "example.mustache", "samples", "example.ts"
            ));
        }

        this.outputFolder = "../../../../../sdk/docspace-api-sdk-typescript";

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

        if (additionalProperties.containsKey(NPM_REPOSITORY)) {
            this.setNpmRepository(additionalProperties.get(NPM_REPOSITORY).toString());
        }

        supportingFiles.add(new SupportingFile("README.mustache", "", "README.md"));
        supportingFiles.add(new SupportingFile("package.mustache", "", "package.json"));
        supportingFiles.add(new SupportingFile("tsconfig.mustache", "", "tsconfig.json"));
        if (supportsES6) {
            supportingFiles.add(new SupportingFile("tsconfig.esm.mustache", "", "tsconfig.esm.json"));
        }
    }

    @Override
    public ModelsMap postProcessModels(ModelsMap objs) {
        addOneOfImports(objs);

        super.postProcessModels(objs);

        for (ModelMap mo : objs.getModels()) {
            CodegenModel model = mo.getModel();

            if (model.getComposedSchemas() != null && model.getComposedSchemas().getAllOf() != null) {
                model.getVendorExtensions().put("x-uses-allOf", true);
                Set<String> localPropertyNames = new HashSet<>();
                Schema<?> modelSchema = this.openAPI.getComponents().getSchemas().get(model.schemaName);

                if (ModelUtils.isAllOf(modelSchema)) {
                    for (Object obj : modelSchema.getAllOf()) {
                        if (obj instanceof Schema) {
                            Schema<?> allOfSchema = (Schema<?>) obj;
                            if ("object".equals(ModelUtils.getType(allOfSchema)) && allOfSchema.getProperties() != null) {
                                localPropertyNames.addAll(allOfSchema.getProperties().keySet());
                            }
                        }
                    }
                }

                List<CodegenProperty> localVars = new ArrayList<>();
                for (CodegenProperty var : model.vars) {
                    if (localPropertyNames.contains(var.baseName)) {
                        localVars.add(var);
                    }
                }

                model.getVendorExtensions().put("x-localVars", localVars);
            }
        }
        
        return objs;
    }

    /**
     * The stock typescript-axios model template prints the import block outside the model context,
     * where the hasOneOf flag set by the parent generator is not visible. A model built from a plain
     * oneOf of $refs therefore renders the union type without importing its members, and the members
     * only reach the import list when some property happens to use them. Registering them as regular
     * model imports here makes the template emit them: the parent postProcessModels fills in the
     * class, filename and tsImport keys for every entry of the import list.
     */
    private void addOneOfImports(ModelsMap objs) {
        for (ModelMap mo : objs.getModels()) {
            CodegenModel model = mo.getModel();

            if (model.oneOf == null || model.oneOf.isEmpty()) {
                continue;
            }

            for (String oneOfType : model.oneOf) {
                if (languageSpecificPrimitives.contains(oneOfType)
                        || oneOfType.equals(model.classname)
                        || !oneOfType.matches("[A-Za-z_][A-Za-z0-9_]*")) {
                    continue;
                }

                String modelImport = toModelImport(oneOfType);
                boolean alreadyImported = objs.getImports().stream()
                        .anyMatch(im -> modelImport.equals(im.get("import")));

                if (!alreadyImported) {
                    Map<String, String> item = new HashMap<>();
                    item.put("import", modelImport);
                    objs.getImports().add(item);
                }
            }
        }
    }

    // The third-party twin of a generic action (see ThirdPartyVariants): the string-id shape the document
    // carries as `x-thirdparty-variant`. TypeScript has no overloading by parameter type, but it has
    // overload signatures, so the twin stays attached to its operation and the template renders one
    // method: the request parameters that differ are typed `number | string`, and where the answer
    // differs too the method gets three signatures - one per id type, and the general one.
    @Override
    public CodegenOperation fromOperation(String path, String httpMethod, Operation operation, List<Server> servers) {
        CodegenOperation op = super.fromOperation(path, httpMethod, operation, servers);
        ThirdPartyVariants.attach(this, op, path, httpMethod, operation, servers, ThirdPartyVariants.Naming.OVERLOAD);
        return op;
    }

    /** On a parameter whose type differs in the third-party twin: the union of both types. */
    private static final String UNION_TYPE = "x-thirdparty-union-type";

    /** On an operation whose answer differs in the twin: render overload signatures. */
    private static final String OVERLOAD = "x-thirdparty-overload";

    /** The object type that narrows the request to the portal (int) shape: `{ fileId: number }`. */
    private static final String NARROW = "x-thirdparty-narrow";

    /** The object type that narrows the request to the third-party (string) shape. */
    private static final String NARROW_VARIANT = "x-thirdparty-narrow-variant";

    /** The twin's return type, and the union of both. */
    private static final String RETURN_TYPE_VARIANT = "x-thirdparty-return-type-variant";
    private static final String RETURN_TYPE_UNION = "x-thirdparty-return-type-union";

    private static void markOverloads(OperationsMap objs) {
        if (objs == null || objs.getOperations() == null || objs.getOperations().getOperation() == null) {
            return;
        }

        for (CodegenOperation op : objs.getOperations().getOperation()) {
            Object attached = op.vendorExtensions.get(ThirdPartyVariants.VARIANT_OPERATION);
            if (!(attached instanceof CodegenOperation)) {
                continue;
            }

            CodegenOperation variant = (CodegenOperation) attached;
            ThirdPartyVariants.alignBodyName(op, variant);

            Map<String, CodegenParameter> variantParams = new HashMap<>();
            CodegenParameter variantBody = null;
            for (CodegenParameter parameter : variant.allParams) {
                variantParams.put(parameter.paramName, parameter);
                if (parameter.isBodyParam) {
                    // The entry of allParams, not variant.bodyParam: the generator keeps those as separate
                    // copies, and the "changed" mark sits on the entry.
                    variantBody = parameter;
                }
            }

            List<String> narrow = new ArrayList<>();
            List<String> narrowVariant = new ArrayList<>();

            for (CodegenParameter parameter : op.allParams) {
                // The body parameter is named after its schema, so the twin's differs (saveAsPdf vs
                // thirdPartySaveAsPdf); there is one body per operation, so it is matched by role.
                CodegenParameter twin = parameter.isBodyParam ? variantBody : variantParams.get(parameter.paramName);
                if (twin == null || !twin.vendorExtensions.containsKey(ThirdPartyVariants.CHANGED)
                        || Objects.equals(parameter.dataType, twin.dataType)) {
                    continue;
                }

                parameter.vendorExtensions.put(UNION_TYPE, parameter.dataType + " | " + twin.dataType);
                // Optional stays optional, or the narrowing signature would never match a call without it.
                String name = parameter.paramName + (parameter.required ? "" : "?");
                narrow.add(name + ": " + parameter.dataType);
                narrowVariant.add(name + ": " + twin.dataType);
            }

            boolean returnChanged = variant.vendorExtensions.containsKey(ThirdPartyVariants.RETURN_CHANGED)
                && !Objects.equals(op.returnType, variant.returnType);

            if (!narrow.isEmpty() && returnChanged) {
                op.vendorExtensions.put(OVERLOAD, true);
                op.vendorExtensions.put(NARROW, "{ " + String.join("; ", narrow) + " }");
                op.vendorExtensions.put(NARROW_VARIANT, "{ " + String.join("; ", narrowVariant) + " }");
                op.vendorExtensions.put(RETURN_TYPE_VARIANT, variant.returnType == null ? "void" : variant.returnType);
                op.vendorExtensions.put(RETURN_TYPE_UNION,
                    (op.returnType == null ? "void" : op.returnType) + " | " + (variant.returnType == null ? "void" : variant.returnType));
            }
        }
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
            snippets = new Snippets(openAPI, allModels, String.valueOf(additionalProperties.get("packageName")));
        }

        String apiClass = String.valueOf(objs.getOperations().get("x-classname"));
        CallSnippets.attach(this, objs, operation -> snippets.attach(operation, apiClass));
    }

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        ThirdPartyVariants.addAttachedImports(this, objs);
        markOverloads(objs);

        objs = super.postProcessOperationsWithModels(objs, allModels);
        OperationMap operationMap = objs.getOperations();
        List<CodegenOperation> operations = operationMap.getOperation();
        String className = operationMap.getClassname();
        if (className != null && className.endsWith(apiNameSuffix)) {
            className = className.substring(0, className.length() - 3);
        }
        TagParts tagParts = tagMap.get(className);
        operationMap.put("x-folder", (tagParts.folderPart).replaceAll("([a-z0-9])([A-Z])", "$1-$2").toLowerCase(Locale.ROOT));
        operationMap.put("x-file", (tagParts.classPart + apiNameSuffix).replaceAll("([a-z0-9])([A-Z])", "$1-$2").toLowerCase(Locale.ROOT));
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
        if (operations != null) {
            for (CodegenOperation op : operations) {
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

        return objs;
    }

    @Override
    public void postProcessParameter(CodegenParameter parameter) {
        super.postProcessParameter(parameter);

        if ("deepObject".equals(parameter.style) && Boolean.TRUE.equals(parameter.isExplode)) {
            parameter.isCollectionFormatMulti = true;
        }
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

        return objs;
    }

    private String toDashCase(String input) {
        return input.replaceAll("([a-z0-9])([A-Z])", "$1-$2")
                    .toLowerCase();
    }

    @Override
    public String apiDocFileFolder() {
        return (outputFolder + "/" + apiDocPath).replace('/', File.separatorChar);
    }

    @Override
    public String modelDocFileFolder() {
        return (outputFolder + "/" + modelDocPath).replace('/', File.separatorChar);
    }

    @Override
    public String getName() {
        return "my-typescript-axios";
    }

    @Override
    public String getHelp() {
        return "Generates a TypeScript client library using axios.";
    }
    
    @Override
    public String apiFilename(String templateName, String tag) {

        String uniqueTag = uniqueCaseInsensitiveString(tag, seenApiFilenames);
        String suffix = apiTemplateFiles().get(templateName);

        TagParts tagParts = tagMap.get(camelize(uniqueTag));
        if (tagParts == null) {
            return apiFileFolder() + File.separator + toApiFilename(uniqueTag) + suffix;
        }

        String folderPath = apiFileFolder() + File.separator + tagParts.folderPart.replaceAll("([a-z0-9])([A-Z])", "$1-$2").toLowerCase(Locale.ROOT);
        String filename = toApiFilename(tagParts.classPart) + suffix;

        return folderPath + File.separator + filename;
    }

    private final Map<String, TagParts> tagMap = new HashMap<>();

    @Override
    public String sanitizeTag(String tag) {
        String sanitized = super.sanitizeTag(tag);
        if (!tagMap.containsKey(sanitized)) {
            String[] parts = tag.split(" / ");
            String folderPart = parts[0];
            String classPart = (parts.length > 1) ? parts[1] : parts[0];

            String folderPartSanitized = camelize(sanitizeName(folderPart));
            final String classPartSanitized = camelize(sanitizeName(classPart));

            boolean duplicate = tagMap.values().stream()
                .anyMatch(tp -> tp.classPart.equals(classPartSanitized));

            String finalClassPartSanitized = duplicate
                ? folderPartSanitized + classPartSanitized
                : classPartSanitized;

            TagParts info = new TagParts(
                tag,
                folderPartSanitized,
                finalClassPartSanitized
            );

            tagMap.put(sanitized, info);
        }
        return sanitized;
    }

    private final Map<String, String> seenApiFilenames = new HashMap<String, String>();

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

    /**
     * Builds what the call snippet of one operation needs beyond the template, as TypeScript: the
     * imports, the request parameters of the call, the request body and the type it is declared with,
     * and whether the configuration carries the token.
     * <p>
     * The body is the minimal one, as for C# (see {@link MyCSharpClientCodegen}): a model is an object
     * literal with its required fields only, with the values the document states for them. Every method
     * takes its parameters as one object, and an optional parameter or field is left out of it.
     */
    private static final class Snippets {

        static final String IMPORTS = "x-snippet-imports";
        static final String ARGUMENTS = "x-snippet-arguments";
        static final String BODY = "x-snippet-body";
        static final String BODY_TYPE = "x-snippet-body-type";
        static final String BEARER = "x-snippet-bearer";

        /** Beyond this length a statement is put a field per line: about what a code block of the page shows. */
        private static final int STATEMENT_LENGTH = 90;

        /** Beyond this length the names an import brings are put one per line. */
        private static final int IMPORT_LENGTH = 80;

        private static final int MAX_DEPTH = 4;

        /** The statements stand in the body of main; a field put on a line of its own, one level deeper. */
        private static final String STATEMENT = "    ";
        private static final String FIELD = "\n        ";
        private static final String CLOSE = "\n    }";

        private static final String NIL_UUID = "\"00000000-0000-0000-0000-000000000000\"";

        private static final Pattern UUID_TEXT =
                Pattern.compile("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        private static final Pattern IDENTIFIER = Pattern.compile("[A-Za-z_$][A-Za-z0-9_$]*");

        private final OpenAPI openAPI;
        private final Map<String, CodegenModel> models;
        private final String packageName;

        /** Values the document states no example for, as "operationId: name", for the run's log. */
        private final List<String> placeholders = new ArrayList<>();

        /** The names the snippet being built imports from the package. */
        private Set<String> names;

        /** Whether the snippet being built reads a file. */
        private boolean readsFile;

        Snippets(OpenAPI openAPI, List<ModelMap> allModels, String packageName) {
            this.openAPI = openAPI;
            this.models = CallSnippets.models(allModels);
            this.packageName = packageName;
        }

        List<String> placeholders() {
            return placeholders;
        }

        void attach(CodegenOperation operation, String apiClass) {
            names = new TreeSet<>();
            readsFile = false;

            List<String> arguments = new ArrayList<>();
            String body = null;
            String bodyType = null;

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;

                if (parameter.isBodyParam) {
                    Shape shape = Shape.of(parameter);
                    body = body(operation, shape, CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);
                    bodyType = shape.dataType;
                    useTypes(bodyType);
                    arguments.add(parameter.paramName + ": body");
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

            if (body != null) {
                operation.vendorExtensions.put(BODY, layout(body, bodyType));
                operation.vendorExtensions.put(BODY_TYPE, bodyType);
            }

            // The call takes its parameters as one object, and none at all when every one is optional.
            String statement = (operation.returnType != null ? "const response = " : "") + "await api."
                    + operation.nickname + "();";
            operation.vendorExtensions.put(ARGUMENTS, object(arguments, statement.length()));

            if (bearer(operation)) {
                operation.vendorExtensions.put(BEARER, true);
            }

            names.add("Configuration");
            names.add(apiClass);
            operation.vendorExtensions.put(IMPORTS, imports());
        }

        /**
         * The import lines: the names of the package the snippet uses, and the reading of a file from
         * Node's file system where it uploads one.
         */
        private String imports() {
            String from = " from " + CallSnippets.quote(packageName) + ";";
            String line = "import { " + String.join(", ", names) + " }" + from;
            if (line.length() > IMPORT_LENGTH) {
                line = "import {\n    " + String.join(",\n    ", names) + ",\n}" + from;
            }

            return readsFile ? line + "\nimport { readFileSync } from \"node:fs\";" : line;
        }

        /**
         * Whether the operation states a scheme the SDK sends the access token for: a bearer scheme or
         * OAuth 2.0. An operation that states neither does not send it, so its configuration has none.
         */
        private static boolean bearer(CodegenOperation operation) {
            if (operation.authMethods != null) {
                for (CodegenSecurity security : operation.authMethods) {
                    if (Boolean.TRUE.equals(security.isBasicBearer) || Boolean.TRUE.equals(security.isOAuth)) {
                        return true;
                    }
                }
            }

            return false;
        }

        /** Notes the models and enums a type names, for the import. */
        private void useTypes(String type) {
            if (type == null) {
                return;
            }

            Matcher matcher = IDENTIFIER.matcher(type);
            while (matcher.find()) {
                if (models.containsKey(matcher.group())) {
                    names.add(matcher.group());
                }
            }
        }

        /**
         * The fields of an object literal on one line, or a field per line once the statement it stands
         * in gets long.
         */
        private static String object(List<String> fields, int around) {
            if (fields.isEmpty()) {
                return "";
            }

            String line = "{ " + String.join(", ", fields) + " }";
            if (STATEMENT.length() + around + line.length() <= STATEMENT_LENGTH) {
                return line;
            }

            return "{" + FIELD + String.join("," + FIELD, fields) + "," + CLOSE;
        }

        /** The body as `const body: <type> = <value>;` puts it: a model a field per line once it gets long. */
        private static String layout(String value, String type) {
            String statement = "const body: " + type + " = ;";
            if (!value.startsWith("{ ") || !value.endsWith(" }")
                    || STATEMENT.length() + statement.length() + value.length() <= STATEMENT_LENGTH) {
                return value;
            }

            List<String> fields = split(value.substring(2, value.length() - 2));
            return "{" + FIELD + String.join("," + FIELD, fields) + "," + CLOSE;
        }

        /** The fields of an object literal, split at the commas that stand outside its values. */
        private static List<String> split(String fields) {
            List<String> parts = new ArrayList<>();
            int depth = 0;
            boolean inString = false;
            int start = 0;
            for (int i = 0; i < fields.length(); i++) {
                char c = fields.charAt(i);
                if (inString) {
                    if (c == '\\') {
                        i++;
                    } else if (c == '"') {
                        inString = false;
                    }
                } else if (c == '"') {
                    inString = true;
                } else if (c == '{' || c == '[' || c == '(') {
                    depth++;
                } else if (c == '}' || c == ']' || c == ')') {
                    depth--;
                } else if (c == ',' && depth == 0) {
                    parts.add(fields.substring(start, i).trim());
                    start = i + 1;
                }
            }
            parts.add(fields.substring(start).trim());
            return parts;
        }

        private CodegenModel bodyModel(Shape shape) {
            return shape.isArray ? null : model(shape);
        }

        /**
         * The request body. A model gets its required fields only, whatever the document states for
         * the body as a whole; an array or a scalar takes the example the document states for it.
         */
        private String body(CodegenOperation operation, Shape shape, Object example, String where) {
            CodegenModel model = bodyModel(shape);

            // A body that is an enum schema the generator may hand over as its underlying type: the
            // value of a member, chosen as for any other enum.
            if (model == null && "string".equals(plainType(shape.dataType))) {
                CodegenModel enumBody = CallSnippets.enumBody(openAPI, operation, models);
                if (enumBody != null) {
                    return enumMember(enumBody.classname, enumBody.allowableValues, example, where);
                }
            }

            if (model == null || model.isEnum || CallSnippets.isComposed(model)) {
                return value(shape, example, "body", where, 0);
            }

            return construct(model, where, 0);
        }

        /**
         * A model as an object literal with its required fields; past the depth a model with required
         * fields is built to, an empty one asserted to its type, and the place is left to the reader.
         */
        private String construct(CodegenModel model, String where, int depth) {
            List<String> fields = fields(model, where, depth);

            if (fields == null) {
                placeholders.add(where);
                names.add(model.classname);
                return "{} as " + model.classname;
            }

            return fields.isEmpty() ? "{}" : "{ " + String.join(", ", fields) + " }";
        }

        /**
         * The model's required fields, as `key: value`; null past the depth a model with required
         * fields is built to. A model composed with allOf has the fields of its parent among its own.
         */
        private List<String> fields(CodegenModel model, String where, int depth) {
            List<String> fields = new ArrayList<>();

            if (model.allVars == null) {
                return fields;
            }

            Set<String> seen = new HashSet<>();
            for (CodegenProperty property : model.allVars) {
                if (!property.required || !seen.add(property.baseName)) {
                    continue;
                }

                if (depth >= MAX_DEPTH) {
                    return null;
                }

                Object example = CallSnippets.plain(SpecExamples.inheritedProperty(openAPI, model.name, property.baseName));
                fields.add(key(property.baseName) + ": " + value(Shape.of(property, model.classname), example,
                        property.baseName, where + "." + property.baseName, depth + 1));
            }

            return fields;
        }

        /** A property name as the key of an object literal, quoted when it is not an identifier. */
        private static String key(String name) {
            return IDENTIFIER.matcher(name).matches() ? name : CallSnippets.quote(name);
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

                return list(shape.dataType, items);
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

            // A free-form object, or a value of any type: the example as it stands. Without one, an
            // empty object; for a value of any type that is a stand-in the log notes.
            if (shape.isFreeFormObject || shape.isAnyType) {
                if (example != null) {
                    return literal(example);
                }
                if (!shape.isFreeFormObject) {
                    placeholders.add(where);
                }
                return "{}";
            }

            return scalar(shape, example, name, where);
        }

        /** An array, or a set where the SDK types the list as one (the document says its items are unique). */
        private static String list(String type, List<String> items) {
            String array = "[" + String.join(", ", items) + "]";
            return plainType(type) != null && plainType(type).startsWith("Set<") ? "new Set(" + array + ")" : array;
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

            return entries.isEmpty() ? "{}" : "{ " + String.join(", ", entries) + " }";
        }

        /** A value of a dictionary. A body parameter carries no item description, only the TypeScript type. */
        private String entry(Shape shape, Object example, String name, String where, int depth) {
            if (shape.items != null) {
                return value(shape.item(), example, name, where, depth + 1);
            }

            return typed(valueType(plainType(shape.dataType)), example, name, where, depth + 1);
        }

        /**
         * A oneOf or anyOf: a union of its variants to the SDK, so it takes the value of the variant
         * {@link CallSnippets#variant} picks.
         */
        private String composed(CodegenModel model, Object example, String name, String where, int depth) {
            // A variant can be a oneOf of its own and lead back here, so it stops at the depth the models do.
            if (depth >= MAX_DEPTH) {
                placeholders.add(where);
                names.add(model.classname);
                return "{} as " + model.classname;
            }

            CallSnippets.Choice<String> choice = CallSnippets.variant(openAPI, model, example, this::fits);

            return typed(choice.variant, choice.example, name, where, depth + 1);
        }

        private boolean fits(String type, Object example) {
            if (example == null) {
                return false;
            }

            String plain = plainType(type);
            if (plain.startsWith("Array<") || plain.startsWith("Set<")) {
                return example instanceof List;
            }

            if (valueType(plain) != null) {
                return example instanceof Map;
            }

            switch (plain) {
                case "string":
                    return example instanceof String;
                case "number":
                    return example instanceof Number;
                case "boolean":
                    return example instanceof Boolean;
                case "object":
                    return example instanceof Map;
                case "any":
                    return true;
                default:
                    CodegenModel model = models.get(plain);
                    if (model == null) {
                        return false;
                    }

                    // An enum fits only an example that names one of its members; any other is left to
                    // a variant beside it, such as a string.
                    return model.isEnum ? member(model.allowableValues, example) != null : example instanceof Map;
            }
        }

        /** A value of a type the generator names by its TypeScript spelling only, as the variants of a oneOf. */
        private String typed(String type, Object example, String name, String where, int depth) {
            String plain = plainType(type);
            if (plain == null) {
                placeholders.add(where);
                return "null";
            }

            if ((plain.startsWith("Array<") || plain.startsWith("Set<")) && plain.endsWith(">")) {
                String itemType = plain.substring(plain.indexOf('<') + 1, plain.length() - 1);

                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(typed(itemType, CallSnippets.plain(item), name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                }

                return list(plain, items);
            }

            String valueType = valueType(plain);
            if (valueType != null) {
                List<String> entries = new ArrayList<>();
                if (example instanceof Map) {
                    for (Map.Entry<?, ?> entry : ((Map<?, ?>) example).entrySet()) {
                        entries.add(CallSnippets.quote(String.valueOf(entry.getKey())) + ": "
                                + typed(valueType, CallSnippets.plain(entry.getValue()), name, where, depth + 1));
                    }
                }
                return entries.isEmpty() ? "{}" : "{ " + String.join(", ", entries) + " }";
            }

            CodegenModel model = models.get(plain);
            if (model == null) {
                Shape shape = new Shape();
                shape.dataType = plain;
                shape.isAnyType = "any".equals(plain) || "object".equals(plain);
                return shape.isAnyType
                        ? value(shape, example, name, where, depth)
                        : scalar(shape, example, name, where);
            }

            if (model.isEnum) {
                return enumMember(model.classname, model.allowableValues, example, where);
            }

            return CallSnippets.isComposed(model) ? composed(model, example, name, where, depth) : construct(model, where, depth);
        }

        /** A file, read from Node's file system into the File the SDK takes. */
        private String file() {
            readsFile = true;
            return "new File([readFileSync(\"file.docx\")], \"file.docx\")";
        }

        /**
         * An enum member. An enum schema is a constant object of the SDK, and so is an enum declared in
         * place, in a model or in a parameter, which the SDK names after its owner.
         */
        private String enumValue(Shape shape, Object example, String where) {
            // An array or a dictionary of enum values is built a value at a time.
            if (shape.isArray || shape.isMap) {
                return null;
            }

            CodegenModel model = model(shape);
            if (model == null && shape.dataType != null) {
                model = models.get(plainType(shape.dataType));
            }

            if (model != null && model.isEnum) {
                return enumMember(model.classname, model.allowableValues, example, where);
            }

            if (shape.isEnum && hasMembers(shape.allowableValues)) {
                return enumMember(shape.enumType, shape.allowableValues, example, where);
            }

            return null;
        }

        private static boolean hasMembers(Map<String, Object> allowableValues) {
            Object values = allowableValues == null ? null : allowableValues.get("enumVars");
            return values instanceof List && !((List<?>) values).isEmpty();
        }

        /**
         * The member of an enum the example names, or else the first one, which the log notes; written
         * by its constant, or by its value where no constant of the SDK names it.
         */
        private String enumMember(String type, Map<String, Object> allowableValues, Object example, String where) {
            Map<?, ?> chosen = member(allowableValues, example);
            if (chosen == null) {
                placeholders.add(where);
                chosen = (Map<?, ?>) ((List<?>) allowableValues.get("enumVars")).get(0);
            }

            if (type == null || !IDENTIFIER.matcher(type).matches()) {
                String value = String.valueOf(chosen.get("value"));
                return value.startsWith("'") ? CallSnippets.quote(unquote(value)) : value;
            }

            names.add(type);
            String member = String.valueOf(chosen.get("name"));
            return IDENTIFIER.matcher(member).matches()
                    ? type + "." + member
                    : type + "[" + CallSnippets.quote(member) + "]";
        }

        /**
         * The member the example names, by its value or its name; null when it names none. The generator
         * holds the value of a string member in single quotes, which CallSnippets.member does not take off.
         */
        private static Map<?, ?> member(Map<String, Object> allowableValues, Object example) {
            Object values = allowableValues == null ? null : allowableValues.get("enumVars");
            if (example == null || !(values instanceof List)) {
                return null;
            }

            String text = SpecExamples.text(example);
            for (Object item : (List<?>) values) {
                Map<?, ?> member = (Map<?, ?>) item;
                String value = unquote(String.valueOf(member.get("value")));
                if (text.equals(value) || text.equals(String.valueOf(member.get("name")))) {
                    return member;
                }
            }

            return null;
        }

        /** A value of an enum member without the quotes the generator writes it in. */
        private static String unquote(String value) {
            if (value.length() >= 2 && value.startsWith("'") && value.endsWith("'")) {
                return value.substring(1, value.length() - 1).replace("\\'", "'").replace("\\\\", "\\");
            }

            return CallSnippets.unquote(value);
        }

        private CodegenModel model(Shape shape) {
            if (shape.complexType != null && models.containsKey(shape.complexType)) {
                return models.get(shape.complexType);
            }

            return shape.isModel ? models.get(plainType(shape.dataType)) : null;
        }

        /**
         * A value of a plain type. Dates, date-times and UUIDs are strings to the SDK; a date-time or a
         * UUID the example does not hold in its form counts as no example.
         */
        private String scalar(Shape shape, Object example, String name, String where) {
            String type = plainType(shape.dataType);
            String text = example == null ? null : SpecExamples.text(example);

            if (shape.isDateTime) {
                return dateTime(text, where);
            }

            if (shape.isDate) {
                return date(text, where);
            }

            if (shape.isUuid) {
                if (text == null || !UUID_TEXT.matcher(text).matches()) {
                    placeholders.add(where);
                    return NIL_UUID;
                }
                return CallSnippets.quote(text);
            }

            if (text == null) {
                placeholders.add(where);
                return placeholder(type, name);
            }

            switch (type == null ? "" : type) {
                case "string":
                    return CallSnippets.quote(text);
                case "number":
                    return text;
                case "boolean":
                    return text.toLowerCase(Locale.ROOT);
                default:
                    return literal(example);
            }
        }

        private static String placeholder(String type, String name) {
            switch (type == null ? "" : type) {
                case "string":
                    return CallSnippets.quote(CallSnippets.placeholderName(name));
                case "number":
                    return "0";
                case "boolean":
                    return "false";
                default:
                    return "null";
            }
        }

        private String dateTime(String text, String where) {
            if (text != null) {
                try {
                    OffsetDateTime.parse(text);
                    return CallSnippets.quote(text);
                } catch (DateTimeParseException notADateTime) {
                    // Falls through to the placeholder.
                }
            }

            placeholders.add(where);
            return "new Date().toISOString()";
        }

        private String date(String text, String where) {
            if (text != null) {
                String day = text.length() > 10 ? text.substring(0, 10) : text;
                try {
                    LocalDate.parse(day);
                    return CallSnippets.quote(day);
                } catch (DateTimeParseException notADate) {
                    // Falls through to the placeholder.
                }
            }

            placeholders.add(where);
            return "new Date().toISOString().slice(0, 10)";
        }

        /** An example as the JavaScript literal of the JSON value it holds. */
        private static String literal(Object example) {
            if (example == null) {
                return "null";
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
                return entries.isEmpty() ? "{}" : "{ " + String.join(", ", entries) + " }";
            }

            return SpecExamples.text(example);
        }

        /** A type without the null it may be united with: `string | null` gives string. */
        private static String plainType(String type) {
            String nullable = " | null";
            return type != null && type.endsWith(nullable) ? type.substring(0, type.length() - nullable.length()) : type;
        }

        /** The value type of a dictionary type, `{ [key: string]: number; }` giving number; null for any other type. */
        private static String valueType(String type) {
            String prefix = "{ [key: string]: ";
            if (type == null || !type.startsWith(prefix) || !type.endsWith("; }")) {
                return null;
            }

            return type.substring(prefix.length(), type.length() - "; }".length()).trim();
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
            boolean isAnyType;
            boolean isModel;
            boolean isDate;
            boolean isDateTime;
            boolean isUuid;
            boolean isFile;
            boolean isFileList;
            Integer minItems;
            Integer minProperties;
            Map<String, Object> allowableValues;
            CodegenProperty items;

            /**
             * The shape of a parameter; an enum declared in place in it is a constant the generator names
             * after the operation already: GetFolderByFolderIdFolderTypeEnum.
             */
            static Shape of(CodegenParameter parameter) {
                Shape shape = new Shape();
                shape.dataType = parameter.dataType;
                shape.complexType = parameter.baseType;
                shape.enumType = enumName("", parameter.datatypeWithEnum);
                shape.isEnum = parameter.isEnum;
                shape.isArray = parameter.isArray;
                shape.isMap = parameter.isMap;
                shape.isFreeFormObject = parameter.isFreeFormObject;
                shape.isAnyType = parameter.isAnyType;
                shape.isModel = parameter.isModel;
                shape.isDate = parameter.isDate;
                shape.isDateTime = parameter.isDateTime;
                shape.isUuid = parameter.isUuid;
                shape.isFile = parameter.isFile || parameter.isBinary;
                shape.isFileList = (shape.isFile && parameter.isCollectionFormatMulti)
                        || (parameter.isArray && parameter.items != null && (parameter.items.isFile || parameter.items.isBinary));
                shape.minItems = parameter.minItems;
                shape.minProperties = parameter.getMinProperties();
                shape.allowableValues = parameter.allowableValues;
                shape.items = parameter.items;
                return shape;
            }

            /**
             * The shape of a property; an enum declared in place in it is a constant named after its model:
             * RoleEnum of AiThreadMessageLike is AiThreadMessageLikeRoleEnum.
             */
            static Shape of(CodegenProperty property, String owner) {
                Shape shape = new Shape();
                shape.dataType = property.dataType;
                shape.complexType = property.complexType;
                shape.enumType = enumName(owner, property.datatypeWithEnum);
                shape.isEnum = property.isEnum;
                shape.isArray = property.isArray;
                shape.isMap = property.isMap;
                shape.isFreeFormObject = property.isFreeFormObject;
                shape.isAnyType = property.isAnyType;
                shape.isModel = property.isModel;
                shape.isDate = property.isDate;
                shape.isDateTime = property.isDateTime;
                shape.isUuid = property.isUuid;
                shape.isFile = property.isFile || property.isBinary;
                shape.isFileList = property.isArray && property.items != null && (property.items.isFile || property.items.isBinary);
                shape.minItems = property.minItems;
                shape.minProperties = property.getMinProperties();
                shape.allowableValues = property.allowableValues;
                shape.items = property.items;
                return shape;
            }

            /**
             * The enum constant the type of a member names, after its owner: RoleEnum in RoleEnum or
             * Array&lt;RoleEnum&gt;; null when the type is a plain one.
             */
            private static String enumName(String owner, String type) {
                String name = plainType(type);
                if (name == null) {
                    return null;
                }

                String value = valueType(name);
                if (value != null) {
                    name = value;
                }

                while ((name.startsWith("Array<") || name.startsWith("Set<")) && name.endsWith(">")) {
                    name = plainType(name.substring(name.indexOf('<') + 1, name.length() - 1));
                }

                return owner != null && IDENTIFIER.matcher(name).matches() && !"string".equals(name)
                        && !"number".equals(name) && !"boolean".equals(name) ? owner + name : null;
            }

            /** The shape of an item of this array; an enum declared in place for the items is the array's. */
            Shape item() {
                Shape item = of(items, null);
                if (item.isEnum && enumType != null) {
                    item.enumType = enumType;
                }
                return item;
            }
        }
    }
}
