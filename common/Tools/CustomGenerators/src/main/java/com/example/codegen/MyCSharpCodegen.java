package com.example.codegen;

import java.util.List;

import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.model.ModelsMap;
import org.openapitools.codegen.languages.CSharpClientCodegen;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.CodegenModel;
import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;

import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.CodegenOperation;

import java.io.File;

public class MyCSharpCodegen extends CSharpClientCodegen {

    public MyCSharpCodegen() {
        super();
        this.outputFolder = "generated-code/my-csharp";
        this.templateDir = "templates/csharp";
        this.embeddedTemplateDir = "csharp";
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

        String packageFolder = sourceFolder + File.separator + packageName;

        supportingFiles.removeIf(f -> f.getTemplateFile().equals("git_push.sh.mustache") || 
            f.getTemplateFile().equals("appveyor.mustache") ||
            f.getDestinationFilename().equals(".openapi-generator-ignore")
        );

        supportingFiles.add(new SupportingFile(
            "Program.mustache", packageFolder + ".Example", "Program.cs"
        ));

        supportingFiles.add(new SupportingFile(
            "ExampleProject.mustache", packageFolder + ".Example", packageName + ".Example.csproj"
        ));

    }

    @Override
    public ModelsMap postProcessModels(ModelsMap objs) {
        for (ModelMap mo : objs.getModels()) {
            CodegenModel model = mo.getModel();
            if ("ApiDateTime".equals(model.classname)) {
                model.vendorExtensions.put("isApiDateTime", true);
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
            if (operationList != null) {
                for (CodegenOperation op : operationList) { 
                    if (op.operationId != null) {
                        String dashedId = toDashCase(op.operationId);
                        String seealsoUrl = "https://api.onlyoffice.com/docspace/api-backend/usage-api/" + dashedId + "/";
                        op.vendorExtensions.put("x-seealsoUrl", seealsoUrl);
                    }
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
    
    @Override
    public String getName() {
        return "my-csharp";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Csharp client";
    }
}