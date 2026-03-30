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

import io.swagger.v3.oas.models.servers.*;
import io.swagger.v3.oas.models.media.Schema;

import org.openapitools.codegen.model.*;
import org.openapitools.codegen.languages.CSharpClientCodegen;
import org.openapitools.codegen.*;
import static org.openapitools.codegen.utils.StringUtils.camelize;
import org.openapitools.codegen.utils.ModelUtils;

import java.io.File;
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

    @Override
    public void processOpts() {
        super.processOpts();

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
            }
        }
        

        return objs;
    }

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
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
            if (operationList != null) {
                for (CodegenOperation op : operationList) { 
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
                        }
                    }
                }
            }
            operationMap.put("x-supportsFields", shouldSupportFields);
            operationMap.put("x-supportsUseAt", supportUseAt);
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

}
