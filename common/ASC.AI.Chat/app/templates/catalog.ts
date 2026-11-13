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

/**
 * The catalog of templates shipped with the service, read from the
 * `templates` folder next to `app`: agents (`agents/<category>/<name>.json`),
 * skill sets (`skill-sets/<name>/template.json` and its Markdown skills) and
 * knowledge bases (`knowledge-bases/<name>/template.json` and its files). A
 * template's id is its path under the folder, without the extension for an
 * agent. An agent names its skill sets and knowledge bases by their ids, and
 * each of those is a template of its own, installable without the agent.
 *
 * The folder is read once and checked as a whole: a broken entry is a
 * packaging mistake, so it fails the read instead of hiding one template.
 */

import { readdir, readFile, stat } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { getArray, getString, isObject } from "../narrow.js";

export type SourceTemplateKind = "skillSet" | "knowledgeBase";

export interface SourceTemplate {
  id: string;
  kind: SourceTemplateKind;
  name: string;
  description: string;
  category: string;
  /** File names inside the template's folder, in the order they are uploaded. */
  files: string[];
}

export interface AgentTemplate {
  id: string;
  kind: "agent";
  name: string;
  description: string;
  category: string;
  instructions: string;
  /** Ids of the skill set templates the agent is linked to. */
  skills: string[];
  /** Ids of the knowledge base templates the agent is linked to. */
  knowledges: string[];
}

export type Template = AgentTemplate | SourceTemplate;

export interface TemplateCatalog {
  agents: AgentTemplate[];
  skillSets: SourceTemplate[];
  knowledgeBases: SourceTemplate[];
}

export const TEMPLATES_ROOT = path.join(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
  "..",
  "templates",
);

const AGENTS_DIR = "agents";
const SOURCE_DIRS: Record<SourceTemplateKind, string> = {
  skillSet: "skill-sets",
  knowledgeBase: "knowledge-bases",
};
const MANIFEST = "template.json";

async function subfolders(dir: string): Promise<string[]> {
  const entries = await readdir(dir, { withFileTypes: true }).catch(() => []);
  return entries
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name)
    .sort();
}

async function readJson(file: string): Promise<Record<string, unknown>> {
  const raw: unknown = JSON.parse(await readFile(file, "utf8"));
  if (!isObject(raw)) {
    throw new Error(`Template ${file} is not a JSON object`);
  }
  return raw;
}

function requiredString(raw: Record<string, unknown>, key: string, file: string): string {
  const value = getString(raw, key)?.trim();
  if (!value) {
    throw new Error(`Template ${file} has no "${key}"`);
  }
  return value;
}

function stringList(raw: Record<string, unknown>, key: string, file: string): string[] {
  const list = getArray(raw, key);
  if (!list || !list.every((item): item is string => typeof item === "string" && item !== "")) {
    throw new Error(`Template ${file} has no list of strings in "${key}"`);
  }
  return list;
}

async function readSources(root: string, kind: SourceTemplateKind): Promise<SourceTemplate[]> {
  const dir = SOURCE_DIRS[kind];
  const templates: SourceTemplate[] = [];
  for (const name of await subfolders(path.join(root, dir))) {
    const manifest = path.join(root, dir, name, MANIFEST);
    const raw = await readJson(manifest);
    const files = stringList(raw, "files", manifest);
    for (const file of files) {
      // A file is named, never addressed: nothing outside the folder is read.
      if (file !== path.basename(file) || file === MANIFEST) {
        throw new Error(`Template ${manifest} names a file outside its folder: ${file}`);
      }
      if (kind === "skillSet" && !file.toLowerCase().endsWith(".md")) {
        throw new Error(`Template ${manifest} holds a skill that is not Markdown: ${file}`);
      }
      const info = await stat(path.join(root, dir, name, file)).catch(() => undefined);
      if (!info?.isFile()) {
        throw new Error(`Template ${manifest} names a missing file: ${file}`);
      }
    }
    if (files.length === 0) {
      throw new Error(`Template ${manifest} has no files`);
    }
    templates.push({
      id: `${dir}/${name}`,
      kind,
      name: requiredString(raw, "name", manifest),
      description: requiredString(raw, "description", manifest),
      category: requiredString(raw, "category", manifest),
      files,
    });
  }
  return templates;
}

async function readAgents(root: string, known: Set<string>): Promise<AgentTemplate[]> {
  const templates: AgentTemplate[] = [];
  for (const category of await subfolders(path.join(root, AGENTS_DIR))) {
    const dir = path.join(root, AGENTS_DIR, category);
    const names = (await readdir(dir)).filter((name) => name.endsWith(".json")).sort();
    for (const name of names) {
      const file = path.join(dir, name);
      const raw = await readJson(file);
      const links = (key: string, prefix: string): string[] => {
        const ids = stringList(raw, key, file);
        for (const id of ids) {
          if (!id.startsWith(`${prefix}/`) || !known.has(id)) {
            throw new Error(`Template ${file} links an unknown template in "${key}": ${id}`);
          }
        }
        return ids;
      };
      templates.push({
        id: `${AGENTS_DIR}/${category}/${name.slice(0, -".json".length)}`,
        kind: "agent",
        name: requiredString(raw, "name", file),
        description: requiredString(raw, "description", file),
        category: requiredString(raw, "category", file),
        instructions: requiredString(raw, "instructions", file),
        skills: links("skills", SOURCE_DIRS.skillSet),
        knowledges: links("knowledges", SOURCE_DIRS.knowledgeBase),
      });
    }
  }
  return templates;
}

/** Reads and checks the whole folder. */
export async function readTemplateCatalog(root: string = TEMPLATES_ROOT): Promise<TemplateCatalog> {
  const skillSets = await readSources(root, "skillSet");
  const knowledgeBases = await readSources(root, "knowledgeBase");
  const known = new Set([...skillSets, ...knowledgeBases].map((template) => template.id));
  return { agents: await readAgents(root, known), skillSets, knowledgeBases };
}

let cached: Promise<TemplateCatalog> | undefined;

/** The shipped catalog, read once per process; a failed read is tried again. */
export function getTemplateCatalog(): Promise<TemplateCatalog> {
  cached ??= readTemplateCatalog().catch((err: unknown) => {
    cached = undefined;
    throw err;
  });
  return cached;
}

export function findTemplate(catalog: TemplateCatalog, id: string): Template | undefined {
  return [...catalog.agents, ...catalog.skillSets, ...catalog.knowledgeBases].find(
    (template) => template.id === id,
  );
}

/** The bytes of one file of a skill set or a knowledge base template. */
export function readTemplateFile(
  template: SourceTemplate,
  file: string,
  root: string = TEMPLATES_ROOT,
): Promise<Uint8Array> {
  if (!template.files.includes(file)) {
    throw new Error(`Template ${template.id} has no file ${file}`);
  }
  return readFile(path.join(root, ...template.id.split("/"), file));
}
