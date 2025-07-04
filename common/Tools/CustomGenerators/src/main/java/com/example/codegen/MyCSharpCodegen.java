package com.example.codegen;

import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;

import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.model.ModelsMap;
import org.openapitools.codegen.languages.CSharpClientCodegen;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.CodegenModel;
import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.CodegenParameter;
import org.openapitools.codegen.CodegenProperty;

import java.io.File;
import java.util.List;
import java.util.Map;
import java.util.HashMap;

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
        super.postProcessModels(objs);

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
        }

        return objs;
    }


    public Map<String, Object> postProcessSupportingFileData(Map<String, Object> objs) {
        super.postProcessSupportingFileData(objs);

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
}