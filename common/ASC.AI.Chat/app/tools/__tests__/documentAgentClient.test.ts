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

import { Agent } from "undici";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { DocumentAgentConfig } from "../../../config/index.js";
import type { DocumentAgentSettings, DocumentTaskInput } from "../documentAgentClient.js";
import {
  TaskState,
  agentMessage,
  artifact,
  artifactEvent,
  dataPart,
  fakeClient,
  messageEvent,
  statusEvent,
  streamOf,
  task,
  taskEvent,
  textPart,
  urlPart,
} from "./documentAgentA2aFakes.js";
import type { FakeClient } from "./documentAgentA2aFakes.js";

// The A2A SDK's client factory and undici's fetch are the boundary: the factory hands out the
// fake client of each test, and the transport's fetch is read back from the options the
// service built it with.
const sdk = vi.hoisted(() => ({
  factoryOptions: [] as unknown[],
  transportOptions: [] as { fetchImpl: typeof fetch }[],
  createFromUrl: vi.fn<(url: string) => Promise<unknown>>(),
  undiciFetch: vi.fn<(input: unknown, init: unknown) => Promise<unknown>>(),
}));
vi.mock("@a2a-js/sdk/client", () => ({
  ClientFactory: class {
    constructor(options: unknown) {
      sdk.factoryOptions.push(options);
    }
    createFromUrl(url: string): Promise<unknown> {
      return sdk.createFromUrl(url);
    }
  },
  JsonRpcTransportFactory: class {
    constructor(options: { fetchImpl: typeof fetch }) {
      sdk.transportOptions.push(options);
    }
  },
}));
vi.mock("undici", async (original) => ({
  ...(await original<typeof import("undici")>()),
  fetch: sdk.undiciFetch,
}));

type ClientModule = typeof import("../documentAgentClient.js");
type Logger = (typeof import("../../log.js"))["default"];

const config: DocumentAgentConfig = {
  url: "http://agent:9838",
  token: "secret",
  taskTimeoutSeconds: 1800,
  maxToolCalls: undefined,
  shareSystemServers: false,
  portalUrl: undefined,
  filesUrl: "http://chat:5157",
};

const settings: DocumentAgentSettings = {
  model: { provider: "openai", modelId: "gpt-5", reasoning: "medium" },
  outputs: { url: "http://chat:5157/document-agent/files/outputs?folder=5" },
};

let mod: ClientModule;
let logger: Logger;
let client: FakeClient;

function input(overrides: Partial<DocumentTaskInput> = {}): DocumentTaskInput {
  return { task: "Write a report", settings, signal: new AbortController().signal, ...overrides };
}

function warnings(): string[] {
  return vi.mocked(logger.warn).mock.calls.map((call) => String(call[0]));
}

function infos(): string[] {
  return vi.mocked(logger.info).mock.calls.map((call) => String(call[0]));
}

beforeEach(async () => {
  // The client is cached for the process: every test starts from a fresh module.
  vi.resetModules();
  sdk.factoryOptions.length = 0;
  sdk.transportOptions.length = 0;
  client = fakeClient();
  sdk.createFromUrl.mockImplementation(() => Promise.resolve(client));
  mod = await import("../documentAgentClient.js");
  logger = (await import("../../log.js")).default;
});

describe("the A2A client of the document agent", () => {
  it("builds one client from the agent's URL and keeps it for the next task", async () => {
    await mod.runDocumentTask(config, input());
    await mod.runDocumentTask(config, input());

    expect(sdk.createFromUrl).toHaveBeenCalledTimes(1);
    expect(sdk.createFromUrl).toHaveBeenCalledWith("http://agent:9838");
    expect(sdk.factoryOptions).toHaveLength(1);
    expect(sdk.factoryOptions[0]).toEqual({ transports: [expect.any(Object)] });
  });

  it.each([
    ["a changed token", { ...config, token: "other" }],
    ["a changed URL", { ...config, url: "http://agent2:9838" }],
  ])("builds a new client for %s", async (_what, changed) => {
    await mod.runDocumentTask(config, input());
    await mod.runDocumentTask(changed, input());

    expect(sdk.createFromUrl).toHaveBeenCalledTimes(2);
    expect(sdk.createFromUrl).toHaveBeenLastCalledWith(changed.url);
  });

  it("rejects with DocumentAgentError naming the URL when the client cannot be built, and tries again next time", async () => {
    const cause = new Error("card unreachable");
    sdk.createFromUrl.mockImplementationOnce(() => Promise.reject(cause));

    const failed = mod.runDocumentTask(config, input());

    await expect(failed).rejects.toBeInstanceOf(mod.DocumentAgentError);
    await expect(failed).rejects.toMatchObject({
      name: "DocumentAgentError",
      message: "document agent at http://agent:9838 is unreachable",
      cause,
    });
    await expect(mod.runDocumentTask(config, input())).resolves.toMatchObject({ state: "unknown" });
    expect(sdk.createFromUrl).toHaveBeenCalledTimes(2);
  });

  it("keeps a newer client when an older build fails after it was replaced", async () => {
    let rejectFirst: (err: Error) => void = () => undefined;
    sdk.createFromUrl.mockImplementationOnce(
      () =>
        new Promise((_resolve, reject) => {
          rejectFirst = reject;
        }),
    );
    const first = mod.runDocumentTask(config, input());
    const other = { ...config, token: "other" };
    await mod.runDocumentTask(other, input());
    rejectFirst(new Error("late failure"));
    await expect(first).rejects.toBeInstanceOf(mod.DocumentAgentError);

    await mod.runDocumentTask(other, input());

    expect(sdk.createFromUrl).toHaveBeenCalledTimes(2);
  });

  it("sends every transport request through undici with the agent's bearer token over the caller's headers", async () => {
    await mod.runDocumentTask(config, input());
    const fetchImpl = sdk.transportOptions[0]!.fetchImpl;
    const answer = new Response("{}");
    sdk.undiciFetch.mockResolvedValue(answer);

    const res = await fetchImpl("http://agent:9838/rpc", {
      method: "POST",
      body: "{}",
      headers: { "content-type": "application/json", authorization: "Bearer someone-else" },
    });

    expect(res).toBe(answer);
    expect(sdk.undiciFetch).toHaveBeenCalledWith("http://agent:9838/rpc", {
      method: "POST",
      body: "{}",
      headers: { "content-type": "application/json", authorization: "Bearer secret" },
      dispatcher: expect.any(Agent),
    });
  });

  it("adds the bearer token to a request that comes without init", async () => {
    await mod.runDocumentTask(config, input());
    sdk.undiciFetch.mockResolvedValue(new Response("{}"));

    await sdk.transportOptions[0]!.fetchImpl("http://agent:9838/.well-known/agent-card.json");

    expect(sdk.undiciFetch).toHaveBeenCalledWith("http://agent:9838/.well-known/agent-card.json", {
      headers: { authorization: "Bearer secret" },
      dispatcher: expect.any(Agent),
    });
  });
});

describe("runDocumentTask", () => {
  it("sends the task as one user message with the settings in the request's metadata", async () => {
    const signal = new AbortController().signal;

    await mod.runDocumentTask(config, input({ signal }));

    expect(client.sendMessageStream).toHaveBeenCalledTimes(1);
    expect(client.sendMessageStream).toHaveBeenCalledWith(
      {
        tenant: "",
        message: {
          messageId: expect.stringMatching(
            /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/,
          ),
          contextId: "",
          taskId: "",
          role: 1,
          parts: [
            {
              content: { $case: "text", value: "Write a report" },
              metadata: undefined,
              filename: "",
              mediaType: "text/plain",
            },
          ],
          metadata: undefined,
          extensions: [],
          referenceTaskIds: [],
        },
        configuration: undefined,
        metadata: { docagent: settings },
      },
      { signal },
    );
  });

  it("answers the final state, the agent's summary and the delivered files of a completed task", async () => {
    client.sendMessageStream.mockImplementation(() =>
      streamOf([
        taskEvent("t1", TaskState.TASK_STATE_SUBMITTED),
        artifactEvent(
          artifact("Report", [
            urlPart("https://chat/doceditor?fileId=30", "Report.docx", "application/docx"),
            textPart("not a file"),
          ]),
        ),
        artifactEvent(artifact("task-log", [urlPart("https://chat/doceditor?fileId=31")], "log")),
        artifactEvent(artifact("", [urlPart("https://chat/doceditor?fileId=32")], "other")),
        artifactEvent(undefined),
        statusEvent(
          TaskState.TASK_STATE_COMPLETED,
          agentMessage(
            textPart("  Done."),
            dataPart({ x: 1 }),
            textPart(""),
            textPart("Two files. "),
          ),
        ),
      ]),
    );

    const result = await mod.runDocumentTask(config, input());

    expect(result).toEqual({
      state: "completed",
      text: "Done.\nTwo files.",
      artifacts: [
        {
          name: "Report.docx",
          mediaType: "application/docx",
          url: "https://chat/doceditor?fileId=30",
        },
        {
          name: "task-log",
          mediaType: "application/octet-stream",
          url: "https://chat/doceditor?fileId=31",
          kind: "log",
        },
        {
          name: "output",
          mediaType: "application/octet-stream",
          url: "https://chat/doceditor?fileId=32",
        },
      ],
    });
    expect(infos()[0]).toBe("documentAgent: task t1 submitted");
    expect(infos().at(-1)).toMatch(/^documentAgent: task t1 completed in \d+ms, 3 file\(s\) back$/);
    expect(client.cancelTask).not.toHaveBeenCalled();
    expect(client.resubscribeTask).not.toHaveBeenCalled();
    expect(client.getTask).not.toHaveBeenCalled();
  });

  it("reports each distinct working step once, to the log and to the caller", async () => {
    const onStatus = vi.fn();
    client.sendMessageStream.mockImplementation(() =>
      streamOf([
        taskEvent("t1", TaskState.TASK_STATE_WORKING, agentMessage(textPart("Reading the file"))),
        statusEvent(TaskState.TASK_STATE_WORKING, agentMessage(textPart("Reading the file"))),
        statusEvent(TaskState.TASK_STATE_WORKING, agentMessage(textPart("Writing"))),
        statusEvent(TaskState.TASK_STATE_WORKING, agentMessage()),
        statusEvent(TaskState.TASK_STATE_WORKING),
        statusEvent(TaskState.TASK_STATE_SUBMITTED, agentMessage(textPart("queued"))),
        statusEvent(undefined),
        messageEvent(),
        {},
        statusEvent(TaskState.TASK_STATE_WORKING, agentMessage(textPart("Reading the file"))),
        taskEvent("t2", TaskState.TASK_STATE_COMPLETED),
      ]),
    );

    const result = await mod.runDocumentTask(config, input({ onStatus }));

    expect(onStatus.mock.calls).toEqual([["Reading the file"], ["Writing"], ["Reading the file"]]);
    expect(infos()).toContain("documentAgent: task t1 Reading the file");
    expect(infos()).toContain("documentAgent: task t1 Writing");
    // The id of the first task event stays the task's id.
    expect(infos().at(-1)).toMatch(/^documentAgent: task t1 completed/);
    expect(result).toEqual({ state: "completed", text: "", artifacts: [] });
  });

  it("logs a step reported before any task event under an unknown id, and runs without onStatus", async () => {
    client.sendMessageStream.mockImplementation(() =>
      streamOf([
        statusEvent(TaskState.TASK_STATE_WORKING, agentMessage(textPart("Thinking"))),
        statusEvent(TaskState.TASK_STATE_COMPLETED, agentMessage(textPart("ok"))),
      ]),
    );

    const result = await mod.runDocumentTask(config, input());

    expect(infos()).toContain("documentAgent: task ? Thinking");
    expect(infos().at(-1)).toMatch(/^documentAgent: task \? completed in \d+ms, 0 file\(s\) back$/);
    expect(result).toEqual({ state: "completed", text: "ok", artifacts: [] });
  });

  it("takes the files of a terminal task event when no artifact update came before it", async () => {
    client.sendMessageStream.mockImplementation(() =>
      streamOf([
        taskEvent("t1", TaskState.TASK_STATE_FAILED, agentMessage(textPart("Out of budget")), [
          artifact("Partial", [urlPart("https://chat/doceditor?fileId=7", "Partial.docx")]),
        ]),
      ]),
    );

    expect(await mod.runDocumentTask(config, input())).toEqual({
      state: "failed",
      text: "Out of budget",
      artifacts: [
        {
          name: "Partial.docx",
          mediaType: "application/octet-stream",
          url: "https://chat/doceditor?fileId=7",
        },
      ],
    });
  });

  it("keeps the files already delivered when a terminal task event repeats them", async () => {
    client.sendMessageStream.mockImplementation(() =>
      streamOf([
        artifactEvent(artifact("A", [urlPart("https://chat/doceditor?fileId=1", "A.docx")])),
        taskEvent("t1", TaskState.TASK_STATE_COMPLETED, undefined, [
          artifact("A", [urlPart("https://chat/doceditor?fileId=1", "A.docx")]),
          artifact("B", [urlPart("https://chat/doceditor?fileId=2", "B.docx")]),
        ]),
      ]),
    );

    const result = await mod.runDocumentTask(config, input());

    expect(result.artifacts.map((file) => file.name)).toEqual(["A.docx"]);
  });

  it("answers a task event without a status as not ended and does not follow a task it has no id of", async () => {
    client.sendMessageStream.mockImplementation(() =>
      streamOf([{ payload: { $case: "task", value: { ...task("t1", undefined) } } }]),
    );
    client.resubscribeTask.mockImplementation(() =>
      streamOf([statusEvent(TaskState.TASK_STATE_COMPLETED)]),
    );

    expect(await mod.runDocumentTask(config, input())).toEqual({
      state: "completed",
      text: "",
      artifacts: [],
    });
    expect(client.resubscribeTask).toHaveBeenCalledWith(
      { tenant: "", id: "t1" },
      { signal: expect.any(AbortSignal) },
    );
  });

  it.each([
    [TaskState.TASK_STATE_FAILED, "failed"],
    [TaskState.TASK_STATE_REJECTED, "rejected"],
    [TaskState.TASK_STATE_CANCELED, "canceled"],
  ])("answers the terminal state %s as %s without cancelling it", async (state, name) => {
    client.sendMessageStream.mockImplementation(() =>
      streamOf([taskEvent("t1", TaskState.TASK_STATE_SUBMITTED), statusEvent(state)]),
    );

    expect(await mod.runDocumentTask(config, input())).toEqual({
      state: name,
      text: "",
      artifacts: [],
    });
    expect(client.cancelTask).not.toHaveBeenCalled();
  });

  it.each([
    [TaskState.TASK_STATE_INPUT_REQUIRED, "input-required"],
    [TaskState.TASK_STATE_AUTH_REQUIRED, "auth-required"],
  ])("cancels a task interrupted in %s and answers %s", async (state, name) => {
    client.sendMessageStream.mockImplementation(() =>
      streamOf([
        taskEvent("t1", TaskState.TASK_STATE_SUBMITTED),
        statusEvent(state, agentMessage(textPart("Which file?"))),
      ]),
    );

    expect(await mod.runDocumentTask(config, input())).toEqual({
      state: name,
      text: "Which file?",
      artifacts: [],
    });
    expect(client.cancelTask).toHaveBeenCalledWith(
      { tenant: "", id: "t1", metadata: undefined },
      { signal: expect.any(AbortSignal) },
    );
  });

  it("does not cancel an interrupted task it has no id of", async () => {
    client.sendMessageStream.mockImplementation(() =>
      streamOf([statusEvent(TaskState.TASK_STATE_INPUT_REQUIRED)]),
    );

    expect((await mod.runDocumentTask(config, input())).state).toBe("input-required");
    expect(client.cancelTask).not.toHaveBeenCalled();
  });

  it.each([
    ["an Error", new Error("refused"), "refused"],
    ["a string", "plain", "plain"],
  ])("logs a cancel that fails with %s and still answers the state", async (_what, err, text) => {
    client.cancelTask.mockRejectedValue(err);
    client.sendMessageStream.mockImplementation(() =>
      streamOf([
        taskEvent("t1", TaskState.TASK_STATE_SUBMITTED),
        statusEvent(TaskState.TASK_STATE_AUTH_REQUIRED),
      ]),
    );

    expect((await mod.runDocumentTask(config, input())).state).toBe("auth-required");
    expect(warnings()).toEqual([`documentAgent: task t1 not cancelled: ${text}`]);
  });

  it("answers unknown when the stream ends before any task event", async () => {
    expect(await mod.runDocumentTask(config, input())).toEqual({
      state: "unknown",
      text: "",
      artifacts: [],
    });
    expect(client.resubscribeTask).not.toHaveBeenCalled();
    expect(infos()).toEqual([
      expect.stringMatching(/^documentAgent: task \? unknown in \d+ms, 0 file\(s\) back$/),
    ]);
  });

  it.each([
    ["an Error", new Error("ECONNREFUSED"), "ECONNREFUSED"],
    ["a string", "socket hang up", "socket hang up"],
  ])(
    "rejects with DocumentAgentError when the request fails with %s before a task exists",
    async (_what, err, text) => {
      client.sendMessageStream.mockImplementation(() => streamOf([], err));

      const run = mod.runDocumentTask(config, input());

      await expect(run).rejects.toBeInstanceOf(mod.DocumentAgentError);
      await expect(run).rejects.toMatchObject({
        message: `document agent request failed: ${text}`,
        cause: err,
      });
      expect(client.cancelTask).not.toHaveBeenCalled();
    },
  );

  it("cancels the task and rethrows the abort reason when the caller aborts the stream", async () => {
    const controller = new AbortController();
    const reason = Object.assign(new Error("The operation was aborted due to timeout"), {
      name: "TimeoutError",
    });
    client.sendMessageStream.mockImplementation(async function* () {
      yield taskEvent("t1", TaskState.TASK_STATE_SUBMITTED);
      controller.abort(reason);
      throw new Error("aborted");
    });

    await expect(mod.runDocumentTask(config, input({ signal: controller.signal }))).rejects.toBe(
      reason,
    );
    expect(client.cancelTask).toHaveBeenCalledWith(
      { tenant: "", id: "t1", metadata: undefined },
      { signal: expect.any(AbortSignal) },
    );
    expect(client.resubscribeTask).not.toHaveBeenCalled();
  });

  it("rejects with DocumentAgentError 'document task aborted' for an abort reason that is no Error, and cancels nothing without a task", async () => {
    const controller = new AbortController();
    client.sendMessageStream.mockImplementation(async function* () {
      yield await Promise.resolve(messageEvent());
      controller.abort("stop");
      throw new Error("aborted");
    });

    const run = mod.runDocumentTask(config, input({ signal: controller.signal }));

    await expect(run).rejects.toBeInstanceOf(mod.DocumentAgentError);
    await expect(run).rejects.toThrow("document task aborted");
    expect(client.cancelTask).not.toHaveBeenCalled();
  });
});

describe("runDocumentTask after the stream broke", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  function brokenStream(err: unknown = new Error("terminated")) {
    client.sendMessageStream.mockImplementation(() =>
      streamOf(
        [
          taskEvent("t1", TaskState.TASK_STATE_SUBMITTED),
          statusEvent(TaskState.TASK_STATE_WORKING, agentMessage(textPart("Step 1"))),
        ],
        err,
      ),
    );
  }

  it.each([
    ["an Error", new Error("terminated"), "terminated"],
    ["a string", "reset", "reset"],
  ])("re-attaches to the task when the stream breaks with %s", async (_what, err, text) => {
    brokenStream(err);
    client.resubscribeTask.mockImplementation(() =>
      streamOf([
        taskEvent("t1", TaskState.TASK_STATE_WORKING, agentMessage(textPart("Step 1"))),
        statusEvent(TaskState.TASK_STATE_WORKING, agentMessage(textPart("Step 2"))),
        statusEvent(TaskState.TASK_STATE_COMPLETED, agentMessage(textPart("Done"))),
      ]),
    );
    const onStatus = vi.fn();
    const signal = new AbortController().signal;

    const result = await mod.runDocumentTask(config, input({ onStatus, signal }));

    expect(result).toEqual({ state: "completed", text: "Done", artifacts: [] });
    expect(onStatus.mock.calls).toEqual([["Step 1"], ["Step 2"]]);
    expect(client.resubscribeTask).toHaveBeenCalledTimes(1);
    expect(client.resubscribeTask).toHaveBeenCalledWith({ tenant: "", id: "t1" }, { signal });
    expect(client.getTask).not.toHaveBeenCalled();
    expect(warnings()).toEqual([`documentAgent: task t1 stream lost (${text}), re-attaching`]);
  });

  it("follows a stream that ended without a terminal status, without a warning", async () => {
    client.sendMessageStream.mockImplementation(() =>
      streamOf([taskEvent("t1", TaskState.TASK_STATE_WORKING)]),
    );
    client.resubscribeTask.mockImplementation(() =>
      streamOf([statusEvent(TaskState.TASK_STATE_COMPLETED)]),
    );

    expect((await mod.runDocumentTask(config, input())).state).toBe("completed");
    expect(warnings()).toEqual([]);
  });

  it.each([
    ["an Error", new Error("410 gone"), "410 gone"],
    ["a string", "gone", "gone"],
  ])("reads the task once when the re-attachment fails with %s", async (_what, err, text) => {
    brokenStream();
    client.resubscribeTask.mockImplementation(() => streamOf([], err));
    client.getTask.mockResolvedValue(
      task("t1", TaskState.TASK_STATE_COMPLETED, agentMessage(textPart("Finished")), [
        artifact("R", [urlPart("https://chat/doceditor?fileId=9", "R.xlsx", "application/xlsx")]),
      ]),
    );
    const signal = new AbortController().signal;

    const result = await mod.runDocumentTask(config, input({ signal }));

    expect(result).toEqual({
      state: "completed",
      text: "Finished",
      artifacts: [
        { name: "R.xlsx", mediaType: "application/xlsx", url: "https://chat/doceditor?fileId=9" },
      ],
    });
    expect(client.getTask).toHaveBeenCalledWith(
      { tenant: "", id: "t1", historyLength: undefined },
      { signal: expect.any(AbortSignal) },
    );
    expect(client.getTask.mock.calls[0]![1]!.signal).not.toBe(signal);
    expect(warnings()[1]).toBe(`documentAgent: task t1 resubscribe failed (1): ${text}`);
  });

  it("does not report again the step a task read repeats", async () => {
    brokenStream();
    const onStatus = vi.fn();
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    client.getTask
      .mockResolvedValueOnce(
        task("t1", TaskState.TASK_STATE_WORKING, agentMessage(textPart("Step 1"))),
      )
      .mockResolvedValueOnce(
        task("t1", TaskState.TASK_STATE_COMPLETED, agentMessage(textPart("Done"))),
      );

    const run = mod.runDocumentTask(config, input({ onStatus }));
    await vi.advanceTimersByTimeAsync(5_000);

    expect(await run).toEqual({ state: "completed", text: "Done", artifacts: [] });
    expect(onStatus.mock.calls).toEqual([["Step 1"]]);
    expect(client.resubscribeTask).toHaveBeenCalledTimes(2);
  });

  it("gives up after three attempts five seconds apart with DocumentAgentError", async () => {
    brokenStream();
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    client.resubscribeTask.mockImplementation(() => streamOf([], new Error("down")));
    client.getTask.mockRejectedValue(new Error("down too"));

    const run = mod.runDocumentTask(config, input());
    const settled = run.catch((err: unknown) => err);
    await vi.advanceTimersByTimeAsync(4_999);
    expect(client.resubscribeTask).toHaveBeenCalledTimes(1);
    await vi.advanceTimersByTimeAsync(1);
    expect(client.resubscribeTask).toHaveBeenCalledTimes(2);
    await vi.advanceTimersByTimeAsync(5_000);
    expect(client.resubscribeTask).toHaveBeenCalledTimes(3);
    await vi.advanceTimersByTimeAsync(5_000);

    const err = await settled;
    expect(err).toBeInstanceOf(mod.DocumentAgentError);
    expect(err).toMatchObject({ message: "task t1 could not be followed to its end" });
    expect(client.getTask).toHaveBeenCalledTimes(3);
    expect(client.cancelTask).not.toHaveBeenCalled();
    expect(warnings().slice(1)).toEqual([
      "documentAgent: task t1 resubscribe failed (1): down",
      "documentAgent: task t1 read failed (1): down too",
      "documentAgent: task t1 resubscribe failed (2): down",
      "documentAgent: task t1 read failed (2): down too",
      "documentAgent: task t1 resubscribe failed (3): down",
      "documentAgent: task t1 read failed (3): down too",
    ]);
  });

  it("logs a task read that fails with a string", async () => {
    brokenStream();
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    client.getTask
      .mockRejectedValueOnce("nope")
      .mockResolvedValueOnce(task("t1", TaskState.TASK_STATE_COMPLETED));

    const run = mod.runDocumentTask(config, input());
    await vi.advanceTimersByTimeAsync(5_000);

    expect((await run).state).toBe("completed");
    expect(warnings()).toContain("documentAgent: task t1 read failed (1): nope");
  });

  it("cancels the task and rethrows the abort reason when the caller aborts during the re-attachment", async () => {
    brokenStream();
    const controller = new AbortController();
    const reason = new Error("timed out");
    client.resubscribeTask.mockImplementation(async function* () {
      yield await Promise.resolve(messageEvent());
      controller.abort(reason);
      throw new Error("aborted");
    });

    await expect(mod.runDocumentTask(config, input({ signal: controller.signal }))).rejects.toBe(
      reason,
    );
    expect(client.getTask).not.toHaveBeenCalled();
    expect(client.cancelTask).toHaveBeenCalledTimes(1);
    expect(warnings()).toHaveLength(1);
  });

  it("cancels the task when the caller aborts during the task read", async () => {
    brokenStream();
    const controller = new AbortController();
    client.getTask.mockImplementation(() => {
      controller.abort("stop");
      return Promise.reject(new Error("aborted"));
    });

    const run = mod.runDocumentTask(config, input({ signal: controller.signal }));

    await expect(run).rejects.toThrow("document task aborted");
    expect(client.cancelTask).toHaveBeenCalledWith(
      { tenant: "", id: "t1", metadata: undefined },
      { signal: expect.any(AbortSignal) },
    );
    expect(warnings().filter((line) => line.includes("read failed"))).toEqual([]);
  });

  it("stops waiting between attempts when the caller aborts", async () => {
    brokenStream();
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    const controller = new AbortController();
    const reason = new Error("caller gone");
    client.getTask.mockResolvedValue(task("t1", TaskState.TASK_STATE_WORKING));

    const run = mod.runDocumentTask(config, input({ signal: controller.signal }));
    const settled = run.catch((err: unknown) => err);
    await vi.advanceTimersByTimeAsync(100);
    controller.abort(reason);

    expect(await settled).toBe(reason);
    expect(client.resubscribeTask).toHaveBeenCalledTimes(1);
    expect(client.cancelTask).toHaveBeenCalledTimes(1);
    expect(vi.getTimerCount()).toBe(0);
  });
});
