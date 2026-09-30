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
import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.io.UncheckedIOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.concurrent.TimeUnit;

/**
 * Gives every generated text file one line ending, the one git checks that file out with in the output
 * repository.
 * <p>
 * openapi-generator copies the line endings of the template into the output, and the text it splices in -
 * descriptions from the document, partials, generated code - carries LF. A template checked out with CRLF
 * therefore yields a file with mixed line endings, which git refuses to normalize, so every generated file
 * shows up as modified when nothing but the line endings changed. Worse, a file that merely differs in size
 * from the checked-out version (LF instead of CRLF, or the reverse) is reported as modified by
 * {@code git status} on size alone. So the output is rewritten to exactly what a checkout produces, and git
 * itself is asked what that is, file by file:
 * <ul>
 *   <li>the {@code eol} attribute of the file ({@code .gitattributes}: {@code * text=auto eol=lf},
 *       {@code *.bat text eol=crlf}) decides where it is set - that is what makes the result the same on
 *       every machine;</li>
 *   <li>otherwise {@code core.autocrlf}: CRLF when it is {@code true}, LF when it is not - the per-machine
 *       behaviour of a repository that has no attributes;</li>
 *   <li>a file git treats as binary ({@code -text}) is left alone.</li>
 * </ul>
 * The files come from the run's manifest ({@code .openapi-generator/FILES}). Call it from
 * {@code postProcess()}.
 */
final class LineEndings {

    private static final Logger LOGGER = LoggerFactory.getLogger(LineEndings.class);

    private static final String LF = "\n";
    private static final String CRLF = "\r\n";

    private LineEndings() {
    }

    static void normalize(CodegenConfig config) {
        Path root = Paths.get(config.outputFolder()).toAbsolutePath().normalize();
        Path manifest = root.resolve(".openapi-generator").resolve("FILES");

        if (!Files.isRegularFile(manifest)) {
            LOGGER.warn("No generator manifest at {}; line endings are not normalized.", manifest);
            return;
        }

        List<String> files = new ArrayList<>();
        try {
            for (String line : Files.readAllLines(manifest)) {
                String relative = line.trim().replace('\\', '/');
                if (!relative.isEmpty() && Files.isRegularFile(root.resolve(relative)) && !isBinary(relative)) {
                    files.add(relative);
                }
            }
        } catch (IOException e) {
            throw new UncheckedIOException("Cannot read the generator manifest " + manifest, e);
        }

        String fallback = autoCrlf(root) ? CRLF : LF;
        Map<String, String[]> attributes = attributes(root, files);

        int toLf = 0;
        int toCrlf = 0;
        try {
            for (String relative : files) {
                String[] attribute = attributes.get(relative);
                String text = attribute == null ? null : attribute[0];
                String eol = attribute == null ? null : attribute[1];

                if ("unset".equals(text)) {
                    continue;
                }

                String target = "lf".equals(eol) ? LF : "crlf".equals(eol) ? CRLF : fallback;

                Path file = root.resolve(relative);
                String content = new String(Files.readAllBytes(file), StandardCharsets.UTF_8);
                String normalized = content.replace("\r\n", "\n").replace('\r', '\n');
                if (CRLF.equals(target)) {
                    normalized = normalized.replace("\n", CRLF);
                }

                if (!normalized.equals(content)) {
                    Files.write(file, normalized.getBytes(StandardCharsets.UTF_8));
                    if (CRLF.equals(target)) {
                        toCrlf++;
                    } else {
                        toLf++;
                    }
                }
            }
        } catch (IOException e) {
            throw new UncheckedIOException("Cannot normalize line endings under " + root, e);
        }

        // stdout, like the generator's own closing banner (see StaleOutput for why not the logger).
        System.out.println("Line endings: " + toLf + " file(s) rewritten with LF, " + toCrlf + " with CRLF under "
            + root + " (" + (attributes.isEmpty() ? "no eol attributes" : "eol attributes")
            + ", core.autocrlf " + (CRLF.equals(fallback) ? "true" : "not true") + ").");
    }

    /**
     * The {@code text} and {@code eol} attributes of each file, as {@code git check-attr} reports them; files
     * for which neither is specified are absent, and so is everything when git cannot be asked.
     */
    private static Map<String, String[]> attributes(Path root, List<String> files) {
        Map<String, String[]> result = new HashMap<>();
        if (files.isEmpty()) {
            return result;
        }

        try {
            Process process = new ProcessBuilder("git", "check-attr", "--stdin", "text", "eol")
                .directory(root.toFile())
                .redirectErrorStream(true)
                .start();

            // Written from a thread of its own: git answers as it reads, and with a few thousand paths
            // either pipe would fill up while the other end waits.
            Thread writer = new Thread(() -> {
                try (BufferedWriter out = new BufferedWriter(new OutputStreamWriter(process.getOutputStream(), StandardCharsets.UTF_8))) {
                    for (String file : files) {
                        out.write(file);
                        out.write('\n');
                    }
                } catch (IOException ignored) {
                    // git went away; the reader below sees the end of its output.
                }
            });
            writer.setDaemon(true);
            writer.start();

            try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
                String line;
                while ((line = reader.readLine()) != null) {
                    // "<path>: <attribute>: <value>"
                    int value = line.lastIndexOf(": ");
                    int name = value < 0 ? -1 : line.lastIndexOf(": ", value - 1);
                    if (name < 0) {
                        continue;
                    }

                    String path = unquote(line.substring(0, name));
                    String attribute = line.substring(name + 2, value);
                    String setting = line.substring(value + 2).trim();
                    if ("unspecified".equals(setting)) {
                        continue;
                    }

                    String[] entry = result.computeIfAbsent(path, k -> new String[2]);
                    if ("text".equals(attribute)) {
                        entry[0] = setting;
                    } else if ("eol".equals(attribute)) {
                        entry[1] = setting;
                    }
                }
            }

            if (!process.waitFor(30, TimeUnit.SECONDS)) {
                process.destroy();
            }
        } catch (IOException e) {
            LOGGER.warn("git check-attr is not available under {}; falling back to core.autocrlf.", root);
            result.clear();
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            result.clear();
        }

        return result;
    }

    private static String unquote(String path) {
        return path.length() > 1 && path.startsWith("\"") && path.endsWith("\"")
            ? path.substring(1, path.length() - 1)
            : path;
    }

    private static boolean autoCrlf(Path root) {
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
                return false;
            }

            return value != null && value.trim().toLowerCase(Locale.ROOT).equals("true");
        } catch (IOException e) {
            return false;
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            return false;
        }
    }

    private static boolean isBinary(String file) {
        String name = file.toLowerCase(Locale.ROOT);
        return name.endsWith(".png") || name.endsWith(".jpg") || name.endsWith(".jpeg") || name.endsWith(".gif")
            || name.endsWith(".ico") || name.endsWith(".jar") || name.endsWith(".zip") || name.endsWith(".gz")
            || name.endsWith(".tgz") || name.endsWith(".pdf") || name.endsWith(".woff") || name.endsWith(".woff2");
    }
}
