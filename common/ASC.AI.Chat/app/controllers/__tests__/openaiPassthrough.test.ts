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

import { afterAll, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import {
  docspace,
  docspaceError,
  http,
  HttpResponse,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { listen, send, until } from "./openaiPassthrough.harness.js";
import type { RawReply, RawRequest } from "./openaiPassthrough.harness.js";
import {
  CALLER,
  CHAT,
  completion,
  envelope,
  GATEWAY,
  IMAGES,
  imageResult,
  MB,
  profileDto,
  PROFILE_ID,
  PROVIDER,
  useFile,
  useFolder,
  useProfile as profileStub,
  useProvider as providerStub,
} from "./openaiPassthrough.fixtures.js";

// The OpenAI-compatible passthrough through the whole application: the body parser left out
// for `/openai/*`, the gate, the request context, the real profile read and Files API lookup.
// The AI service, the Files API and the provider are stubbed with msw; the client is a raw
// socket (openaiPassthrough.harness.ts), so the unparsed body reaches the handler as sent.

// CORS from the developer's appsettings would add headers to every answer.
vi.stubEnv("core__cors", "");
// The access log is not what these tests are about.
vi.mock("morgan", () => ({
  default: () => (_req: unknown, _res: unknown, next: () => void) => next(),
}));

const upstream = useUpstream();
const useProfile = (dto?: unknown): void => profileStub(upstream, dto);
const useProvider = (url: string, answer: () => Response | Promise<Response>) =>
  providerStub(upstream, url, answer);
const { default: logger } = await import("../../log.js");
const { createApp } = await import("../../createApp.js");
const { aiServiceBaseUrl } = await import("../../storage/httpClient.js");

let server: { port: number; close: () => Promise<void> };
beforeAll(async () => {
  server = await listen(createApp());
});
afterAll(async () => {
  await server.close();
  vi.unstubAllEnvs();
});
beforeEach(() => {
  vi.mocked(logger.info).mockClear();
  vi.mocked(logger.warn).mockClear();
  vi.mocked(logger.error).mockClear();
});

function post(path: string, body: RawRequest["body"], headers: Record<string, string> = CALLER) {
  return send(server.port, { path, headers, body }).done;
}

function json(reply: RawReply): unknown {
  return JSON.parse(reply.body);
}

describe("chat/completions to a third-party provider", () => {
  it("relays the answer with its status and content type, and sends the profile key as a Bearer token", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));
    const body = '{"model":"gpt-4o","messages":[{"role":"user","content":"Hi"}]}';

    const reply = await post(CHAT, body);

    expect(reply.status).toBe(200);
    expect(reply.headers["content-type"]).toBe("application/json");
    expect(reply.headers["cache-control"]).toBe("no-cache, no-transform");
    expect(reply.headers["x-accel-buffering"]).toBe("no");
    expect(reply.body).toBe(JSON.stringify(completion));
    expect(responseViolations("aiOpenaiChatCompletions", 200, json(reply))).toEqual([]);
    expect(calls).toEqual([
      {
        path: "/v1/chat/completions",
        headers: { authorization: "Bearer sk-profile-secret", "content-type": "application/json" },
        body,
        signal: expect.any(AbortSignal),
      },
    ]);
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      `GET /internal/ai/profiles/${PROFILE_ID}`,
      "POST /v1/chat/completions",
    ]);
  });

  it("forwards none of the caller's own headers to a third-party provider", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, "{}", {
      ...CALLER,
      Cookie: "asc_auth_key=caller-cookie",
      Accept: "application/json",
      "X-Session-Id": "caller-session",
    });

    expect(calls[0]?.headers).toEqual({
      authorization: "Bearer sk-profile-secret",
      "content-type": "application/json",
    });
  });

  it("sends no Authorization when the profile carries no key", async () => {
    useProfile(profileDto({ key: null }));
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, "{}");

    expect(calls[0]?.headers).toEqual({ "content-type": "application/json" });
  });

  it("drops a trailing slash of the base URL", async () => {
    useProfile(profileDto({ baseUrl: `${PROVIDER}/` }));
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, "{}");

    expect(calls[0]?.path).toBe("/v1/chat/completions");
  });

  it("consumes entityId, entityKind and sessionId, relays the rest of the query and sends the session as x-session-id", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    await post(
      `${CHAT}?api-version=2024-10-21&entityId=17&entityKind=file&sessionId=thread-9&user=u%201`,
      "{}",
    );

    expect(calls[0]?.path).toBe("/v1/chat/completions?api-version=2024-10-21&user=u+1");
    expect(calls[0]?.headers).toEqual({
      authorization: "Bearer sk-profile-secret",
      "content-type": "application/json",
      "x-session-id": "thread-9",
    });
    // The Files API is asked only for the ONLYOFFICE provider.
    expect(upstream.seen.some((r) => r.path.startsWith("/api/2.0/files/"))).toBe(false);
  });

  it("fills in the profile's model when the body names none (Bug 84416)", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, '{ "messages": [ {"role":"user","content":"Hi"} ] }');

    expect(calls[0]?.body).toBe('{"model":"gpt-4o","messages":[{"role":"user","content":"Hi"}]}');
  });

  it.each([
    ["a model of its own", '{ "model": "o3",  "messages": [] }'],
    ["a null model", '{ "model": null, "messages": [] }'],
    ["a JSON array", '[ {"messages": []} ]'],
    ["JSON null", "null"],
    ["a JSON string", '"text"'],
    ["broken JSON", '{"messages": ['],
    ["an empty body", ""],
  ])("forwards a body with %s byte for byte", async (_what, body) => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    const reply = await post(CHAT, body);

    expect(reply.status).toBe(200);
    expect(calls[0]?.body).toBe(body);
  });

  it("leaves the model out when the profile's model is empty", async () => {
    useProfile(profileDto({ modelId: "" }));
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, '{"messages":[]}');

    expect(calls[0]?.body).toBe('{"messages":[]}');
  });

  it("does not parse a JSON body over 2 MB, so no model is filled in", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));
    const body = `{"messages":[{"role":"user","content":"${"x".repeat(2 * MB)}"}]}`;

    await post(CHAT, body);

    expect(calls[0]?.body).toBe(body);
  });

  it("parses a JSON body of exactly 2 MB", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));
    const frame = '{"messages":[{"role":"user","content":""}]}';
    const body = `{"messages":[{"role":"user","content":"${"x".repeat(2 * MB - frame.length)}"}]}`;

    await post(CHAT, body);

    expect(calls[0]?.body.startsWith('{"model":"gpt-4o","messages"')).toBe(true);
  });

  it("forwards a body of another content type untouched, under that content type", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, '{"messages":[]}', { ...CALLER, "Content-Type": "text/plain" });

    expect(calls[0]?.body).toBe('{"messages":[]}');
    expect(calls[0]?.headers["content-type"]).toBe("text/plain");
  });

  it("sends application/json to the provider when the request has no content type", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, '{"messages":[]}', { Authorization: "Bearer caller-token" });

    // Not parsed: without a JSON content type the body is not shaped either.
    expect(calls[0]?.body).toBe('{"messages":[]}');
    expect(calls[0]?.headers["content-type"]).toBe("application/json");
  });

  it("relays a provider 4xx with its status, content type and body", async () => {
    useProfile();
    const providerError = {
      error: {
        message: "Rate limit reached",
        type: "requests",
        code: "rate_limit_exceeded",
        param: null,
      },
    };
    useProvider(`${PROVIDER}/chat/completions`, () =>
      HttpResponse.json(providerError, { status: 429 }),
    );

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(429);
    expect(reply.headers["content-type"]).toBe("application/json");
    expect(json(reply)).toEqual(providerError);
    expect(responseViolations("aiOpenaiChatCompletions", 429, json(reply))).toEqual([]);
  });

  // DEFECT passthrough-1: a provider 400 is relayed as 400, a status aiOpenaiChatCompletions does not publish — expected the 400 in the published responses, as aiOpenaiImagesGenerations has it (ts-ai-tests.md §2: every answer matches the published schema)
  it("relays a provider 400, which the operation does not publish", async () => {
    useProfile();
    const providerError = {
      error: {
        message: "Invalid model",
        type: "invalid_request_error",
        code: null,
        param: "model",
      },
    };
    useProvider(`${PROVIDER}/chat/completions`, () =>
      HttpResponse.json(providerError, { status: 400 }),
    );

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(400);
    expect(json(reply)).toEqual(providerError);
    expect(responseViolations("aiOpenaiChatCompletions", 400, json(reply))).toEqual([
      "aiOpenaiChatCompletions publishes no JSON schema for status 400",
    ]);
  });

  it("relays a provider 4xx without a content type with none", async () => {
    useProfile();
    useProvider(
      `${PROVIDER}/chat/completions`,
      () => new HttpResponse(new TextEncoder().encode("denied"), { status: 403 }),
    );

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(403);
    expect(reply.headers["content-type"]).toBeUndefined();
    expect(reply.body).toBe("denied");
  });

  it("ends an answer without a body", async () => {
    useProfile();
    useProvider(`${PROVIDER}/chat/completions`, () => new HttpResponse(null, { status: 204 }));

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(204);
    expect(reply.body).toBe("");
  });

  it("answers a provider 5xx with a fixed 502 and keeps the provider's body in the log", async () => {
    useProfile();
    useProvider(
      `${PROVIDER}/chat/completions`,
      () => new HttpResponse("at Gateway.Proxy() in /src/internal.cs", { status: 503 }),
    );

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(502);
    expect(json(reply)).toEqual(envelope("Upstream request failed", "server_error"));
    expect(reply.body).not.toContain("internal.cs");
    expect(responseViolations("aiOpenaiChatCompletions", 502, json(reply))).toEqual([]);
    expect(logger.error).toHaveBeenCalledWith(
      "openai passthrough chat/completions: upstream answered 503: at Gateway.Proxy() in /src/internal.cs",
    );
  });

  it("answers 502 when the provider cannot be reached, and keeps the URL out of the answer", async () => {
    useProfile();
    useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.error());

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(502);
    expect(json(reply)).toEqual(envelope("Upstream request failed", "server_error"));
    expect(reply.body).not.toContain("provider.test");
    expect(vi.mocked(logger.error).mock.calls[0]?.[0]).toMatch(
      /^openai passthrough: upstream fetch failed: /,
    );
  });
});

describe("the profile", () => {
  it("answers 404 in the OpenAI envelope when the AI service does not know the profile", async () => {
    upstream.use(
      http.get(`${aiServiceBaseUrl}/internal/ai/profiles/:id`, () => docspaceError(404)),
    );

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(404);
    expect(json(reply)).toEqual(envelope("Profile not found", "invalid_request_error"));
    expect(responseViolations("aiOpenaiChatCompletions", 404, json(reply))).toEqual([]);
    expect(upstream.seen).toHaveLength(1);
  });

  it("answers 404 for a profile with an empty base URL", async () => {
    useProfile(profileDto({ baseUrl: "" }));

    const reply = await post(IMAGES, '{"prompt":"a cat"}');

    expect(reply.status).toBe(404);
    expect(json(reply)).toEqual(envelope("Profile not found", "invalid_request_error"));
    expect(responseViolations("aiOpenaiImagesGenerations", 404, json(reply))).toEqual([]);
  });

  // DEFECT passthrough-2: a 403 of the AI service (a guest, AI switched off) is answered as `{ error: "Forbidden" }`, while the operation publishes the OpenAI error envelope for 403 — expected the envelope the plugin's SDK reads (ts-ai-tests.md §2; apiCatalog.ts, aiOpenaiChatCompletions 403)
  it("answers a 403 of the AI service with the plain error object", async () => {
    upstream.use(
      http.get(`${aiServiceBaseUrl}/internal/ai/profiles/:id`, () => docspaceError(403)),
    );

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(403);
    expect(json(reply)).toEqual({ error: "Forbidden" });
    expect(responseViolations("aiOpenaiChatCompletions", 403, json(reply))).toEqual([
      "/error must be object",
    ]);
    expect(upstream.seen).toHaveLength(1);
  });

  // DEFECT passthrough-3: a 5xx of the AI service on the profile read is answered 500 `{ error: "Internal Server Error" }`, which is not the OpenAI envelope the operation publishes for 500 — expected the envelope (ts-ai-tests.md §2)
  it("answers a 500 of the AI service with 500 and the plain error object", async () => {
    upstream.use(
      http.get(`${aiServiceBaseUrl}/internal/ai/profiles/:id`, () => docspaceError(500)),
    );

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(500);
    expect(json(reply)).toEqual({ error: "Internal Server Error" });
    expect(responseViolations("aiOpenaiChatCompletions", 500, json(reply))).toEqual([
      "/error must be object",
    ]);
  });

  // products/ASC.AI/Server/Models/ResponseDto/ProfileDto.cs: each required field of the DTO
  // missing, null and of the wrong type.
  // DEFECT passthrough-4: an AI-service answer of the wrong shape is read as "no such profile" and answered 404 "Profile not found" — expected a 5xx, a body of the wrong shape is unknown, not absent (ts-ai-failure-paths.md §1)
  it.each(
    ["id", "name", "providerType", "baseUrl", "modelId", "createdAt"].flatMap((field) => [
      [field, "missing", undefined],
      [field, "null", null],
      [field, "of the wrong type", field === "createdAt" ? "1767225600000" : 42],
    ]),
  )("answers 404 when the profile's %s is %s", async (field, _what, value) => {
    const dto = profileDto();
    if (value === undefined) {
      delete dto[field as string];
    } else {
      dto[field as string] = value;
    }
    useProfile(dto);

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(404);
    expect(json(reply)).toEqual(envelope("Profile not found", "invalid_request_error"));
  });

  it.each([
    ["missing", undefined],
    ["null", null],
    ["of the wrong type", 42],
    ["empty", ""],
  ])("sends no Authorization when the profile's key is %s", async (_what, value) => {
    const dto = profileDto();
    if (value === undefined) {
      delete dto["key"];
    } else {
      dto["key"] = value;
    }
    useProfile(dto);
    const calls = useProvider(`${PROVIDER}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, "{}");

    expect(calls[0]?.headers).toEqual({ "content-type": "application/json" });
  });

  it("answers 404 for a profile whose answer is not an object", async () => {
    useProfile("a string");

    const reply = await post(CHAT, "{}");

    expect(reply.status).toBe(404);
  });
});

describe("the body limits", () => {
  it("answers 413 on a declared length over 100 MB without reading the body or calling the provider", async () => {
    useProfile();

    const reply = await send(server.port, {
      path: CHAT,
      headers: { ...CALLER, "Content-Length": String(100 * MB + 1) },
    }).done;

    expect(reply.status).toBe(413);
    expect(json(reply)).toEqual(envelope("Request body too large", "invalid_request_error"));
    expect(responseViolations("aiOpenaiChatCompletions", 413, json(reply))).toEqual([]);
    expect(upstream.seen).toHaveLength(1);
  });

  it("answers 413 on a declared length over 28 MB for the ONLYOFFICE provider (Bug 84415)", async () => {
    useProfile(profileDto({ providerType: "onlyoffice" }));

    const reply = await send(server.port, {
      path: IMAGES,
      headers: { ...CALLER, "Content-Length": String(28 * MB + 1) },
    }).done;

    expect(reply.status).toBe(413);
    expect(json(reply)).toEqual(envelope("Request body too large", "invalid_request_error"));
    expect(responseViolations("aiOpenaiImagesGenerations", 413, json(reply))).toEqual([]);
  });

  it("answers 413 on a chunked body that grows over 28 MB for the ONLYOFFICE provider", async () => {
    useProfile(profileDto({ providerType: "onlyoffice" }));

    const reply = await send(server.port, {
      path: CHAT,
      headers: CALLER,
      body: Buffer.alloc(28 * MB + 1, 0x20),
      chunked: true,
    }).done;

    expect(reply.status).toBe(413);
    expect(json(reply)).toEqual(envelope("Request body too large", "invalid_request_error"));
    expect(upstream.seen).toHaveLength(1);
  });

  it("answers nothing and logs a warning when the client goes away during the upload", async () => {
    useProfile();
    const exchange = send(server.port, {
      path: CHAT,
      headers: { ...CALLER, "Content-Length": "1000" },
    });
    await until(() => upstream.seen.length === 1);
    exchange.abort();

    await until(() => vi.mocked(logger.warn).mock.calls.length > 0);

    expect(vi.mocked(logger.warn).mock.calls[0]?.[0]).toMatch(
      /^openai passthrough: reading request body failed: /,
    );
    expect(upstream.seen).toHaveLength(1);
  });
});

describe("the ONLYOFFICE provider", () => {
  const onlyoffice = () => useProfile(profileDto({ providerType: "onlyoffice", key: null }));

  it("sends the round to the AI service's gateway with the caller's forwarded headers and the source as metadata", async () => {
    onlyoffice();
    useFolder(upstream, "17", "Contracts");
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    const reply = await post(
      `${CHAT}?entityId=17&sessionId=thread-9`,
      '{"model":"x","messages":[]}',
      {
        ...CALLER,
        Cookie: "asc_auth_key=caller-cookie",
        "X-Session-Id": "caller-session",
      },
    );

    expect(reply.status).toBe(200);
    expect(calls).toEqual([
      {
        path: "/api/2.0/ai/gateway/chat/completions",
        headers: {
          authorization: "Bearer caller-token",
          cookie: "asc_auth_key=caller-cookie",
          "content-type": "application/json",
          "x-session-id": "thread-9",
        },
        body: '{"metadata":{"source_id":"17","source_type":"Room","source_title":"Contracts"},"model":"x","messages":[]}',
        signal: expect.any(AbortSignal),
      },
    ]);
    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      `GET /internal/ai/profiles/${PROFILE_ID}`,
      "GET /api/2.0/files/folder/17",
      "POST /api/2.0/ai/gateway/chat/completions",
    ]);
  });

  it("sends a profile that runs on the portal's own key through the BYOK route of the gateway", async () => {
    useProfile(profileDto({ providerType: "onlyoffice", key: null, byokProvider: "openai" }));
    const calls = useProvider(`${GATEWAY}/byok/${PROFILE_ID}/chat/completions`, () =>
      HttpResponse.json(completion),
    );

    const reply = await post(CHAT, '{"model":"x"}');

    expect(reply.status).toBe(200);
    expect(calls[0]?.path).toBe(`/api/2.0/ai/gateway/byok/${PROFILE_ID}/chat/completions`);
  });

  it("keeps the caller's Authorization over the profile key", async () => {
    useProfile(profileDto({ providerType: "onlyoffice", key: "sk-profile-secret" }));
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, '{"model":"x"}');

    expect(calls[0]?.headers["authorization"]).toBe("Bearer caller-token");
  });

  it("sends the profile key when the caller authenticated by cookie alone", async () => {
    useProfile(profileDto({ providerType: "onlyoffice", key: "sk-profile-secret" }));
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, '{"model":"x"}', {
      "Content-Type": "application/json",
      Cookie: "asc_auth_key=caller-cookie",
    });

    expect(calls[0]?.headers).toEqual({
      authorization: "Bearer sk-profile-secret",
      cookie: "asc_auth_key=caller-cookie",
      "content-type": "application/json",
    });
  });

  it("resolves a file entry through the file route when entityKind is file", async () => {
    onlyoffice();
    useFile(upstream, "17", "Offer.docx");
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    await post(`${CHAT}?entityId=17&entityKind=file`, '{"model":"x"}');

    expect(calls[0]?.body).toBe(
      '{"metadata":{"source_id":"17","source_type":"File","source_title":"Offer.docx"},"model":"x"}',
    );
  });

  it("sends no metadata when the Files API fails, and logs the failure", async () => {
    onlyoffice();
    upstream.use(http.get(`*/api/2.0/files/folder/17`, () => docspaceError(500)));
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    const reply = await post(`${CHAT}?entityId=17`, '{"model":"x"}');

    expect(reply.status).toBe(200);
    expect(calls[0]?.body).toBe('{"model":"x"}');
    expect(vi.mocked(logger.warn).mock.calls[0]?.[0]).toMatch(
      /^source entity fetch failed \(folder 17\): /,
    );
  });

  it("re-serializes a parsed chat body without metadata when no entry is named", async () => {
    onlyoffice();
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, '{ "model": "x",  "messages": [] }');

    expect(calls[0]?.body).toBe('{"model":"x","messages":[]}');
  });

  it("puts the metadata into an empty object without a separator", async () => {
    useProfile(profileDto({ providerType: "onlyoffice", key: null, modelId: "" }));
    useFolder(upstream, "17", "Contracts");
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    await post(`${CHAT}?entityId=17`, "{}");

    expect(calls[0]?.body).toBe(
      '{"metadata":{"source_id":"17","source_type":"Room","source_title":"Contracts"}}',
    );
  });

  it("marks the system prompt and the last message for prompt caching on a streaming round", async () => {
    onlyoffice();
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));
    const body = {
      model: "x",
      stream: true,
      messages: [
        { role: "system", content: "Be brief" },
        { role: "user", content: "Hi" },
        { role: "assistant", content: "Hello" },
        { role: "user", content: "Again" },
      ],
    };

    await post(CHAT, JSON.stringify(body));

    const ephemeral = { type: "ephemeral" };
    expect(JSON.parse(calls[0]?.body ?? "")).toEqual({
      model: "x",
      stream: true,
      messages: [
        { role: "system", content: [{ type: "text", text: "Be brief", cache_control: ephemeral }] },
        { role: "user", content: "Hi" },
        { role: "assistant", content: "Hello" },
        { role: "user", content: [{ type: "text", text: "Again", cache_control: ephemeral }] },
      ],
    });
  });

  it.each([
    ["a round that is not streamed", { model: "x", messages: [{ role: "user", content: "Hi" }] }],
    ["a round whose messages are not a list", { model: "x", stream: true, messages: "Hi" }],
    [
      "a round that carries breakpoints of its own",
      {
        model: "x",
        stream: true,
        messages: [
          { role: "system", content: "Be brief" },
          { role: "user", content: [{ type: "text", text: "Hi", cache_control: null }] },
        ],
      },
    ],
  ])("adds no breakpoints to %s", async (_what, body) => {
    onlyoffice();
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    await post(CHAT, JSON.stringify(body));

    expect(calls[0]?.body).toBe(JSON.stringify(body));
  });

  it.each([
    ["broken JSON", '{"model": "x",'],
    ["a JSON array", '[{"model":"x"}]'],
  ])("forwards %s untouched", async (_what, body) => {
    onlyoffice();
    useFolder(upstream, "17", "Contracts");
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    await post(`${CHAT}?entityId=17`, body);

    expect(calls[0]?.body).toBe(body);
  });

  it("forwards a body of another content type untouched and does not resolve metadata into it", async () => {
    onlyoffice();
    useFolder(upstream, "17", "Contracts");
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));

    await post(`${CHAT}?entityId=17`, '{"model":"x"}', { ...CALLER, "Content-Type": "text/plain" });

    expect(calls[0]?.body).toBe('{"model":"x"}');
  });

  it("splices the metadata into a body over 2 MB without parsing it, and logs that the breakpoints were skipped", async () => {
    onlyoffice();
    useFolder(upstream, "17", "Contracts");
    const calls = useProvider(`${GATEWAY}/chat/completions`, () => HttpResponse.json(completion));
    const rest = `"stream":true,"messages":[{"role":"user","content":"${"x".repeat(2 * MB)}"}]}`;
    const body = `  {${rest}`;

    await post(`${CHAT}?entityId=17`, body);

    expect(calls[0]?.body).toBe(
      `  {"metadata":{"source_id":"17","source_type":"Room","source_title":"Contracts"},${rest}`,
    );
    expect(logger.info).toHaveBeenCalledWith(
      `openai passthrough chat/completions: body ${body.length}B over ${2 * MB}B — prompt-cache breakpoints skipped, metadata still sent`,
    );
  });

  it.each([
    ["a JSON array", '[{"prompt":"a cat"}]'],
    ["text before the object", 'x{"prompt":"a cat"}'],
    ["no object at all", '"a cat"'],
  ])("splices no metadata into an image body that is %s", async (_what, body) => {
    onlyoffice();
    useFolder(upstream, "17", "Contracts");
    const calls = useProvider(`${GATEWAY}/images/generations`, () =>
      HttpResponse.json(imageResult),
    );

    await post(`${IMAGES}?entityId=17`, body);

    expect(calls[0]?.body).toBe(body);
  });

  it("splices the metadata into an image body as sent, with no prompt-cache work", async () => {
    onlyoffice();
    useFolder(upstream, "17", "Contracts");
    const calls = useProvider(`${GATEWAY}/images/generations`, () =>
      HttpResponse.json(imageResult),
    );

    const reply = await post(`${IMAGES}?entityId=17`, '\n {  "prompt": "a cat", "stream": true }');

    expect(reply.status).toBe(200);
    expect(calls[0]?.body).toBe(
      '\n {"metadata":{"source_id":"17","source_type":"Room","source_title":"Contracts"},  "prompt": "a cat", "stream": true }',
    );
  });
});

describe("images/generations", () => {
  it("relays the provider's answer and leaves the body as sent", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/images/generations`, () =>
      HttpResponse.json(imageResult),
    );
    const body = '{ "prompt": "a cat", "n": 1, "size": "1024x1024" }';

    const reply = await post(IMAGES, body);

    expect(reply.status).toBe(200);
    expect(reply.headers["content-type"]).toBe("application/json");
    expect(json(reply)).toEqual(imageResult);
    expect(responseViolations("aiOpenaiImagesGenerations", 200, json(reply))).toEqual([]);
    expect(calls[0]?.body).toBe(body);
    expect(calls[0]?.path).toBe("/v1/images/generations");
  });

  it.each([
    ["no prompt", {}, "prompt is required and must be a non-empty string"],
    ["a null prompt", { prompt: null }, "prompt is required and must be a non-empty string"],
    [
      "a prompt that is a number",
      { prompt: 5 },
      "prompt is required and must be a non-empty string",
    ],
    ["an empty prompt", { prompt: "" }, "prompt is required and must be a non-empty string"],
    ["a blank prompt", { prompt: "   " }, "prompt is required and must be a non-empty string"],
    ["n of 0", { prompt: "p", n: 0 }, "n must be a positive integer"],
    ["a negative n", { prompt: "p", n: -1 }, "n must be a positive integer"],
    ["a fractional n", { prompt: "p", n: 1.5 }, "n must be a positive integer"],
    ["n as a string", { prompt: "p", n: "2" }, "n must be a positive integer"],
    ["a model that is a number", { prompt: "p", model: 1 }, "model must be a string"],
    ["a size that is an object", { prompt: "p", size: {} }, "size must be a string"],
    ["a quality that is a boolean", { prompt: "p", quality: true }, "quality must be a string"],
    ["a style that is an array", { prompt: "p", style: ["vivid"] }, "style must be a string"],
    [
      "a response_format that is a number",
      { prompt: "p", response_format: 1 },
      "response_format must be a string",
    ],
  ])(
    "answers 400 for a body with %s and does not call the provider (Bug 84414)",
    async (_what, body, message) => {
      useProfile();

      const reply = await post(IMAGES, JSON.stringify(body));

      expect(reply.status).toBe(400);
      expect(json(reply)).toEqual(envelope(message, "invalid_request_error"));
      expect(responseViolations("aiOpenaiImagesGenerations", 400, json(reply))).toEqual([]);
      expect(upstream.seen).toHaveLength(1);
    },
  );

  it.each([
    [
      "null optional fields",
      {
        prompt: "p",
        n: null,
        model: null,
        size: null,
        quality: null,
        style: null,
        response_format: null,
      },
    ],
    [
      "n of 3 and every string field",
      {
        prompt: "p",
        n: 3,
        model: "m",
        size: "s",
        quality: "q",
        style: "v",
        response_format: "url",
      },
    ],
    ["fields the route does not check", { prompt: "p", user: 7, background: false }],
  ])("forwards a body with %s", async (_what, body) => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/images/generations`, () =>
      HttpResponse.json(imageResult),
    );

    const reply = await post(IMAGES, JSON.stringify(body));

    expect(reply.status).toBe(200);
    expect(calls[0]?.body).toBe(JSON.stringify(body));
  });

  // DEFECT passthrough-5: a JSON body that is not an object (null, an array, a string) or does not parse skips the field checks of Bug 84414 and is forwarded to the provider — expected 400 naming the body, as for an object without a prompt (ts-ai-failure-paths.md §4)
  it.each([
    ["JSON null", "null"],
    ["a JSON array", "[]"],
    ["a JSON string", '"a cat"'],
    ["broken JSON", '{"prompt":'],
  ])("forwards %s to the provider unchecked", async (_what, body) => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/images/generations`, () =>
      HttpResponse.json(imageResult),
    );

    const reply = await post(IMAGES, body);

    expect(reply.status).toBe(200);
    expect(calls[0]?.body).toBe(body);
  });

  it("does not check a body of another content type", async () => {
    useProfile();
    const calls = useProvider(`${PROVIDER}/images/generations`, () =>
      HttpResponse.json(imageResult),
    );

    const reply = await post(IMAGES, "{}", { ...CALLER, "Content-Type": "text/plain" });

    expect(reply.status).toBe(200);
    expect(calls[0]?.body).toBe("{}");
  });

  it("answers a provider 5xx that comes before the padding window with a fixed 502", async () => {
    useProfile();
    useProvider(`${PROVIDER}/images/generations`, () => new HttpResponse("boom", { status: 500 }));

    const reply = await post(IMAGES, '{"prompt":"p"}');

    expect(reply.status).toBe(502);
    expect(json(reply)).toEqual(envelope("Upstream request failed", "server_error"));
    expect(responseViolations("aiOpenaiImagesGenerations", 502, json(reply))).toEqual([]);
  });

  it("relays a provider 4xx that comes before the padding window with its status", async () => {
    useProfile();
    const providerError = {
      error: { message: "Bad size", type: "invalid_request_error", code: null, param: "size" },
    };
    useProvider(`${PROVIDER}/images/generations`, () =>
      HttpResponse.json(providerError, { status: 400 }),
    );

    const reply = await post(IMAGES, '{"prompt":"p"}');

    expect(reply.status).toBe(400);
    expect(json(reply)).toEqual(providerError);
    expect(responseViolations("aiOpenaiImagesGenerations", 400, json(reply))).toEqual([]);
  });
});
