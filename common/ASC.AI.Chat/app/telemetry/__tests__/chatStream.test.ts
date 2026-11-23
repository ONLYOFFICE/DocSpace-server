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

import { beforeEach, describe, expect, it, vi } from "vitest";

// @opentelemetry/api stood in for by recorders: the tracer, the span, the meter's instruments
// and the context, each created once when chatStream.ts loads, as the real API does.
const otel = vi.hoisted(() => {
  const span = {
    addEvent: vi.fn(),
    setAttribute: vi.fn(),
    recordException: vi.fn(),
    setStatus: vi.fn(),
    end: vi.fn(),
  };
  const instruments: Record<
    string,
    { record: ReturnType<typeof vi.fn>; add: ReturnType<typeof vi.fn> }
  > = {};
  const instrument = (name: string) => {
    instruments[name] = { record: vi.fn(), add: vi.fn() };
    return instruments[name];
  };
  return {
    span,
    instruments,
    startSpan: vi.fn(() => span),
    contextWith: vi.fn((_ctx: unknown, fn: () => unknown) => fn()),
    setSpan: vi.fn((ctx: unknown, s: unknown) => ({ parent: ctx, span: s })),
    instrument,
    created: [] as unknown[][],
  };
});

vi.mock("@opentelemetry/api", () => ({
  SpanStatusCode: { UNSET: 0, OK: 1, ERROR: 2 },
  context: {
    active: () => "ACTIVE",
    with: (ctx: unknown, fn: () => unknown) => otel.contextWith(ctx, fn),
  },
  trace: {
    getTracer: (name: string) => {
      otel.created.push(["tracer", name]);
      return { startSpan: (...args: unknown[]) => otel.startSpan(...(args as [])) };
    },
    setSpan: (ctx: unknown, s: unknown) => otel.setSpan(ctx, s),
  },
  metrics: {
    getMeter: (name: string) => {
      otel.created.push(["meter", name]);
      return {
        createHistogram: (name: string, options: unknown) => {
          otel.created.push(["histogram", name, options]);
          return otel.instrument(name);
        },
        createCounter: (name: string, options: unknown) => {
          otel.created.push(["counter", name, options]);
          return otel.instrument(name);
        },
      };
    },
  },
}));

const { observeChatStream } = await import("../chatStream.js");

const LOADED = [...otel.created];

const latency = () => otel.instruments["ai_chat.latency"];
const generation = () => otel.instruments["ai_chat.generation"];
const rounds = () => otel.instruments["ai_chat.rounds"];

async function* events(...items: unknown[]): AsyncGenerator<unknown> {
  for (const item of items) {
    yield item;
  }
}

async function drain(iter: AsyncIterable<unknown>): Promise<unknown[]> {
  const out: unknown[] = [];
  for await (const event of iter) {
    out.push(event);
  }
  return out;
}

/** performance.now() answers these values one per call. */
function clock(...values: number[]): void {
  const spy = vi.spyOn(performance, "now");
  for (const value of values) {
    spy.mockReturnValueOnce(value);
  }
}

function spanAttributes(): Record<string, unknown> {
  return Object.fromEntries(otel.span.setAttribute.mock.calls.map((c) => [c[0], c[1]]));
}

beforeEach(() => {
  otel.startSpan.mockImplementation(() => otel.span);
  otel.contextWith.mockImplementation((_ctx: unknown, fn: () => unknown) => fn());
  otel.setSpan.mockImplementation((ctx: unknown, s: unknown) => ({ parent: ctx, span: s }));
});

describe("instruments", () => {
  it("creates the tracer, the meter, two histograms and a counter once, when loaded", () => {
    expect(LOADED).toEqual([
      ["tracer", "asc-ai-chat"],
      ["meter", "asc-ai-chat"],
      [
        "histogram",
        "ai_chat.latency",
        { unit: "ms", description: "Time from round start to the first model chunk" },
      ],
      [
        "histogram",
        "ai_chat.generation",
        { unit: "ms", description: "Time from the first model chunk to the end of the stream" },
      ],
      ["counter", "ai_chat.rounds", { description: "Streaming chat rounds by terminal outcome" }],
    ]);
  });
});

describe("observeChatStream, chat dialect", () => {
  it("yields every event and records the round with its latency and generation time", async () => {
    clock(100, 150.4, 400.6);
    const input = [
      { type: "user-message-stored" },
      { type: "message-start" },
      { type: "text-delta", delta: "hi" },
      { type: "message-end" },
    ];

    const observed = observeChatStream("/ai/chat", events(...input));
    expect(performance.now).toHaveBeenCalledTimes(1);
    expect(otel.startSpan).toHaveBeenCalledWith("/ai/chat", {
      attributes: { "ai_chat.route": "/ai/chat" },
    });
    expect(otel.setSpan).toHaveBeenCalledWith("ACTIVE", otel.span);

    await expect(drain(observed)).resolves.toEqual(input);

    expect(otel.contextWith).toHaveBeenCalledTimes(5);
    for (const call of otel.contextWith.mock.calls) {
      expect(call[0]).toEqual({ parent: "ACTIVE", span: otel.span });
    }
    expect(latency()?.record.mock.calls).toEqual([[50.400000000000006, { route: "/ai/chat" }]]);
    expect(generation()?.record.mock.calls).toEqual([[250.20000000000002, { route: "/ai/chat" }]]);
    expect(rounds()?.add.mock.calls).toEqual([[1, { route: "/ai/chat", outcome: "completed" }]]);
    expect(otel.span.addEvent.mock.calls).toEqual([["gen_ai.first_token"]]);
    expect(otel.span.setAttribute.mock.calls).toEqual([
      ["ai_chat.timing.latency_ms", 50],
      ["ai_chat.timing.generation_ms", 250],
      ["ai_chat.outcome", "completed"],
    ]);
    expect(otel.span.setStatus).not.toHaveBeenCalled();
    expect(otel.span.recordException).not.toHaveBeenCalled();
    expect(otel.span.end).toHaveBeenCalledTimes(1);
  });

  it("times only the first message-start", async () => {
    clock(0, 10, 20, 30);

    await drain(
      observeChatStream(
        "r",
        events({ type: "message-start" }, { type: "message-start" }, { type: "message-end" }),
      ),
    );

    expect(latency()?.record.mock.calls).toEqual([[10, { route: "r" }]]);
    expect(otel.span.addEvent).toHaveBeenCalledTimes(1);
    expect(generation()?.record.mock.calls).toEqual([[10, { route: "r" }]]);
  });

  it("records no latency and no generation time for a round without a model chunk, and its outcome as none", async () => {
    await drain(observeChatStream("r", events({ type: "user-message-stored" }, null, "x", 5)));

    expect(latency()?.record).not.toHaveBeenCalled();
    expect(generation()?.record).not.toHaveBeenCalled();
    expect(rounds()?.add.mock.calls).toEqual([[1, { route: "r", outcome: "none" }]]);
    expect(spanAttributes()).toEqual({ "ai_chat.outcome": "none" });
    expect(otel.span.end).toHaveBeenCalledTimes(1);
  });

  it("treats an openai chunk as no model chunk", async () => {
    await drain(
      observeChatStream(
        "r",
        events({ object: "chat.completion.chunk", choices: [{ finish_reason: "stop" }] }),
      ),
    );

    expect(latency()?.record).not.toHaveBeenCalled();
    expect(rounds()?.add.mock.calls).toEqual([[1, { route: "r", outcome: "none" }]]);
  });

  it.each<[string, unknown[], string, boolean]>([
    ["message-end", [{ type: "message-end" }], "completed", false],
    ["tool-call-pending", [{ type: "tool-call-pending" }], "tool-pending", false],
    [
      "an incomplete message with its reason",
      [{ type: "message-incomplete", message: { status: { reason: "length" } } }],
      "incomplete-length",
      false,
    ],
    [
      "an incomplete message failing with error",
      [{ type: "message-incomplete", message: { status: { reason: "error" } } }],
      "incomplete-error",
      true,
    ],
    [
      "an incomplete message without a message",
      [{ type: "message-incomplete" }],
      "incomplete-unknown",
      false,
    ],
    [
      "an incomplete message with a non-object message",
      [{ type: "message-incomplete", message: "m" }],
      "incomplete-unknown",
      false,
    ],
    [
      "an incomplete message without a status",
      [{ type: "message-incomplete", message: {} }],
      "incomplete-unknown",
      false,
    ],
    [
      "an incomplete message with a non-object status",
      [{ type: "message-incomplete", message: { status: "x" } }],
      "incomplete-unknown",
      false,
    ],
    [
      "an incomplete message with a numeric reason",
      [{ type: "message-incomplete", message: { status: { reason: 3 } } }],
      "incomplete-unknown",
      false,
    ],
    [
      "the last terminal event",
      [{ type: "message-end" }, { type: "tool-call-pending" }],
      "tool-pending",
      false,
    ],
    [
      "a terminal event followed by others",
      [{ type: "message-end" }, { type: "text-delta" }, null],
      "completed",
      false,
    ],
  ])("records the outcome of %s", async (_name, input, outcome, failed) => {
    await drain(observeChatStream("r", events(...input)));

    expect(rounds()?.add.mock.calls).toEqual([[1, { route: "r", outcome }]]);
    expect(spanAttributes()["ai_chat.outcome"]).toBe(outcome);
    if (failed) {
      expect(otel.span.setStatus.mock.calls).toEqual([[{ code: 2 }]]);
    } else {
      expect(otel.span.setStatus).not.toHaveBeenCalled();
    }
  });
});

describe("observeChatStream, openai dialect", () => {
  it("times the first chat.completion.chunk and records completed on a finish_reason", async () => {
    clock(0, 5, 25);
    const input = [
      {
        object: "chat.completion.chunk",
        choices: [{ delta: { content: "a" }, finish_reason: null }],
      },
      { object: "chat.completion.chunk", choices: [{ delta: {}, finish_reason: "stop" }] },
    ];

    await expect(
      drain(observeChatStream("/v1/chat/completions", events(...input), "openai")),
    ).resolves.toEqual(input);

    expect(latency()?.record.mock.calls).toEqual([[5, { route: "/v1/chat/completions" }]]);
    expect(generation()?.record.mock.calls).toEqual([[20, { route: "/v1/chat/completions" }]]);
    expect(rounds()?.add.mock.calls).toEqual([
      [1, { route: "/v1/chat/completions", outcome: "completed" }],
    ]);
  });

  it.each<[string, unknown, string, boolean]>([
    ["an error object", { error: { message: "x" } }, "incomplete-error", true],
    ["an error that is a string", { error: "x" }, "none", false],
    ["no choices", { object: "chat.completion.chunk" }, "none", false],
    ["choices that are not a list", { choices: { finish_reason: "stop" } }, "none", false],
    ["an empty choices list", { choices: [] }, "none", false],
    ["a first choice that is not an object", { choices: ["stop"] }, "none", false],
    ["a null finish_reason", { choices: [{ finish_reason: null }] }, "none", false],
    [
      "a finish_reason on the second choice only",
      { choices: [{}, { finish_reason: "stop" }] },
      "none",
      false,
    ],
    [
      "a finish_reason without the chunk object",
      { choices: [{ finish_reason: "length" }] },
      "completed",
      false,
    ],
    ["a non-object event", "data", "none", false],
    ["a chat message-end", { type: "message-end" }, "none", false],
  ])("records the outcome of %s", async (_name, event, outcome, failed) => {
    await drain(observeChatStream("r", events(event), "openai"));

    expect(rounds()?.add.mock.calls).toEqual([[1, { route: "r", outcome }]]);
    expect(otel.span.setStatus).toHaveBeenCalledTimes(failed ? 1 : 0);
  });

  it("does not time a chat message-start", async () => {
    await drain(observeChatStream("r", events({ type: "message-start" }), "openai"));

    expect(latency()?.record).not.toHaveBeenCalled();
  });
});

describe("observeChatStream, a stream that fails or stops early", () => {
  it("records stream-error, marks the span failed and rethrows the error", async () => {
    clock(0, 1, 9);
    const failure = new Error("provider reset");
    async function* failing(): AsyncGenerator<unknown> {
      yield { type: "message-start" };
      yield { type: "message-end" };
      throw failure;
    }

    const seen: unknown[] = [];
    await expect(
      (async () => {
        for await (const event of observeChatStream("r", failing())) {
          seen.push(event);
        }
      })(),
    ).rejects.toBe(failure);

    expect(seen).toEqual([{ type: "message-start" }, { type: "message-end" }]);
    expect(otel.span.recordException.mock.calls).toEqual([[failure]]);
    expect(otel.span.setStatus.mock.calls).toEqual([[{ code: 2 }]]);
    expect(rounds()?.add.mock.calls).toEqual([[1, { route: "r", outcome: "stream-error" }]]);
    expect(generation()?.record.mock.calls).toEqual([[8, { route: "r" }]]);
    expect(otel.span.end).toHaveBeenCalledTimes(1);
  });

  it("records a thrown non-Error as an Error with its text", async () => {
    async function* failing(): AsyncGenerator<unknown> {
      yield* [];
      throw "socket closed" as unknown as Error;
    }

    await expect(drain(observeChatStream("r", failing()))).rejects.toBe("socket closed");

    const recorded = otel.span.recordException.mock.calls[0]?.[0] as unknown;
    expect(recorded).toBeInstanceOf(Error);
    expect((recorded as Error).message).toBe("socket closed");
  });

  it("starts the span at the call even when the stream is never read", () => {
    observeChatStream("r", events());

    expect(otel.startSpan).toHaveBeenCalledTimes(1);
    expect(otel.span.end).not.toHaveBeenCalled();
  });

  // DEFECT tools-9: when the consumer stops early the wrapper ends its span but never calls return() on the wrapped iterator, so the source stream stays suspended and its own cleanup (finally) does not run — expected the source to be closed with the wrapper (chatStream.ts:117-156)
  it("ends the span with the outcome so far when the consumer stops early, and leaves the source open", async () => {
    let sourceClosed = false;
    async function* source(): AsyncGenerator<unknown> {
      try {
        yield { type: "message-start" };
        yield { type: "tool-call-pending" };
        yield { type: "message-end" };
      } finally {
        sourceClosed = true;
      }
    }

    for await (const event of observeChatStream("r", source())) {
      if (isPending(event)) {
        break;
      }
    }

    expect(rounds()?.add.mock.calls).toEqual([[1, { route: "r", outcome: "tool-pending" }]]);
    expect(otel.span.end).toHaveBeenCalledTimes(1);
    expect(sourceClosed).toBe(false);
  });
});

function isPending(event: unknown): boolean {
  return (
    typeof event === "object" &&
    event !== null &&
    (event as { type?: unknown }).type === "tool-call-pending"
  );
}
