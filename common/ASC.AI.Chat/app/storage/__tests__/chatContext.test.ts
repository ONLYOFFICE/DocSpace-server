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

import { afterEach, describe, expect, it, vi } from "vitest";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import {
  delay,
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import {
  countFilesApiRead,
  getChatContextSnapshot,
  noteChatContextMiss,
} from "../../requestContext.js";
import { describeChatContextUsage, parseChatContext, primeChatContext } from "../chatContext.js";
import type { ChatContextRequest } from "../chatContext.js";
import { AiServiceHttpError, aiService } from "../httpClient.js";
import { dtoToProfile } from "../profilesStorage.js";
import {
  chatContextDto,
  MESSAGE_ID,
  PROFILE_ID,
  THREAD_ID,
} from "../../__tests__/fixtures/chatContext.js";

// GET internal/ai/chat-context: products/ASC.AI/Server/Api/ChatContextController.cs ReadAsync.
const upstream = useUpstream();

const full: ChatContextRequest = { threadId: THREAD_ID, entityId: "42", contextEntityId: "43" };

const parsedFull = () => ({
  requested: { threadId: THREAD_ID, entityId: "42", contextEntityId: "43" },
  aiReady: true,
  toolPermissionMode: "auto",
  profiles: [dtoToProfile((chatContextDto()["profiles"] as unknown[])[0])],
  global: {
    entityId: undefined,
    folder: undefined,
    assignments: { Chat: PROFILE_ID },
    reasoningLevel: "high",
    toolPrefs: { "docspace-integration": { disabled: ["generate_docx"], allowAlways: null } },
    mcpServers: { jira: { url: "https://mcp.example.com/jira" } },
  },
  entity: {
    entityId: "42",
    folder: { title: "Launch plan", folderType: 40, canCreate: true },
    assignments: {},
    reasoningLevel: null,
    toolPrefs: {},
    mcpServers: {},
  },
  contextEntity: {
    entityId: "43",
    folder: { title: "Docs", folderType: 1, canCreate: false },
    assignments: {},
    reasoningLevel: null,
    toolPrefs: {},
    mcpServers: {},
  },
  thread: {
    threadId: THREAD_ID,
    title: "Quarterly report",
    lastEditDate: 1_700_000_001_000,
    profileId: PROFILE_ID,
  },
  messages: [
    {
      role: "user",
      content: [{ type: "text", text: "hi" }],
      id: MESSAGE_ID,
      createdAt: new Date(1_700_000_002_000),
    },
  ],
  webSearch: { provider: "exa", key: "exa-key" },
  documentAgentEnabled: true,
  stale: new Set(),
});

function withField(path: string[], value: unknown): Record<string, unknown> {
  const dto = chatContextDto();
  let target: Record<string, unknown> = dto;
  for (const key of path.slice(0, -1)) {
    target = target[key] as Record<string, unknown>;
  }
  const last = path[path.length - 1] as string;
  if (value === MISSING) {
    delete target[last];
  } else {
    target[last] = value;
  }
  return dto;
}

const MISSING = Symbol("missing");

describe("parseChatContext", () => {
  it("maps the full ChatContextDto", () => {
    expect(parseChatContext(chatContextDto(), full)).toEqual(parsedFull());
  });

  it("maps the profiles through dtoToProfile", () => {
    const profile = (chatContextDto()["profiles"] as unknown[])[0];
    expect(parseChatContext(chatContextDto(), full)?.profiles).toEqual([dtoToProfile(profile)]);
    expect(dtoToProfile(profile)).toMatchObject({ id: PROFILE_ID, modelId: "gpt-4o" });
  });

  it.each([null, undefined, "chat-context", 42, [], true])(
    "answers undefined for a payload that is not an object (%j)",
    (raw) => {
      expect(parseChatContext(raw, full)).toBeUndefined();
    },
  );

  it.each([MISSING, null, "global", 7, []])("answers undefined when global is %s", (value) => {
    expect(parseChatContext(withField(["global"], value), full)).toBeUndefined();
  });

  it("takes the ids it was asked for, not the ones the payload names", () => {
    const parsed = parseChatContext(chatContextDto(), { entityId: "9", contextEntityId: "8" });
    expect(parsed?.requested).toEqual({
      threadId: undefined,
      entityId: "9",
      contextEntityId: "8",
    });
    expect(parsed?.entity?.entityId).toBe("9");
    expect(parsed?.contextEntity?.entityId).toBe("8");
    expect(parsed?.global.entityId).toBeUndefined();
  });

  describe("config", () => {
    it.each([MISSING, null, "config", 1, []])(
      "reads config %s as an aggregate that predates it",
      (value) => {
        const parsed = parseChatContext(withField(["config"], value), full);
        expect(parsed).toMatchObject({ toolPermissionMode: null });
        expect(parsed?.aiReady).toBeUndefined();
      },
    );

    it.each([
      [MISSING, undefined],
      [null, undefined],
      ["true", undefined],
      [1, undefined],
      [false, false],
    ])("reads config.aiReady %s as %s", (value, expected) => {
      expect(parseChatContext(withField(["config", "aiReady"], value), full)?.aiReady).toBe(
        expected,
      );
    });

    it("keeps no portal message for a portal that is not ready: AiSettingsDto declares none", () => {
      const parsed = parseChatContext(
        withField(["config", "aiNotReadyMessage"], "AI is not configured"),
        full,
      );
      expect(parsed).not.toHaveProperty("aiNotReadyMessage");
    });

    it.each([
      [0, "ask"],
      [1, "auto"],
      [2, "allow"],
      [3, null],
      [-1, null],
      ["Allow", "allow"],
      ["2", "allow"],
      ["nope", null],
      [MISSING, null],
      [null, null],
      [true, null],
    ])("reads config.toolPermissionMode %s as %s", (value, expected) => {
      expect(
        parseChatContext(withField(["config", "toolPermissionMode"], value), full)
          ?.toolPermissionMode,
      ).toBe(expected);
    });
  });

  describe("profiles", () => {
    it.each([MISSING, null, "profiles", {}, 3])("reads profiles %s as none", (value) => {
      expect(parseChatContext(withField(["profiles"], value), full)?.profiles).toEqual([]);
    });

    it("drops a profile dtoToProfile refuses and keeps the rest", () => {
      const dto = chatContextDto();
      const profile = (dto["profiles"] as unknown[])[0];
      dto["profiles"] = [null, "p", { id: "x" }, profile];
      expect(parseChatContext(dto, full)?.profiles).toEqual([dtoToProfile(profile)]);
    });
  });

  describe("scopes", () => {
    it.each([MISSING, null, "entity", 1, []])("reads entity %s as null", (value) => {
      expect(parseChatContext(withField(["entity"], value), full)?.entity).toBeNull();
    });

    it.each([MISSING, null, "contextEntity", 1, []])("reads contextEntity %s as null", (value) => {
      expect(parseChatContext(withField(["contextEntity"], value), full)?.contextEntity).toBeNull();
    });

    it("lands contextEntityId on the entity scope when both ids match, whatever the payload says", () => {
      const parsed = parseChatContext(withField(["contextEntity"], MISSING), {
        entityId: "42",
        contextEntityId: "42",
      });
      expect(parsed?.contextEntity).toBe(parsed?.entity);
      expect(parsed?.contextEntity?.folder?.title).toBe("Launch plan");
    });

    it("parses the context scope when only contextEntityId is given", () => {
      const parsed = parseChatContext(chatContextDto(), { contextEntityId: "43" });
      expect(parsed?.entity).toMatchObject({ entityId: undefined });
      expect(parsed?.contextEntity).toMatchObject({ entityId: "43" });
    });

    it.each([
      ["missing", MISSING, undefined],
      ["null", null, undefined],
      ["a string", "folder", undefined],
      ["without id", { title: "T", folderType: 1 }, undefined],
      ["with an empty id", { id: "", title: "T" }, undefined],
      ["with a null id", { id: null, title: "T" }, undefined],
      ["with a string id", { id: "42", title: "T" }, { title: "T" }],
      ["without title", { id: 42 }, { title: undefined }],
      ["with a numeric title", { id: 42, title: 5 }, { title: undefined }],
      ["with folderType as text", { id: 42, title: "T", folderType: "40" }, { title: "T" }],
      ["with folderType null", { id: 42, title: "T", folderType: null }, { title: "T" }],
      ["with canCreate null", { id: 42, title: "T", canCreate: null }, { title: "T" }],
      ["with canCreate as text", { id: 42, title: "T", canCreate: "true" }, { title: "T" }],
      [
        "with every field",
        { id: 42, title: "T", folderType: 0, canCreate: false, isAgent: true, prompt: "p" },
        { title: "T", folderType: 0, canCreate: false },
      ],
    ])("maps a scope folder %s", (_name, value, expected) => {
      expect(
        parseChatContext(withField(["entity", "folder"], value), full)?.entity?.folder,
      ).toEqual(expected);
    });

    it.each([
      [MISSING, {}],
      [null, {}],
      ["Chat", {}],
      [[PROFILE_ID], {}],
      [
        { Chat: PROFILE_ID, Default: null, Code: 3, Vision: "" },
        { Chat: PROFILE_ID, Vision: "" },
      ],
    ])("reads assignments %j as %j", (value, expected) => {
      expect(
        parseChatContext(withField(["global", "assignments"], value), full)?.global.assignments,
      ).toEqual(expected);
    });

    it.each([
      [MISSING, null],
      [null, null],
      ["high", null],
      [{}, null],
      [{ depth: null }, null],
      [{ depth: 3 }, null],
      [{ depth: "none" }, "off"],
      [{ depth: "XHigh" }, "max"],
      [{ depth: "minimal" }, null],
    ])("reads preferences %j as reasoning level %s", (value, expected) => {
      expect(
        parseChatContext(withField(["global", "preferences"], value), full)?.global.reasoningLevel,
      ).toBe(expected);
    });

    it.each([
      [MISSING, {}],
      [null, {}],
      ["prefs", {}],
      [[{ disabled: ["a"] }], {}],
      [{ any: "thing" }, { any: "thing" }],
    ])("keeps toolPrefs %j raw as %j", (value, expected) => {
      expect(
        parseChatContext(withField(["global", "toolPrefs"], value), full)?.global.toolPrefs,
      ).toEqual(expected);
    });

    it.each([
      ["missing", MISSING, {}],
      ["null", null, {}],
      ["an object", { jira: "{}" }, {}],
      ["a list of non-objects", [null, "jira", 1], {}],
      ["an entry without name", [{ config: "{}" }], {}],
      ["an entry with a numeric name", [{ name: 1, config: "{}" }], {}],
      ["an entry with config null", [{ name: "a", config: null }], {}],
      ["an entry without config", [{ name: "a" }], {}],
      ["an entry with config that is not JSON", [{ name: "a", config: "{" }], {}],
      ["an entry with config JSON that is not an object", [{ name: "a", config: "[1]" }], {}],
      ["an entry with config as a number", [{ name: "a", config: 1 }], {}],
      [
        "an entry with config as an object",
        [{ name: "a", config: { url: "u" } }],
        { a: { url: "u" } },
      ],
      [
        "a name listed twice",
        [
          { name: "a", config: '{"n":1}' },
          { name: "a", config: '{"n":2}' },
        ],
        { a: { n: 2 } },
      ],
      ["an empty name", [{ name: "", config: "{}" }], { "": {} }],
    ])("maps mcpServers given %s", (_name, value, expected) => {
      expect(
        parseChatContext(withField(["global", "mcpServers"], value), full)?.global.mcpServers,
      ).toEqual(expected);
    });
  });

  describe("thread and messages", () => {
    it.each([MISSING, null])("reads thread %s as null", (value) => {
      expect(parseChatContext(withField(["thread"], value), full)?.thread).toBeNull();
    });

    it.each([
      ["a string", "thread"],
      ["an empty object", {}],
      ["a thread without title", { id: THREAD_ID, lastEditDate: 1 }],
    ])("reads a thread that is %s as null", (_name, value) => {
      expect(parseChatContext(withField(["thread"], value), full)?.thread).toBeNull();
    });

    it.each([MISSING, null, "messages", { items: [] }])(
      "reads messages %s as not requested (null)",
      (value) => {
        expect(parseChatContext(withField(["messages"], value), full)?.messages).toBeNull();
      },
    );

    it("reads an empty list as a thread without messages", () => {
      expect(parseChatContext(withField(["messages"], []), full)?.messages).toEqual([]);
    });

    it("drops a message dtoToMessage refuses and keeps the rest in order", () => {
      const parsed = parseChatContext(
        withField(
          ["messages"],
          [
            { id: "m1", contents: '{"role":"user"}' },
            null,
            { contents: "no id" },
            { id: "m2", contents: "plain text", timestamp: 5 },
          ],
        ),
        full,
      );
      expect(parsed?.messages).toEqual([
        { role: "user", id: "m1" },
        { content: "plain text", id: "m2", createdAt: new Date(5) },
      ]);
    });
  });

  describe("webSearch and documentAgentEnabled", () => {
    it.each([MISSING, null, "exa", {}, { provider: "" }, { provider: 1 }])(
      "reads webSearch %j as null",
      (value) => {
        expect(parseChatContext(withField(["webSearch"], value), full)?.webSearch).toBeNull();
      },
    );

    it.each([
      [MISSING, undefined],
      [null, undefined],
      ["true", undefined],
      [1, undefined],
      [false, false],
    ])("reads documentAgentEnabled %s as %s", (value, expected) => {
      expect(
        parseChatContext(withField(["documentAgentEnabled"], value), full)?.documentAgentEnabled,
      ).toBe(expected);
    });
  });

  it("starts every snapshot with no stale slice, each with its own set", () => {
    const a = parseChatContext(chatContextDto(), full);
    const b = parseChatContext(chatContextDto(), full);
    expect(a?.stale.size).toBe(0);
    expect(a?.stale).not.toBe(b?.stale);
  });
});

describe("primeChatContext", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("asks the aggregate for the round's ids with messages and installs the snapshot", async () => {
    upstream.use(http.get("*/internal/ai/chat-context", () => docspace(chatContextDto())));

    const snapshot = await inRequestContext({ cookie: "asc_auth_key=abc" }, async () => {
      await primeChatContext(full);
      return getChatContextSnapshot();
    });

    expect(snapshot).toEqual(parsedFull());
    expect(upstream.seen).toHaveLength(1);
    expect(upstream.seen[0]).toMatchObject({
      method: "GET",
      path: `/internal/ai/chat-context?threadId=${THREAD_ID}&entityId=42&contextEntityId=43&includeMessages=true`,
      body: undefined,
    });
    expect(upstream.seen[0]?.headers["cookie"]).toBe("asc_auth_key=abc");
    expect(vi.mocked(logger.info).mock.calls).toEqual([
      [
        `chat-context: primed threadId=${THREAD_ID} entityId=42 contextEntityId=43 profiles=1 ` +
          "entity=folder thread=yes messages=1 webSearch=yes",
      ],
    ]);
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("asks without messages and leaves the absent ids out when the round has no thread", async () => {
    const dto = chatContextDto();
    delete dto["entity"];
    delete dto["thread"];
    delete dto["messages"];
    delete dto["webSearch"];
    upstream.use(http.get("*/internal/ai/chat-context", () => docspace(dto)));

    await inRequestContext({}, () => primeChatContext({}));

    expect(upstream.seen[0]?.path).toBe("/internal/ai/chat-context?includeMessages=false");
    expect(vi.mocked(logger.info).mock.calls).toEqual([
      [
        "chat-context: primed threadId=- entityId=- contextEntityId=- profiles=1 " +
          "entity=none thread=no messages=- webSearch=no",
      ],
    ]);
  });

  it("does not install anything outside a request context, but still asks", async () => {
    upstream.use(http.get("*/internal/ai/chat-context", () => docspace(chatContextDto())));
    await primeChatContext(full);
    expect(getChatContextSnapshot()).toBeUndefined();
    expect(upstream.seen).toHaveLength(1);
  });

  it.each([401, 403])("rethrows a %i as the caller's own failure", async (status) => {
    upstream.use(http.get("*/internal/ai/chat-context", () => docspaceError(status, "denied")));

    const result = await inRequestContext({}, async () => {
      const error = await primeChatContext(full).catch((e: unknown) => e);
      return { error, snapshot: getChatContextSnapshot() };
    });

    expect(result.error).toBeInstanceOf(AiServiceHttpError);
    expect((result.error as AiServiceHttpError).status).toBe(status);
    expect(result.snapshot).toBeUndefined();
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it.each([400, 404, 409, 500, 502, 503])(
    "falls back to the per-entity reads on a %i, with a warning",
    async (status) => {
      upstream.use(
        http.get("*/internal/ai/chat-context", () => docspaceError(status, "upstream detail")),
      );

      const snapshot = await inRequestContext({}, async () => {
        await expect(primeChatContext(full)).resolves.toBeUndefined();
        return getChatContextSnapshot();
      });

      expect(snapshot).toBeUndefined();
      expect(vi.mocked(logger.warn).mock.calls).toHaveLength(1);
      const line = String(vi.mocked(logger.warn).mock.calls[0]?.[0]);
      expect(line).toBe(
        `chat-context: aggregate read failed, falling back to per-entity reads: AI service ${status}`,
      );
      expect(line).not.toContain("/internal/ai/chat-context");
      expect(line).not.toContain("upstream detail");
      expect(logger.info).not.toHaveBeenCalled();
    },
  );

  it("falls back on a dropped connection", async () => {
    upstream.use(http.get("*/internal/ai/chat-context", () => HttpResponse.error()));

    const snapshot = await inRequestContext({}, async () => {
      await primeChatContext(full);
      return getChatContextSnapshot();
    });

    expect(snapshot).toBeUndefined();
    expect(String(vi.mocked(logger.warn).mock.calls[0]?.[0])).toMatch(
      /^chat-context: aggregate read failed, falling back to per-entity reads: /,
    );
  });

  it("falls back when the aggregate does not answer within the upstream timeout", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    upstream.use(
      http.get("*/internal/ai/chat-context", async () => {
        await delay("infinite");
        return docspace(chatContextDto());
      }),
    );

    const pending = inRequestContext({}, async () => {
      await primeChatContext(full);
      return getChatContextSnapshot();
    });
    await vi.advanceTimersByTimeAsync(300_000);

    await expect(pending).resolves.toBeUndefined();
    expect(vi.mocked(logger.warn).mock.calls).toEqual([
      [
        "chat-context: aggregate read failed, falling back to per-entity reads: upstream timeout after 300000ms",
      ],
    ]);
  });

  it.each([
    ["an answer without global", { config: {} }],
    ["an empty answer", new HttpResponse(null, { status: 204 })],
    ["a text answer", new HttpResponse("ok", { headers: { "content-type": "text/plain" } })],
  ])("falls back on %s", async (_name, answer) => {
    upstream.use(
      http.get("*/internal/ai/chat-context", () =>
        answer instanceof Response ? answer : docspace(answer),
      ),
    );

    const snapshot = await inRequestContext({}, async () => {
      await primeChatContext(full);
      return getChatContextSnapshot();
    });

    expect(snapshot).toBeUndefined();
    expect(vi.mocked(logger.warn).mock.calls).toHaveLength(1);
    expect(String(vi.mocked(logger.warn).mock.calls[0]?.[0])).toMatch(
      /^chat-context: unusable payload, falling back to per-entity reads: /,
    );
    expect(logger.info).not.toHaveBeenCalled();
  });

  it("asks when the opt-out header carries another value", async () => {
    upstream.use(http.get("*/internal/ai/chat-context", () => docspace(chatContextDto())));
    await inRequestContext({ "x-ai-chat-context": "on" }, () => primeChatContext(full));
    expect(upstream.seen).toHaveLength(1);
    expect(upstream.seen[0]?.headers["x-ai-chat-context"]).toBe("on");
  });
});

describe("describeChatContextUsage", () => {
  it("reports nothing outside a request context", () => {
    expect(describeChatContextUsage()).toBe("upstreamGets=0 upstreamCalls={} filesApiReads=0");
  });

  it("reports the round's calls by method in name order, its Files API reads and its misses", async () => {
    upstream.use(
      http.get("*/internal/ai/a", () => docspace(null)),
      http.put("*/internal/ai/b", () => new HttpResponse(null, { status: 204 })),
      http.delete("*/internal/ai/c", () => new HttpResponse(null, { status: 204 })),
    );

    const line = await inRequestContext({}, async () => {
      await aiService.get("/a");
      await aiService.get("/a");
      await aiService.put("/b", {});
      await aiService.delete("/c");
      countFilesApiRead();
      noteChatContextMiss("profiles.readAll");
      noteChatContextMiss("threads.readById(t)");
      return describeChatContextUsage();
    });

    expect(line).toBe(
      "upstreamGets=2 upstreamCalls={DELETE:1,GET:2,PUT:1} filesApiReads=1 " +
        "snapshotMisses=[profiles.readAll, threads.readById(t)]",
    );
  });

  it("leaves the misses out when there are none", async () => {
    const line = await inRequestContext({}, async () => describeChatContextUsage());
    expect(line).toBe("upstreamGets=0 upstreamCalls={} filesApiReads=0");
  });
});

describe("the switches", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.doUnmock("../../../config/index.js");
    vi.resetModules();
  });

  async function freshModules() {
    const chatContext = await import("../chatContext.js");
    const httpHelpers = await import("../../__tests__/helpers/http.js");
    const requestContext = await import("../../requestContext.js");
    return { ...chatContext, ...httpHelpers, ...requestContext };
  }

  it("asks nothing while AI_CHAT_CONTEXT is off", async () => {
    vi.resetModules();
    vi.stubEnv("AI_CHAT_CONTEXT", "off");
    const m = await freshModules();

    const snapshot = await m.inRequestContext({}, async () => {
      await m.primeChatContext(full);
      return m.getChatContextSnapshot();
    });

    expect(snapshot).toBeUndefined();
    expect(upstream.seen).toEqual([]);
  });

  it.each(["OFF", "0", "false", ""])("asks while AI_CHAT_CONTEXT is %j", async (value) => {
    vi.resetModules();
    vi.stubEnv("AI_CHAT_CONTEXT", value);
    upstream.use(http.get("*/internal/ai/chat-context", () => docspace(chatContextDto())));
    const m = await freshModules();

    const snapshot = await m.inRequestContext({}, async () => {
      await m.primeChatContext(full);
      return m.getChatContextSnapshot();
    });

    expect(snapshot?.requested.threadId).toBe(THREAD_ID);
  });

  it("asks nothing for a request that opts out outside production", async () => {
    vi.resetModules();
    const m = await freshModules();

    await m.inRequestContext({ "x-ai-chat-context": "off" }, () => m.primeChatContext(full));

    expect(upstream.seen).toEqual([]);
  });

  it("honours the opt-out header when the settings name no environment", async () => {
    vi.resetModules();
    vi.doMock("../../../config/index.js", async (importOriginal) => {
      const original = await importOriginal<typeof import("../../../config/index.js")>();
      const nconf = original.default;
      return {
        ...original,
        default: { get: (key?: string): unknown => (key === "app" ? undefined : nconf.get(key)) },
      };
    });
    const m = await freshModules();

    await m.inRequestContext({ "x-ai-chat-context": "off" }, () => m.primeChatContext(full));

    expect(upstream.seen).toEqual([]);
  });

  it("ignores the opt-out header in production", async () => {
    vi.resetModules();
    vi.doMock("../../../config/index.js", async (importOriginal) => {
      const original = await importOriginal<typeof import("../../../config/index.js")>();
      const nconf = original.default;
      return {
        ...original,
        default: {
          get: (key?: string): unknown =>
            key === "app"
              ? { ...(nconf.get("app") as object), environment: "Production" }
              : nconf.get(key),
        },
      };
    });
    upstream.use(http.get("*/internal/ai/chat-context", () => docspace(chatContextDto())));
    const m = await freshModules();

    const snapshot = await m.inRequestContext({ "x-ai-chat-context": "off" }, async () => {
      await m.primeChatContext(full);
      return m.getChatContextSnapshot();
    });

    expect(upstream.seen).toHaveLength(1);
    expect(snapshot?.requested.entityId).toBe("42");
  });
});
