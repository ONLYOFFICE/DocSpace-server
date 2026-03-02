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

import org.openapitools.codegen.languages.PostmanCollectionCodegen;
import org.openapitools.codegen.model.*;
import org.openapitools.codegen.*;
import io.swagger.v3.oas.models.servers.*;

import java.util.*;

public class MyPostmanCollectionClientCodegen extends PostmanCollectionCodegen {

    public MyPostmanCollectionClientCodegen() {
        super();
        this.templateDir = "templates/postman-collection";
        this.embeddedTemplateDir = "postman-collection";
    }

    @Override
    public void processOpts() {
        super.processOpts();
        this.outputFolder = "../../../../../sdk/docspace-api-postman-collections";

        if (openAPI.getServers() != null && !openAPI.getServers().isEmpty()) {
            Server server = openAPI.getServers().get(0);
            ServerVariables serverVars = server.getVariables();
            if (serverVars != null){
                ServerVariable baseUrlVar = serverVars.get("baseUrl");
                if(baseUrlVar != null && "".equals(baseUrlVar.getDefault())){
                    baseUrlVar.setDefault("http://localhost:8092");
                }
            }
        }

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

    public static class TagGroup {
        public String name;
        public String description; 
        public List<ChildGroup> children = new ArrayList<>();
    }

    public static class ChildGroup {
        public String name;
        public String description;
        public List<CodegenOperation> operations = new ArrayList<>();
        public boolean isSelf;
    }

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        OperationsMap results = super.postProcessOperationsWithModels(objs, allModels);

        OperationMap ops = results.getOperations();
        List<CodegenOperation> opList = ops.getOperation();

        for (CodegenOperation codegenOperation : opList) {

            codegenOperation.vendorExtensions.put("hasVariables", codegenOperation.pathParams != null && !codegenOperation.pathParams.isEmpty());

            codegenOperation.vendorExtensions.put("hasQueryParams", codegenOperation.queryParams != null && !codegenOperation.queryParams.isEmpty());

            if (folderStrategy.equalsIgnoreCase("tags")) {
                addToMap(codegenOperation);
            } else {
                addToList(codegenOperation);
            }

        }

        List<TagGroup> groups = new ArrayList<>();
        for (Map.Entry<String, Map<String, List<CodegenOperation>>> parentEntry : codegenOperationsByTag.entrySet()) {
            TagGroup tg = new TagGroup();
            tg.name = parentEntry.getKey();

            Map<String, List<CodegenOperation>> childrenMap = parentEntry.getValue();

            for (Map.Entry<String, List<CodegenOperation>> childEntry : childrenMap.entrySet()) {
                ChildGroup cg = new ChildGroup();
                cg.name = childEntry.getKey();
                cg.operations = childEntry.getValue();
                cg.isSelf = "_self".equals(cg.name);
                if (cg.isSelf)
                {
                    tg.description = getTagDescription(tg.name, null);
                }
                else
                {
                    cg.description = getTagDescription(tg.name, cg.name);
                }
                tg.children.add(cg);
            }
            groups.add(tg);
        }

        additionalProperties.put("tagGroups", groups);

        return results;
    }

    private String getTagDescription(String parent, String child) {

        if (openAPI.getTags() == null) {
            return "";
        }

        if (child == null || child.equals("Group")) {
            return findTagDescription(parent);
        }
        String fullTag = parent + " / " + child;
        String fullTagDescription = findTagDescription(fullTag);
        if (!fullTagDescription.isEmpty()) {
            return fullTagDescription;
        }

        return "";
    }

    private String findTagDescription(String tagName) {
        for (io.swagger.v3.oas.models.tags.Tag tag : openAPI.getTags()) {
            if (tagName.equals(tag.getName())) {
                return tag.getDescription() != null ? tag.getDescription() : "";
            }
        }
        return "";
    }

    private Map<String, Map<String, List<CodegenOperation>>> codegenOperationsByTag = new HashMap<>();

    public void addToMap(CodegenOperation codegenOperation) {
        String rawTag = "default";
        if (codegenOperation.tags != null && !codegenOperation.tags.isEmpty()) {
            rawTag = codegenOperation.tags.get(0).getName();
        }

        String[] parts = rawTag.split(" / ");
        String parent = parts[0];
        String child = parts.length > 1 ? parts[1] : (parts[0].equals("Group") ? "Group" : "_self");

        Map<String, List<CodegenOperation>> childMap =
            codegenOperationsByTag.computeIfAbsent(parent, k -> new HashMap<>());

        List<CodegenOperation> list =
            childMap.computeIfAbsent(child, k -> new ArrayList<>());

        if (!list.contains(codegenOperation)) {
            list.add(codegenOperation);
        }

        list.sort(Comparator.comparing(obj -> obj.path));
    }

    public void addToList(CodegenOperation codegenOperation) {
        codegenOperationsList.add(codegenOperation);

        Collections.sort(codegenOperationsList, Comparator.comparing(obj -> obj.path));
    }

    @Override
    public String getName() {
        return "my-postman-collection";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Postman Collection.";
    }
}
