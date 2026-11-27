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

import fs from "fs/promises";
import os from "os";
import path from "path";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import logger from "../../log.js";
import {
  forgetMissingSkill,
  loadPluginSkill,
  MAX_SKILL_FILE_BYTES,
  PLUGIN_SKILL_DIR,
  pluginSkillPresent,
  readSkillFile,
} from "../skill.js";
import type { PluginSkill } from "../skill.js";

let dir = "";

const write = async (relative: string, content: string | Buffer = "x"): Promise<void> => {
  const file = path.join(dir, relative);
  await fs.mkdir(path.dirname(file), { recursive: true });
  await fs.writeFile(file, content);
};

beforeEach(async () => {
  dir = await fs.mkdtemp(path.join(os.tmpdir(), "plugin-skill-test-"));
  forgetMissingSkill();
});

afterEach(async () => {
  await fs.rm(dir, { recursive: true, force: true });
});

describe("loadPluginSkill", () => {
  it("reads the instructions and lists the scripts and templates to copy", async () => {
    await write("SKILL.md", "# the skill");
    await write("scripts/generate-plugin.mjs");
    await write("scripts/validator/rules.mjs");
    await write("scripts/validator-tests/case.mjs");
    await write("templates/tsconfig");
    await write("references/build.md");
    await write("evals/evals.json");
    await fs.symlink(path.join(dir, "SKILL.md"), path.join(dir, "scripts", "link.md"));

    expect(await loadPluginSkill(dir)).toEqual({
      dir,
      instructions: "# the skill",
      sandboxFiles: [
        "scripts/generate-plugin.mjs",
        "scripts/validator/rules.mjs",
        "templates/tsconfig",
      ],
    });
  });

  it("answers nothing and says so once when the skill is not there", async () => {
    expect(await loadPluginSkill(dir)).toBeUndefined();
    expect(await loadPluginSkill(dir)).toBeUndefined();

    const lines = vi.mocked(logger.warn).mock.calls.map((call) => String(call[0]));
    expect(lines.filter((line) => line.includes("the plugin skill is not at"))).toHaveLength(1);
  });

  it("answers nothing when the skill has no scripts", async () => {
    await write("SKILL.md");
    expect(await loadPluginSkill(dir)).toBeUndefined();
  });

  it("does not take another failure of the disk for a missing skill", async () => {
    await write("SKILL.md");
    await write("scripts");
    await write("templates/tsconfig");
    await expect(loadPluginSkill(dir)).rejects.toMatchObject({ code: "ENOTDIR" });
  });

  it("looks for the skill in the submodule next to the service", () => {
    expect(PLUGIN_SKILL_DIR.split(path.sep).slice(-5)).toEqual([
      "ASC.AI.Chat",
      "skills",
      "agent-skills",
      "skills",
      "apps-plugin-sdk",
    ]);
  });
});

describe("pluginSkillPresent", () => {
  it("is true when the skill's text is there, without listing its scripts", async () => {
    await write("SKILL.md");
    const listed = vi.spyOn(fs, "readdir");
    expect(await pluginSkillPresent(dir)).toBe(true);
    expect(listed).not.toHaveBeenCalled();
  });

  it("is false, said once, when the skill is not there", async () => {
    expect(await pluginSkillPresent(dir)).toBe(false);
    expect(await pluginSkillPresent(dir)).toBe(false);
    const lines = vi.mocked(logger.warn).mock.calls.map((call) => String(call[0]));
    expect(lines.filter((line) => line.includes("the plugin skill is not at"))).toHaveLength(1);
  });

  it("does not take another failure of the disk for a missing skill", async () => {
    await write("blocker", "a file where the skill's directory should be");
    await expect(pluginSkillPresent(path.join(dir, "blocker"))).rejects.toMatchObject({
      code: "ENOTDIR",
    });
  });
});

describe("readSkillFile", () => {
  let skill: PluginSkill;

  beforeEach(async () => {
    await write("SKILL.md", "# the skill");
    await write("references/build.md", "how to build");
    await write("references/items/common-fields.md", "fields");
    await write("references/huge.md", Buffer.alloc(MAX_SKILL_FILE_BYTES + 1));
    await write("references/at-limit.md", Buffer.alloc(MAX_SKILL_FILE_BYTES, "a"));
    await write("scripts/generate-plugin.mjs", "code");
    await write("templates/tsconfig");
    await fs.writeFile(path.join(dir, "..", `${path.basename(dir)}-outside.md`), "secret");
    skill = (await loadPluginSkill(dir))!;
  });

  afterEach(async () => {
    await fs.rm(path.join(dir, "..", `${path.basename(dir)}-outside.md`), { force: true });
  });

  it.each([
    ["SKILL.md", "# the skill"],
    ["./SKILL.md", "# the skill"],
    ["references/build.md", "how to build"],
    ["references/items/common-fields.md", "fields"],
    ["references\\build.md", "how to build"],
    ["references/items/../build.md", "how to build"],
  ])("reads %s", async (requested, text) => {
    expect(await readSkillFile(skill, requested)).toBe(text);
  });

  it("reads a file of exactly the size limit and refuses one a byte larger", async () => {
    expect(await readSkillFile(skill, "references/at-limit.md")).toHaveLength(MAX_SKILL_FILE_BYTES);
    expect(await readSkillFile(skill, "references/huge.md")).toBe(
      '"references/huge.md" is too large to read.',
    );
  });

  it.each([
    "../" + "outside.md",
    "references/../../outside.md",
    "references/../scripts/generate-plugin.mjs",
    "scripts/generate-plugin.mjs",
    "/etc/passwd",
    "",
    "references",
    "..",
  ])("refuses %s as no file of the skill", async (requested) => {
    expect(await readSkillFile(skill, requested)).toContain("is not a file of the skill");
  });

  it("does not read a file outside the skill through a climbing path", async () => {
    const outside = `references/../../${path.basename(dir)}-outside.md`;
    expect(await readSkillFile(skill, outside)).not.toContain("secret");
  });

  it("says that a file does not exist", async () => {
    expect(await readSkillFile(skill, "references/nope.md")).toBe(
      '"references/nope.md" does not exist in the skill.',
    );
  });

  it("says that a directory is not a file", async () => {
    expect(await readSkillFile(skill, "references/items")).toBe(
      '"references/items" is a directory: name one of its files.',
    );
  });

  it.each([
    ["leads through a file", "references/build.md/inside"],
    ["is longer than a name may be", `references/${"n".repeat(5000)}.md`],
    ["holds a character no path may", "references/bu\0ild.md"],
  ])("says that a path that %s does not exist, and goes on", async (_name, requested) => {
    expect(await readSkillFile(skill, requested)).toBe(
      `"${requested}" does not exist in the skill.`,
    );
  });

  it("answers an empty file with a text, never with nothing", async () => {
    await write("references/empty.md", "");
    expect(await readSkillFile(skill, "references/empty.md")).toBe(
      '"references/empty.md" is empty.',
    );
  });

  it("does not take another failure of the disk for a missing file", async () => {
    vi.spyOn(fs, "readFile").mockRejectedValue(
      Object.assign(new Error("EACCES: permission denied"), { code: "EACCES" }),
    );
    await expect(readSkillFile(skill, "references/build.md")).rejects.toMatchObject({
      code: "EACCES",
    });
  });
});
