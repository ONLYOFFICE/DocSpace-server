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

import io.swagger.v3.core.util.Json31;
import io.swagger.v3.oas.models.Operation;
import io.swagger.v3.oas.models.parameters.Parameter;
import io.swagger.v3.oas.models.parameters.RequestBody;
import io.swagger.v3.oas.models.responses.ApiResponse;
import io.swagger.v3.oas.models.responses.ApiResponses;
import io.swagger.v3.oas.models.servers.Server;

import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.CodegenParameter;
import org.openapitools.codegen.DefaultCodegen;
import org.openapitools.codegen.model.OperationsMap;

import static org.openapitools.codegen.utils.StringUtils.camelize;
import static org.openapitools.codegen.utils.StringUtils.underscore;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.ListIterator;
import java.util.Locale;
import java.util.Map;
import java.util.Objects;
import java.util.Set;

/**
 * The third-party twin of a generic controller action.
 * <p>
 * On the server a generic action exists twice, on one and the same route: closed over int for an entry the
 * portal stores itself and over string for an entry on a connected third-party account. The service documents
 * describe the int shape as the operation and the string shape as {@code x-thirdparty-variant} - only the
 * parameters, request body and responses that differ. This class turns the extension into a second
 * {@link CodegenOperation}. It is built through the swagger models, so the generator derives its parameter and
 * return types exactly as it does for every other operation, and each language decides how to expose it:
 * <ul>
 *   <li>{@link Naming#OVERLOAD} keeps the operation id, and the template renders one more overload of the same
 *       method (C#, Java, Kotlin, Swift): {@code getFileInfo(int)} returns FileWrapper, {@code getFileInfo(String)}
 *       returns ThirdPartyFileWrapper.</li>
 *   <li>{@link Naming#SIBLING} appends {@code ThirdParty} to the operation id and lets the language's own naming
 *       rules take it from there - TypeScript {@code getFileInfoThirdParty}, Python {@code get_file_info_third_party},
 *       Go {@code GetFileInfoThirdParty}. For languages without overloading, and rather than guessing the kind of an
 *       id at run time.</li>
 * </ul>
 * A client codegen calls {@link #attach} at the end of its {@code fromOperation} override and {@link #insert}
 * first thing in {@code postProcessOperationsWithModels}. A documentation codegen that renders the twin inside
 * the operation's own section calls {@link #attach} alone and reads {@link #VARIANT_OPERATION} from the template.
 */
final class ThirdPartyVariants {

    static final String EXTENSION = "x-thirdparty-variant";

    /** Set on the variant operation, for templates that have to tell it apart: README rows, doc anchors. */
    static final String IS_VARIANT = "x-is-thirdparty-variant";

    /** The variant, attached to the original operation until {@link #insert} makes it an operation of its own. */
    static final String VARIANT_OPERATION = "x-thirdparty-variant-operation";

    /** Set on the parameters of the variant that differ from the original's - the ids that became strings. */
    static final String CHANGED = "x-thirdparty-changed";

    /** Set on the variant when its response differs from the original's. */
    static final String RETURN_CHANGED = "x-thirdparty-return-changed";

    private static final String SUMMARY_SUFFIX = " (third-party storage)";
    private static final String SIBLING_SUFFIX = "ThirdParty";

    enum Naming {
        OVERLOAD,
        SIBLING
    }

    private ThirdPartyVariants() {
    }

    /**
     * Builds the variant of {@code operation}, when the document declares one, and attaches it to {@code op}.
     */
    static void attach(DefaultCodegen codegen, CodegenOperation op, String path, String httpMethod,
                       Operation operation, List<Server> servers, Naming naming) {
        Object extension = operation.getExtensions() == null ? null : operation.getExtensions().get(EXTENSION);
        if (!(extension instanceof Map)) {
            return;
        }

        Map<?, ?> overrides = (Map<?, ?>) extension;
        Operation variant = build(operation, overrides, naming);

        // Back through the codegen's own fromOperation. The variant carries no extension, so it comes out plain.
        CodegenOperation variantOp = codegen.fromOperation(path, httpMethod, variant, servers);
        variantOp.vendorExtensions.put(IS_VARIANT, true);
        markChanged(variantOp, overrides);

        op.vendorExtensions.put(VARIANT_OPERATION, variantOp);
    }

    /**
     * Turns every attached variant into an operation of the list, right after its original, and gives the
     * api file the imports the variants need. Call it before the base class processes the list, so that the
     * variants get the same treatment as the operations they were derived from.
     */
    static void insert(DefaultCodegen codegen, OperationsMap objs) {
        if (objs == null || objs.getOperations() == null || objs.getOperations().getOperation() == null) {
            return;
        }

        List<CodegenOperation> variants = new ArrayList<>();
        ListIterator<CodegenOperation> iterator = objs.getOperations().getOperation().listIterator();

        while (iterator.hasNext()) {
            CodegenOperation op = iterator.next();
            Object attached = op.vendorExtensions.remove(VARIANT_OPERATION);
            if (!(attached instanceof CodegenOperation)) {
                continue;
            }

            CodegenOperation variantOp = (CodegenOperation) attached;

            // What DefaultGenerator sets on an operation after fromOperation, which the variant never went through.
            variantOp.tags = op.tags;
            variantOp.baseName = op.baseName;
            variantOp.operationIdLowerCase = variantOp.operationId.toLowerCase(Locale.ROOT);
            variantOp.operationIdCamelCase = camelize(variantOp.operationId);
            variantOp.operationIdSnakeCase = underscore(variantOp.operationId);
            variantOp.authMethods = op.authMethods;
            variantOp.hasAuthMethods = op.hasAuthMethods;

            iterator.add(variantOp);
            variants.add(variantOp);
        }

        addImports(codegen, objs, variants);
    }

    /**
     * The variant as a full Operation: the original with the pieces named by the extension swapped in.
     */
    private static Operation build(Operation operation, Map<?, ?> overrides, Naming naming) {
        Map<String, Object> extensions = operation.getExtensions() == null
            ? null
            : new LinkedHashMap<>(operation.getExtensions());
        if (extensions != null) {
            extensions.remove(EXTENSION);
        }

        String operationId = operation.getOperationId();
        if (naming == Naming.SIBLING && operationId != null) {
            operationId = operationId + SIBLING_SUFFIX;
        }

        Operation variant = new Operation()
            .operationId(operationId)
            .summary(operation.getSummary() == null ? null : operation.getSummary() + SUMMARY_SUFFIX)
            .description(operation.getDescription())
            .tags(operation.getTags())
            .deprecated(operation.getDeprecated())
            .security(operation.getSecurity())
            .servers(operation.getServers())
            .externalDocs(operation.getExternalDocs())
            .callbacks(operation.getCallbacks())
            .extensions(extensions);

        List<Parameter> parameters = new ArrayList<>();
        if (operation.getParameters() != null) {
            parameters.addAll(operation.getParameters());
        }

        Object parameterOverrides = overrides.get("parameters");
        if (parameterOverrides instanceof List) {
            for (Object item : (List<?>) parameterOverrides) {
                Parameter override = Json31.mapper().convertValue(item, Parameter.class);
                int index = -1;
                for (int i = 0; i < parameters.size(); i++) {
                    Parameter existing = parameters.get(i);
                    if (Objects.equals(existing.getName(), override.getName())
                            && Objects.equals(existing.getIn(), override.getIn())) {
                        index = i;
                        break;
                    }
                }
                if (index >= 0) {
                    parameters.set(index, override);
                } else {
                    parameters.add(override);
                }
            }
        }
        variant.setParameters(parameters);

        Object requestBody = overrides.get("requestBody");
        variant.setRequestBody(requestBody instanceof Map
            ? Json31.mapper().convertValue(requestBody, RequestBody.class)
            : operation.getRequestBody());

        ApiResponses responses = new ApiResponses();
        if (operation.getResponses() != null) {
            responses.putAll(operation.getResponses());
        }

        Object responseOverrides = overrides.get("responses");
        if (responseOverrides instanceof Map) {
            for (Map.Entry<?, ?> entry : ((Map<?, ?>) responseOverrides).entrySet()) {
                responses.put(String.valueOf(entry.getKey()),
                    Json31.mapper().convertValue(entry.getValue(), ApiResponse.class));
            }
        }
        variant.setResponses(responses);

        return variant;
    }

    /**
     * Flags on the variant what the extension changed, so that a documentation page can show the difference
     * alone instead of repeating the whole operation.
     */
    private static void markChanged(CodegenOperation variantOp, Map<?, ?> overrides) {
        Set<String> changed = new HashSet<>();
        Object parameterOverrides = overrides.get("parameters");
        if (parameterOverrides instanceof List) {
            for (Object item : (List<?>) parameterOverrides) {
                if (item instanceof Map) {
                    Map<?, ?> parameter = (Map<?, ?>) item;
                    changed.add(parameter.get("in") + ":" + parameter.get("name"));
                }
            }
        }

        boolean bodyChanged = overrides.get("requestBody") instanceof Map;

        if (variantOp.allParams != null) {
            for (CodegenParameter parameter : variantOp.allParams) {
                boolean isChanged = parameter.isBodyParam
                    ? bodyChanged
                    : changed.contains(location(parameter) + ":" + parameter.baseName);
                if (isChanged) {
                    parameter.vendorExtensions.put(CHANGED, true);
                }
            }
        }

        if (overrides.get("responses") instanceof Map) {
            variantOp.vendorExtensions.put(RETURN_CHANGED, true);
        }
    }

    private static String location(CodegenParameter parameter) {
        if (parameter.isPathParam) {
            return "path";
        }
        if (parameter.isQueryParam) {
            return "query";
        }
        if (parameter.isHeaderParam) {
            return "header";
        }
        if (parameter.isCookieParam) {
            return "cookie";
        }
        return "";
    }

    /**
     * The api file imports what its operations return and take; DefaultGenerator collected that before the
     * variants joined the list, so their models - ThirdPartyFileWrapper, for one - are added here the same way.
     */
    private static void addImports(DefaultCodegen codegen, OperationsMap objs, List<CodegenOperation> variants) {
        if (variants.isEmpty() || objs.getImports() == null) {
            return;
        }

        List<Map<String, String>> imports = new ArrayList<>(objs.getImports());
        Set<String> present = new HashSet<>();
        for (Map<String, String> entry : imports) {
            present.add(entry.get("classname"));
        }

        for (CodegenOperation variantOp : variants) {
            for (String name : variantOp.imports) {
                if (!present.add(name)) {
                    continue;
                }

                Map<String, String> mapped = new LinkedHashMap<>();
                String mapping = codegen.importMapping().get(name);
                if (mapping != null) {
                    mapped.put(mapping, name);
                } else {
                    mapped.putAll(codegen.toModelImportMap(name));
                }

                for (Map.Entry<String, String> entry : mapped.entrySet()) {
                    Map<String, String> item = new LinkedHashMap<>();
                    item.put("import", entry.getKey());
                    item.put("classname", entry.getValue());
                    imports.add(item);
                }
            }
        }

        imports.sort(Comparator.comparing(item -> item.get("classname")));
        objs.setImports(imports);
        objs.put("hasImport", !imports.isEmpty());
    }
}
