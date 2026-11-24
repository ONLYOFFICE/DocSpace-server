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
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

// The catalog reader over folders written for each test, and the per-process memo of the
// shipped catalog.

import { chmod, mkdtemp, mkdir, symlink, writeFile } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { afterEach, describe, expect, it, vi } from "vitest";
import { findTemplate, readTemplateCatalog, readTemplateFile, TEMPLATES_ROOT } from "../catalog.js";
import type { SourceTemplate } from "../catalog.js";

async function folder(files: Record<string, string>): Promise<string> {
  const root = await mkdtemp(path.join(os.tmpdir(), "templates-"));
  for (const [name, content] of Object.entries(files)) {
    await mkdir(path.dirname(path.join(root, name)), { recursive: true });
    await writeFile(path.join(root, name), content);
  }
  return root;
}

const SET = {
  name: "Set",
  description: "A set",
  category: "Document",
  color: "a1b2c3",
  cover: "shield",
  files: ["a.md", "b.md"],
};
const BASE = { name: "Base", description: "A base", category: "Tables", files: ["Guide.docx"] };
const AGENT = {
  name: "Agent",
  description: "An agent",
  category: "Document",
  color: "6191F2",
  cover: "star_1",
  instructions: "Do it",
  skills: ["skill-sets/set"],
  knowledges: ["knowledge-bases/base"],
};

const SHIPPED_FILES = {
  "skill-sets/set/a.md": "# A",
  "skill-sets/set/b.md": "# B",
  "knowledge-bases/base/Guide.docx": "docx",
};

function tree(overrides: Record<string, unknown> = {}): Record<string, string> {
  const files: Record<string, string> = {
    ...SHIPPED_FILES,
    "skill-sets/set/template.json": JSON.stringify(SET),
    "knowledge-bases/base/template.json": JSON.stringify(BASE),
    "agents/document/agent.json": JSON.stringify(AGENT),
  };
  for (const [name, value] of Object.entries(overrides)) {
    if (value === undefined) {
      delete files[name];
    } else {
      files[name] = typeof value === "string" ? value : JSON.stringify(value);
    }
  }
  return files;
}

function without<T extends Record<string, unknown>>(raw: T, key: string): Record<string, unknown> {
  const { [key]: _dropped, ...rest } = raw;
  return rest;
}

describe("readTemplateCatalog", () => {
  it("reads every kind of template, its look upper-cased and its files in their order", async () => {
    const root = await folder(tree());

    expect(await readTemplateCatalog(root)).toEqual({
      agents: [
        {
          id: "agents/document/agent",
          kind: "agent",
          name: "Agent",
          description: "An agent",
          category: "Document",
          color: "6191F2",
          cover: "star_1",
          instructions: "Do it",
          skills: ["skill-sets/set"],
          knowledges: ["knowledge-bases/base"],
        },
      ],
      skillSets: [
        {
          id: "skill-sets/set",
          kind: "skillSet",
          name: "Set",
          description: "A set",
          category: "Document",
          color: "A1B2C3",
          cover: "shield",
          files: ["a.md", "b.md"],
        },
      ],
      knowledgeBases: [
        {
          id: "knowledge-bases/base",
          kind: "knowledgeBase",
          name: "Base",
          description: "A base",
          category: "Tables",
          files: ["Guide.docx"],
        },
      ],
    });
  });

  it("trims the texts, sorts the folders and the agents, and skips what is not a template", async () => {
    const root = await folder(
      tree({
        "agents/document/b.json": { ...AGENT, name: "  B  " },
        "agents/document/a.json": { ...AGENT, name: "A" },
        "agents/document/notes.txt": "not a template",
        "agents/readme.json": "{}",
        "agents/document/agent.json": undefined,
        "skill-sets/readme.md": "not a folder",
        "agents/chart/z.json": { ...AGENT, name: "Z", category: "Chart" },
      }),
    );

    const catalog = await readTemplateCatalog(root);

    expect(catalog.agents.map((a) => [a.id, a.name])).toEqual([
      ["agents/chart/z", "Z"],
      ["agents/document/a", "A"],
      ["agents/document/b", "B"],
    ]);
    expect(catalog.skillSets.map((s) => s.id)).toEqual(["skill-sets/set"]);
  });

  it("reads a folder without the template folders as an empty catalog", async () => {
    expect(await readTemplateCatalog(await folder({ "readme.md": "x" }))).toEqual({
      agents: [],
      skillSets: [],
      knowledgeBases: [],
    });
  });

  it("fails the read when a template folder cannot be listed instead of reading it as empty", async () => {
    const root = await folder({ "skill-sets": "a file, not a folder" });

    await expect(readTemplateCatalog(root)).rejects.toMatchObject({ code: "ENOTDIR" });
  });

  // A process running as root reads a folder whatever its mode, so the refusal cannot be made there.
  it.skipIf(process.getuid?.() === 0)(
    "fails the read when a template folder may not be listed instead of reading it as empty",
    async () => {
      const root = await folder(tree());
      await chmod(path.join(root, "skill-sets"), 0o000);
      try {
        await expect(readTemplateCatalog(root)).rejects.toMatchObject({ code: "EACCES" });
      } finally {
        await chmod(path.join(root, "skill-sets"), 0o755);
      }
    },
  );

  it("fails the read when a named file cannot be examined instead of calling it missing", async () => {
    const root = await folder(tree({ "skill-sets/set/a.md": undefined }));
    await symlink("a.md", path.join(root, "skill-sets/set/a.md"));

    await expect(readTemplateCatalog(root)).rejects.toMatchObject({ code: "ELOOP" });
  });

  const SET_FILE = "skill-sets/set/template.json";
  const BASE_FILE = "knowledge-bases/base/template.json";
  const AGENT_FILE = "agents/document/agent.json";
  it.each([
    [SET_FILE, "[]", "is not a JSON object"],
    [SET_FILE, "null", "is not a JSON object"],
    [SET_FILE, '"set"', "is not a JSON object"],
    [SET_FILE, without(SET, "name"), 'has no "name"'],
    [SET_FILE, { ...SET, name: null }, 'has no "name"'],
    [SET_FILE, { ...SET, name: 5 }, 'has no "name"'],
    [SET_FILE, { ...SET, name: "  " }, 'has no "name"'],
    [SET_FILE, without(SET, "description"), 'has no "description"'],
    [SET_FILE, { ...SET, description: "" }, 'has no "description"'],
    [SET_FILE, without(SET, "category"), 'has no "category"'],
    [SET_FILE, { ...SET, category: ["Document"] }, 'has no "category"'],
    [SET_FILE, without(SET, "files"), 'has no list of strings in "files"'],
    [SET_FILE, { ...SET, files: null }, 'has no list of strings in "files"'],
    [SET_FILE, { ...SET, files: "a.md" }, 'has no list of strings in "files"'],
    [SET_FILE, { ...SET, files: ["a.md", ""] }, 'has no list of strings in "files"'],
    [SET_FILE, { ...SET, files: ["a.md", 5] }, 'has no list of strings in "files"'],
    [SET_FILE, { ...SET, files: [] }, "has no files"],
    [SET_FILE, { ...SET, files: ["../b.md"] }, "names a file outside its folder: ../b.md"],
    [SET_FILE, { ...SET, files: ["sub/b.md"] }, "names a file outside its folder: sub/b.md"],
    [SET_FILE, { ...SET, files: ["template.json"] }, "names a file outside its folder: template.json"],
    [SET_FILE, { ...SET, files: ["c.md"] }, "names a missing file: c.md"],
    [SET_FILE, { ...SET, files: ["Guide.docx"] }, "holds a skill that is not Markdown: Guide.docx"],
    [SET_FILE, { ...SET, color: null }, 'has a "color" that is not six hexadecimal digits'],
    [SET_FILE, { ...SET, color: "" }, 'has a "color" that is not six hexadecimal digits'],
    [SET_FILE, { ...SET, color: 0xa1b2c3 }, 'has a "color" that is not six hexadecimal digits'],
    [SET_FILE, { ...SET, color: "#a1b2c3" }, 'has a "color" that is not six hexadecimal digits'],
    [SET_FILE, { ...SET, color: "abc" }, 'has a "color" that is not six hexadecimal digits'],
    [SET_FILE, { ...SET, color: "a1b2c3d" }, 'has a "color" that is not six hexadecimal digits'],
    [SET_FILE, { ...SET, color: "g1b2c3" }, 'has a "color" that is not six hexadecimal digits'],
    [SET_FILE, { ...SET, cover: null }, 'has a "cover" that is not a room cover id'],
    [SET_FILE, { ...SET, cover: "" }, 'has a "cover" that is not a room cover id'],
    [SET_FILE, { ...SET, cover: 3 }, 'has a "cover" that is not a room cover id'],
    [SET_FILE, { ...SET, cover: "Shield" }, 'has a "cover" that is not a room cover id'],
    [SET_FILE, { ...SET, cover: "../x" }, 'has a "cover" that is not a room cover id'],
    [SET_FILE, { ...SET, cover: "a".repeat(51) }, 'has a "cover" that is not a room cover id'],
    [BASE_FILE, without(BASE, "files"), 'has no list of strings in "files"'],
    [BASE_FILE, { ...BASE, files: ["Missing.docx"] }, "names a missing file: Missing.docx"],
    [AGENT_FILE, without(AGENT, "name"), 'has no "name"'],
    [AGENT_FILE, without(AGENT, "instructions"), 'has no "instructions"'],
    [AGENT_FILE, { ...AGENT, instructions: null }, 'has no "instructions"'],
    [AGENT_FILE, { ...AGENT, instructions: " \n " }, 'has no "instructions"'],
    [AGENT_FILE, without(AGENT, "skills"), 'has no list of strings in "skills"'],
    [AGENT_FILE, { ...AGENT, skills: "skill-sets/set" }, 'has no list of strings in "skills"'],
    [AGENT_FILE, without(AGENT, "knowledges"), 'has no list of strings in "knowledges"'],
    [AGENT_FILE, { ...AGENT, knowledges: null }, 'has no list of strings in "knowledges"'],
    [
      AGENT_FILE,
      { ...AGENT, skills: ["knowledge-bases/base"] },
      'links an unknown template in "skills": knowledge-bases/base',
    ],
    [
      AGENT_FILE,
      { ...AGENT, knowledges: ["skill-sets/set"] },
      'links an unknown template in "knowledges": skill-sets/set',
    ],
    [
      AGENT_FILE,
      { ...AGENT, skills: ["skill-sets/none"] },
      'links an unknown template in "skills": skill-sets/none',
    ],
    [
      AGENT_FILE,
      { ...AGENT, skills: ["agents/document/agent"] },
      'links an unknown template in "skills": agents/document/agent',
    ],
    [AGENT_FILE, { ...AGENT, color: "red" }, 'has a "color" that is not six hexadecimal digits'],
    [AGENT_FILE, { ...AGENT, cover: "a b" }, 'has a "cover" that is not a room cover id'],
  ])("refuses %s holding %j: %s", async (file, content, problem) => {
    const root = await folder(tree({ [file]: content }));

    await expect(readTemplateCatalog(root)).rejects.toThrow(
      new Error(`Template ${path.join(root, file)} ${problem}`),
    );
  });

  it("refuses a manifest that is not JSON with the parser's error", async () => {
    const root = await folder(tree({ "skill-sets/set/template.json": "{ name: " }));

    await expect(readTemplateCatalog(root)).rejects.toBeInstanceOf(SyntaxError);
  });

  it("refuses a skill-set folder without a manifest", async () => {
    const root = await folder(tree({ "skill-sets/other/a.md": "# A" }));

    await expect(readTemplateCatalog(root)).rejects.toMatchObject({ code: "ENOENT" });
  });

  it("accepts an agent without skills or knowledge, and a template without a look", async () => {
    const root = await folder(
      tree({
        "agents/document/agent.json": {
          ...without(without(AGENT, "color"), "cover"),
          skills: [],
          knowledges: [],
        },
      }),
    );

    const [agent] = (await readTemplateCatalog(root)).agents;

    expect(agent).toEqual({
      id: "agents/document/agent",
      kind: "agent",
      name: "Agent",
      description: "An agent",
      category: "Document",
      instructions: "Do it",
      skills: [],
      knowledges: [],
    });
  });

  it("accepts a Markdown skill with an upper-case extension", async () => {
    const root = await folder(
      tree({
        "skill-sets/set/template.json": { ...SET, files: ["A.MD"] },
        "skill-sets/set/A.MD": "# A",
      }),
    );

    expect((await readTemplateCatalog(root)).skillSets[0]!.files).toEqual(["A.MD"]);
  });
});

describe("findTemplate and readTemplateFile", () => {
  it("finds a template of every kind by id, and nothing for an unknown id", async () => {
    const catalog = await readTemplateCatalog(await folder(tree()));

    expect(findTemplate(catalog, "agents/document/agent")?.kind).toBe("agent");
    expect(findTemplate(catalog, "skill-sets/set")?.kind).toBe("skillSet");
    expect(findTemplate(catalog, "knowledge-bases/base")?.kind).toBe("knowledgeBase");
    expect(findTemplate(catalog, "skill-sets")).toBeUndefined();
  });

  it("reads the bytes of a listed file from the given root", async () => {
    const root = await folder(tree());
    const catalog = await readTemplateCatalog(root);

    const bytes = await readTemplateFile(catalog.skillSets[0]!, "b.md", root);

    expect(new TextDecoder().decode(bytes)).toBe("# B");
  });

  it("throws synchronously for a file the template does not list", () => {
    const template = { id: "skill-sets/set", files: ["a.md"] } as SourceTemplate;

    expect(() => readTemplateFile(template, "template.json")).toThrow(
      "Template skill-sets/set has no file template.json",
    );
  });

  it("reads the shipped files from the templates folder of the service", () => {
    expect(path.basename(TEMPLATES_ROOT)).toBe("templates");
  });
});

describe("getTemplateCatalog", () => {
  afterEach(() => {
    vi.doUnmock("node:fs/promises");
    vi.resetModules();
  });

  async function freshCatalogModule(failFirstReads: number) {
    vi.resetModules();
    const reads = { readdir: 0, readFile: 0 };
    vi.doMock("node:fs/promises", async (original) => {
      const fs = await original<typeof import("node:fs/promises")>();
      return {
        ...fs,
        readdir: ((...args: Parameters<typeof fs.readdir>) => {
          reads.readdir += 1;
          return fs.readdir(...args);
        }) as typeof fs.readdir,
        readFile: ((...args: Parameters<typeof fs.readFile>) => {
          reads.readFile += 1;
          if (reads.readFile <= failFirstReads) {
            return Promise.reject(Object.assign(new Error("EIO: i/o error"), { code: "EIO" }));
          }
          return fs.readFile(...args);
        }) as typeof fs.readFile,
      };
    });
    return { module: await import("../catalog.js"), reads };
  }

  it("reads the shipped folder once for every caller", async () => {
    const { module, reads } = await freshCatalogModule(0);

    const [first, second] = await Promise.all([
      module.getTemplateCatalog(),
      module.getTemplateCatalog(),
    ]);
    const readsAfterFirst = { ...reads };
    const third = await module.getTemplateCatalog();

    expect(second).toBe(first);
    expect(third).toBe(first);
    expect(reads).toEqual(readsAfterFirst);
    expect(first.agents.length).toBeGreaterThan(0);
  });

  it("drops a failed read, so the next call reads the folder again", async () => {
    const { module, reads } = await freshCatalogModule(1);

    await expect(module.getTemplateCatalog()).rejects.toThrow("EIO: i/o error");
    const readsAfterFailure = reads.readFile;
    const catalog = await module.getTemplateCatalog();

    expect(reads.readFile).toBeGreaterThan(readsAfterFailure);
    expect(catalog.skillSets.length).toBeGreaterThan(0);
  });
});
