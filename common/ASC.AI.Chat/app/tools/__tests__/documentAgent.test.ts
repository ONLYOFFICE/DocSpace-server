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

import type { ProgressCallback } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { DocumentAgentConfig, getMcpServers } from "../../../config/index.js";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { afterUpstreamTimeout } from "../../__tests__/helpers/time.js";
import {
  HttpResponse,
  docspace,
  docspaceError,
  http,
  useUpstream,
  stalled,
} from "../../__tests__/helpers/upstream.js";
import type { ChatContextSnapshot } from "../../storage/chatContextSnapshot.js";
import type { DocspaceProfile } from "../../storage/profilesStorage.js";
import type { DocumentAgentSettings } from "../documentAgentClient.js";
import {
  TaskState,
  agentMessage,
  artifact,
  artifactEvent,
  fakeClient,
  statusEvent,
  streamOf,
  taskEvent,
  textPart,
  urlPart,
} from "./documentAgentA2aFakes.js";

// The `document_agent` tool from the call to its answer. The boundaries are stubbed: the
// configuration (`ai.documentAgent`, `ai.mcp`), the AI service's storages, the Files API and the
// AI service's settings route at the network (msw), and the A2A SDK's client, whose fake is
// what the real documentAgentClient drives.

const conf = vi.hoisted(() => ({
  agent: undefined as DocumentAgentConfig | undefined,
  mcp: [] as ReturnType<typeof getMcpServers>,
}));
vi.mock("../../../config/index.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../../config/index.js")>()),
  getDocumentAgent: () => conf.agent,
  getMcpServers: () => conf.mcp,
}));

const client = fakeClient();
const createFromUrl = vi.hoisted(() => vi.fn());
vi.mock("@a2a-js/sdk/client", () => ({
  ClientFactory: class {
    createFromUrl(url: string): unknown {
      return createFromUrl(url);
    }
  },
  JsonRpcTransportFactory: class {},
}));

// systemTools reads `ai.mcp` once, at import: these are the system servers it knows.
conf.mcp = [
  { id: "1", name: "docspace", endpoint: "http://mcp:8000/mcp" },
  { id: "2", name: "jira", endpoint: "http://jira-mcp/mcp" },
];

const { default: logger } = await import("../../log.js");
const { DOCUMENT_AGENT_TYPE } = await import("../../../config/index.js");
const { storage } = await import("../../storage/index.js");
const { aiServiceBaseUrl } = await import("../../storage/httpClient.js");
const requestContext = await import("../../requestContext.js");
const { DOCUMENT_AGENT_TOOL_NAME, documentAgentTools } = await import("../documentAgent.js");

const upstream = useUpstream();

const CONFIG: DocumentAgentConfig = {
  url: "http://agent:9838",
  token: "agent-token",
  taskTimeoutSeconds: 1800,
  maxToolCalls: undefined,
  shareSystemServers: false,
  portalUrl: undefined,
  filesUrl: "http://chat:5157",
};

const SESSION = { cookie: "asc_auth_key=user-key; theme=dark" };
const THREAD = "0198c3f4-6a2e-7c41-9b8d-2f5e7a1c3d90";
const MAX_FILE_BYTES = 32 * 1024 * 1024;

// `GET internal/ai/document-agent/settings` as the AI service answers it
// (common/ASC.AI.Integration/DocumentAgent/DocumentAgentSettings.cs, the AI service's settings action).
const tenantSettings = {
  enabled: true,
  profileId: null,
  reasoning: "default",
  maxToolCalls: 150,
  maxFileBytes: MAX_FILE_BYTES,
  taskLog: false,
  taskLogFolderId: null,
};

const chatProfile: DocspaceProfile = {
  id: "p-chat",
  name: "Chat",
  providerType: "openai",
  baseUrl: "https://api.openai.com/v1",
  key: "sk-chat",
  modelId: "gpt-5",
  reasoningSupport: {
    thinks: true,
    canDisable: true,
    depths: ["low", "medium", "high"],
    defaultDepth: "medium",
  },
} as DocspaceProfile;

// `GET api/2.0/files/file/{id}` (products/ASC.Files/Core/ApiModels/ResponseDto/FileDto.cs).
const savedFile = {
  id: 901,
  title: "Report (1).docx",
  folderId: 500,
  fileExst: ".docx",
  webUrl: "https://portal.example.com/doceditor?fileid=901",
};

// `GET api/2.0/files/folder/{id}` (products/ASC.Files/Core/ApiModels/ResponseDto/FolderDto.cs).
function folderDto(id: number, create: boolean) {
  return {
    id,
    title: `Folder ${id}`,
    parentId: 3,
    rootFolderType: 14,
    type: 1,
    security: { Create: create },
  };
}

let profiles: Record<string, DocspaceProfile | undefined>;
let assignments: Record<string, string | null>;

function arrange(
  options: {
    settings?: unknown;
    folders?: Record<string, ReturnType<typeof folderDto> | number>;
    my?: number | null;
  } = {},
): void {
  upstream.use(
    http.get("*/internal/ai/document-agent/settings", () =>
      docspace((options.settings ?? tenantSettings) as never),
    ),
    http.get("*/api/2.0/files/@my", () =>
      options.my === null ? docspaceError(404) : docspace({ current: { id: options.my ?? 500 } }),
    ),
    http.get("*/api/2.0/files/folder/:id", ({ params }) => {
      const folder = options.folders?.[String(params["id"])];
      if (folder === undefined) {
        return docspaceError(404);
      }
      return typeof folder === "number" ? docspaceError(folder) : docspace(folder);
    }),
    http.get("*/api/2.0/files/file/:id", ({ params }) =>
      docspace({ ...savedFile, id: Number(params["id"]) }),
    ),
  );
}

function completes(text = "Made the report.", fileIds: string[] = ["901"]): void {
  client.sendMessageStream.mockImplementation(() =>
    streamOf([
      taskEvent("t1", TaskState.TASK_STATE_SUBMITTED),
      ...fileIds.map((id) =>
        artifactEvent(
          artifact(`file-${id}`, [
            urlPart(`http://chat:5157/doceditor?fileId=${id}`, "Report.docx"),
          ]),
        ),
      ),
      statusEvent(TaskState.TASK_STATE_COMPLETED, agentMessage(textPart(text))),
    ]),
  );
}

interface CallOptions {
  headers?: Record<string, string>;
  entityId?: string;
  onprogress?: ProgressCallback;
  context?: () => void;
}

function call(args: unknown, options: CallOptions = {}): Promise<unknown> {
  return inRequestContext(options.headers ?? SESSION, () => {
    options.context?.();
    return documentAgentTools.callTool(
      DOCUMENT_AGENT_TOOL_NAME,
      args as Record<string, unknown>,
      options.entityId,
      options.onprogress ? { onprogress: options.onprogress } : undefined,
    );
  });
}

async function run(args: unknown, options?: CallOptions): Promise<unknown> {
  const answer = await call(args, options);
  expect(typeof answer).toBe("string");
  return JSON.parse(answer as string);
}

/** The request the agent was sent: its task text and its `metadata.docagent`. */
function sent(): { task: string; settings: DocumentAgentSettings } {
  const [params] = client.sendMessageStream.mock.calls[0]!;
  const part = params.message!.parts[0]!;
  return {
    task: part.content?.$case === "text" ? part.content.value : "",
    settings: params.metadata!["docagent"] as DocumentAgentSettings,
  };
}

function warnings(): string[] {
  return vi.mocked(logger.warn).mock.calls.map((c) => String(c[0]));
}

function snapshot(patch: Partial<ChatContextSnapshot>): ChatContextSnapshot {
  return {
    requested: { threadId: undefined, entityId: undefined, contextEntityId: undefined },
    aiReady: true,
    toolPermissionMode: null,
    profiles: [],
    global: {
      entityId: undefined,
      folder: undefined,
      assignments: {},
      reasoningLevel: null,
      toolPrefs: {},
      mcpServers: {},
    },
    entity: null,
    contextEntity: null,
    thread: null,
    messages: null,
    webSearch: null,
    documentAgentEnabled: undefined,
    stale: new Set(["profiles", "assignments", "mcpServers"]),
    ...patch,
  };
}

beforeEach(() => {
  conf.agent = { ...CONFIG };
  conf.mcp = [{ id: "1", name: "docspace", endpoint: "http://mcp:8000/mcp" }];
  createFromUrl.mockImplementation(() => Promise.resolve(client));
  profiles = { "p-chat": chatProfile };
  assignments = { Chat: "p-chat" };
  vi.spyOn(storage.profiles, "readByIdRaw").mockImplementation((id) =>
    Promise.resolve(profiles[id]),
  );
  vi.spyOn(storage.assignments, "readByType").mockImplementation((type) =>
    Promise.resolve(assignments[type] ?? null),
  );
  vi.spyOn(storage.attachments, "readManyByIds").mockResolvedValue([]);
  vi.spyOn(storage.mcpServers, "readAll").mockResolvedValue({});
});

describe("documentAgentTools.getTools", () => {
  it("offers nothing when the server has no document agent", async () => {
    conf.agent = undefined;

    expect(await inRequestContext(SESSION, () => documentAgentTools.getTools())).toEqual({});
    expect(upstream.seen).toEqual([]);
  });

  it("offers the one tool when the portal has the agent on", async () => {
    arrange();

    const tools = await inRequestContext(SESSION, () => documentAgentTools.getTools());

    expect(Object.keys(tools)).toEqual([DOCUMENT_AGENT_TYPE]);
    expect(tools[DOCUMENT_AGENT_TYPE]).toMatchInlineSnapshot(`
      [
        {
          "description": "Hand a document task to the document agent, which works autonomously with ONLYOFFICE Document Builder in a sandbox of its own and saves the resulting files into the workspace as new files. Use it whenever the user wants a real file made or an existing file worked on: a report, a letter, a spreadsheet with data and charts, a presentation, a filled template, a translation, a redaction, a merge, a conversion, a fix of a file. The agent opens the files itself: never read a file for it and never put a file's content into \`task\`. The files attached to the current message go along automatically; every other file the request is about (named, selected or open) goes by id in \`fileIds\`, a whole folder in \`folderIds\`. Existing files are never changed: a result always comes back as a new file. The call returns the agent's summary and the saved files with \`url\`; when you mention a file, link it exactly as \`[<title>](<url>)\`, copying \`url\` verbatim. The task may take several minutes.",
          "enabled": true,
          "inputSchema": {
            "properties": {
              "fileIds": {
                "description": "Ids of every workspace file the request is about, besides the message's attachments: the file the user named, selected or has open. A request about a file always carries the file's id here, never its text in \`task\`.",
                "items": {
                  "type": "string",
                },
                "type": "array",
              },
              "folderIds": {
                "description": "Ids of workspace folders or rooms whose files the agent should read, only when the request is about a whole folder.",
                "items": {
                  "type": "string",
                },
                "type": "array",
              },
              "task": {
                "description": "The user's request for the agent, in their own words and language, as close to verbatim as possible. Add only what the user said elsewhere in the conversation and the agent needs (the format, the file name, the language). Do not rephrase it into steps, do not add your own method, and do not paste any file content: the agent cannot ask questions, but it has its own procedures and reads the files itself.",
                "type": "string",
              },
            },
            "required": [
              "task",
            ],
            "type": "object",
          },
          "name": "document_agent",
          "requireApproval": false,
          "serverType": "document-agent",
        },
      ]
    `);
  });

  it.each([
    ["switched it off", () => arrange({ settings: { ...tenantSettings, enabled: false } })],
    [
      "settings that cannot be read",
      () =>
        upstream.use(http.get("*/internal/ai/document-agent/settings", () => docspaceError(500))),
    ],
  ])("offers nothing when the portal has %s", async (_what, setup) => {
    setup();

    expect(await inRequestContext(SESSION, () => documentAgentTools.getTools())).toEqual({});
  });

  it("answers from the round's primed context without reading the settings", async () => {
    const tools = await inRequestContext(SESSION, () => {
      requestContext.setChatContextSnapshot(snapshot({ documentAgentEnabled: false }));
      return documentAgentTools.getTools();
    });

    expect(tools).toEqual({});
    expect(upstream.seen).toEqual([]);
  });
});

describe("documentAgentTools.callTool refusing the call", () => {
  it("answers an unknown tool name", async () => {
    expect(await inRequestContext(SESSION, () => documentAgentTools.callTool("other", {}))).toBe(
      'Unknown tool "other"',
    );
  });

  it("answers that the agent is not configured", async () => {
    conf.agent = undefined;

    expect(await call({ task: "Write" })).toBe(
      "The document agent is not configured on this server.",
    );
    expect(upstream.seen).toEqual([]);
  });

  it.each([
    ["null args", null],
    ["a string", "Write a report"],
    ["an array", ["Write"]],
    ["{}", {}],
    ["task null", { task: null }],
    ["task a number", { task: 5 }],
    ["task an array", { task: ["Write"] }],
    ["task an object", { task: { text: "Write" } }],
    ["task empty", { task: "" }],
    ["task blank", { task: "  \n " }],
  ])("asks for a task when given %s, before any request", async (_what, args) => {
    expect(await call(args)).toBe("document_agent requires a non-empty 'task'.");
    expect(upstream.seen).toEqual([]);
    expect(client.sendMessageStream).not.toHaveBeenCalled();
  });
});

describe("documentAgentTools.callTool running a task", () => {
  it("answers the summary and the saved files of a completed task", async () => {
    arrange();
    completes();

    expect(await run({ task: "  Write a report  " })).toEqual({
      data: {
        summary: "Made the report.",
        files: [
          {
            id: "901",
            title: "Report (1).docx",
            url: "/doceditor?fileId=901",
            folderId: "500",
            fileExst: ".docx",
            webUrl: "https://portal.example.com/doceditor?fileid=901",
          },
        ],
      },
    });
    expect(sent().task).toBe("Write a report");
    expect(createFromUrl).toHaveBeenCalledWith("http://agent:9838");
  });

  it("hands the agent the model, the servers, the outputs and the limits as settings", async () => {
    arrange();
    completes();

    await run(
      { task: "Write" },
      {
        headers: {
          ...SESSION,
          "x-forwarded-host": "portal.example.com",
          "x-forwarded-proto": "https",
        },
      },
    );

    const own = {
      "x-docagent-token": "agent-token",
      cookie: "asc_auth_key=user-key",
      "x-forwarded-host": "portal.example.com",
      "x-forwarded-proto": "https",
    };
    expect(sent().settings).toEqual({
      model: {
        provider: "openai",
        baseUrl: "https://api.openai.com/v1",
        modelId: "gpt-5",
        reasoning: "medium",
        apiKey: "sk-chat",
      },
      mcpServers: {
        docspace: {
          url: "http://mcp:8000/mcp",
          headers: { authorization: "user-key", referer: "https://portal.example.com/" },
        },
        workspace: { url: "http://chat:5157/document-agent/files/mcp", headers: own },
      },
      outputs: { url: "http://chat:5157/document-agent/files/outputs?folder=500", headers: own },
      limits: { taskTimeoutSeconds: 1800, maxFileBytes: MAX_FILE_BYTES, maxToolCalls: 150 },
    });
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      "document_agent: start entityId=- profile=p-chat model=gpt-5 files=[] folders=[] " +
        "servers=[docspace,workspace] target=500 log=-",
    );
  });

  it("gives the task the configured timeout plus a minute", async () => {
    arrange();
    completes();
    conf.agent = { ...CONFIG, taskTimeoutSeconds: 60 };
    const timeout = vi.spyOn(AbortSignal, "timeout");

    await run({ task: "Write" });

    expect(timeout).toHaveBeenCalledWith(120_000);
    expect(client.sendMessageStream.mock.calls[0]![1]!.signal).toBe(timeout.mock.results[0]!.value);
  });

  it.each([
    [
      "the portal's lower limits",
      { maxToolCalls: 20, maxFileBytes: 1000 },
      undefined,
      { maxToolCalls: 20, maxFileBytes: 1000 },
    ],
    ["the server's lower tool budget", {}, 30, { maxToolCalls: 30, maxFileBytes: MAX_FILE_BYTES }],
    [
      "the agent's ceilings over higher stored ones",
      { maxToolCalls: 900, maxFileBytes: MAX_FILE_BYTES * 4 },
      undefined,
      { maxToolCalls: 150, maxFileBytes: MAX_FILE_BYTES },
    ],
  ])("takes %s", async (_what, stored, serverMax, limits) => {
    arrange({ settings: { ...tenantSettings, ...stored } });
    completes();
    conf.agent = { ...CONFIG, maxToolCalls: serverMax };

    await run({ task: "Write" });

    expect(sent().settings.limits).toEqual({ taskTimeoutSeconds: 1800, ...limits });
  });

  it("names the input files and folders in the task, the attachments' entries once each", async () => {
    arrange();
    completes();
    vi.mocked(storage.attachments.readManyByIds).mockResolvedValue([
      { id: "att-1", path: "41/report.docx" },
      { id: "att-2", path: "12/notes.txt" },
      null,
      { id: "att-3" },
      { id: "att-4", path: "/x" },
    ] as never);

    const answer = await run(
      {
        task: "Merge them",
        fileIds: [" 12 ", 13, "", "  ", 1.5, Number.NaN, Infinity, null, { id: 1 }, "12"],
        folderIds: ["7", 8],
      },
      { context: () => requestContext.setRoundAttachmentIds(["att-1", "att-2", "att-3", "att-4"]) },
    );

    expect(answer).toMatchObject({ data: { summary: "Made the report." } });
    expect(storage.attachments.readManyByIds).toHaveBeenCalledWith([
      "att-1",
      "att-2",
      "att-3",
      "att-4",
    ]);
    expect(sent().task).toBe(
      "Merge them\n\n" +
        "The request's input files, by workspace id. To open or edit a file as it is, fetch it with " +
        "workspace_get_file_link: it lands in your sandbox as a file. " +
        "docspace_download_file_as_text gives a file's text only; docspace_get_folder_content lists " +
        "a folder. Never change the originals: produce every result as a new file under " +
        "/workspace/out.\n" +
        "- file ids: 12, 13, 1.5, 41\n" +
        "- folder ids: 7, 8",
    );
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      expect.stringContaining("files=[12,13,1.5,41] folders=[7,8]"),
    );
  });

  it.each([
    ["only files", { fileIds: ["3"] }, "- file ids: 3", "- folder ids"],
    ["only folders", { folderIds: ["4"] }, "- folder ids: 4", "- file ids"],
  ])("lists %s", async (_what, ids, present, absent) => {
    arrange();
    completes();

    await run({ task: "Do", ...ids });

    expect(sent().task).toContain(present);
    expect(sent().task).not.toContain(absent);
  });

  it.each([
    ["a string", "12"],
    ["an object", { id: "12" }],
    ["null", null],
  ])("takes no file ids from %s", async (_what, fileIds) => {
    arrange();
    completes();

    await run({ task: "Do", fileIds });

    expect(sent().task).toBe("Do");
  });

  it("reports each step of the agent as numbered progress", async () => {
    arrange();
    const onprogress = vi.fn();
    client.sendMessageStream.mockImplementation(() =>
      streamOf([
        taskEvent("t1", TaskState.TASK_STATE_WORKING, agentMessage(textPart("Reading"))),
        statusEvent(TaskState.TASK_STATE_WORKING, agentMessage(textPart("Writing"))),
        statusEvent(TaskState.TASK_STATE_COMPLETED, agentMessage(textPart("ok"))),
      ]),
    );

    await run({ task: "Do" }, { onprogress });

    expect(onprogress.mock.calls).toEqual([
      [{ progress: 1, message: "Reading" }],
      [{ progress: 2, message: "Writing" }],
    ]);
  });

  it.each([
    [
      "with its text",
      "Out of tool calls",
      "The document agent did not finish the task (failed): Out of tool calls",
    ],
    ["without text", "", "The document agent did not finish the task (failed): no details"],
  ])(
    "answers a task that did not finish %s as an error with the files it saved",
    async (_what, text, error) => {
      arrange();
      client.sendMessageStream.mockImplementation(() =>
        streamOf([
          taskEvent("t1", TaskState.TASK_STATE_SUBMITTED),
          artifactEvent(artifact("p", [urlPart("http://chat:5157/doceditor?fileId=902")])),
          statusEvent(TaskState.TASK_STATE_FAILED, agentMessage(textPart(text))),
        ]),
      );

      expect(await run({ task: "Do" })).toEqual({
        error,
        data: {
          files: [
            {
              id: "902",
              title: "Report (1).docx",
              url: "/doceditor?fileId=902",
              folderId: "500",
              fileExst: ".docx",
              webUrl: "https://portal.example.com/doceditor?fileid=901",
            },
          ],
        },
      });
    },
  );

  it("answers a task that did not finish and saved nothing with the error alone", async () => {
    arrange();
    client.sendMessageStream.mockImplementation(() =>
      streamOf([taskEvent("t1", TaskState.TASK_STATE_REJECTED, agentMessage(textPart("No.")))]),
    );

    expect(await run({ task: "Do" })).toEqual({
      error: "The document agent did not finish the task (rejected): No.",
    });
  });

  it.each([
    ["500", () => docspaceError(500), "500"],
    ["a dropped connection", () => HttpResponse.error(), "no HTTP answer"],
  ])(
    "lists a saved file under the artifact's name and warns when its read-back answers %s",
    async (_what, answer, status) => {
      arrange();
      completes();
      upstream.use(http.get("*/api/2.0/files/file/:id", answer));

      expect(await run({ task: "Do" })).toEqual({
        data: {
          summary: "Made the report.",
          files: [
            { id: "901", title: "Report.docx", url: "/doceditor?fileId=901", fileExst: ".docx" },
          ],
        },
      });
      expect(warnings()).toContain(
        `document_agent: saved file 901 could not be read back (${status}), listing it without its details`,
      );
      expect(warnings().join("\n")).not.toContain("http");
    },
  );

  it("lists a saved file the read-back no longer finds under the artifact's name without a warning", async () => {
    arrange();
    completes();
    upstream.use(http.get("*/api/2.0/files/file/:id", () => docspaceError(404)));

    expect(await run({ task: "Do" })).toEqual({
      data: {
        summary: "Made the report.",
        files: [
          { id: "901", title: "Report.docx", url: "/doceditor?fileId=901", fileExst: ".docx" },
        ],
      },
    });
    expect(warnings().filter((w) => w.includes("could not be read back"))).toEqual([]);
  });

  it("answers a completed task that saved nothing with an empty list", async () => {
    arrange();
    completes("Nothing to do.", []);

    expect(await run({ task: "Do" })).toEqual({ data: { summary: "Nothing to do.", files: [] } });
  });
});

describe("the portal's settings for a task", () => {
  it("answers that the agent is switched off on the portal, without calling the agent", async () => {
    arrange({ settings: { ...tenantSettings, enabled: false } });

    expect(await run({ task: "Do" })).toEqual({
      error: "The document agent is switched off on this portal.",
    });
    expect(client.sendMessageStream).not.toHaveBeenCalled();
  });

  it.each([
    ["500", () => docspaceError(500)],
    ["403", () => docspaceError(403)],
    ["a dropped connection", () => HttpResponse.error()],
  ])("keeps the agent off when the settings read answers %s", async (_what, answer) => {
    upstream.use(http.get("*/internal/ai/document-agent/settings", answer));

    expect(await run({ task: "Do" })).toEqual({
      error: "The document agent is switched off on this portal.",
    });
    expect(client.sendMessageStream).not.toHaveBeenCalled();
  });

  // Each field of the stored settings missing, null and of the wrong type: the nullable
  // references read as none, anything else broken keeps the agent off.
  const wrongType: Record<string, unknown> = {
    enabled: "true",
    profileId: 5,
    reasoning: 5,
    maxToolCalls: "150",
    maxFileBytes: "1000",
    taskLog: "yes",
    taskLogFolderId: true,
  };
  const nullable = new Set(["profileId", "taskLogFolderId"]);
  it.each(
    Object.keys(tenantSettings).flatMap((field) => [
      [field, "missing", undefined, nullable.has(field)],
      [field, "null", null, nullable.has(field)],
      [field, "of the wrong type", wrongType[field], false],
    ]),
  )("reads settings whose %s is %s", async (field, _how, value, runs) => {
    const stored: Record<string, unknown> = { ...tenantSettings };
    if (value === undefined) {
      delete stored[field as string];
    } else {
      stored[field as string] = value;
    }
    arrange({ settings: stored });
    completes();

    const answer = await run({ task: "Do" });

    if (runs) {
      expect(answer).toMatchObject({ data: { summary: "Made the report." } });
    } else {
      expect(answer).toEqual({ error: "The document agent is switched off on this portal." });
      expect(client.sendMessageStream).not.toHaveBeenCalled();
    }
  });
});

describe("the model a task runs on", () => {
  const picked: DocspaceProfile = {
    ...chatProfile,
    id: "p-agent",
    modelId: "gpt-5-mini",
    key: "sk-agent",
  };

  it("runs on the portal's pick for the agent", async () => {
    arrange({ settings: { ...tenantSettings, profileId: "p-agent", reasoning: "high" } });
    completes();
    profiles["p-agent"] = picked;

    await run({ task: "Do" });

    expect(sent().settings.model).toEqual({
      provider: "openai",
      baseUrl: "https://api.openai.com/v1",
      modelId: "gpt-5-mini",
      reasoning: "high",
      apiKey: "sk-agent",
    });
    expect(storage.assignments.readByType).not.toHaveBeenCalled();
  });

  it("falls back to the round's profile when the pick is gone, and says so", async () => {
    arrange({ settings: { ...tenantSettings, profileId: "p-gone" } });
    completes();
    profiles["p-round"] = { ...chatProfile, id: "p-round", modelId: "round-model" };

    await run({ task: "Do" }, { context: () => requestContext.setRoundProfileId("p-round") });

    expect(sent().settings.model.modelId).toBe("round-model");
    expect(warnings()).toContain(
      "document_agent: the portal's agent profile p-gone is gone, using the chat's",
    );
    expect(storage.assignments.readByType).not.toHaveBeenCalled();
  });

  it("falls back to the chat assignment when the round's profile is gone", async () => {
    arrange();
    completes();

    await run({ task: "Do" }, { context: () => requestContext.setRoundProfileId("p-missing") });

    expect(sent().settings.model.modelId).toBe("gpt-5");
    expect(vi.mocked(storage.profiles.readByIdRaw).mock.calls).toEqual([["p-missing"], ["p-chat"]]);
  });

  it("falls back to the Default assignment without a Chat one", async () => {
    arrange();
    completes();
    assignments = { Default: "p-chat" };

    await run({ task: "Do" });

    expect(vi.mocked(storage.assignments.readByType).mock.calls).toEqual([["Chat"], ["Default"]]);
    expect(sent().settings.model.modelId).toBe("gpt-5");
  });

  it.each([
    ["no assignment at all", {}, {}],
    ["an assigned profile that is gone", { Chat: "p-gone" }, {}],
  ])("answers that no model is configured with %s", async (_what, assigned, known) => {
    arrange();
    assignments = assigned;
    profiles = known;

    expect(await run({ task: "Do" })).toEqual({
      error: "No model is configured for this chat, the document agent cannot run.",
    });
    expect(client.sendMessageStream).not.toHaveBeenCalled();
  });

  it("answers an internal error when the profile cannot be read", async () => {
    arrange();
    vi.mocked(storage.assignments.readByType).mockRejectedValue(new Error("AI service 500"));

    expect(await call({ task: "Do" })).toBe(
      "The document task could not be completed due to an internal error.",
    );
    expect(warnings()).toEqual(["document_agent failed: AI service 500"]);
  });

  it("sends a third-party profile's headers and Responses API flag, and no source", async () => {
    arrange();
    completes();
    profiles["p-chat"] = {
      ...chatProfile,
      key: undefined,
      headers: { "x-gateway": "g1" },
      useResponsesApi: false,
    } as DocspaceProfile;

    await run(
      { task: "Do" },
      { context: () => requestContext.setSourceMeta({ id: "5", type: "Folder" }) },
    );

    expect(sent().settings.model).toEqual({
      provider: "openai",
      baseUrl: "https://api.openai.com/v1",
      modelId: "gpt-5",
      reasoning: "medium",
      headers: { "x-gateway": "g1" },
      responsesApi: false,
    });
  });

  it("leaves out empty profile headers", async () => {
    arrange();
    completes();
    profiles["p-chat"] = { ...chatProfile, headers: {} };

    await run({ task: "Do" });

    expect(sent().settings.model).not.toHaveProperty("headers");
  });

  describe("on the portal's own provider", () => {
    const portal: DocspaceProfile = {
      ...chatProfile,
      providerType: "onlyoffice",
      baseUrl: "https://portal.example.com",
      key: "placeholder",
      headers: { "x-extra": "1" },
    } as DocspaceProfile;

    beforeEach(() => {
      profiles["p-chat"] = portal;
    });

    it("goes through the AI service's gateway with the session cookie, the tenant and the round's source", async () => {
      arrange();
      completes();

      await run(
        { task: "Do" },
        {
          headers: {
            ...SESSION,
            authorization: "Bearer other",
            "x-forwarded-host": "portal.example.com",
            "x-forwarded-proto": "https",
          },
          context: () => requestContext.setSourceMeta({ id: "5", type: "Room", title: "Sales" }),
        },
      );

      expect(sent().settings.model).toEqual({
        provider: "onlyoffice",
        baseUrl: `${aiServiceBaseUrl}/api/2.0/ai/gateway`,
        modelId: "gpt-5",
        reasoning: "medium",
        apiKey: "placeholder",
        headers: {
          "x-extra": "1",
          cookie: "asc_auth_key=user-key",
          "x-forwarded-host": "portal.example.com",
          "x-forwarded-proto": "https",
        },
        source: { id: "5", type: "Room", title: "Sales" },
      });
    });

    it("goes through the configured portal address and the BYOK route of a profile on the portal's key", async () => {
      arrange();
      completes();
      conf.agent = { ...CONFIG, portalUrl: "https://portal.example.com" };
      profiles["p-chat"] = { ...portal, byokProvider: "openai" };

      await run({ task: "Do" });

      expect(sent().settings.model.baseUrl).toBe(
        "https://portal.example.com/api/2.0/ai/gateway/byok/p-chat",
      );
    });

    it("puts a caller's Authorization in place of the key when there is no session cookie", async () => {
      arrange();
      completes();

      await run({ task: "Do" }, { headers: { authorization: "Bearer api-token" } });

      expect(sent().settings.model).toEqual({
        provider: "onlyoffice",
        baseUrl: `${aiServiceBaseUrl}/api/2.0/ai/gateway`,
        modelId: "gpt-5",
        reasoning: "medium",
        headers: { "x-extra": "1", authorization: "Bearer api-token" },
      });
      expect(sent().settings.outputs.headers).toEqual({
        "x-docagent-token": "agent-token",
        authorization: "Bearer api-token",
      });
    });

    it("sends the key and the profile's headers alone without a session", async () => {
      arrange();
      completes();

      await run({ task: "Do" }, { headers: {} });

      expect(sent().settings.model).toEqual({
        provider: "onlyoffice",
        baseUrl: `${aiServiceBaseUrl}/api/2.0/ai/gateway`,
        modelId: "gpt-5",
        reasoning: "medium",
        apiKey: "placeholder",
        headers: { "x-extra": "1" },
      });
      expect(sent().settings.outputs.headers).toEqual({ "x-docagent-token": "agent-token" });
    });

    it("forwards the authorization cookie as the session", async () => {
      arrange();
      completes();

      await run({ task: "Do" }, { headers: { cookie: "authorization=tok" } });

      expect(sent().settings.model.headers).toEqual({
        "x-extra": "1",
        cookie: "authorization=tok",
      });
    });
  });
});

describe("where the files of a task go", () => {
  it("saves into the current folder when the user may create there", async () => {
    arrange({ folders: { "42": folderDto(42, true) } });
    completes();

    await run({ task: "Do" }, { entityId: "42" });

    expect(sent().settings.outputs.url).toBe(
      "http://chat:5157/document-agent/files/outputs?folder=42",
    );
    expect(upstream.seen.some((r) => r.path.startsWith("/api/2.0/files/@my"))).toBe(false);
  });

  it.each([
    ["a folder the user may not create in", { "42": folderDto(42, false) }],
    ["a folder that does not exist", {}],
  ])("saves into My documents from %s", async (_what, folders) => {
    arrange({ folders });
    completes();

    await run({ task: "Do" }, { entityId: "42" });

    expect(sent().settings.outputs.url).toBe(
      "http://chat:5157/document-agent/files/outputs?folder=500",
    );
    expect(upstream.seen.map((r) => r.path)).toContain("/api/2.0/files/@my?count=1");
  });

  it("answers that there is no folder when My documents cannot be resolved", async () => {
    arrange({ my: null });

    expect(await run({ task: "Do" })).toEqual({
      error: "There is no folder the resulting files could be saved into.",
    });
    expect(client.sendMessageStream).not.toHaveBeenCalled();
  });

  it("answers that the workspace could not be reached when the My documents read fails", async () => {
    arrange();
    upstream.use(http.get("*/api/2.0/files/@my", () => docspaceError(500)));

    const answer = await call({ task: "Do" });

    expect(answer).toBe("The workspace could not be reached: it answered 500.");
    expect(client.sendMessageStream).not.toHaveBeenCalled();
  });

  it("answers that the workspace could not be reached when the folder read fails", async () => {
    arrange({ folders: { "42": 500 } });

    const answer = await call({ task: "Do" }, { entityId: "42" });

    expect(answer).toBe("The workspace could not be reached: it answered 500.");
    expect(answer).not.toContain("http");
    expect(client.sendMessageStream).not.toHaveBeenCalled();
  });

  it.each([
    ["the round's thread", () => requestContext.setRoundThreadId(THREAD), `&thread=${THREAD}`],
    [
      "the primed context's thread",
      () =>
        requestContext.setChatContextSnapshot(
          snapshot({
            requested: { threadId: THREAD, entityId: undefined, contextEntityId: undefined },
          }),
        ),
      `&thread=${THREAD}`,
    ],
    ["no thread", () => undefined, ""],
  ])("names %s on the results' address", async (_what, context, thread) => {
    arrange();
    completes();

    await run({ task: "Do" }, { context });

    expect(sent().settings.outputs.url).toBe(
      `http://chat:5157/document-agent/files/outputs?folder=500${thread}`,
    );
  });
});

describe("the task's log", () => {
  it.each([
    ["no folder of its own", null, "500"],
    ["the results' folder", "500", "500"],
    ["a writable folder", "61", "61"],
  ])("goes to %s", async (_what, taskLogFolderId, folder) => {
    arrange({
      settings: { ...tenantSettings, taskLog: true, taskLogFolderId },
      folders: { "61": folderDto(61, true) },
    });
    completes();

    await run({ task: "Do" }, { context: () => requestContext.setRoundThreadId(THREAD) });

    expect(sent().settings.log).toEqual({
      url: `http://chat:5157/document-agent/files/outputs?folder=${folder}`,
      headers: { "x-docagent-token": "agent-token", cookie: "asc_auth_key=user-key" },
    });
    expect(vi.mocked(logger.info)).toHaveBeenCalledWith(
      expect.stringMatching(new RegExp(`log=${folder}$`)),
    );
  });

  it("goes next to the results from a folder the user may not create in, and says so", async () => {
    arrange({
      settings: { ...tenantSettings, taskLog: true, taskLogFolderId: "61" },
      folders: { "61": folderDto(61, false) },
    });
    completes();

    await run({ task: "Do" });

    expect(sent().settings.log?.url).toBe(
      "http://chat:5157/document-agent/files/outputs?folder=500",
    );
    expect(warnings()).toContain(
      "document_agent: log folder 61 is not writable for the user, logging next to the results",
    );
  });

  it("goes next to the results from a folder the user may not see, and says so", async () => {
    arrange({
      settings: { ...tenantSettings, taskLog: true, taskLogFolderId: "61" },
      folders: { "61": 403 },
    });
    completes();

    await run({ task: "Do" });

    expect(sent().settings.log?.url).toBe(
      "http://chat:5157/document-agent/files/outputs?folder=500",
    );
    expect(warnings()).toContain(
      "document_agent: log folder 61 is not writable for the user, logging next to the results",
    );
  });

  // Only a 403 says the log folder is not the user's to write: a read that got no answer is not.
  it("fails the tool call when the log folder read gets no answer, without calling the agent", async () => {
    arrange({ settings: { ...tenantSettings, taskLog: true, taskLogFolderId: "61" } });
    upstream.use(http.get("*/api/2.0/files/folder/61", stalled));
    completes();

    const outcome = await afterUpstreamTimeout(() => call({ task: "Do" }));

    expect(outcome).toEqual({
      value: "The document task could not be completed due to an internal error.",
    });
    expect(client.sendMessageStream).not.toHaveBeenCalled();
    expect(warnings()).not.toContain(
      "document_agent: log folder 61 is not writable for the user, logging next to the results",
    );
  });

  it("answers that the workspace could not be reached when the log folder read fails, without calling the agent", async () => {
    arrange({
      settings: { ...tenantSettings, taskLog: true, taskLogFolderId: "61" },
      folders: { "61": 500 },
    });
    completes();

    const answer = await call({ task: "Do" });

    expect(answer).toBe("The workspace could not be reached: it answered 500.");
    expect(answer).not.toContain("http");
    expect(client.sendMessageStream).not.toHaveBeenCalled();
    expect(warnings()).not.toContain(
      "document_agent: log folder 61 is not writable for the user, logging next to the results",
    );
  });

  it("is left out when the portal keeps no logs", async () => {
    arrange();
    completes();

    await run({ task: "Do" });

    expect(sent().settings).not.toHaveProperty("log");
  });
});

describe("the MCP servers the agent gets", () => {
  it("shares the other system servers with the session when the host allows it", async () => {
    arrange();
    completes();
    conf.agent = { ...CONFIG, shareSystemServers: true };
    conf.mcp = [
      { id: "1", name: "docspace", endpoint: "http://mcp:8000/mcp" },
      { id: "2", name: "jira", endpoint: "http://jira-mcp/mcp" },
      { id: "3", name: "late", endpoint: "http://late-mcp/mcp" },
      { id: "4", name: "Bad Name", endpoint: "http://bad/mcp" },
      { id: "5", name: "sandbox", endpoint: "http://sandbox/mcp" },
    ];

    await run({ task: "Do" }, { headers: { authorization: "Bearer api-token" } });

    const session = { authorization: "Bearer api-token" };
    expect(sent().settings.mcpServers).toEqual({
      docspace: { url: "http://mcp:8000/mcp", headers: session },
      jira: { url: "http://jira-mcp/mcp", headers: session },
      // Not known to systemTools at import: its endpoint as configured now.
      late: { url: "http://late-mcp/mcp", headers: session },
      workspace: {
        url: "http://chat:5157/document-agent/files/mcp",
        headers: { "x-docagent-token": "agent-token", authorization: "Bearer api-token" },
      },
    });
    expect(warnings()).toEqual([
      'document_agent: MCP server "Bad Name" skipped, the agent cannot take that name',
      'document_agent: MCP server "sandbox" skipped, the agent cannot take that name',
    ]);
  });

  it("hands over the system servers without headers when there is no session", async () => {
    arrange();
    completes();

    await run({ task: "Do" }, { headers: {} });

    expect(sent().settings.mcpServers?.["docspace"]).toEqual({ url: "http://mcp:8000/mcp" });
  });

  it("warns when the portal's own server is not configured", async () => {
    arrange();
    completes();
    conf.mcp = [{ id: "2", name: "jira", endpoint: "http://jira-mcp/mcp" }];

    await run({ task: "Do" });

    expect(Object.keys(sent().settings.mcpServers ?? {})).toEqual(["workspace"]);
    expect(warnings()).toEqual([
      'document_agent: the portal\'s MCP server "docspace" is not configured in ai.mcp, the agent cannot read workspace files',
    ]);
  });

  it("adds the portal's custom servers after its own, with their string headers only", async () => {
    arrange();
    completes();
    vi.mocked(storage.mcpServers.readAll).mockResolvedValue({
      crm: { url: "https://crm.example.com/mcp", headers: { "x-key": "k", n: 5 } },
      plain: { url: "https://plain.example.com/mcp" },
      empty: { url: "https://empty.example.com/mcp", headers: { n: 1 } },
      workspace: { url: "https://evil.example.com/mcp", headers: { "x-key": "k" } },
      "Bad.Name": { url: "https://x.example.com/mcp" },
      docbuilder: { url: "https://y.example.com/mcp" },
    } as never);

    await run({ task: "Do" });

    const servers = sent().settings.mcpServers ?? {};
    expect(Object.keys(servers)).toEqual(["docspace", "workspace", "crm", "plain", "empty"]);
    expect(servers["crm"]).toEqual({
      url: "https://crm.example.com/mcp",
      headers: { "x-key": "k" },
    });
    expect(servers["plain"]).toEqual({ url: "https://plain.example.com/mcp" });
    expect(servers["empty"]).toEqual({ url: "https://empty.example.com/mcp" });
    expect(servers["workspace"]!.url).toBe("http://chat:5157/document-agent/files/mcp");
    expect(warnings()).toEqual([
      'document_agent: MCP server "workspace" skipped, a server of that name is already handed over',
      'document_agent: MCP server "Bad.Name" skipped, the agent cannot take that name',
      'document_agent: MCP server "docbuilder" skipped, the agent cannot take that name',
    ]);
  });
});

describe("documentAgentTools.callTool when the run fails", () => {
  it("answers that the agent is unavailable when it cannot be reached", async () => {
    arrange();
    conf.agent = { ...CONFIG, url: "http://agent-down:9838" };
    createFromUrl.mockImplementation(() => Promise.reject(new Error("ECONNREFUSED")));

    const answer = await call({ task: "Do" });

    expect(answer).toBe("The document agent is unavailable: the document agent is unreachable.");
    expect(answer).not.toContain("agent-down");
    expect(warnings()).toEqual([
      "documentAgent: the agent at http://agent-down:9838 is unreachable: ECONNREFUSED",
      "document_agent failed: the document agent is unreachable",
    ]);
  });

  it("answers that the task timed out when its signal fires", async () => {
    arrange();
    const timeout = new DOMException("The operation was aborted due to timeout", "TimeoutError");
    vi.spyOn(AbortSignal, "timeout").mockReturnValue(AbortSignal.abort(timeout));
    client.sendMessageStream.mockImplementation(() => streamOf([], new Error("aborted")));

    expect(await call({ task: "Do" })).toBe(
      "The document task timed out before the agent finished.",
    );
  });

  it.each([
    ["an Error", new Error("boom"), "boom"],
    ["a string", "boom", "boom"],
  ])("answers an internal error for %s thrown along the way", async (_what, err, text) => {
    arrange();
    vi.mocked(storage.mcpServers.readAll).mockResolvedValue({});
    vi.spyOn(storage.attachments, "readManyByIds").mockRejectedValue(err);

    expect(
      await call({ task: "Do" }, { context: () => requestContext.setRoundAttachmentIds(["a"]) }),
    ).toBe("The document task could not be completed due to an internal error.");
    expect(warnings()).toEqual([`document_agent failed: ${text}`]);
  });
});
