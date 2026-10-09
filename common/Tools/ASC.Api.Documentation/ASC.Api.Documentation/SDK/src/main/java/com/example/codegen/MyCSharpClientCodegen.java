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

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.servers.*;
import io.swagger.v3.oas.models.headers.*;
import io.swagger.v3.oas.models.media.Schema;
import io.swagger.v3.core.util.Json;

import com.fasterxml.jackson.core.JsonProcessingException;

import org.openapitools.codegen.model.*;
import org.openapitools.codegen.languages.CSharpClientCodegen;
import org.openapitools.codegen.*;
import static org.openapitools.codegen.utils.StringUtils.camelize;
import org.openapitools.codegen.utils.ModelUtils;
import org.openapitools.codegen.templating.mustache.ReplaceAllLambda;

import com.google.common.collect.ImmutableMap;
import com.samskivert.mustache.Mustache;
import com.samskivert.mustache.Template;
import java.io.IOException;
import java.io.Writer;

import java.io.File;
import java.util.function.UnaryOperator;
import java.util.stream.Collectors;
import java.util.Map.Entry;
import java.util.*;

public class MyCSharpClientCodegen extends CSharpClientCodegen {
    
    protected String apiNamePrefix = "", apiNameSuffix = "Api";

    public MyCSharpClientCodegen() {
        super();
        this.templateDir = "templates/csharp";
        this.embeddedTemplateDir = "csharp";
    }

    /**
     * The call snippets of the reference pages instead of the SDK: one file, with a snippet per
     * operation, written into the folder the run was given with -o.
     */
    private boolean snippetsOnly;

    private Snippets snippets;

    @Override
    public void processOpts() {
        super.processOpts();

        // Returns before the output folder is pointed at the SDK checkout (see CallSnippets.configure).
        snippetsOnly = CallSnippets.configure(this);
        if (snippetsOnly) {
            return;
        }

        this.outputFolder = "../../../../../sdk/docspace-api-sdk-csharp";

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
            f.getTemplateFile().equals("appveyor.mustache") ||
            f.getDestinationFilename().equals(".openapi-generator-ignore")
        );

        supportingFiles.add(new SupportingFile(
            "Program.mustache", "samples" + File.separator + packageName + ".Example", "Program.cs"
        ));

        supportingFiles.add(new SupportingFile(
            "ExampleProject.mustache", "samples" + File.separator + packageName + ".Example", packageName + ".Example.csproj"
        ));

        supportingFiles.add(new SupportingFile(
            "AUTHORS.mustache", "", "AUTHORS.md"
        ));

        supportingFiles.add(new SupportingFile(
            "LICENSE.mustache", "", "LICENSE"
        ));

        supportingFiles.add(new SupportingFile(
            "CHANGELOG.mustache", "", "CHANGELOG.md"
        ));

        supportingFiles.add(new SupportingFile(
            "README_nuget.mustache", "docs", "README_nuget.md"
        ));

        supportingFiles.add(new SupportingFile(
            "GlobalUsings.mustache", sourceFolder + File.separator + packageName, "GlobalUsings.cs"
        ));
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
            model.readWriteVars = model.vars.stream()
            .filter(v -> !v.isReadOnly)
            .collect(Collectors.toList());
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
                model.getVendorExtensions().put("x-has-localVars", !localVars.isEmpty());
            }
        }
        

        return objs;
    }

    // A generic controller action exists twice on the server, on one and the same route: closed over int for
    // an entry the portal stores itself and over string for an entry on a connected third-party account. The
    // document carries the string shape as `x-thirdparty-variant`; ThirdPartyVariants turns it into a second
    // operation under the same name, which the class template renders as one more overload:
    // `GetFileInfo(int fileId)` returns FileWrapper, `GetFileInfo(string fileId)` returns ThirdPartyFileWrapper.
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
            // The name a property takes as a parameter of its model's constructor, spelled by the
            // same lambda the model template spells it with.
            Mustache.Lambda lambda = addMustacheLambdas().build().get("camelcase_sanitize_param");
            Template template = Mustache.compiler().escapeHTML(false).compile("{{#lambda}}{{name}}{{/lambda}}");

            snippets = new Snippets(openAPI, allModels, name -> template.execute(Map.of("lambda", lambda, "name", name)));
        }

        CallSnippets.attach(objs, snippets::attach);
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

                    if (op.allParams != null) {
                        for (CodegenParameter param : op.allParams) {
                            if (!param.isPrimitiveType)
                            {
                                String typeName = param.dataType.replace("?", "");
                                param.vendorExtensions.put("x-mdModelName", typeName);

                                if (param.description == null || param.description.isEmpty()) {
                                    for (ModelMap modelMap : allModels) {
                                        CodegenModel model = modelMap.getModel();
                                        if (model.classname.equals(typeName)) {
                                            if (model.description != null && !model.description.isEmpty()) {
                                                param.description = model.description;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                
                            if (Boolean.TRUE.equals(param.isDeepObject) && param.items != null && param.items.vars != null) {
                                for (CodegenProperty itemVar : param.items.vars) {
                                    boolean isValue = (itemVar.isNumeric
                                            || itemVar.isBoolean
                                            || itemVar.isDate
                                            || itemVar.isDateTime
                                            || itemVar.isUuid
                                            || itemVar.isEnum)
                                            && !itemVar.isNullable;

                                    if (isValue) {
                                        itemVar.vendorExtensions.put("x-is-value", true);
                                    }
                                }
                            }
                        }
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
    public String escapeReservedWord(String name) {
        if (isReservedWord(name) || name.matches("^\\d.*")) {
            return "@" + name;
        }
        return name;
    }

    @Override
    protected ImmutableMap.Builder<String, Mustache.Lambda> addMustacheLambdas() {
        return super.addMustacheLambdas()
            .put("unescape_param", new ReplaceAllLambda("^@", ""))
            .put("xml_doc_text", new XmlDocTextLambda());
    }

    // Makes a free-form value safe to place inside an XML documentation comment: escapes only the
    // three characters XML requires (so JSON quotes stay readable) and folds line breaks, which
    // would otherwise spill out of the `///` comment.
    private static class XmlDocTextLambda implements Mustache.Lambda {
        @Override
        public void execute(Template.Fragment fragment, Writer writer) throws IOException {
            String text = fragment.execute()
                .replace("&", "&amp;")
                .replace("<", "&lt;")
                .replace(">", "&gt;")
                .replaceAll("\\R+", " ");

            writer.write(text);
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

        String appName = (String) additionalProperties.get("appName");
        if (appName != null) {
            objs.put("appName", appName.toUpperCase());
        }

        return objs;
    }

    private String toDashCase(String input) {
        return input.replaceAll("([a-z])([A-Z])", "$1-$2")
                    .replaceAll("([A-Z]+)([A-Z][a-z])", "$1-$2")
                    .toLowerCase();
    }

    // In an openapi 3.1 document the parser hands structured `example` values over as plain Java
    // collections instead of Jackson nodes, so the default String.valueOf() rendering produces
    // `[{id=conn1, ip=192.168.1.1}]` instead of JSON. Re-serialize them so the <example> tags stay
    // copy-pasteable.
    @Override
    public CodegenProperty fromProperty(String name, Schema p, boolean required, boolean schemaIsFromAdditionalProperties) {
        CodegenProperty property = super.fromProperty(name, p, required, schemaIsFromAdditionalProperties);

        if (p != null) {
            String json = toJsonExample(p.getExample());
            if (json != null) {
                property.example = json;
            }
        }

        return property;
    }

    private static String toJsonExample(Object example) {
        if (!(example instanceof Map) && !(example instanceof List)) {
            return null;
        }

        try {
            return Json.mapper().writeValueAsString(example);
        } catch (JsonProcessingException e) {
            return null;
        }
    }
    
    @Override
    public Map<String, ModelsMap> postProcessAllModels(Map<String, ModelsMap> objs) {
        Map<String, ModelsMap> processed = super.postProcessAllModels(objs);

        Map<String, CodegenModel> byName = new HashMap<>();
        for (ModelsMap mm : processed.values()) {
            for (ModelMap m : mm.getModels()) {
                if (m.getModel() != null) {
                    byName.put(m.getModel().classname, m.getModel());
                }
            }
        }

        Map<String, String> parentOf = new HashMap<>();
        for (CodegenModel model : byName.values()) {
            Schema<?> schema = model.schemaName == null
                ? null
                : openAPI.getComponents().getSchemas().get(model.schemaName);
            if (schema != null && ModelUtils.isAllOf(schema)) {
                for (Object obj : schema.getAllOf()) {
                    if (obj instanceof Schema && ((Schema<?>) obj).get$ref() != null) {
                        parentOf.put(model.classname,
                            toModelName(ModelUtils.getSimpleRef(((Schema<?>) obj).get$ref())));
                        break;
                    }
                }
            }
        }

        for (CodegenModel model : byName.values()) {
            if (!model.isAdditionalPropertiesTrue) {
                continue;
            }
            String parent = parentOf.get(model.classname);
            while (parent != null) {
                CodegenModel ancestor = byName.get(parent);
                if (ancestor != null && ancestor.isAdditionalPropertiesTrue) {
                    model.vendorExtensions.put("x-hides-additional-properties", true);
                    break;
                }
                parent = parentOf.get(parent);
            }
        }

        return processed;
    }

    @Override
    public String getName() {
        return "my-csharp";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Csharp client";
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

    /**
     * Builds what the call snippet of one operation needs beyond the template: the arguments of the
     * call and the request body, as C# expressions.
     * <p>
     * The body is the minimal one: a model gets its required properties only, with the values the
     * document states for them. Optional parameters are left out of the call, because the methods of
     * the SDK default them.
     */
    private static final class Snippets {

        static final String ARGUMENTS = "x-snippet-arguments";
        static final String BODY = "x-snippet-body";
        static final String USES_MODEL = "x-snippet-uses-model";

        /** Beyond this length the arguments are put one per line. */
        private static final int LINE_LENGTH = 60;

        private static final int MAX_DEPTH = 4;

        private static final String FILE = "new FileParameter(\"file.docx\", File.OpenRead(\"file.docx\"))";

        private final OpenAPI openAPI;
        private final Map<String, CodegenModel> models;
        private final UnaryOperator<String> constructorParameter;

        /** Values the document states no example for, as "operationId: name", for the run's log. */
        private final List<String> placeholders = new ArrayList<>();

        private boolean usesModel;

        Snippets(OpenAPI openAPI, List<ModelMap> allModels, UnaryOperator<String> constructorParameter) {
            this.openAPI = openAPI;
            this.constructorParameter = constructorParameter;
            this.models = CallSnippets.models(allModels);
        }

        List<String> placeholders() {
            return placeholders;
        }

        void attach(CodegenOperation operation) {
            usesModel = false;

            List<String> arguments = new ArrayList<>();
            String body = null;

            for (CodegenParameter parameter : operation.allParams) {
                String where = operation.operationIdOriginal + ": " + parameter.baseName;

                if (parameter.isBodyParam) {
                    body = body(Shape.of(parameter), CallSnippets.plain(SpecExamples.requestBody(openAPI, operation)), where);
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

            operation.vendorExtensions.put(ARGUMENTS, list(arguments));
            if (body != null) {
                operation.vendorExtensions.put(BODY, body);
            }
            operation.vendorExtensions.put(USES_MODEL, usesModel);
        }

        /**
         * The request body. A model gets its required properties only, whatever the document states
         * for the body as a whole; an array or a scalar takes the example the document states for it.
         */
        private String body(Shape shape, Object example, String where) {
            CodegenModel model = shape.isArray ? null : model(shape);
            if (model != null && !model.isEnum) {
                return CallSnippets.isComposed(model) ? composed(model, null, where, 0) : construct(model, where, 0);
            }

            String value = value(shape, example, "body", where, 0);

            // `var` takes its type from the value, and a bare default has none.
            return "default".equals(value) ? "default(" + shape.type() + ")" : value;
        }

        /**
         * The model with its required properties set: the constructor of a generated model checks its
         * required reference-typed properties for null, so an object initializer cannot be used.
         * <p>
         * A model composed with allOf is the exception: its constructor takes only the properties it
         * declares itself and leaves the parent's to an initializer, because it calls the parameterless
         * constructor of the parent.
         */
        private String construct(CodegenModel model, String where, int depth) {
            usesModel = true;

            List<String> arguments = new ArrayList<>();
            List<String> initializers = new ArrayList<>();

            if (depth < MAX_DEPTH && model.readWriteVars != null) {
                List<String> own = ownProperties(model);

                for (CodegenProperty property : model.readWriteVars) {
                    if (!property.required) {
                        continue;
                    }

                    Object example = CallSnippets.plain(SpecExamples.inheritedProperty(openAPI, model.name, property.baseName));
                    String value = value(Shape.of(property, model.classname), example, property.baseName,
                            where + "." + property.baseName, depth + 1);

                    if (own == null || own.contains(property.baseName)) {
                        arguments.add(constructorParameter.apply(property.name) + ": " + value);
                    } else {
                        initializers.add(property.name + " = " + value);
                    }
                }
            }

            String construction = "new " + model.classname + "("
                    + (depth == 0 ? list(arguments) : String.join(", ", arguments)) + ")";

            if (initializers.isEmpty()) {
                return construction;
            }

            return construction + " { " + String.join(", ", initializers) + " }";
        }

        /**
         * The properties the constructor of an allOf model takes, or null for a model whose constructor
         * takes all of them.
         */
        private static List<String> ownProperties(CodegenModel model) {
            Object localVars = model.vendorExtensions.get("x-localVars");
            if (!Boolean.TRUE.equals(model.vendorExtensions.get("x-uses-allOf")) || !(localVars instanceof List)) {
                return null;
            }

            List<String> names = new ArrayList<>();
            for (Object item : (List<?>) localVars) {
                names.add(((CodegenProperty) item).baseName);
            }

            return names;
        }

        /**
         * A oneOf or anyOf model: a wrapper with a constructor per variant. The variant is the one the
         * example fits, or the first one when there is no example.
         */
        private String composed(CodegenModel model, Object example, String where, int depth) {
            usesModel = true;

            // A variant can be a oneOf of its own and lead back here, so the wrapper stops at the depth
            // the models do. It has no constructor without a variant, hence the default.
            if (depth >= MAX_DEPTH) {
                return "default(" + model.classname + ")";
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

            return "new " + model.classname + "(" + typed(chosen, example, where, depth + 1) + ")";
        }

        private boolean fits(String type, Object example) {
            if (example == null) {
                return false;
            }

            if (type.startsWith("List<")) {
                return example instanceof List;
            }

            switch (type) {
                case "string":
                    return example instanceof String;
                case "int":
                case "long":
                    return example instanceof Integer || example instanceof Long;
                case "double":
                case "float":
                case "decimal":
                    return example instanceof Number;
                case "bool":
                    return example instanceof Boolean;
                default:
                    CodegenModel model = models.get(stripNullable(type));
                    if (model == null) {
                        return false;
                    }

                    // An enum fits only an example that names one of its members; any other is left to
                    // a variant beside it, such as a string.
                    return model.isEnum ? CallSnippets.member(model.allowableValues, example) != null : example instanceof Map;
            }
        }

        /** A value of a type the generator names by its C# spelling only, as the variants of a oneOf. */
        private String typed(String type, Object example, String where, int depth) {
            if (type.startsWith("List<") && type.endsWith(">")) {
                String itemType = type.substring("List<".length(), type.length() - 1);

                List<String> items = new ArrayList<>();
                if (example instanceof List) {
                    for (Object item : (List<?>) example) {
                        items.add(typed(itemType, item, where, depth + 1));
                    }
                }

                if (items.isEmpty()) {
                    placeholders.add(where);
                    if (models.containsKey(itemType)) {
                        usesModel = true;
                    }
                    return "new " + type + "()";
                }

                return "new " + type + " { " + String.join(", ", items) + " }";
            }

            String text = example == null ? null : SpecExamples.text(example);

            switch (stripNullable(type)) {
                case "string":
                    return text == null ? placeholderString(where) : CallSnippets.quote(text);
                case "int":
                case "long":
                case "double":
                    return text == null ? "0" : text;
                case "float":
                    return (text == null ? "0" : text) + "f";
                case "decimal":
                    return (text == null ? "0" : text) + "m";
                case "bool":
                    return text == null ? "false" : text.toLowerCase(Locale.ROOT);
                default:
                    CodegenModel model = models.get(stripNullable(type));
                    if (model == null) {
                        return "default(" + type + ")";
                    }
                    if (model.isEnum) {
                        Shape shape = new Shape();
                        shape.complexType = type;
                        return enumValue(shape, example, where);
                    }
                    return CallSnippets.isComposed(model) ? composed(model, example, where, depth) : construct(model, where, depth);
            }
        }

        private String placeholderString(String where) {
            placeholders.add(where);
            return CallSnippets.quote("YOUR_VALUE");
        }

        private String value(Shape shape, Object example, String name, String where, int depth) {
            if (shape.isArray && shape.items != null && (shape.items.isFile || shape.items.isBinary)) {
                return "new List<FileParameter> { " + FILE + " }";
            }

            if (shape.isFile) {
                return FILE;
            }

            String enumValue = enumValue(shape, example, where);
            if (enumValue != null) {
                return enumValue;
            }

            if (shape.isArray) {
                // A body parameter carries no item description, only the C# type.
                if (shape.items == null) {
                    return typed(shape.type(), example, where, depth);
                }

                if (example instanceof List) {
                    List<String> items = new ArrayList<>();
                    for (Object item : (List<?>) example) {
                        items.add(value(Shape.of(shape.items, shape.owner), item, name, where, depth + 1));
                    }

                    return "new " + shape.type() + " { " + String.join(", ", items) + " }";
                }

                placeholders.add(where);
                if (model(Shape.of(shape.items, shape.owner)) != null) {
                    usesModel = true;
                }
                return "new " + shape.type() + "()";
            }

            if (shape.isMap) {
                return "new " + shape.type() + "()";
            }

            CodegenModel model = model(shape);
            if (model != null) {
                return CallSnippets.isComposed(model) ? composed(model, example, where, depth) : construct(model, where, depth);
            }

            if (example == null) {
                placeholders.add(where);
                return placeholder(shape, name);
            }

            String text = SpecExamples.text(example);

            if (shape.isBoolean) {
                return text.toLowerCase(Locale.ROOT);
            }
            if (shape.isUuid) {
                return "Guid.Parse(" + CallSnippets.quote(text) + ")";
            }
            if (shape.isDateTime || shape.isDate) {
                return "DateTime.Parse(" + CallSnippets.quote(text) + ")";
            }
            if (shape.isFloat) {
                return text + "f";
            }
            if (shape.isDecimal) {
                return text + "m";
            }
            if (shape.isNumeric) {
                return text;
            }

            return CallSnippets.quote(text);
        }

        /**
         * An enum member: the one whose value the example states, or the first one when it states none.
         */
        private String enumValue(Shape shape, Object example, String where) {
            if (shape.isArray) {
                return null;
            }

            String type;
            Map<String, Object> allowableValues;

            // A parameter typed by an enum schema names it in its type rather than as a model.
            CodegenModel model = model(shape);
            if (model == null && shape.dataType != null) {
                model = models.get(stripNullable(shape.dataType));
            }

            if (model != null && model.isEnum) {
                type = model.classname;
                allowableValues = model.allowableValues;
            } else if (shape.isEnum && shape.owner != null) {
                // An enum declared in place is a nested type of the model that holds it; anywhere else
                // the SDK takes the underlying value, as List<int> for the access levels of a request.
                String enumType = shape.datatypeWithEnum != null ? shape.datatypeWithEnum : shape.dataType;
                type = shape.isInnerEnum ? shape.owner + "." + enumType : enumType;
                allowableValues = shape.allowableValues;
            } else {
                return null;
            }

            Object values = allowableValues == null ? null : allowableValues.get("enumVars");
            if (!(values instanceof List) || ((List<?>) values).isEmpty()) {
                return null;
            }

            usesModel = true;

            Map<?, ?> chosen = CallSnippets.member(allowableValues, example);
            if (chosen == null) {
                placeholders.add(where);
                chosen = (Map<?, ?>) ((List<?>) values).get(0);
            }

            return stripNullable(type) + "." + chosen.get("name");
        }

        private CodegenModel model(Shape shape) {
            if (shape.complexType != null && models.containsKey(shape.complexType)) {
                return models.get(shape.complexType);
            }

            return shape.isModel ? models.get(stripNullable(shape.dataType)) : null;
        }

        private static String placeholder(Shape shape, String name) {
            if (shape.isBoolean) {
                return "false";
            }
            if (shape.isUuid) {
                return "Guid.Empty";
            }
            if (shape.isDateTime || shape.isDate) {
                return "DateTime.UtcNow";
            }
            if (shape.isNumeric) {
                return "0";
            }
            if (shape.isString || "string".equals(shape.type())) {
                return CallSnippets.quote(CallSnippets.placeholderName(name));
            }

            return "default";
        }

        /**
         * Arguments on one line, or one per line once they get long.
         */
        private static String list(List<String> items) {
            String line = String.join(", ", items);
            if (line.length() <= LINE_LENGTH && !line.contains("\n")) {
                return line;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < items.size(); i++) {
                builder.append("\n    ").append(items.get(i));
                if (i < items.size() - 1) {
                    builder.append(',');
                }
            }

            return builder.toString();
        }

        private static String stripNullable(String type) {
            return type != null && type.endsWith("?") ? type.substring(0, type.length() - 1) : type;
        }

        /** What the snippet needs to know about the type of a parameter or a property. */
        private static final class Shape {
            String dataType;
            String datatypeWithEnum;
            String complexType;
            String owner;
            boolean isString;
            boolean isNumeric;
            boolean isFloat;
            boolean isDecimal;
            boolean isBoolean;
            boolean isDate;
            boolean isDateTime;
            boolean isUuid;
            boolean isEnum;
            boolean isInnerEnum;
            boolean isArray;
            boolean isMap;
            boolean isModel;
            boolean isFile;
            Map<String, Object> allowableValues;
            CodegenProperty items;

            String type() {
                return stripNullable(dataType);
            }

            static Shape of(CodegenParameter parameter) {
                Shape shape = new Shape();
                shape.dataType = parameter.dataType;
                shape.datatypeWithEnum = parameter.datatypeWithEnum;
                shape.complexType = stripNullable(parameter.baseType);
                shape.isString = parameter.isString;
                shape.isNumeric = parameter.isNumeric || parameter.isInteger || parameter.isLong || parameter.isNumber
                        || parameter.isFloat || parameter.isDouble || parameter.isDecimal;
                shape.isFloat = parameter.isFloat;
                shape.isDecimal = parameter.isDecimal;
                shape.isBoolean = parameter.isBoolean;
                shape.isDate = parameter.isDate;
                shape.isDateTime = parameter.isDateTime;
                shape.isUuid = parameter.isUuid;
                shape.isEnum = parameter.isEnum;
                shape.isArray = parameter.isArray;
                shape.isMap = parameter.isMap;
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
                shape.isString = property.isString;
                shape.isNumeric = property.isNumeric || property.isInteger || property.isLong || property.isNumber
                        || property.isFloat || property.isDouble || property.isDecimal;
                shape.isFloat = property.isFloat;
                shape.isDecimal = property.isDecimal;
                shape.isBoolean = property.isBoolean;
                shape.isDate = property.isDate;
                shape.isDateTime = property.isDateTime;
                shape.isUuid = property.isUuid;
                shape.isEnum = property.isEnum;
                shape.isInnerEnum = property.isInnerEnum;
                shape.isArray = property.isArray;
                shape.isMap = property.isMap;
                shape.isModel = property.isModel;
                shape.isFile = property.isFile || property.isBinary;
                shape.allowableValues = property.allowableValues;
                shape.items = property.items;
                return shape;
            }
        }
    }
}
