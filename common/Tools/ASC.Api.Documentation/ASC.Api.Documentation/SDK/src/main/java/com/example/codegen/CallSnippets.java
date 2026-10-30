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
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.media.Schema;
import org.openapitools.codegen.CodegenConfig;
import org.openapitools.codegen.CodegenModel;
import org.openapitools.codegen.CodegenOperation;
import org.openapitools.codegen.SupportingFile;
import org.openapitools.codegen.model.ModelMap;
import org.openapitools.codegen.model.OperationsMap;
import org.openapitools.codegen.utils.ModelUtils;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.function.BiPredicate;
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
     * <p>
     * An operation that states more than one tag stays only in the API of its first tag. The generator
     * puts it into the API of every tag it states, each time through another API class, and the
     * snippet file takes one example per operation. The first tag is the one the page is filed under
     * (see OpenapiSplitter), and the SDK has the method in that tag's API class as well.
     */
    static void attach(CodegenConfig config, OperationsMap objs, Consumer<CodegenOperation> builder) {
        Iterator<CodegenOperation> operations = objs.getOperations().getOperation().iterator();

        while (operations.hasNext()) {
            CodegenOperation op = operations.next();

            if (op.tags != null && op.tags.size() > 1
                    && !config.sanitizeTag(op.tags.get(0).getName()).equals(op.baseName)) {
                operations.remove();
            } else if (!op.vendorExtensions.containsKey(ThirdPartyVariants.IS_VARIANT)) {
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
     * The enum schema a JSON request body is, or null. The generator hands such a body to the language
     * as the enum's underlying type and leaves nothing in the parameter that names the enum, so it is
     * found through the reference the document writes.
     */
    static CodegenModel enumBody(OpenAPI openAPI, CodegenOperation operation, Map<String, CodegenModel> models) {
        Schema<?> schema = SpecExamples.requestBodySchema(openAPI, operation);
        if (schema == null || schema.get$ref() == null) {
            return null;
        }

        String name = ModelUtils.getSimpleRef(schema.get$ref());
        for (CodegenModel model : models.values()) {
            if (model.isEnum && name.equals(model.name)) {
                return model;
            }
        }

        return null;
    }

    /**
     * A structured example as plain collections: depending on where it is written, the parser hands
     * it over either that way or as a Jackson node.
     */
    static Object plain(Object example) {
        return example instanceof JsonNode ? Json.mapper().convertValue(example, Object.class) : example;
    }

    static boolean isComposed(CodegenModel model) {
        return !variants(model).isEmpty();
    }

    /** The variants of a oneOf or anyOf model: its oneOf, or else its anyOf; none for any other model. */
    static List<String> variants(CodegenModel model) {
        if (model.oneOf != null && !model.oneOf.isEmpty()) {
            return new ArrayList<>(model.oneOf);
        }

        return model.anyOf != null ? new ArrayList<>(model.anyOf) : new ArrayList<>();
    }

    /** The variant a value of a oneOf or anyOf goes into, and the example the value is built from, if any. */
    static final class Choice<T> {
        final T variant;
        final Object example;

        private Choice(T variant, Object example) {
            this.variant = variant;
            this.example = example;
        }
    }

    /** {@link #variant(OpenAPI, CodegenModel, List, Object, BiPredicate)} out of all the model's variants. */
    static Choice<String> variant(OpenAPI openAPI, CodegenModel model, Object example,
                                  BiPredicate<String, Object> fits) {
        return variant(openAPI, model, variants(model), example, fits);
    }

    /**
     * The variant of a oneOf or anyOf model a value goes into, with the example it is built from: the
     * variant the value's own example fits; failing that, the variant an example its variants state
     * fits, with that example; failing both, the first variant, with no example.
     */
    static <T> Choice<T> variant(OpenAPI openAPI, CodegenModel model, List<T> variants, Object example,
                                 BiPredicate<T, Object> fits) {
        T chosen = fitting(variants, example, fits);
        if (chosen != null) {
            return new Choice<>(chosen, example);
        }

        for (Object stated : SpecExamples.variantExamples(openAPI, model.name)) {
            Object plain = plain(stated);
            chosen = fitting(variants, plain, fits);
            if (chosen != null) {
                return new Choice<>(chosen, plain);
            }
        }

        return new Choice<>(variants.get(0), null);
    }

    private static <T> T fitting(List<T> variants, Object example, BiPredicate<T, Object> fits) {
        for (T variant : variants) {
            if (fits.test(variant, example)) {
                return variant;
            }
        }

        return null;
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
