package com.example.codegen;

import java.io.File;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.languages.GoClientCodegen;
import org.openapitools.codegen.model.ApiInfoMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.model.OperationsMap;

public class MyGoClientCodegen extends GoClientCodegen {

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
    public String apiFileFolder() {
        return outputFolder + File.separator + "api";
    }

    @Override
    public String modelFileFolder() {
        return outputFolder + File.separator + "models";
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

            TagParts info = new TagParts(
                tag,
                folderPartSanitized,
                finalClassPart
            );

            tagMap.put(sanitized, info);
        }

        return sanitized;
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
                tagParts = new TagParts(fallback, "Default", fallback);
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