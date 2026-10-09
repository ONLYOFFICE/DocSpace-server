package com.example.codegen;

import java.io.File;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeParseException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Set;
import java.util.TreeSet;
import java.util.function.UnaryOperator;

import com.samskivert.mustache.Mustache;
import com.samskivert.mustache.Template;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.CodegenModel;
import org.openapitools.codegen.CodegenParameter;
import org.openapitools.codegen.CodegenProperty;
import org.openapitools.codegen.languages.GoClientCodegen;
import org.openapitools.codegen.model.ApiInfoMap;
import org.openapitools.codegen.model.ModelsMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.model.OperationsMap;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;
import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.headers.*;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.model.ModelMap;

public class MyGoClientCodegen extends GoClientCodegen {

    private static class TagParts {
        final String originalTag;
        final String folderPart;
        final String classPart;
        final String filePart;

        TagParts(String originalTag, String folderPart, String classPart, String filePart) {
            this.originalTag = originalTag;
            this.folderPart = folderPart;
            this.classPart = classPart;
            this.filePart = filePart;
        }
    }

    private final Map<String, TagParts> tagMap = new HashMap<>();
    private final Map<String, String> seenApiFilenames = new HashMap<>();

    /**
     * The call snippets of the reference pages instead of the SDK: one file, with a snippet per
     * operation, written into the folder the run was given with -o.
     */
    private boolean snippetsOnly;

    private Snippets snippets;

    public MyGoClientCodegen() {
        super();
        this.templateDir = "templates/go";
        this.embeddedTemplateDir = "go";
    }

    @Override
    public String getName() {
        return "my-go";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Golang client";
    }

    @Override
    public String sanitizeTag(String tag) {
        String sanitized = super.sanitizeTag(tag);

        if (!tagMap.containsKey(sanitized)) {
            String[] parts = tag.split(" / ", 2);
            String folderPartRaw = parts[0];
            String classPartRaw = (parts.length > 1) ? parts[1] : parts[0];

            String folderPartSanitized = normalizeTagDisplayName(sanitizeName(folderPartRaw));
            String classPartSanitized = normalizeTagDisplayName(sanitizeName(classPartRaw));

            boolean duplicateClass = tagMap.values().stream()
                .anyMatch(tp -> tp.classPart.equalsIgnoreCase(classPartSanitized));

            String finalClassPart = duplicateClass
                ? folderPartSanitized + classPartSanitized
                : classPartSanitized;
            String finalFilePart = parts.length > 1
                ? folderPartSanitized + classPartSanitized
                : classPartSanitized;

            TagParts info = new TagParts(
                tag,
                folderPartSanitized,
                finalClassPart,
                finalFilePart
            );

            tagMap.put(sanitized, info);
        }

        return sanitized;
    }

    @Override
    public String apiFilename(String templateName, String tag) {
        String sanitizedTag = sanitizeTag(tag);
        String suffix = apiTemplateFiles().get(templateName);

        TagParts tagParts = tagMap.get(sanitizedTag);

        if (tagParts == null) {
            String uniqueTag = makeUniqueTag(sanitizedTag);
            return apiFileFolder() + File.separator + toApiFilename(uniqueTag) + suffix;
        }

        String uniqueFilePart = makeUniqueTag(tagParts.filePart);
        String filename = toApiFilename(uniqueFilePart) + suffix;

        return apiFileFolder() + File.separator + filename;
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

        // Models and api files live in the output root here, named model-*.go and api-*.go (see the
        // toModelFilename / toApiFilename overrides below), so only those are swept there.
        StaleOutput.delete(this, name -> name.startsWith("model-") || name.startsWith("api-"));
        LineEndings.normalize(this);
    }

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        // Go has neither overloads nor unions, so the twin stays attached: an id that differs is taken
        // as interface{} (int32 or string), a body that differs gets a second setter, and where the
        // answer differs the request gets ExecuteThirdParty() next to Execute() - the same request,
        // decoded into the third-party model. The template renders the execute function once per shape.
        ThirdPartyVariants.addAttachedImports(this, objs);

        super.postProcessOperationsWithModels(objs, allModels);
        for (CodegenOperation op : objs.getOperations().getOperation()) {
            // Also gives the twin's body parameter the original's name, which the twin's execute function,
            // rendered against the original's request struct, needs (saveAsPdf, not thirdPartySaveAsPdf).
            ThirdPartyVariants.markUnions(op, (a, b) -> "interface{}");

            Object attached = op.vendorExtensions.get(ThirdPartyVariants.VARIANT_OPERATION);
            if (attached instanceof CodegenOperation) {
                // The base class turns "POST" into "Post" (http.MethodPost) for the listed operations
                // only; the twin, rendered through the same execute partial, takes it from the original.
                ((CodegenOperation) attached).httpMethod = op.httpMethod;
            }
        }

        if (objs != null && objs.getOperations() != null) {
            OperationMap operationMap = objs.getOperations();
            List<CodegenOperation> operationList = operationMap.getOperation();
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

                    if (op.notes != null && !op.notes.isEmpty()) {
                        String commentedNotes = op.notes
                            .replace("\r\n", "\n")
                            .replace("\r", "\n")
                            .replace("\n", "\n// ");
                        op.vendorExtensions.put("x-commentedNotes", commentedNotes);
                    }
                }
            }

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
            // The field of a oneOf or anyOf struct that holds a variant, named by the same lambda the
            // model template names it with.
            Object lambda = additionalProperties.get("lambda.type-to-name");
            if (!(lambda instanceof Mustache.Lambda)) {
                throw new IllegalStateException("The Go generator offers no lambda.type-to-name to name the fields of a oneOf by");
            }
            Template template = Mustache.compiler().escapeHTML(false).compile("{{#lambda}}{{type}}{{/lambda}}");

            snippets = new Snippets(
                openAPI,
                allModels,
                String.valueOf(additionalProperties.get("packageName")),
                Boolean.parseBoolean(String.valueOf(additionalProperties.get("enumClassPrefix"))),
                type -> template.execute(Map.of("lambda", lambda, "type", type))
            );
        }

        CallSnippets.attach(objs, snippets::attach);
    }

    @Override
    public ModelsMap postProcessModels(ModelsMap objs) {
        super.postProcessModels(objs);

        for (ModelMap modelMap : objs.getModels()) {
            CodegenModel model = modelMap.getModel();
            if ("ApiDateTime".equals(model.classname)) {
                model.vendorExtensions.put("x-docspace-handle-string-datetime", true);
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
        }

        return objs;
    }

    private String toDashCase(String input) {
        return input.replaceAll("([a-z])([A-Z])", "$1-$2")
                    .replaceAll("([A-Z]+)([A-Z][a-z])", "$1-$2")
                    .toLowerCase();
    }

    private String makeUniqueTag(String tag) {
        String lower = tag.toLowerCase(Locale.ROOT);
        if (!seenApiFilenames.containsKey(lower)) {
            seenApiFilenames.put(lower, tag);
            return tag;
        }

        int i = 2;
        while (seenApiFilenames.containsKey(lower + "_" + i)) {
            i++;
        }

        String unique = tag + "_" + i;
        seenApiFilenames.put(lower + "_" + i, unique);
        return unique;
    }

    @Override
    public void processOpts() {
        super.processOpts();

        // Returns before the output folder is pointed at the SDK checkout (see CallSnippets.configure).
        snippetsOnly = CallSnippets.configure(this);
        if (snippetsOnly) {
            // The snippets import the SDK by the path of its module.
            applyModulePath();

            return;
        }

        this.outputFolder = "../../../../../sdk/docspace-api-sdk-go";

        supportingFiles.removeIf(file ->
            "README.md".equals(file.getDestinationFilename())
                || "README.mustache".equals(file.getTemplateFile())
                || "golang_README.mustache".equals(file.getTemplateFile())
        );
        supportingFiles.add(new SupportingFile("golang_README.mustache", "", "README.md"));

        String sampleDir = "samples" + File.separator + "docspace-api-sdk-go-sample";
        supportingFiles.add(new SupportingFile("sample_main.mustache", sampleDir, "main.go"));
        supportingFiles.add(new SupportingFile("sample_go.mod.mustache", sampleDir, "go.mod"));

        supportingFiles.add(new SupportingFile(
            "AUTHORS.mustache", "", "AUTHORS.md"
        ));

        supportingFiles.add(new SupportingFile(
            "LICENSE.mustache", "", "LICENSE"
        ));

        supportingFiles.add(new SupportingFile(
            "CHANGELOG.mustache", "", "CHANGELOG.md"
        ));
        
        Object exclude = additionalProperties.get("excludeTests");
        if (Boolean.TRUE.equals(exclude)) {
            apiTestTemplateFiles.clear();
            modelTestTemplateFiles.clear();
        }

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

        supportingFiles.removeIf(f -> f.getTemplateFile().equals("git_push.sh.mustache"));

        applyModulePath();
    }

    /**
     * The import path of the SDK, taken from its repository URL, and the path of its module, which
     * carries the major version from v2 on, as Go requires.
     */
    private void applyModulePath() {
        String packageImportPath = null;
        Object repositoryUrlObj = additionalProperties.get("repositoryUrl");
        if (repositoryUrlObj != null) {
            packageImportPath = String.valueOf(repositoryUrlObj)
                .trim()
                .replaceFirst("^https?://", "")
                .replaceFirst("\\.git$", "")
                .replaceFirst("/+$", "");
        } else if (additionalProperties.get("packageImportPath") != null) {
            packageImportPath = String.valueOf(additionalProperties.get("packageImportPath")).trim();
        }

        if (packageImportPath != null && !packageImportPath.isEmpty()) {
            additionalProperties.put("packageImportPath", packageImportPath);

            String version = String.valueOf(additionalProperties.getOrDefault("packageVersion", "0.0.0"));
            String major = version.replaceFirst("^v", "").split("\\.")[0];

            String goMajorSuffix = "";
            if (!"0".equals(major) && !"1".equals(major)) {
                goMajorSuffix = "/v" + major;
            }

            additionalProperties.put("goMajorSuffix", goMajorSuffix);
            additionalProperties.put("goModulePath", packageImportPath + goMajorSuffix);
        }
    }

    @Override
    public Map<String, Object> postProcessSupportingFileData(Map<String, Object> objs) {
        super.postProcessSupportingFileData(objs);

        Object apiInfoObject = objs.get("apiInfo");
        if (!(apiInfoObject instanceof ApiInfoMap)) {
            return objs;
        }

        ApiInfoMap apiInfo = (ApiInfoMap) apiInfoObject;
        Map<String, List<Map<String, Object>>> folderToApis = new LinkedHashMap<>();

        for (OperationsMap api : apiInfo.getApis()) {
            OperationMap operationMap = api.getOperations();
            String className = trimApiSuffix(operationMap.getClassname());
            TagParts tagParts = findTagParts(className);

            if (tagParts == null) {
                String fallback = normalizeTagDisplayName(className == null ? "Default" : className);
                tagParts = new TagParts(fallback, "Default", fallback, fallback);
            }

            api.put("x-folder", tagParts.folderPart);
            api.put("x-classname", tagParts.classPart + apiNameSuffix);

            folderToApis.computeIfAbsent(tagParts.folderPart, k -> new ArrayList<>()).add(api);
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

    private String trimApiSuffix(String className) {
        if (className == null) {
            return null;
        }
        if (apiNameSuffix != null && !apiNameSuffix.isEmpty() && className.endsWith(apiNameSuffix)) {
            return className.substring(0, className.length() - apiNameSuffix.length());
        }
        return className;
    }

    private TagParts findTagParts(String className) {
        if (className == null) {
            return null;
        }

        TagParts direct = tagMap.get(className);
        if (direct != null) {
            return direct;
        }

        for (Map.Entry<String, TagParts> entry : tagMap.entrySet()) {
            TagParts parts = entry.getValue();
            if (entry.getKey().equalsIgnoreCase(className)
                    || parts.classPart.equalsIgnoreCase(className)
                    || (parts.folderPart + parts.classPart).equalsIgnoreCase(className)) {
                return parts;
            }
        }

        return null;
    }

    private String normalizeTagDisplayName(String value) {
        if (value == null || value.isEmpty()) {
            return value;
        }

        String normalized = value
            .replace('_', ' ')
            .replace('-', ' ')
            .replaceAll("([a-z0-9])([A-Z])", "$1 $2")
            .replaceAll("([A-Z]+)([A-Z][a-z])", "$1 $2")
            .replaceAll("[^A-Za-z0-9 ]+", " ")
            .trim();

        if (normalized.isEmpty()) {
            return value;
        }

        StringBuilder result = new StringBuilder();
        for (String part : normalized.split("\\s+")) {
            if (part.isEmpty()) {
                continue;
            }

            if (part.matches("[A-Z0-9]+")) {
                result.append(part);
            } else {
                result.append(Character.toUpperCase(part.charAt(0)));
                if (part.length() > 1) {
                    result.append(part.substring(1).toLowerCase(Locale.ROOT));
                }
            }
        }

        return result.toString();
    }

    @Override
    public String toApiFilename(String name) {
        return super.toApiFilename(name).replace('_', '-');
    }

    @Override
    public String toModelFilename(String name) {
        return super.toModelFilename(name).replace('_', '-');
    }

    /**
     * Builds what the call snippet of one operation needs beyond the template, as Go: the arguments of
     * the method, the setters of the request, the request body and the standard packages the snippet
     * imports.
     * <p>
     * The body is the minimal one, as for C# (see {@link MyCSharpClientCodegen}): a model gets its
     * required properties only, with the values the document states for them. Optional parameters are
     * left out of the call, because the request leaves them unset.
     */
    private static final class Snippets {

        static final String IMPORTS = "x-snippet-imports";
        static final String BODY = "x-snippet-body";
        static final String CALL = "x-snippet-call";
        static final String OPENS_FILE = "x-snippet-opens-file";

        /** Beyond this length the arguments of the body's constructor are put one per line. */
        private static final int LINE_LENGTH = 60;

        private static final int MAX_DEPTH = 4;

        /** The variable the template opens the uploaded file into. */
        private static final String FILE = "file";

        private static final String NULLABLE = "Nullable";

        private static final Set<String> BUILTIN = new HashSet<>(Arrays.asList(
                "string", "bool", "byte", "rune", "int", "int32", "int64", "float32", "float64", "interface{}", "any"));

        /** The pointer helpers of the SDK, by the type they point to. */
        private static final Map<String, String> POINTERS = new HashMap<>();

        static {
            POINTERS.put("string", "PtrString");
            POINTERS.put("bool", "PtrBool");
            POINTERS.put("int", "PtrInt");
            POINTERS.put("int32", "PtrInt32");
            POINTERS.put("int64", "PtrInt64");
            POINTERS.put("float32", "PtrFloat32");
            POINTERS.put("float64", "PtrFloat64");
            POINTERS.put("time.Time", "PtrTime");
        }

        /** The nullable wrappers of the SDK for the types it does not name after a model. */
        private static final Map<String, String> NULLABLES = new HashMap<>();

        static {
            NULLABLES.put("NullableString", "string");
            NULLABLES.put("NullableBool", "bool");
            NULLABLES.put("NullableInt", "int");
            NULLABLES.put("NullableInt32", "int32");
            NULLABLES.put("NullableInt64", "int64");
            NULLABLES.put("NullableFloat32", "float32");
            NULLABLES.put("NullableFloat64", "float64");
            NULLABLES.put("NullableTime", "time.Time");
        }

        private static final String[] MONTHS = {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        private final OpenAPI openAPI;
        private final String sdk;
        private final boolean enumClassPrefix;
        private final Map<String, CodegenModel> models;

        /** The field of a oneOf or anyOf struct that holds a variant of the given type. */
        private final UnaryOperator<String> fieldName;

        /** Values the document states no example for, as "operationId: name", for the run's log. */
        private final List<String> placeholders = new ArrayList<>();

        private final Set<String> imports = new TreeSet<>();
        private boolean opensFile;

        Snippets(OpenAPI openAPI, List<ModelMap> allModels, String sdk, boolean enumClassPrefix,
                UnaryOperator<String> fieldName) {
            this.openAPI = openAPI;
            this.sdk = sdk;
            this.enumClassPrefix = enumClassPrefix;
            this.models = CallSnippets.models(allModels);
            this.fieldName = fieldName;
        }

        List<String> placeholders() {
            return placeholders;
        }

        void attach(CodegenOperation operation) {
            imports.clear();
            imports.add("context");
            imports.add("log");
            if (operation.returnType != null) {
                imports.add("fmt");
            }
            opensFile = false;

            Map<String, String> path = new LinkedHashMap<>();
            List<String> setters = new ArrayList<>();
            String body = null;

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;

                if (parameter.isBodyParam) {
                    String value = body(Shape.of(parameter), CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);

                    // A model is built by its constructor, which returns a pointer, as the SDK's sample
                    // keeps it; the setter takes the value.
                    boolean pointer = value.startsWith("*");
                    body = pointer ? value.substring(1) : value;
                    setters.add(setter(parameter) + "(" + (pointer ? "*body" : "body") + ")");
                    continue;
                }

                if (!parameter.required && !parameter.isFile && !parameter.isBinary) {
                    continue;
                }

                Object example = CallSnippets.plain(parameter.isFormParam
                        ? SpecExamples.formField(openAPI, operation, parameter.baseName)
                        : SpecExamples.parameter(openAPI, operation, parameter.baseName, SpecExamples.location(parameter)));

                String value = value(Shape.of(parameter), example, parameter.baseName, where, 1);

                if (parameter.isPathParam) {
                    path.put(parameter.paramName, value);
                } else {
                    setters.add(setter(parameter) + "(" + value + ")");
                }
            }

            // The method takes the path parameters in the order of its signature.
            StringBuilder method = new StringBuilder(operation.nickname).append("(ctx");
            for (CodegenParameter parameter : operation.pathParams) {
                String value = path.get(parameter.paramName);
                if (value != null) {
                    method.append(", ").append(value);
                }
            }
            method.append(')');

            String call;
            if (setters.isEmpty()) {
                call = method + ".Execute()";
            } else {
                // One step of the chain per line, as the SDK's sample writes it.
                StringBuilder chain = new StringBuilder("\n\t\t").append(method).append('.');
                for (String setter : setters) {
                    chain.append("\n\t\t").append(setter).append('.');
                }
                call = chain.append("\n\t\tExecute()").toString();
            }

            operation.vendorExtensions.put(CALL, call);
            if (body != null) {
                operation.vendorExtensions.put(BODY, body);
            }
            operation.vendorExtensions.put(OPENS_FILE, opensFile);
            if (opensFile) {
                imports.add("os");
            }
            operation.vendorExtensions.put(IMPORTS, new ArrayList<>(imports));
        }

        private static String setter(CodegenParameter parameter) {
            Object name = parameter.vendorExtensions.get("x-export-param-name");
            return name != null ? String.valueOf(name) : parameter.paramName;
        }

        /**
         * The request body. A model gets its required properties only, whatever the document states
         * for the body as a whole; an array or a scalar takes the example the document states for it.
         */
        private String body(Shape shape, Object example, String where) {
            CodegenModel model = shape.isArray ? null : model(shape);
            if (model != null && !model.isEnum) {
                return CallSnippets.isComposed(model) ? composed(model, null, where, 0) : construct(model, where, 0, true);
            }

            // A body parameter carries no item description, only the Go type.
            return shape.isArray && shape.items == null
                    ? typed(shape.dataType, example, where, 0)
                    : value(shape, example, "body", where, 0);
        }

        /**
         * The model built by its constructor, which takes every required property, in the order the
         * model declares them. The constructor returns a pointer, so the value is that pointer
         * dereferenced.
         */
        private String construct(CodegenModel model, String where, int depth, boolean top) {
            if (depth >= MAX_DEPTH) {
                return "*" + sdk + ".New" + model.classname + "WithDefaults()";
            }

            List<String> arguments = new ArrayList<>();
            if (model.requiredVars != null) {
                for (CodegenProperty property : model.requiredVars) {
                    Object example = CallSnippets.plain(SpecExamples.inheritedProperty(openAPI, model.name, property.baseName));
                    arguments.add(value(Shape.of(property), example, property.baseName,
                            where + "." + property.baseName, depth + 1));
                }
            }

            return "*" + sdk + ".New" + model.classname + "(" + (top ? list(arguments) : String.join(", ", arguments)) + ")";
        }

        /**
         * A oneOf or anyOf model: a struct with a pointer field per variant, named after the variant's
         * type. The variant set is the one the example fits, or the first one when there is no example.
         */
        private String composed(CodegenModel model, Object example, String where, int depth) {
            // A variant can be a oneOf of its own and lead back here, so the struct stops at the depth
            // the models do, with no variant set.
            if (depth >= MAX_DEPTH) {
                return qualify(model.classname) + "{}";
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

            String value = typed(chosen, example, where, depth + 1);

            return qualify(model.classname) + "{" + fieldName.apply(chosen) + ": " + pointer(chosen, value) + "}";
        }

        private boolean fits(String type, Object example) {
            if (example == null) {
                return false;
            }

            if (type.startsWith("[]")) {
                return example instanceof List;
            }

            switch (type) {
                case "string":
                    return example instanceof String;
                case "int":
                case "int32":
                case "int64":
                    return example instanceof Integer || example instanceof Long;
                case "float32":
                case "float64":
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
                    return model.isEnum ? CallSnippets.member(model.allowableValues, example) != null : example instanceof Map;
            }
        }

        /** A value of a type the generator names by its Go spelling only, as the variants of a oneOf. */
        private String typed(String type, Object example, String where, int depth) {
            if (type.startsWith("[]")) {
                String itemType = type.substring(2);

                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(element(itemType, typed(itemType, item, where, depth + 1)));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                }

                return qualify(type) + "{" + String.join(", ", items) + "}";
            }

            if (type.startsWith("map[")) {
                return qualify(type) + "{}";
            }

            String text = example == null ? null : SpecExamples.text(example);

            switch (type) {
                case "string":
                    if (text == null) {
                        placeholders.add(where);
                        return CallSnippets.quote("YOUR_VALUE");
                    }
                    return CallSnippets.quote(text);
                case "int":
                case "int32":
                case "int64":
                case "float32":
                case "float64":
                    return text == null ? "0" : text;
                case "bool":
                    return text == null ? "false" : text.toLowerCase(Locale.ROOT);
                case "time.Time":
                    return time(text, where);
                case "interface{}":
                    return text == null ? "nil" : literal(example, text);
                default:
                    CodegenModel model = models.get(type);
                    if (model == null) {
                        return "nil";
                    }
                    if (model.isEnum) {
                        Shape shape = new Shape();
                        shape.complexType = type;
                        return enumValue(shape, example, where);
                    }
                    return CallSnippets.isComposed(model) ? composed(model, example, where, depth) : construct(model, where, depth, false);
            }
        }

        private String value(Shape shape, Object example, String name, String where, int depth) {
            if (shape.isArray && shape.items != null && (shape.items.isFile || shape.items.isBinary)) {
                opensFile = true;
                return "[]*os.File{" + FILE + "}";
            }

            if (shape.isFile) {
                opensFile = true;
                return FILE;
            }

            // A nullable property is set through the SDK's wrapper, around a pointer to the value.
            String underlying = nullable(shape);
            if (underlying != null) {
                Shape inner = shape.copy();
                inner.dataType = underlying;
                // The value inside the wrapper is not wrapped again.
                inner.isNullable = false;
                String value = value(inner, example, name, where, depth);
                return "*" + sdk + ".New" + shape.dataType + "(" + pointer(underlying, value) + ")";
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
                        items.add(element(shape.items.dataType,
                                value(Shape.of(shape.items), item, name, where, depth + 1)));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                }

                return qualify(shape.dataType) + "{" + String.join(", ", items) + "}";
            }

            if (shape.isMap) {
                return qualify(shape.dataType) + "{}";
            }

            CodegenModel model = model(shape);
            if (model != null) {
                return CallSnippets.isComposed(model) ? composed(model, example, where, depth) : construct(model, where, depth, false);
            }

            if (shape.isDateTime && "time.Time".equals(shape.dataType)) {
                return time(example == null ? null : SpecExamples.text(example), where);
            }

            if (example == null) {
                placeholders.add(where);
                return placeholder(shape, name);
            }

            String text = SpecExamples.text(example);

            if (shape.isBoolean) {
                return text.toLowerCase(Locale.ROOT);
            }
            if (shape.isNumeric) {
                return text;
            }
            if ("interface{}".equals(shape.dataType)) {
                return literal(example, text);
            }

            return CallSnippets.quote(text);
        }

        /**
         * An enum member. An enum schema is a type of the SDK with a constant per member; an enum
         * declared in place is only its underlying type in Go, so the member is its value.
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

            Map<?, ?> chosen = CallSnippets.member(allowableValues, example);
            if (chosen == null) {
                placeholders.add(where);
                chosen = (Map<?, ?>) ((List<?>) values).get(0);
            }

            if (!schema) {
                return String.valueOf(chosen.get("value"));
            }

            String prefix = enumClassPrefix ? model.classname.toUpperCase(Locale.ROOT) + "_" : "";
            return sdk + "." + prefix + chosen.get("name");
        }

        private CodegenModel model(Shape shape) {
            if (shape.complexType != null && models.containsKey(shape.complexType)) {
                return models.get(shape.complexType);
            }

            return shape.isModel ? models.get(shape.dataType) : null;
        }

        /** The type a nullable wrapper holds, or null for a shape that is not wrapped. */
        private static String nullable(Shape shape) {
            if (!shape.isNullable || shape.isArray || shape.isMap || shape.dataType == null
                    || !shape.dataType.startsWith(NULLABLE)) {
                return null;
            }

            String known = NULLABLES.get(shape.dataType);
            return known != null ? known : shape.dataType.substring(NULLABLE.length());
        }

        /**
         * A pointer to a value of the given type: the constructor of a model already returns one, an
         * enum constant has a method for it, and the SDK has a helper for each primitive.
         */
        private String pointer(String type, String value) {
            if (value.startsWith("*")) {
                return value.substring(1);
            }

            CodegenModel model = models.get(type);
            if (model != null && model.isEnum) {
                return value + ".Ptr()";
            }

            String helper = POINTERS.get(type);
            if (helper != null) {
                return sdk + "." + helper + "(" + value + ")";
            }

            return "&" + value;
        }

        private String time(String text, String where) {
            imports.add("time");

            if (text != null) {
                try {
                    OffsetDateTime moment = OffsetDateTime.parse(text).withOffsetSameInstant(ZoneOffset.UTC);
                    return "time.Date(" + moment.getYear() + ", time." + MONTHS[moment.getMonthValue() - 1] + ", "
                            + moment.getDayOfMonth() + ", " + moment.getHour() + ", " + moment.getMinute() + ", "
                            + moment.getSecond() + ", " + moment.getNano() + ", time.UTC)";
                } catch (DateTimeParseException notADateTime) {
                    try {
                        LocalDate day = LocalDate.parse(text);
                        return "time.Date(" + day.getYear() + ", time." + MONTHS[day.getMonthValue() - 1] + ", "
                                + day.getDayOfMonth() + ", 0, 0, 0, 0, time.UTC)";
                    } catch (DateTimeParseException notADate) {
                        // Falls through to the placeholder.
                    }
                }
            }

            placeholders.add(where);
            return "time.Now()";
        }

        private static String literal(Object example, String text) {
            return example instanceof Number || example instanceof Boolean ? text.toLowerCase(Locale.ROOT) : CallSnippets.quote(text);
        }

        private static String placeholder(Shape shape, String name) {
            if (shape.isBoolean) {
                return "false";
            }
            if (shape.isNumeric) {
                return "0";
            }
            if ("string".equals(shape.dataType)) {
                return CallSnippets.quote(CallSnippets.placeholderName(name));
            }

            return "nil";
        }

        /**
         * An element of a slice literal. A struct literal leaves out its type there, which the slice
         * already states, as gofmt -s writes it.
         */
        private String element(String type, String value) {
            String spelled = qualify(type) + "{";
            return value.startsWith(spelled) ? value.substring(spelled.length() - 1) : value;
        }

        /** A Go type with the types of the SDK named through its package. */
        private String qualify(String type) {
            if (type.startsWith("[]")) {
                return "[]" + qualify(type.substring(2));
            }
            if (type.startsWith("*")) {
                return "*" + qualify(type.substring(1));
            }
            if (type.startsWith("map[string]")) {
                return "map[string]" + qualify(type.substring("map[string]".length()));
            }
            if (BUILTIN.contains(type) || type.contains(".")) {
                return type;
            }

            return sdk + "." + type;
        }

        /**
         * Arguments on one line, or one per line once they get long, with the trailing comma Go
         * requires there.
         */
        private static String list(List<String> items) {
            String line = String.join(", ", items);
            if (line.length() <= LINE_LENGTH && !line.contains("\n")) {
                return line;
            }

            StringBuilder builder = new StringBuilder();
            for (String item : items) {
                builder.append("\n\t\t").append(item).append(',');
            }

            return builder.append("\n\t").toString();
        }

        /** What the snippet needs to know about the type of a parameter or a property. */
        private static final class Shape {
            String dataType;
            String complexType;
            boolean isNumeric;
            boolean isBoolean;
            boolean isDateTime;
            boolean isEnum;
            boolean isArray;
            boolean isMap;
            boolean isModel;
            boolean isFile;
            boolean isNullable;
            Map<String, Object> allowableValues;
            CodegenProperty items;

            Shape copy() {
                Shape shape = new Shape();
                shape.dataType = dataType;
                shape.complexType = complexType;
                shape.isNumeric = isNumeric;
                shape.isBoolean = isBoolean;
                shape.isDateTime = isDateTime;
                shape.isEnum = isEnum;
                shape.isArray = isArray;
                shape.isMap = isMap;
                shape.isModel = isModel;
                shape.isFile = isFile;
                shape.isNullable = isNullable;
                shape.allowableValues = allowableValues;
                shape.items = items;
                return shape;
            }

            static Shape of(CodegenParameter parameter) {
                Shape shape = new Shape();
                shape.dataType = parameter.dataType;
                shape.complexType = parameter.baseType;
                shape.isNumeric = parameter.isNumeric || parameter.isInteger || parameter.isLong || parameter.isNumber
                        || parameter.isFloat || parameter.isDouble || parameter.isDecimal;
                shape.isBoolean = parameter.isBoolean;
                shape.isDateTime = parameter.isDateTime;
                shape.isEnum = parameter.isEnum;
                shape.isArray = parameter.isArray;
                shape.isMap = parameter.isMap;
                shape.isModel = parameter.isModel;
                shape.isFile = parameter.isFile || parameter.isBinary;
                shape.isNullable = parameter.isNullable;
                shape.allowableValues = parameter.allowableValues;
                shape.items = parameter.items;
                return shape;
            }

            static Shape of(CodegenProperty property) {
                Shape shape = new Shape();
                shape.dataType = property.dataType;
                shape.complexType = property.complexType;
                shape.isNumeric = property.isNumeric || property.isInteger || property.isLong || property.isNumber
                        || property.isFloat || property.isDouble || property.isDecimal;
                shape.isBoolean = property.isBoolean;
                shape.isDateTime = property.isDateTime;
                shape.isEnum = property.isEnum;
                shape.isArray = property.isArray;
                shape.isMap = property.isMap;
                shape.isModel = property.isModel;
                shape.isFile = property.isFile || property.isBinary;
                shape.isNullable = property.isNullable;
                shape.allowableValues = property.allowableValues;
                shape.items = property.items;
                return shape;
            }
        }
    }
}
