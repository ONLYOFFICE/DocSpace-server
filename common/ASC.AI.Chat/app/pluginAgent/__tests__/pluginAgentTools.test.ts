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

import { ActionType, custom } from "@onlyoffice/ai-chat/core";
import type { Progress } from "@onlyoffice/ai-chat/core";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { inRequestContext } from "../../__tests__/helpers/http.js";
import { afterUpstreamTimeout } from "../../__tests__/helpers/time.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  stalled,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import {
  setRoundProfileId,
  setRoundSignal,
  setRoundThreadId,
  shouldForwardHeadersToProvider,
} from "../../requestContext.js";
import { invalidateChatContext, readChatContext } from "../../storage/chatContextSnapshot.js";
import type { ChatContextSnapshot } from "../../storage/chatContextSnapshot.js";
import { DocspaceApiHttpError, getFileInfo } from "../../storage/docspaceFilesApi.js";
import { storage } from "../../storage/index.js";
import { FileTooLargeError, readFileBytes } from "../../storage/roomSkills.js";
import { UpstreamShapeError } from "../../storage/upstreamShape.js";
import { resolveUploadTarget } from "../../tools/documentAgent.js";
import { MAX_FILE_BYTES, saveResult } from "../../tools/documentAgentFiles.js";
import { MAX_TASK_CHARS, pluginAgentTools } from "../pluginAgentTools.js";
import { loadPluginSkill, pluginSkillPresent } from "../skill.js";
import { endTask, runningTaskCount, startTask } from "../tasks.js";
import type { RunningTask } from "../tasks.js";
import { fakeGateway, refusedWith } from "../../__tests__/helpers/sandboxGateway.js";
import type { FakeGateway } from "../../__tests__/helpers/sandboxGateway.js";
import { PROFILE, PROVIDER_ERROR, scriptModel } from "./model.js";
import type { ScriptedTurn } from "./model.js";

vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@onlyoffice/ai-chat/core")>()),
  custom: vi.fn(),
}));
// What the adapter reads the workspace and the portal's settings through; the gateway and the
// People API (`people/@self`, `settings/webplugins`) are stubbed at the network instead.
vi.mock("../skill.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../skill.js")>()),
  loadPluginSkill: vi.fn(),
  pluginSkillPresent: vi.fn(),
}));
vi.mock("../../tools/documentAgent.js", () => ({
  resolveUploadTarget: vi.fn(),
}));
vi.mock("../../tools/documentAgentFiles.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../tools/documentAgentFiles.js")>()),
  saveResult: vi.fn(),
}));
vi.mock("../../storage/chatContextSnapshot.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../storage/chatContextSnapshot.js")>()),
  invalidateChatContext: vi.fn(),
  // No copy of the round by default, as outside a primed round.
  readChatContext: vi.fn(),
}));
vi.mock("../../storage/roomSkills.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../storage/roomSkills.js")>()),
  readFileBytes: vi.fn(),
}));
vi.mock("../../storage/docspaceFilesApi.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../storage/docspaceFilesApi.js")>()),
  getFileInfo: vi.fn(),
}));

const upstream = useUpstream();

const DIR = "/workspace/my-plugin";
const ZIP = Buffer.from("PK\u0003\u0004 the rest of an archive", "latin1");
const SKILL = { dir: "/no-skill", instructions: "# the skill", sandboxFiles: [] };
const ARCHIVE_PREFIX = "rm -f /workspace/.sources.zip";

let gateway: FakeGateway;
let self: () => Response | Promise<Response>;

const MAY_BE_INSTALLED =
  "Whether the plugin was installed is not known. Check the portal's plugin settings before trying again.";

/** A sandbox whose project passes every check, with the role, the model and the folder in place. */
const ready = (): void => {
  gateway = fakeGateway(upstream);
  gateway.files.set(
    `${DIR}/package.json`,
    Buffer.from(
      JSON.stringify({ name: "my-plugin", version: "1.0.0", dependencies: { zod: "^3" } }),
    ),
  );
  gateway.files.set(`${DIR}/dist/plugin.zip`, Buffer.from("PK package"));
  gateway.run = (call) => {
    if (call.command.startsWith(ARCHIVE_PREFIX)) {
      gateway.files.set("/workspace/.sources.zip", Buffer.from("PK sources"));
    }
    return {};
  };
};

const DELIVER: ScriptedTurn = [{ name: "deliver_plugin", args: { project_dir: DIR } }];

const call = (
  tool: string,
  args: unknown,
  onprogress?: (progress: Progress) => void,
  signal?: AbortSignal,
): Promise<unknown> =>
  inRequestContext({ authorization: "Bearer user" }, () => {
    if (signal) {
      setRoundSignal(signal);
    }
    return pluginAgentTools.callTool(
      tool,
      args as Record<string, unknown>,
      "folder-7",
      onprogress ? { onprogress } : undefined,
    );
  });

const build = (args: unknown, onprogress?: (progress: Progress) => void, signal?: AbortSignal) =>
  call("plugin_agent", args, onprogress, signal);

beforeEach(() => {
  vi.stubEnv("AI__PLUGIN_AGENT__SANDBOX_URL", "http://gateway:9090");
  vi.stubEnv("AI__PLUGIN_AGENT__SANDBOX_TOKEN", "gw-secret");
  vi.stubEnv("AI__COMPACTION__ENABLED", "false");
  self = () => docspace({ isAdmin: true, isOwner: false, isRoomAdmin: false });
  // GET people/@self (PeopleController): the caller's role flags.
  upstream.use(http.get("*/api/2.0/people/@self", () => self()));
  vi.mocked(custom).mockReset();
  vi.mocked(loadPluginSkill).mockReset().mockResolvedValue(SKILL);
  vi.mocked(pluginSkillPresent).mockReset().mockResolvedValue(true);
  vi.mocked(invalidateChatContext).mockClear();
  vi.mocked(readChatContext).mockReset();
  // The portal's chat model, read the way an in-process call needs it.
  vi.spyOn(storage.assignments, "readByType").mockResolvedValue("p-chat");
  vi.spyOn(storage.profiles, "readById").mockResolvedValue(PROFILE);
  vi.spyOn(storage.toolPrefs, "updateAllowAlways").mockResolvedValue(undefined);
  vi.mocked(resolveUploadTarget).mockReset().mockResolvedValue("folder-7");
  vi.mocked(getFileInfo)
    .mockReset()
    .mockResolvedValue({ title: "my-plugin-src.zip", fileExst: ".zip" });
  vi.mocked(readFileBytes).mockReset().mockResolvedValue(ZIP);
  let saved = 100;
  vi.mocked(saveResult)
    .mockReset()
    .mockImplementation((_thread, folderId, name) =>
      Promise.resolve({ id: String(++saved), title: name, where: `into folder ${folderId}` }),
    );
  vi.spyOn(storage.preferences, "readToolPermissionMode").mockResolvedValue("auto");
  vi.spyOn(storage.toolPrefs, "readAllowAlways").mockResolvedValue([]);
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.useRealTimers();
  expect(runningTaskCount()).toBe(0);
});

describe("who is offered the plugin agent", () => {
  const offered = () =>
    inRequestContext({ authorization: "Bearer user" }, () => pluginAgentTools.getTools());

  it("tells the model to ask the user what a request leaves open before it calls plugin_agent", async () => {
    self = () => docspace({ isAdmin: true, isOwner: false, isRoomAdmin: false });
    const description = (await offered())["plugin-agent"]?.[0]?.description ?? "";
    expect(description).toContain(
      "Do not call this tool on a request that leaves the plugin unclear",
    );
    expect(description).toContain("ask_user_question");
    expect(description).toContain("need no questions");
    // The portal's plugins, never the editors': a model left alone asks which editor.
    expect(description).toContain("do not ask which editor it is for");
    // What a plugin can be and cannot do, so that the questions come with options and a
    // request no plugin can serve is answered before a task is spent on it.
    expect(description).toContain("a page of its own in the left sidebar");
    expect(description).toContain("a handler of a file extension");
    expect(description).toContain("It cannot: change anything inside an open document");
    expect(description).toContain("offer the closest thing above instead of calling this tool");
  });

  it.each([
    ["a DocSpace admin", { isAdmin: true, isOwner: false, isRoomAdmin: false }],
    ["the owner", { isAdmin: false, isOwner: true, isRoomAdmin: false }],
  ])("offers both tools to %s", async (_name, role) => {
    self = () => docspace(role);
    const tools = await offered();
    expect(Object.keys(tools)).toEqual(["plugin-agent", "plugin-install"]);
    expect(tools["plugin-agent"]?.[0]).toMatchObject({
      name: "plugin_agent",
      requireApproval: true,
      enabled: true,
      serverType: "plugin-agent",
    });
    expect(tools["plugin-install"]?.[0]).toMatchObject({
      name: "install_plugin",
      requireApproval: true,
      enabled: true,
      serverType: "plugin-install",
    });
  });

  it.each([
    ["a room admin", { isAdmin: false, isOwner: false, isRoomAdmin: true }],
    ["a user", { isAdmin: false, isOwner: false, isRoomAdmin: false }],
  ])("offers nothing to %s", async (_name, role) => {
    self = () => docspace(role);
    expect(await offered()).toEqual({});
  });

  it("offers nothing on an installation without a plugin agent, without asking for the role", async () => {
    vi.stubEnv("AI__PLUGIN_AGENT__SANDBOX_URL", "");
    expect(await offered()).toEqual({});
    expect(upstream.seen).toHaveLength(0);
  });

  it("offers nothing when the skill is not installed, without asking for the role", async () => {
    vi.mocked(pluginSkillPresent).mockResolvedValue(false);
    expect(await offered()).toEqual({});
    expect(upstream.seen).toHaveLength(0);
  });

  it("offers nothing, and does not fail the round, when the skill's files cannot be read", async () => {
    vi.mocked(pluginSkillPresent).mockRejectedValue(
      Object.assign(new Error("EACCES: permission denied"), { code: "EACCES" }),
    );
    expect(await offered()).toEqual({});
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      expect.stringContaining("the plugin skill was not read, the tools are off"),
    );
  });

  it("does not load the skill to decide what to offer", async () => {
    await offered();
    expect(loadPluginSkill).not.toHaveBeenCalled();
  });

  it("offers nothing for the round when the People API never answers", async () => {
    self = stalled;
    expect(await afterUpstreamTimeout(offered)).toEqual({ value: {} });
  });

  it.each([
    ["answers 500", () => docspaceError(500)],
    ["cannot be reached", () => HttpResponse.error()],
    ["answers something else", () => HttpResponse.json({ response: null })],
  ])("offers nothing for the round when the People API %s", async (_name, answer) => {
    self = answer;
    expect(await offered()).toEqual({});
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      expect.stringContaining("the caller's role was not read, the tools are off"),
    );
  });
});

describe("a call that starts nothing", () => {
  it("answers an unknown tool by its name", async () => {
    expect(await call("other_tool", {})).toBe('Unknown tool "other_tool"');
    expect(upstream.seen).toHaveLength(0);
  });

  it.each(["plugin_agent", "install_plugin"])(
    "refuses %s on an installation without a plugin agent",
    async (tool) => {
      vi.stubEnv("AI__PLUGIN_AGENT__SANDBOX_TOKEN", "");
      expect(await call(tool, { task: "x", fileId: 5 })).toBe(
        "The plugin agent is not configured on this server.",
      );
    },
  );

  it("refuses when the skill is not installed", async () => {
    vi.mocked(loadPluginSkill).mockResolvedValue(undefined);
    expect(await build({ task: "x" })).toBe("The plugin agent is not configured on this server.");
  });

  it.each(["plugin_agent", "install_plugin"])(
    "refuses %s for a caller who is not an admin, and starts nothing when the role cannot be read",
    async (tool) => {
      self = () => docspace({ isAdmin: false, isOwner: false, isRoomAdmin: true });
      expect(await call(tool, { task: "x", fileId: 5 })).toBe(
        "Only the owner and the DocSpace admins of the portal may build or install plugins.",
      );
      // An admin during an outage of the People API is not told that they are no admin.
      self = () => docspaceError(500);
      expect(await call(tool, { task: "x", fileId: 5 })).toBe(
        "The caller's role could not be read just now, so nothing was started. Try again.",
      );
      expect(getFileInfo).not.toHaveBeenCalled();
      expect(storage.profiles.readById).not.toHaveBeenCalled();
    },
  );

  it.each([
    ["missing", {}],
    ["empty", { task: "" }],
    ["white space", { task: " \n\t " }],
    ["not text", { task: 5 }],
    ["one character over the limit", { task: "a".repeat(MAX_TASK_CHARS + 1) }],
    ["arguments that are not an object", "build it"],
  ])("refuses a task that is %s", async (_name, args) => {
    fakeGateway(upstream);
    expect(await build(args)).toBe(
      "plugin_agent requires 'task', a text of 1 to 20000 characters.",
    );
    expect(upstream.seen.filter((r) => r.path.includes("SandboxService"))).toHaveLength(0);
  });

  it.each([[0], [-3], [1.5], ["12"], [null], [Number.MAX_SAFE_INTEGER + 1]])(
    "refuses the sources id %j",
    async (sourcesFileId) => {
      expect(await build({ task: "x", sourcesFileId })).toBe(
        "'sourcesFileId' is the id of a file: a whole number above zero.",
      );
      expect(getFileInfo).not.toHaveBeenCalled();
    },
  );

  it("refuses sources that do not exist or that the caller may not read", async () => {
    const refusal = "File 12 does not exist or cannot be read by the current user.";
    vi.mocked(getFileInfo).mockResolvedValue(undefined);
    expect(await build({ task: "x", sourcesFileId: 12 })).toBe(refusal);

    for (const status of [403, 404]) {
      vi.mocked(getFileInfo).mockRejectedValue(
        new DocspaceApiHttpError(status, "x", "http://internal/api"),
      );
      expect(await build({ task: "x", sourcesFileId: 12 })).toBe(refusal);
    }
  });

  it("refuses sources over the size a file may have", async () => {
    vi.mocked(readFileBytes).mockRejectedValue(new FileTooLargeError("12", MAX_FILE_BYTES));
    expect(await build({ task: "x", sourcesFileId: 12 })).toBe(
      `File 12 is over ${MAX_FILE_BYTES} bytes.`,
    );
    expect(vi.mocked(readFileBytes)).toHaveBeenCalledWith("12", 32 * 1024 * 1024);
  });

  it.each([
    ["has another extension", { title: "a.docx", fileExst: ".docx" }, ZIP],
    ["has no extension", { title: "a" }, ZIP],
    [
      "does not start as a zip archive",
      { title: "a.zip", fileExst: ".zip" },
      Buffer.from("<html>"),
    ],
    ["is empty", { title: "a.zip", fileExst: ".ZIP" }, Buffer.alloc(0)],
  ])("refuses a sources file that %s", async (_name, info, bytes) => {
    vi.mocked(getFileInfo).mockResolvedValue(info);
    vi.mocked(readFileBytes).mockResolvedValue(bytes);
    expect(await build({ task: "x", sourcesFileId: 12 })).toBe(
      "File 12 is not a zip archive: pass the '-src.zip' file an earlier task saved.",
    );
  });

  it("downloads no file that is not a zip archive by its name", async () => {
    vi.mocked(getFileInfo).mockResolvedValue({ title: "a.docx", fileExst: ".docx" });
    await build({ task: "x", sourcesFileId: 12 });
    expect(readFileBytes).not.toHaveBeenCalled();
  });

  it("answers the workspace's status when the sources cannot be read for another reason", async () => {
    vi.mocked(getFileInfo).mockRejectedValue(
      new DocspaceApiHttpError(500, "x", "http://internal/api"),
    );
    const answer = await build({ task: "x", sourcesFileId: 12 });
    expect(answer).toBe("The workspace could not be reached: it answered 500. No file was saved.");
    expect(answer).not.toContain("internal");
  });

  it("refuses when no model is configured, or no folder can take the files", async () => {
    vi.spyOn(storage.assignments, "readByType").mockResolvedValue(null);
    expect(await build({ task: "x" })).toBe(
      "No model is configured for this chat, the plugin agent cannot run.",
    );

    vi.spyOn(storage.assignments, "readByType").mockResolvedValue("p-chat");
    vi.mocked(resolveUploadTarget).mockResolvedValue(undefined);
    expect(await build({ task: "x" })).toBe(
      "There is no folder the resulting files could be saved into.",
    );
    expect(vi.mocked(resolveUploadTarget)).toHaveBeenCalledWith("folder-7");
    // Refused before the sources were asked for.
    expect(getFileInfo).not.toHaveBeenCalled();
  });

  it("answers an internal error for a failure it does not know, without its text", async () => {
    vi.spyOn(storage.assignments, "readByType").mockRejectedValue(
      new Error("connect ECONNREFUSED 10.0.0.5:5157"),
    );
    const answer = await build({ task: "x" });
    expect(answer).toBe(
      "The call could not be completed due to an internal error. No file was saved.",
    );
  });

  it("starts a task for a description of exactly 20000 characters, counted without the white space around it", async () => {
    ready();
    const model = scriptModel([DELIVER]);

    const answer = await build({ task: `  ${"a".repeat(MAX_TASK_CHARS)}\n` });

    expect(MAX_TASK_CHARS).toBe(20_000);
    expect(JSON.parse(answer as string)).toMatchObject({ name: "my-plugin" });
    expect(model.histories[0]?.[0]?.content).toHaveLength(20_000);
  });
});

describe("concurrent plugin tasks", () => {
  const others: RunningTask[] = [];
  afterEach(() => {
    for (const task of others.splice(0)) {
      endTask(task);
    }
  });

  it("refuses at once when the limit of 2 is reached and starts again once a place is free", async () => {
    ready();
    scriptModel([DELIVER]);
    others.push(startTask(2)!, startTask(2)!);

    expect(await build({ task: "x" })).toBe(
      "The plugin agent is busy with other tasks. Try again in a few minutes.",
    );
    expect(gateway.calls).toEqual([]);

    endTask(others.pop()!);
    expect(JSON.parse((await build({ task: "x" })) as string)).toMatchObject({ name: "my-plugin" });
    endTask(others.pop()!);
  });

  it("takes the limit the installation sets", async () => {
    vi.stubEnv("AI__PLUGIN_AGENT__MAX_CONCURRENT_TASKS", "1");
    others.push(startTask(2)!);
    expect(await build({ task: "x" })).toContain("busy with other tasks");
    endTask(others.pop()!);
  });
});

describe("a plugin task", () => {
  it("builds in a sandbox, saves the package and the sources, installs nothing and removes the sandbox", async () => {
    ready();
    const model = scriptModel([
      [{ name: "bash", args: { command: "node /workspace/.skill/scripts/generate-plugin.mjs" } }],
      DELIVER,
    ]);

    const answer = await build({ task: "  A button that greets  " });

    expect(JSON.parse(answer as string)).toEqual({
      name: "my-plugin",
      version: "1.0.0",
      package: { fileId: "101", title: "my-plugin.zip", bytes: 10 },
      sources: { fileId: "102", title: "my-plugin-src.zip" },
      dependencies: ["zod"],
      devDependencies: [],
      installed: false,
    });
    expect(
      vi
        .mocked(saveResult)
        .mock.calls.map(([thread, folder, name, bytes, type]) => [
          thread,
          folder,
          name,
          String(Buffer.from(bytes)),
          type,
        ]),
    ).toEqual([
      [undefined, "folder-7", "my-plugin.zip", "PK package", "application/zip"],
      [undefined, "folder-7", "my-plugin-src.zip", "PK sources", "application/zip"],
    ]);
    expect(model.histories[0]).toEqual([{ role: "user", content: "A button that greets" }]);
    const asked = vi.mocked(custom).mock.calls[0]?.[0];
    expect(asked?.profile).toBe(PROFILE);
    expect(asked?.systemPrompt.startsWith("# the skill\n\n# Where you work")).toBe(true);
    expect(asked?.systemPrompt).toContain("/workspace/.skill");
    expect(asked?.tools?.map((tool) => tool.name)).toContain("deliver_plugin");
    expect(gateway.calls[0]).toBe("Create");
    expect(gateway.calls.at(-1)).toBe("Delete");
    // Nothing of the portal goes into the sandbox, and nothing is installed.
    expect(upstream.seen.some((r) => r.path.includes("webplugins"))).toBe(false);
    expect(
      upstream.seen
        .filter((r) => r.path.includes("gateway.v1") || r.path.includes("/sandboxes/"))
        .every((r) => r.headers["authorization"] === "Bearer gw-secret"),
    ).toBe(true);
  });

  it("keeps the files among the artifacts of the round's conversation when it has one", async () => {
    ready();
    scriptModel([DELIVER]);
    await inRequestContext({ authorization: "Bearer user" }, () => {
      setRoundThreadId("thread-9");
      return pluginAgentTools.callTool("plugin_agent", { task: "x" }, "folder-7");
    });
    expect(vi.mocked(saveResult).mock.calls.map(([thread]) => thread)).toEqual([
      "thread-9",
      "thread-9",
    ]);
  });

  it("asks for the folder of a round that names none, which is My documents (the chat beside the plugins page)", async () => {
    ready();
    scriptModel([DELIVER]);
    await inRequestContext({ authorization: "Bearer user" }, () =>
      pluginAgentTools.callTool("plugin_agent", { task: "x" }, undefined),
    );
    // `resolveUploadTarget` answers My documents for no folder; tested with `document_agent`.
    expect(vi.mocked(resolveUploadTarget)).toHaveBeenLastCalledWith(undefined);
    expect(saveResult).toHaveBeenCalledTimes(2);
  });

  it("runs on the round's own model, read with the caller's session for the portal's own provider", async () => {
    ready();
    scriptModel([DELIVER]);
    const asked: [string, boolean][] = [];
    vi.spyOn(storage.profiles, "readById").mockImplementation((id) => {
      // Whether the session was asked for when the profile was read: only then does the
      // portal's own provider get the caller's headers and its internal address.
      asked.push([id, shouldForwardHeadersToProvider()]);
      return Promise.resolve(PROFILE);
    });
    const raw = vi.spyOn(storage.profiles, "readByIdRaw");

    await inRequestContext({ authorization: "Bearer user" }, () => {
      setRoundProfileId("p-round");
      return pluginAgentTools.callTool("plugin_agent", { task: "x" }, "folder-7");
    });

    expect(asked).toEqual([["p-round", true]]);
    expect(raw).not.toHaveBeenCalled();
    expect(storage.assignments.readByType).not.toHaveBeenCalled();
  });

  it("falls back on the portal's chat model, then on its default one", async () => {
    ready();
    scriptModel([DELIVER, DELIVER]);
    const byType = vi.spyOn(storage.assignments, "readByType");

    byType.mockImplementation((type) =>
      Promise.resolve(type === ActionType.Chat ? "p-chat" : null),
    );
    await build({ task: "x" });
    expect(storage.profiles.readById).toHaveBeenLastCalledWith("p-chat");

    byType.mockImplementation((type) =>
      Promise.resolve(type === ActionType.Default ? "p-default" : null),
    );
    await build({ task: "x" });
    expect(storage.profiles.readById).toHaveBeenLastCalledWith("p-default");
  });

  it("falls back on the portal's model when the round's own is gone", async () => {
    ready();
    scriptModel([DELIVER]);
    vi.spyOn(storage.profiles, "readById").mockImplementation((id) =>
      Promise.resolve(id === "p-gone" ? undefined : PROFILE),
    );
    await inRequestContext({ authorization: "Bearer user" }, () => {
      setRoundProfileId("p-gone");
      return pluginAgentTools.callTool("plugin_agent", { task: "x" }, "folder-7");
    });
    expect(storage.profiles.readById).toHaveBeenLastCalledWith("p-chat");
  });

  it("shows a line for each tool call, in order, without what the call carries", async () => {
    ready();
    scriptModel([
      [{ name: "write_file", args: { path: `${DIR}/src/a.ts`, content: "const secret = 1;" } }],
      [{ name: "bash", args: { command: "npm run build" } }],
      DELIVER,
    ]);
    gateway.run = (exec) => {
      if (exec.command.startsWith(ARCHIVE_PREFIX)) {
        gateway.files.set("/workspace/.sources.zip", Buffer.from("PK sources"));
      }
      return { stdout: "built in 2s" };
    };
    const steps: Progress[] = [];

    await build({ task: "x" }, (progress) => steps.push(progress));

    expect(steps).toEqual([
      { progress: 1, message: `Writing ${DIR}/src/a.ts` },
      { progress: 2, message: "Running: npm run build" },
      { progress: 3, message: "Checking and packaging the plugin" },
    ]);
    expect(JSON.stringify(steps)).not.toContain("secret");
    expect(JSON.stringify(steps)).not.toContain("built in");
  });

  it("sends a failed check back to the model and delivers once it passes", async () => {
    ready();
    let builds = 0;
    const passing = gateway.run;
    gateway.run = (exec) =>
      exec.command === "npm run build" && ++builds === 1
        ? { stderr: "TS2322", code: 1 }
        : passing(exec);
    const model = scriptModel([DELIVER, DELIVER]);

    const answer = await build({ task: "x" });

    expect(JSON.parse(answer as string)).toMatchObject({ name: "my-plugin" });
    expect(JSON.stringify(model.histories[1])).toContain("The build failed (npm run build).");
    expect(saveResult).toHaveBeenCalledTimes(2);
  });

  it.each([[undefined], ["/tmp/x"], ["/workspace"], ["/workspace/my plugin"]])(
    "sends back a hand-over that names the project as %j",
    async (projectDir) => {
      ready();
      const model = scriptModel([
        [{ name: "deliver_plugin", args: { project_dir: projectDir } }],
        DELIVER,
      ]);
      await build({ task: "x" });
      expect(JSON.stringify(model.histories[1])).toContain(
        "deliver_plugin requires 'project_dir', the plugin project's directory under /workspace: plain names without spaces.",
      );
    },
  );

  it("saves nothing and names the last failed check when the budget of tool calls is spent", async () => {
    vi.stubEnv("AI__PLUGIN_AGENT__MAX_TOOL_CALLS", "2");
    ready();
    gateway.run = (exec) =>
      exec.command === "npm run typecheck" ? { stderr: "TS2322", code: 2 } : {};
    scriptModel([DELIVER, DELIVER, DELIVER]);

    expect(await build({ task: "x" })).toBe(
      "The plugin was not finished: the budget of 2 tool calls was spent. The last failed check: type check. No file was saved.",
    );
    expect(saveResult).not.toHaveBeenCalled();
    expect(gateway.calls.at(-1)).toBe("Delete");
  });

  it("says so when the budget is spent before any check ran", async () => {
    vi.stubEnv("AI__PLUGIN_AGENT__MAX_TOOL_CALLS", "1");
    ready();
    scriptModel([
      [{ name: "bash", args: { command: "ls" } }],
      [{ name: "bash", args: { command: "ls" } }],
    ]);
    expect(await build({ task: "x" })).toBe(
      "The plugin was not finished: the budget of 1 tool calls was spent. No file was saved.",
    );
  });

  it("saves nothing when the model stops without handing a plugin over", async () => {
    ready();
    scriptModel(["Which colour?", "And the icon?", `I give up. ${"x".repeat(600)}`]);
    const answer = (await build({ task: "x" })) as string;
    expect(
      answer.startsWith(
        "The plugin was not finished: the agent stopped without handing it over. No file was saved. Its last words: I give up. xxx",
      ),
    ).toBe(true);
    expect(answer).toHaveLength(
      "The plugin was not finished: the agent stopped without handing it over. No file was saved. Its last words: "
        .length + 500,
    );
    expect(saveResult).not.toHaveBeenCalled();
    expect(gateway.calls.at(-1)).toBe("Delete");
  });

  it("stops at the time limit, removes the sandbox and saves nothing", async () => {
    vi.stubEnv("AI__PLUGIN_AGENT__TASK_TIMEOUT_SECONDS", "90");
    ready();
    scriptModel([[{ name: "bash", args: { command: "sleep 1000" } }]]);
    gateway.run = () => new Promise<Response>(() => undefined);
    // Only the timer behind `AbortSignal.timeout` is the real one here: a moment in place of ninety seconds.
    vi.spyOn(AbortSignal, "timeout").mockImplementation(() => {
      const controller = new AbortController();
      setTimeout(() => controller.abort(new DOMException("timed out", "TimeoutError")), 20);
      return controller.signal;
    });

    expect(await build({ task: "x" })).toBe(
      "The plugin task was stopped at its time limit of 90 seconds. No file was saved.",
    );
    expect(vi.mocked(AbortSignal.timeout)).toHaveBeenCalledWith(90_000);
    expect(saveResult).not.toHaveBeenCalled();
    expect(gateway.calls.at(-1)).toBe("Delete");
  });

  it("stops with the reply, removes the sandbox and saves nothing", async () => {
    ready();
    scriptModel([[{ name: "bash", args: { command: "sleep 1000" } }]]);
    const reply = new AbortController();
    gateway.run = () => {
      reply.abort();
      return new Promise<Response>(() => undefined);
    };

    expect(await build({ task: "x" }, undefined, reply.signal)).toBe(
      "The plugin task was stopped with the reply. No file was saved.",
    );
    expect(saveResult).not.toHaveBeenCalled();
    expect(gateway.calls.at(-1)).toBe("Delete");
  });

  it("runs no task when the sandbox service has no free sandbox", async () => {
    ready();
    gateway.fail.set("Create", refusedWith("resource_exhausted", 429));
    scriptModel([DELIVER]);

    expect(await build({ task: "x" })).toBe(
      "The plugin agent is busy: no sandbox is free. Try again in a few minutes.",
    );
    expect(custom).not.toHaveBeenCalled();
    expect(gateway.calls).toEqual(["Create"]);
  });

  it("runs no task when the sandbox service cannot be reached", async () => {
    ready();
    gateway.fail.set("Create", () => HttpResponse.error());
    scriptModel([DELIVER]);

    expect(await build({ task: "x" })).toBe(
      "The plugin agent is unavailable: its sandbox cannot be reached. No file was saved.",
    );
    expect(custom).not.toHaveBeenCalled();
    // Asked once more for the same session, to delete what the first call may have made.
    expect(gateway.calls).toEqual(["Create", "Create"]);
  });

  it("does not call a slow command a missing sandbox", async () => {
    ready();
    scriptModel([[{ name: "bash", args: { command: "npm install" } }]]);
    gateway.run = () => HttpResponse.json({ code: "deadline_exceeded" }, { status: 504 });

    expect(await build({ task: "x" })).toBe(
      "The plugin agent is unavailable: its sandbox cannot be reached. No file was saved.",
    );
  });

  it("ends the task and removes the sandbox when the sandbox is lost on the way", async () => {
    ready();
    scriptModel([[{ name: "bash", args: { command: "ls" } }]]);
    gateway.run = () => HttpResponse.json({ code: "failed_precondition" }, { status: 400 });
    expect(await build({ task: "x" })).toBe(
      "The plugin agent is unavailable: its sandbox cannot be reached. No file was saved.",
    );
    expect(gateway.calls.at(-1)).toBe("Delete");
  });

  it("says that no file was saved when the workspace refuses the package", async () => {
    ready();
    scriptModel([DELIVER]);
    vi.mocked(saveResult).mockRejectedValue(
      new DocspaceApiHttpError(403, "x", "http://internal/api"),
    );

    const answer = await build({ task: "x" });

    expect(answer).toBe(
      "The plugin was built, but the workspace refused to save it: it answered 403. No file was saved.",
    );
    expect(answer).not.toContain("internal");
    expect(saveResult).toHaveBeenCalledTimes(1);
    expect(gateway.calls.at(-1)).toBe("Delete");
  });

  it.each([
    ["answers 500", new DocspaceApiHttpError(500, "x", "http://internal/api")],
    ["answers a body that cannot be read", new UpstreamShapeError("POST files/{folderId}/insert")],
    ["does not answer in time", new Error("upstream timeout after 300000ms")],
  ])("says that the package may have been saved when the workspace %s", async (_name, failure) => {
    ready();
    scriptModel([DELIVER]);
    vi.mocked(saveResult).mockRejectedValue(failure);

    const answer = await build({ task: "x" });

    expect(answer).toBe(
      "The plugin was built, but the workspace's answer to saving my-plugin.zip could not be read: the file may or may not have been saved. Look for it before building again.",
    );
    expect(saveResult).toHaveBeenCalledTimes(1);
  });

  it.each([
    [
      "refuses them",
      new DocspaceApiHttpError(403, "x", "http://internal/api"),
      "The package was saved, the archive of its sources was not: a follow-up task cannot continue from this one.",
    ],
    [
      "answers what cannot be read",
      new UpstreamShapeError("POST files/{folderId}/insert"),
      "The package was saved; whether the archive of its sources was saved is not known. A follow-up task needs that archive's file id.",
    ],
  ])(
    "names the saved package when the workspace %s for the sources",
    async (_name, failure, warning) => {
      ready();
      scriptModel([DELIVER]);
      vi.mocked(saveResult)
        .mockResolvedValueOnce({ id: "201", title: "my-plugin.zip", where: "x" })
        .mockRejectedValueOnce(failure);

      expect(JSON.parse((await build({ task: "x" })) as string)).toMatchObject({
        package: { fileId: "201", title: "my-plugin.zip" },
        sources: null,
        warning,
      });
    },
  );

  it("says that the provider failed when a request to the model fails, and saves nothing", async () => {
    ready();
    scriptModel([[{ name: "bash", args: { command: "ls" } }], PROVIDER_ERROR]);

    expect(await build({ task: "x" })).toBe(
      "The plugin was not finished: the model's provider failed a request. No file was saved.",
    );
    expect(custom).toHaveBeenCalledTimes(2);
    expect(saveResult).not.toHaveBeenCalled();
    expect(gateway.calls.at(-1)).toBe("Delete");
  });
});

describe("continuing from a sources archive", () => {
  it("unpacks the archive in the sandbox and tells the model where the project is", async () => {
    ready();
    const model = scriptModel([DELIVER]);

    const answer = await build({ task: "Add a button", sourcesFileId: 12 });

    expect(JSON.parse(answer as string)).toMatchObject({
      package: { fileId: "101" },
      sources: { fileId: "102" },
    });
    expect(vi.mocked(getFileInfo)).toHaveBeenCalledWith("12");
    expect(String(gateway.files.get("/workspace/.input.zip"))).toBe(String(ZIP));
    expect(gateway.execs[0]).toMatchObject({
      command:
        "mkdir -p /workspace/plugin && unzip -q -o /workspace/.input.zip -d /workspace/plugin && rm -f /workspace/.input.zip",
      cwd: "/workspace",
      timeout: "120s",
    });
    expect(model.histories[0]?.[0]?.content).toBe(
      "Add a button\n\nThe plugin's project from an earlier task is unpacked in /workspace/plugin, without its node_modules and without a build: run npm install there first, change that project, and hand it over.",
    );
  });

  it("runs no task when the archive does not unpack", async () => {
    ready();
    gateway.run = () => ({ stderr: "End-of-central-directory signature not found", code: 9 });
    scriptModel([DELIVER]);

    expect(await build({ task: "x", sourcesFileId: 12 })).toBe(
      "The sources archive could not be unpacked: it is damaged or not a plugin's sources. No task was run.",
    );
    expect(custom).not.toHaveBeenCalled();
    expect(gateway.calls.at(-1)).toBe("Delete");
  });
});

describe("install_plugin", () => {
  const WEBPLUGINS = "*/api/2.0/settings/webplugins";
  const install = (args: unknown) => call("install_plugin", args);
  // POST settings/webplugins (WebPluginsController.AddWebPluginFromFile) answers WebPluginDto.
  const installed = () =>
    docspace({ name: "my-plugin", version: "1.0.0", enabled: true, pluginName: "MyPlugin" });

  it("uploads the package under the caller's session and answers what the portal installed", async () => {
    let form: FormData | undefined;
    upstream.use(
      http.post(WEBPLUGINS, async ({ request }) => {
        form = await request.formData();
        return installed();
      }),
    );

    const answer = await install({ fileId: 101 });

    expect(JSON.parse(answer as string)).toEqual({
      installed: true,
      name: "my-plugin",
      version: "1.0.0",
      enabled: true,
    });
    expect(vi.mocked(getFileInfo)).toHaveBeenCalledWith("101");
    const file = form?.get("file") as File;
    expect(file.name).toBe("plugin-101.zip");
    expect(Buffer.from(await file.arrayBuffer()).toString("latin1")).toBe(ZIP.toString("latin1"));
    expect(upstream.seen.at(-1)?.headers["authorization"]).toBe("Bearer user");
  });

  it("says that nothing was installed for a package the portal refuses with 400", async () => {
    upstream.use(
      http.post(WEBPLUGINS, () => docspaceError(400, "Wrong plugin archive. Plugin already exist")),
    );
    expect(await install({ fileId: 101 })).toBe(
      'Nothing was installed: the portal answered 400 "Wrong plugin archive. Plugin already exist".',
    );
  });

  // The portal answers 403 before the installation (no right, uploads switched off) and after
  // it, when the plugin's domains do not fit into the CSP header: the status does not say which.
  it("does not say that nothing was installed for a 403", async () => {
    upstream.use(http.post(WEBPLUGINS, () => docspaceError(403, "Forbidden action")));
    expect(await install({ fileId: 101 })).toBe(
      'The installation was refused: the portal answered 403 "Forbidden action". ' +
        MAY_BE_INSTALLED,
    );
  });

  it.each([
    ["is not JSON", () => new HttpResponse("<html>", { status: 400 })],
    ["has no error", () => HttpResponse.json({ status: 1 }, { status: 400 })],
    ["has an error without a message", () => HttpResponse.json({ error: {} }, { status: 400 })],
    ["is not an object", () => HttpResponse.json([1], { status: 400 })],
  ])("answers the status alone when the refusal's body %s", async (_name, answer) => {
    upstream.use(http.post(WEBPLUGINS, answer));
    expect(await install({ fileId: 101 })).toBe("Nothing was installed: the portal answered 400.");
  });

  it.each([[undefined], [0], [-1], [2.5], ["101"], [null]])(
    "refuses the file id %j without reading anything",
    async (fileId) => {
      expect(await install({ fileId })).toBe(
        "install_plugin requires 'fileId', the id of the plugin package: a whole number above zero.",
      );
      expect(getFileInfo).not.toHaveBeenCalled();
    },
  );

  it("installs nothing for a file the caller cannot read, or one that is no archive", async () => {
    vi.mocked(getFileInfo).mockResolvedValue(undefined);
    expect(await install({ fileId: 101 })).toBe(
      "Nothing was installed. File 101 does not exist or cannot be read by the current user.",
    );
    vi.mocked(getFileInfo).mockResolvedValue({ title: "a.docx", fileExst: ".docx" });
    expect(await install({ fileId: 101 })).toBe(
      "Nothing was installed. File 101 is not a zip archive: pass the plugin package a plugin task saved.",
    );
    expect(upstream.seen.some((r) => r.path.includes("webplugins"))).toBe(false);
  });

  it("installs nothing under the permission mode that runs every tool unasked", async () => {
    vi.spyOn(storage.preferences, "readToolPermissionMode").mockResolvedValue("allow");
    const answer = await install({ fileId: 101 });
    expect(answer).toBe(
      'Nothing was installed: installing a plugin needs the user\'s approval of the call. Ask the user to switch the tool permissions of the chat from "Allow" to "Auto" or "Ask" and to ask again.',
    );
    expect(getFileInfo).not.toHaveBeenCalled();
    expect(storage.toolPrefs.updateAllowAlways).not.toHaveBeenCalled();
    expect(upstream.seen.some((r) => r.path.includes("webplugins"))).toBe(false);
  });

  it("installs nothing on a standing approval, takes it back, and installs when asked again", async () => {
    upstream.use(http.post(WEBPLUGINS, installed));
    // What the engine has written when the user ticked "always allow" on the dialog of this
    // very call, and what an earlier tick left: the adapter cannot tell them apart.
    const list = vi
      .spyOn(storage.toolPrefs, "readAllowAlways")
      .mockResolvedValue([
        "docspace_x",
        "plugin-install_install_plugin",
        "plugin-agent_plugin_agent",
      ]);

    expect(await install({ fileId: 101 })).toBe(
      "Nothing was installed: an installation is approved one call at a time, so the standing approval of install_plugin was removed. Call install_plugin again: the user will be asked.",
    );
    expect(storage.toolPrefs.updateAllowAlways).toHaveBeenCalledWith(
      ["docspace_x", "plugin-agent_plugin_agent"],
      "folder-7",
    );
    // The list is read anew for this: what is written back is the whole list, and the one the
    // round started with may be many minutes old.
    expect(invalidateChatContext).toHaveBeenCalledWith("toolPrefs");
    expect(vi.mocked(invalidateChatContext).mock.invocationCallOrder[0]).toBeLessThan(
      list.mock.invocationCallOrder[0]!,
    );
    expect(getFileInfo).not.toHaveBeenCalled();
    expect(upstream.seen.some((r) => r.path.includes("webplugins"))).toBe(false);

    // The list without the entry: the engine asks, the user approves, the call installs.
    list.mockResolvedValue(["docspace_x", "plugin-agent_plugin_agent"]);
    expect(JSON.parse((await install({ fileId: 101 })) as string)).toMatchObject({
      installed: true,
    });
  });

  it("installs nothing on a standing approval only the round's copy of the list still holds", async () => {
    upstream.use(http.post(WEBPLUGINS, installed));
    // The round holds a copy of the list: the engine ran the call unasked on it.
    vi.mocked(readChatContext).mockReturnValue({} as ChatContextSnapshot);
    const list = vi
      .spyOn(storage.toolPrefs, "readAllowAlways")
      .mockResolvedValueOnce(["docspace_x", "plugin-install_install_plugin"])
      // The read made now: the entry was removed after the round began.
      .mockResolvedValueOnce(["docspace_x"]);

    expect(await install({ fileId: 101 })).toBe(
      "Nothing was installed: the call was started on a standing approval of install_plugin that has been removed since, and an installation is approved one call at a time. Call install_plugin again: the user will be asked.",
    );
    expect(list).toHaveBeenCalledTimes(2);
    // The round's copy is read before it is dropped, the list of now after.
    const dropped = vi.mocked(invalidateChatContext).mock.invocationCallOrder[0]!;
    expect(list.mock.invocationCallOrder[0]).toBeLessThan(dropped);
    expect(dropped).toBeLessThan(list.mock.invocationCallOrder[1]!);
    expect(storage.toolPrefs.updateAllowAlways).not.toHaveBeenCalled();
    expect(getFileInfo).not.toHaveBeenCalled();
    expect(upstream.seen.some((r) => r.path.includes("webplugins"))).toBe(false);
  });

  it("writes back the list of now, not the round's copy, when both hold the standing approval", async () => {
    vi.mocked(readChatContext).mockReturnValue({} as ChatContextSnapshot);
    vi.spyOn(storage.toolPrefs, "readAllowAlways")
      .mockResolvedValueOnce(["old_entry", "plugin-install_install_plugin"])
      .mockResolvedValueOnce(["plugin-install_install_plugin", "new_entry"]);

    expect(await install({ fileId: 101 })).toMatch(/^Nothing was installed: /);
    expect(storage.toolPrefs.updateAllowAlways).toHaveBeenCalledWith(["new_entry"], "folder-7");
    expect(upstream.seen.some((r) => r.path.includes("webplugins"))).toBe(false);
  });

  it("installs nothing when the standing approval cannot be taken back", async () => {
    vi.spyOn(storage.toolPrefs, "readAllowAlways").mockResolvedValue([
      "plugin-install_install_plugin",
    ]);
    vi.spyOn(storage.toolPrefs, "updateAllowAlways").mockRejectedValue(new Error("down"));
    expect(await install({ fileId: 101 })).toBe(
      "The call could not be completed due to an internal error. Nothing was installed.",
    );
    expect(upstream.seen.some((r) => r.path.includes("webplugins"))).toBe(false);
  });

  it("installs under the mode that asks for every call, whatever the list holds, and when no mode is stored", async () => {
    upstream.use(http.post(WEBPLUGINS, installed));
    vi.spyOn(storage.toolPrefs, "readAllowAlways").mockResolvedValue([
      "plugin-install_install_plugin",
    ]);
    vi.spyOn(storage.preferences, "readToolPermissionMode").mockResolvedValue("ask");
    expect(JSON.parse((await install({ fileId: 101 })) as string)).toMatchObject({
      installed: true,
    });

    vi.spyOn(storage.toolPrefs, "readAllowAlways").mockResolvedValue(["plugin-agent_plugin_agent"]);
    vi.spyOn(storage.preferences, "readToolPermissionMode").mockResolvedValue(null);
    expect(JSON.parse((await install({ fileId: 101 })) as string)).toMatchObject({
      installed: true,
    });
  });

  it.each([
    ["answers 500", () => docspaceError(500, "secret upstream text")],
    [
      "answers 502 from the proxy in front of it",
      () => new HttpResponse("<html>", { status: 502 }),
    ],
    ["answers a body it cannot read", () => docspace({ name: "my-plugin" })],
    ["answers no JSON", () => new HttpResponse("ok")],
    ["cannot be reached", () => HttpResponse.error()],
  ])("says that the plugin may be installed when the portal %s", async (_name, answer) => {
    upstream.use(http.post(WEBPLUGINS, answer));
    const result = await install({ fileId: 101 });
    expect(result).toBe(
      "The portal's answer to the installation could not be read. " + MAY_BE_INSTALLED,
    );
    expect(result).not.toContain("secret");
  });

  it("says that the plugin may be installed when the portal never answers the upload", async () => {
    upstream.use(http.post(WEBPLUGINS, stalled));
    const outcome = await afterUpstreamTimeout(() => install({ fileId: 101 }));
    expect(outcome).toEqual({
      value: "The portal's answer to the installation could not be read. " + MAY_BE_INSTALLED,
    });
  });

  it("says that nothing was installed when the file could not be read from the workspace", async () => {
    vi.mocked(getFileInfo).mockRejectedValue(new DocspaceApiHttpError(500, "x", "http://internal"));
    const result = await install({ fileId: 101 });
    expect(result).toBe(
      "The workspace could not be reached: it answered 500. Nothing was installed.",
    );
    expect(upstream.seen.some((r) => r.path.includes("webplugins"))).toBe(false);

    vi.mocked(getFileInfo).mockRejectedValue(new Error("connect ECONNREFUSED 10.0.0.5:8092"));
    expect(await install({ fileId: 101 })).toBe(
      "The call could not be completed due to an internal error. Nothing was installed.",
    );
  });

  it("hands the reader the size a file may have and refuses a package over it", async () => {
    vi.mocked(readFileBytes).mockRejectedValue(new FileTooLargeError("101", MAX_FILE_BYTES));
    expect(await install({ fileId: 101 })).toBe(
      `Nothing was installed. File 101 is over ${MAX_FILE_BYTES} bytes.`,
    );
    expect(vi.mocked(readFileBytes)).toHaveBeenCalledWith("101", 32 * 1024 * 1024);
  });
});
