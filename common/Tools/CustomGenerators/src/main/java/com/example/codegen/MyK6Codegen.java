package com.example.codegen;

import org.openapitools.codegen.languages.K6ClientCodegen;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.meta.GeneratorMetadata;
import org.openapitools.codegen.meta.Stability;
import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.CodegenOperation;
import io.swagger.v3.oas.models.OpenAPI;
import java.io.File;
import java.util.List;
import java.util.Map;
import java.util.Map.Entry;
import java.util.Optional;
import java.util.Locale;
import java.util.stream.Collectors;
import java.util.HashMap;
import java.util.Collection;

import static org.openapitools.codegen.utils.StringUtils.*;

public class MyK6Codegen extends K6ClientCodegen {
    
    public MyK6Codegen() {
        super();
    }

    @Override
    public String getName() {
        return "my-k6";
    }

    @Override
    public String getHelp() {
        return "Custom generator for k6 with nested directory output based on tags.";
    }

    @Override
    public void processOpts() {
        super.processOpts();

        this.apiPackage = "api";
        this.templateDir = "templates/k6";
        this.embeddedTemplateDir = "k6";

        supportingFiles.clear();

        //apiTemplateFiles.put("script.mustache", ".js");

        supportingFiles.add(new SupportingFile("script.mustache", "", "script.js"));
        supportingFiles.add(new SupportingFile("README.mustache", "", "README.md"));
        supportingFiles.add(new SupportingFile("LICENSE.mustache", "", "LICENSE"));
        supportingFiles.add(new SupportingFile("CHANGELOG.mustache", "", "CHANGELOG.md"));

        // Map<String, Object> additional = this.additionalProperties;
        // Object rg = additional.get("requestGroups");
        // System.out.println(rg); 

        // if (rg instanceof Collection) {
        //     Collection<?> groups = (Collection<?>) rg;
        //     for (Object g : groups) {
        //         System.out.println(g); 
        //     }
        // }
    }

    //protected Map<String, HTTPRequestGroup> requestGroups = new HashMap<>();

    // @Override
    // public void preprocessOpenAPI(OpenAPI openAPI) {
    //     super.preprocessOpenAPI(openAPI);

    //     this.requestGroups = preprocessOpenAPI(openAPI);
    // }

    // private final Map<String, TagParts> tagMap = new HashMap<>();
    

    // @Override
    // public String sanitizeTag(String tag) {
    //     System.out.println("Tag " + tag);
    //     String sanitized = super.sanitizeTag(tag);
    //     if (!tagMap.containsKey(sanitized)) {
    //         String[] parts = tag.split(" / ");
    //         String folderPart = parts[0];
    //         String classPart = (parts.length > 1) ? parts[1] : parts[0];

    //         TagParts info = new TagParts(
    //             tag,
    //             camelize(sanitizeName(folderPart)),
    //             camelize(sanitizeName(classPart))
    //         );

    //         tagMap.put(sanitized, info);
    //     }
    //     return sanitized;
    // }

    // private final Map<String, String> seenApiFilenames = new HashMap<String, String>();

    // @Override
    // public String apiFilename(String templateName, String tag) {
    //     String uniqueTag = uniqueCaseInsensitiveString(tag, seenApiFilenames);
    //     String suffix = apiTemplateFiles().get(templateName);
    //     TagParts tagParts = tagMap.get(camelize(uniqueTag));
    //     if (tagParts == null) {
    //         return apiFileFolder() + File.separator + toApiFilename(uniqueTag) + suffix;
    //     }

    //     String folderPath = apiFileFolder() + File.separator + tagParts.folderPart;
    //     String filename = toApiFilename(tagParts.classPart) + suffix;

    //     return folderPath + File.separator + filename;
    // }

    // private String uniqueCaseInsensitiveString(String value, Map<String, String> seenValues) {
    //     if (seenValues.keySet().contains(value)) {
    //         return seenValues.get(value);
    //     }

    //     Optional<Entry<String, String>> foundEntry = seenValues.entrySet().stream().filter(v -> v.getValue().toLowerCase(Locale.ROOT).equals(value.toLowerCase(Locale.ROOT))).findAny();
    //     if (foundEntry.isPresent()) {
    //         int counter = 0;
    //         String uniqueValue = value + "_" + counter;

    //         while (seenValues.values().stream().map(v -> v.toLowerCase(Locale.ROOT)).collect(Collectors.toList()).contains(uniqueValue.toLowerCase(Locale.ROOT))) {
    //             counter++;
    //             uniqueValue = value + "_" + counter;
    //         }

    //         seenValues.put(value, uniqueValue);
    //         return uniqueValue;
    //     }

    //     seenValues.put(value, value);
    //     return value;
    // }


    // /**
    //  * Sanitizes a filename by converting to lowercase and replacing spaces and camelCase with hyphens.
    //  *
    //  * @param input The filename to sanitize
    //  * @return The sanitized filename
    //  */
    // private String sanitizeFilename(String input) {
    //     if (input == null || input.trim().isEmpty()) {
    //         return "default";
    //     }
    //     // First replace spaces with hyphens
    //     String result = input.trim().toLowerCase().replaceAll("\\s+", "-");
    //     // Then insert hyphens between camelCase sections
    //     result = result.replaceAll("([a-z])([A-Z])", "$1-$2").toLowerCase();
    //     return result;
    // }

}
