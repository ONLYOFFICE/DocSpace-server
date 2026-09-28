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

import org.openapitools.codegen.CodegenConfig;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.UncheckedIOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Locale;
import java.util.concurrent.TimeUnit;

/**
 * Gives every generated text file one line ending, the one git checks files out with in that repository.
 * <p>
 * openapi-generator copies the line endings of the template into the output, and the text it splices in -
 * descriptions from the document, partials, generated code - carries LF. A template checked out on Windows
 * with {@code core.autocrlf=true} therefore yields a file with mixed line endings, which git refuses to
 * normalize, so every generated file shows up as modified when nothing but the line endings changed. Worse,
 * a file that merely got shorter than the checked-out version (LF instead of CRLF) is reported as modified
 * by {@code git status} on size alone. So the output is rewritten to exactly what a checkout produces:
 * CRLF where {@code core.autocrlf} is {@code true}, LF otherwise. The files come from the run's manifest
 * ({@code .openapi-generator/FILES}). Call it from {@code postProcess()}.
 */
final class LineEndings {

    private static final Logger LOGGER = LoggerFactory.getLogger(LineEndings.class);

    private LineEndings() {
    }

    static void normalize(CodegenConfig config) {
        Path root = Paths.get(config.outputFolder()).toAbsolutePath().normalize();
        Path manifest = root.resolve(".openapi-generator").resolve("FILES");

        if (!Files.isRegularFile(manifest)) {
            LOGGER.warn("No generator manifest at {}; line endings are not normalized.", manifest);
            return;
        }

        String target = checkoutLineEnding(root);
        int changed = 0;

        try {
            for (String line : Files.readAllLines(manifest)) {
                if (line.trim().isEmpty()) {
                    continue;
                }

                Path file = root.resolve(line.trim()).normalize();
                if (!Files.isRegularFile(file) || isBinary(file)) {
                    continue;
                }

                byte[] bytes = Files.readAllBytes(file);
                String text = new String(bytes, StandardCharsets.UTF_8);
                String normalized = text.replace("\r\n", "\n").replace('\r', '\n');
                if (!"\n".equals(target)) {
                    normalized = normalized.replace("\n", target);
                }

                if (!normalized.equals(text)) {
                    Files.write(file, normalized.getBytes(StandardCharsets.UTF_8));
                    changed++;
                }
            }
        } catch (IOException e) {
            throw new UncheckedIOException("Cannot normalize line endings under " + root, e);
        }

        // stdout, like the generator's own closing banner (see StaleOutput for why not the logger).
        System.out.println("Line endings: " + changed + " file(s) rewritten with "
            + ("\n".equals(target) ? "LF" : "CRLF") + " under " + root + ".");
    }

    /**
     * What git writes into the working tree of the output repository: CRLF with {@code core.autocrlf=true},
     * LF otherwise (including outside a repository, or without git on the path).
     */
    private static String checkoutLineEnding(Path root) {
        try {
            Process process = new ProcessBuilder("git", "config", "--get", "core.autocrlf")
                .directory(root.toFile())
                .redirectErrorStream(true)
                .start();

            String value;
            try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
                value = reader.readLine();
            }

            if (!process.waitFor(10, TimeUnit.SECONDS)) {
                process.destroy();
                return "\n";
            }

            return value != null && value.trim().toLowerCase(Locale.ROOT).equals("true") ? "\r\n" : "\n";
        } catch (IOException e) {
            return "\n";
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            return "\n";
        }
    }

    private static boolean isBinary(Path file) {
        String name = file.getFileName().toString().toLowerCase(Locale.ROOT);
        return name.endsWith(".png") || name.endsWith(".jpg") || name.endsWith(".jpeg") || name.endsWith(".gif")
            || name.endsWith(".ico") || name.endsWith(".jar") || name.endsWith(".zip") || name.endsWith(".gz")
            || name.endsWith(".tgz") || name.endsWith(".pdf") || name.endsWith(".woff") || name.endsWith(".woff2");
    }
}
