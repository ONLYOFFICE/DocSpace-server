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

import java.io.IOException;
import java.io.UncheckedIOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;
import java.util.function.Predicate;
import java.util.stream.Stream;

/**
 * Removes what an earlier run generated and this one did not.
 * <p>
 * openapi-generator writes files and never deletes any: after a schema is renamed its old model and doc files stay
 * behind in the SDK, compile fine, and ship in the package. The manifest the generator writes at the end of a run,
 * {@code .openapi-generator/FILES}, lists everything the run produced, so every file under the generated folders
 * that the manifest does not list is a leftover. Only the folders the generator owns are swept - where it puts
 * the api classes, the models and their docs - so hand-written files elsewhere in the SDK are never touched.
 * Call it from {@code postProcess()}, which the generator runs after the manifest is written.
 */
final class StaleOutput {

    private static final Logger LOGGER = LoggerFactory.getLogger(StaleOutput.class);

    private StaleOutput() {
    }

    /**
     * Sweeps the api, model and doc folders. A folder that is the output root itself is skipped: it holds the
     * whole SDK, not just generated files. A generator that writes models into the root - Go does - passes a
     * filter through {@link #delete(CodegenConfig, Predicate)} instead.
     */
    static void delete(CodegenConfig config) {
        delete(config, null);
    }

    /**
     * @param rootFileFilter which files of the output root count as generated, by file name, when one of the
     *                       generated folders is the root itself; null skips the root altogether.
     */
    static void delete(CodegenConfig config, Predicate<String> rootFileFilter) {
        Path root = Paths.get(config.outputFolder()).toAbsolutePath().normalize();
        Path manifest = root.resolve(".openapi-generator").resolve("FILES");

        if (!Files.isRegularFile(manifest)) {
            LOGGER.warn("No generator manifest at {}; stale files are not removed.", manifest);
            return;
        }

        Set<Path> generated = new LinkedHashSet<>();
        try {
            for (String line : Files.readAllLines(manifest)) {
                if (!line.trim().isEmpty()) {
                    generated.add(root.resolve(line.trim()).normalize());
                }
            }
        } catch (IOException e) {
            throw new UncheckedIOException("Cannot read the generator manifest " + manifest, e);
        }

        Set<Path> folders = new LinkedHashSet<>();
        for (String folder : List.of(config.apiFileFolder(), config.modelFileFolder(),
                config.apiDocFileFolder(), config.modelDocFileFolder())) {
            if (folder != null && !folder.isEmpty()) {
                folders.add(Paths.get(folder).toAbsolutePath().normalize());
            }
        }

        int deleted = 0;
        for (Path folder : folders) {
            if (!folder.startsWith(root) || !Files.isDirectory(folder)) {
                continue;
            }

            boolean isRoot = folder.equals(root);
            if (isRoot && rootFileFilter == null) {
                System.out.println("Generated folder is the output root " + root + "; not swept.");
                continue;
            }

            try (Stream<Path> stream = isRoot ? Files.list(folder) : Files.walk(folder)) {
                for (Path file : (Iterable<Path>) stream::iterator) {
                    if (!Files.isRegularFile(file)) {
                        continue;
                    }
                    if (isRoot && !rootFileFilter.test(file.getFileName().toString())) {
                        continue;
                    }
                    if (generated.contains(file.normalize())) {
                        continue;
                    }

                    Files.delete(file);
                    deleted++;
                    // stdout, like the generator's own closing banner: the generator's logback config keeps
                    // loggers outside org.openapitools at warn, so an info line here would never be seen.
                    System.out.println("Deleted stale file " + root.relativize(file));
                }
            } catch (IOException e) {
                throw new UncheckedIOException("Cannot sweep " + folder, e);
            }
        }

        System.out.println("Stale output: " + deleted + " file(s) removed under " + root
            + " (" + generated.size() + " generated files in the manifest).");
    }
}
