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

import { UpstreamShapeError } from "../../storage/upstreamShape.js";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import {
  setKnowledgeRoomIds,
  setResolvedAttachmentId,
  setResolvedFormId,
} from "../../requestContext.js";
import { AiServiceHttpError, aiService } from "../../storage/httpClient.js";
import {
  DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
  extractAttachmentRefIds,
  fileEditorUrl,
  HttpToolsAdapter,
  safeGetToolsPrompt,
  withGeneratedFileUrl,
} from "../httpToolsAdapter.js";

const state = vi.hoisted(() => ({ documentAgent: false }));

vi.mock("../../../config/index.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../../config/index.js")>()),
  getDocumentAgent: () =>
    state.documentAgent ? { url: "http://agent:9838", token: "secret" } : undefined,
}));

const upstream = useUpstream();

const SILENT = "docspace-integration";
const APPROVAL = "docspace-integration-approval";
const LINK_HINT =
  "The result contains `url`, the relative link to the created file. " +
  "When you mention the file in your reply, link it exactly as " +
  "`[<title>](<url>)`, copying `url` verbatim — never invent, shorten or " +
  "rewrite the link, and never link to the file by its id or title alone.";

const EMPTY_CONTEXT = {
  folderId: 0,
  formId: 0,
  attachmentId: "",
  formSubAgent: false,
  knowledgeRoomId: 0,
  knowledgeRoomIds: [],
};

// POST internal/ai/tools/list answers `ToolListResponse(Prompt, Tools)` of
// products/ASC.AI/Server/Tools/Core/ToolDescriptor.cs (ToolsController.ListAsync).
const FOLDER_SCHEMA = {
  type: "object",
  properties: { folderId: { type: "integer" } },
  required: ["folderId"],
};
const DOCX_SCHEMA = { type: "object", properties: { title: { type: "string" } } };
const TOOL_LIST = {
  prompt: "Use the DocSpace tools.",
  tools: [
    { name: "get_folder", description: "Read a folder", parameters: FOLDER_SCHEMA },
    { name: "onlyoffice_generate_docx", description: "Create a document", parameters: DOCX_SCHEMA },
  ],
};

function answerList(body: unknown = TOOL_LIST): void {
  upstream.use(http.post("*/internal/ai/tools/list", () => docspace(body as never)));
}

// POST internal/ai/tools/call answers `IReadOnlyList<ToolCallResult(Id, Name, Result, Error)>`
// of products/ASC.AI/Server/Tools/Core/ToolCallResult.cs (ToolsController.CallAsync).
function answerCall(body: unknown): void {
  upstream.use(http.post("*/internal/ai/tools/call", () => docspace(body as never)));
}

// POST internal/ai/attachments/read answers `AttachmentDto` records (HttpAttachmentsStorage).
function answerAttachments(body: unknown): void {
  upstream.use(http.post("*/internal/ai/attachments/read", () => docspace(body as never)));
}

beforeEach(() => {
  state.documentAgent = false;
});

describe("fileEditorUrl", () => {
  it("builds the relative editor link of a numeric id", () => {
    expect(fileEditorUrl(42)).toBe("/doceditor?fileId=42");
  });

  it("encodes a string id", () => {
    expect(fileEditorUrl("a b/c&d")).toBe("/doceditor?fileId=a%20b%2Fc%26d");
  });
});

describe("withGeneratedFileUrl", () => {
  it.each<[string, unknown]>([
    ["a string", "done"],
    ["null", null],
    ["an array", [{ data: { id: 1 } }]],
    ["an object without data", { ok: true }],
    ["data that is a string", { data: "x" }],
    ["data that is an array", { data: [1] }],
    ["data without an id", { data: { title: "R" } }],
    ["an empty string id", { data: { id: "" } }],
    ["a boolean id", { data: { id: true } }],
    ["a null id", { data: { id: null } }],
    ["an object id", { data: { id: { v: 1 } } }],
  ])("passes %s through untouched", (_name, value) => {
    expect(withGeneratedFileUrl(value)).toBe(value);
  });

  it("adds the editor url next to the camelCase id and keeps the other fields", () => {
    const value = {
      data: { id: 5, title: "Report.docx", parentId: 3, parentTitle: "My" },
      extra: 1,
    };

    expect(withGeneratedFileUrl(value)).toEqual({
      data: {
        id: 5,
        title: "Report.docx",
        parentId: 3,
        parentTitle: "My",
        url: "/doceditor?fileId=5",
      },
      extra: 1,
    });
    expect(value.data).not.toHaveProperty("url");
  });

  it("reads a PascalCase Data and Id", () => {
    expect(withGeneratedFileUrl({ Data: { Id: "7", Title: "T" } })).toEqual({
      Data: { Id: "7", Title: "T", url: "/doceditor?fileId=7" },
    });
  });

  it("reads Id inside a camelCase data", () => {
    expect(withGeneratedFileUrl({ data: { Id: 3 } })).toEqual({
      data: { Id: 3, url: "/doceditor?fileId=3" },
    });
  });

  it("links an id of zero", () => {
    expect(withGeneratedFileUrl({ data: { id: 0 } })).toEqual({
      data: { id: 0, url: "/doceditor?fileId=0" },
    });
  });

  it("writes the linked Data under the data key when data is present but not an object", () => {
    expect(withGeneratedFileUrl({ data: "x", Data: { id: 1 } })).toEqual({
      data: { id: 1, url: "/doceditor?fileId=1" },
      Data: { id: 1 },
    });
  });
});

describe("extractAttachmentRefIds", () => {
  it.each<[string, unknown]>([
    ["null", null],
    ["a string", "hello"],
    ["an array", [{ type: "file", mimeType: '{"ref":"a"}' }]],
    ["an object without content", {}],
    ["string content", { content: "hello" }],
    ["object content", { content: { type: "file" } }],
  ])("answers no ids for %s", (_name, message) => {
    expect(extractAttachmentRefIds(message)).toEqual([]);
  });

  it("collects the refs of file and image parts once each, in order", () => {
    const message = {
      role: "user",
      content: [
        null,
        "text",
        { type: "text", text: '{"ref":"x"}' },
        { type: "file", mimeType: '{"ref":"a","title":"A.docx","kind":"file"}' },
        { type: "image", image: '{"ref":"b","title":"B.png","kind":"image"}' },
        { type: "file", mimeType: '{"ref":"a"}' },
        { type: "file", mimeType: "application/pdf" },
        { type: "file", mimeType: "{not json" },
        { type: "file", mimeType: '{"ref":5}' },
        { type: "file", mimeType: "" },
        { type: "file", mimeType: 7 },
        { type: "file" },
        { type: "image", image: '["c"]' },
        { type: "image", image: '{"title":"no ref"}' },
        { type: "image", image: "data:image/png;base64,AAAA" },
        { type: "image", image: '{"ref":"c"}' },
        { type: "file", mimeType: '{"ref":""}' },
      ],
    };

    expect(extractAttachmentRefIds(message)).toEqual(["a", "b", "c"]);
  });
});

describe("HttpToolsAdapter.getTools", () => {
  it("posts the empty context and splits the tools into the silent and the approval group", async () => {
    answerList();

    const grouped = await new HttpToolsAdapter().getTools();

    expect(grouped).toEqual({
      [SILENT]: [
        {
          name: "get_folder",
          description: "Read a folder",
          inputSchema: FOLDER_SCHEMA,
          enabled: true,
        },
      ],
      [APPROVAL]: [
        {
          name: "onlyoffice_generate_docx",
          description: `Create a document ${LINK_HINT}`,
          inputSchema: DOCX_SCHEMA,
          enabled: true,
        },
      ],
    });
    expect(upstream.seen).toEqual([
      expect.objectContaining({
        method: "POST",
        path: "/internal/ai/tools/list",
        body: EMPTY_CONTEXT,
      }),
    ]);
    expect(DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE).toBe(APPROVAL);
  });

  it("answers only the silent group when no approval tool is listed", async () => {
    answerList({ prompt: "", tools: [TOOL_LIST.tools[0]] });

    expect(Object.keys(await new HttpToolsAdapter().getTools("1"))).toEqual([SILENT]);
  });

  it.each([
    "onlyoffice_generate_docx",
    "onlyoffice_generate_presentation",
    "onlyoffice_generate_form",
  ])("puts %s alone into the approval group", async (name) => {
    answerList({ prompt: "", tools: [{ name, description: "", parameters: {} }] });

    expect(await new HttpToolsAdapter().getTools()).toEqual({
      [APPROVAL]: [{ name, description: LINK_HINT, inputSchema: {}, enabled: true }],
    });
  });

  it("answers an empty map when the list is empty", async () => {
    answerList({ prompt: "p", tools: [] });

    expect(await new HttpToolsAdapter().getTools()).toEqual({});
  });

  it("leaves the generate tools out when a document agent is configured", async () => {
    state.documentAgent = true;
    answerList();

    expect(await new HttpToolsAdapter().getTools()).toEqual({
      [SILENT]: [
        {
          name: "get_folder",
          description: "Read a folder",
          inputSchema: FOLDER_SCHEMA,
          enabled: true,
        },
      ],
    });
  });

  it("answers an empty map when a document agent removes the only tools", async () => {
    state.documentAgent = true;
    answerList({ prompt: "", tools: [TOOL_LIST.tools[1]] });

    expect(await new HttpToolsAdapter().getTools()).toEqual({});
  });

  it.each<[string | undefined, number]>([
    [undefined, 0],
    ["", 0],
    ["123", 123],
    ["12abc", 12],
    ["abc", 0],
    ["-4", -4],
  ])("sends entityId %j as folderId %i", async (entityId, folderId) => {
    answerList();

    await new HttpToolsAdapter().getTools(entityId);

    expect(upstream.seen[0]?.body).toEqual({ ...EMPTY_CONTEXT, folderId });
  });

  it("ignores the engine's attachment list and sends the form resolved earlier in the request", async () => {
    answerList();

    await inRequestContext({}, async () => {
      setResolvedFormId(55);
      setResolvedAttachmentId("att-1");
      setKnowledgeRoomIds([7, 9]);
      await new HttpToolsAdapter().getTools("3", { attachmentId: ["ignored"] });
    });

    expect(upstream.seen).toHaveLength(1);
    expect(upstream.seen[0]?.body).toEqual({
      folderId: 3,
      formId: 55,
      attachmentId: "att-1",
      formSubAgent: false,
      knowledgeRoomId: 7,
      knowledgeRoomIds: [7, 9],
    });
  });

  it("flags the request of the form sub-agent", async () => {
    answerList();

    await new HttpToolsAdapter(true).getTools();

    expect(upstream.seen[0]?.body).toEqual({ ...EMPTY_CONTEXT, formSubAgent: true });
  });

  it("logs the context and the tool names, not the answer itself", async () => {
    answerList();

    await new HttpToolsAdapter().getTools("5");

    expect(vi.mocked(logger.info).mock.calls.map((c) => c[0])).toEqual([
      `docspaceTools.list entityId=5 -> /tools/list context=${JSON.stringify({ ...EMPTY_CONTEXT, folderId: 5 })}`,
      expect.stringMatching(
        /^docspaceTools\.list entityId=5 -> 2 tool\(s\) in \d+ms: \[get_folder, onlyoffice_generate_docx\]$/,
      ),
    ]);
  });

  it("logs <none> and - for an empty list without an entity", async () => {
    answerList({ prompt: "", tools: [] });

    await new HttpToolsAdapter().getTools();

    expect(logger.info).toHaveBeenLastCalledWith(
      expect.stringMatching(/^docspaceTools\.list entityId=- -> 0 tool\(s\) in \d+ms: \[<none>\]$/),
    );
  });

  it.each([400, 403, 500, 503])("rejects with the AiServiceHttpError of a %i", async (status) => {
    upstream.use(http.post("*/internal/ai/tools/list", () => docspaceError(status)));

    const error = await new HttpToolsAdapter().getTools("9").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(AiServiceHttpError);
    expect((error as AiServiceHttpError).status).toBe(status);
    expect(logger.error).toHaveBeenCalledWith(
      expect.stringMatching(
        new RegExp(
          `^docspaceTools\\.list entityId=9 request failed after \\d+ms: AI service ${status} `,
        ),
      ),
    );
  });

  it("logs a non-Error rejection as text and rethrows it", async () => {
    vi.spyOn(aiService, "post").mockRejectedValueOnce("down");

    await expect(new HttpToolsAdapter().getTools()).rejects.toBe("down");
    expect(logger.error).toHaveBeenCalledWith(
      expect.stringMatching(/^docspaceTools\.list entityId=- request failed after \d+ms: down$/),
    );
  });
});

describe("the tools/list answer (ToolListResponse)", () => {
  const tool = (over: Record<string, unknown>) => ({
    name: "t",
    description: "d",
    parameters: { type: "object", description: "schema d" },
    ...over,
  });
  const item = (over: Record<string, unknown> = {}) => ({
    name: "t",
    description: "d",
    inputSchema: { type: "object", description: "schema d" },
    enabled: true,
    ...over,
  });

  // ToolListResponse carries `tools`: an answer without the list is unknown, not "no tools".
  it.each<[string, () => Response]>([
    ["a null response", () => docspace(null)],
    ["a text answer", () => HttpResponse.text("hello")],
    ["an array", () => docspace([TOOL_LIST])],
    ["an empty object", () => docspace({})],
    ["tools missing", () => docspace({ prompt: "p" })],
    ["tools null", () => docspace({ prompt: "p", tools: null })],
    ["tools a string", () => docspace({ prompt: "p", tools: "x" })],
    ["tools an object", () => docspace({ prompt: "p", tools: {} })],
  ])("throws UpstreamShapeError for %s", async (_name, answer) => {
    upstream.use(http.post("*/internal/ai/tools/list", answer));

    await expect(new HttpToolsAdapter().listToolset()).rejects.toEqual(
      new UpstreamShapeError("POST tools/list"),
    );
  });

  it.each<[string, () => Response, unknown]>([
    ["prompt missing", () => docspace({ tools: [] }), { tools: [], prompt: "" }],
    ["prompt null", () => docspace({ tools: [], prompt: null }), { tools: [], prompt: "" }],
    ["prompt a number", () => docspace({ tools: [], prompt: 5 }), { tools: [], prompt: "" }],
    ["prompt empty", () => docspace({ tools: [], prompt: "" }), { tools: [], prompt: "" }],
    [
      "PascalCase fields",
      () =>
        docspace({
          Prompt: "P",
          Tools: [{ Name: "T", Description: "D", Parameters: { type: "object" } }],
        }),
      {
        tools: [{ name: "T", description: "D", inputSchema: { type: "object" }, enabled: true }],
        prompt: "P",
      },
    ],
    [
      "camelCase winning over PascalCase",
      () =>
        docspace({
          prompt: "p",
          Prompt: "P",
          tools: [tool({ Name: "N", Description: "D2", Parameters: {} })],
          Tools: [tool({ name: "other" })],
        }),
      { tools: [item()], prompt: "p" },
    ],
    [
      "items that are not objects",
      () => docspace({ prompt: "", tools: [null, "t", 5, [], tool({})] }),
      { tools: [item()], prompt: "" },
    ],
    [
      "an item without a name",
      () => docspace({ tools: [tool({ name: undefined })] }),
      { tools: [], prompt: "" },
    ],
    [
      "an item with a null name",
      () => docspace({ tools: [tool({ name: null })] }),
      { tools: [], prompt: "" },
    ],
    [
      "an item with an empty name",
      () => docspace({ tools: [tool({ name: "" })] }),
      { tools: [], prompt: "" },
    ],
    [
      "an item with a numeric name",
      () => docspace({ tools: [tool({ name: 5 })] }),
      { tools: [], prompt: "" },
    ],
    [
      "an item with an empty name, its PascalCase Name not consulted",
      () => docspace({ tools: [tool({ name: "", Name: "N" })] }),
      { tools: [], prompt: "" },
    ],
    [
      "parameters missing",
      () => docspace({ tools: [tool({ parameters: undefined })] }),
      { tools: [item({ inputSchema: {} })], prompt: "" },
    ],
    [
      "parameters null",
      () => docspace({ tools: [tool({ parameters: null, description: undefined })] }),
      { tools: [item({ inputSchema: {}, description: "" })], prompt: "" },
    ],
    [
      "parameters a string",
      () => docspace({ tools: [tool({ parameters: "{}" })] }),
      { tools: [item({ inputSchema: {} })], prompt: "" },
    ],
    [
      "parameters an array",
      () => docspace({ tools: [tool({ parameters: [1] })] }),
      { tools: [item({ inputSchema: {} })], prompt: "" },
    ],
    [
      "description missing",
      () => docspace({ tools: [tool({ description: undefined })] }),
      { tools: [item({ description: "schema d" })], prompt: "" },
    ],
    [
      "description null",
      () => docspace({ tools: [tool({ description: null })] }),
      { tools: [item({ description: "schema d" })], prompt: "" },
    ],
    [
      "description a number",
      () => docspace({ tools: [tool({ description: 5, Description: "D" })] }),
      { tools: [item({ description: "D" })], prompt: "" },
    ],
    [
      "description empty",
      () => docspace({ tools: [tool({ description: "" })] }),
      { tools: [item({ description: "" })], prompt: "" },
    ],
    [
      "no description anywhere",
      () => docspace({ tools: [tool({ description: undefined, parameters: { description: 3 } })] }),
      { tools: [item({ description: "", inputSchema: { description: 3 } })], prompt: "" },
    ],
  ])("maps %s", async (_name, answer, expected) => {
    upstream.use(http.post("*/internal/ai/tools/list", answer));

    expect(await new HttpToolsAdapter().listToolset()).toEqual(expected);
  });

  it("answers the full list flat, without grouping, for the sub-agent", async () => {
    answerList();

    expect(await new HttpToolsAdapter(true).listToolset("8")).toEqual({
      prompt: "Use the DocSpace tools.",
      tools: [
        {
          name: "get_folder",
          description: "Read a folder",
          inputSchema: FOLDER_SCHEMA,
          enabled: true,
        },
        {
          name: "onlyoffice_generate_docx",
          description: `Create a document ${LINK_HINT}`,
          inputSchema: DOCX_SCHEMA,
          enabled: true,
        },
      ],
    });
    expect(upstream.seen[0]?.body).toEqual({ ...EMPTY_CONTEXT, folderId: 8, formSubAgent: true });
  });
});

describe("HttpToolsAdapter.getPrompt", () => {
  // AttachmentDto of products/ASC.AI (AttachmentsController read): `entryId` is the DocSpace file id.
  const attachment = (id: string, entryId?: string) => ({
    id,
    title: "Form.pdf",
    kind: "file",
    createdAt: 1,
    ...(entryId === undefined ? {} : { entryId }),
  });

  it("answers the prompt of the list", async () => {
    answerList();

    expect(await new HttpToolsAdapter().getPrompt("2")).toBe("Use the DocSpace tools.");
    expect(upstream.seen).toHaveLength(1);
  });

  it.each<[string, string[] | undefined]>([
    ["undefined", undefined],
    ["an empty list", []],
  ])("reads no attachment for %s", async (_name, refs) => {
    answerList();

    await new HttpToolsAdapter().getPrompt(undefined, refs);

    expect(upstream.seen.map((r) => r.path)).toEqual(["/internal/ai/tools/list"]);
  });

  it("resolves the first attachment with a positive numeric entry id and keeps it for the request", async () => {
    answerAttachments([
      attachment("r1", "abc"),
      attachment("r2"),
      attachment("r3", "0"),
      attachment("r4", "-3"),
      attachment("r5", "42"),
      attachment("r6", "43"),
    ]);
    answerList();
    answerCall([{ id: "c", name: "get_folder", result: "ok", error: null }]);

    await inRequestContext({}, async () => {
      const adapter = new HttpToolsAdapter();
      await adapter.getPrompt("1", ["r1", "r2", "missing", "r3", "r4", "r5", "r6"]);
      await adapter.callTool("get_folder", {}, "1");
    });

    expect(upstream.seen.map((r) => [r.path, r.body])).toEqual([
      ["/internal/ai/attachments/read", { ids: ["r1", "r2", "missing", "r3", "r4", "r5", "r6"] }],
      [
        "/internal/ai/tools/list",
        { ...EMPTY_CONTEXT, folderId: 1, formId: 42, attachmentId: "r5" },
      ],
      [
        "/internal/ai/tools/call",
        {
          ...EMPTY_CONTEXT,
          folderId: 1,
          formId: 42,
          attachmentId: "r5",
          calls: [{ id: expect.any(String), name: "get_folder", arguments: {} }],
        },
      ],
    ]);
  });

  it("sends no form and keeps the one resolved earlier untouched when no attachment is a file", async () => {
    answerAttachments([attachment("r1", "x")]);
    answerList();
    answerCall([{ result: "ok" }]);

    await inRequestContext({}, async () => {
      setResolvedFormId(55);
      setResolvedAttachmentId("old");
      const adapter = new HttpToolsAdapter();
      await adapter.getPrompt(undefined, ["r1"]);
      await adapter.callTool("t", {});
    });

    expect(upstream.seen[1]?.body).toEqual(EMPTY_CONTEXT);
    expect(upstream.seen[2]?.body).toMatchObject({ formId: 55, attachmentId: "old" });
  });

  it("fails the list and lists nothing when the attachments read answers 500", async () => {
    upstream.use(http.post("*/internal/ai/attachments/read", () => docspaceError(500, "boom")));
    answerList();

    const failure = await new HttpToolsAdapter().getPrompt("4", ["a", "b"]).catch((e) => e);

    expect(failure).toBeInstanceOf(AiServiceHttpError);
    expect(failure).toMatchObject({ status: 500 });
    expect(upstream.seen.map((r) => r.path)).toEqual(["/internal/ai/attachments/read"]);
  });

  it("fails the list with the read's own failure when it is not an HTTP answer", async () => {
    answerList();
    const { storage } = await import("../../storage/index.js");
    vi.spyOn(storage.attachments, "readManyByIds").mockRejectedValueOnce(
      new Error("upstream timeout after 60000ms"),
    );

    await expect(new HttpToolsAdapter().getPrompt(undefined, ["a"])).rejects.toThrow(
      "upstream timeout after 60000ms",
    );
    expect(upstream.seen).toEqual([]);
  });

  it("fails safeGetToolsPrompt with the read's own failure when the attachments read fails", async () => {
    upstream.use(http.post("*/internal/ai/attachments/read", () => docspaceError(503)));
    answerList();

    await expect(safeGetToolsPrompt(new HttpToolsAdapter(), "4", ["a"])).rejects.toMatchObject({
      status: 503,
    });
    expect(logger.warn).not.toHaveBeenCalled();
    expect(upstream.seen.map((r) => r.path)).toEqual(["/internal/ai/attachments/read"]);
  });
});

describe("safeGetToolsPrompt", () => {
  it("answers the prompt of the list", async () => {
    answerList();

    await expect(safeGetToolsPrompt(new HttpToolsAdapter(), "1")).resolves.toBe(
      "Use the DocSpace tools.",
    );
  });

  it("passes the attachment refs on", async () => {
    answerAttachments([attachment2("r", "9")]);
    answerList();

    await safeGetToolsPrompt(new HttpToolsAdapter(), undefined, ["r"]);

    expect(upstream.seen[1]?.body).toMatchObject({ formId: 9, attachmentId: "r" });
  });

  it("answers an empty prompt and warns when the list fails", async () => {
    upstream.use(http.post("*/internal/ai/tools/list", () => docspaceError(503)));

    await expect(safeGetToolsPrompt(new HttpToolsAdapter(), "1")).resolves.toBe("");
    expect(logger.warn).toHaveBeenCalledWith(
      expect.stringMatching(/^tools\/list prompt fetch failed: AI service 503 /),
    );
  });

  it("warns with the text of a non-Error failure", async () => {
    const adapter = new HttpToolsAdapter();
    vi.spyOn(adapter, "getPrompt").mockRejectedValueOnce("nope");

    await expect(safeGetToolsPrompt(adapter, undefined)).resolves.toBe("");
    expect(logger.warn).toHaveBeenCalledWith("tools/list prompt fetch failed: nope");
  });
});

function attachment2(id: string, entryId: string) {
  return { id, title: "F.pdf", kind: "file", createdAt: 1, entryId };
}

describe("HttpToolsAdapter.callTool", () => {
  const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;

  it("posts the context and one call with a fresh id, and answers a string result as it is", async () => {
    answerCall([{ id: "x", name: "get_folder", result: "folder text", error: null }]);

    const result = await new HttpToolsAdapter().callTool(
      "get_folder",
      { folderId: 1, deep: true },
      "6",
    );

    expect(result).toBe("folder text");
    expect(upstream.seen).toHaveLength(1);
    expect(upstream.seen[0]).toMatchObject({ method: "POST", path: "/internal/ai/tools/call" });
    expect(upstream.seen[0]?.body).toEqual({
      ...EMPTY_CONTEXT,
      folderId: 6,
      calls: [
        {
          id: expect.stringMatching(UUID),
          name: "get_folder",
          arguments: { folderId: 1, deep: true },
        },
      ],
    });
    expect(vi.mocked(logger.info).mock.calls.map((c) => c[0])).toEqual([
      "docspaceTools.callTool name=get_folder args=[folderId,deep] entityId=6 -> /tools/call",
      expect.stringMatching(
        /^docspaceTools\.callTool name=get_folder ok in \d+ms \(resultLength=11\)$/,
      ),
    ]);
  });

  it("sends the context of the request and the sub-agent flag", async () => {
    answerCall([{ result: "ok" }]);

    await inRequestContext({}, async () => {
      setResolvedFormId(12);
      setResolvedAttachmentId("att");
      setKnowledgeRoomIds([4]);
      await new HttpToolsAdapter(true).callTool("t", {});
    });

    expect(upstream.seen[0]?.body).toMatchObject({
      folderId: 0,
      formId: 12,
      attachmentId: "att",
      formSubAgent: true,
      knowledgeRoomId: 4,
      knowledgeRoomIds: [4],
    });
  });

  it.each<[string, unknown, string]>([
    ["an object result as JSON", [{ result: { a: 1 } }], '{"a":1}'],
    ["an array result as JSON", [{ result: [1, 2] }], "[1,2]"],
    ["a number result as JSON", [{ result: 5 }], "5"],
    ["a null result as null", [{ result: null }], "null"],
    ["a missing result as null", [{ id: "x" }], "null"],
    ["a PascalCase Result", [{ Result: "R" }], "R"],
    ["result winning over Result", [{ result: "r", Result: "R" }], "r"],
    [
      "the first object of the list",
      [null, "x", { result: "second" }, { result: "third" }],
      "second",
    ],
    ["an error string", [{ error: "boom", result: "r" }], 'Tool "t" failed: boom'],
    ["a PascalCase Error", [{ Error: "BOOM" }], 'Tool "t" failed: BOOM'],
    [
      "an empty error as a failure",
      [{ error: "", result: "r" }],
      'Tool "t" failed: the tool service reported an error without a description',
    ],
    [
      "a non-string error as a failure",
      [{ error: 5, result: "r" }],
      'Tool "t" failed: the tool service reported an error without a description',
    ],
    ["a null error with an Error", [{ error: null, Error: "E" }], 'Tool "t" failed: E'],
    // A call answered without a ToolCallResult is unknown: the model is told it failed.
    [
      "an empty list as a failure",
      [],
      'Tool "t" failed: the tool service answered in an unexpected shape',
    ],
    [
      "a list without objects as a failure",
      [null, "x"],
      'Tool "t" failed: the tool service answered in an unexpected shape',
    ],
    [
      "an object answer as a failure",
      { result: "r" },
      'Tool "t" failed: the tool service answered in an unexpected shape',
    ],
    [
      "a null answer as a failure",
      null,
      'Tool "t" failed: the tool service answered in an unexpected shape',
    ],
    [
      "a string answer as a failure",
      "x",
      'Tool "t" failed: the tool service answered in an unexpected shape',
    ],
  ])("answers %s", async (_name, answer, expected) => {
    answerCall(answer);

    await expect(new HttpToolsAdapter().callTool("t", {})).resolves.toBe(expected);
  });

  it("warns about a failed call and about a call without a result", async () => {
    answerCall([{ error: "boom" }]);
    await new HttpToolsAdapter().callTool("t", {});
    answerCall([]);
    await new HttpToolsAdapter().callTool("u", {});

    expect(vi.mocked(logger.warn).mock.calls.map((c) => c[0])).toEqual([
      expect.stringMatching(/^docspaceTools\.callTool name=t failed after \d+ms: boom$/),
      expect.stringMatching(/^docspaceTools\.callTool name=u no-result after \d+ms$/),
    ]);
  });

  it("keeps the last line of a tool's error in the log, cut, and hands the model the whole text", async () => {
    const error = `the request was { "prompt": "the user's text" }\n${"e".repeat(400)}`;
    answerCall([{ error }]);

    await expect(new HttpToolsAdapter().callTool("t", {})).resolves.toBe(
      `Tool "t" failed: ${error}`,
    );

    const line = String(vi.mocked(logger.warn).mock.calls[0]?.[0]);
    expect(line).toMatch(/^docspaceTools\.callTool name=t failed after \d+ms: e{300}$/);
    expect(line).not.toContain("the user's text");
  });

  it("adds the editor link to the result of a generate tool", async () => {
    answerCall([
      { result: { data: { id: 9, title: "R.docx", parentId: 1, parentTitle: "My" } }, error: null },
    ]);

    const text = await new HttpToolsAdapter().callTool("onlyoffice_generate_docx", { title: "R" });

    expect(JSON.parse(text as string)).toEqual({
      data: { id: 9, title: "R.docx", parentId: 1, parentTitle: "My", url: "/doceditor?fileId=9" },
    });
  });

  it("leaves the result of another tool without a link", async () => {
    answerCall([{ result: { data: { id: 9 } } }]);

    await expect(new HttpToolsAdapter().callTool("get_file", {})).resolves.toBe(
      '{"data":{"id":9}}',
    );
  });

  it("answers a generate tool's string result as it is", async () => {
    answerCall([{ result: "created" }]);

    await expect(new HttpToolsAdapter().callTool("onlyoffice_generate_form", {})).resolves.toBe(
      "created",
    );
  });

  it.each([400, 500, 504])(
    "rethrows the AiServiceHttpError of a %i and logs it",
    async (status) => {
      upstream.use(http.post("*/internal/ai/tools/call", () => docspaceError(status)));

      const error = await new HttpToolsAdapter().callTool("t", {}).catch((e: unknown) => e);

      expect((error as AiServiceHttpError).status).toBe(status);
      expect(logger.error).toHaveBeenCalledWith(
        expect.stringMatching(
          new RegExp(`^docspaceTools\\.callTool name=t threw after \\d+ms: AI service ${status} `),
        ),
      );
    },
  );

  it("logs a non-Error rejection as text", async () => {
    vi.spyOn(aiService, "post").mockRejectedValueOnce("down");

    await expect(new HttpToolsAdapter().callTool("t", { a: 1 })).rejects.toBe("down");
    expect(logger.error).toHaveBeenCalledWith(
      expect.stringMatching(/^docspaceTools\.callTool name=t threw after \d+ms: down$/),
    );
    expect(logger.info).toHaveBeenCalledWith(
      "docspaceTools.callTool name=t args=[a] entityId=- -> /tools/call",
    );
  });
});
