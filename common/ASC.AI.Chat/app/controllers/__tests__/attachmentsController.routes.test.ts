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

import type { AttachmentsEngine } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import { inRequestContext, invoke } from "../../__tests__/helpers/http.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";

// Characterization of every route of attachmentsController: the engine class,
// the storage it reads and the suggested-questions generator are stubbed; the
// request context (`setAnalyzeEntryIds`) is the real one.

const engine = engineMock<AttachmentsEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  AttachmentsEngine: engineClass(() => engine),
}));
const store = vi.hoisted(() => ({
  readThreadId: vi.fn(),
  findMissingIds: vi.fn(),
  getSuggestedQuestions: vi.fn(),
}));
// The access gate (aiAccess.ts) reads the tool prefs; a stand-in that answers lets the route go on.
const toolPrefs = vi.hoisted(() => ({ readDisabled: vi.fn() }));
vi.mock("../../storage/index.js", () => ({
  storage: {
    messages: { readThreadId: store.readThreadId },
    attachments: { findMissingIds: store.findMissingIds },
    toolPrefs,
  },
}));
vi.mock("../../forms/suggestedQuestions.js", () => ({
  getSuggestedQuestions: store.getSuggestedQuestions,
}));

const { attachmentsController } = await import("../attachmentsController.js");
const { getAnalyzeEntryIds } = await import("../../requestContext.js");
const { AiServiceHttpError } = await import("../../storage/httpClient.js");

// An attachment as the engine answers it, the shape HttpAttachmentsStorage maps from the AI
// service's attachment DTO (products/ASC.AI/Server/Models/ResponseDto/AttachmentDto.cs).
const attachment = {
  id: "55555555-5555-5555-5555-555555555555",
  kind: "file" as const,
  source: "user" as const,
  title: "contract.docx",
  content: "This agreement is made on 1 January 2026",
  path: "1234",
  type: 65,
  entityId: "42",
  createdAt: 1767225600000,
  canAnalyze: false,
  formKeys: [],
};

const input = { path: "1234", content: "text", type: 65, title: "contract.docx" };

beforeEach(() => {
  mockReset(engine);
  store.readThreadId.mockReset();
  store.findMissingIds.mockReset();
  store.getSuggestedQuestions.mockReset();
});

// Bad `input` objects shared by save-file and each element of save-files-many.
const badInputs: Array<[string, unknown, string]> = [
  ["null", null, "input is required and must be an object"],
  ["a string", "1234", "input is required and must be an object"],
  ["an array", [input], "input is required and must be an object"],
  ["a number", 5, "input is required and must be an object"],
  ["{}", {}, "input.path is required and must be a non-empty string"],
  [
    "without path",
    { content: "", type: 65 },
    "input.path is required and must be a non-empty string",
  ],
  [
    "with path null",
    { ...input, path: null },
    "input.path is required and must be a non-empty string",
  ],
  [
    "with path a number",
    { ...input, path: 1234 },
    "input.path is required and must be a non-empty string",
  ],
  [
    "with path empty",
    { ...input, path: "" },
    "input.path is required and must be a non-empty string",
  ],
  [
    "with path blank",
    { ...input, path: "   " },
    "input.path is required and must be a non-empty string",
  ],
  ["without content", { path: "1", type: 65 }, "input.content is required and must be a string"],
  [
    "with content null",
    { ...input, content: null },
    "input.content is required and must be a string",
  ],
  [
    "with content a number",
    { ...input, content: 5 },
    "input.content is required and must be a string",
  ],
  [
    "with content an object",
    { ...input, content: {} },
    "input.content is required and must be a string",
  ],
  [
    "without type",
    { path: "1", content: "" },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  [
    "with type null",
    { ...input, type: null },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  [
    "with type empty",
    { ...input, type: "" },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  [
    "with type a boolean",
    { ...input, type: true },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  [
    "with type not numeric",
    { ...input, type: "docx" },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  [
    "with type an object",
    { ...input, type: { v: 1 } },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  [
    'with type "65abc", not its leading digits',
    { ...input, type: "65abc" },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  [
    'with type "1.5", a string that is no integer',
    { ...input, type: "1.5" },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  [
    'with type "1e3", in exponent notation',
    { ...input, type: "1e3" },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  [
    'with type " 65", padded',
    { ...input, type: " 65" },
    "input.type is required and must be a number (ONLYOFFICE file type code)",
  ],
  ["with title null", { ...input, title: null }, "input.title must be a string when present"],
  ["with title a number", { ...input, title: 5 }, "input.title must be a string when present"],
  [
    "with an archive title",
    { ...input, title: "backup.ZIP" },
    "archive attachments are not supported: backup.ZIP",
  ],
  [
    "with an archive title of another kind",
    { ...input, title: "logs.tar.gz" },
    "archive attachments are not supported: logs.tar.gz",
  ],
  [
    "with an archive path and no title",
    { path: "dir/sub/a.7z", content: "", type: 1 },
    "archive attachments are not supported: a.7z",
  ],
  [
    "with an archive path behind a backslash and an empty title",
    { path: "C:\\x\\b.rar", content: "", type: 1, title: "" },
    "archive attachments are not supported: b.rar",
  ],
];

describe("attachmentsController.saveFile", () => {
  const handler = attachmentsController.saveFile;

  it.each([
    ["the positional array the widget sends", [input, "42"]],
    ["named fields", { input, entityId: "42" }],
  ])("saves the input given as %s and answers the engine's attachment", async (_what, body) => {
    engine.saveFile.mockResolvedValue(attachment);

    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(attachment);
    expect(engine.saveFile).toHaveBeenCalledWith(input, "42");
    expect(responseViolations("aiAttachmentsSaveFile", 200, reply.body)).toEqual([]);
  });

  it.each([
    ["a numeric string type, made a number", { ...input, type: "65" }, { ...input, type: 65 }],
    // CONTRACT data-21: a fractional number is kept as it is. Pinned by ASC.AI.Tests (AttachmentSaveFileTypeTests.SaveFile_AnyNumericType_IsAcceptedAndEchoedLikeAnInEnumOne echoes 1.5 back).
    ["a fractional type, kept as it is", { ...input, type: 65.5 }, { ...input, type: 65.5 }],
    ["an empty content", { ...input, content: "" }, { ...input, content: "" }],
    ["an empty title, kept", { ...input, title: "" }, { ...input, title: "" }],
    [
      "no title",
      { path: "1234", content: "t", type: 65 },
      { path: "1234", content: "t", type: 65 },
    ],
    ["extra fields, dropped", { ...input, analyzeOnly: true, base64: "x" }, input],
    [
      "an archive-looking path under a real title",
      { ...input, path: "a.zip" },
      { ...input, path: "a.zip" },
    ],
  ])("passes the engine %s", async (_what, given, passed) => {
    engine.saveFile.mockResolvedValue(attachment);

    const reply = await invoke(handler, { body: [given] });

    expect(reply.status).toBe(200);
    expect(engine.saveFile).toHaveBeenCalledWith(passed, undefined);
  });

  it.each([
    ["absent", undefined],
    ["a string", "1234"],
    ["an empty array", []],
    ["a number", 5],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "input is required and must be an object" });
    expect(engine.saveFile).not.toHaveBeenCalled();
  });

  it.each(badInputs)("answers 400 for an input that is %s", async (_what, bad, error) => {
    const reply = await invoke(handler, { body: { input: bad, entityId: "42" } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(engine.saveFile).not.toHaveBeenCalled();
    expect(responseViolations("aiAttachmentsSaveFile", 400, reply.body)).toEqual([]);
  });

  it.each([
    ["a number", 42],
    ["an object", { id: "42" }],
  ])("answers 400 for an entityId that is %s, before the engine", async (_what, entityId) => {
    const reply = await invoke(handler, { body: [input, entityId] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "entityId must be a string" });
    expect(responseViolations("aiAttachmentsSaveFile", 400, reply.body)).toEqual([]);
    expect(engine.saveFile).not.toHaveBeenCalled();
  });

  it("takes a null entityId, as the widget sends an omitted argument, for none", async () => {
    engine.saveFile.mockResolvedValue(attachment);

    const reply = await invoke(handler, { body: [input, null] });

    expect(reply.status).toBe(200);
    expect(engine.saveFile).toHaveBeenCalledWith(input, undefined);
  });

  // HANDLED DOWNSTREAM ctl-28: the engine's attachment is sent as it came (`res.json(result)`), so a field the storage adds leaves in the answer and breaks AiAttachment's additionalProperties:false. In production the storage builds every attachment from listed fields (dtoToAttachment in storage/attachmentsStorage.ts), so no other field can come from it; it shows only with the engine or the service stubbed.
  it("echoes a field the engine's attachment carries beyond the published ones", async () => {
    engine.saveFile.mockResolvedValue({ ...attachment, internalUrl: "http://storage/x" } as never);

    const reply = await invoke(handler, { body: [input] });

    expect(reply.body).toEqual({ ...attachment, internalUrl: "http://storage/x" });
    expect(responseViolations("aiAttachmentsSaveFile", 200, reply.body)).toEqual([
      "/ must NOT have additional properties",
    ]);
  });

  it("relays an AI service failure with its status and reason phrase only", async () => {
    engine.saveFile.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://ai/x"),
    );

    const reply = await invoke(handler, { body: [input] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations("aiAttachmentsSaveFile", 500, reply.body)).toEqual([]);
  });

  it("answers 500 when the engine throws a plain Error", async () => {
    engine.saveFile.mockRejectedValue(new Error("draft without a path"));

    const reply = await invoke(handler, { body: [input] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});

describe("attachmentsController.saveFilesMany", () => {
  const handler = attachmentsController.saveFilesMany;

  it("saves the inputs and marks the analysis subjects in the request context", async () => {
    engine.saveFilesMany.mockResolvedValue([attachment]);
    const inputs = [
      { ...input, path: "1", analyzeOnly: true },
      { ...input, path: "2", analyzeOnly: "true" },
      { ...input, path: "3" },
      { ...input, path: "4", analyzeOnly: true },
    ];

    const { reply, marked } = await inRequestContext({}, async () => {
      const r = await invoke(handler, { body: [inputs, "42"] });
      return { reply: r, marked: getAnalyzeEntryIds() };
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([attachment]);
    expect(marked).toEqual(["1", "4"]);
    expect(engine.saveFilesMany).toHaveBeenCalledWith(
      [1, 2, 3, 4].map((n) => ({ ...input, path: String(n) })),
      "42",
    );
    expect(responseViolations("aiAttachmentsSaveFilesMany", 200, reply.body)).toEqual([]);
  });

  it("saves an explicit empty list as an empty batch", async () => {
    engine.saveFilesMany.mockResolvedValue([]);

    const { reply, marked } = await inRequestContext({}, async () => {
      const r = await invoke(handler, { body: { inputs: [] } });
      return { reply: r, marked: getAnalyzeEntryIds() };
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([]);
    expect(marked).toEqual([]);
    expect(engine.saveFilesMany).toHaveBeenCalledWith([], undefined);
  });

  it.each([
    ["absent", undefined],
    ["null", null],
    ["a string", "inputs"],
    ["{}", {}],
    ["with inputs null", { inputs: null }],
    ["with inputs an object", { inputs: input }],
    ["with inputs a string", { inputs: "1234" }],
    ["a positional array whose first item is not a list", [input, "42"]],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "inputs must be an array" });
    expect(engine.saveFilesMany).not.toHaveBeenCalled();
    expect(responseViolations("aiAttachmentsSaveFilesMany", 400, reply.body)).toEqual([]);
  });

  it("answers 400 for 101 inputs before reading any of them", async () => {
    const list = Array.from({ length: 101 }, () => null);

    const reply = await invoke(handler, { body: [list] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "inputs may name at most 100 files (got 101)" });
    expect(engine.saveFilesMany).not.toHaveBeenCalled();
  });

  it.each(badInputs)(
    "answers 400 naming the index for an element that is %s",
    async (_what, bad, error) => {
      const reply = await invoke(handler, { body: [[input, input, bad]] });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: `inputs[2]: ${error}` });
      expect(engine.saveFilesMany).not.toHaveBeenCalled();
      expect(responseViolations("aiAttachmentsSaveFilesMany", 400, reply.body)).toEqual([]);
    },
  );

  it("does not mark the analysis subjects when an element is refused", async () => {
    const marked = await inRequestContext({}, async () => {
      await invoke(handler, { body: [[{ ...input, analyzeOnly: true }, null]] });
      return getAnalyzeEntryIds();
    });

    expect(marked).toEqual([]);
  });

  it("answers 400 for an entityId that is a number, before the engine", async () => {
    const reply = await invoke(handler, { body: [[input], 42] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "entityId must be a string" });
    expect(engine.saveFilesMany).not.toHaveBeenCalled();
  });

  it("answers 500 when the engine fails", async () => {
    engine.saveFilesMany.mockRejectedValue(new Error("down"));

    const reply = await invoke(handler, { body: [[input]] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiAttachmentsSaveFilesMany", 500, reply.body)).toEqual([]);
  });
});

// The single-id routes: the widget sends the bare id as the body.
const idRoutes = [
  ["get", "aiAttachmentsGet"],
  ["getSuggestedQuestions", "aiAttachmentsGetSuggestedQuestions"],
  ["delete", "aiAttachmentsDelete"],
] as const;

describe.each(idRoutes)("attachmentsController.%s id validation", (route, operationId) => {
  const handler = attachmentsController[route];

  it.each([
    ["absent", undefined],
    ["null", null],
    ["empty", ""],
    ["a number", 5],
    ["an array", ["a1"]],
    // `{}` lacks the key, so unpackPositional takes the whole object as the id.
    ["{}", {}],
    ["a named null", { id: null }],
    ["a named number", { id: 5 }],
    ["a named empty string", { id: "" }],
  ])("answers 400 when the id is %s", async (_what, body) => {
    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "id is required" });
    expect(engine.get).not.toHaveBeenCalled();
    expect(engine.delete).not.toHaveBeenCalled();
    expect(store.getSuggestedQuestions).not.toHaveBeenCalled();
    expect(responseViolations(operationId, 400, reply.body)).toEqual([]);
  });
});

describe("attachmentsController.get", () => {
  const handler = attachmentsController.get;

  it.each([
    ["the bare id", "a1"],
    ["a named id", { id: "a1" }],
  ])("answers the attachment read by %s", async (_what, body) => {
    engine.get.mockResolvedValue(attachment);

    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(attachment);
    expect(engine.get).toHaveBeenCalledWith("a1");
    expect(responseViolations("aiAttachmentsGet", 200, reply.body)).toEqual([]);
  });

  it("answers null with 200 for an attachment the engine does not find", async () => {
    engine.get.mockResolvedValue(null);

    const reply = await invoke(handler, { body: "a1" });

    expect(reply.status).toBe(200);
    expect(reply.body).toBeNull();
    expect(responseViolations("aiAttachmentsGet", 200, reply.body)).toEqual([]);
  });

  it("relays an AI service failure with its status and reason phrase only", async () => {
    engine.get.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://ai/x"),
    );

    const reply = await invoke(handler, { body: "a1" });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations("aiAttachmentsGet", 500, reply.body)).toEqual([]);
  });
});

describe("attachmentsController.getMany", () => {
  const handler = attachmentsController.getMany;

  it.each([
    ["the bare list", ["a1", "a2"]],
    ["a named list", { ids: ["a1", "a2"] }],
  ])("answers the engine's list for %s", async (_what, body) => {
    engine.getMany.mockResolvedValue([attachment, null]);

    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual([attachment, null]);
    expect(engine.getMany).toHaveBeenCalledWith(["a1", "a2"]);
    expect(responseViolations("aiAttachmentsGetMany", 200, reply.body)).toEqual([]);
  });

  it("reads exactly 100 ids", async () => {
    engine.getMany.mockResolvedValue([]);
    const ids = Array.from({ length: 100 }, (_, i) => `a${i}`);

    const reply = await invoke(handler, { body: ids });

    expect(reply.status).toBe(200);
    expect(engine.getMany).toHaveBeenCalledWith(ids);
  });

  it.each([
    ["absent", undefined],
    ["null", null],
    ["a string", "a1"],
    ["an empty list", []],
    ["{}", {}],
    ["a named null", { ids: null }],
    ["a named string", { ids: "a1" }],
    ["a named empty list", { ids: [] }],
  ])("answers 400 for a body that is %s", async (_what, body) => {
    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "ids is required and must be a non-empty array" });
    expect(engine.getMany).not.toHaveBeenCalled();
    expect(responseViolations("aiAttachmentsGetMany", 400, reply.body)).toEqual([]);
  });

  it("answers 400 for 101 ids", async () => {
    const reply = await invoke(handler, { body: Array.from({ length: 101 }, (_, i) => `a${i}`) });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "ids may name at most 100 attachments (got 101)" });
    expect(engine.getMany).not.toHaveBeenCalled();
  });

  it.each([
    ["a number", ["a1", 5]],
    ["null", ["a1", null]],
    ["an object", [{ id: "a1" }]],
    ["an empty string", ["a1", ""]],
  ])("answers 400 for ids holding %s, before the engine", async (_what, ids) => {
    const reply = await invoke(handler, { body: { ids } });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "ids must hold non-empty strings only" });
    expect(engine.getMany).not.toHaveBeenCalled();
  });

  it("answers 500 when the engine fails", async () => {
    engine.getMany.mockRejectedValue(new Error("down"));

    const reply = await invoke(handler, { body: ["a1"] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });
});

describe("attachmentsController.getSuggestedQuestions", () => {
  const handler = attachmentsController.getSuggestedQuestions;

  it.each([
    [
      "ready",
      { status: "ready", questions: [{ question: "Which region?", prompt: "Group by region." }] },
    ],
    ["pending", { status: "pending", questions: [] }],
    ["unavailable", { status: "unavailable", questions: [] }],
  ])("answers the %s result for the attachment id", async (_what, result) => {
    store.getSuggestedQuestions.mockResolvedValue(result);

    const reply = await invoke(handler, { body: "a1" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(result);
    expect(store.getSuggestedQuestions).toHaveBeenCalledWith("a1");
    expect(responseViolations("aiAttachmentsGetSuggestedQuestions", 200, reply.body)).toEqual([]);
  });

  it("answers 500 when the generator fails", async () => {
    store.getSuggestedQuestions.mockRejectedValue(new Error("model down"));

    const reply = await invoke(handler, { body: { id: "a1" } });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiAttachmentsGetSuggestedQuestions", 500, reply.body)).toEqual([]);
  });
});

describe("attachmentsController.delete", () => {
  const handler = attachmentsController.delete;

  it("deletes by a named id", async () => {
    engine.delete.mockResolvedValue(undefined);

    const reply = await invoke(handler, { body: { id: "a1" } });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.delete).toHaveBeenCalledWith("a1");
    expect(responseViolations("aiAttachmentsDelete", 200, reply.body)).toEqual([]);
  });

  it("relays an AI service 403 with its reason phrase only", async () => {
    engine.delete.mockRejectedValue(
      new AiServiceHttpError(403, "Forbidden", "MARKER", "http://ai/x"),
    );

    const reply = await invoke(handler, { body: "a1" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiAttachmentsDelete", 403, reply.body)).toEqual([]);
  });
});

describe("attachmentsController.deleteMany", () => {
  const handler = attachmentsController.deleteMany;

  it.each([
    ["the bare list", ["a1", "a2"], ["a1", "a2"]],
    ["a named list", { ids: ["a1"] }, ["a1"]],
    ["absent, as an empty list", undefined, []],
    ["null, as an empty list", null, []],
    ["a named null, as an empty list", { ids: null }, []],
  ])("deletes %s", async (_what, body, ids) => {
    engine.deleteMany.mockResolvedValue(undefined);

    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.deleteMany).toHaveBeenCalledWith(ids);
    expect(responseViolations("aiAttachmentsDeleteMany", 200, reply.body)).toEqual([]);
  });

  it.each([
    ["a bare string", "a1", "ids must be an array"],
    ["a number", 5, "ids must be an array"],
    ["{}", {}, "ids must be an array"],
    ["non-string elements", [5, null], "ids must hold non-empty strings only"],
    ["an empty string among the ids", ["a1", ""], "ids must hold non-empty strings only"],
  ])("answers 400 for a body that is %s, before the engine", async (_what, body, error) => {
    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error });
    expect(responseViolations("aiAttachmentsDeleteMany", 400, reply.body)).toEqual([]);
    expect(engine.deleteMany).not.toHaveBeenCalled();
  });

  it("deletes 100 ids in one call and answers 400 for 101", async () => {
    engine.deleteMany.mockResolvedValue(undefined);
    const ids = Array.from({ length: 101 }, (_, i) => `a${i}`);

    const most = await invoke(handler, { body: ids.slice(0, 100) });
    const over = await invoke(handler, { body: ids });

    expect(most.status).toBe(200);
    expect(over.status).toBe(400);
    expect(over.body).toEqual({ error: "ids may name at most 100 attachments (got 101)" });
    expect(engine.deleteMany).toHaveBeenCalledTimes(1);
  });

  it("answers 500 when the engine fails", async () => {
    engine.deleteMany.mockRejectedValue(new Error("down"));

    const reply = await invoke(handler, { body: ["a1"] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiAttachmentsDeleteMany", 500, reply.body)).toEqual([]);
  });
});

describe("attachmentsController.linkToMessage", () => {
  const handler = attachmentsController.linkToMessage;
  const shapeError = "ids (non-empty array), messageId (string) and threadId (string) are required";

  beforeEach(() => {
    store.readThreadId.mockResolvedValue("t1");
    store.findMissingIds.mockResolvedValue([]);
    engine.linkToMessage.mockResolvedValue(undefined);
  });

  it.each([
    ["the positional array", [["a1", "a2"], "m1", "t1"]],
    ["named fields", { ids: ["a1", "a2"], messageId: "m1", threadId: "t1" }],
  ])(
    "binds the attachments given as %s after checking the message and the records",
    async (_what, body) => {
      const reply = await invoke(handler, { body });

      expect(reply.status).toBe(200);
      expect(reply.body).toEqual({ success: true });
      expect(store.readThreadId).toHaveBeenCalledWith("m1");
      expect(store.findMissingIds).toHaveBeenCalledWith(["a1", "a2"]);
      expect(engine.linkToMessage).toHaveBeenCalledWith(["a1", "a2"], "m1", "t1");
      expect(responseViolations("aiAttachmentsLinkToMessage", 200, reply.body)).toEqual([]);
    },
  );

  it("binds exactly 100 ids", async () => {
    const ids = Array.from({ length: 100 }, (_, i) => `a${i}`);

    const reply = await invoke(handler, { body: [ids, "m1", "t1"] });

    expect(reply.status).toBe(200);
    expect(engine.linkToMessage).toHaveBeenCalledWith(ids, "m1", "t1");
  });

  it.each([
    ["absent", undefined],
    ["null", null],
    ["a string", "a1"],
    ["{}", {}],
    ["an empty array", []],
    ["without ids", { messageId: "m1", threadId: "t1" }],
    ["with ids null", { ids: null, messageId: "m1", threadId: "t1" }],
    ["with ids a string", { ids: "a1", messageId: "m1", threadId: "t1" }],
    ["with ids empty", { ids: [], messageId: "m1", threadId: "t1" }],
    ["without messageId", { ids: ["a1"], threadId: "t1" }],
    ["with messageId null", { ids: ["a1"], messageId: null, threadId: "t1" }],
    ["with messageId a number", { ids: ["a1"], messageId: 1, threadId: "t1" }],
    ["with messageId empty", { ids: ["a1"], messageId: "", threadId: "t1" }],
    ["without threadId", { ids: ["a1"], messageId: "m1" }],
    ["with threadId null", { ids: ["a1"], messageId: "m1", threadId: null }],
    ["with threadId a number", { ids: ["a1"], messageId: "m1", threadId: 1 }],
    ["with threadId empty", { ids: ["a1"], messageId: "m1", threadId: "" }],
    [
      "101 ids without a messageId",
      { ids: Array.from({ length: 101 }, () => "a"), threadId: "t1" },
    ],
  ])("answers 400 for a body that is %s and reads nothing", async (_what, body) => {
    const reply = await invoke(handler, { body });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: shapeError });
    expect(store.readThreadId).not.toHaveBeenCalled();
    expect(engine.linkToMessage).not.toHaveBeenCalled();
    expect(responseViolations("aiAttachmentsLinkToMessage", 400, reply.body)).toEqual([]);
  });

  it("answers 400 for 101 ids and reads nothing", async () => {
    const ids = Array.from({ length: 101 }, (_, i) => `a${i}`);

    const reply = await invoke(handler, { body: [ids, "m1", "t1"] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "ids may name at most 100 attachments (got 101)" });
    expect(store.readThreadId).not.toHaveBeenCalled();
  });

  it("answers 404 for a message that does not exist and binds nothing", async () => {
    store.readThreadId.mockResolvedValue(null);

    const reply = await invoke(handler, { body: [["a1"], "m1", "t1"] });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "message not found" });
    expect(store.findMissingIds).not.toHaveBeenCalled();
    expect(engine.linkToMessage).not.toHaveBeenCalled();
    expect(responseViolations("aiAttachmentsLinkToMessage", 404, reply.body)).toEqual([]);
  });

  it("answers 400 for a message of another thread and binds nothing", async () => {
    store.readThreadId.mockResolvedValue("t2");

    const reply = await invoke(handler, { body: [["a1"], "m1", "t1"] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "messageId does not belong to the given threadId" });
    expect(store.findMissingIds).not.toHaveBeenCalled();
    expect(engine.linkToMessage).not.toHaveBeenCalled();
  });

  it("answers 404 naming every missing attachment and binds nothing", async () => {
    store.findMissingIds.mockResolvedValue(["a2", "a3"]);

    const reply = await invoke(handler, { body: [["a1", "a2", "a3"], "m1", "t1"] });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "attachment(s) not found: a2, a3" });
    expect(engine.linkToMessage).not.toHaveBeenCalled();
    expect(responseViolations("aiAttachmentsLinkToMessage", 404, reply.body)).toEqual([]);
  });

  it("answers the AI service's 500 of the message read, not 404", async () => {
    store.readThreadId.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://ai/x"),
    );

    const reply = await invoke(handler, { body: [["a1"], "m1", "t1"] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(engine.linkToMessage).not.toHaveBeenCalled();
    expect(responseViolations("aiAttachmentsLinkToMessage", 500, reply.body)).toEqual([]);
  });

  it("answers 500, not 404, when the message read times out", async () => {
    store.readThreadId.mockRejectedValue(
      new DOMException("The operation timed out.", "TimeoutError"),
    );

    const reply = await invoke(handler, { body: [["a1"], "m1", "t1"] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });

  it("answers 500, not 404, when the attachment records read fails", async () => {
    store.findMissingIds.mockRejectedValue(
      new AiServiceHttpError(500, "Internal Server Error", "MARKER", "http://ai/x"),
    );

    const reply = await invoke(handler, { body: [["a1"], "m1", "t1"] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(engine.linkToMessage).not.toHaveBeenCalled();
  });

  it("answers 500 when the binding itself fails", async () => {
    engine.linkToMessage.mockRejectedValue(new Error("down"));

    const reply = await invoke(handler, { body: [["a1"], "m1", "t1"] });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
  });

  it("answers 400 for ids that are not strings, before any read", async () => {
    const reply = await invoke(handler, { body: [[5, null, 5], "m1", "t1"] });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "ids must hold non-empty strings only" });
    expect(store.findMissingIds).not.toHaveBeenCalled();
    expect(engine.linkToMessage).not.toHaveBeenCalled();
  });
});

// ts-ai-failure-paths.md §3: the access gate comes before the body. With the AI service refusing
// the caller (a guest, or AI switched off), a body the route would refuse with 400 answers 403.
describe.each([
  ["saveFile", "aiAttachmentsSaveFile", { input: null, entityId: 42 }],
  ["saveFilesMany", "aiAttachmentsSaveFilesMany", { inputs: "x", entityId: 42 }],
] as const)("attachmentsController.%s behind the access gate", (route, operationId, body) => {
  it("answers 403 for a body it would refuse, before the body is read", async () => {
    toolPrefs.readDisabled.mockRejectedValueOnce(
      new AiServiceHttpError(403, "Forbidden", "", "http://ai/tool-prefs"),
    );

    const reply = await invoke(attachmentsController[route], { body });

    expect(reply.status).toBe(403);
    expect(responseViolations(operationId, 403, reply.body)).toEqual([]);
    expect(engine[route]).not.toHaveBeenCalled();
  });
});
