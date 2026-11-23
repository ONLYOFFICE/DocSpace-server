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

import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { delay, http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { ioTurn, listen, send, until } from "../../__tests__/helpers/rawHttp.js";
import type { Exchange } from "../../__tests__/helpers/rawHttp.js";
import {
  CALLER,
  CHAT,
  completion,
  IMAGES,
  imageResult,
  PROVIDER,
  profileDto,
  useProfile,
  useProvider,
} from "../../__tests__/fixtures/providers.js";

// The OpenAI passthrough over time: the SSE relay and its keep-alive, the grace window before
// the provider's headers (10 s for SSE, 20 s for the padded JSON of images/generations), what
// is written after the 200 is committed, and a client that goes away. The clock is fake and
// driven by the test; socket I/O runs on the real event loop (`until`, `ioTurn`).

// The access log is not what these tests are about.
vi.mock("morgan", () => ({
  default: () => (_req: unknown, _res: unknown, next: () => void) => next(),
}));

const upstream = useUpstream();
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
  vi.useFakeTimers({
    toFake: ["setTimeout", "clearTimeout", "setInterval", "clearInterval", "Date"],
  });
  vi.mocked(logger.info).mockClear();
  vi.mocked(logger.warn).mockClear();
  vi.mocked(logger.error).mockClear();
});
afterEach(() => {
  vi.useRealTimers();
});

const SSE_PING = ": ping\n\n";
const encoder = new TextEncoder();

/** A body the test writes chunk by chunk. */
function controlledStream(): {
  stream: ReadableStream<Uint8Array>;
  push: (text: string) => void;
  close: () => void;
  fail: (err: Error) => void;
} {
  let controller: ReadableStreamDefaultController<Uint8Array> | undefined;
  const stream = new ReadableStream<Uint8Array>({
    start(c) {
      controller = c;
    },
  });
  return {
    stream,
    push: (text) => controller?.enqueue(encoder.encode(text)),
    close: () => controller?.close(),
    fail: (err) => controller?.error(err),
  };
}

/** An answer the test releases when it wants the provider to speak. */
function heldAnswer(): { answer: () => Promise<Response>; release: (res: Response) => void } {
  let release: (res: Response) => void = () => undefined;
  const pending = new Promise<Response>((resolve) => {
    release = resolve;
  });
  return { answer: () => pending, release };
}

const sse = (stream: ReadableStream<Uint8Array>, status = 200) =>
  new HttpResponse(stream, { status, headers: { "Content-Type": "text/event-stream" } });

function start(path: string, body: string, headers: Record<string, string> = CALLER): Exchange {
  return send(server.port, { path, headers, body });
}

/** Lets the real event loop deliver whatever the server has written. */
async function settle(): Promise<void> {
  for (let i = 0; i < 30; i += 1) {
    await ioTurn();
  }
}

const bodyOf = (exchange: Exchange) => exchange.current()?.body;

const streamBody = '{"model":"gpt-4o","stream":true,"messages":[{"role":"user","content":"Hi"}]}';

describe("the SSE relay", () => {
  it("relays the provider's events byte for byte as they arrive", async () => {
    useProfile(upstream);
    const provider = controlledStream();
    useProvider(upstream, `${PROVIDER}/chat/completions`, () => sse(provider.stream));
    const exchange = start(CHAT, streamBody);

    await until(() => upstream.seen.length === 2);
    provider.push('data: {"a":1}\n\n');
    await until(() => bodyOf(exchange) === 'data: {"a":1}\n\n');
    provider.push('data: {"b"');
    provider.push(":2}\n\ndata: [DONE]\n\n");
    provider.close();
    const reply = await exchange.done;

    expect(reply.status).toBe(200);
    expect(reply.headers["content-type"]).toBe("text/event-stream");
    expect(reply.headers["cache-control"]).toBe("no-cache, no-transform");
    expect(reply.headers["x-accel-buffering"]).toBe("no");
    expect(reply.body).toBe('data: {"a":1}\n\ndata: {"b":2}\n\ndata: [DONE]\n\n');
  });

  it("writes a ping after 10 s of silence at a frame boundary, and none inside a half-written line", async () => {
    useProfile(upstream);
    const provider = controlledStream();
    useProvider(upstream, `${PROVIDER}/chat/completions`, () => sse(provider.stream));
    const exchange = start(CHAT, streamBody);
    await until(() => upstream.seen.length === 2);

    provider.push("data: 1\n\n");
    await until(() => bodyOf(exchange) === "data: 1\n\n");
    await vi.advanceTimersByTimeAsync(7_500);
    await settle();
    expect(bodyOf(exchange)).toBe("data: 1\n\n");
    await vi.advanceTimersByTimeAsync(2_500);
    await until(() => bodyOf(exchange) === `data: 1\n\n${SSE_PING}`);

    provider.push('data: {"x"');
    await until(() => bodyOf(exchange) === `data: 1\n\n${SSE_PING}data: {"x"`);
    await vi.advanceTimersByTimeAsync(30_000);
    await settle();
    expect(bodyOf(exchange)).toBe(`data: 1\n\n${SSE_PING}data: {"x"`);

    provider.push(":1}\n\n");
    provider.close();
    const reply = await exchange.done;

    expect(reply.body).toBe(`data: 1\n\n${SSE_PING}data: {"x":1}\n\n`);
  });

  it("ends an event-stream answer that has no body", async () => {
    useProfile(upstream);
    useProvider(
      upstream,
      `${PROVIDER}/chat/completions`,
      () =>
        new HttpResponse(null, { status: 200, headers: { "Content-Type": "text/event-stream" } }),
    );

    const reply = await start(CHAT, streamBody).done;

    expect(reply.status).toBe(200);
    expect(reply.headers["content-type"]).toBe("text/event-stream");
    expect(reply.body).toBe("");
  });

  it("ends the reply and logs a warning when the provider's stream breaks", async () => {
    useProfile(upstream);
    const provider = controlledStream();
    useProvider(upstream, `${PROVIDER}/chat/completions`, () => sse(provider.stream));
    const exchange = start(CHAT, streamBody);
    await until(() => upstream.seen.length === 2);

    provider.push("data: 1\n\n");
    await until(() => bodyOf(exchange) === "data: 1\n\n");
    provider.fail(new Error("connection reset"));
    const reply = await exchange.done;

    expect(reply.body).toBe("data: 1\n\n");
    expect(logger.warn).toHaveBeenCalledWith(
      "openai passthrough: stream ended early: Error: connection reset",
    );
  });
});

describe("the grace window of a streaming round", () => {
  async function graceCommitted(
    path = CHAT,
    body = streamBody,
    headers: Record<string, string> = CALLER,
  ) {
    useProfile(upstream);
    const held = heldAnswer();
    const calls = useProvider(upstream, `${PROVIDER}/chat/completions`, held.answer);
    const exchange = start(path, body, headers);
    await until(() => calls.length === 1);
    await vi.advanceTimersByTimeAsync(10_000);
    await until(() => bodyOf(exchange) === SSE_PING);
    return { exchange, held, calls };
  }

  it("sends nothing for 10 s, then commits a 200 event stream with a ping and keeps pinging", async () => {
    useProfile(upstream);
    const held = heldAnswer();
    const calls = useProvider(upstream, `${PROVIDER}/chat/completions`, held.answer);
    const exchange = start(CHAT, streamBody);
    await until(() => calls.length === 1);

    await vi.advanceTimersByTimeAsync(9_999);
    await settle();
    expect(exchange.current()).toBeUndefined();

    await vi.advanceTimersByTimeAsync(1);
    await until(() => bodyOf(exchange) === SSE_PING);
    const head = exchange.current();
    expect(head?.status).toBe(200);
    expect(head?.headers["content-type"]).toBe("text/event-stream; charset=utf-8");
    expect(head?.headers["cache-control"]).toBe("no-cache, no-transform");
    expect(head?.headers["x-accel-buffering"]).toBe("no");
    expect(logger.info).toHaveBeenCalledWith(
      "openai passthrough chat/completions: no provider headers after 10000ms — keeping the response alive (sse)",
    );

    await vi.advanceTimersByTimeAsync(10_000);
    await until(() => bodyOf(exchange) === SSE_PING + SSE_PING);

    const provider = controlledStream();
    held.release(sse(provider.stream));
    provider.push("data: [DONE]\n\n");
    provider.close();
    const reply = await exchange.done;

    expect(reply.body).toBe(`${SSE_PING}${SSE_PING}data: [DONE]\n\n`);
  });

  it("keeps a round alive that asks for an event stream by its Accept header alone", async () => {
    const { exchange, held } = await graceCommitted(CHAT, '{"model":"gpt-4o"}', {
      ...CALLER,
      Accept: "text/event-stream",
    });

    held.release(sse(new Blob(["data: 1\n\n"]).stream()));
    const reply = await exchange.done;

    expect(reply.body).toBe(`${SSE_PING}data: 1\n\n`);
  });

  it("re-frames a provider 4xx with an OpenAI envelope as one compacted event", async () => {
    const { exchange, held } = await graceCommitted();
    const providerError = {
      error: { message: "Rate limit", type: "requests", code: "rate_limit_exceeded", param: null },
    };

    held.release(
      new HttpResponse(JSON.stringify(providerError, null, 2), {
        status: 429,
        headers: { "Content-Type": "application/json" },
      }),
    );
    const reply = await exchange.done;

    expect(reply.status).toBe(200);
    expect(reply.body).toBe(`${SSE_PING}data: ${JSON.stringify(providerError)}\n\n`);
    expect(vi.mocked(logger.warn).mock.calls[0]?.[0]).toBe(
      `openai passthrough chat/completions: provider answered 429 application/json after the keep-alive was committed: ${JSON.stringify(providerError, null, 2)}`,
    );
  });

  it.each([
    [
      "a 5xx with an OpenAI envelope",
      503,
      '{"error":{"message":"at Internal.Stack()"}}',
      "application/json",
    ],
    ["a 4xx that is not JSON", 403, "denied", "text/plain"],
    ["a 4xx whose JSON has no error object", 400, '{"message":"bad"}', "application/json"],
    ["a 200 that is not an event stream", 200, JSON.stringify(completion), "application/json"],
  ])("re-frames %s as the generic error event", async (_what, status, text, contentType) => {
    const { exchange, held } = await graceCommitted();

    held.release(new HttpResponse(text, { status, headers: { "Content-Type": contentType } }));
    const reply = await exchange.done;

    expect(reply.status).toBe(200);
    expect(reply.body).toBe(
      `${SSE_PING}data: ${JSON.stringify({
        error: {
          message: `Upstream request failed with status ${status}`,
          type: "server_error",
          code: null,
          param: null,
        },
      })}\n\n`,
    );
    expect(reply.body).not.toContain("Internal.Stack");
  });

  it("logs a provider answer without a content type as such", async () => {
    const { exchange, held } = await graceCommitted();

    held.release(new HttpResponse(encoder.encode("x"), { status: 500 }));
    await exchange.done;

    expect(vi.mocked(logger.warn).mock.calls[0]?.[0]).toBe(
      "openai passthrough chat/completions: provider answered 500 no content-type after the keep-alive was committed: x",
    );
  });

  it("writes the generic 502 event when the provider cannot be reached after the commit", async () => {
    const { exchange, held } = await graceCommitted();

    held.release(HttpResponse.error());
    const reply = await exchange.done;

    expect(reply.body).toBe(
      `${SSE_PING}data: ${JSON.stringify({
        error: {
          message: "Upstream request failed with status 502",
          type: "server_error",
          code: null,
          param: null,
        },
      })}\n\n`,
    );
    expect(vi.mocked(logger.error).mock.calls[0]?.[0]).toMatch(
      /^openai passthrough: upstream fetch failed: /,
    );
  });

  it("aborts the provider call when the client goes away after the commit, and logs no error", async () => {
    const { exchange, calls } = await graceCommitted();

    exchange.abort();
    await until(() => calls[0]?.signal.aborted === true);
    await vi.advanceTimersByTimeAsync(30_000);
    await settle();

    expect(logger.error).not.toHaveBeenCalled();
  });
});

describe("a client that goes away before the provider answers", () => {
  it("aborts the provider call and answers nothing", async () => {
    useProfile(upstream);
    const calls = useProvider(upstream, `${PROVIDER}/chat/completions`, async () => {
      await delay("infinite");
      return HttpResponse.json(completion);
    });
    const exchange = start(CHAT, streamBody);
    await until(() => calls.length === 1);

    exchange.abort();
    await until(() => calls[0]?.signal.aborted === true);
    await vi.advanceTimersByTimeAsync(10_000);
    await settle();

    expect(logger.info).not.toHaveBeenCalledWith(
      expect.stringContaining("keeping the response alive"),
    );
    expect(logger.error).not.toHaveBeenCalled();
  });
});

describe("a round that is not streamed", () => {
  it("is not kept alive: nothing is sent until the provider answers", async () => {
    useProfile(upstream);
    const held = heldAnswer();
    const calls = useProvider(upstream, `${PROVIDER}/chat/completions`, held.answer);
    const exchange = start(CHAT, '{"model":"gpt-4o","messages":[]}');
    await until(() => calls.length === 1);

    await vi.advanceTimersByTimeAsync(60_000);
    await settle();
    expect(exchange.current()).toBeUndefined();

    held.release(HttpResponse.json(completion));
    const reply = await exchange.done;

    expect(reply.status).toBe(200);
    expect(reply.body).toBe(JSON.stringify(completion));
  });
});

describe("the padded JSON of images/generations", () => {
  async function padCommitted(body = '{"prompt":"a cat"}') {
    useProfile(upstream);
    const held = heldAnswer();
    const calls = useProvider(upstream, `${PROVIDER}/images/generations`, held.answer);
    const exchange = start(IMAGES, body);
    await until(() => calls.length === 1);
    await vi.advanceTimersByTimeAsync(20_000);
    await until(() => bodyOf(exchange) === " ");
    return { exchange, held };
  }

  it("sends nothing for 20 s, then commits a 200 JSON reply padded with spaces until the provider answers", async () => {
    useProfile(upstream);
    const held = heldAnswer();
    const calls = useProvider(upstream, `${PROVIDER}/images/generations`, held.answer);
    const exchange = start(IMAGES, '{"prompt":"a cat"}');
    await until(() => calls.length === 1);

    await vi.advanceTimersByTimeAsync(19_999);
    await settle();
    expect(exchange.current()).toBeUndefined();

    await vi.advanceTimersByTimeAsync(1);
    await until(() => bodyOf(exchange) === " ");
    const head = exchange.current();
    expect(head?.status).toBe(200);
    expect(head?.headers["content-type"]).toBe("application/json; charset=utf-8");
    expect(head?.headers["cache-control"]).toBe("no-cache, no-transform");
    expect(head?.headers["x-accel-buffering"]).toBe("no");
    expect(logger.info).toHaveBeenCalledWith(
      "openai passthrough images/generations: no provider headers after 20000ms — keeping the response alive (json)",
    );

    await vi.advanceTimersByTimeAsync(10_000);
    await until(() => bodyOf(exchange) === "  ");

    held.release(HttpResponse.json(imageResult));
    const reply = await exchange.done;

    expect(reply.body).toBe(`  ${JSON.stringify(imageResult)}`);
    expect(responseViolations("aiOpenaiImagesGenerations", 200, JSON.parse(reply.body))).toEqual(
      [],
    );
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("pads a body that asks for a stream as well: the route always answers JSON", async () => {
    const { exchange, held } = await padCommitted('{"prompt":"a cat","stream":true}');

    held.release(HttpResponse.json(imageResult));
    const reply = await exchange.done;

    expect(reply.headers["content-type"]).toBe("application/json; charset=utf-8");
    expect(reply.body).toBe(` ${JSON.stringify(imageResult)}`);
  });

  it("writes the generic envelope after the padding when the provider answers 5xx, and logs its body", async () => {
    const { exchange, held } = await padCommitted();

    held.release(new HttpResponse("at Gateway.Stack()", { status: 500 }));
    const reply = await exchange.done;

    expect(reply.status).toBe(200);
    expect(JSON.parse(reply.body)).toEqual({
      error: { message: "Upstream request failed", type: "server_error", code: null, param: null },
    });
    expect(reply.body).not.toContain("Gateway.Stack");
    expect(logger.error).toHaveBeenCalledWith(
      "openai passthrough images/generations: upstream answered 500: at Gateway.Stack()",
    );
  });

  it("relays a provider 4xx inside the 200 and logs the downgrade", async () => {
    const { exchange, held } = await padCommitted();
    const providerError = {
      error: { message: "Bad size", type: "invalid_request_error", code: null, param: "size" },
    };

    held.release(HttpResponse.json(providerError, { status: 400 }));
    const reply = await exchange.done;

    expect(reply.status).toBe(200);
    expect(reply.body).toBe(` ${JSON.stringify(providerError)}`);
    expect(logger.warn).toHaveBeenCalledWith(
      "openai passthrough images/generations: provider answered 400 application/json after the keep-alive was committed — status downgraded to 200",
    );
  });

  it("logs a provider 4xx without a content type after the padding as such", async () => {
    const { exchange, held } = await padCommitted();

    held.release(new HttpResponse(encoder.encode("denied"), { status: 403 }));
    const reply = await exchange.done;

    expect(reply.body).toBe(" denied");
    expect(logger.warn).toHaveBeenCalledWith(
      "openai passthrough images/generations: provider answered 403 no content-type after the keep-alive was committed — status downgraded to 200",
    );
  });

  it("relays a 200 that is not JSON and logs it", async () => {
    const { exchange, held } = await padCommitted();

    held.release(
      new HttpResponse("plain", { status: 200, headers: { "Content-Type": "text/plain" } }),
    );
    const reply = await exchange.done;

    expect(reply.body).toBe(" plain");
    expect(logger.warn).toHaveBeenCalledWith(
      "openai passthrough images/generations: provider answered 200 text/plain after the keep-alive was committed — status downgraded to 200",
    );
  });

  it("relays an answer without a content type as JSON, without a warning", async () => {
    const { exchange, held } = await padCommitted();

    held.release(new HttpResponse(encoder.encode(JSON.stringify(imageResult)), { status: 200 }));
    const reply = await exchange.done;

    expect(reply.body).toBe(` ${JSON.stringify(imageResult)}`);
    expect(logger.warn).not.toHaveBeenCalled();
  });

  it("logs an event stream that meets the padded reply and relays it as it is", async () => {
    const { exchange, held } = await padCommitted();

    held.release(sse(new Blob(["data: 1\n\n"]).stream()));
    const reply = await exchange.done;

    expect(reply.headers["content-type"]).toBe("application/json; charset=utf-8");
    expect(reply.body).toBe(" data: 1\n\n");
    expect(logger.error).toHaveBeenCalledWith(
      "openai passthrough images/generations: padded reply met a text/event-stream body — the round cannot be relayed",
    );
  });

  it("writes the generic envelope after the padding when the provider cannot be reached", async () => {
    const { exchange, held } = await padCommitted();

    held.release(HttpResponse.error());
    const reply = await exchange.done;

    expect(reply.body).toBe(
      ` ${JSON.stringify({
        error: {
          message: "Upstream request failed",
          type: "server_error",
          code: null,
          param: null,
        },
      })}`,
    );
  });

  it("ends an answer without a body after the padding", async () => {
    const { exchange, held } = await padCommitted();

    held.release(new HttpResponse(null, { status: 204 }));
    const reply = await exchange.done;

    expect(reply.status).toBe(200);
    expect(reply.body).toBe(" ");
  });
});

describe("a profile read that never answers", () => {
  // DEFECT passthrough-3 (as above): the timeout of the profile read is answered 500 `{ error: "Internal server error" }`, not the OpenAI envelope published for 500 (ts-ai-tests.md §2)
  it("answers 500 once the upstream timeout of 300 s has passed, and never calls the provider", async () => {
    upstream.use(
      http.get(`${aiServiceBaseUrl}/internal/ai/profiles/:id`, async () => {
        await delay("infinite");
        return HttpResponse.json(profileDto());
      }),
    );
    const exchange = start(CHAT, streamBody);
    await until(() => upstream.seen.length === 1);

    await vi.advanceTimersByTimeAsync(299_999);
    await settle();
    expect(exchange.current()).toBeUndefined();
    await vi.advanceTimersByTimeAsync(1);
    const reply = await exchange.done;

    expect(reply.status).toBe(500);
    expect(JSON.parse(reply.body)).toEqual({ error: "Internal server error" });
    expect(responseViolations("aiOpenaiChatCompletions", 500, JSON.parse(reply.body))).toEqual([
      "/error must be object",
    ]);
    expect(upstream.seen).toHaveLength(1);
  });
});
