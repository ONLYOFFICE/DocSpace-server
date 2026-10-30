package com.example.codegen;

import java.io.File;
import java.net.URI;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.format.DateTimeParseException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Locale;
import java.util.Set;
import java.util.regex.Pattern;

import org.openapitools.codegen.CodegenModel;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.CodegenParameter;
import org.openapitools.codegen.CodegenProperty;
import org.openapitools.codegen.CodegenSecurity;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.languages.RubyClientCodegen;
import org.openapitools.codegen.model.ApiInfoMap;
import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.model.OperationsMap;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;
import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.headers.*;

import static org.openapitools.codegen.utils.StringUtils.camelize;

public class MyRubyClientCodegen extends RubyClientCodegen {

    private static class TagParts {
        final String originalTag;
        final String folderPart;
        final String classPart;

        TagParts(String originalTag, String folderPart, String classPart) {
            this.originalTag = originalTag;
            this.folderPart = folderPart;
            this.classPart = classPart;
        }
    }

    protected String apiDocPath = "docs/";
    protected String modelDocPath = "docs/";

    private final Map<String, TagParts> tagMap = new HashMap<>();
    private final Map<String, TagParts> generatedApiClassNameToTagParts = new HashMap<>();
    private final Map<String, String> seenApiFilenames = new HashMap<>();

    /**
     * The call snippets of the reference pages instead of the SDK: one file, with a snippet per
     * operation, written into the folder the run was given with -o.
     */
    private boolean snippetsOnly;

    private Snippets snippets;

    private static final Map<String, String> RUBY_SUPPORTING_FILE_NAMES = Map.of(
        "api_client.rb", "api-client.rb",
        "api_error.rb", "api-error.rb",
        "api_model_base.rb", "api-model-base.rb"
    );

    public MyRubyClientCodegen() {
        super();

        this.templateDir = "templates/ruby";
        this.embeddedTemplateDir = "ruby-client";

        additionalProperties.put("apiDocPath", apiDocPath);
        additionalProperties.put("modelDocPath", modelDocPath);
    }

    @Override
    public String getName() {
        return "my-ruby";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Ruby client";
    }

    @Override
    public void processOpts() {
        super.processOpts();

        // Returns before the output folder is pointed at the SDK checkout (see CallSnippets.configure).
        snippetsOnly = CallSnippets.configure(this);
        if (snippetsOnly) {
            // The SDK takes the portal's address as the scheme and the host apart, as its sample sets them.
            URI server = URI.create(String.valueOf(additionalProperties.get("snippetsServerUrl")));
            additionalProperties.put("snippetsScheme", server.getScheme());
            additionalProperties.put("snippetsHost", server.getRawAuthority());
            if (server.getRawPath() != null && !server.getRawPath().isEmpty() && !"/".equals(server.getRawPath())) {
                additionalProperties.put("snippetsBasePath", server.getRawPath());
            }
            return;
        }

        this.outputFolder = "../../../../../sdk/docspace-api-sdk-ruby";

        supportingFiles.removeIf(file ->
            "README.md".equals(file.getDestinationFilename())
                && (file.getFolder() == null || file.getFolder().isEmpty())
        );
        supportingFiles.add(new SupportingFile("README.mustache", "", "README.md"));
        String sampleDir = "samples" + File.separator + "docspace-api-sdk-ruby-sample";
        supportingFiles.add(new SupportingFile("sample_main.mustache", sampleDir, "main.rb"));
        supportingFiles.add(new SupportingFile("sample_Gemfile.mustache", sampleDir, "Gemfile"));

        supportingFiles.add(new SupportingFile(
            "AUTHORS.mustache", "", "AUTHORS.md"
        ));

        supportingFiles.add(new SupportingFile(
            "LICENSE.mustache", "", "LICENSE"
        ));

        supportingFiles.add(new SupportingFile(
            "CHANGELOG.mustache", "", "CHANGELOG.md"
        ));

        supportingFiles.removeIf(f -> f.getTemplateFile().equals("git_push.sh.mustache"));

        apiTestTemplateFiles.clear();
        modelTestTemplateFiles.clear();

        supportingFiles.removeIf(file ->
            "spec_helper.rb".equals(file.getDestinationFilename())
                && "spec".equals(file.getFolder())
        );

        List<SupportingFile> rewritten = new ArrayList<>();
        for (SupportingFile file : supportingFiles) {
            String oldName = file.getDestinationFilename();
            String newName = RUBY_SUPPORTING_FILE_NAMES.getOrDefault(oldName, oldName);

            SupportingFile replacement;
            if (file.getFolder() != null) {
                replacement = new SupportingFile(
                    file.getTemplateFile(),
                    file.getFolder(),
                    newName
                );
            } else {
                replacement = new SupportingFile(
                    file.getTemplateFile(),
                    newName
                );
            }

            rewritten.add(replacement);
        }


        supportingFiles.clear();
        supportingFiles.addAll(rewritten);

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
    }

    @Override
    public String apiFileFolder() {
        return outputFolder + File.separator + "lib" + File.separator + gemName + File.separator + "api";
    }

    @Override
    public String modelFileFolder() {
        return outputFolder + File.separator + "lib" + File.separator + gemName + File.separator + "models";
    }

    @Override
    public String sanitizeTag(String tag) {
        String sanitized = super.sanitizeTag(tag);

        if (!tagMap.containsKey(sanitized)) {
            String[] parts = tag.split(" / ", 2);
            String folderPart = parts[0];
            String classPart = (parts.length > 1) ? parts[1] : parts[0];

            String folderPartSanitized = camelize(sanitizeName(folderPart));
            String classPartSanitized = camelize(sanitizeName(classPart));
            String finalClassPartSanitized = classPartSanitized;
            if (finalClassPartSanitized.startsWith(folderPartSanitized)
                && finalClassPartSanitized.length() > folderPartSanitized.length()) {
                finalClassPartSanitized = finalClassPartSanitized.substring(folderPartSanitized.length());
            }

            TagParts info = new TagParts(
                tag,
                folderPartSanitized,
                finalClassPartSanitized
            );

            tagMap.put(sanitized, info);
            generatedApiClassNameToTagParts.put(sanitized, info);
            generatedApiClassNameToTagParts.put(camelize(sanitized), info);
        }

        return sanitized;
    }

    @Override
    public String toApiFilename(String name) {
        return super.toApiFilename(name).replace('_', '-');
    }

    @Override
    public String toModelFilename(String name) {
        return super.toModelFilename(name).replace('_', '-');
    }

    @Override
    public String apiFilename(String templateName, String tag) {
        String sanitizedTag = sanitizeTag(tag);
        String suffix = apiTemplateFiles().get(templateName);

        String uniqueTag = makeUniqueTag(sanitizedTag);
        TagParts tagParts = tagMap.get(sanitizedTag);

        if (tagParts == null) {
            return apiFileFolder() + File.separator + toApiFilename(uniqueTag) + suffix;
        }

        String folderPath = apiFileFolder() + File.separator + tagParts.folderPart;
        String filename = toApiFilename(tagParts.classPart) + suffix;

        return folderPath + File.separator + filename;
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
        // Ruby is dynamic, so the twin stays attached: one method, [Integer, String] in the docs, and
        // the response model picked at run time by the id's class (see ThirdPartyVariants.markUnions).
        ThirdPartyVariants.addAttachedImports(this, objs);

        super.postProcessOperationsWithModels(objs, allModels);
        for (CodegenOperation op : objs.getOperations().getOperation()) {
            ThirdPartyVariants.markUnions(op, (a, b) -> a + ", " + b);
        }

        if (objs == null || objs.getOperations() == null) {
            return objs;
        }
        
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
                            .replace("\n", "\n# ");
                        op.vendorExtensions.put("x-commentedNotes", commentedNotes);
                    }
                }
            }

        TagParts tagParts = resolveTagParts(operationMap.getClassname());
        if (tagParts == null) {
            return objs;
        }

        operationMap.put("x-folder", tagParts.folderPart);
        operationMap.put("x-classname", tagParts.classPart + apiNameSuffix);

        if (snippetsOnly) {
            attachSnippets(objs, allModels);
        }

        return objs;
    }

    /**
     * Gives every operation what its call snippet needs. The third-party twin of an operation
     * (see ThirdPartyVariants) gets none: it is the same method, on the same page.
     */
    private void attachSnippets(OperationsMap objs, List<ModelMap> allModels) {
        if (snippets == null) {
            snippets = new Snippets(openAPI, allModels, moduleName);
        }

        CallSnippets.attach(this, objs, snippets::attach);
    }

    private String toDashCase(String input) {
        return input.replaceAll("([a-z])([A-Z])", "$1-$2")
                    .replaceAll("([A-Z]+)([A-Z][a-z])", "$1-$2")
                    .replace('_', '-')
                    .toLowerCase(Locale.ROOT);
    }

    @Override
    public Map<String, Object> postProcessSupportingFileData(Map<String, Object> objs) {
        super.postProcessSupportingFileData(objs);

        Object apiInfoObj = objs.get("apiInfo");
        if (!(apiInfoObj instanceof ApiInfoMap)) {
            return objs;
        }
        
        ApiInfoMap apiInfo = (ApiInfoMap) apiInfoObj;

        if (apiInfo.getApis() == null) {
            return objs;
        }

        Map<String, List<Map<String, Object>>> folderToApis = new LinkedHashMap<>();
        for (OperationsMap api : apiInfo.getApis()) {
            OperationMap operationMap = api.getOperations();
            if (operationMap == null) {
                continue;
            }

            TagParts tagParts = resolveTagParts(operationMap.getClassname());
            if (tagParts == null) {
                continue;
            }

            String folder = tagParts.folderPart;
            String displayClassName = tagParts.classPart + apiNameSuffix;
            String apiRequirePath = gemName + "/api/" + folder + "/" + toApiFilename(tagParts.classPart);

            api.put("x-folder", folder);
            api.put("x-classname", displayClassName);
            api.put("importPath", apiRequirePath);

            folderToApis.computeIfAbsent(folder, key -> new ArrayList<>()).add(api);
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

    private TagParts resolveTagParts(String classNameWithSuffix) {
        if (classNameWithSuffix == null) {
            return null;
        }

        String className = classNameWithSuffix;
        if (className.endsWith(apiNameSuffix)) {
            className = className.substring(0, className.length() - apiNameSuffix.length());
        }

        TagParts tagParts = generatedApiClassNameToTagParts.get(className);
        if (tagParts == null) {
            tagParts = generatedApiClassNameToTagParts.get(camelize(className));
        }
        if (tagParts == null) {
            tagParts = tagMap.get(super.sanitizeTag(className));
        }

        return tagParts;
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

    /**
     * Builds what the call snippet of one operation needs beyond the template, as Ruby: the arguments
     * of the call, the request body, and whether the operation takes the token.
     * <p>
     * The body is the minimal one, as for C# (see {@link MyCSharpClientCodegen}): a model gets its
     * required fields only, as the attributes it is built with, with the values the document states for
     * them. A required parameter is an argument of the method, in its place; an optional one goes into
     * the hash of options the method ends with, and only a body or a file is shown there.
     */
    private static final class Snippets {

        static final String ARGUMENTS = "x-snippet-arguments";
        static final String BODY = "x-snippet-body";
        static final String BEARER = "x-snippet-bearer";

        /** Beyond this length the arguments are put one per line, and so are the attributes of the body. */
        private static final int LINE_LENGTH = 60;

        private static final int MAX_DEPTH = 4;

        /** An argument or an attribute on a line of its own, inside a statement of the script. */
        private static final String INDENT = "\n  ";

        private static final String FILE = "File.new('file.docx')";

        private static final String NIL_UUID = "00000000-0000-0000-0000-000000000000";

        private static final Pattern UUID_TEXT =
                Pattern.compile("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

        /** A key Ruby writes as a bare symbol: title in title: 'Project Alpha'. */
        private static final Pattern SYMBOL = Pattern.compile("[A-Za-z_][A-Za-z0-9_]*");

        /** A name Ruby reads as a constant; any other member name of an enum is only a local of its class. */
        private static final Pattern CONSTANT = Pattern.compile("[A-Z][A-Za-z0-9_]*");

        private final OpenAPI openAPI;
        private final String moduleName;
        private final Map<String, CodegenModel> models;

        /** Values the document states no example for, as "operationId: name", for the run's log. */
        private final List<String> placeholders = new ArrayList<>();

        Snippets(OpenAPI openAPI, List<ModelMap> allModels, String moduleName) {
            this.openAPI = openAPI;
            this.moduleName = moduleName;
            this.models = CallSnippets.models(allModels);
        }

        List<String> placeholders() {
            return placeholders;
        }

        void attach(CodegenOperation operation) {
            List<String> arguments = new ArrayList<>();
            List<String> options = new ArrayList<>();
            String body = null;

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;
                String argument;

                if (parameter.isBodyParam) {
                    body = body(operation, Shape.of(parameter), CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);
                    argument = "body";
                } else {
                    if (!parameter.required && !parameter.isFile && !parameter.isBinary) {
                        continue;
                    }

                    Object example = CallSnippets.plain(parameter.isFormParam
                            ? SpecExamples.formField(openAPI, operation, parameter.baseName)
                            : SpecExamples.parameter(openAPI, operation, parameter.baseName, SpecExamples.location(parameter)));

                    argument = value(Shape.of(parameter), example, parameter.baseName, where, 0);
                }

                if (parameter.required) {
                    arguments.add(argument);
                } else {
                    options.add(key(parameter.paramName) + argument);
                }
            }

            arguments.addAll(options);
            operation.vendorExtensions.put(ARGUMENTS, arguments(arguments));
            if (body != null) {
                operation.vendorExtensions.put(BODY, body);
            }

            if (takesToken(operation)) {
                operation.vendorExtensions.put(BEARER, true);
            }
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
         * The request body. A model gets its required fields only, whatever the document states for
         * the body as a whole; an array or a scalar takes the example the document states for it.
         */
        private String body(CodegenOperation operation, Shape shape, Object example, String where) {
            CodegenModel model = shape.isArray ? null : model(shape);

            // A body that is an enum schema reaches the SDK as a string: a member, chosen as for any other enum.
            if (model == null && "String".equals(shape.dataType)) {
                CodegenModel enumBody = CallSnippets.enumBody(openAPI, operation, models);
                if (enumBody != null) {
                    return enumMember(enumBody, example, where);
                }
            }

            if (model == null || model.isEnum || CallSnippets.isComposed(model)) {
                return value(shape, example, "body", where, 0);
            }

            String type = moduleName + "::" + model.classname;
            List<String> attributes = attributes(model, where, 0);
            if (attributes.isEmpty()) {
                return type + ".new";
            }

            String line = String.join(", ", attributes);
            return line.length() <= LINE_LENGTH
                    ? type + ".new(" + line + ")"
                    : type + ".new(" + INDENT + String.join("," + INDENT, attributes) + "\n)";
        }

        private String construct(CodegenModel model, String where, int depth) {
            String type = moduleName + "::" + model.classname;
            List<String> attributes = attributes(model, where, depth);

            // A model can contain itself; past this depth it is not built, and the place is left to the reader.
            if (attributes == null) {
                placeholders.add(where);
                return type + ".new";
            }

            return attributes.isEmpty() ? type + ".new" : type + ".new(" + String.join(", ", attributes) + ")";
        }

        /**
         * The model's required fields, as the attributes it is built with; null past the depth a model
         * with required fields is built to. A model composed with allOf has the fields of its parent too.
         */
        private List<String> attributes(CodegenModel model, String where, int depth) {
            List<String> attributes = new ArrayList<>();
            if (model.allVars == null) {
                return attributes;
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
                attributes.add(key(property.name) + value(Shape.of(property), example,
                        property.baseName, where + "." + property.baseName, depth + 1));
            }

            return attributes;
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
                        items.add(value(Shape.of(shape.items), CallSnippets.plain(item), name, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    // A list the document requires items in gets one, standing in for them: an empty
                    // one would not make a valid request.
                    if (shape.minItems != null && shape.minItems > 0) {
                        items.add(value(Shape.of(shape.items), null, name, where, depth + 1));
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
                return example instanceof Map ? mixed(example) : "{}";
            }

            CodegenModel model = model(shape);
            if (model != null) {
                return CallSnippets.isComposed(model) ? composed(model, example, name, where, depth) : construct(model, where, depth);
            }

            return scalar(shape.dataType, shape.isUuid, example, name, where);
        }

        /**
         * A map, as the hash the SDK takes it as, with the entries of the example; one the document
         * requires entries in and gives no example for gets one standing in for them.
         */
        private String map(Shape shape, Object example, String name, String where, int depth) {
            List<String> entries = new ArrayList<>();
            if (example instanceof Map && shape.items != null) {
                for (Map.Entry<?, ?> entry : ((Map<?, ?>) example).entrySet()) {
                    entries.add(quote(String.valueOf(entry.getKey())) + " => "
                            + value(Shape.of(shape.items), CallSnippets.plain(entry.getValue()), name, where, depth + 1));
                }
            }

            if (entries.isEmpty()) {
                if (shape.minProperties == null || shape.minProperties == 0 || shape.items == null) {
                    return "{}";
                }

                int logged = placeholders.size();
                entries.add(quote("YOUR_KEY") + " => " + value(Shape.of(shape.items), null, name, where, depth + 1));
                if (placeholders.size() == logged) {
                    placeholders.add(where);
                }
            }

            return "{ " + String.join(", ", entries) + " }";
        }

        /**
         * A value of a oneOf or anyOf. The SDK's module for it only reads an answer back into one of its
         * variants, and a request takes the variant itself, so the snippet gives the one
         * {@link CallSnippets#variant} picks.
         */
        private String composed(CodegenModel model, Object example, String name, String where, int depth) {
            // A variant can be a oneOf of its own and lead back here, so it stops at the depth the models do.
            if (depth >= MAX_DEPTH) {
                placeholders.add(where);
                return "nil";
            }

            CallSnippets.Choice<String> choice = CallSnippets.variant(openAPI, model, example, this::fits);

            return typed(choice.variant, choice.example, name, where, depth + 1);
        }

        private boolean fits(String type, Object example) {
            if (example == null) {
                return false;
            }

            if (type.startsWith("Array<")) {
                return example instanceof List;
            }

            if (type.startsWith("Hash<")) {
                return example instanceof Map;
            }

            switch (type) {
                case "String":
                    return example instanceof String;
                case "Integer":
                    return example instanceof Integer || example instanceof Long;
                case "Float":
                    return example instanceof Number;
                case "Boolean":
                    return example instanceof Boolean;
                case "Object":
                    return example instanceof Map;
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

        /** A value of a type the generator names by its Ruby spelling only, as the variants of a oneOf. */
        private String typed(String type, Object example, String name, String where, int depth) {
            if (type.startsWith("Array<") && type.endsWith(">")) {
                String itemType = type.substring("Array<".length(), type.length() - 1);

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

            if (type.startsWith("Hash<")) {
                return example instanceof Map ? mixed(example) : "{}";
            }

            CodegenModel model = models.get(type);
            if (model == null) {
                return scalar(type, false, example, name, where);
            }

            if (model.isEnum) {
                return enumMember(model, example, where);
            }

            return CallSnippets.isComposed(model) ? composed(model, example, name, where, depth) : construct(model, where, depth);
        }

        /**
         * An enum member. An enum schema is a class of constants in the SDK; an enum declared in place,
         * in a model or in a parameter, is only the values its setter or its method accepts.
         */
        private String enumValue(Shape shape, Object example, String where) {
            // An array or a map of enum values is built a value at a time.
            if (shape.isArray || shape.isMap) {
                return null;
            }

            CodegenModel model = model(shape);
            if (model != null && model.isEnum) {
                return enumMember(model, example, where);
            }

            Object values = shape.allowableValues == null ? null : shape.allowableValues.get("enumVars");
            if (!shape.isEnum || !(values instanceof List) || ((List<?>) values).isEmpty()) {
                return null;
            }

            return literal(String.valueOf(member(shape.allowableValues, example, where).get("value")));
        }

        /**
         * A member of an enum schema, as the constant of its class. A member whose name Ruby does not
         * read as a constant, kDrive or linked, is only a local of the class body, so it is its value.
         */
        private String enumMember(CodegenModel model, Object example, String where) {
            Map<?, ?> chosen = member(model.allowableValues, example, where);
            String name = String.valueOf(chosen.get("name"));

            return CONSTANT.matcher(name).matches()
                    ? moduleName + "::" + model.classname + "::" + name
                    : literal(String.valueOf(chosen.get("value")));
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

        /**
         * The value of an enum member as the snippet writes it. The generator writes the value of a
         * string enum as a literal in double quotes; the snippet writes its strings as the SDK's sample does.
         */
        private static String literal(String value) {
            if (value.length() >= 2 && value.startsWith("\"") && value.endsWith("\"")) {
                return quote(CallSnippets.unquote(value).replace("\\\"", "\"").replace("\\\\", "\\"));
            }

            return value;
        }

        /** An example of a value of any type, as the Ruby literal of what it holds. */
        private static String mixed(Object example) {
            if (example == null) {
                return "nil";
            }
            if (example instanceof String) {
                return quote((String) example);
            }
            if (example instanceof List) {
                List<String> items = new ArrayList<>();
                for (Object item : (List<?>) example) {
                    items.add(mixed(CallSnippets.plain(item)));
                }
                return "[" + String.join(", ", items) + "]";
            }
            if (example instanceof Map) {
                List<String> entries = new ArrayList<>();
                for (Map.Entry<?, ?> entry : ((Map<?, ?>) example).entrySet()) {
                    entries.add(quote(String.valueOf(entry.getKey())) + " => "
                            + mixed(CallSnippets.plain(entry.getValue())));
                }
                return entries.isEmpty() ? "{}" : "{ " + String.join(", ", entries) + " }";
            }

            return SpecExamples.text(example);
        }

        private CodegenModel model(Shape shape) {
            CodegenModel model = shape.complexType == null ? null : models.get(shape.complexType);
            if (model == null && !shape.isArray && shape.dataType != null) {
                model = models.get(shape.dataType);
            }

            return model;
        }

        /** A value of a plain type, as the Ruby literal of the type it has in the SDK. */
        private String scalar(String type, boolean uuid, Object example, String name, String where) {
            String text = example == null ? null : SpecExamples.text(example);

            switch (type == null ? "" : type) {
                case "Time":
                    return time(text, where);
                case "Date":
                    return date(text, where);
                case "File":
                    return FILE;
                case "Object":
                    if (example != null) {
                        return mixed(example);
                    }
                    placeholders.add(where);
                    return "{}";
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
                case "String":
                    return quote(text);
                case "Integer":
                case "Float":
                    return text;
                case "Boolean":
                    return text.toLowerCase(Locale.ROOT);
                default:
                    return "nil";
            }
        }

        private static String placeholder(String type, String name) {
            switch (type == null ? "" : type) {
                case "String":
                    return quote(CallSnippets.placeholderName(name));
                case "Integer":
                    return "0";
                case "Float":
                    return "0.0";
                case "Boolean":
                    return "false";
                default:
                    return "nil";
            }
        }

        /** A date and time, as the SDK's type for it, Time, parsed from the text the example states. */
        private String time(String text, String where) {
            if (text != null) {
                try {
                    OffsetDateTime.parse(text);
                    return "Time.parse(" + quote(text) + ")";
                } catch (DateTimeParseException notADateTime) {
                    // Falls through to the placeholder.
                }
            }

            placeholders.add(where);
            return "Time.now";
        }

        private String date(String text, String where) {
            if (text != null) {
                try {
                    LocalDate.parse(text);
                    return "Date.parse(" + quote(text) + ")";
                } catch (DateTimeParseException notADate) {
                    // Falls through to the placeholder.
                }
            }

            placeholders.add(where);
            return "Date.today";
        }

        private String uuid(String text, String where) {
            // The SDK takes a uuid as a string and sends it as it is, so an example such as "1" counts as none.
            if (text == null || !UUID_TEXT.matcher(text).matches()) {
                placeholders.add(where);
                return quote(NIL_UUID);
            }

            return quote(text);
        }

        /** The arguments in parentheses, on one line or one per line once they get long; none, no parentheses. */
        private static String arguments(List<String> arguments) {
            if (arguments.isEmpty()) {
                return "";
            }

            String line = String.join(", ", arguments);
            if (line.length() <= LINE_LENGTH) {
                return "(" + line + ")";
            }

            return "(" + INDENT + String.join("," + INDENT, arguments) + "\n)";
        }

        /** A key of a hash of attributes or options: a bare symbol where Ruby allows one. */
        private static String key(String name) {
            return (SYMBOL.matcher(name).matches() ? name : quote(name)) + ": ";
        }

        /**
         * A string literal in single quotes, as the SDK's sample writes them, which keep everything but a
         * backslash and the quote as it is. A string with a line break or a tab takes double quotes, which
         * also read #{ as the start of code, so a number sign is escaped there.
         */
        private static String quote(String text) {
            if (text.indexOf('\r') < 0 && text.indexOf('\n') < 0 && text.indexOf('\t') < 0) {
                return "'" + text.replace("\\", "\\\\").replace("'", "\\'") + "'";
            }

            return "\"" + text
                    .replace("\\", "\\\\")
                    .replace("\"", "\\\"")
                    .replace("#", "\\#")
                    .replace("\r", "\\r")
                    .replace("\n", "\\n")
                    .replace("\t", "\\t") + "\"";
        }

        /** What the snippet needs to know about the type of a parameter or a property. */
        private static final class Shape {
            String dataType;
            String complexType;
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

            static Shape of(CodegenProperty property) {
                Shape shape = new Shape();
                shape.dataType = property.dataType;
                shape.complexType = property.complexType;
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
        }
    }
}
