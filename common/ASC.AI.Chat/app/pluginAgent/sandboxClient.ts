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

// The sandbox a plugin task and a conversation of the chat work in, as this service uses it. The
// sandbox is somebody else's service (the gateway of the document agent's repository,
// `gateway.v1.SandboxService`); this is the only module that calls it, so a different API changes
// this file and its tests.
//
// Three kinds of call. The unary methods are plain JSON `POST`s, as the gateway documents them.
// `Exec` is a Connect server stream (`application/connect+json`): the request and every message
// of the answer travel in an envelope of one flag byte and a four-byte length, and the last
// envelope (flag 2) says how the stream ended. File bytes are plain HTTP bodies.
//
// Every route wants the gateway's bearer token. Nothing of the caller's session is sent: a
// sandbox never sees a credential of the portal.

import {
  providerFetch,
  readBodyCapped,
  readTextCapped,
  withTimeout,
} from "../storage/httpClient.js";
import { cancelReader } from "../storage/errorBody.js";
import { jsonAnswer, mapped } from "../storage/upstreamShape.js";
import { getString, isObject } from "../narrow.js";

/** Where the gateway is and the token it wants: the two values of the configuration this module reads. */
export interface SandboxGateway {
  sandboxUrl: string;
  sandboxToken: string;
}

/**
 * Why a call to the sandbox failed, for the code that decides what the task comes to:
 * `busy` — no sandbox is free, or one did not come up in time; `lost` — the sandbox is gone or
 * no longer running; `absent` — the path is not there; `tooLarge` — a file or a message over its
 * limit; `unavailable` — everything else: unreachable, a 5xx, an answer that cannot be read.
 */
export type SandboxFailure = "busy" | "lost" | "absent" | "tooLarge" | "unavailable";

export class SandboxError extends Error {
  public readonly kind: SandboxFailure;
  /**
   * For the log: the status and the code of a refusal, or this side's own failure (no answer,
   * an answer that cannot be read, a body over its limit); never the gateway's own text.
   */
  public readonly detail: string;
  /** The Connect code of a refusal; empty when the failure is not one or names none. */
  public readonly code: string;

  constructor(kind: SandboxFailure, call: string, detail: string, code = "") {
    super(`sandbox ${call}: ${kind}`);
    this.name = "SandboxError";
    this.kind = kind;
    this.detail = detail;
    this.code = code;
  }
}

export interface ExecOptions {
  /** Working directory; the sandbox's own default when left out. */
  cwd?: string;
  env?: Record<string, string>;
  /** Enforced in the sandbox, which kills the command; the call itself waits a little longer. */
  timeoutSeconds: number;
  /** The most of stdout and of stderr that is kept, in characters; the rest is dropped. */
  maxOutputChars: number;
  signal?: AbortSignal;
}

export interface ExecResult {
  stdout: string;
  stderr: string;
  /** The shell's exit status; -1 when the command was killed, by its timeout included. */
  code: number;
  /** Whether output was dropped at `maxOutputChars`. */
  truncated: boolean;
}

const SERVICE = "gateway.v1.SandboxService";
/** A unary call that only changes or reads state in the gateway. */
const UNARY_TIMEOUT_MS = 30_000;
/** Create waits for the VM and for what starts in it. */
const CREATE_TIMEOUT_MS = 120_000;
const FILE_TIMEOUT_MS = 120_000;
/** How much longer than the command's own timeout the call waits for the exit event. */
const EXEC_GRACE_MS = 15_000;
const ERROR_BODY_LIMIT = 4096;
/** One message of an Exec stream; the gateway sends output in chunks far smaller than this. */
const MAX_FRAME_BYTES = 4 * 1024 * 1024;
const ENVELOPE_HEADER_BYTES = 5;
const END_STREAM_FLAG = 0x02;

// The Connect codes this module tells apart; any other is `unavailable`. "No room" and "not in
// time" mean that no sandbox is free only as the answer of Create: for a command or a file
// they are a failure of that call.
const KIND_BY_CODE = new Map<string, SandboxFailure>([
  ["resource_exhausted", "busy"],
  ["deadline_exceeded", "busy"],
  ["not_found", "lost"],
  ["failed_precondition", "lost"],
]);

function kindOf(code: string, call: string): SandboxFailure {
  const kind = KIND_BY_CODE.get(code) ?? "unavailable";
  return kind === "busy" && call !== "Create" ? "unavailable" : kind;
}

// The file routes answer a plain status: 404 when the path is not there (and when the sandbox
// itself is unknown, which the next command then reports as lost), 502 when the sandbox's own
// port does not answer (a stopped sandbox).
const KIND_BY_FILE_STATUS = new Map<number, SandboxFailure>([
  [404, "absent"],
  [502, "lost"],
]);

function headers(gateway: SandboxGateway, contentType?: string): Record<string, string> {
  return {
    Authorization: `Bearer ${gateway.sandboxToken}`,
    ...(contentType ? { "Content-Type": contentType } : {}),
  };
}

/**
 * A refused call by the `code` of its `{ code, message }` body, when the body is that. The body
 * itself is the gateway's text and stays out of the error and of the log (ts-ai-style.md, State
 * and logging): the status and the code are what is kept.
 */
async function refusal(res: Response, call: string): Promise<SandboxError> {
  const text = await readTextCapped(res, ERROR_BODY_LIMIT);
  let code = "";
  try {
    const body: unknown = JSON.parse(text);
    code = (isObject(body) ? getString(body, "code") : undefined) ?? "";
  } catch {
    // not JSON: a proxy in front of the gateway, or the gateway's own plain refusal
  }
  return new SandboxError(kindOf(code, call), call, `${res.status} ${code}`.trim(), code);
}

/** A failure that is not an answer (a refused connection, a timeout) is `unavailable`; an abort of the caller's stays what it is. */
function unreachable(err: unknown, call: string, signal: AbortSignal | undefined): unknown {
  if (err instanceof SandboxError || signal?.aborted) {
    return err;
  }
  return new SandboxError("unavailable", call, String(err));
}

async function unary(
  gateway: SandboxGateway,
  method: string,
  body: Record<string, unknown>,
  signal: AbortSignal | undefined,
  timeoutMs = UNARY_TIMEOUT_MS,
): Promise<unknown> {
  const bounded = withTimeout(signal, timeoutMs);
  try {
    const res = await providerFetch(
      `${gateway.sandboxUrl}/${SERVICE}/${method}`,
      {
        method: "POST",
        headers: headers(gateway, "application/json"),
        body: JSON.stringify(body),
        signal: bounded.signal,
      },
      "none",
    );
    if (!res.ok) {
      throw await refusal(res, method);
    }
    return await jsonAnswer(res, `sandbox ${method}`);
  } catch (err) {
    throw unreachable(err, method, signal);
  } finally {
    bounded.cancel();
  }
}

/**
 * The sandbox of the session, created when there is none: the same `sessionId` (a UUID) answers
 * the same sandbox. `busy` when the gateway has no room or the sandbox did not come up in time.
 */
export async function createSandbox(
  gateway: SandboxGateway,
  sessionId: string,
  ttlSeconds: number,
  signal?: AbortSignal,
): Promise<string> {
  const answer = await unary(
    gateway,
    "Create",
    { sessionId, ttl: `${ttlSeconds}s` },
    signal,
    CREATE_TIMEOUT_MS,
  );
  return mapped(
    (isObject(answer) ? getString(answer, "id") : undefined) || undefined,
    "sandbox Create",
  );
}

/** Moves the expiry to now plus `ttlSeconds`; `lost` when the sandbox is no longer running. */
export async function renewSandbox(
  gateway: SandboxGateway,
  id: string,
  ttlSeconds: number,
  signal?: AbortSignal,
): Promise<void> {
  await unary(gateway, "Renew", { id, ttl: `${ttlSeconds}s` }, signal);
}

export async function deleteSandbox(gateway: SandboxGateway, id: string): Promise<void> {
  await unary(gateway, "Delete", { id }, undefined);
}

/** Like `mkdir -p`: an existing directory is a success. */
export async function makeDir(
  gateway: SandboxGateway,
  id: string,
  path: string,
  signal?: AbortSignal,
): Promise<void> {
  await unary(gateway, "MakeDir", { id, path }, signal);
}

function fileUrl(gateway: SandboxGateway, id: string, path: string): string {
  return `${gateway.sandboxUrl}/sandboxes/${encodeURIComponent(id)}/files?path=${encodeURIComponent(path)}`;
}

async function fileRefusal(res: Response, call: string): Promise<SandboxError> {
  // Read, so that the connection is free again; what it says is not kept.
  await readTextCapped(res, ERROR_BODY_LIMIT);
  return new SandboxError(
    KIND_BY_FILE_STATUS.get(res.status) ?? "unavailable",
    call,
    String(res.status),
  );
}

/** The bytes of a file by its absolute path; `absent` when it is not there, `tooLarge` over `limit`. */
export async function readSandboxFile(
  gateway: SandboxGateway,
  id: string,
  path: string,
  limit: number,
  signal?: AbortSignal,
): Promise<Uint8Array> {
  const bounded = withTimeout(signal, FILE_TIMEOUT_MS);
  try {
    const res = await providerFetch(
      fileUrl(gateway, id, path),
      { method: "GET", headers: headers(gateway), signal: bounded.signal },
      "none",
    );
    if (!res.ok) {
      throw await fileRefusal(res, "read");
    }
    const bytes = await readBodyCapped(res, limit);
    if (bytes === null) {
      throw new SandboxError("tooLarge", "read", `over ${limit} bytes`);
    }
    return bytes;
  } catch (err) {
    throw unreachable(err, "read", signal);
  } finally {
    bounded.cancel();
  }
}

/** Writes a file by its absolute path, replacing what is there. */
export async function writeSandboxFile(
  gateway: SandboxGateway,
  id: string,
  path: string,
  bytes: Uint8Array,
  signal?: AbortSignal,
): Promise<void> {
  const bounded = withTimeout(signal, FILE_TIMEOUT_MS);
  try {
    const res = await providerFetch(
      fileUrl(gateway, id, path),
      {
        method: "PUT",
        headers: headers(gateway, "application/octet-stream"),
        body: bytes,
        signal: bounded.signal,
      },
      "none",
    );
    if (!res.ok) {
      throw await fileRefusal(res, "write");
    }
  } catch (err) {
    throw unreachable(err, "write", signal);
  } finally {
    bounded.cancel();
  }
}

/** One message in a Connect envelope: a flag byte, the length as a big-endian uint32, the JSON. */
function envelope(message: Record<string, unknown>): Uint8Array {
  const json = Buffer.from(JSON.stringify(message), "utf8");
  const header = Buffer.alloc(ENVELOPE_HEADER_BYTES);
  header.writeUInt32BE(json.length, 1);
  return Buffer.concat([header, json]);
}

/** What is kept of one output stream: at most `limit` characters, and whether more came. */
class CappedText {
  public text = "";
  public truncated = false;

  constructor(private readonly limit: number) {}

  public add(chunk: string): void {
    const room = this.limit - this.text.length;
    if (chunk.length > room) {
      this.truncated = true;
    }
    this.text += chunk.slice(0, Math.max(room, 0));
  }
}

/**
 * Runs one shell command and waits for it to exit. A command that fails is an answer (its
 * `code`), not an error; the call rejects when the sandbox does not answer, the stream ends
 * without the exit event, or the caller's signal fires, which also kills the command.
 */
export async function execInSandbox(
  gateway: SandboxGateway,
  id: string,
  command: string,
  options: ExecOptions,
): Promise<ExecResult> {
  const bounded = withTimeout(options.signal, options.timeoutSeconds * 1000 + EXEC_GRACE_MS);
  try {
    const res = await providerFetch(
      `${gateway.sandboxUrl}/${SERVICE}/Exec`,
      {
        method: "POST",
        headers: {
          ...headers(gateway, "application/connect+json"),
          "Connect-Protocol-Version": "1",
        },
        body: envelope({
          id,
          command,
          ...(options.cwd ? { cwd: options.cwd } : {}),
          ...(options.env ? { env: options.env } : {}),
          timeout: `${options.timeoutSeconds}s`,
        }),
        signal: bounded.signal,
      },
      "none",
    );
    if (!res.ok || !res.body) {
      throw await refusal(res, "Exec");
    }
    return await readExecStream(res.body, options.maxOutputChars);
  } catch (err) {
    throw unreachable(err, "Exec", options.signal);
  } finally {
    bounded.cancel();
  }
}

async function readExecStream(
  body: ReadableStream<Uint8Array>,
  maxOutputChars: number,
): Promise<ExecResult> {
  const stdout = new CappedText(maxOutputChars);
  const stderr = new CappedText(maxOutputChars);
  let code: number | undefined;
  let ended = false;
  let pending: Buffer = Buffer.alloc(0);
  const reader = body.getReader();
  try {
    for (;;) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }
      pending = Buffer.concat([pending, value]);
      while (pending.length >= ENVELOPE_HEADER_BYTES) {
        const length = pending.readUInt32BE(1);
        if (length > MAX_FRAME_BYTES) {
          throw new SandboxError("tooLarge", "Exec", `a message of ${length} bytes`);
        }
        if (pending.length < ENVELOPE_HEADER_BYTES + length) {
          break;
        }
        const flags = pending.readUInt8(0);
        const message = parseMessage(
          pending.subarray(ENVELOPE_HEADER_BYTES, ENVELOPE_HEADER_BYTES + length),
        );
        pending = pending.subarray(ENVELOPE_HEADER_BYTES + length);
        if ((flags & END_STREAM_FLAG) !== 0) {
          const error = message["error"];
          if (isObject(error)) {
            const errorCode = getString(error, "code") ?? "";
            throw new SandboxError(kindOf(errorCode, "Exec"), "Exec", errorCode);
          }
          ended = true;
          continue;
        }
        const out = getString(message, "stdout");
        const err = getString(message, "stderr");
        const exit = message["exit"];
        if (out !== undefined) {
          stdout.add(out);
        } else if (err !== undefined) {
          stderr.add(err);
        } else if (isObject(exit)) {
          // proto3 JSON leaves a zero out: an exit event without a code is exit status 0.
          const exitCode = exit["code"];
          code = typeof exitCode === "number" ? exitCode : 0;
        }
      }
    }
  } finally {
    await cancelReader(reader);
  }
  if (code === undefined || !ended) {
    throw new SandboxError("unavailable", "Exec", "the stream ended before the command's exit");
  }
  return {
    stdout: stdout.text,
    stderr: stderr.text,
    code,
    truncated: stdout.truncated || stderr.truncated,
  };
}

function parseMessage(bytes: Uint8Array): Record<string, unknown> {
  let message: unknown;
  try {
    message = JSON.parse(Buffer.from(bytes).toString("utf8"));
  } catch {
    throw new SandboxError("unavailable", "Exec", "a message that is not JSON");
  }
  if (!isObject(message)) {
    throw new SandboxError("unavailable", "Exec", "a message that is not an object");
  }
  return message;
}
