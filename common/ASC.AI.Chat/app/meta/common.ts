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
 * Shared pieces of the metadata routes: the model profile, the document set
 * a request reads (files named directly or through folders, 20 at most,
 * each cut to a budget) and the report of what was left out.
 */

import { ActionType } from "@onlyoffice/ai-chat/core";
import type { Profile } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { DocspaceApiHttpError } from "../storage/docspaceFilesApi.js";
import logger from "../log.js";
import {
  getPortalFile,
  listFolderFiles,
  PortalTextReader,
  UnsupportedFileError,
} from "./portalFiles.js";
import type { PortalFile } from "./portalFiles.js";

/** Files read per request, after the folders are expanded. */
export const MAX_FILES = 20;
/** Folders accepted per request. */
export const MAX_FOLDERS = 20;
/** Characters of one document handed to the model. */
const MAX_CHARS_PER_FILE = 40_000;
/** Characters of all documents together. */
const MAX_TOTAL_CHARS = 240_000;
/** Conversions running at the same time. */
const READ_CONCURRENCY = 4;
/** Length of the user's refinement prompt that is passed on. */
export const MAX_PROMPT_CHARS = 2_000;

export type SkipReason = "unsupported" | "limit" | "forbidden" | "not_found" | "empty" | "failed";

export interface MetaSource {
  id: string;
  title: string;
  truncated: boolean;
}

export interface MetaSkipped {
  id: string;
  title?: string;
  reason: SkipReason;
}

export interface Document {
  file: PortalFile;
  text: string;
  truncated: boolean;
}

export class NoProfileError extends Error {
  readonly status = 503;
  readonly expose = true;
  constructor() {
    super("No model is configured for metadata generation");
  }
}

export class NoContentError extends Error {
  readonly status = 422;
  readonly expose = true;
  constructor(public readonly skipped: MetaSkipped[]) {
    super("None of the selected files could be read as text");
  }
}

/** The `GenerateMeta` profile, falling back to the `Default` slot. */
export async function resolveMetaProfile(): Promise<Profile | undefined> {
  const profileId =
    (await storage.assignments.readByType(ActionType.GenerateMeta)) ??
    (await storage.assignments.readByType(ActionType.Default));
  if (!profileId) {
    return undefined;
  }
  return storage.profiles.readById(profileId);
}

function dedupeIds(ids: string[]): string[] {
  return [...new Set(ids.map((id) => id.trim()).filter((id) => id.length > 0))];
}

/**
 * Expand the selection into the files to read: the listed files first, then
 * the top-level content of each folder in order, until {@link MAX_FILES}.
 * Files past the limit are reported as skipped, so are ids the caller cannot
 * see.
 */
export async function collectFiles(
  fileIds: string[],
  folderIds: string[],
  skipped: MetaSkipped[],
): Promise<PortalFile[]> {
  const files: PortalFile[] = [];
  const seen = new Set<string>();
  const take = (file: PortalFile): void => {
    if (seen.has(file.id)) {
      return;
    }
    seen.add(file.id);
    if (files.length >= MAX_FILES) {
      skipped.push({ id: file.id, title: file.title, reason: "limit" });
      return;
    }
    files.push(file);
  };

  for (const id of dedupeIds(fileIds)) {
    try {
      const file = await getPortalFile(id);
      if (!file) {
        skipped.push({ id, reason: "not_found" });
        continue;
      }
      take(file);
    } catch (err) {
      if (err instanceof DocspaceApiHttpError && err.status === 403) {
        skipped.push({ id, reason: "forbidden" });
        continue;
      }
      throw err;
    }
  }

  for (const folderId of dedupeIds(folderIds)) {
    // One page of the folder is enough: whatever lies past MAX_FILES is
    // skipped anyway, and the page is already sorted newest first.
    const listed = await listFolderFiles(folderId, MAX_FILES + 1);
    for (const file of listed) {
      take(file);
    }
  }
  return files;
}

/** Read the files as text, {@link READ_CONCURRENCY} at a time, within the budgets. */
export async function readDocuments(
  files: PortalFile[],
  skipped: MetaSkipped[],
  signal: AbortSignal,
): Promise<Document[]> {
  const results: (Document | undefined)[] = new Array(files.length);
  const reader = new PortalTextReader(signal);
  let next = 0;
  const worker = async (): Promise<void> => {
    while (next < files.length) {
      const index = next;
      next += 1;
      const file = files[index]!;
      try {
        const raw = await reader.readFileAsText(file);
        const text = raw.trim();
        if (!text) {
          skipped.push({ id: file.id, title: file.title, reason: "empty" });
          continue;
        }
        results[index] = {
          file,
          text: text.slice(0, MAX_CHARS_PER_FILE),
          truncated: text.length > MAX_CHARS_PER_FILE,
        };
      } catch (err) {
        const reason = skipReasonOf(err);
        logger.warn(
          `meta: file ${file.id} "${file.title}" skipped (${reason}): ${
            err instanceof Error ? err.message : String(err)
          }`,
        );
        skipped.push({ id: file.id, title: file.title, reason });
      }
    }
  };
  await Promise.all(Array.from({ length: Math.min(READ_CONCURRENCY, files.length) }, worker));
  const documents = results.filter((d): d is Document => d !== undefined);
  fitBudget(documents);
  return documents;
}

function skipReasonOf(err: unknown): SkipReason {
  if (err instanceof UnsupportedFileError) {
    return "unsupported";
  }
  if (err instanceof DocspaceApiHttpError) {
    if (err.status === 403) {
      return "forbidden";
    }
    if (err.status === 404) {
      return "not_found";
    }
  }
  return "failed";
}

// Spread the shared budget over the documents: each gets its own cap, and
// when the sum still exceeds the total the longest ones are cut first.
export function fitBudget(documents: Document[]): void {
  const total = documents.reduce((sum, d) => sum + d.text.length, 0);
  if (total <= MAX_TOTAL_CHARS) {
    return;
  }
  const perDocument = Math.floor(MAX_TOTAL_CHARS / documents.length);
  for (const document of documents) {
    if (document.text.length > perDocument) {
      document.text = document.text.slice(0, perDocument);
      document.truncated = true;
    }
  }
}

/** The documents as the model sees them, each between marked boundaries. */
export function formatDocuments(documents: Document[]): string {
  return documents
    .map((document, index) => {
      const note = document.truncated ? " (truncated)" : "";
      return (
        `===== Document ${index + 1}: ${document.file.title}${note} =====\n` +
        `${document.text}\n===== End of document ${index + 1} =====`
      );
    })
    .join("\n\n");
}

export function toSources(documents: Document[]): MetaSource[] {
  return documents.map((d) => ({ id: d.file.id, title: d.file.title, truncated: d.truncated }));
}

export function totalChars(documents: Document[]): number {
  return documents.reduce((sum, d) => sum + d.text.length, 0);
}

// The model is told to answer with a bare object, but a code fence or a
// sentence around it is common enough to tolerate: take the outermost braces.
export function parseJsonObject(text: string): Record<string, unknown> | undefined {
  const start = text.indexOf("{");
  const end = text.lastIndexOf("}");
  if (start < 0 || end <= start) {
    return undefined;
  }
  try {
    const parsed: unknown = JSON.parse(text.slice(start, end + 1));
    return parsed !== null && typeof parsed === "object" && !Array.isArray(parsed)
      ? (parsed as Record<string, unknown>)
      : undefined;
  } catch {
    return undefined;
  }
}
