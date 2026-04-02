package com.example.codegen;

import java.io.File;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.CodegenModel;
import org.openapitools.codegen.CodegenProperty;
import org.openapitools.codegen.languages.GoClientCodegen;
import org.openapitools.codegen.model.ApiInfoMap;
import org.openapitools.codegen.model.ModelsMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.model.OperationsMap;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;
import io.swagger.v3.oas.models.servers.ServerVariables;
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

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        super.postProcessOperationsWithModels(objs, allModels);

        if (objs != null && objs.getOperations() != null) {
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
                            .replace("\n", "\n// ");
                        op.vendorExtensions.put("x-commentedNotes", commentedNotes);
                    }
                }
            }
        }

        return objs;
    }

    @Override
    public ModelsMap postProcessModels(ModelsMap objs) {
        super.postProcessModels(objs);

        for (ModelMap modelMap : objs.getModels()) {
            CodegenModel model = modelMap.getModel();
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

        supportingFiles.removeIf(file ->
            "README.md".equals(file.getDestinationFilename())
                || "README.mustache".equals(file.getTemplateFile())
                || "golang_README.mustache".equals(file.getTemplateFile())
        );
        supportingFiles.add(new SupportingFile("golang_README.mustache", "", "README.md"));

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

        Object repositoryUrlObj = additionalProperties.get("repositoryUrl");
        if (repositoryUrlObj != null) {
            String packageImportPath = String.valueOf(repositoryUrlObj)
                .trim()
                .replaceFirst("^https?://", "")
                .replaceFirst("\\.git$", "")
                .replaceFirst("/+$", "");

            additionalProperties.put("packageImportPath", packageImportPath);
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
}
