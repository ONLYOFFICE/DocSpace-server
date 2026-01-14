package com.example.codegen;

import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.languages.JavascriptClientCodegen;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.CodegenParameter;

import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;

import java.util.List;
import java.util.Map;

public class MyJavaScriptCodegen extends JavascriptClientCodegen {

    public MyJavaScriptCodegen() {
        super();
        this.outputFolder = "generated-code/my-javascript";
        this.templateDir = "templates/javascript";
        this.embeddedTemplateDir = "javascript";

        supportingFiles.add(new SupportingFile("example.mustache", "", "example.js"));
        
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

        supportingFiles.removeIf(f -> f.getTemplateFile().equals("git_push.sh.mustache") || 
            f.getDestinationFilename().equals(".openapi-generator-ignore")
        );

        if(Boolean.TRUE.equals(additionalProperties.get("excludeTests")))
        {
            modelTestTemplateFiles.clear();
            apiTestTemplateFiles.clear();
        }
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
        return input.replaceAll("([a-z])([A-Z])", "$1-$2")
                    .toLowerCase();
    }
    
    @Override
    public String getName() {
        return "my-javascript";
    }

    @Override
    public String getHelp() {
        return "Generates a custom JavaScript client";
    }
}