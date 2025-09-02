package com.example.codegen;

import org.openapitools.codegen.languages.PostmanCollectionCodegen;
import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.CodegenParameter;
import org.openapitools.codegen.CodegenProperty;
import org.openapitools.codegen.SupportingFile;
import io.swagger.v3.oas.models.servers.ServerVariable;
import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.servers.Server;

import java.util.List;
import java.util.HashMap;
import java.util.Map;
import java.util.ArrayList;
import java.util.Set;
import java.util.HashSet;
import java.util.Collections;
import java.util.Comparator;

public class MyPostmanCollectionCodegen extends PostmanCollectionCodegen {

    public MyPostmanCollectionCodegen() {
        super();
        this.outputFolder = "generated-code/my-postman-collection";
        this.templateDir = "templates/postman-collection";
        this.embeddedTemplateDir = "postman-collection";
    }

    @Override
    public void processOpts() {
        super.processOpts();

        String baseURL = openAPI.getServers().get(0).getUrl();
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
        public List<ChildGroup> children = new ArrayList<>();
    }

    public static class ChildGroup {
        public String name;
        public List<CodegenOperation> operations = new ArrayList<>();
        public boolean isSelf;
    }

    @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        OperationsMap results = super.postProcessOperationsWithModels(objs, allModels);

        OperationMap ops = results.getOperations();
        List<CodegenOperation> opList = ops.getOperation();

        for (CodegenOperation codegenOperation : opList) {

            if (folderStrategy.equalsIgnoreCase("tags")) {
                addToMap(codegenOperation);
            } else {
                addToList(codegenOperation);
            }

        }

        List<TagGroup> groups = new ArrayList<>();
        for (Map.Entry<String, Map<String, List<CodegenOperation>>> parentEntry : codegenOperationsByTag.entrySet()) {
            TagGroup tg = new TagGroup();
            String parentKey = parentEntry.getKey();
            tg.name = parentEntry.getKey();

            for (Map.Entry<String, List<CodegenOperation>> childEntry : parentEntry.getValue().entrySet()) {
                ChildGroup cg = new ChildGroup();
                cg.name = childEntry.getKey();
                cg.operations = childEntry.getValue();
                cg.isSelf = "_self".equals(cg.name);
                tg.children.add(cg);
            }
            groups.add(tg);
        }

        additionalProperties.put("tagGroups", groups);

        return results;
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
