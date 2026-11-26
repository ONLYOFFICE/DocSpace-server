// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

// The gate a plugin leaves its sandbox through. It is run by this service, not left to the
// model to report: the project's build, its type check and the skill's validator are started
// here, and only when all three exit clean are the package and the sources taken. A failed check
// goes back to the model as the tool's result.
//
// What the gate is not: a defence against the model itself. The commands run in the sandbox,
// where the model can write any file, the project's own scripts and the copied validator
// included; the gate catches a plugin that does not build or does not load, and the user's
// approval of an installation is what stands between a package and the portal.

import { MAX_FILE_BYTES } from "../tools/documentAgentFiles.js";
import { getObject, getString, isObject } from "../narrow.js";
import { execReport, MAX_OUTPUT_CHARS } from "./agentTools.js";
import type { ToolContext } from "./agentTools.js";
import { execInSandbox, readSandboxFile, SandboxError } from "./sandboxClient.js";
import { SANDBOX_SKILL_DIR, SANDBOX_WORKSPACE } from "./sandboxSession.js";

/** A build of a plugin with its dependencies already installed takes well under this. */
const CHECK_TIMEOUT_SECONDS = 600;
const MANIFEST_LIMIT_BYTES = 1024 * 1024;
const PROBE_TIMEOUT_SECONDS = 30;
/** Where the sources are archived before they are read: outside every project. */
const SOURCES_ARCHIVE = `${SANDBOX_WORKSPACE}/.sources.zip`;

// The checks in the order they run, by the name the model and the user are told.
const CHECKS: [name: string, command: string][] = [
  ["build", "npm run build"],
  ["type check", "npm run typecheck"],
  ["validator", `node ${SANDBOX_SKILL_DIR}/scripts/validate-plugin.mjs . --invoke`],
];

// Without what `npm install` and the build put there: both are made again from the sources.
const ARCHIVE_COMMAND = `rm -f ${SOURCES_ARCHIVE} && zip -qr ${SOURCES_ARCHIVE} . -x 'node_modules/*' -x 'dist/*' -x '.git/*'`;

export interface DeliveredPlugin {
  /** The package's name, as a file name takes it. */
  name: string;
  version: string;
  dependencies: string[];
  devDependencies: string[];
  packageBytes: Uint8Array;
  sourcesBytes: Uint8Array;
}

export type DeliverOutcome =
  | { passed: true; plugin: DeliveredPlugin }
  /** `check` names what failed; `report` is what the model reads to fix it. */
  | { passed: false; check: string; report: string };

// A directory name as a generated project has it; nothing a shell would read as more than a name.
const PROJECT_DIR = /^[A-Za-z0-9_-][A-Za-z0-9._-]*(\/[A-Za-z0-9_-][A-Za-z0-9._-]*)*$/;
const MAX_PROJECT_DIR_CHARS = 200;

/**
 * A project directory: under the workspace and not the workspace itself, every part a plain
 * name that does not start with a dot (so no `..` and no hidden directory).
 */
export function projectDirOf(value: unknown): string | undefined {
  if (typeof value !== "string" || value.length > MAX_PROJECT_DIR_CHARS) {
    return undefined;
  }
  const dir = value.replace(/\/+$/, "");
  const prefix = `${SANDBOX_WORKSPACE}/`;
  return dir.startsWith(prefix) && PROJECT_DIR.test(dir.slice(prefix.length)) ? dir : undefined;
}

/** What a manifest may say of itself and of each dependency, as it goes into the task's result. */
const MAX_MANIFEST_TEXT_CHARS = 100;
const MAX_LISTED_DEPENDENCIES = 100;
const VERSION = /^[0-9A-Za-z.+-]+$/;
const DEPENDENCY = /^(@[a-z0-9~][a-z0-9._~-]*\/)?[a-z0-9][a-z0-9._~-]*$/i;

function fileName(packageName: string): string {
  const name = packageName
    .slice(0, MAX_MANIFEST_TEXT_CHARS)
    .replace(/^@/, "")
    .replace(/[^a-z0-9._-]+/gi, "-")
    .replace(/^[-.]+|[-.]+$/g, "");
  return name === "" ? "plugin" : name;
}

/** The dependency names of a manifest that are package names, a bounded number of them. */
function namesOf(manifest: Record<string, unknown>, key: string): string[] {
  return Object.keys(getObject(manifest, key) ?? {})
    .filter((name) => name.length <= MAX_MANIFEST_TEXT_CHARS && DEPENDENCY.test(name))
    .sort()
    .slice(0, MAX_LISTED_DEPENDENCIES);
}

function versionOf(manifest: Record<string, unknown>): string {
  const version = getString(manifest, "version") ?? "";
  return version.length <= MAX_MANIFEST_TEXT_CHARS && VERSION.test(version) ? version : "0.0.0";
}

async function readOr(
  context: ToolContext,
  file: string,
  limit: number,
): Promise<Uint8Array | "absent" | "tooLarge"> {
  try {
    return await readSandboxFile(context.config, context.session.id, file, limit, context.signal);
  } catch (err) {
    if (err instanceof SandboxError && (err.kind === "absent" || err.kind === "tooLarge")) {
      return err.kind;
    }
    throw err;
  }
}

/**
 * Checks the project and, when it passes, takes the package and the sources out of the sandbox.
 * A sandbox that is gone or unreachable rejects; everything the model can fix is an outcome.
 */
export async function deliverPlugin(
  context: ToolContext,
  projectDir: string,
): Promise<DeliverOutcome> {
  const run = (command: string) =>
    execInSandbox(context.config, context.session.id, command, {
      cwd: projectDir,
      env: context.session.env,
      timeoutSeconds: CHECK_TIMEOUT_SECONDS,
      maxOutputChars: MAX_OUTPUT_CHARS,
      signal: context.signal,
    });

  // The directory goes into `cwd` and, a plain name by now, into this one test. Asked for first,
  // so that a wrong one is something the model reads and not a refusal of the sandbox that ends
  // the task.
  const exists = await execInSandbox(context.config, context.session.id, `test -d ${projectDir}`, {
    cwd: SANDBOX_WORKSPACE,
    timeoutSeconds: PROBE_TIMEOUT_SECONDS,
    maxOutputChars: MAX_OUTPUT_CHARS,
    signal: context.signal,
  });
  if (exists.code !== 0) {
    return { passed: false, check: "project", report: `${projectDir} is not a directory.` };
  }

  for (const [check, command] of CHECKS) {
    const result = await run(command);
    if (result.code !== 0) {
      return {
        passed: false,
        check,
        report: `The ${check} failed (${command}).\n${execReport(result)}`,
      };
    }
  }

  const manifestBytes = await readOr(context, `${projectDir}/package.json`, MANIFEST_LIMIT_BYTES);
  let manifest: unknown;
  try {
    manifest =
      typeof manifestBytes === "string"
        ? undefined
        : JSON.parse(Buffer.from(manifestBytes).toString("utf8"));
  } catch {
    manifest = undefined;
  }
  if (!isObject(manifest)) {
    return {
      passed: false,
      check: "package.json",
      report: `${projectDir}/package.json cannot be read as JSON.`,
    };
  }

  const packageBytes = await readOr(context, `${projectDir}/dist/plugin.zip`, MAX_FILE_BYTES);
  if (typeof packageBytes === "string") {
    return {
      passed: false,
      check: "package",
      report:
        packageBytes === "absent"
          ? `The build left no ${projectDir}/dist/plugin.zip.`
          : `${projectDir}/dist/plugin.zip is over ${MAX_FILE_BYTES} bytes.`,
    };
  }

  const archived = await run(ARCHIVE_COMMAND);
  const sourcesBytes =
    archived.code === 0 ? await readOr(context, SOURCES_ARCHIVE, MAX_FILE_BYTES) : "absent";
  if (typeof sourcesBytes === "string") {
    return {
      passed: false,
      check: "sources archive",
      report:
        sourcesBytes === "tooLarge"
          ? `The sources are over ${MAX_FILE_BYTES} bytes as an archive: remove what is not source.`
          : `The sources could not be archived.\n${execReport(archived)}`,
    };
  }

  return {
    passed: true,
    plugin: {
      name: fileName(getString(manifest, "name") ?? ""),
      version: versionOf(manifest),
      dependencies: namesOf(manifest, "dependencies"),
      devDependencies: namesOf(manifest, "devDependencies"),
      packageBytes,
      sourcesBytes,
    },
  };
}
