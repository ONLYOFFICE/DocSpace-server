package com.example.codegen;

import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.model.ModelsMap;
import org.openapitools.codegen.languages.PythonClientCodegen;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.CodegenModel;
import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;
import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.CodegenParameter;
import org.openapitools.codegen.CodegenOperation;

import java.util.List;

public class MyPythonCodegen extends PythonClientCodegen {

    public MyPythonCodegen() {
        super();
        this.outputFolder = "generated-code/my-python-custom";
        this.templateDir = "templates/python";
        this.embeddedTemplateDir = "python";

        supportingFiles.add(new SupportingFile("main.mustache", "", "main.py"));
        
        supportingFiles.add(new SupportingFile(
            "AUTHORS.mustache", "", "AUTHORS.md"
        ));

        supportingFiles.add(new SupportingFile(
            "LICENSE.mustache", "", "LICENSE"
        ));
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
    }

        @Override
    public OperationsMap postProcessOperationsWithModels(OperationsMap objs, List<ModelMap> allModels) {
        super.postProcessOperationsWithModels(objs, allModels);

        if (objs != null && objs.getOperations() != null) {
            OperationMap operationMap = objs.getOperations();
            List<CodegenOperation> operationList = operationMap.getOperation();
            if (operationList != null) {
                for (CodegenOperation op : operationList) { 

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
    public String getName() {
        return "my-python-custom";
    }

    @Override
    public String getHelp() {
        return "Generates a custom Python client with example main.py.";
    }
}