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

import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.languages.TypeScriptAxiosClientCodegen;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.CodegenParameter;

import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;

public class MyTypeScriptAxiosCodegen extends TypeScriptAxiosClientCodegen {

    protected String apiDocPath = "docs/";
    protected String modelDocPath = "docs/";
    public static final String BASE_URL = "baseURL";

    public MyTypeScriptAxiosCodegen() {
        super();
        this.outputFolder = "generated-code/my-typescript-axios";
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
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        objs = super.postProcessOperationsWithModels(objs, allModels);
        OperationMap vals = objs.getOperations();
        List<CodegenOperation> operations = vals.getOperation();
        if (operations != null) {
            for (CodegenOperation op : operations) {
                if (op.tags != null && !op.tags.isEmpty()) {
                    String operationTag = op.tags.get(0).getName();
                    String cleanedTag = operationTag.trim().replaceAll("[^a-zA-Z0-9/]", "-").toLowerCase(Locale.ROOT);

                    String[] tagParts = cleanedTag.split("/");

                    String foldername = tagParts[0].isEmpty() ? "" : tagParts[0];
                    foldername = foldername.endsWith("-") ? foldername.substring(0, foldername.length() - 1) : foldername;

                    String filename = tagParts.length > 1 && !tagParts[1].isEmpty()
                            ? tagParts[1] + "-api"
                            : foldername + "-api";
                    filename = filename.startsWith("-") ? filename.substring(1) : filename;

                    String className = String.join("", 
                        java.util.Arrays.stream(tagParts)
                            .map(part -> part.substring(0, 1).toUpperCase() + part.substring(1))
                            .collect(Collectors.toList()));

                    op.vendorExtensions.put("x-classFoldername", foldername);
                    op.vendorExtensions.put("x-classFilename", filename);
                    op.vendorExtensions.put("x-className", className);
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
                        CodegenParameter fieldsParam = new CodegenParameter();
                        fieldsParam.baseName = "fields";
                        fieldsParam.paramName = "fields";
                        fieldsParam.dataType = "string";
                        fieldsParam.description = "Comma-separated list of fields to include in the response";
                        fieldsParam.required = false;
                        fieldsParam.isQueryParam = true;
                        fieldsParam.isPrimitiveType = true;
                        fieldsParam.isNullable = true;
                        fieldsParam.collectionFormat = "csv";

                        op.allParams.add(fieldsParam);
                        op.queryParams.add(fieldsParam);
                    }
                }
            }
        }

        return objs;
    }

    
    public Map<String, Object> postProcessSupportingFileData(Map<String, Object> objs) {
        super.postProcessSupportingFileData(objs);

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
    };

    private final Map<String, String> seenApiFilenames = new HashMap<String, String>();
    private String currentFolderName = null;
    private static final java.util.Set<String> noSplitTags = new java.util.HashSet<>(java.util.Arrays.asList(
        "ApiKeys", "ThirdParty"
    ));
    private String convertToHyphenatedFormat(String value) {
        return value
                .replaceAll("([a-z])([A-Z])", "$1-$2")
                .replaceAll("([A-Za-z])([0-9])", "$1-$2")
                .replaceAll("([0-9])([0-9])", "$1-$2") 
                .toLowerCase(Locale.ROOT);
    }
    @Override
    public String apiFilename(String templateName, String tag) {
        String uniqueTag = uniqueCaseInsensitiveString(tag, seenApiFilenames);
        String suffix = apiTemplateFiles().get(templateName);
        String fileName;
        if (noSplitTags.contains(uniqueTag)) {
            currentFolderName = uniqueTag;
            fileName = uniqueTag;
        } else {
            int splitIndex = findSplitIndex(uniqueTag);
            if(splitIndex > 0)
            {
                currentFolderName = uniqueTag.substring(0, splitIndex);
                fileName = uniqueTag.substring(splitIndex);
            }
            else {
                fileName = uniqueTag;
                currentFolderName = uniqueTag;
            }
        }
        if (!"ThirdParty".equalsIgnoreCase(currentFolderName)) {
            currentFolderName = convertToHyphenatedFormat(currentFolderName);
        }
        return apiFileFolder() + File.separator + toApiFilename(fileName) + suffix;
    }

    private static final List<String> customApiFilenames = Arrays.asList(
        "AccessToDevTools",
        "SMTPSettings",
        "IPRestrictions",
        "TFASettings",
        "ThirdParty"
    );

    @Override
    public String toApiFilename(String name) {
        if (name.matches("^[A-Z0-9]+$")) {
            name = name.toLowerCase(Locale.ROOT);
        }
        if (customApiFilenames.contains(name)) {
            switch (name) {
                case "AccessToDevTools":
                    return "access-to-devtools-api";
                case "SMTPSettings":
                    return "smtp-settings-api";
                case "IPRestrictions":
                    return "ip-restrictions-api";
                case "TFASettings":
                    return "tfa-settings-api";
                case "ThirdParty":
                    return "thirdparty-api";
                default:
                    return super.toApiFilename(name).replaceAll("([a-z0-9])([A-Z])", "$1-$2").toLowerCase(Locale.ROOT);
            }
        }
        return super.toApiFilename(name).replaceAll("([a-z0-9])([A-Z])", "$1-$2").toLowerCase(Locale.ROOT);
    }
    
    @Override
    public String apiFileFolder() {
        return outputFolder + File.separator + apiPackage().replace('.', File.separatorChar)
               + (currentFolderName == null || currentFolderName.isEmpty() ? "" : File.separator + currentFolderName.toLowerCase(Locale.ROOT));
    }

    private int findSplitIndex(String name) {
        for (int i = 2; i < name.length(); i++) {
            if (Character.isUpperCase(name.charAt(i))) {
                return i;
            }
        }
        return -1;
    }
}
