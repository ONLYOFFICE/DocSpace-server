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

// One task on the document agent over A2A, through the protocol's own client
// (`@a2a-js/sdk/client`): the agent card is read once, the message carries
// the task text, the input files as bytes and the run settings in
// `metadata.docagent`, and the stream is drained until the task ends. The
// agent's status lines (its one-line intent before every tool call, sent as
// working-state status updates) go to the log and to the caller as steps; a
// task that stops for input or auth (A2A's interrupted states) is cancelled,
// as nobody here can answer it; what comes back is the final state, the
// agent's summary and the files it produced.

import { randomUUID } from "crypto";
import { Role, TaskState } from "@a2a-js/sdk";
import type { Artifact, Message, Part, Task } from "@a2a-js/sdk";
import { ClientFactory, JsonRpcTransportFactory } from "@a2a-js/sdk/client";
import type { Client } from "@a2a-js/sdk/client";
import type { DocumentAgentConfig } from "../../config/index.js";
import logger from "../log.js";

/** The `metadata.docagent` block of a request, as the agent's settings schema reads it. */
export interface DocumentAgentSettings {
  model: {
    provider: string;
    baseUrl?: string;
    apiKey?: string;
    headers?: Record<string, string>;
    modelId: string;
    reasoning?: "off" | "low" | "medium" | "high" | "max";
    responsesApi?: boolean;
  };
  mcpServers?: Record<string, { url: string; headers?: Record<string, string> }>;
  limits?: { maxToolCalls?: number; taskTimeoutSeconds?: number; maxFileBytes?: number };
}

export interface DocumentTaskInput {
  task: string;
  files: readonly { name: string; bytes: Uint8Array; mediaType: string }[];
  settings: DocumentAgentSettings;
  signal: AbortSignal;
  /**
   * The agent's one-line status of each step: the text of a status update in
   * the working state, from the stream or, once the stream is lost, from the
   * reads that follow the task. Each distinct step once.
   */
  onStatus?: (text: string) => void;
}

export interface DocumentTaskOutput {
  state:
    | "completed"
    | "failed"
    | "rejected"
    | "canceled"
    | "input-required"
    | "auth-required"
    | "unknown";
  /** The agent's final text: its summary, or the reason it did not finish. */
  text: string;
  artifacts: { name: string; bytes: Uint8Array; mediaType: string }[];
}

export class DocumentAgentError extends Error {
  constructor(message: string, options?: { cause?: unknown }) {
    super(message, options);
    this.name = "DocumentAgentError";
  }
}

let cached: { url: string; token: string; client: Promise<Client> } | undefined;

// A request that is not the task stream: a cancel, a task read.
const CALL_TIMEOUT_MS = 30_000;
// Between task reads while the stream is gone and the task still runs.
const POLL_INTERVAL_MS = 5_000;

function callSignal(outer: AbortSignal): AbortSignal {
  return AbortSignal.any([outer, AbortSignal.timeout(CALL_TIMEOUT_MS)]);
}

function sleep(ms: number, signal: AbortSignal): Promise<void> {
  return new Promise((resolve) => {
    const timer = setTimeout(done, ms);
    function done(): void {
      clearTimeout(timer);
      signal.removeEventListener("abort", done);
      resolve();
    }
    signal.addEventListener("abort", done, { once: true });
  });
}

// The agent's bearer token goes on every request of the transport; the SDK
// takes a fetch implementation for exactly that. The client, and with it
// the agent card it was built from, is kept for the process; a changed URL
// or token builds a new one, a failed build is forgotten so the next call
// tries again.
function clientFor(config: DocumentAgentConfig): Promise<Client> {
  if (cached && cached.url === config.url && cached.token === config.token) {
    return cached.client;
  }
  const authedFetch: typeof fetch = (input, init) => {
    // The SDK passes its own headers (content type, accept) in whatever
    // shape; a Headers copy keeps them all, with the token on top.
    const headers = new Headers(init?.headers);
    headers.set("authorization", `Bearer ${config.token}`);
    return fetch(input, { ...init, headers });
  };
  const client = new ClientFactory({
    transports: [new JsonRpcTransportFactory({ fetchImpl: authedFetch })],
  }).createFromUrl(config.url);
  const entry = { url: config.url, token: config.token, client };
  cached = entry;
  client.catch(() => {
    if (cached === entry) {
      cached = undefined;
    }
  });
  return client;
}

function textPart(text: string): Part {
  return {
    content: { $case: "text", value: text },
    metadata: undefined,
    filename: "",
    mediaType: "text/plain",
  };
}

function filePart(file: DocumentTaskInput["files"][number]): Part {
  return {
    content: { $case: "raw", value: Buffer.from(file.bytes) },
    metadata: undefined,
    filename: file.name,
    mediaType: file.mediaType,
  };
}

function userMessage(input: DocumentTaskInput): Message {
  return {
    messageId: randomUUID(),
    contextId: "",
    taskId: "",
    role: Role.ROLE_USER,
    parts: [textPart(input.task), ...input.files.map(filePart)],
    metadata: { docagent: input.settings },
    extensions: [],
    referenceTaskIds: [],
  };
}

function stateOf(state: TaskState | undefined): DocumentTaskOutput["state"] {
  switch (state) {
    case TaskState.TASK_STATE_COMPLETED:
      return "completed";
    case TaskState.TASK_STATE_FAILED:
      return "failed";
    case TaskState.TASK_STATE_REJECTED:
      return "rejected";
    case TaskState.TASK_STATE_CANCELED:
      return "canceled";
    case TaskState.TASK_STATE_INPUT_REQUIRED:
      return "input-required";
    case TaskState.TASK_STATE_AUTH_REQUIRED:
      return "auth-required";
    default:
      return "unknown";
  }
}

function messageText(message: Message | undefined): string {
  if (!message) {
    return "";
  }
  return message.parts
    .map((part) => (part.content?.$case === "text" ? part.content.value : ""))
    .filter((text) => text.length > 0)
    .join("\n")
    .trim();
}

function artifactFiles(artifact: Artifact): DocumentTaskOutput["artifacts"] {
  const out: DocumentTaskOutput["artifacts"] = [];
  for (const part of artifact.parts) {
    if (part.content?.$case !== "raw") {
      continue;
    }
    const name = part.filename || artifact.name || "output";
    out.push({
      name,
      bytes: part.content.value,
      mediaType: part.mediaType || "application/octet-stream",
    });
  }
  return out;
}

// Whether the run is over for this client: a terminal state (completed,
// failed, rejected, canceled) or an interrupted one (input-required,
// auth-required), which waits for an answer this tool cannot give.
function endsRun(state: TaskState | undefined): boolean {
  return stateOf(state) !== "unknown";
}

function isInterrupted(state: TaskState | undefined): boolean {
  return (
    state === TaskState.TASK_STATE_INPUT_REQUIRED || state === TaskState.TASK_STATE_AUTH_REQUIRED
  );
}

async function cancel(client: Client, taskId: string): Promise<void> {
  try {
    await client.cancelTask(
      { tenant: "", id: taskId, metadata: undefined },
      { signal: AbortSignal.timeout(CALL_TIMEOUT_MS) },
    );
  } catch (err) {
    logger.warn(
      `documentAgent: task ${taskId} not cancelled: ${err instanceof Error ? err.message : String(err)}`,
    );
  }
}

// Reads the task until its run ends (see endsRun), handing the status text
// of every read in the working state to `onWorking`, so the steps keep
// showing after the stream is lost. On the caller's abort the task is
// cancelled and the abort reason thrown; undefined when a read failed for
// good (the task is then unknown, not abandoned: the agent ends it itself).
async function follow(
  client: Client,
  taskId: string,
  signal: AbortSignal,
  onWorking: (text: string) => void,
): Promise<Task | undefined> {
  let failures = 0;
  for (;;) {
    if (signal.aborted) {
      await cancel(client, taskId);
      throw signal.reason instanceof Error
        ? signal.reason
        : new DocumentAgentError("document task aborted");
    }
    try {
      const task = await client.getTask(
        { tenant: "", id: taskId, historyLength: undefined },
        { signal: callSignal(signal) },
      );
      failures = 0;
      if (endsRun(task.status?.state)) {
        return task;
      }
      if (task.status?.state === TaskState.TASK_STATE_WORKING) {
        onWorking(messageText(task.status.message));
      }
    } catch (err) {
      if (signal.aborted) {
        continue;
      }
      failures += 1;
      logger.warn(
        `documentAgent: task ${taskId} read failed (${failures}): ${
          err instanceof Error ? err.message : String(err)
        }`,
      );
      if (failures >= 3) {
        return undefined;
      }
    }
    await sleep(POLL_INTERVAL_MS, signal);
  }
}

/**
 * Runs one task to its end. Resolves with whatever terminal state the agent
 * reported; rejects with {@link DocumentAgentError} only when the agent could
 * not be reached or the protocol broke, and with the signal's reason when the
 * caller aborted (the task is then cancelled on the agent, best effort).
 */
export async function runDocumentTask(
  config: DocumentAgentConfig,
  input: DocumentTaskInput,
): Promise<DocumentTaskOutput> {
  let client: Client;
  try {
    client = await clientFor(config);
  } catch (err) {
    throw new DocumentAgentError(`document agent at ${config.url} is unreachable`, { cause: err });
  }

  let taskId: string | undefined;
  let finalState: TaskState | undefined;
  let finalText = "";
  const artifacts: DocumentTaskOutput["artifacts"] = [];
  const started = Date.now();
  // A step is a status update in the working state with text; a read while
  // polling repeats the last one until the agent moves on, so a repeat of
  // the step just reported is not a new one.
  let lastStep = "";
  const reportStep = (text: string): void => {
    if (!text || text === lastStep) {
      return;
    }
    lastStep = text;
    logger.info(`documentAgent: task ${taskId ?? "?"} ${text}`);
    input.onStatus?.(text);
  };

  try {
    const stream = client.sendMessageStream(
      { tenant: "", message: userMessage(input), configuration: undefined, metadata: undefined },
      { signal: input.signal },
    );
    for await (const event of stream) {
      const payload = event.payload;
      switch (payload?.$case) {
        case "task": {
          taskId = payload.value.id;
          logger.info(`documentAgent: task ${taskId} submitted (${input.files.length} file(s))`);
          break;
        }
        case "statusUpdate": {
          const status = payload.value.status;
          const text = messageText(status?.message);
          if (endsRun(status?.state)) {
            finalState = status?.state;
            finalText = text;
          } else if (status?.state === TaskState.TASK_STATE_WORKING) {
            reportStep(text);
          }
          break;
        }
        case "artifactUpdate": {
          const artifact = payload.value.artifact;
          if (artifact) {
            artifacts.push(...artifactFiles(artifact));
          }
          break;
        }
        default:
          break;
      }
    }
  } catch (err) {
    if (input.signal.aborted) {
      if (taskId) {
        await cancel(client, taskId);
      }
      throw input.signal.reason instanceof Error
        ? input.signal.reason
        : new DocumentAgentError("document task aborted");
    }
    if (taskId === undefined) {
      throw new DocumentAgentError(
        `document agent request failed: ${err instanceof Error ? err.message : String(err)}`,
        { cause: err },
      );
    }
    // The task was accepted and the stream broke under it. Node's fetch gives a
    // body five minutes of silence, and the agent speaks only before a tool
    // call, so a long model turn ends the stream while the task goes on: the
    // task is followed by reads until it ends, or the caller's deadline does.
    logger.warn(
      `documentAgent: task ${taskId} stream lost (${
        err instanceof Error ? err.message : String(err)
      }), following the task by polling`,
    );
  }

  // A stream that ended without a terminal status: follow the task itself.
  if (finalState === undefined && taskId) {
    const task = await follow(client, taskId, input.signal, reportStep);
    if (task === undefined) {
      throw new DocumentAgentError(`task ${taskId} could not be read back after the stream ended`);
    }
    finalState = task.status?.state;
    finalText = messageText(task.status?.message);
    if (artifacts.length === 0) {
      for (const artifact of task.artifacts) {
        artifacts.push(...artifactFiles(artifact));
      }
    }
  }

  // An interrupted task waits for input it will never get from here; the
  // agent is told to stop rather than left holding a sandbox.
  if (isInterrupted(finalState) && taskId) {
    await cancel(client, taskId);
  }

  const state = stateOf(finalState);
  logger.info(
    `documentAgent: task ${taskId ?? "?"} ${state} in ${Date.now() - started}ms, ` +
      `${artifacts.length} file(s) back`,
  );
  return { state, text: finalText, artifacts };
}
