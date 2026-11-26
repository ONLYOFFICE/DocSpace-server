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

// Builders of what the A2A client of @a2a-js/sdk hands documentAgentClient: the stream events,
// the tasks a read answers, and a client whose four calls are spies. The real client cannot run
// against msw: the transport fetches through undici's own `fetch` with a dispatcher of its own,
// which msw does not intercept, so the tests stand in for the SDK's client instead.

import { Role, TaskState } from "@a2a-js/sdk";
import type { Artifact, Message, Part, StreamResponse, Task } from "@a2a-js/sdk";
import type { Client } from "@a2a-js/sdk/client";
import type { Mock } from "vitest";
import { vi } from "vitest";

export { TaskState };

export function textPart(value: string): Part {
  return { content: { $case: "text", value }, metadata: undefined, filename: "", mediaType: "" };
}

export function urlPart(url: string, filename = "", mediaType = ""): Part {
  return { content: { $case: "url", value: url }, metadata: undefined, filename, mediaType };
}

export function dataPart(value: unknown): Part {
  return { content: { $case: "data", value }, metadata: undefined, filename: "", mediaType: "" };
}

export function agentMessage(...parts: Part[]): Message {
  return {
    messageId: "m",
    contextId: "c",
    taskId: "t",
    role: Role.ROLE_AGENT,
    parts,
    metadata: undefined,
    extensions: [],
    referenceTaskIds: [],
  };
}

export function artifact(name: string, parts: Part[], kind?: string): Artifact {
  return {
    artifactId: `a-${name}`,
    name,
    description: "",
    parts,
    metadata: kind === undefined ? undefined : { docagent: { kind } },
    extensions: [],
  } as Artifact;
}

export function task(
  id: string,
  state: TaskState | undefined,
  message?: Message,
  artifacts: Artifact[] = [],
): Task {
  return {
    id,
    contextId: "c",
    status: state === undefined ? undefined : { state, message, timestamp: undefined },
    artifacts,
    history: [],
    metadata: undefined,
  } as Task;
}

export function taskEvent(
  id: string,
  state: TaskState | undefined,
  message?: Message,
  artifacts: Artifact[] = [],
): StreamResponse {
  return { payload: { $case: "task", value: task(id, state, message, artifacts) } };
}

export function statusEvent(state: TaskState | undefined, message?: Message): StreamResponse {
  return {
    payload: {
      $case: "statusUpdate",
      value: {
        taskId: "t",
        contextId: "c",
        status: state === undefined ? undefined : { state, message, timestamp: undefined },
        metadata: undefined,
      },
    },
  } as StreamResponse;
}

export function artifactEvent(value: Artifact | undefined): StreamResponse {
  return {
    payload: {
      $case: "artifactUpdate",
      value: { taskId: "t", contextId: "c", artifact: value, append: false, lastChunk: true },
    },
  } as StreamResponse;
}

export function messageEvent(): StreamResponse {
  return { payload: { $case: "message", value: agentMessage(textPart("hello")) } };
}

/** A stream that yields `events`, then throws `error` when one is given. */
export async function* streamOf(
  events: StreamResponse[],
  error?: unknown,
): AsyncGenerator<StreamResponse, void, undefined> {
  for (const event of events) {
    yield await Promise.resolve(event);
  }
  if (error !== undefined) {
    await Promise.reject(error as Error);
  }
}

export interface FakeClient {
  sendMessageStream: Mock<Client["sendMessageStream"]>;
  resubscribeTask: Mock<Client["resubscribeTask"]>;
  getTask: Mock<Client["getTask"]>;
  cancelTask: Mock<Client["cancelTask"]>;
}

/** A client whose calls are spies; by default the stream is empty and cancel succeeds. */
export function fakeClient(): FakeClient {
  return {
    sendMessageStream: vi.fn<Client["sendMessageStream"]>(() => streamOf([])),
    resubscribeTask: vi.fn<Client["resubscribeTask"]>(() => streamOf([])),
    getTask: vi.fn<Client["getTask"]>(),
    cancelTask: vi.fn<Client["cancelTask"]>(() => Promise.resolve(task("t", undefined))),
  };
}
