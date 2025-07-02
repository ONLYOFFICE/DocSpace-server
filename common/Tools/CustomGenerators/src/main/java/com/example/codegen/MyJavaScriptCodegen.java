package com.example.codegen;

import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.model.ModelsMap;
import org.openapitools.codegen.languages.JavascriptClientCodegen;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.CodegenModel;
import io.swagger.v3.oas.models.media.Schema;
import io.swagger.v3.oas.models.servers.ServerVariables;
import io.swagger.v3.oas.models.servers.Server;
import io.swagger.v3.oas.models.servers.ServerVariable;
import java.io.IOException;
import java.io.Writer;
import java.util.Map;
import java.util.List;
import java.util.HashMap;
import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.model.OperationMap;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.CodegenProperty;

public class MyJavaScriptCodegen extends JavascriptClientCodegen {

    public MyJavaScriptCodegen() {
        super();
        this.outputFolder = "generated-code/my-javascript";
        this.templateDir = "templates/javascript";
        this.embeddedTemplateDir = "javascript";

        supportingFiles.add(new SupportingFile("example.mustache", "", "example.js"));
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
    }


    @Override
    public ModelsMap postProcessModels(ModelsMap objs) {
        super.postProcessModels(objs);

        List<ModelMap> allModels = objs.getModels();
        System.out.println("\n===== Starting property-model description matching =====\n");

        // Define match priority levels
        final int EXACT_MATCH = 100;
        final int PREFIX_MATCH = 80;
        final int SUFFIX_MATCH = 70;
        final int MODEL_CONTAINS_PROP = 50;
        final int PROP_CONTAINS_MODEL = 40;
        final int NO_MATCH = 0;

        for (ModelMap mo : allModels) {
            CodegenModel model = mo.getModel();
            
            if (model.vars != null) {
                for (CodegenProperty prop : model.vars) {
                    if (!prop.isPrimitiveType && (prop.description == null || prop.description.isEmpty())) {
                        // Track the best match for this property
                        int bestMatchScore = NO_MATCH;
                        CodegenModel bestMatchModel = null;
                        String matchType = "None";
                        
                        System.out.println("\nFinding best match for property: " + prop.complexType);
                        
                        // Compare with all models to find the best match
                        for (ModelMap candidate : allModels) { 
                            CodegenModel candidateModel = candidate.getModel();
                            int currentScore = NO_MATCH;
                            String currentMatchType = "None";
                            
                            // Try different matching strategies and assign scores
                            if (prop.complexType.equals(candidateModel.classname)) {
                                currentScore = EXACT_MATCH;
                                currentMatchType = "Exact match";
                            } else if (candidateModel.classname.startsWith(prop.complexType)) {
                                currentScore = PREFIX_MATCH;
                                currentMatchType = "Model starts with property";
                            } else if (prop.complexType.startsWith(candidateModel.classname)) {
                                currentScore = PREFIX_MATCH;
                                currentMatchType = "Property starts with model";
                            } else if (candidateModel.classname.endsWith(prop.complexType)) {
                                currentScore = SUFFIX_MATCH;
                                currentMatchType = "Model ends with property";
                            } else if (prop.complexType.endsWith(candidateModel.classname)) {
                                currentScore = SUFFIX_MATCH;
                                currentMatchType = "Property ends with model";
                            } else if (candidateModel.classname.contains(prop.complexType) && 
                                      !prop.complexType.contains("Wrapper") && 
                                      !prop.complexType.contains("Array")) {
                                currentScore = MODEL_CONTAINS_PROP;
                                currentMatchType = "Model contains property";
                            } else if (prop.complexType.contains(candidateModel.classname) && 
                                      !candidateModel.classname.contains("Wrapper") && 
                                      !candidateModel.classname.contains("Array")) {
                                currentScore = PROP_CONTAINS_MODEL;
                                currentMatchType = "Property contains model";
                            }
                            
                            // If this is a better match than what we've seen so far, update
                            if (currentScore > bestMatchScore) {
                                bestMatchScore = currentScore;
                                bestMatchModel = candidateModel;
                                matchType = currentMatchType;
                            }
                        }
                        
                        // Apply the best match if one was found
                        if (bestMatchModel != null) {
                            prop.description = bestMatchModel.description;
                            System.out.println("  ✓ Match found: " + matchType);
                            System.out.println("    Property: " + prop.complexType);
                            System.out.println("    Model: " + bestMatchModel.classname);
                            System.out.println("    Score: " + bestMatchScore);
                            System.out.println("    Description applied: " + 
                                (prop.description != null ? 
                                 (prop.description.length() > 50 ? 
                                  prop.description.substring(0, 47) + "..." : 
                                  prop.description) : 
                                 "<null>"));
                        } else {
                            System.out.println("  ✗ No match found for property: " + prop.complexType);
                        }
                    }
                }
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