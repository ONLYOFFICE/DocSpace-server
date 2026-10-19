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
 * Read-only access to portal files for the metadata generator: list a
 * folder's files and fetch one file's content as plain text. Both run under
 * the caller's forwarded credentials, so the portal enforces the access
 * rights and a file the caller cannot read answers 403 like everywhere else.
 *
 * The text comes from the Document Server conversion the Files API already
 * offers (`PUT files/fileops/bulkdownload` with a target extension, then the
 * finished operation's download URL) — the same path the portal's MCP tool
 * `download_file_as_text` takes. `.txt` / `.csv` are downloaded as they are;
 * every other extension is converted to the first of `.txt` / `.csv` that the
 * portal's `extsConvertible` table allows (word-processing and presentation
 * formats, pdf included, go to txt; spreadsheets to csv). Images and anything
 * else outside the table are reported as unsupported.
 */

import { withTimeout, proxyBaseUrl } from "../storage/httpClient.js";
import { DocspaceApiHttpError } from "../storage/docspaceFilesApi.js";
import { getForwardedHeaders } from "../requestContext.js";
import { getObject, getObjectArray, getString, isObject } from "../narrow.js";
import logger from "../log.js";

/** A file as the folder listing or the file-info endpoint describes it. */
export interface PortalFile {
  id: string;
  title: string;
  /** Extension with the leading dot, lower case (`.docx`); empty when unknown. */
  ext: string;
}

const PLAIN_TEXT_EXTS = new Set([".txt", ".csv"]);
const TEXT_TARGETS = [".txt", ".csv"] as const;

/** How long one conversion may take before it is given up as hung. */
const CONVERSION_BUDGET_MS = 90_000;
/** Pause between two polls of the file-operations endpoint. */
const POLL_INTERVAL_MS = 500;
/** Polls an operation may be missing from the list before it counts as lost. */
const MAX_POLL_MISSES = 3;
/** Attempts to start a conversion while the user's download slot is busy. */
const START_ATTEMPTS = 4;
/** Pause between two such attempts. */
const START_RETRY_MS = 1_500;

export class UnsupportedFileError extends Error {
  constructor(
    public readonly file: PortalFile,
    message: string,
  ) {
    super(message);
  }
}

function envelope(raw: unknown): Record<string, unknown> | undefined {
  return isObject(raw) ? getObject(raw, "response") : undefined;
}

// The list-shaped envelope: `{ response: [...] }`.
function envelopeList(raw: unknown): unknown[] {
  const list = isObject(raw) ? raw["response"] : undefined;
  return Array.isArray(list) ? list : [];
}

function toPortalFile(raw: Record<string, unknown>): PortalFile | undefined {
  const idValue = raw["id"];
  const id =
    typeof idValue === "number" ? String(idValue) : typeof idValue === "string" ? idValue : "";
  const title = getString(raw, "title") ?? "";
  if (!id || !title) {
    return undefined;
  }
  const ext = (getString(raw, "fileExst") ?? extensionOf(title)).toLowerCase();
  return { id, title, ext };
}

function extensionOf(title: string): string {
  const dot = title.lastIndexOf(".");
  return dot > 0 ? title.slice(dot) : "";
}

// A numeric id is an internal file and the Files API binds it as an integer;
// anything else is a third-party id and stays a string.
function idForBody(id: string): number | string {
  return /^\d+$/.test(id) ? Number(id) : id;
}

async function portalGet(url: string, signal?: AbortSignal): Promise<unknown> {
  const { signal: timed, cancel } = withTimeout(signal);
  try {
    const res = await fetch(url, { headers: getForwardedHeaders(), signal: timed });
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    return await res.json();
  } finally {
    cancel();
  }
}

/**
 * The files of a folder, top level only, newest first, at most `count`.
 * A folder the caller cannot open propagates the portal's 403 / 404.
 */
export async function listFolderFiles(folderId: string, count: number): Promise<PortalFile[]> {
  const url =
    `${proxyBaseUrl}/api/2.0/files/${encodeURIComponent(folderId)}` +
    `?filterType=FilesOnly&count=${count}&sortBy=DateAndTime&sortOrder=descending`;
  const body = envelope(await portalGet(url));
  const files = body ? (getObjectArray(body, "files") ?? []) : [];
  const result: PortalFile[] = [];
  for (const raw of files) {
    const file = toPortalFile(raw);
    if (file) {
      result.push(file);
    }
  }
  logger.info(`meta.listFolderFiles(${folderId}) -> ${result.length} file(s)`);
  return result;
}

/** One file as the Files API describes it; `undefined` on 404. */
export async function getPortalFile(fileId: string): Promise<PortalFile | undefined> {
  const url = `${proxyBaseUrl}/api/2.0/files/file/${encodeURIComponent(fileId)}`;
  try {
    const body = envelope(await portalGet(url));
    return body ? toPortalFile(body) : undefined;
  } catch (err) {
    if (err instanceof DocspaceApiHttpError && err.status === 404) {
      return undefined;
    }
    throw err;
  }
}

let convertibleCache: Promise<Record<string, string[]>> | undefined;

// `GET files/settings` → `extsConvertible`: source extension → target
// extensions. The table is a portal-wide constant (it mirrors the Document
// Server's formats file), so one read per process is enough.
function getConvertible(): Promise<Record<string, string[]>> {
  if (!convertibleCache) {
    convertibleCache = (async () => {
      const body = envelope(await portalGet(`${proxyBaseUrl}/api/2.0/files/settings`));
      const table = body ? getObject(body, "extsConvertible") : undefined;
      const result: Record<string, string[]> = {};
      if (table) {
        for (const [ext, targets] of Object.entries(table)) {
          if (Array.isArray(targets)) {
            result[ext.toLowerCase()] = targets.filter((t): t is string => typeof t === "string");
          }
        }
      }
      if (Object.keys(result).length === 0) {
        // No table means the settings call answered something unexpected;
        // keep asking rather than pinning "nothing converts" for the process.
        convertibleCache = undefined;
      }
      return result;
    })();
    convertibleCache.catch(() => {
      convertibleCache = undefined;
    });
  }
  return convertibleCache;
}

/** The text target (`.txt` / `.csv`) a file converts to, or `undefined`. */
export async function textTargetFor(file: PortalFile): Promise<string | undefined> {
  if (PLAIN_TEXT_EXTS.has(file.ext)) {
    return file.ext;
  }
  const targets = (await getConvertible())[file.ext] ?? [];
  return TEXT_TARGETS.find((t) => targets.includes(t));
}

interface FileOperation {
  id: string;
  finished: boolean;
  error: string;
  url: string | undefined;
}

function toOperation(raw: Record<string, unknown>): FileOperation | undefined {
  const id = getString(raw, "id");
  if (!id) {
    return undefined;
  }
  return {
    id,
    finished: raw["finished"] === true,
    error: getString(raw, "error") ?? "",
    url: getString(raw, "url"),
  };
}

async function startConversion(
  file: PortalFile,
  target: string,
  signal: AbortSignal,
): Promise<FileOperation> {
  const url = `${proxyBaseUrl}/api/2.0/files/fileops/bulkdownload`;
  const { signal: timed, cancel } = withTimeout(signal);
  try {
    const res = await fetch(url, {
      method: "PUT",
      headers: { ...getForwardedHeaders(), "content-type": "application/json" },
      // `returnSingleOperation`: answer with this operation only. Without it
      // the portal lists every operation of the user and drops the finished
      // ones from its queue as it does so, which would hand another file's
      // result (or an unrelated archive download) to this reader.
      body: JSON.stringify({
        fileConvertIds: [{ key: idForBody(file.id), value: target }],
        returnSingleOperation: true,
      }),
      signal: timed,
    });
    if (!res.ok) {
      throw new DocspaceApiHttpError(res.status, res.statusText, url);
    }
    for (const raw of envelopeList(await res.json())) {
      const op = isObject(raw) ? toOperation(raw) : undefined;
      if (op) {
        return op;
      }
    }
    throw new Error(`bulkdownload for file ${file.id} returned no operation`);
  } finally {
    cancel();
  }
}

/**
 * Polls `GET files/fileops` for the operations a reader started and settles
 * each one as it finishes. One poller per reader: the list is the caller's
 * own, and a read of it drops the finished operations, so several pollers
 * for the same user would steal each other's results.
 */
class OperationWatcher {
  private readonly waiters = new Map<
    string,
    { resolve: (op: FileOperation) => void; reject: (err: unknown) => void; misses: number }
  >();
  private timer: NodeJS.Timeout | undefined;

  constructor(private readonly signal: AbortSignal) {}

  /**
   * Settles when the operation finishes. `signal` is the one read's own
   * budget (derived from the reader's signal, so a request-level abort
   * reaches it too); the poller itself runs on the reader's signal.
   */
  wait(op: FileOperation, signal: AbortSignal): Promise<FileOperation> {
    if (op.error) {
      return Promise.reject(new Error(`conversion failed: ${op.error}`));
    }
    if (op.finished) {
      return Promise.resolve(op);
    }
    if (signal.aborted) {
      return Promise.reject(signal.reason ?? new Error("aborted"));
    }
    return new Promise<FileOperation>((resolve, reject) => {
      const onAbort = (): void => {
        this.waiters.delete(op.id);
        reject(signal.reason ?? new Error("aborted"));
      };
      signal.addEventListener("abort", onAbort, { once: true });
      const settle =
        <T>(fn: (v: T) => void) =>
        (v: T) => {
          signal.removeEventListener("abort", onAbort);
          this.waiters.delete(op.id);
          fn(v);
        };
      this.waiters.set(op.id, { resolve: settle(resolve), reject: settle(reject), misses: 0 });
      this.schedule();
    });
  }

  private schedule(): void {
    if (this.timer || this.waiters.size === 0 || this.signal.aborted) {
      return;
    }
    this.timer = setTimeout(() => {
      this.timer = undefined;
      void this.poll();
    }, POLL_INTERVAL_MS);
  }

  private async poll(): Promise<void> {
    if (this.waiters.size === 0) {
      return;
    }
    let operations: FileOperation[] | undefined;
    try {
      operations = envelopeList(
        await portalGet(`${proxyBaseUrl}/api/2.0/files/fileops`, this.signal),
      ).flatMap((raw) => {
        const op = isObject(raw) ? toOperation(raw) : undefined;
        return op ? [op] : [];
      });
    } catch (err) {
      if (this.signal.aborted) {
        return;
      }
      // A failed poll (a proxy hiccup, a transient 5xx) is one missed look
      // at the list, not the end of every conversion in flight.
      logger.warn(
        `meta: polling file operations failed: ${err instanceof Error ? err.message : String(err)}`,
      );
    }
    for (const [id, waiter] of [...this.waiters.entries()]) {
      const found = operations?.find((op) => op.id === id);
      if (!found) {
        // The list holds the operations still running or not yet read; one
        // that is gone was finished and read by someone else (the portal UI
        // polls the same list), so its result is lost to us.
        waiter.misses += 1;
        if (waiter.misses >= MAX_POLL_MISSES) {
          waiter.reject(new Error(`file operation ${id} disappeared before it finished`));
        }
        continue;
      }
      if (found.error) {
        waiter.reject(new Error(`conversion failed: ${found.error}`));
      } else if (found.finished) {
        waiter.resolve(found);
      }
    }
    this.schedule();
  }
}

// The operation's download URL names the portal's public host; inside the
// deployment that host is not always reachable, so only its path is kept and
// the request goes through the same proxy as every other Files API call.
function throughProxy(downloadUrl: string): string {
  try {
    const parsed = new URL(downloadUrl, proxyBaseUrl);
    return `${proxyBaseUrl}${parsed.pathname}${parsed.search}`;
  } catch {
    return downloadUrl;
  }
}

/**
 * Reads files as text for one request. All conversions started through the
 * same reader share one poller of the file-operations list and one abort
 * signal.
 */
export class PortalTextReader {
  private readonly watcher: OperationWatcher;
  // The portal runs one download operation per user at a time: a second
  // `bulkdownload` while one is running answers 403. The conversions of one
  // reader therefore start one after another; only the result downloads
  // overlap.
  private conversionQueue: Promise<unknown> = Promise.resolve();

  constructor(private readonly signal: AbortSignal) {
    this.watcher = new OperationWatcher(signal);
  }

  private convert(file: PortalFile, target: string, signal: AbortSignal): Promise<FileOperation> {
    const run = async (): Promise<FileOperation> => {
      // A download the user started elsewhere (the UI's archive, say) holds
      // the slot too; give it a few moments before reporting the file.
      for (let attempt = 1; ; attempt += 1) {
        try {
          return await this.watcher.wait(await startConversion(file, target, signal), signal);
        } catch (err) {
          if (
            !(err instanceof DocspaceApiHttpError) ||
            err.status !== 403 ||
            attempt >= START_ATTEMPTS ||
            signal.aborted
          ) {
            throw err;
          }
          await new Promise((resolve) => setTimeout(resolve, START_RETRY_MS));
        }
      }
    };
    const turn = this.conversionQueue.then(run, run);
    this.conversionQueue = turn.catch(() => undefined);
    return turn;
  }

  /**
   * The file's content as plain text. Throws {@link UnsupportedFileError}
   * when the portal has no text conversion for the file's extension, a
   * {@link DocspaceApiHttpError} when the portal refuses the file, and an
   * `Error` when the conversion fails or exceeds its budget.
   */
  async readFileAsText(file: PortalFile): Promise<string> {
    const target = await textTargetFor(file);
    if (!target) {
      throw new UnsupportedFileError(
        file,
        `no text conversion for ${file.ext || "a file without extension"}`,
      );
    }
    const { signal: timed, cancel } = withTimeout(this.signal, CONVERSION_BUDGET_MS);
    const started = Date.now();
    try {
      const op = await this.convert(file, target, timed);
      if (!op.url) {
        throw new Error(`conversion of file ${file.id} finished without a download URL`);
      }
      const url = throughProxy(op.url);
      const res = await fetch(url, {
        headers: { ...getForwardedHeaders(), accept: "text/plain" },
        signal: timed,
      });
      if (!res.ok) {
        throw new DocspaceApiHttpError(res.status, res.statusText, url);
      }
      const text = await res.text();
      logger.info(
        `meta.readFileAsText(${file.id} "${file.title}" -> ${target}) ${text.length} chars in ${Date.now() - started}ms`,
      );
      return text;
    } finally {
      cancel();
    }
  }
}

/** One-off read of a single file; see {@link PortalTextReader}. */
export function readFileAsText(file: PortalFile, signal?: AbortSignal): Promise<string> {
  return new PortalTextReader(signal ?? new AbortController().signal).readFileAsText(file);
}
