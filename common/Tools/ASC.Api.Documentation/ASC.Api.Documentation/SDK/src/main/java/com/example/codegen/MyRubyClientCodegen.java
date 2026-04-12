package com.example.codegen;

import java.io.File;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.Locale;

import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.languages.RubyClientCodegen;
import org.openapitools.codegen.model.ApiInfoMap;
import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.model.OperationsMap;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;
import io.swagger.v3.oas.models.servers.ServerVariables;

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

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        super.postProcessOperationsWithModels(objs, allModels);

        if (objs == null || objs.getOperations() == null) {
            return objs;
        }
        
        OperationMap operationMap = objs.getOperations();
            List<CodegenOperation> operationList = operationMap.getOperation();
            if (operationList != null) {
                for (CodegenOperation op : operationList) { 
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

        return objs;
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
}
