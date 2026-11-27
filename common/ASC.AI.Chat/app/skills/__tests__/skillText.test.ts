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

// The text helpers of skill generation and creation at their edges: limits without a word
// boundary, quoting, frontmatter the model wrote in unusual shapes.

import { describe, expect, it, vi } from "vitest";

vi.mock("../../storage/index.js", () => ({ storage: {} }));

const { cleanDescription, cleanTitle, parseSkill, toSkillName } =
  await import("../generateSkill.js");
const { buildSkillTemplate, buildSkillTemplateBody, toSkillTitle } =
  await import("../createSkill.js");

describe("cleanDescription", () => {
  it("keeps a text of exactly 1024 characters", () => {
    const text = "a".repeat(1024);
    expect(cleanDescription(text)).toBe(text);
  });

  it("cuts a text without a space in its second half at the limit", () => {
    expect(cleanDescription("a".repeat(2000))).toBe(`${"a".repeat(1021)}...`);
  });

  it("cuts at the last space when it lies in the second half", () => {
    const text = `${"a".repeat(600)} ${"b".repeat(600)}`;
    expect(cleanDescription(text)).toBe(`${"a".repeat(600)}...`);
  });

  it("is empty for a text of tags and blanks only", () => {
    expect(cleanDescription(" <b></b>\n<i/> ")).toBe("");
  });
});

describe("cleanTitle", () => {
  it("cuts a title without a space in its second half at 64 characters", () => {
    expect(cleanTitle("a".repeat(100))).toBe("a".repeat(64));
  });

  it("cuts at the last space when it lies in the second half", () => {
    expect(cleanTitle(`${"a".repeat(40)} ${"b".repeat(40)}`)).toBe("a".repeat(40));
  });

  it("drops the marks around a title and its extension", () => {
    expect(cleanTitle("# `Sales report`.MD")).toBe("Sales report");
    expect(cleanTitle("'Sales report'")).toBe("Sales report");
  });
});

describe("toSkillName", () => {
  it("folds full-width characters and counts code units against the limit", () => {
    expect(toSkillName("ＳＡＬＥＳ")).toBe("sales");
    expect(toSkillName("𝟘".repeat(40))).toBe("0".repeat(40));
  });
});

describe("parseSkill", () => {
  it.each([
    [
      "a double-quoted name",
      '---\nname: "x"\ndescription: d\n---\nB',
      { name: "x", description: "d", body: "B" },
    ],
    [
      "a single-quoted name",
      "---\nname: 'x'\ndescription: d\n---\nB",
      { name: "x", description: "d", body: "B" },
    ],
    [
      "mismatched quotes",
      "---\nname: \"x'\ndescription: d\n---\nB",
      { name: "\"x'", description: "d", body: "B" },
    ],
    [
      "a lone quote",
      '---\nname: "\ndescription: d\n---\nB',
      { name: '"', description: "d", body: "B" },
    ],
    ["no name", "---\ndescription: d\n---\nB", { name: undefined, description: "d", body: "B" }],
    ["no description", "---\nname: x\n---\nB", { name: "x", description: undefined, body: "B" }],
    [
      "Windows line breaks",
      "---\r\nname: x\r\ndescription: d\r\n---\r\nB",
      { name: "x", description: "d", body: "B" },
    ],
    [
      "a block scalar ended by the next key",
      "---\ndescription: |\n  line a\n  line b\nname: x\n---\nB",
      { name: "x", description: "line a line b", body: "B" },
    ],
    [
      "a block scalar with a blank line",
      "---\nname: x\ndescription: >-\n  a\n\n  b\n---\nB",
      { name: "x", description: "a  b", body: "B" },
    ],
    [
      "an empty body",
      "---\nname: x\ndescription: d\n---\n",
      { name: "x", description: "d", body: "" },
    ],
  ])("reads frontmatter with %s", (_what, text, parsed) => {
    expect(parseSkill(text)).toStrictEqual(parsed);
  });

  it.each([
    ["an unclosed frontmatter", "---\nname: x\nBody"],
    ["a fence that is never closed", "```markdown\n---\nname: x\n---"],
  ])("takes %s as the body", (_what, text) => {
    expect(parseSkill(text)).toStrictEqual({ body: text });
  });
});

describe("the skill template of createSkill", () => {
  it("takes the name as the description when the title is only a tag", () => {
    expect(buildSkillTemplate("<x>")).toBe(
      `---\nname: "x"\ndescription: "x"\n---\n\n${buildSkillTemplateBody("<x>")}\n`,
    );
  });

  it("keeps the heading on one line for a title with line breaks", () => {
    expect(buildSkillTemplate("Sales\nreport").split("\n").slice(0, 6)).toEqual([
      "---",
      'name: "sales-report"',
      'description: "Sales report"',
      "---",
      "",
      "# Sales report",
    ]);
  });

  it("strips one Markdown extension and the blanks around it", () => {
    expect(toSkillTitle("  Report.md.MD ")).toBe("Report.md");
  });
});
