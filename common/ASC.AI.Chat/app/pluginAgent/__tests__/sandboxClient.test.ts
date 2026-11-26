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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

import { describe, expect, it, vi } from "vitest";

import { http, HttpResponse, stalled, useUpstream } from "../../__tests__/helpers/upstream.js";
import { UpstreamShapeError } from "../../storage/upstreamShape.js";
import {
  createSandbox,
  deleteSandbox,
  execInSandbox,
  makeDir,
  readSandboxFile,
  renewSandbox,
  SandboxError,
  writeSandboxFile,
} from "../sandboxClient.js";

const upstream = useUpstream();

const GATEWAY = { sandboxUrl: "http://gateway:9090", sandboxToken: "gw-secret" };
const RPC = `${GATEWAY.sandboxUrl}/gateway.v1.SandboxService`;
const FILES = `${GATEWAY.sandboxUrl}/sandboxes/:id/files`;
const EXEC = { timeoutSeconds: 30, maxOutputChars: 1000 };

/** One Connect envelope: the flag byte, the length, the JSON. */
const frame = (message: unknown, flags = 0): Uint8Array => {
  const json = Buffer.from(JSON.stringify(message), "utf8");
  const header = Buffer.alloc(5);
  header.writeUInt8(flags, 0);
  header.writeUInt32BE(json.length, 1);
  return Buffer.concat([header, json]);
};
const END = frame({}, 2);

/** An answer whose body arrives in the given chunks, without a declared length. */
const execAnswer = (...chunks: Uint8Array[]): Response =>
  new Response(
    new ReadableStream<Uint8Array>({
      start(controller) {
        for (const chunk of chunks) {
          controller.enqueue(chunk);
        }
        controller.close();
      },
    }),
    { headers: { "Content-Type": "application/connect+json" } },
  );

const failure = async (run: Promise<unknown>): Promise<SandboxError> => {
  const err = await run.then(
    () => undefined,
    (e: unknown) => e,
  );
  expect(err).toBeInstanceOf(SandboxError);
  return err as SandboxError;
};

describe("unary calls", () => {
  it("creates a sandbox for the session with the lifetime and the gateway's token", async () => {
    upstream.use(http.post(`${RPC}/Create`, () => HttpResponse.json({ id: "sb-1" })));

    expect(await createSandbox(GATEWAY, "0000-session", 600)).toBe("sb-1");

    expect(upstream.seen).toHaveLength(1);
    expect(upstream.seen[0]).toMatchObject({
      method: "POST",
      path: "/gateway.v1.SandboxService/Create",
      body: { sessionId: "0000-session", ttl: "600s" },
    });
    expect(upstream.seen[0]?.headers).toMatchObject({
      authorization: "Bearer gw-secret",
      "content-type": "application/json",
    });
  });

  it.each([
    ["no id", {}],
    ["an empty id", { id: "" }],
    ["an id that is not text", { id: 7 }],
    ["an array", []],
  ])("takes a Create answer with %s for an answer it cannot read", async (_name, body) => {
    upstream.use(http.post(`${RPC}/Create`, () => HttpResponse.json(body)));
    await expect(createSandbox(GATEWAY, "s", 600)).rejects.toBeInstanceOf(UpstreamShapeError);
  });

  it.each([
    ["resource_exhausted", 429, "busy"],
    ["deadline_exceeded", 504, "busy"],
    ["not_found", 404, "lost"],
    ["failed_precondition", 400, "lost"],
    ["internal", 500, "unavailable"],
  ])("reads the code %s of a refusal as %s", async (code, status, kind) => {
    upstream.use(
      http.post(`${RPC}/Create`, () =>
        HttpResponse.json({ code, message: "2 of 2 in use" }, { status }),
      ),
    );
    const err = await failure(createSandbox(GATEWAY, "s", 600));
    expect(err.kind).toBe(kind);
    expect(err.message).toBe(`sandbox Create: ${kind}`);
    // The status and the code are kept for the log; the gateway's own text is not.
    expect(err.detail).toBe(`${status} ${code}`);
    expect(err.message + err.detail).not.toContain("2 of 2 in use");
  });

  it.each(["resource_exhausted", "deadline_exceeded"])(
    "reads the code %s as unavailable for a call that creates nothing",
    async (code) => {
      upstream.use(
        http.post(`${RPC}/Renew`, () => HttpResponse.json({ code }, { status: 429 })),
        http.post(`${RPC}/MakeDir`, () => HttpResponse.json({ code }, { status: 504 })),
        http.post(`${RPC}/Exec`, () => execAnswer(frame({ error: { code } }, 2))),
      );
      expect((await failure(renewSandbox(GATEWAY, "sb-1", 600))).kind).toBe("unavailable");
      expect((await failure(makeDir(GATEWAY, "sb-1", "/a"))).kind).toBe("unavailable");
      expect((await failure(execInSandbox(GATEWAY, "sb-1", "x", EXEC))).kind).toBe("unavailable");
    },
  );

  it("does not take a code that names a property of every object for a kind", async () => {
    upstream.use(
      http.post(`${RPC}/Renew`, () => HttpResponse.json({ code: "constructor" }, { status: 500 })),
    );
    expect((await failure(renewSandbox(GATEWAY, "sb-1", 600))).kind).toBe("unavailable");
  });

  it.each([
    ["plain text", "bad gateway"],
    ["JSON that is not an object", "[1]"],
    ["an object without a code", "{}"],
  ])("reads a refusal with %s as unavailable", async (_name, text) => {
    upstream.use(http.post(`${RPC}/Renew`, () => new HttpResponse(text, { status: 502 })));
    expect((await failure(renewSandbox(GATEWAY, "sb-1", 600))).kind).toBe("unavailable");
  });

  it("reads a gateway that cannot be reached as unavailable", async () => {
    upstream.use(http.post(`${RPC}/Renew`, () => HttpResponse.error()));
    expect((await failure(renewSandbox(GATEWAY, "sb-1", 600))).kind).toBe("unavailable");
  });

  it("gives up on a gateway that never answers", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(http.post(`${RPC}/Renew`, stalled));
      const outcome = failure(renewSandbox(GATEWAY, "sb-1", 600));
      await vi.advanceTimersByTimeAsync(30_000);
      expect((await outcome).kind).toBe("unavailable");
    } finally {
      vi.useRealTimers();
    }
  });

  it("waits longer for Create than for another call", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(http.post(`${RPC}/Create`, stalled));
      let settled = false;
      const outcome = failure(createSandbox(GATEWAY, "s", 600)).finally(() => {
        settled = true;
      });
      await vi.advanceTimersByTimeAsync(119_000);
      expect(settled).toBe(false);
      await vi.advanceTimersByTimeAsync(1_000);
      expect((await outcome).kind).toBe("unavailable");
    } finally {
      vi.useRealTimers();
    }
  });

  it("leaves an abort of the caller's as it is", async () => {
    upstream.use(http.post(`${RPC}/Renew`, stalled));
    const controller = new AbortController();
    const reason = new Error("stopped");
    const outcome = renewSandbox(GATEWAY, "sb-1", 600, controller.signal);
    controller.abort(reason);
    await expect(outcome).rejects.toBe(reason);
  });

  it("renews, deletes and makes a directory by the sandbox's id", async () => {
    upstream.use(
      http.post(`${RPC}/Renew`, () => HttpResponse.json({ sandbox: {} })),
      http.post(`${RPC}/Delete`, () => HttpResponse.json({})),
      http.post(`${RPC}/MakeDir`, () => HttpResponse.json({})),
    );

    await renewSandbox(GATEWAY, "sb-1", 120);
    await deleteSandbox(GATEWAY, "sb-1");
    await makeDir(GATEWAY, "sb-1", "/workspace/plugin");

    expect(upstream.seen.map((r) => [r.path.split("/").pop(), r.body])).toEqual([
      ["Renew", { id: "sb-1", ttl: "120s" }],
      ["Delete", { id: "sb-1" }],
      ["MakeDir", { id: "sb-1", path: "/workspace/plugin" }],
    ]);
  });
});

describe("files", () => {
  it("reads a file by its path", async () => {
    upstream.use(http.get(FILES, () => execAnswer(Buffer.from("zip bytes"))));

    const bytes = await readSandboxFile(GATEWAY, "sb 1", "/workspace/a b.zip", 100);

    expect(Buffer.from(bytes).toString()).toBe("zip bytes");
    expect(upstream.seen[0]?.path).toBe("/sandboxes/sb%201/files?path=%2Fworkspace%2Fa%20b.zip");
    expect(upstream.seen[0]?.headers["authorization"]).toBe("Bearer gw-secret");
  });

  it("refuses a file over the limit without keeping it", async () => {
    upstream.use(http.get(FILES, () => execAnswer(Buffer.alloc(6), Buffer.alloc(5))));
    expect((await failure(readSandboxFile(GATEWAY, "sb-1", "/f", 10))).kind).toBe("tooLarge");
  });

  it.each([
    [404, "absent"],
    [502, "lost"],
    [503, "unavailable"],
    [500, "unavailable"],
  ])("reads the status %i of a file route as %s", async (status, kind) => {
    upstream.use(
      http.get(FILES, () => HttpResponse.json({ error: "x" }, { status })),
      http.put(FILES, () => HttpResponse.json({ error: "x" }, { status })),
    );
    const read = await failure(readSandboxFile(GATEWAY, "sb-1", "/f", 10));
    expect(read.kind).toBe(kind);
    expect(read.detail).toBe(String(status));
    expect((await failure(writeSandboxFile(GATEWAY, "sb-1", "/f", Buffer.from("x")))).kind).toBe(
      kind,
    );
  });

  /** An answer that sends its headers and the first bytes, then nothing until the request is aborted. */
  const headersThenStall = ({ request }: { request: Request }): Response =>
    new Response(
      new ReadableStream<Uint8Array>({
        start(controller) {
          controller.enqueue(Buffer.from([0, 0]));
          request.signal.addEventListener("abort", () => controller.error(request.signal.reason));
        },
      }),
    );

  it("gives up on a file whose body stalls after the headers", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(http.get(FILES, headersThenStall));
      const outcome = failure(readSandboxFile(GATEWAY, "sb-1", "/f", 100));
      await vi.advanceTimersByTimeAsync(120_000);
      expect((await outcome).kind).toBe("unavailable");
    } finally {
      vi.useRealTimers();
    }
  });

  it("gives up on a command whose stream stalls after its first bytes", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(http.post(`${RPC}/Exec`, headersThenStall));
      const outcome = failure(execInSandbox(GATEWAY, "sb-1", "sleep 1000", EXEC));
      await vi.advanceTimersByTimeAsync(45_000);
      expect((await outcome).kind).toBe("unavailable");
    } finally {
      vi.useRealTimers();
    }
  });

  it("writes a file with its bytes as the body", async () => {
    let received = "";
    upstream.use(
      http.put(FILES, async ({ request }) => {
        received = await request.text();
        return new HttpResponse(null, { status: 204 });
      }),
    );

    await writeSandboxFile(GATEWAY, "sb-1", "/workspace/a.ts", Buffer.from("const a = 1;"));

    expect(received).toBe("const a = 1;");
    expect(upstream.seen[0]).toMatchObject({
      method: "PUT",
      path: "/sandboxes/sb-1/files?path=%2Fworkspace%2Fa.ts",
    });
    expect(upstream.seen[0]?.headers["content-type"]).toBe("application/octet-stream");
  });

  it("reads a gateway that cannot be reached as unavailable", async () => {
    upstream.use(
      http.get(FILES, () => HttpResponse.error()),
      http.put(FILES, () => HttpResponse.error()),
    );
    expect((await failure(readSandboxFile(GATEWAY, "sb-1", "/f", 10))).kind).toBe("unavailable");
    expect((await failure(writeSandboxFile(GATEWAY, "sb-1", "/f", Buffer.from("x")))).kind).toBe(
      "unavailable",
    );
  });
});

describe("execInSandbox", () => {
  it("sends the command in an envelope and collects the output in order", async () => {
    let sent: Uint8Array = new Uint8Array();
    upstream.use(
      http.post(`${RPC}/Exec`, async ({ request }) => {
        sent = new Uint8Array(await request.arrayBuffer());
        return execAnswer(
          frame({ stdout: "one\n" }),
          frame({ stderr: "warn\n" }),
          frame({ stdout: "two\n" }),
          frame({ exit: { code: 3 } }),
          END,
        );
      }),
    );

    const result = await execInSandbox(GATEWAY, "sb-1", "npm run build", {
      ...EXEC,
      cwd: "/workspace/plugin",
      env: { CI: "1" },
    });

    expect(result).toEqual({ stdout: "one\ntwo\n", stderr: "warn\n", code: 3, truncated: false });
    expect(sent[0]).toBe(0);
    expect(Buffer.from(sent).readUInt32BE(1)).toBe(sent.length - 5);
    expect(JSON.parse(Buffer.from(sent.subarray(5)).toString())).toEqual({
      id: "sb-1",
      command: "npm run build",
      cwd: "/workspace/plugin",
      env: { CI: "1" },
      timeout: "30s",
    });
    expect(upstream.seen[0]?.headers).toMatchObject({
      authorization: "Bearer gw-secret",
      "content-type": "application/connect+json",
      "connect-protocol-version": "1",
    });
  });

  it("leaves the working directory and the environment out when none is given", async () => {
    let sent: Uint8Array = new Uint8Array();
    upstream.use(
      http.post(`${RPC}/Exec`, async ({ request }) => {
        sent = new Uint8Array(await request.arrayBuffer());
        return execAnswer(frame({ exit: {} }), END);
      }),
    );

    const result = await execInSandbox(GATEWAY, "sb-1", "true", EXEC);

    // proto3 JSON leaves a zero out.
    expect(result.code).toBe(0);
    expect(JSON.parse(Buffer.from(sent.subarray(5)).toString())).toEqual({
      id: "sb-1",
      command: "true",
      timeout: "30s",
    });
  });

  it("puts together messages that arrive split across chunks", async () => {
    const all = Buffer.concat([frame({ stdout: "héllo" }), frame({ exit: { code: 0 } }), END]);
    upstream.use(
      http.post(`${RPC}/Exec`, () =>
        execAnswer(all.subarray(0, 3), all.subarray(3, 9), all.subarray(9)),
      ),
    );
    expect((await execInSandbox(GATEWAY, "sb-1", "echo", EXEC)).stdout).toBe("héllo");
  });

  it("keeps the first characters of each stream and says that it dropped the rest", async () => {
    upstream.use(
      http.post(`${RPC}/Exec`, () =>
        execAnswer(
          frame({ stdout: "abcd" }),
          frame({ stdout: "efgh" }),
          frame({ stdout: "ijkl" }),
          frame({ stderr: "xy" }),
          frame({ exit: { code: 0 } }),
          END,
        ),
      ),
    );
    expect(
      await execInSandbox(GATEWAY, "sb-1", "cat big", { timeoutSeconds: 30, maxOutputChars: 6 }),
    ).toEqual({ stdout: "abcdef", stderr: "xy", code: 0, truncated: true });
  });

  it("skips a message it does not know", async () => {
    upstream.use(
      http.post(`${RPC}/Exec`, () =>
        execAnswer(frame({ progress: 1 }), frame({ exit: { code: 0 } }), END),
      ),
    );
    expect((await execInSandbox(GATEWAY, "sb-1", "true", EXEC)).code).toBe(0);
  });

  it.each([
    ["before the exit event", [frame({ stdout: "half" })]],
    ["after the exit event, without the end of the stream", [frame({ exit: { code: 0 } })]],
    ["in the middle of a message", [frame({ exit: { code: 0 } }).subarray(0, 7)]],
    ["with the end of the stream alone", [END]],
  ])("fails when the stream is cut %s", async (_name, chunks) => {
    upstream.use(http.post(`${RPC}/Exec`, () => execAnswer(...chunks)));
    expect((await failure(execInSandbox(GATEWAY, "sb-1", "x", EXEC))).kind).toBe("unavailable");
  });

  it.each([
    ["failed_precondition", "lost"],
    ["not_found", "lost"],
    ["internal", "unavailable"],
  ])("reads the code %s at the end of the stream as %s", async (code, kind) => {
    upstream.use(
      http.post(`${RPC}/Exec`, () =>
        execAnswer(frame({ stdout: "x" }), frame({ error: { code, message: "gone" } }, 2)),
      ),
    );
    const err = await failure(execInSandbox(GATEWAY, "sb-1", "x", EXEC));
    expect(err.kind).toBe(kind);
    expect(err.detail).toBe(code);
  });

  it("reads an error without a code as unavailable", async () => {
    upstream.use(http.post(`${RPC}/Exec`, () => execAnswer(frame({ error: {} }, 2))));
    const err = await failure(execInSandbox(GATEWAY, "sb-1", "x", EXEC));
    expect(err.kind).toBe("unavailable");
    expect(err.detail).toBe("");
  });

  it.each([
    ["is not JSON", Buffer.concat([Buffer.from([0, 0, 0, 0, 3]), Buffer.from("{{{")])],
    ["is not an object", frame([1])],
  ])("fails on a message that %s", async (_name, chunk) => {
    upstream.use(http.post(`${RPC}/Exec`, () => execAnswer(chunk)));
    expect((await failure(execInSandbox(GATEWAY, "sb-1", "x", EXEC))).kind).toBe("unavailable");
  });

  it("refuses a message larger than it is ready to hold", async () => {
    const header = Buffer.alloc(5);
    header.writeUInt32BE(4 * 1024 * 1024 + 1, 1);
    upstream.use(http.post(`${RPC}/Exec`, () => execAnswer(header)));
    expect((await failure(execInSandbox(GATEWAY, "sb-1", "x", EXEC))).kind).toBe("tooLarge");
  });

  it("reads a refusal ahead of the stream by its code", async () => {
    upstream.use(
      http.post(`${RPC}/Exec`, () => HttpResponse.json({ code: "not_found" }, { status: 404 })),
    );
    expect((await failure(execInSandbox(GATEWAY, "sb-1", "x", EXEC))).kind).toBe("lost");
  });

  it("reads an answer without a body as unavailable", async () => {
    upstream.use(http.post(`${RPC}/Exec`, () => new HttpResponse(null, { status: 200 })));
    expect((await failure(execInSandbox(GATEWAY, "sb-1", "x", EXEC))).kind).toBe("unavailable");
  });

  it("waits for the command's own timeout and a little longer, then gives up", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(http.post(`${RPC}/Exec`, stalled));
      let settled = false;
      const outcome = failure(execInSandbox(GATEWAY, "sb-1", "sleep 1000", EXEC)).finally(() => {
        settled = true;
      });
      await vi.advanceTimersByTimeAsync(44_000);
      expect(settled).toBe(false);
      await vi.advanceTimersByTimeAsync(1_000);
      expect((await outcome).kind).toBe("unavailable");
    } finally {
      vi.useRealTimers();
    }
  });

  it("ends with the caller's signal", async () => {
    upstream.use(http.post(`${RPC}/Exec`, stalled));
    const controller = new AbortController();
    const reason = new Error("reply stopped");
    const outcome = execInSandbox(GATEWAY, "sb-1", "sleep 1000", {
      ...EXEC,
      signal: controller.signal,
    });
    controller.abort(reason);
    await expect(outcome).rejects.toBe(reason);
  });
});
