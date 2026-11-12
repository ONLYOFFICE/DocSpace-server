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

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.media.Schema;

/**
 * Removes every {@code discriminator} from the document before a generator that cannot carry it reads it.
 *
 * <p>With a discriminator the base generator switches a schema built with {@code allOf} from flattened fields to real
 * inheritance. Swift has no inheritance for its {@code struct} models and dropped every inherited field (a
 * {@code FileDto} without {@code id} or {@code title}); Kotlin made the parent a final {@code data class} that the child
 * cannot extend. Without the discriminator both flatten the fields as before, and the discriminator property stays an
 * ordinary string; an item of a list declared with the base type is read as the base there.
 */
final class Discriminators {

    private Discriminators() {
    }

    static void strip(OpenAPI openAPI) {
        if (openAPI.getComponents() == null || openAPI.getComponents().getSchemas() == null) {
            return;
        }

        for (Schema<?> schema : openAPI.getComponents().getSchemas().values()) {
            schema.setDiscriminator(null);
        }
    }
}
