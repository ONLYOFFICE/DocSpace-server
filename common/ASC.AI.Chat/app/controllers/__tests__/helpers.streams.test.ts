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

// The two stream framings and the keep-alive under them: what a client reads on a normal end,
// on a failure before the first event, on a failure mid-way, and while the stream is silent.

import { EventEmitter } from "node:events";
import type { Response } from "express";
import { createResponse } from "node-mocks-http";
import type { MockResponse } from "node-mocks-http";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import logger from "../../log.js";
import { startStreamHeartbeat, streamNdjson, streamOpenAiSse } from "../_helpers.js";

const response = (): MockResponse<Response> & { flushHeaders: ReturnType<typeof vi.fn> } =>
  Object.assign(createResponse<Response>({ eventEmitter: EventEmitter }), {
    flushHeaders: vi.fn(),
  });

async function* events(...items: unknown[]): AsyncGenerator<unknown> {
  for (const item of items) {
    yield item;
  }
}

async function* failing(error: unknown, ...before: unknown[]): AsyncGenerator<unknown> {
  yield* events(...before);
  throw error;
}

// A stream that yields `first`, then stays silent until `release()`, then yields `last`.
function held(
  first: unknown,
  last: unknown,
): { stream: AsyncGenerator<unknown>; release: () => void } {
  let release: () => void = () => {};
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  async function* run(): AsyncGenerator<unknown> {
    yield first;
    await gate;
    yield last;
  }
  return { stream: run(), release };
}

beforeEach(() => {
  vi.clearAllMocks();
});

afterEach(() => {
  vi.useRealTimers();
});

describe("streamNdjson", () => {
  it("writes one JSON line per event after the stream headers, and ends", async () => {
    const res = response();

    await streamNdjson(res, events({ type: "message-start" }, "two", 3, null));

    expect(res._getHeaders()).toEqual({
      "content-type": "application/x-ndjson; charset=utf-8",
      "cache-control": "no-cache, no-transform",
      "x-accel-buffering": "no",
    });
    expect(res.flushHeaders).toHaveBeenCalledTimes(1);
    expect(res.statusCode).toBe(200);
    expect(res._getData()).toBe('{"type":"message-start"}\n"two"\n3\nnull\n');
    expect(res._isEndCalled()).toBe(true);
  });

  it("ends an empty stream with the headers and no line", async () => {
    const res = response();

    await streamNdjson(res, events());

    expect(res._getHeaders()["content-type"]).toBe("application/x-ndjson; charset=utf-8");
    expect(res._getData()).toBe("");
    expect(res._isEndCalled()).toBe(true);
  });

  it("rejects before any header when the first event fails, leaving the answer to the caller", async () => {
    const res = response();

    await expect(streamNdjson(res, failing(new Error("no thread")))).rejects.toThrow("no thread");

    expect(res._getHeaders()).toEqual({});
    expect(res.headersSent).toBe(false);
    expect(res._isEndCalled()).toBe(false);
  });

  it("writes a generic error line when the stream fails mid-way, and ends", async () => {
    const res = response();

    await streamNdjson(
      res,
      failing(new Error("upstream http://10.0.0.7 said: key sk-123"), { type: "message-start" }),
    );

    expect(res._getData()).toBe(
      '{"type":"message-start"}\n{"type":"error","message":"stream error"}\n',
    );
    expect(res._isEndCalled()).toBe(true);
    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      expect.stringMatching(
        /^stream aborted: Error: upstream http:\/\/10\.0\.0\.7 said: key sk-123\n/,
      ),
    );
  });

  it("works with a response that cannot flush its headers", async () => {
    const res = createResponse<Response>({ eventEmitter: EventEmitter });

    await streamNdjson(res, events(1));

    expect(res._getData()).toBe("1\n");
  });

  it("sends a blank line after ten silent seconds, and none before", async () => {
    vi.useFakeTimers();
    const res = response();
    const stream = held({ type: "message-start" }, { type: "message-end" });

    const done = streamNdjson(res, stream.stream);
    await vi.advanceTimersByTimeAsync(9_999);
    expect(res._getData()).toBe('{"type":"message-start"}\n');

    await vi.advanceTimersByTimeAsync(1);
    expect(res._getData()).toBe('{"type":"message-start"}\n\n');

    stream.release();
    await done;
    expect(res._getData()).toBe('{"type":"message-start"}\n\n{"type":"message-end"}\n');
    expect(vi.getTimerCount()).toBe(0);
  });

  // DEFECT ai-14: the result of res.write is never read, so a slow client buffers the whole stream in memory — expected a wait for `drain` when write answers false (ts-ai-express.md §3)
  it("keeps writing when the response asks it to wait", async () => {
    const res = response();
    const write = vi.fn(() => false);
    res.write = write as never;

    await streamNdjson(res, events(1, 2, 3));

    expect(write).toHaveBeenCalledTimes(3);
    expect(res.listenerCount("drain")).toBe(0);
  });
});

describe("streamOpenAiSse", () => {
  it("writes one data frame per chunk and the [DONE] sentinel, and ends", async () => {
    const res = response();

    await streamOpenAiSse(res, events({ id: "c1" }, { id: "c2" }));

    expect(res._getHeaders()).toEqual({
      "content-type": "text/event-stream; charset=utf-8",
      "cache-control": "no-cache, no-transform",
      connection: "keep-alive",
      "x-accel-buffering": "no",
    });
    expect(res.flushHeaders).toHaveBeenCalledTimes(1);
    expect(res._getData()).toBe('data: {"id":"c1"}\n\ndata: {"id":"c2"}\n\ndata: [DONE]\n\n');
    expect(res._isEndCalled()).toBe(true);
  });

  it("ends an empty stream with [DONE] alone", async () => {
    const res = response();

    await streamOpenAiSse(res, events());

    expect(res._getData()).toBe("data: [DONE]\n\n");
  });

  it("rejects before any header when the first chunk fails", async () => {
    const res = response();

    await expect(streamOpenAiSse(res, failing(new Error("refused")))).rejects.toThrow("refused");

    expect(res._getHeaders()).toEqual({});
    expect(res._isEndCalled()).toBe(false);
  });

  it("writes a generic OpenAI error frame and no [DONE] when the stream fails mid-way", async () => {
    const res = response();

    await streamOpenAiSse(res, failing(new Error("provider key sk-999 rejected"), { id: "c1" }));

    expect(res._getData()).toBe(
      'data: {"id":"c1"}\n\ndata: {"error":{"message":"stream error","type":"server_error"}}\n\n',
    );
    expect(res._isEndCalled()).toBe(true);
    expect(vi.mocked(logger.error)).toHaveBeenCalledWith(
      expect.stringMatching(/^openai stream aborted: Error: provider key sk-999 rejected\n/),
    );
  });

  it("sends an SSE comment after ten silent seconds", async () => {
    vi.useFakeTimers();
    const res = response();
    const stream = held({ id: "c1" }, { id: "c2" });

    const done = streamOpenAiSse(res, stream.stream);
    await vi.advanceTimersByTimeAsync(10_000);
    stream.release();
    await done;

    expect(res._getData()).toBe(
      'data: {"id":"c1"}\n\n: ping\n\ndata: {"id":"c2"}\n\ndata: [DONE]\n\n',
    );
    expect(vi.getTimerCount()).toBe(0);
  });

  it("works with a response that cannot flush its headers", async () => {
    const res = createResponse<Response>({ eventEmitter: EventEmitter });

    await streamOpenAiSse(res, events());

    expect(res._getData()).toBe("data: [DONE]\n\n");
  });
});

describe("startStreamHeartbeat", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  const start = (canWrite?: () => boolean) => {
    const res = response();
    const beat = startStreamHeartbeat(res, "·", canWrite);
    return { res, beat };
  };

  it("writes the frame on the first check at least ten seconds after the last activity", async () => {
    const { res, beat } = start();

    await vi.advanceTimersByTimeAsync(9_999);
    expect(res._getData()).toBe("");
    await vi.advanceTimersByTimeAsync(1);
    expect(res._getData()).toBe("·");
    await vi.advanceTimersByTimeAsync(9_999);
    expect(res._getData()).toBe("·");
    await vi.advanceTimersByTimeAsync(1);
    expect(res._getData()).toBe("··");
    beat.stop();
  });

  it("checks every 2.5 seconds, so activity just after a check delays the beat by less than a check", async () => {
    const { res, beat } = start();

    await vi.advanceTimersByTimeAsync(2_600);
    beat.touch();
    // Idle since 2.6 s: the checks at 10, 12.5 s find less than ten seconds, the one at 15 s more.
    await vi.advanceTimersByTimeAsync(9_900);
    expect(res._getData()).toBe("");
    await vi.advanceTimersByTimeAsync(2_500);
    expect(res._getData()).toBe("·");
    beat.stop();
  });

  it("skips a beat while the caller cannot write and beats once it can", async () => {
    let writable = false;
    const { res, beat } = start(() => writable);

    await vi.advanceTimersByTimeAsync(20_000);
    expect(res._getData()).toBe("");
    writable = true;
    await vi.advanceTimersByTimeAsync(2_500);
    expect(res._getData()).toBe("·");
    beat.stop();
  });

  it("writes nothing to an ended or destroyed response", async () => {
    const ended = start();
    ended.res.end();
    const destroyed = start();
    Object.assign(destroyed.res, { destroyed: true });

    await vi.advanceTimersByTimeAsync(30_000);

    expect(ended.res._getData()).toBe("");
    expect(destroyed.res._getData()).toBe("");
    ended.beat.stop();
    destroyed.beat.stop();
  });

  it("writes nothing after stop", async () => {
    const { res, beat } = start();

    beat.stop();
    await vi.advanceTimersByTimeAsync(60_000);

    expect(res._getData()).toBe("");
    expect(vi.getTimerCount()).toBe(0);
  });
});
