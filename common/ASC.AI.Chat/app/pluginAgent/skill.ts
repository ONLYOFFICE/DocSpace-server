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

// The plugin skill (`apps-plugin-sdk` of the agent-skills repository, a git submodule of this
// service) as a plugin task uses it. Its texts stay here: `SKILL.md` opens the task's system
// prompt and the model reads the references through a tool. Its scripts and templates have to
// run next to the project, so they are copied into the task's sandbox.
//
// A checkout without the submodule, or an image built without it, has no skill: the plugin
// agent is then off, said once in the log, and the service runs as before.

import fs from "fs/promises";
import path from "path";
import { fileURLToPath } from "url";

import logger from "../log.js";
import { readProp } from "../narrow.js";

/** Where the submodule puts the skill, next to the service's own code. */
export const PLUGIN_SKILL_DIR = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
  "..",
  "skills",
  "agent-skills",
  "skills",
  "apps-plugin-sdk",
);

/** The parts of the skill that run in the sandbox. */
const SANDBOX_PARTS = ["scripts", "templates"];
/** The tests of the skill's own validator: nothing a task runs. */
const NOT_COPIED = new Set(["validator-tests"]);
/** What the model may read through `read_skill`, next to `SKILL.md`. */
const READABLE_PART = "references";
/** What the file system answers for a path that names no file. */
const NO_SUCH_FILE = new Set(["ENOENT", "ENOTDIR", "ENAMETOOLONG", "ERR_INVALID_ARG_VALUE"]);
/** A reference is a page of text; one far over that is not what the skill ships. */
export const MAX_SKILL_FILE_BYTES = 512 * 1024;

export interface PluginSkill {
  dir: string;
  /** The text of `SKILL.md`. */
  instructions: string;
  /** The files to copy into a sandbox, as paths relative to the skill, with `/` between the parts. */
  sandboxFiles: string[];
}

async function listFiles(dir: string, relative: string): Promise<string[]> {
  const files: string[] = [];
  for (const entry of await fs.readdir(path.join(dir, relative), { withFileTypes: true })) {
    const child = `${relative}/${entry.name}`;
    if (entry.isDirectory()) {
      if (!NOT_COPIED.has(entry.name)) {
        files.push(...(await listFiles(dir, child)));
      }
    } else if (entry.isFile()) {
      files.push(child);
    }
  }
  return files;
}

// Whether the missing skill was reported: once for the process, not per portal or request, as
// it is a fact about the installation (ts-ai-failure-paths.md §8).
let reportedMissing = false;

/**
 * Whether the skill is installed: its `SKILL.md` is there. Cheap, as it is asked on every chat
 * round of an installation with a plugin agent, before the tools are offered.
 */
export async function pluginSkillPresent(dir: string = PLUGIN_SKILL_DIR): Promise<boolean> {
  const found = await loadOrMissing(dir, async () => {
    await fs.access(path.join(dir, "SKILL.md"));
    return true;
  });
  return found === true;
}

/**
 * The skill from `dir`, or undefined when it is not there. Read from the disk when a task
 * starts: nothing of it is kept between tasks or portals.
 */
export async function loadPluginSkill(
  dir: string = PLUGIN_SKILL_DIR,
): Promise<PluginSkill | undefined> {
  return loadOrMissing(dir, async () => {
    const instructions = await fs.readFile(path.join(dir, "SKILL.md"), "utf8");
    const sandboxFiles: string[] = [];
    for (const part of SANDBOX_PARTS) {
      sandboxFiles.push(...(await listFiles(dir, part)));
    }
    return { dir, instructions, sandboxFiles: sandboxFiles.sort() };
  });
}

/** What `read` answers, or undefined, said once, when a file it needs is not there. */
async function loadOrMissing<T>(dir: string, read: () => Promise<T>): Promise<T | undefined> {
  try {
    return await read();
  } catch (err) {
    if (readProp(err, "code") !== "ENOENT") {
      throw err;
    }
    if (!reportedMissing) {
      reportedMissing = true;
      logger.warn(
        `pluginAgent: the plugin skill is not at ${dir} (is the agent-skills submodule checked out?); the plugin agent is off`,
      );
    }
    return undefined;
  }
}

/** For tests: report a missing skill again. */
export function forgetMissingSkill(): void {
  reportedMissing = false;
}

/**
 * A text of the skill for the model: `SKILL.md` or a file under `references/`, named relative
 * to the skill. Answers the text, or the reason it cannot be read, as a result the model reads.
 */
export async function readSkillFile(skill: PluginSkill, requested: string): Promise<string> {
  const normalized = path.posix.normalize(requested.replaceAll("\\", "/")).replace(/^\.\//, "");
  const allowed = normalized === "SKILL.md" || normalized.startsWith(`${READABLE_PART}/`);
  // `normalize` has folded every `..` it could: one left over leads out of the skill.
  if (!allowed || normalized.split("/").includes("..")) {
    return `"${requested}" is not a file of the skill: read SKILL.md or a path under ${READABLE_PART}/.`;
  }
  try {
    const file = path.join(skill.dir, ...normalized.split("/"));
    const stat = await fs.stat(file);
    if (!stat.isFile()) {
      return `"${requested}" is a directory: name one of its files.`;
    }
    if (stat.size > MAX_SKILL_FILE_BYTES) {
      return `"${requested}" is too large to read.`;
    }
    const text = await fs.readFile(file, "utf8");
    return text === "" ? `"${requested}" is empty.` : text;
  } catch (err) {
    // A path the model made up that leads through a file, is too long or holds a character no
    // path may: its mistake to read about, not a failure of the task.
    if (!NO_SUCH_FILE.has(String(readProp(err, "code")))) {
      throw err;
    }
    return `"${requested}" does not exist in the skill.`;
  }
}
