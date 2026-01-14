package com.example.codegen;

import java.io.File;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;
import java.util.Map.Entry;
import java.util.List;
import java.util.Arrays;
import java.util.LinkedHashMap;
import java.util.ArrayList;
import java.util.Set;
import java.util.HashSet;

import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.languages.TypeScriptAxiosClientCodegen;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.CodegenParameter;
import org.openapitools.codegen.model.ApiInfoMap;
import static org.openapitools.codegen.utils.StringUtils.camelize;
import org.openapitools.codegen.model.ModelsMap;
import org.openapitools.codegen.CodegenModel;
import org.openapitools.codegen.CodegenProperty;
import org.openapitools.codegen.utils.ModelUtils;

import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;
import io.swagger.v3.oas.models.media.Schema;

public class MyTypeScriptAxiosCodegen extends TypeScriptAxiosClientCodegen {

    protected String apiDocPath = "docs/";
    protected String modelDocPath = "docs/";
    public static final String BASE_URL = "baseURL";

    public MyTypeScriptAxiosCodegen() {
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
        this.outputFolder = "../../../sdk/docspace-api-sdk-typescript";

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
        super.postProcessModels(objs);

        for (ModelMap mo : objs.getModels()) {
            CodegenModel model = mo.getModel();

            if (model.getComposedSchemas() != null && model.getComposedSchemas().getAllOf() != null) {
                model.getVendorExtensions().put("x-uses-allOf", true);
                Set<String> localPropertyNames = new HashSet<>();
                Schema modelSchema = this.openAPI.getComponents().getSchemas().get(model.schemaName);

                if (ModelUtils.isAllOf(modelSchema)) {
                    for (Object obj : modelSchema.getAllOf()) {
                        if (obj instanceof Schema) {
                            Schema allOfSchema = (Schema) obj;
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
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
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
        if (operations != null) {
            for (CodegenOperation op : operations) {
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
            }
        }
        operationMap.put("x-supportsFields", shouldSupportFields);

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
}
