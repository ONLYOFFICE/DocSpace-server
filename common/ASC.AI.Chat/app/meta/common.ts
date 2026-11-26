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
 * a request reads (one file, or the top-level files of one folder, 20 at
 * most, each cut to a budget) and the report of what was left out.
 */

import { ActionType, customSync, extractErrorMessage, toChatError } from "@onlyoffice/ai-chat/core";
import type { Profile } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";
import { storage } from "../storage/index.js";
import { DocspaceApiHttpError } from "../storage/docspaceFilesApi.js";
import logger from "../log.js";
import {
  ConversionFailedError,
  ConversionTimeoutError,
  getPortalFile,
  listFolderFiles,
  PortalTextReader,
  UnsupportedFileError,
} from "./portalFiles.js";
import type { PortalFile } from "./portalFiles.js";

/** Files read per request, after a folder is expanded. */
export const MAX_FILES = 20;
/** Files listed per folder: the ones taken plus the ones reported as over the limit. */
const FOLDER_PAGE = 100;
/** Characters of one document handed to the model. */
const MAX_CHARS_PER_FILE = 40_000;
/** Characters of all documents together. */
const MAX_TOTAL_CHARS = 240_000;
/** Conversions running at the same time. */
const READ_CONCURRENCY = 4;
/** File-info reads of the selected file ids running at the same time. */
const FILE_INFO_CONCURRENCY = 4;
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
    super(
      skipped.length === 0
        ? "The selection holds no files"
        : "None of the selected files could be read as text",
    );
  }
}

/**
 * The model round did not produce an answer: the request's budget ran out
 * (504), the provider throttled the call (429), refused it for lack of
 * credit (402) or failed (502). Besides the status and a short reason the
 * client gets the chat library's stable error `code` (`rate_limit`,
 * `insufficient_funds`, `auth`, `model_not_found`, `timeout`, ...) and the
 * provider's own message, so it can say what went wrong instead of a
 * generic "the request failed" (the chat widget shows the same pair).
 */
export class ModelCallError extends Error {
  readonly expose = true;
  constructor(
    public readonly status: 402 | 429 | 502 | 504,
    public readonly code: string,
    message: string,
    public readonly providerMessage?: string,
    public override readonly cause?: unknown,
  ) {
    super(message);
  }
}

/** Length of the provider's message relayed to the client. */
const MAX_PROVIDER_MESSAGE_CHARS = 500;

/** The model round's budget, reading the files included. */
export const GENERATION_BUDGET_MS = 120_000;

/**
 * One model round. A failure is classified into a {@link ModelCallError} so
 * the controller can answer with a status that says what happened instead
 * of a bare 500.
 */
export async function runModel(
  args: { profile: Profile; systemPrompt: string; userMessage: string },
  signal: AbortSignal,
): Promise<ThreadMessageLike> {
  try {
    return await customSync({
      profile: args.profile,
      messages: [{ role: "user", content: args.userMessage }],
      systemPrompt: args.systemPrompt,
      signal,
    });
  } catch (err) {
    throw classifyModelError(err, signal);
  }
}

export function classifyModelError(err: unknown, signal: AbortSignal): ModelCallError {
  if (signal.aborted || (err instanceof Error && err.name === "AbortError")) {
    return new ModelCallError(
      504,
      "timeout",
      "The model did not answer within the time budget",
      undefined,
      err,
    );
  }
  const { code } = toChatError(asError(err));
  const providerMessage = providerMessageOf(err);
  if (code === "rate_limit") {
    return new ModelCallError(
      429,
      code,
      "The model provider is rate-limiting requests",
      providerMessage,
      err,
    );
  }
  if (code === "insufficient_funds") {
    return new ModelCallError(
      402,
      code,
      "The model provider refused the request for lack of credit",
      providerMessage,
      err,
    );
  }
  const detail = err instanceof Error ? err.message : String(err);
  logger.error(`meta: model call failed (${code}): ${detail}`);
  return new ModelCallError(502, code, "The model request failed", providerMessage, err);
}

// `toChatError` reads the status, code and type off an `Error` only; a
// plain object (a rejection built by hand, a proxy's parsed body) is given
// the same shape so its status is classified the same way.
function asError(err: unknown): unknown {
  if (err instanceof Error || typeof err !== "object" || err === null) {
    return err;
  }
  const message = (err as { message?: unknown }).message;
  return Object.assign(new Error(typeof message === "string" ? message : String(err)), err);
}

// The provider's own words, as the SDK error carries them (the nested
// Anthropic envelope included), cut to a sane length. A bare transport
// failure has nothing worth relaying.
function providerMessageOf(err: unknown): string | undefined {
  const message = extractErrorMessage(err)?.trim();
  if (!message) {
    return undefined;
  }
  return message.length > MAX_PROVIDER_MESSAGE_CHARS
    ? `${message.slice(0, MAX_PROVIDER_MESSAGE_CHARS)}...`
    : message;
}

const FILES_TIMEOUT_MESSAGE = "Reading the files did not finish within the time budget";

/** Thrown when the budget ran out while the files were still being read. */
export function assertNotTimedOut(signal: AbortSignal): void {
  if (signal.aborted) {
    throw new ModelCallError(504, "timeout", FILES_TIMEOUT_MESSAGE);
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

type FileInfoOutcome = { id: string } & ({ file: PortalFile | undefined } | { error: unknown });

// The file info of each id, {@link FILE_INFO_CONCURRENCY} at a time, in the
// order of the ids. After a read fails no new one starts: the ids after it
// are never used, as the failure ends the request. The outcomes stop at the
// first failure (or one of the reads already running beside it).
async function readFileInfos(ids: readonly string[]): Promise<FileInfoOutcome[]> {
  const outcomes: FileInfoOutcome[] = [];
  let failed = false;
  // One iterator for every worker: each takes the next id nobody has taken yet.
  const queue = ids.entries();
  const worker = async (): Promise<void> => {
    for (const [index, id] of queue) {
      if (failed) {
        return;
      }
      try {
        outcomes[index] = { id, file: await getPortalFile(id) };
      } catch (error) {
        outcomes[index] = { id, error };
        if (!(error instanceof DocspaceApiHttpError && error.status === 403)) {
          failed = true;
        }
      }
    }
  };
  await Promise.all(Array.from({ length: Math.min(FILE_INFO_CONCURRENCY, ids.length) }, worker));
  return outcomes;
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

  // The reads overlap, the outcomes are applied in the order of the ids: the
  // files, the skipped entries and the first failure are the ones a reading
  // one after another would give.
  for (const outcome of await readFileInfos(dedupeIds(fileIds))) {
    const { id } = outcome;
    if ("error" in outcome) {
      if (outcome.error instanceof DocspaceApiHttpError && outcome.error.status === 403) {
        skipped.push({ id, reason: "forbidden" });
        continue;
      }
      throw outcome.error;
    }
    if (!outcome.file) {
      skipped.push({ id, reason: "not_found" });
      continue;
    }
    take(outcome.file);
  }

  for (const folderId of dedupeIds(folderIds)) {
    // One page of the folder is enough: whatever lies past MAX_FILES is
    // skipped anyway, and the page is already sorted newest first. The page
    // is larger than the limit so the skipped files can be reported by name.
    const listed = await listFolderFiles(folderId, FOLDER_PAGE);
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
  // One iterator for every worker: each takes the next file nobody has taken yet.
  const queue = files.entries();
  // Set by the read that fails the request: the other workers take no further file.
  let failed = false;
  const worker = async (): Promise<void> => {
    for (const [index, file] of queue) {
      if (failed) {
        return;
      }
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
        // Only a refusal of the file itself leaves it out. Anything else - an expired
        // session, a 5xx, a timeout, a dropped connection, an answer of an unknown shape - is
        // the request's failure: relayed, so the caller gets a 5xx and can retry, instead of
        // every file reported as unreadable and a 422 (ts-ai-failure-paths.md §1).
        const reason = skipReasonOf(err);
        if (reason === undefined) {
          failed = true;
          // The request's own budget ran out under the read: the 504 of that budget.
          assertNotTimedOut(signal);
          if (err instanceof ConversionTimeoutError) {
            throw new ModelCallError(504, "timeout", FILES_TIMEOUT_MESSAGE, undefined, err);
          }
          throw err;
        }
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

/** Why a file is left out, or `undefined` when the failure is not a refusal of that file. */
function skipReasonOf(err: unknown): SkipReason | undefined {
  if (err instanceof UnsupportedFileError) {
    return "unsupported";
  }
  if (err instanceof ConversionFailedError) {
    return "failed";
  }
  if (err instanceof DocspaceApiHttpError) {
    if (err.status === 403) {
      return "forbidden";
    }
    if (err.status === 404) {
      return "not_found";
    }
  }
  return undefined;
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
// sentence around it is common enough to tolerate: take the first balanced
// object. Braces inside JSON strings are skipped, and a brace in prose after
// the object does not move its end.
export function parseJsonObject(text: string): Record<string, unknown> | undefined {
  const start = text.indexOf("{");
  if (start < 0) {
    return undefined;
  }
  let depth = 0;
  let inString = false;
  for (let i = start; i < text.length; i += 1) {
    const char = text[i];
    if (inString) {
      if (char === "\\") {
        i += 1;
      } else if (char === '"') {
        inString = false;
      }
      continue;
    }
    if (char === '"') {
      inString = true;
    } else if (char === "{") {
      depth += 1;
    } else if (char === "}") {
      depth -= 1;
      if (depth === 0) {
        try {
          const parsed: unknown = JSON.parse(text.slice(start, i + 1));
          return parsed !== null && typeof parsed === "object" && !Array.isArray(parsed)
            ? (parsed as Record<string, unknown>)
            : undefined;
        } catch {
          return undefined;
        }
      }
    }
  }
  return undefined;
}
