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

import com.fasterxml.jackson.databind.JsonNode;
import io.swagger.v3.core.util.Json;
import org.openapitools.codegen.CodegenConfig;
import org.openapitools.codegen.CodegenModel;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.model.OperationsMap;

import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.function.Consumer;

/**
 * What the snippet mode of every language's generator shares: switching a run from the SDK to the
 * call snippets of the reference pages, handing the operations to the language's builder, and the
 * helpers those builders use alike.
 * <p>
 * The language itself - its literals, constructors, enums, files - stays in the nested Snippets
 * class of its generator.
 */
final class CallSnippets {

    private CallSnippets() {
    }

    /**
     * Turns the run into the snippet mode when the command asked for it: instead of the SDK, one file
     * with a snippet per operation, rendered from the language's snippets.mustache into the folder the
     * run was given with -o. Returns whether it did.
     * <p>
     * The generator returns from processOpts as soon as this is true, before it points the output
     * folder at the SDK checkout: StaleOutput removes from that folder everything the run did not
     * write, and this run writes a single file.
     */
    static boolean configure(CodegenConfig config) {
        if (!Boolean.parseBoolean(String.valueOf(config.additionalProperties().get("snippetsOnly")))) {
            return false;
        }

        config.apiTemplateFiles().clear();
        config.modelTemplateFiles().clear();
        config.apiDocTemplateFiles().clear();
        config.modelDocTemplateFiles().clear();
        config.apiTestTemplateFiles().clear();
        config.modelTestTemplateFiles().clear();
        config.supportingFiles().clear();

        config.supportingFiles().add(new SupportingFile(
            "snippets.mustache", "", String.valueOf(config.additionalProperties().get("snippetsFile"))
        ));

        return true;
    }

    /**
     * Hands every operation of an API to the language's builder. The third-party twin of an operation
     * (see ThirdPartyVariants) is left out: it is an overload of the same method, on the same page.
     */
    static void attach(OperationsMap objs, Consumer<CodegenOperation> builder) {
        for (CodegenOperation op : objs.getOperations().getOperation()) {
            if (!op.vendorExtensions.containsKey(ThirdPartyVariants.IS_VARIANT)) {
                builder.accept(op);
            }
        }
    }

    /**
     * Prints, for the run's log, the values the snippets were given without the document stating an
     * example for them, as "operationId: name".
     */
    static void report(List<String> placeholders) {
        if (placeholders.isEmpty()) {
            return;
        }

        System.out.println("Snippet values the document states no example for (" + placeholders.size() + "):");
        for (String placeholder : placeholders) {
            System.out.println("  " + placeholder);
        }
    }

    /** The models of the run, by class name. */
    static Map<String, CodegenModel> models(List<ModelMap> allModels) {
        Map<String, CodegenModel> models = new HashMap<>();

        if (allModels != null) {
            for (ModelMap modelMap : allModels) {
                CodegenModel model = modelMap.getModel();
                if (model != null) {
                    models.put(model.classname, model);
                }
            }
        }

        return models;
    }

    /**
     * A structured example as plain collections: depending on where it is written, the parser hands
     * it over either that way or as a Jackson node.
     */
    static Object plain(Object example) {
        return example instanceof JsonNode ? Json.mapper().convertValue(example, Object.class) : example;
    }

    static boolean isComposed(CodegenModel model) {
        return (model.oneOf != null && !model.oneOf.isEmpty()) || (model.anyOf != null && !model.anyOf.isEmpty());
    }

    /** A string literal in double quotes, escaped the way C# and Go both read it. */
    static String quote(String text) {
        return "\"" + text
                .replace("\\", "\\\\")
                .replace("\"", "\\\"")
                .replace("\r", "\\r")
                .replace("\n", "\\n")
                .replace("\t", "\\t") + "\"";
    }

    static String unquote(String text) {
        if (text.length() >= 2 && text.startsWith("\"") && text.endsWith("\"")) {
            return text.substring(1, text.length() - 1);
        }

        return text;
    }

    /**
     * The member of an enum the example names, by its value or by its name; null when there is no
     * example or it names none of them. Both the choice of a oneOf variant and the choice of the member
     * go by it, so they cannot disagree.
     */
    static Map<?, ?> member(Map<String, Object> allowableValues, Object example) {
        Object values = allowableValues == null ? null : allowableValues.get("enumVars");
        if (example == null || !(values instanceof List)) {
            return null;
        }

        String text = SpecExamples.text(example);
        for (Object item : (List<?>) values) {
            Map<?, ?> member = (Map<?, ?>) item;
            if (text.equals(unquote(String.valueOf(member.get("value")))) || text.equals(String.valueOf(member.get("name")))) {
                return member;
            }
        }

        return null;
    }

    /**
     * The stand-in for a string the document states no example for, named after what it stands in
     * for: roomId gives YOUR_ROOM_ID.
     */
    static String placeholderName(String name) {
        return "YOUR_" + name.replaceAll("([a-z0-9])([A-Z])", "$1_$2").toUpperCase(Locale.ROOT);
    }
}
