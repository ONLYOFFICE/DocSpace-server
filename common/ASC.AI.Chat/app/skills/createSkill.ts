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
 * Skill creation: a new Markdown skill file in an Instructions room, written
 * from a template, for the user to fill in. No model is involved. The Files
 * service refuses a skill without its `name` / `description` frontmatter
 * (`SkillFrontmatter` in ASC.Files.Core), so an empty file could never be
 * created there: the template carries both, derived from the title, and a
 * body laid out the way a skill is expected to read.
 */

import { uploadFile } from "../storage/docspaceFileTransfer.js";
import type { UploadedFile } from "../storage/docspaceFileTransfer.js";
import {
  assertInstructionsRoom,
  buildSkillFile,
  cleanDescription,
  toSkillName,
} from "./generateSkill.js";

/** Length of the file title a request may carry, extension excluded. */
export const MAX_TITLE_LENGTH = 165;

export interface CreateSkillInput {
  destId: string;
  title: string;
}

function httpError(status: number, message: string): Error {
  return Object.assign(new Error(message), { status, expose: true });
}

/** The title as the dialog sends it, without a `.md` the user typed themselves. */
export function toSkillTitle(title: string): string {
  return title.trim().replace(/\.md$/i, "").trim();
}

/** The body of a new skill: the sections a skill is made of, to be filled in. */
export function buildSkillTemplateBody(title: string): string {
  return [
    `# ${title}`,
    "",
    "Describe in one or two sentences what this skill is for.",
    "",
    "## When to use",
    "",
    "- The tasks and requests this skill applies to.",
    "",
    "## Instructions",
    "",
    "1. The first step.",
    "2. The next step.",
    "",
    "## Output format",
    "",
    "Describe the expected result, or give a template of it.",
    "",
    "## Examples",
    "",
    "Give one or two short examples of a request and the expected result.",
  ].join("\n");
}

/** The whole template file for a title: frontmatter and body. */
export function buildSkillTemplate(title: string): string {
  const name = toSkillName(title);
  // The description decides when the skill is used; until the user writes
  // one, the title stands in for it, as the service refuses an empty one.
  const description = cleanDescription(title) || name;
  // A heading is one line: a line break in the title would start body text.
  return buildSkillFile(name, description, buildSkillTemplateBody(title.replace(/\s+/g, " ")));
}

export async function createSkill(input: CreateSkillInput): Promise<UploadedFile> {
  const title = toSkillTitle(input.title);
  if (!title || title.length > MAX_TITLE_LENGTH) {
    throw httpError(
      400,
      `title must be a non-empty string of at most ${MAX_TITLE_LENGTH} characters`,
    );
  }
  if (!toSkillName(title)) {
    throw httpError(400, "title must contain a letter or a digit");
  }
  await assertInstructionsRoom(input.destId);

  const bytes = new TextEncoder().encode(buildSkillTemplate(title));
  return uploadFile(input.destId, `${title}.md`, bytes, "text/markdown");
}
