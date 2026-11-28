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

import type { IncomingHttpHeaders } from "http";
import type { Request, Response } from "express";
import { describe, expect, it, vi } from "vitest";
import * as ctx from "../requestContext.js";
import type { SourceMeta } from "../storage/docspaceFilesApi.js";
import type { ChatContextSnapshot } from "../storage/chatContextSnapshot.js";

// The real request context: AsyncLocalStorage set up by the middleware the app mounts.

/** Runs `fn` inside a context made from these raw headers, and returns what it returns. */
function within<T>(headers: IncomingHttpHeaders | undefined, fn: () => T): T {
  let result: T | undefined;
  const next = vi.fn(() => {
    result = fn();
  });
  ctx.requestContextMiddleware({ headers } as Request, {} as Response, next);
  expect(next).toHaveBeenCalledOnce();
  expect(next).toHaveBeenCalledWith();
  return result as T;
}

const HOP_BY_HOP = [
  "host",
  "content-length",
  "content-type",
  "connection",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailer",
  "transfer-encoding",
  "upgrade",
  "expect",
  "accept-encoding",
  "x-session-id",
  "mcp-session-id",
];

describe("the forwarded headers", () => {
  it.each(HOP_BY_HOP)("drops %s", (name) => {
    expect(within({ [name]: "v", cookie: "c" }, ctx.getForwardedHeaders)).toEqual({ cookie: "c" });
  });

  it.each(["X-Session-Id", "MCP-Session-Id", "Host", "Content-Type"])(
    "drops %s whatever its case",
    (name) => {
      expect(within({ [name]: "v" }, ctx.getForwardedHeaders)).toEqual({});
    },
  );

  it("keeps every other header under its lowercase name", () => {
    const headers = {
      Authorization: "Bearer t",
      cookie: "asc_auth_key=k",
      Referer: "https://portal/",
      "X-Forwarded-For": "1.2.3.4",
      origin: "https://portal",
      "user-agent": "ua",
      accept: "application/json",
      "accept-language": "de",
    } as unknown as IncomingHttpHeaders;

    expect(within(headers, ctx.getForwardedHeaders)).toEqual({
      authorization: "Bearer t",
      cookie: "asc_auth_key=k",
      referer: "https://portal/",
      "x-forwarded-for": "1.2.3.4",
      origin: "https://portal",
      "user-agent": "ua",
      accept: "application/json",
      "accept-language": "de",
    });
  });

  it("joins a repeated header with a comma and a space", () => {
    const headers = {
      "set-cookie": ["a=1", "b=2"],
      "x-many": [],
    } as unknown as IncomingHttpHeaders;

    expect(within(headers, ctx.getForwardedHeaders)).toEqual({
      "set-cookie": "a=1, b=2",
      "x-many": "",
    });
  });

  it("drops an undefined or null value and stringifies any other", () => {
    const headers = {
      a: undefined,
      b: null,
      c: 42,
      d: "",
      e: false,
    } as unknown as IncomingHttpHeaders;

    expect(within(headers, ctx.getForwardedHeaders)).toEqual({ c: "42", d: "", e: "false" });
  });

  it("is empty for a request without headers", () => {
    expect(within(undefined, ctx.getForwardedHeaders)).toEqual({});
  });

  it("lets the last of two names that lowercase alike win", () => {
    const headers = { "X-A": "first", "x-a": "second" } as unknown as IncomingHttpHeaders;

    expect(within(headers, ctx.getForwardedHeaders)).toEqual({ "x-a": "second" });
  });

  it("is a copy: changing it does not change the request's headers", () => {
    const raw: IncomingHttpHeaders = { cookie: "c" };
    within(raw, () => {
      ctx.getForwardedHeaders()["cookie"] = "changed";
    });

    expect(raw).toEqual({ cookie: "c" });
  });

  it("is the same object for every read within one request", () => {
    within({ cookie: "c" }, () => {
      ctx.getForwardedHeaders()["x-added"] = "1";
      expect(ctx.getForwardedHeaders()).toEqual({ cookie: "c", "x-added": "1" });
    });
  });
});

describe("outside a request", () => {
  it("answers the empty defaults", () => {
    expect(ctx.getForwardedHeaders()).toEqual({});
    expect(ctx.shouldForwardHeadersToProvider()).toBe(false);
    expect(ctx.getResolvedFormId()).toBeUndefined();
    expect(ctx.getResolvedAttachmentId()).toBeUndefined();
    expect(ctx.getAnalyzeEntryIds()).toEqual([]);
    expect(ctx.getRoundProfileId()).toBeUndefined();
    expect(ctx.getRoundThreadId()).toBeUndefined();
    expect(ctx.getRoundSignal()).toBeUndefined();
    expect(ctx.getRoundAttachmentIds()).toEqual([]);
    expect(ctx.getCustomServerNames()).toEqual([]);
    expect(ctx.getKnowledgeRoomIds()).toEqual([]);
    expect(ctx.getFolderInfoCache()).toBeUndefined();
    expect(ctx.getFileInfoCache()).toBeUndefined();
    expect(ctx.getSourceMeta()).toBeUndefined();
    expect(ctx.getChatContextSnapshot()).toBeUndefined();
    expect(ctx.getUpstreamReadCount()).toBe(0);
    expect(ctx.getUpstreamCalls()).toEqual({});
    expect(ctx.getFilesApiReadCount()).toBe(0);
    expect(ctx.getChatContextMisses()).toEqual([]);
  });

  it("ignores every setter and counter", () => {
    ctx.markForwardHeadersToProvider();
    ctx.setResolvedFormId(1);
    ctx.setResolvedAttachmentId("a");
    ctx.setAnalyzeEntryIds(["1"]);
    ctx.setRoundProfileId("p");
    ctx.setRoundThreadId("t");
    ctx.setRoundSignal(new AbortController().signal);
    ctx.setRoundAttachmentIds(["r"]);
    ctx.setCustomServerNames(["s"]);
    ctx.setKnowledgeRoomIds([1]);
    ctx.setSourceMeta({} as SourceMeta);
    ctx.setChatContextSnapshot({} as ChatContextSnapshot);
    ctx.countUpstreamRead();
    ctx.countUpstreamCall("GET");
    ctx.countFilesApiRead();
    ctx.noteChatContextMiss("m");

    expect(ctx.shouldForwardHeadersToProvider()).toBe(false);
    expect(ctx.getResolvedFormId()).toBeUndefined();
    expect(ctx.getResolvedAttachmentId()).toBeUndefined();
    expect(ctx.getAnalyzeEntryIds()).toEqual([]);
    expect(ctx.getRoundProfileId()).toBeUndefined();
    expect(ctx.getRoundThreadId()).toBeUndefined();
    expect(ctx.getRoundSignal()).toBeUndefined();
    expect(ctx.getRoundAttachmentIds()).toEqual([]);
    expect(ctx.getCustomServerNames()).toEqual([]);
    expect(ctx.getKnowledgeRoomIds()).toEqual([]);
    expect(ctx.getSourceMeta()).toBeUndefined();
    expect(ctx.getChatContextSnapshot()).toBeUndefined();
    expect(ctx.getUpstreamReadCount()).toBe(0);
    expect(ctx.getUpstreamCalls()).toEqual({});
    expect(ctx.getFilesApiReadCount()).toBe(0);
    expect(ctx.getChatContextMisses()).toEqual([]);
  });
});

describe("within a request", () => {
  it("starts with the empty defaults and two fresh caches", () => {
    within({}, () => {
      expect(ctx.shouldForwardHeadersToProvider()).toBe(false);
      expect(ctx.getAnalyzeEntryIds()).toEqual([]);
      expect(ctx.getRoundAttachmentIds()).toEqual([]);
      expect(ctx.getCustomServerNames()).toEqual([]);
      expect(ctx.getKnowledgeRoomIds()).toEqual([]);
      expect(ctx.getFolderInfoCache()).toEqual(new Map());
      expect(ctx.getFileInfoCache()).toEqual(new Map());
      expect(ctx.getFolderInfoCache()).not.toBe(ctx.getFileInfoCache());
      expect(ctx.getUpstreamReadCount()).toBe(0);
      expect(ctx.getUpstreamCalls()).toEqual({});
      expect(ctx.getFilesApiReadCount()).toBe(0);
      expect(ctx.getChatContextMisses()).toEqual([]);
    });
  });

  it("gives each request its own caches", () => {
    const first = within({}, ctx.getFolderInfoCache);
    const second = within({}, ctx.getFolderInfoCache);

    expect(first).not.toBe(second);
  });

  it("keeps what each setter stores", () => {
    const source = { entryId: "1" } as unknown as SourceMeta;
    const snapshot = { threads: [] } as unknown as ChatContextSnapshot;
    within({}, () => {
      ctx.markForwardHeadersToProvider();
      ctx.setResolvedFormId(7);
      ctx.setResolvedAttachmentId("att");
      ctx.setAnalyzeEntryIds(["10", "11"]);
      ctx.setRoundAttachmentIds(["r1"]);
      ctx.setCustomServerNames(["mcp-a"]);
      ctx.setKnowledgeRoomIds([3, 4]);
      ctx.setSourceMeta(source);
      ctx.setChatContextSnapshot(snapshot);

      expect(ctx.shouldForwardHeadersToProvider()).toBe(true);
      expect(ctx.getResolvedFormId()).toBe(7);
      expect(ctx.getResolvedAttachmentId()).toBe("att");
      expect(ctx.getAnalyzeEntryIds()).toEqual(["10", "11"]);
      expect(ctx.getRoundAttachmentIds()).toEqual(["r1"]);
      expect(ctx.getCustomServerNames()).toEqual(["mcp-a"]);
      expect(ctx.getKnowledgeRoomIds()).toEqual([3, 4]);
      expect(ctx.getSourceMeta()).toBe(source);
      expect(ctx.getChatContextSnapshot()).toBe(snapshot);
    });
  });

  it("lets a later setter call replace the earlier value", () => {
    within({}, () => {
      ctx.setResolvedFormId(1);
      ctx.setResolvedFormId(2);
      ctx.setAnalyzeEntryIds(["a"]);
      ctx.setAnalyzeEntryIds([]);
      ctx.setSourceMeta({} as SourceMeta);
      ctx.setSourceMeta(undefined);
      ctx.setChatContextSnapshot({} as ChatContextSnapshot);
      ctx.setChatContextSnapshot(undefined);

      expect(ctx.getResolvedFormId()).toBe(2);
      expect(ctx.getAnalyzeEntryIds()).toEqual([]);
      expect(ctx.getSourceMeta()).toBeUndefined();
      expect(ctx.getChatContextSnapshot()).toBeUndefined();
    });
  });

  it("keeps the latest non-empty round profile and ignores an empty or absent one", () => {
    within({}, () => {
      ctx.setRoundProfileId(undefined);
      ctx.setRoundProfileId("");
      expect(ctx.getRoundProfileId()).toBeUndefined();

      ctx.setRoundProfileId("p1");
      ctx.setRoundProfileId("p2");
      ctx.setRoundProfileId("");
      ctx.setRoundProfileId(undefined);
      expect(ctx.getRoundProfileId()).toBe("p2");
    });
  });

  it("keeps the first non-empty round thread", () => {
    within({}, () => {
      ctx.setRoundThreadId(undefined);
      ctx.setRoundThreadId("");
      expect(ctx.getRoundThreadId()).toBeUndefined();

      ctx.setRoundThreadId("t1");
      ctx.setRoundThreadId("t2");
      expect(ctx.getRoundThreadId()).toBe("t1");
    });
  });

  it("keeps the first signal of the round", () => {
    within({}, () => {
      const first = new AbortController().signal;
      ctx.setRoundSignal(first);
      ctx.setRoundSignal(new AbortController().signal);
      expect(ctx.getRoundSignal()).toBe(first);
    });
  });

  it("counts reads, calls per method, Files reads and misses", () => {
    within({}, () => {
      ctx.countUpstreamRead();
      ctx.countUpstreamRead();
      ctx.countUpstreamCall("GET");
      ctx.countUpstreamCall("POST");
      ctx.countUpstreamCall("GET");
      ctx.countFilesApiRead();
      ctx.noteChatContextMiss("profiles");
      ctx.noteChatContextMiss("profiles");

      expect(ctx.getUpstreamReadCount()).toBe(2);
      expect(ctx.getUpstreamCalls()).toEqual({ GET: 2, POST: 1 });
      expect(ctx.getFilesApiReadCount()).toBe(1);
      expect(ctx.getChatContextMisses()).toEqual(["profiles", "profiles"]);
    });
  });

  it("does not let a value of one request reach the next one", () => {
    within({}, () => {
      ctx.setResolvedFormId(1);
      ctx.countUpstreamRead();
    });

    within({}, () => {
      expect(ctx.getResolvedFormId()).toBeUndefined();
      expect(ctx.getUpstreamReadCount()).toBe(0);
    });
  });
});

describe("across asynchronous work", () => {
  const tick = () => new Promise<void>((resolve) => setImmediate(resolve));

  it("keeps two concurrent requests apart", async () => {
    const run = (name: string, formId: number) =>
      within({ "x-name": name }, async () => {
        ctx.setResolvedFormId(formId);
        await tick();
        ctx.countUpstreamCall(name);
        await tick();
        return [
          ctx.getForwardedHeaders()["x-name"],
          ctx.getResolvedFormId(),
          ctx.getUpstreamCalls(),
        ];
      });

    const [a, b] = await Promise.all([run("a", 1), run("b", 2)]);

    expect(a).toEqual(["a", 1, { a: 1 }]);
    expect(b).toEqual(["b", 2, { b: 1 }]);
  });

  it("survives a timer, setImmediate and a microtask", async () => {
    const seen = await within({ "x-name": "t" }, async () => {
      const read = () => ctx.getForwardedHeaders()["x-name"];
      const fromTimer = await new Promise((resolve) => setTimeout(() => resolve(read()), 0));
      const fromImmediate = await new Promise((resolve) => setImmediate(() => resolve(read())));
      const fromMicrotask = await new Promise((resolve) => queueMicrotask(() => resolve(read())));
      return [fromTimer, fromImmediate, fromMicrotask];
    });

    expect(seen).toEqual(["t", "t", "t"]);
  });

  it("runs an async generator in the context of the code that iterates it", async () => {
    async function* names() {
      yield ctx.getForwardedHeaders()["x-name"];
      await tick();
      yield ctx.getForwardedHeaders()["x-name"];
    }
    const madeInA = within({ "x-name": "a" }, () => names());

    const inB = await within({ "x-name": "b" }, async () => {
      const out: unknown[] = [];
      for await (const name of madeInA) {
        out.push(name);
      }
      return out;
    });

    expect(inB).toEqual(["b", "b"]);
  });

  it("keeps the context in a generator created and iterated in the same request", async () => {
    const out = await within({ "x-name": "a" }, async () => {
      async function* counts() {
        for (let i = 0; i < 2; i++) {
          ctx.countUpstreamRead();
          await tick();
          yield ctx.getUpstreamReadCount();
        }
      }
      const seen: number[] = [];
      for await (const n of counts()) {
        seen.push(n);
      }
      return seen;
    });

    expect(out).toEqual([1, 2]);
  });

  it("is gone after the request when work is started outside it", async () => {
    let later: (() => unknown) | undefined;
    within({ "x-name": "a" }, () => {
      later = () => ctx.getForwardedHeaders();
    });

    expect(later?.()).toEqual({});
  });
});
