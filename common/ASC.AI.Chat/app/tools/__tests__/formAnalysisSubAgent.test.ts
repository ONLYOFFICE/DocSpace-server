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

import { custom } from "@onlyoffice/ai-chat/core";
import type { Profile } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { docspace, docspaceError, http, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { setResolvedAttachmentId, shouldForwardHeadersToProvider } from "../../requestContext.js";
import type { AiServiceHttpError } from "../../storage/httpClient.js";
import { storage } from "../../storage/index.js";
import { formAnalysisSubAgent } from "../formAnalysisSubAgent.js";

vi.mock("@onlyoffice/ai-chat/core", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@onlyoffice/ai-chat/core")>()),
  custom: vi.fn(),
}));

const upstream = useUpstream();

const PROFILE: Profile = {
  id: "p-forms",
  name: "Forms",
  providerType: "openai",
  baseUrl: "https://api.example.com/v1",
  modelId: "gpt-x",
};

const ANALYZE_FORM_TOOL = {
  name: "analyze_form",
  description:
    "Analyze the attached form's submitted data and answer a question about it. Pass the user's " +
    "question verbatim, in natural language; returns a text answer.",
  inputSchema: {
    type: "object",
    properties: {
      question: {
        type: "string",
        description: "The user's question about the form's submissions, in natural language.",
      },
    },
    required: ["question"],
  },
  serverType: "form-analysis",
  requireApproval: false,
  enabled: true,
};

const QUERY_SCHEMA = { type: "object", properties: { sql: { type: "string" } } };
const QUERY_TOOL = {
  name: "query_form",
  description: "Query rows",
  inputSchema: QUERY_SCHEMA,
  enabled: true,
};

// POST internal/ai/tools/list with `formSubAgent: true` answers the form-data tools and the
// FormDataRules prompt (ToolListResponse, products/ASC.AI/Server/Tools/Core/ToolDescriptor.cs).
function answerFormTools(
  tools: unknown[] = [{ name: "query_form", description: "Query rows", parameters: QUERY_SCHEMA }],
): void {
  upstream.use(
    http.post("*/internal/ai/tools/list", () => docspace({ prompt: "Form rules", tools })),
  );
}

// POST internal/ai/tools/call (ToolCallResult, products/ASC.AI/Server/Tools/Core/ToolCallResult.cs).
function answerToolCalls(): void {
  let n = 0;
  upstream.use(
    http.post("*/internal/ai/tools/call", () => {
      n += 1;
      return docspace([{ id: "x", name: "query_form", result: `rows ${n}`, error: null }]);
    }),
  );
}

function useProfile(
  formAnalysis: string | null,
  fallback: string | null = null,
  profile: Profile | null = PROFILE,
): void {
  vi.spyOn(storage.assignments, "readByType").mockImplementation(async (type) =>
    type === "FormAnalysis" ? formAnalysis : fallback,
  );
  vi.spyOn(storage.profiles, "readById").mockResolvedValue(profile as Profile);
}

type Seen = { profile: unknown; messages: unknown; tools: unknown; systemPrompt: unknown };

/** Each model turn yields the events of one entry; answers what every turn was called with. */
function modelTurns(...turns: unknown[][]): Seen[] {
  const seen: Seen[] = [];
  let index = 0;
  vi.mocked(custom).mockImplementation(async function* (args) {
    seen.push({
      profile: args.profile,
      messages: JSON.parse(JSON.stringify(args.messages)) as unknown,
      tools: args.tools,
      systemPrompt: args.systemPrompt,
    });
    const events = turns[index] ?? [];
    index += 1;
    for (const event of events) {
      yield event as never;
    }
  });
  return seen;
}

const end = (content: unknown) => ({
  isEnd: true,
  responseMessage: { role: "assistant", content },
});
const call = (id: string, args: Record<string, unknown> = { sql: "select 1" }) => ({
  type: "tool-call",
  toolCallId: id,
  toolName: "query_form",
  args,
});

function run(
  args: Record<string, unknown>,
  entityId?: string,
): Promise<{ result: unknown; forward: boolean }> {
  return inRequestContext({}, async () => {
    const result = await formAnalysisSubAgent.callTool("analyze_form", args, entityId);
    return { result, forward: shouldForwardHeadersToProvider() };
  });
}

beforeEach(() => {
  vi.mocked(custom).mockImplementation(async function* () {
    yield* [];
    throw new Error("custom was not expected to run");
  });
});

describe("formAnalysisSubAgent.getTools", () => {
  it("offers nothing without a resolved attachment and reads nothing", async () => {
    await expect(inRequestContext({}, () => formAnalysisSubAgent.getTools())).resolves.toEqual({});
    await expect(formAnalysisSubAgent.getTools()).resolves.toEqual({});
    expect(upstream.seen).toEqual([]);
  });

  it("offers nothing for an empty attachment id", async () => {
    const result = await inRequestContext({}, () => {
      setResolvedAttachmentId("");
      return formAnalysisSubAgent.getTools();
    });

    expect(result).toEqual({});
    expect(upstream.seen).toEqual([]);
  });

  // GET internal/ai/form-analysis/{attachmentId} (HttpFormAnalysisStorage.read).
  it.each<[string, unknown, Record<string, unknown>]>([
    ["unavailable", { status: "unavailable" }, {}],
    ["an unknown status, read as unavailable", { status: "other" }, {}],
    ["ready", { status: "ready", questions: [] }, { "form-analysis": [ANALYZE_FORM_TOOL] }],
    [
      "to generate",
      {
        status: "generate",
        schema: { title: "T", culture: "en", cultureName: "English", rowCount: 1, columns: [] },
      },
      { "form-analysis": [ANALYZE_FORM_TOOL] },
    ],
  ])("answers for an analysis that is %s", async (_name, answer, expected) => {
    upstream.use(http.get("*/internal/ai/form-analysis/:id", () => docspace(answer as never)));

    const result = await inRequestContext({}, () => {
      setResolvedAttachmentId("att/1");
      return formAnalysisSubAgent.getTools();
    });

    expect(result).toEqual(expected);
    expect(upstream.seen.map((r) => r.path)).toEqual(["/internal/ai/form-analysis/att%2F1"]);
  });

  it.each([403, 500])("rethrows the read failing with %i", async (status) => {
    upstream.use(http.get("*/internal/ai/form-analysis/:id", () => docspaceError(status)));

    const error = await inRequestContext({}, () => {
      setResolvedAttachmentId("a");
      return formAnalysisSubAgent.getTools();
    }).catch((e: unknown) => e);

    expect((error as AiServiceHttpError).status).toBe(status);
  });
});

describe("formAnalysisSubAgent.callTool arguments", () => {
  it("answers an unknown tool by name, without any call", async () => {
    await expect(formAnalysisSubAgent.callTool("query_form", { question: "q" })).resolves.toBe(
      'Unknown tool "query_form"',
    );
    expect(upstream.seen).toEqual([]);
  });

  it.each<[string, unknown]>([
    ["an empty object", {}],
    ["question null", { question: null }],
    ["question a number", { question: 5 }],
    ["question an array", { question: ["q"] }],
    ["question empty", { question: "" }],
    ["question blank", { question: "  \n " }],
    ["args null", null],
    ["args a string", "how many?"],
    ["args an array", ["how many?"]],
  ])("asks for a question when given %s", async (_name, args) => {
    await expect(
      formAnalysisSubAgent.callTool("analyze_form", args as Record<string, unknown>),
    ).resolves.toBe("analyze_form requires a non-empty 'question'.");
    expect(upstream.seen).toEqual([]);
    expect(custom).not.toHaveBeenCalled();
  });
});

describe("formAnalysisSubAgent.callTool analysis", () => {
  it("runs the form tools on the FormAnalysis profile until the model answers", async () => {
    useProfile("p-forms");
    answerFormTools();
    answerToolCalls();
    const first = {
      role: "assistant",
      content: [{ type: "text", text: "Let me look." }, call("c1"), call("c2", { sql: "x" })],
    };
    const seen = modelTurns(
      [
        { role: "assistant", content: "partial" },
        "noise",
        null,
        { isEnd: true, responseMessage: first },
      ],
      [
        end([
          { type: "text", text: "There are " },
          { type: "text", text: "12 rows." },
        ]),
      ],
    );

    const { result, forward } = await run({ question: "  How many rows?  " }, "77");

    expect(result).toBe("There are 12 rows.");
    expect(forward).toBe(true);
    expect(storage.assignments.readByType).toHaveBeenCalledTimes(1);
    expect(storage.assignments.readByType).toHaveBeenCalledWith("FormAnalysis");
    expect(storage.profiles.readById).toHaveBeenCalledWith("p-forms");
    expect(seen).toEqual([
      {
        profile: PROFILE,
        messages: [{ role: "user", content: "How many rows?" }],
        tools: [QUERY_TOOL],
        systemPrompt: "Form rules",
      },
      {
        profile: PROFILE,
        messages: [
          { role: "user", content: "How many rows?" },
          {
            role: "assistant",
            content: [
              { type: "text", text: "Let me look." },
              { ...call("c1"), result: "rows 1" },
              { ...call("c2", { sql: "x" }), result: "rows 2" },
            ],
          },
        ],
        tools: [QUERY_TOOL],
        systemPrompt: "Form rules",
      },
    ]);
    expect(upstream.seen.map((r) => [r.path, r.body])).toEqual([
      [
        "/internal/ai/tools/list",
        {
          folderId: 77,
          formId: 0,
          attachmentId: "",
          formSubAgent: true,
          knowledgeRoomId: 0,
          knowledgeRoomIds: [],
        },
      ],
      [
        "/internal/ai/tools/call",
        expect.objectContaining({
          folderId: 77,
          formSubAgent: true,
          calls: [{ id: expect.any(String), name: "query_form", arguments: { sql: "select 1" } }],
        }),
      ],
      [
        "/internal/ai/tools/call",
        expect.objectContaining({
          calls: [{ id: expect.any(String), name: "query_form", arguments: { sql: "x" } }],
        }),
      ],
    ]);
    expect(logger.info).toHaveBeenCalledWith(
      'analyze_form: running on profile "Forms" (id=p-forms, model=gpt-x)',
    );
  });

  it("falls back to the Default assignment", async () => {
    useProfile(null, "p-default");
    answerFormTools();
    modelTurns([end("Answer")]);

    await expect(run({ question: "q" })).resolves.toEqual({ result: "Answer", forward: true });
    expect(vi.mocked(storage.assignments.readByType).mock.calls).toEqual([
      ["FormAnalysis"],
      ["Default"],
    ]);
    expect(storage.profiles.readById).toHaveBeenCalledWith("p-default");
  });

  it("answers that no model is configured when neither slot is assigned", async () => {
    useProfile(null, null);

    await expect(run({ question: "q" })).resolves.toEqual({
      result: "Form analysis is unavailable: no analysis model is configured.",
      forward: true,
    });
    expect(storage.profiles.readById).not.toHaveBeenCalled();
    expect(upstream.seen).toEqual([]);
  });

  it("answers that no model is configured when the assigned profile is not found", async () => {
    useProfile("gone", null, null);

    await expect(run({ question: "q" })).resolves.toMatchObject({
      result: "Form analysis is unavailable: no analysis model is configured.",
    });
    expect(upstream.seen).toEqual([]);
  });

  it("answers that the submissions are not accessible when no form tool is listed", async () => {
    useProfile("p-forms");
    answerFormTools([]);

    await expect(run({ question: "q" })).resolves.toMatchObject({
      result: "Form analysis is unavailable: the form's submission table is not accessible.",
    });
    expect(custom).not.toHaveBeenCalled();
  });

  // A tools/list answer without the list is unknown, not "the table is not accessible".
  it.each([
    ["null", null],
    ["a string", "x"],
    ["an object without tools", { prompt: "Form rules" }],
  ])(
    "answers an internal error, not an inaccessible table, for a tool list that is %s",
    async (_what, body) => {
      useProfile("p-forms");
      upstream.use(http.post("*/internal/ai/tools/list", () => docspace(body)));

      await expect(run({ question: "q" })).resolves.toMatchObject({
        result: "Form analysis could not be completed due to an internal error.",
      });
      expect(custom).not.toHaveBeenCalled();
    },
  );

  it.each<[string, unknown[], string]>([
    ["no event", [], "The analysis produced no answer."],
    ["an empty final text", [end("")], "The analysis produced no answer."],
    [
      "a final message without content",
      [{ isEnd: true, responseMessage: { role: "assistant" } }],
      "The analysis produced no answer.",
    ],
    [
      "a final message with object content",
      [end({ text: "x" })],
      "The analysis produced no answer.",
    ],
    [
      "the last delta when no end arrives",
      [
        { role: "assistant", content: "a" },
        { role: "assistant", content: "ab" },
      ],
      "ab",
    ],
    [
      "the end event itself when its message is not an object",
      [{ isEnd: true, responseMessage: "x", content: "own" }],
      "own",
    ],
    [
      "the event itself when isEnd is not true",
      [{ isEnd: "true", responseMessage: { content: "no" }, content: "yes" }],
      "yes",
    ],
    [
      "only the text parts",
      [
        end([
          { type: "text", text: "a" },
          { type: "reasoning", text: "r" },
          { type: "text" },
          null,
          { type: "text", text: "b" },
        ]),
      ],
      "ab",
    ],
  ])("answers %s", async (_name, events, expected) => {
    useProfile("p-forms");
    answerFormTools();
    modelTurns(events);

    await expect(run({ question: "q" })).resolves.toMatchObject({ result: expected });
  });

  it.each<[string, Record<string, unknown>, Record<string, unknown> | null]>([
    ["args as an object", { args: { a: 1 }, argsText: '{"b":2}' }, { a: 1 }],
    ["argsText as JSON", { argsText: '{"b":2}' }, { b: 2 }],
    ["argsText that is not JSON as no arguments", { argsText: "{b" }, {}],
    ["argsText that is a JSON array as no arguments", { argsText: "[1]" }, {}],
    ["an empty argsText as no arguments", { argsText: "" }, {}],
    ["args that are an array as no arguments", { args: [1] }, {}],
    ["no args as no arguments", {}, {}],
  ])("reads %s", async (_name, part, expected) => {
    useProfile("p-forms");
    answerFormTools();
    answerToolCalls();
    modelTurns(
      [end([{ type: "tool-call", toolCallId: "c", toolName: "query_form", ...part }])],
      [end("done")],
    );

    await expect(run({ question: "q" })).resolves.toMatchObject({ result: "done" });
    expect(upstream.seen[1]?.body).toMatchObject({
      calls: [{ name: "query_form", arguments: expected }],
    });
  });

  it("skips tool-call parts without an id or a name and leaves them without a result", async () => {
    useProfile("p-forms");
    answerFormTools();
    answerToolCalls();
    const parts = [
      { type: "tool-call", toolName: "query_form", args: {} },
      { type: "tool-call", toolCallId: "", toolName: "query_form", args: {} },
      { type: "tool-call", toolCallId: "n", args: {} },
      { type: "tool-call", toolCallId: "m", toolName: 5, args: {} },
      { type: "tool-result", toolCallId: "r" },
      "text",
      call("ok"),
    ];
    const seen = modelTurns([end(parts)], [end("done")]);

    await expect(run({ question: "q" })).resolves.toMatchObject({ result: "done" });
    expect(upstream.seen.filter((r) => r.path === "/internal/ai/tools/call")).toHaveLength(1);
    expect(seen[1]?.messages).toEqual([
      { role: "user", content: "q" },
      { role: "assistant", content: [...parts.slice(0, 6), { ...call("ok"), result: "rows 1" }] },
    ]);
  });

  it("answers the text of a turn whose only tool calls are unusable", async () => {
    useProfile("p-forms");
    answerFormTools();
    modelTurns([
      end([
        { type: "text", text: "plain" },
        { type: "tool-call", toolName: "query_form" },
      ]),
    ]);

    await expect(run({ question: "q" })).resolves.toMatchObject({ result: "plain" });
    expect(custom).toHaveBeenCalledTimes(1);
  });

  it("passes a failed form tool's text back to the model", async () => {
    useProfile("p-forms");
    answerFormTools();
    upstream.use(http.post("*/internal/ai/tools/call", () => docspace([{ error: "no access" }])));
    const seen = modelTurns([end([call("c")])], [end("sorry")]);

    await expect(run({ question: "q" })).resolves.toMatchObject({ result: "sorry" });
    expect(seen[1]?.messages).toEqual([
      { role: "user", content: "q" },
      {
        role: "assistant",
        content: [{ ...call("c"), result: 'Tool "query_form" failed: no access' }],
      },
    ]);
  });

  it("stops after 20 rounds without an answer", async () => {
    useProfile("p-forms");
    answerFormTools();
    answerToolCalls();
    modelTurns(...Array.from({ length: 25 }, (_, i) => [end([call(`c${i}`)])]));

    await expect(run({ question: "q" })).resolves.toMatchObject({
      result: "The analysis could not be completed within the allotted steps.",
    });
    expect(custom).toHaveBeenCalledTimes(20);
    expect(upstream.seen.filter((r) => r.path === "/internal/ai/tools/call")).toHaveLength(20);
    expect(logger.warn).toHaveBeenCalledWith(
      "analyze_form: reached the 20-round budget without a final answer",
    );
  });

  it.each<[string, () => void, string]>([
    [
      "the tools list failing",
      () => {
        useProfile("p-forms");
        upstream.use(http.post("*/internal/ai/tools/list", () => docspaceError(500, "secret")));
      },
      "analyze_form failed: AI service 500 ",
    ],
    [
      "the assignment read failing",
      () => {
        vi.spyOn(storage.assignments, "readByType").mockRejectedValue(
          new Error("assignments down"),
        );
      },
      "analyze_form failed: assignments down",
    ],
    [
      "the model throwing",
      () => {
        useProfile("p-forms");
        answerFormTools();
        vi.mocked(custom).mockImplementation(async function* () {
          yield* [];
          throw new Error("provider 429");
        });
      },
      "analyze_form failed: provider 429",
    ],
    [
      "the model throwing a non-Error",
      () => {
        useProfile("p-forms");
        answerFormTools();
        vi.mocked(custom).mockImplementation(async function* () {
          yield* [];
          throw "boom" as unknown as Error;
        });
      },
      "analyze_form failed: boom",
    ],
    [
      "a form tool call failing",
      () => {
        useProfile("p-forms");
        answerFormTools();
        upstream.use(http.post("*/internal/ai/tools/call", () => docspaceError(502)));
        modelTurns([end([call("c")])]);
      },
      "analyze_form failed: AI service 502 ",
    ],
  ])("answers an internal error for %s and warns", async (_name, arrange, logged) => {
    arrange();

    const { result } = await run({ question: "q" });

    expect(result).toBe("Form analysis could not be completed due to an internal error.");
    expect(result).not.toContain("secret");
    expect(logger.warn).toHaveBeenCalledWith(expect.stringContaining(logged));
  });
});
