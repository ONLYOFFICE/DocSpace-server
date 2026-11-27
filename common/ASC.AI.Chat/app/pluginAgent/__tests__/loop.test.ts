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

import { compactHistory, custom } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";

import logger from "../../log.js";
import { MAX_NUDGES, ModelTurnError, NO_OUTPUT, NUDGE, runLoop } from "../loop.js";
import type { LoopOptions, ToolOutcome } from "../loop.js";
import { AGENT_TOOLS } from "../agentTools.js";
import { PROFILE, PROVIDER_ERROR, resultsIn, scriptModel } from "./model.js";

vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@onlyoffice/ai-chat/core")>()),
  custom: vi.fn(),
  compactHistory: vi.fn(),
}));

const called: [string, Record<string, unknown>][] = [];

const options = (over: Partial<LoopOptions<string>> = {}): LoopOptions<string> => ({
  profile: PROFILE,
  systemPrompt: "You build plugins.",
  task: "Build a plugin",
  tools: AGENT_TOOLS,
  maxToolCalls: 10,
  compaction: { enabled: false },
  signal: new AbortController().signal,
  callTool: (name, args): Promise<ToolOutcome<string>> => {
    called.push([name, args]);
    return Promise.resolve(
      name === "deliver_plugin" ? { text: "", done: "delivered" } : { text: `ran ${name}` },
    );
  },
  ...over,
});

beforeEach(() => {
  called.length = 0;
  vi.mocked(compactHistory).mockReset();
  vi.mocked(custom).mockReset();
});

describe("runLoop", () => {
  it("runs the tools the model calls and ends with the tool that finishes the task", async () => {
    const model = scriptModel([
      [{ name: "bash", args: { command: "ls" } }],
      [
        { name: "write_file", args: { path: "/a" } },
        { name: "bash", args: { command: "npm run build" } },
      ],
      [{ name: "deliver_plugin", args: { project_dir: "/workspace/p" } }],
    ]);

    const end = await runLoop(options());

    expect(end).toEqual({ kind: "done", value: "delivered", toolCalls: 4 });
    expect(called).toEqual([
      ["bash", { command: "ls" }],
      ["write_file", { path: "/a" }],
      ["bash", { command: "npm run build" }],
      ["deliver_plugin", { project_dir: "/workspace/p" }],
    ]);
    // The first call carries the task, each later one the results of the calls before it.
    expect(model.histories[0]).toEqual([{ role: "user", content: "Build a plugin" }]);
    expect(resultsIn(model.histories[1]!)).toEqual(["ran bash"]);
    expect(resultsIn(model.histories[2]!)).toEqual(["ran bash", "ran write_file", "ran bash"]);
    expect(vi.mocked(custom).mock.calls[0]?.[0]).toMatchObject({
      profile: PROFILE,
      systemPrompt: "You build plugins.",
      tools: AGENT_TOOLS,
    });
  });

  it("does not run the calls that follow the one that finished the task", async () => {
    scriptModel([[{ name: "deliver_plugin" }, { name: "bash", args: { command: "rm -rf /" } }]]);
    expect(await runLoop(options())).toEqual({ kind: "done", value: "delivered", toolCalls: 1 });
    expect(called.map(([name]) => name)).toEqual(["deliver_plugin"]);
  });

  it("tells a model that stops to ask to go on, and gives up after the second time", async () => {
    const model = scriptModel(["Which colour?", "And the icon?", "I cannot go on."]);

    const end = await runLoop(options());

    expect(MAX_NUDGES).toBe(2);
    expect(end).toEqual({ kind: "stopped", text: "I cannot go on.", toolCalls: 0 });
    expect(model.histories).toHaveLength(3);
    expect(model.histories[2]?.filter((m) => m.role === "user").map((m) => m.content)).toEqual([
      "Build a plugin",
      NUDGE,
      NUDGE,
    ]);
  });

  it("goes on after a nudge when the model calls a tool again", async () => {
    scriptModel(["Which colour?", [{ name: "deliver_plugin" }]]);
    expect(await runLoop(options())).toEqual({ kind: "done", value: "delivered", toolCalls: 1 });
  });

  it("ends when the budget of tool calls is spent, running exactly that many", async () => {
    scriptModel([
      [{ name: "bash" }, { name: "bash" }],
      [{ name: "bash" }, { name: "read_file" }, { name: "deliver_plugin" }],
    ]);

    const end = await runLoop(options({ maxToolCalls: 3 }));

    expect(end).toEqual({ kind: "budget", toolCalls: 3 });
    expect(called.map(([name]) => name)).toEqual(["bash", "bash", "bash"]);
  });

  it("counts the turns without a tool in a row, not over the whole task", async () => {
    scriptModel([
      "I will scaffold the project.",
      [{ name: "bash" }],
      "Now the component.",
      [{ name: "write_file" }],
      "Now the build.",
      "Almost there.",
      [{ name: "deliver_plugin" }],
    ]);
    expect(await runLoop(options())).toEqual({ kind: "done", value: "delivered", toolCalls: 3 });
  });

  it("rejects when the provider fails a turn, without reading it as an answer", async () => {
    const model = scriptModel([[{ name: "bash" }], PROVIDER_ERROR, "never asked"]);

    await expect(runLoop(options())).rejects.toBeInstanceOf(ModelTurnError);

    // No nudge, no further turn: the failed request is the end.
    expect(model.histories).toHaveLength(2);
  });

  it("does not put what the provider said into the failure", async () => {
    scriptModel([PROVIDER_ERROR]);
    await expect(runLoop(options())).rejects.toThrow("the model's provider failed the turn");
  });

  it("hands the model a text for a tool that answered nothing", async () => {
    const model = scriptModel([[{ name: "read_file" }], [{ name: "deliver_plugin" }]]);

    await runLoop(
      options({
        callTool: (name) =>
          Promise.resolve(
            name === "deliver_plugin" ? { text: "", done: "delivered" } : { text: "" },
          ),
      }),
    );

    expect(NO_OUTPUT).toBe("(no output)");
    expect(resultsIn(model.histories[1]!)).toEqual(["(no output)"]);
  });

  it("lets the last call of the budget finish the task", async () => {
    scriptModel([[{ name: "bash" }], [{ name: "deliver_plugin" }]]);
    expect(await runLoop(options({ maxToolCalls: 2 }))).toEqual({
      kind: "done",
      value: "delivered",
      toolCalls: 2,
    });
  });

  it("ends without a call under a budget of zero", async () => {
    scriptModel([[{ name: "bash" }]]);
    expect(await runLoop(options({ maxToolCalls: 0 }))).toEqual({ kind: "budget", toolCalls: 0 });
    expect(called).toEqual([]);
  });

  it("rejects with the signal's reason before asking the model", async () => {
    scriptModel([]);
    const controller = new AbortController();
    const reason = new Error("reply stopped");
    controller.abort(reason);

    await expect(runLoop(options({ signal: controller.signal }))).rejects.toBe(reason);

    expect(custom).not.toHaveBeenCalled();
  });

  it("rejects with the signal's reason when it fires while the model answers", async () => {
    const controller = new AbortController();
    const reason = new Error("time limit");
    scriptModel([[{ name: "bash" }]]);
    const ask = vi.mocked(custom).getMockImplementation()!;
    vi.mocked(custom).mockImplementation((args) => {
      controller.abort(reason);
      return ask(args);
    });

    await expect(runLoop(options({ signal: controller.signal }))).rejects.toBe(reason);

    expect(called).toEqual([]);
  });

  it("passes on what a tool throws", async () => {
    scriptModel([[{ name: "bash" }]]);
    const lost = new Error("sandbox lost");
    await expect(runLoop(options({ callTool: () => Promise.reject(lost) }))).rejects.toBe(lost);
  });
});

describe("compaction between the rounds", () => {
  const TWO_ROUNDS = [[{ name: "bash" }], [{ name: "deliver_plugin" }]];

  it("is not asked for when it is switched off", async () => {
    scriptModel(TWO_ROUNDS);
    await runLoop(options());
    expect(compactHistory).not.toHaveBeenCalled();
  });

  it("sends on the folded history when the library folds it", async () => {
    const model = scriptModel(TWO_ROUNDS);
    const summary = { role: "user" as const, content: "summary of the work so far" };
    vi.mocked(compactHistory).mockResolvedValue({
      summary,
      messages: [summary],
      estimatedTokens: 1,
    });

    await runLoop(options({ compaction: { enabled: true } }));

    expect(model.histories[1]).toEqual([summary]);
    expect(vi.mocked(compactHistory).mock.calls[0]?.[1]).toEqual({
      contextWindow: 128_000,
      threshold: 0.8,
      keepRecent: 4,
    });
  });

  it("measures against the profile's window and the installation's knobs", async () => {
    scriptModel(TWO_ROUNDS);
    vi.mocked(compactHistory).mockResolvedValue(null);

    await runLoop(
      options({
        profile: { ...PROFILE, contextWindow: 200_000 },
        compaction: { enabled: true, threshold: 0.5, keepRecent: 2, defaultContextWindow: 64_000 },
      }),
    );
    scriptModel(TWO_ROUNDS);
    await runLoop(options({ compaction: { enabled: true, defaultContextWindow: 64_000 } }));

    expect(vi.mocked(compactHistory).mock.calls.map((call) => call[1])).toEqual([
      { contextWindow: 200_000, threshold: 0.5, keepRecent: 2 },
      { contextWindow: 64_000, threshold: 0.8, keepRecent: 4 },
    ]);
  });

  it("keeps the history as it is when the library folds nothing", async () => {
    const model = scriptModel(TWO_ROUNDS);
    vi.mocked(compactHistory).mockResolvedValue(null);

    await runLoop(options({ compaction: { enabled: true } }));

    expect(resultsIn(model.histories[1]!)).toEqual(["ran bash"]);
  });

  it("goes on with the whole history when the summary fails", async () => {
    const model = scriptModel(TWO_ROUNDS);
    vi.mocked(compactHistory).mockRejectedValue(new Error("provider down"));

    const end = await runLoop(options({ compaction: { enabled: true } }));

    expect(end.kind).toBe("done");
    expect(resultsIn(model.histories[1]!)).toEqual(["ran bash"]);
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      "pluginAgent: the history was not compacted: Error: provider down",
    );
  });

  it("ends with the signal when it fired during the summary", async () => {
    scriptModel(TWO_ROUNDS);
    const controller = new AbortController();
    const failure = new Error("aborted");
    vi.mocked(compactHistory).mockImplementation(() => {
      controller.abort(new Error("reply stopped"));
      return Promise.reject(failure);
    });

    await expect(
      runLoop(options({ compaction: { enabled: true }, signal: controller.signal })),
    ).rejects.toBe(failure);
  });
});
