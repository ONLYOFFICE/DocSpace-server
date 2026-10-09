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

import org.openapitools.codegen.model.*;
import org.openapitools.codegen.languages.PythonClientCodegen;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.*;
import io.swagger.v3.oas.models.headers.*;
import static org.openapitools.codegen.utils.StringUtils.underscore;
import static org.openapitools.codegen.utils.StringUtils.camelize;
import io.swagger.v3.oas.models.media.Schema;
import org.openapitools.codegen.utils.*;
import org.openapitools.codegen.*;

import java.util.*;
import java.io.File;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeParseException;
import java.util.Map.Entry;
import java.util.regex.Pattern;
import java.util.stream.Collectors;

public class MyPythonClientCodegen extends PythonClientCodegen {

    /**
     * The call snippets of the reference pages instead of the SDK: one file, with a snippet per
     * operation, written into the folder the run was given with -o.
     */
    private boolean snippetsOnly;

    private Snippets snippets;

    public MyPythonClientCodegen() {
        super();
        this.templateDir = "templates/python";
        this.embeddedTemplateDir = "python";

        supportingFiles.add(new SupportingFile("main.mustache", "samples", "main.py"));
        
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

        this.outputFolder = "../../../../../sdk/docspace-api-sdk-python";

        if (openAPI.getServers() != null && !openAPI.getServers().isEmpty()) {
            Server server = openAPI.getServers().get(0);
            ServerVariables serverVars = server.getVariables();
            if (serverVars != null){
                ServerVariable baseUrlVar = serverVars.get("baseUrl");
                if(baseUrlVar != null && "".equals(baseUrlVar.getDefault())){
                    baseUrlVar.setDefault("http://localhost:8092");
                }
            }
        }

        supportingFiles.removeIf(f -> f.getTemplateFile().equals("git_push.sh.mustache") || 
            f.getDestinationFilename().equals(".openapi-generator-ignore") || 
            f.getTemplateFile().equals("setup.mustache") ||
            f.getTemplateFile().equals("setup_cfg.mustache")
        );

        if(Boolean.TRUE.equals(additionalProperties.get("excludeTests")))
        {
            modelTestTemplateFiles.clear();
            apiTestTemplateFiles.clear();
        }
    }

    /** On a parameter that differs in the twin: the pydantic typing of the union, and of the twin alone. */
    private static final String PY_TYPING = "x-py-typing";
    private static final String PY_TYPING_UNION = "x-thirdparty-py-typing-union";
    private static final String PY_TYPING_VARIANT = "x-thirdparty-py-typing-variant";

    // Python has typing.overload, so the twin stays attached to its operation: the method takes
    // Union[int, str] ids, three @overload stubs give the precise answer type per id type, and the
    // response map picks the model at run time by the id's type. The twin's parameters get their
    // pydantic typing the same way the real operations do - by running them through the base class.
    private void markThirdPartyOverloads(OperationsMap objs, List<ModelMap> allModels) {
        List<CodegenOperation> variants = new ArrayList<>();
        for (CodegenOperation op : objs.getOperations().getOperation()) {
            Object attached = op.vendorExtensions.get(ThirdPartyVariants.VARIANT_OPERATION);
            if (attached instanceof CodegenOperation) {
                variants.add((CodegenOperation) attached);
            }
        }
        if (variants.isEmpty()) {
            return;
        }

        boolean typed = variants.stream()
            .flatMap(v -> v.allParams.stream())
            .allMatch(p -> p.vendorExtensions.containsKey(PY_TYPING));
        if (!typed) {
            OperationMap variantMap = new OperationMap();
            variantMap.setClassname(objs.getOperations().getClassname());
            variantMap.setOperation(variants);
            OperationsMap scratch = new OperationsMap();
            scratch.setOperation(variantMap);
            scratch.setImports(new ArrayList<>(objs.getImports()));
            super.postProcessOperationsWithModels(scratch, allModels);
        }

        // The base class rebuilds the file's imports from what the operations' own typing names, so the
        // twin's models - its answer, its body - are appended here in the same "from ... import ..." form.
        List<Map<String, String>> imports = objs.getImports();
        Set<String> present = new HashSet<>();
        for (Map<String, String> entry : imports) {
            present.add(entry.get("import"));
        }
        for (CodegenOperation variant : variants) {
            for (String name : variant.imports) {
                String line = toModelImport(name);
                if (line != null && present.add(line)) {
                    Map<String, String> entry = new LinkedHashMap<>();
                    entry.put("import", line);
                    imports.add(entry);
                }
            }
        }

        for (CodegenOperation op : objs.getOperations().getOperation()) {
            ThirdPartyVariants.markUnions(op, (a, b) -> "Union[" + a + ", " + b + "]");

            Object attached = op.vendorExtensions.get(ThirdPartyVariants.VARIANT_OPERATION);
            if (!(attached instanceof CodegenOperation)) {
                continue;
            }
            CodegenOperation variant = (CodegenOperation) attached;
            CodegenParameter variantBody = variant.allParams.stream().filter(p -> p.isBodyParam).findFirst().orElse(null);

            for (CodegenParameter parameter : op.allParams) {
                if (!parameter.vendorExtensions.containsKey(ThirdPartyVariants.UNION_TYPE)) {
                    continue;
                }
                CodegenParameter twin = parameter.isBodyParam
                    ? variantBody
                    : variant.allParams.stream().filter(p -> p.paramName.equals(parameter.paramName)).findFirst().orElse(null);
                if (twin == null) {
                    continue;
                }
                Object original = parameter.vendorExtensions.get(PY_TYPING);
                Object twinTyping = twin.vendorExtensions.get(PY_TYPING);
                if (original != null && twinTyping != null) {
                    parameter.vendorExtensions.put(PY_TYPING_VARIANT, twinTyping);
                    parameter.vendorExtensions.put(PY_TYPING_UNION, "Union[" + original + ", " + twinTyping + "]");
                }
            }
        }
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

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        ThirdPartyVariants.addAttachedImports(this, objs);

        super.postProcessOperationsWithModels(objs, allModels);
        markThirdPartyOverloads(objs, allModels);

        if (objs != null && objs.getOperations() != null) {
            OperationMap operationMap = objs.getOperations();
            List<CodegenOperation> operationList = operationMap.getOperation();

            String className = operationMap.getClassname();
            if (className != null && className.endsWith(apiNameSuffix)) {
                className = className.substring(0, className.length() - 3);
            }
            TagParts tagParts = tagMapSanitize.get(className);
            operationMap.put("x-folder", underscore(tagParts.folderPart));
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

    /**
     * Gives every operation what its call snippet needs. A third-party twin stays attached to its
     * original here rather than being an operation of its own, so it never gets a snippet.
     */
    private void attachSnippets(OperationsMap objs, List<ModelMap> allModels) {
        if (snippets == null) {
            snippets = new Snippets(openAPI, allModels, packageName);
        }

        // From its own module, as the package's __init__ imports it: the classes of different sections
        // can share a name (SettingsApi, QuotaApi), and the package root keeps only the last of them.
        OperationMap operations = objs.getOperations();
        String classname = String.valueOf(operations.get("x-classname"));
        String apiImport = "from " + apiPackage + "." + operations.get("x-folder") + "." + underscore(classname)
                + " import " + classname;

        CallSnippets.attach(objs, operation -> snippets.attach(operation, apiImport));
    }

    @Override
    public ModelsMap postProcessModels(ModelsMap objs) {
        super.postProcessModels(objs);

        for (ModelMap mo : objs.getModels()) {
            CodegenModel model = mo.getModel();
            if ("ApiDateTime".equals(model.classname)) {
                model.vendorExtensions.put("isApiDateTime", true);
            }

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
            TagParts tagParts = tagMapSanitize.get(className);

            String folder = tagParts.folderPart;
            String classname = tagParts.classPart + apiNameSuffix;

            api.put("x-folder", underscore(folder));
            api.put("x-folder-api", underscore(tagParts.classPart + apiNameSuffix));
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

    @Override
    public Map<String, ModelsMap> postProcessAllModels(Map<String, ModelsMap> objs) {
        for (ModelsMap modelsMap : objs.values()) {
            for (ModelMap m : modelsMap.getModels()) {
                CodegenModel model = m.getModel();
                if (model != null) {
                    sanitizeNoneType(model.vars);
                    sanitizeNoneType(model.allVars);
                    sanitizeNoneType(model.requiredVars);
                    sanitizeNoneType(model.optionalVars);
                    sanitizeNoneType(model.readOnlyVars);
                    sanitizeNoneType(model.readWriteVars);
                }
            }
        }

        final Map<String, ModelsMap> processed = super.postProcessAllModels(objs);

        for (Map.Entry<String, ModelsMap> entry : processed.entrySet()) {
            ModelsMap modelsMap = entry.getValue();

            for (ModelMap m : modelsMap.getModels()) {
                CodegenModel model = m.getModel();

                if (model.isEnum && model.getAllowableValues() != null) {
                    
                    List<Map<String, Object>> enumVars = (List<Map<String, Object>>) model.getAllowableValues().get("enumVars");

                    if (enumVars != null) {
                        for (Map<String, Object> ev : enumVars) {
                            String name = (String) ev.get("name");

                            if ("None".equals(name)) {
                                ev.put("name", "None_");
                            }
                        }
                    }
                }
            }
        }

        return processed;
    }

    private void sanitizeNoneType(List<CodegenProperty> vars) {
        if (vars == null) {
            return;
        }
        for (CodegenProperty cp : vars) {
            coerceNoneTypeToAny(cp);
        }
    }

    private void coerceNoneTypeToAny(CodegenProperty cp) {
        if (cp == null) {
            return;
        }
        if (cp.isNull
            || "none_type".equals(cp.dataType)
            || "none_type".equals(cp.baseType)
            || "none_type".equals(cp.complexType)) {
            cp.isNull = false;
            cp.isArray = false;
            cp.isMap = false;
            cp.isFreeFormObject = false;
            cp.isModel = false;
            cp.isPrimitiveType = false;
            cp.isEnum = false;
            cp.isAnyType = true;
            cp.dataType = "object";
            cp.datatypeWithEnum = "object";
            cp.baseType = "object";
            cp.complexType = null;
        }
        coerceNoneTypeToAny(cp.items);
        coerceNoneTypeToAny(cp.additionalProperties);
    }

    @Override
    public String getName() {
        return "my-python";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Python client with example main.py.";
    }

    @Override
    public String apiFilename(String templateName, String tag) {
        String uniqueTag = uniqueCaseInsensitiveString(tag, seenApiFilenames);
        String suffix = apiTemplateFiles().get(templateName);
        TagParts tagParts = tagMap.get(tag);
        if (tagParts == null) {
            return apiFileFolder() + File.separator + toApiFilename(uniqueTag) + suffix;
        }

        String folderPath = apiFileFolder() + File.separator + underscore(tagParts.folderPart);
        String filename = toApiFilename(tagParts.classPart) + suffix;

        return folderPath + File.separator + filename;
    }

    private final Map<String, TagParts> tagMap = new HashMap<>();
    private final Map<String, TagParts> tagMapSanitize = new HashMap<>();

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
            tagMapSanitize.put(camelize(sanitizeName(tag)), info);
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
     * Builds what the call snippet of one operation needs beyond the template, as Python: the keyword
     * arguments of the call, the request body and the imports.
     * <p>
     * The body is the minimal one, as for C# (see {@link MyCSharpClientCodegen}): a model gets its
     * required fields only, with the values the document states for them. Optional parameters are left
     * out of the call, because the methods of the SDK default them to None.
     */
    private static final class Snippets {

        static final String ARGUMENTS = "x-snippet-arguments";
        static final String BODY = "x-snippet-body";
        static final String IMPORTS = "x-snippet-imports";

        /** Beyond this length the arguments are put one per line. */
        private static final int LINE_LENGTH = 60;

        /** Beyond this length an import is wrapped in parentheses, a name per line, as PEP 8 limits a line. */
        private static final int IMPORT_LENGTH = 79;

        private static final int MAX_DEPTH = 4;

        /** The SDK takes a file by its path and reads it itself. */
        private static final String FILE = "\"file.docx\"";

        private static final String NIL_UUID = "UUID(\"00000000-0000-0000-0000-000000000000\")";

        private static final Pattern UUID_TEXT =
                Pattern.compile("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        private final OpenAPI openAPI;
        private final String sdk;
        private final Map<String, CodegenModel> models;

        /** Values the document states no example for, as "operationId: name", for the run's log. */
        private final List<String> placeholders = new ArrayList<>();

        /** The names the snippet imports from the datetime module, from the SDK's models, and UUID. */
        private final Set<String> datetimes = new TreeSet<>();
        private final Set<String> modelNames = new TreeSet<>();
        private boolean usesUuid;

        Snippets(OpenAPI openAPI, List<ModelMap> allModels, String sdk) {
            this.openAPI = openAPI;
            this.sdk = sdk;
            this.models = CallSnippets.models(allModels);
        }

        List<String> placeholders() {
            return placeholders;
        }

        void attach(CodegenOperation operation, String apiImport) {
            datetimes.clear();
            modelNames.clear();
            usesUuid = false;

            List<String> arguments = new ArrayList<>();
            String body = null;

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;

                if (parameter.isBodyParam) {
                    body = body(Shape.of(parameter), CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);
                    arguments.add(parameter.paramName + "=body");
                    continue;
                }

                if (!parameter.required && !parameter.isFile && !parameter.isBinary) {
                    continue;
                }

                Object example = CallSnippets.plain(parameter.isFormParam
                        ? SpecExamples.formField(openAPI, operation, parameter.baseName)
                        : SpecExamples.parameter(openAPI, operation, parameter.baseName, SpecExamples.location(parameter)));

                arguments.add(parameter.paramName + "=" + value(Shape.of(parameter), example, parameter.baseName, where, 0));
            }

            operation.vendorExtensions.put(ARGUMENTS, list(arguments));
            if (body != null) {
                operation.vendorExtensions.put(BODY, body);
            }
            operation.vendorExtensions.put(IMPORTS, imports(apiImport));
        }

        /**
         * The imports, in the groups PEP 8 asks for: the standard library first, then the SDK - its
         * client, the API class and the models the snippet names.
         */
        private String imports(String apiImport) {
            List<String> standard = new ArrayList<>();
            if (!datetimes.isEmpty()) {
                standard.add(fromImport("datetime", datetimes));
            }
            if (usesUuid) {
                standard.add("from uuid import UUID");
            }

            List<String> own = new ArrayList<>();
            own.add("from " + sdk + " import ApiClient, Configuration");
            own.add(apiImport);
            if (!modelNames.isEmpty()) {
                own.add(fromImport(sdk + ".models", modelNames));
            }

            String block = String.join("\n", own);
            return standard.isEmpty() ? block : String.join("\n", standard) + "\n\n" + block;
        }

        private static String fromImport(String module, Collection<String> names) {
            String line = "from " + module + " import " + String.join(", ", names);
            if (line.length() <= IMPORT_LENGTH) {
                return line;
            }

            StringBuilder builder = new StringBuilder("from ").append(module).append(" import (");
            for (String name : names) {
                builder.append("\n    ").append(name).append(',');
            }

            return builder.append("\n)").toString();
        }

        /**
         * The request body. A model gets its required fields only, whatever the document states for
         * the body as a whole; an array or a scalar takes the example the document states for it.
         */
        private String body(Shape shape, Object example, String where) {
            CodegenModel model = shape.isArray ? null : model(shape);
            if (model != null && !model.isEnum) {
                return CallSnippets.isComposed(model) ? composed(model, null, where, 0) : construct(model, where, 0);
            }

            // A body parameter carries no item description, only the Python type.
            return shape.isArray && shape.items == null
                    ? typed(shape.dataType, example, where, 0)
                    : value(shape, example, "body", where, 0);
        }

        /**
         * The model with its required fields as keyword arguments. A model composed with allOf is a
         * subclass of its parent in pydantic, so the parent's required fields are among its own.
         */
        private String construct(CodegenModel model, String where, int depth) {
            modelNames.add(model.classname);

            // A model can contain itself; past this depth it is built without its fields, unvalidated.
            if (depth >= MAX_DEPTH) {
                return model.classname + ".model_construct()";
            }

            List<String> arguments = new ArrayList<>();
            Set<String> seen = new HashSet<>();
            List<CodegenProperty> properties = model.allVars != null && !model.allVars.isEmpty() ? model.allVars : model.vars;
            if (properties != null) {
                for (CodegenProperty property : properties) {
                    if (!property.required || !seen.add(property.name)) {
                        continue;
                    }

                    Object example = CallSnippets.plain(SpecExamples.inheritedProperty(openAPI, model.name, property.baseName));
                    arguments.add(property.name + "=" + value(Shape.of(property), example, property.baseName,
                            where + "." + property.baseName, depth + 1));
                }
            }

            return model.classname + "(" + (depth == 0 ? list(arguments) : String.join(", ", arguments)) + ")";
        }

        /**
         * A oneOf or anyOf model: a wrapper that takes the variant as its only positional argument. The
         * variant is the one the example fits, or the first one when there is no example.
         */
        private String composed(CodegenModel model, Object example, String where, int depth) {
            modelNames.add(model.classname);

            // A variant can be a oneOf of its own and lead back here, so the wrapper stops at the depth
            // the models do, with no variant set.
            if (depth >= MAX_DEPTH) {
                return model.classname + ".model_construct()";
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

            return model.classname + "(" + typed(chosen, example, where, depth + 1) + ")";
        }

        private boolean fits(String type, Object example) {
            if (example == null) {
                return false;
            }

            if (type.startsWith("List[")) {
                return example instanceof List;
            }

            switch (type) {
                case "str":
                    return example instanceof String;
                case "int":
                    return example instanceof Integer || example instanceof Long;
                case "float":
                    return example instanceof Number;
                case "bool":
                    return example instanceof Boolean;
                default:
                    CodegenModel model = models.get(type);
                    if (model == null) {
                        return false;
                    }

                    // An enum fits only an example that names one of its members; any other is left to
                    // a variant beside it, such as a string.
                    return model.isEnum ? member(model.allowableValues, example) != null : example instanceof Map;
            }
        }

        /** A value of a type the generator names by its Python spelling only, as the variants of a oneOf. */
        private String typed(String type, Object example, String where, int depth) {
            if (type.startsWith("List[") && type.endsWith("]")) {
                String itemType = type.substring("List[".length(), type.length() - 1);

                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(typed(itemType, item, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                }

                return "[" + String.join(", ", items) + "]";
            }

            if (type.startsWith("Dict[")) {
                return "{}";
            }

            String text = example == null ? null : SpecExamples.text(example);

            switch (type) {
                case "str":
                    if (text == null) {
                        placeholders.add(where);
                        return CallSnippets.quote("YOUR_VALUE");
                    }
                    return CallSnippets.quote(text);
                case "int":
                case "float":
                    return text == null ? "0" : text;
                case "bool":
                    return Boolean.parseBoolean(text) ? "True" : "False";
                case "datetime":
                    return dateTime(text, where);
                case "date":
                    return date(text, where);
                case "UUID":
                    return uuid(text, where);
                case "object":
                    return literal(example);
                default:
                    CodegenModel model = models.get(type);
                    if (model == null) {
                        return "None";
                    }
                    if (model.isEnum) {
                        Shape shape = new Shape();
                        shape.complexType = type;
                        return enumValue(shape, example, where);
                    }
                    return CallSnippets.isComposed(model) ? composed(model, example, where, depth) : construct(model, where, depth);
            }
        }

        private String value(Shape shape, Object example, String name, String where, int depth) {
            if (shape.isArray && shape.items != null && (shape.items.isFile || shape.items.isBinary)) {
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
                    return typed(shape.dataType, example, where, depth);
                }

                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(value(Shape.of(shape.items), item, name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    // The model checks the least number of items before the request is sent, so a list
                    // that must have some gets one, standing in for them.
                    if (shape.minItems != null && shape.minItems > 0) {
                        items.add(value(Shape.of(shape.items), null, name, where, depth + 1));
                    } else {
                        placeholders.add(where);
                    }
                }

                return "[" + String.join(", ", items) + "]";
            }

            // A free-form object is a dictionary in Python, as is a map.
            if (shape.isMap || shape.isFreeFormObject || (shape.dataType != null && shape.dataType.startsWith("Dict["))) {
                return "{}";
            }

            CodegenModel model = model(shape);
            if (model != null) {
                return CallSnippets.isComposed(model) ? composed(model, example, where, depth) : construct(model, where, depth);
            }

            String text = example == null ? null : SpecExamples.text(example);

            if (shape.isDateTime) {
                return dateTime(text, where);
            }
            if (shape.isDate) {
                return date(text, where);
            }
            if (shape.isUuid) {
                return uuid(text, where);
            }

            if (example == null) {
                placeholders.add(where);
                return placeholder(shape, name);
            }

            if (shape.isBoolean) {
                return Boolean.parseBoolean(text) ? "True" : "False";
            }
            if (shape.isNumeric) {
                return text;
            }
            if (shape.isAnyType) {
                return literal(example);
            }

            return CallSnippets.quote(text);
        }

        /**
         * An enum member. An enum schema is a class of the SDK with a member per value; an enum declared
         * in place is only its underlying type in Python, so the member is its value.
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

            boolean schema = model != null && model.isEnum;
            if (!schema && !shape.isEnum) {
                return null;
            }

            Map<String, Object> allowableValues = schema ? model.allowableValues : shape.allowableValues;
            Object values = allowableValues == null ? null : allowableValues.get("enumVars");
            if (!(values instanceof List) || ((List<?>) values).isEmpty()) {
                return null;
            }

            Map<?, ?> chosen = member(allowableValues, example);
            if (chosen == null) {
                placeholders.add(where);
                chosen = (Map<?, ?>) ((List<?>) values).get(0);
            }

            if (!schema) {
                return doubleQuoted(String.valueOf(chosen.get("value")));
            }

            modelNames.add(model.classname);
            return model.classname + "." + chosen.get("name");
        }

        /**
         * The member of an enum the example names (see CallSnippets.member). The Python generator writes
         * the values of a string enum in single quotes, so they are handed over in double ones, which
         * CallSnippets reads.
         */
        private static Map<?, ?> member(Map<String, Object> allowableValues, Object example) {
            Object values = allowableValues == null ? null : allowableValues.get("enumVars");
            if (!(values instanceof List)) {
                return null;
            }

            List<?> members = (List<?>) values;
            List<Map<Object, Object>> requoted = new ArrayList<>();
            for (Object item : members) {
                Map<Object, Object> copy = new HashMap<>((Map<?, ?>) item);
                copy.put("value", doubleQuoted(String.valueOf(copy.get("value"))));
                requoted.add(copy);
            }

            Map<String, Object> enumVars = new HashMap<>();
            enumVars.put("enumVars", requoted);

            Map<?, ?> found = CallSnippets.member(enumVars, example);
            for (int i = 0; i < requoted.size(); i++) {
                if (requoted.get(i) == found) {
                    return (Map<?, ?>) members.get(i);
                }
            }

            return null;
        }

        /** A string literal of the generator, 'text', in the double quotes the rest of the snippet uses. */
        private static String doubleQuoted(String literal) {
            if (literal.length() >= 2 && literal.startsWith("'") && literal.endsWith("'")) {
                return CallSnippets.quote(literal.substring(1, literal.length() - 1).replace("\\'", "'"));
            }

            return literal;
        }

        private CodegenModel model(Shape shape) {
            if (shape.complexType != null && models.containsKey(shape.complexType)) {
                return models.get(shape.complexType);
            }
            if (shape.isModel) {
                return models.get(shape.dataType);
            }

            // A body that is an enum schema comes as its underlying string; only the typing the method
            // signature gets from the generator names the enum.
            return shape.typing != null ? models.get(shape.typing) : null;
        }

        /** A date and time as an aware datetime in UTC, the way the example states the moment. */
        private String dateTime(String text, String where) {
            datetimes.add("datetime");
            datetimes.add("timezone");

            if (text != null) {
                try {
                    OffsetDateTime moment = OffsetDateTime.parse(text).withOffsetSameInstant(ZoneOffset.UTC);
                    StringBuilder builder = new StringBuilder("datetime(")
                            .append(moment.getYear()).append(", ")
                            .append(moment.getMonthValue()).append(", ")
                            .append(moment.getDayOfMonth()).append(", ")
                            .append(moment.getHour()).append(", ")
                            .append(moment.getMinute());
                    int microsecond = moment.getNano() / 1000;
                    if (moment.getSecond() != 0 || microsecond != 0) {
                        builder.append(", ").append(moment.getSecond());
                    }
                    if (microsecond != 0) {
                        builder.append(", ").append(microsecond);
                    }
                    return builder.append(", tzinfo=timezone.utc)").toString();
                } catch (DateTimeParseException notADateTime) {
                    try {
                        LocalDate day = LocalDate.parse(text);
                        return "datetime(" + day.getYear() + ", " + day.getMonthValue() + ", " + day.getDayOfMonth()
                                + ", tzinfo=timezone.utc)";
                    } catch (DateTimeParseException notADate) {
                        // Falls through to the placeholder.
                    }
                }
            }

            placeholders.add(where);
            return "datetime.now(timezone.utc)";
        }

        private String date(String text, String where) {
            datetimes.add("date");

            if (text != null) {
                try {
                    LocalDate day = LocalDate.parse(text.length() > 10 ? text.substring(0, 10) : text);
                    return "date(" + day.getYear() + ", " + day.getMonthValue() + ", " + day.getDayOfMonth() + ")";
                } catch (DateTimeParseException notADate) {
                    // Falls through to the placeholder.
                }
            }

            placeholders.add(where);
            return "date.today()";
        }

        private String uuid(String text, String where) {
            usesUuid = true;

            // UUID() refuses a text that is not one, so an example such as "1" counts as none.
            if (text == null || !UUID_TEXT.matcher(text).matches()) {
                placeholders.add(where);
                return NIL_UUID;
            }

            return "UUID(" + CallSnippets.quote(text) + ")";
        }

        /** An example of a value of any type, as the Python literal of what it holds. */
        private static String literal(Object example) {
            if (example == null) {
                return "None";
            }
            if (example instanceof Boolean) {
                return (Boolean) example ? "True" : "False";
            }
            if (example instanceof Number) {
                return SpecExamples.text(example);
            }
            if (example instanceof List) {
                List<String> items = new ArrayList<>();
                for (Object item : (List<?>) example) {
                    items.add(literal(item));
                }
                return "[" + String.join(", ", items) + "]";
            }
            if (example instanceof Map) {
                List<String> entries = new ArrayList<>();
                for (Map.Entry<?, ?> entry : ((Map<?, ?>) example).entrySet()) {
                    entries.add(CallSnippets.quote(String.valueOf(entry.getKey())) + ": " + literal(entry.getValue()));
                }
                return "{" + String.join(", ", entries) + "}";
            }

            return CallSnippets.quote(SpecExamples.text(example));
        }

        private static String placeholder(Shape shape, String name) {
            if (shape.isBoolean) {
                return "False";
            }
            if (shape.isNumeric) {
                return "0";
            }
            if (shape.isString || "str".equals(shape.dataType)) {
                return CallSnippets.quote(CallSnippets.placeholderName(name));
            }

            return "None";
        }

        /**
         * Keyword arguments on one line, or one per line once they get long, with the trailing comma
         * the formatters put there. The call stands in the block of the client, four spaces in.
         */
        private static String list(List<String> items) {
            String line = String.join(", ", items);
            if (line.length() <= LINE_LENGTH && !line.contains("\n")) {
                return line;
            }

            StringBuilder builder = new StringBuilder();
            for (String item : items) {
                builder.append("\n        ").append(item).append(',');
            }

            return builder.append("\n    ").toString();
        }

        /** What the snippet needs to know about the type of a parameter or a property. */
        private static final class Shape {
            String dataType;
            String complexType;
            boolean isString;
            boolean isNumeric;
            boolean isBoolean;
            boolean isDate;
            boolean isDateTime;
            boolean isUuid;
            boolean isEnum;
            boolean isArray;
            boolean isMap;
            boolean isModel;
            boolean isFile;
            boolean isAnyType;
            boolean isFreeFormObject;
            Integer minItems;
            String typing;
            Map<String, Object> allowableValues;
            CodegenProperty items;

            static Shape of(CodegenParameter parameter) {
                Shape shape = new Shape();
                shape.dataType = parameter.dataType;
                shape.complexType = parameter.baseType;
                shape.isFreeFormObject = parameter.isFreeFormObject;
                shape.minItems = parameter.minItems;
                Object typing = parameter.vendorExtensions.get(PY_TYPING);
                shape.typing = typing == null ? null : String.valueOf(typing);
                shape.isString = parameter.isString;
                shape.isNumeric = parameter.isNumeric || parameter.isInteger || parameter.isLong || parameter.isNumber
                        || parameter.isFloat || parameter.isDouble || parameter.isDecimal;
                shape.isBoolean = parameter.isBoolean;
                shape.isDate = parameter.isDate;
                shape.isDateTime = parameter.isDateTime;
                shape.isUuid = parameter.isUuid;
                shape.isEnum = parameter.isEnum;
                shape.isArray = parameter.isArray;
                shape.isMap = parameter.isMap;
                shape.isModel = parameter.isModel;
                shape.isFile = parameter.isFile || parameter.isBinary;
                shape.isAnyType = parameter.isAnyType;
                shape.allowableValues = parameter.allowableValues;
                shape.items = parameter.items;
                return shape;
            }

            static Shape of(CodegenProperty property) {
                Shape shape = new Shape();
                shape.dataType = property.dataType;
                shape.complexType = property.complexType;
                shape.isFreeFormObject = property.isFreeFormObject;
                shape.minItems = property.minItems;
                shape.isString = property.isString;
                shape.isNumeric = property.isNumeric || property.isInteger || property.isLong || property.isNumber
                        || property.isFloat || property.isDouble || property.isDecimal;
                shape.isBoolean = property.isBoolean;
                shape.isDate = property.isDate;
                shape.isDateTime = property.isDateTime;
                shape.isUuid = property.isUuid;
                shape.isEnum = property.isEnum;
                shape.isArray = property.isArray;
                shape.isMap = property.isMap;
                shape.isModel = property.isModel;
                shape.isFile = property.isFile || property.isBinary;
                shape.isAnyType = property.isAnyType;
                shape.allowableValues = property.allowableValues;
                shape.items = property.items;
                return shape;
            }
        }
    }
}
