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

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { snapshotOf } from "../../__tests__/fixtures/chatContext.js";
import { fileDto, folderDto } from "../../__tests__/fixtures/filesApi.js";
import { selfDto } from "../../__tests__/fixtures/peopleApi.js";
import { inRequestContext } from "../../__tests__/helpers/http.js";
import { afterUpstreamTimeout } from "../../__tests__/helpers/time.js";
import {
  docspaceError,
  http,
  HttpResponse,
  stalled,
  useUpstream,
} from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { chunked, fakeGateway, refusedWith } from "../../__tests__/helpers/sandboxGateway.js";
import type { FakeGateway } from "../../__tests__/helpers/sandboxGateway.js";
import { MAX_OUTPUT_CHARS, MAX_READ_BYTES, MAX_WRITE_CHARS } from "../../pluginAgent/agentTools.js";
import { setChatContextSnapshot, setRoundSignal, setRoundThreadId } from "../../requestContext.js";
import { MAX_FILE_BYTES } from "../../tools/documentAgentFiles.js";
import { EXEC_MAX_TIMEOUT_SECONDS, MAX_TITLE_CHARS, sandboxTools } from "../sandboxTools.js";

// Everything the tools talk to is stubbed at the network: the gateway, the People API, the Files
// API and the AI service. Where a saved file goes is `sandboxTools.upstream.test.ts`.

const upstream = useUpstream();

const THREAD = "0b9f2c66-6a0e-4a7e-9f0e-1c2d3e4f5a6b";
const DIR = `/workspace/${THREAD}`;
const USER = "a0d6bc40-2b3c-4c1a-9a7d-1f1b2c3d4e5f";
// GET api/2.0/people/@self (UserController.GetSelfProfile).
const SELF = "*/api/2.0/people/@self";
// GET api/2.0/files/folder/{folderId} (FoldersController.GetFolderInfo): the round's folder.
const FOLDER = "*/api/2.0/files/folder/30";
// POST internal/ai/threads/{id}/artifacts (ThreadArtifactsController.UploadAsync).
const ARTIFACTS = "*/internal/ai/threads/:threadId/artifacts";
// POST api/2.0/files/{folderId}/insert (UploadController.InsertFile).
const INSERT = "*/api/2.0/files/:folderId/insert";

/** What the Files API was asked to store. */
interface Insert {
  folder: string;
  title: string;
  type: string;
  bytes: number;
}
let inserts: Insert[];

let gateway: FakeGateway;

/** The requests made: `self` for the People API, the gateway's own names for the rest. */
const requests = (): string[] =>
  upstream.seen.map((request) =>
    request.path.endsWith("/people/@self")
      ? "self"
      : (request.path.split("/").pop() ?? "").replace(/\?.*$/, ""),
  );

/** The body the gateway method was last called with. */
const sentTo = (method: string): unknown =>
  upstream.seen.filter((request) => request.path.endsWith(`/${method}`)).at(-1)?.body;

const call = (
  tool: string,
  args: unknown,
  options: {
    thread?: string | null;
    contextThread?: string;
    signal?: AbortSignal;
    /** The round's folder; none when null. */
    entity?: string | null;
  } = {},
): Promise<unknown> =>
  inRequestContext({ authorization: "Bearer user" }, () => {
    if (options.thread !== null) {
      setRoundThreadId(options.thread ?? THREAD);
    }
    if (options.contextThread) {
      setChatContextSnapshot(
        snapshotOf({
          requested: {
            threadId: options.contextThread,
            entityId: undefined,
            contextEntityId: undefined,
          },
        }),
      );
    }
    if (options.signal) {
      setRoundSignal(options.signal);
    }
    return sandboxTools.callTool(
      tool,
      args as Record<string, unknown>,
      options.entity === null ? undefined : (options.entity ?? "30"),
    );
  });

const exec = (args: unknown = { command: "ls" }) => call("sandbox_exec", args);

beforeEach(() => {
  vi.stubEnv("AI__SANDBOX__URL", "http://gateway:9090");
  vi.stubEnv("AI__SANDBOX__TOKEN", "gw-secret");
  vi.stubEnv("AI__SANDBOX__IDLE_TTL_SECONDS", "900");
  gateway = fakeGateway(upstream);
  inserts = [];
  upstream.use(
    http.get(SELF, () => HttpResponse.json(selfDto({ id: USER }))),
    // The portal as a save meets it unless a test says otherwise: a room the caller may create
    // in, a conversation of no AI project (400), an upload that is taken under its title.
    http.get(FOLDER, () => HttpResponse.json(folderDto())),
    http.post(ARTIFACTS, () => docspaceError(400)),
    http.post(INSERT, async ({ request, params }) => {
      const form = await request.formData();
      const file = form.get("file") as File;
      const title = String(form.get("title"));
      inserts.push({
        folder: String(params["folderId"]),
        title,
        type: file.type,
        bytes: file.size,
      });
      return HttpResponse.json(fileDto({ id: 101, title }));
    }),
  );
});

afterEach(() => {
  vi.unstubAllEnvs();
  vi.restoreAllMocks();
});

describe("who is offered the sandbox", () => {
  it("offers nothing on an installation that names no sandbox", async () => {
    vi.stubEnv("AI__SANDBOX__URL", "");
    expect(await sandboxTools.getTools()).toEqual({});
    expect(await exec()).toBe("The sandbox is not configured on this server.");
    expect(upstream.seen).toEqual([]);
  });

  it("offers the four tools under one group, without an approval of their own", async () => {
    const tools = (await sandboxTools.getTools())["chat-sandbox"] ?? [];
    expect(tools.map((tool) => tool.name)).toEqual([
      "sandbox_exec",
      "sandbox_read_file",
      "sandbox_write_file",
      "sandbox_save_file",
    ]);
    expect(tools.every((tool) => tool.requireApproval === false)).toBe(true);
    // The idle lifetime of the configuration, in the words the model reads.
    expect(tools[0]?.description).toContain("for at least 15 minutes after the last one");
    expect(upstream.seen).toEqual([]);
  });

  it("answers an unknown tool and a call outside a conversation without asking anybody", async () => {
    expect(await call("sandbox_other", {})).toBe('Unknown tool "sandbox_other"');
    expect(await call("sandbox_exec", { command: "ls" }, { thread: null })).toBe(
      "A sandbox belongs to a conversation, and this call is made outside one.",
    );
    expect(upstream.seen).toEqual([]);
  });

  it("works in the thread the request names when the round has set none", async () => {
    await call("sandbox_exec", { command: "ls" }, { thread: null, contextThread: "t-9" });

    expect(gateway.execs[0]?.cwd).toBe("/workspace/t-9");
  });

  it("takes a thread id of 100 characters as a directory name", async () => {
    const thread = "x".repeat(100);

    await call("sandbox_exec", { command: "ls" }, { thread });

    expect(gateway.execs[0]?.cwd).toBe(`/workspace/${thread}`);
  });

  it.each([["a/b"], [".."], ["a b"], ["x".repeat(101)]])(
    "asks nobody for a thread id that is not a directory name: %j",
    async (thread) => {
      expect(await call("sandbox_exec", { command: "ls" }, { thread })).toBe(
        "This conversation cannot have a directory in the sandbox.",
      );
      expect(upstream.seen).toEqual([]);
    },
  );
});

describe("the sandbox of a caller", () => {
  it("is asked for the caller's user id at every call, renewed, and given the conversation's directory", async () => {
    gateway.run = () => ({ stdout: "a.txt\n" });

    const result = await exec();

    expect(requests()).toEqual(["self", "Create", "Renew", "MakeDir", "Exec"]);
    // The longest command timeout on top of the idle lifetime, whatever the call: 900 + 600.
    expect(sentTo("Create")).toEqual({ sessionId: USER, ttl: "1500s" });
    expect(sentTo("Renew")).toEqual({ id: "sb-1", ttl: "1500s" });
    expect(sentTo("MakeDir")).toEqual({ id: "sb-1", path: DIR });
    expect(gateway.execs).toEqual([{ id: "sb-1", command: "ls", cwd: DIR, timeout: "60s" }]);
    expect(result).toBe("exit code: 0\n[stdout]\na.txt\n");
  });

  it("is the same one for another conversation of the caller, which works in its own directory", async () => {
    await exec();
    await call("sandbox_exec", { command: "ls", timeout_seconds: 300 }, { thread: "t-2" });

    expect(upstream.seen.filter((request) => request.path.endsWith("/Create"))).toHaveLength(2);
    expect(sentTo("Create")).toEqual({ sessionId: USER, ttl: "1500s" });
    expect(gateway.execs.map((made) => made.cwd)).toEqual([DIR, "/workspace/t-2"]);
    expect(gateway.execs[1]?.timeout).toBe("300s");
  });

  it("is never deleted by a call, whatever comes of it", async () => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    await exec();
    gateway.run = () => refusedWith("internal", 500)();
    await exec();

    expect(gateway.calls).not.toContain("Delete");
  });

  it("says no sandbox is free when the gateway has none, and runs nothing", async () => {
    const warn = vi.spyOn(logger, "warn").mockImplementation(() => logger);
    gateway.fail.set("Create", refusedWith("resource_exhausted", 429));

    expect(await exec()).toBe("No sandbox is free right now. Try again in a few minutes.");
    expect(requests()).toEqual(["self", "Create"]);
    expect(warn).toHaveBeenCalledWith(
      "sandbox_exec: sandbox Create: busy (429 resource_exhausted)",
    );
  });

  it.each([
    ["a creation", "Create"],
    ["a renewal", "Renew"],
    ["the directory", "MakeDir"],
  ])(
    "says the sandbox cannot be reached when the gateway fails %s, and runs nothing",
    async (_name, method) => {
      vi.spyOn(logger, "warn").mockImplementation(() => logger);
      gateway.fail.set(method, refusedWith("internal", 500));

      expect(await exec()).toBe("The sandbox cannot be reached right now. Try again later.");
      expect(gateway.execs).toEqual([]);
    },
  );

  it.each([
    [
      "between its creation and its renewal",
      () => gateway.fail.set("Renew", refusedWith("not_found", 404)),
    ],
    ["under a command", () => (gateway.run = () => refusedWith("failed_precondition", 400)())],
  ])(
    "says a sandbox that went away %s is gone, and that a new call starts another",
    async (_name, arrange) => {
      vi.spyOn(logger, "warn").mockImplementation(() => logger);
      arrange();

      expect(await exec()).toBe(
        "The sandbox is gone. Call the tool again: a new, empty sandbox will be started.",
      );
    },
  );

  it("answers the status when the portal does not say who the caller is, and asks the gateway for nothing", async () => {
    const warn = vi.spyOn(logger, "warn").mockImplementation(() => logger);
    upstream.use(http.get(SELF, () => docspaceError(500)));

    expect(await exec()).toBe("The workspace could not be reached: it answered 500.");
    expect(requests()).toEqual(["self"]);
    expect(warn).toHaveBeenCalledOnce();
  });

  it("answers an internal error for a caller the portal sends without an id", async () => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    upstream.use(http.get(SELF, () => HttpResponse.json(selfDto({ id: undefined }))));

    expect(await exec()).toBe("The call could not be completed due to an internal error.");
    expect(requests()).toEqual(["self"]);
  });

  it("says a call stopped with the reply was stopped", async () => {
    const round = new AbortController();
    gateway.run = () => {
      round.abort();
      return HttpResponse.error();
    };

    expect(await call("sandbox_exec", { command: "sleep 100" }, { signal: round.signal })).toBe(
      "The call was stopped with the reply.",
    );
  });

  it("stops asking the portal who the caller is when the reply is stopped", async () => {
    const round = new AbortController();
    upstream.use(
      http.get(SELF, () => {
        round.abort();
        return stalled();
      }),
    );

    // A call that went on asking would end at the upstream timeout only: it is given turns of
    // the event loop, not time.
    const turns = async (count: number): Promise<string> => {
      for (let turn = 0; turn < count; turn++) {
        await new Promise((resolve) => setImmediate(resolve));
      }
      return "still asking the portal";
    };
    const answer = await Promise.race([
      call("sandbox_exec", { command: "ls" }, { signal: round.signal }),
      turns(200),
    ]);

    expect(answer).toBe("The call was stopped with the reply.");
    expect(requests()).toEqual(["self"]);
  });

  it.each([
    ["a portal that does not answer", () => stalled()],
    ["an answer of the portal that is not JSON", () => new HttpResponse("who?", { status: 200 })],
  ])(
    "answers an internal error for %s, and asks the gateway for nothing",
    async (_name, answer) => {
      vi.spyOn(logger, "warn").mockImplementation(() => logger);
      upstream.use(http.get(SELF, answer));

      expect(await afterUpstreamTimeout(() => exec())).toEqual({
        value: "The call could not be completed due to an internal error.",
      });
      expect(requests()).toEqual(["self"]);
    },
  );

  it("is renewed to the same lifetime by every call, so that no call moves its expiry back", async () => {
    await exec({ command: "sleep 590", timeout_seconds: EXEC_MAX_TIMEOUT_SECONDS });
    const afterCommand = sentTo("Renew");
    await call("sandbox_read_file", { path: "/workspace/a.txt" });

    expect(afterCommand).toEqual({ id: "sb-1", ttl: "1500s" });
    expect(sentTo("Renew")).toEqual(afterCommand);
  });
});

describe("sandbox_exec", () => {
  it.each([[{}], [{ command: "  " }], [{ command: 5 }], [null]])(
    "refuses %j before a sandbox is asked for",
    async (args) => {
      expect(await exec(args)).toBe("sandbox_exec requires a non-empty 'command'.");
      expect(upstream.seen).toEqual([]);
    },
  );

  it.each([[0], [-1], [null], [EXEC_MAX_TIMEOUT_SECONDS + 1], [1.5], ["60"]])(
    "refuses a timeout of %j before a sandbox is asked for",
    async (timeout) => {
      expect(await exec({ command: "ls", timeout_seconds: timeout })).toBe(
        "'timeout_seconds' is a whole number from 1 to 600.",
      );
      expect(upstream.seen).toEqual([]);
    },
  );

  it.each([[1], [EXEC_MAX_TIMEOUT_SECONDS]])(
    "runs a command with a timeout of %j seconds, the bound of what is taken",
    async (timeout) => {
      expect(await exec({ command: "ls", timeout_seconds: timeout })).toBe("exit code: 0");
      expect(gateway.execs[0]?.timeout).toBe(`${timeout}s`);
    },
  );

  it("reports a failed command as its exit code and output, not as a failure", async () => {
    gateway.run = () => ({ stderr: "no such file\n", code: 2 });

    expect(await exec()).toBe("exit code: 2\n[stderr]\nno such file\n");
  });

  it("cuts an output over its limit and says so", async () => {
    gateway.run = () => ({ stdout: "x".repeat(MAX_OUTPUT_CHARS + 1) });

    expect(await exec()).toBe(
      `exit code: 0\n[stdout]\n${"x".repeat(MAX_OUTPUT_CHARS)}\n(output cut at ${MAX_OUTPUT_CHARS} characters per stream)`,
    );
  });

  it("says that the output could not be read when a piece of it is too large", async () => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    // One envelope that announces 2 GiB: over what a message of the stream may be.
    gateway.run = () => chunked(Buffer.from([0, 0x7f, 0xff, 0xff, 0xff]));

    expect(await exec()).toBe(
      "The command's output could not be read: one piece of it was too large. Write the output to a file and read a part of it.",
    );
  });
});

describe("sandbox_read_file and sandbox_write_file", () => {
  it("writes a file with its directories and reads it back, each call renewing the sandbox", async () => {
    expect(await call("sandbox_write_file", { path: "/workspace/a/b.txt", content: "héllo" })).toBe(
      "Wrote 6 bytes to /workspace/a/b.txt.",
    );
    expect(gateway.calls).toEqual([
      "Create",
      "Renew",
      `MakeDir ${DIR}`,
      "MakeDir /workspace/a",
      "PUT /workspace/a/b.txt",
    ]);
    expect(sentTo("Renew")).toEqual({ id: "sb-1", ttl: "1500s" });

    expect(await call("sandbox_read_file", { path: "/workspace/a/b.txt" })).toBe("héllo");
  });

  it("takes a relative path as one of the conversation's directory", async () => {
    expect(await call("sandbox_write_file", { path: "out/a.txt", content: "x" })).toBe(
      `Wrote 1 bytes to ${DIR}/out/a.txt.`,
    );
    expect(await call("sandbox_read_file", { path: "out/a.txt" })).toBe("x");
    gateway.files.set(`${DIR}/r.csv`, Buffer.from("a\n"));
    expect(await call("sandbox_save_file", { path: "r.csv" })).toBe(
      JSON.stringify({ fileId: "101", title: "r.csv", bytes: 2, where: "into folder 30" }),
    );
  });

  it("reads a file of the largest size it takes and writes a content of the longest", async () => {
    gateway.files.set("/workspace/most", Buffer.alloc(MAX_READ_BYTES, "a"));

    expect(await call("sandbox_read_file", { path: "/workspace/most" })).toHaveLength(
      MAX_READ_BYTES,
    );
    expect(
      await call("sandbox_write_file", {
        path: "/workspace/long.txt",
        content: "x".repeat(MAX_WRITE_CHARS),
      }),
    ).toBe(`Wrote ${MAX_WRITE_CHARS} bytes to /workspace/long.txt.`);
  });

  it("makes the conversation's directory once for a file written into it", async () => {
    await call("sandbox_write_file", { path: "a.txt", content: "x" });

    expect(gateway.calls).toEqual(["Create", "Renew", `MakeDir ${DIR}`, `PUT ${DIR}/a.txt`]);
  });

  it("writes an empty content as an empty file", async () => {
    expect(await call("sandbox_write_file", { path: "/workspace/none.txt", content: "" })).toBe(
      "Wrote 0 bytes to /workspace/none.txt.",
    );
    expect(gateway.files.get("/workspace/none.txt")).toHaveLength(0);
  });

  it("says what a file is when it is empty, absent or too large to read", async () => {
    gateway.files.set("/workspace/empty", Buffer.alloc(0));
    gateway.files.set("/workspace/big", Buffer.alloc(MAX_READ_BYTES + 1));

    expect(await call("sandbox_read_file", { path: "/workspace/empty" })).toBe(
      "(/workspace/empty is empty)",
    );
    expect(await call("sandbox_read_file", { path: "/workspace/none" })).toBe(
      "/workspace/none does not exist.",
    );
    expect(await call("sandbox_read_file", { path: "/workspace/big" })).toBe(
      `/workspace/big is over ${MAX_READ_BYTES} bytes: read a part of it with sandbox_exec (head, sed, grep).`,
    );
  });

  it.each([[{}], [{ path: "" }], [{ path: "../a.txt" }], [{ path: "/workspace/../etc/passwd" }]])(
    "refuses to read %j before a sandbox is asked for",
    async (args) => {
      expect(await call("sandbox_read_file", args)).toBe(
        "sandbox_read_file requires 'path', a path without '..'.",
      );
      expect(upstream.seen).toEqual([]);
    },
  );

  it.each([
    [{ path: "/workspace/a.txt" }],
    [{ path: "a/../../b.txt", content: "x" }],
    [{ path: "/workspace/a.txt", content: 5 }],
  ])("refuses to write %j before a sandbox is asked for", async (args) => {
    expect(await call("sandbox_write_file", args)).toBe(
      "sandbox_write_file requires 'path', a path without '..', and 'content', a string.",
    );
    expect(upstream.seen).toEqual([]);
  });

  it("refuses a content over its limit before a sandbox is asked for", async () => {
    expect(
      await call("sandbox_write_file", {
        path: "/workspace/a.txt",
        content: "x".repeat(MAX_WRITE_CHARS + 1),
      }),
    ).toBe(
      `'content' is over ${MAX_WRITE_CHARS} characters: write the file in parts with sandbox_exec.`,
    );
    expect(upstream.seen).toEqual([]);
  });

  it("answers a failure of the sandbox's file route as unreachable", async () => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    gateway.fail.set("GET", () => new HttpResponse(null, { status: 500 }));

    expect(await call("sandbox_read_file", { path: "/workspace/a.txt" })).toBe(
      "The sandbox cannot be reached right now. Try again later.",
    );
  });

  it("writes nothing when the file's directory cannot be made", async () => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    // The conversation's directory is made, the file's own is refused.
    let made = 0;
    gateway.fail.set("MakeDir", () =>
      ++made === 1 ? HttpResponse.json({}) : refusedWith("internal", 500)(),
    );

    expect(await call("sandbox_write_file", { path: "/workspace/a/b.txt", content: "x" })).toBe(
      "The sandbox cannot be reached right now. Try again later.",
    );
    expect(gateway.calls).not.toContain("PUT /workspace/a/b.txt");
  });

  it("answers a failed write of the file as unreachable", async () => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    gateway.fail.set("PUT", () => new HttpResponse(null, { status: 500 }));

    expect(await call("sandbox_write_file", { path: "/workspace/a.txt", content: "x" })).toBe(
      "The sandbox cannot be reached right now. Try again later.",
    );
    expect(gateway.files.has("/workspace/a.txt")).toBe(false);
  });

  it("says the sandbox is gone when the gateway does not know it at the write", async () => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    gateway.fail.set("PUT", () => HttpResponse.json({ error: "not found" }, { status: 404 }));

    expect(await call("sandbox_write_file", { path: "/workspace/a.txt", content: "x" })).toBe(
      "The sandbox is gone. Call the tool again: a new, empty sandbox will be started.",
    );
  });
});

describe("sandbox_save_file", () => {
  const save = (args: unknown, options: Parameters<typeof call>[2] = {}) =>
    call("sandbox_save_file", args, options);
  const TITLE_REFUSAL =
    "'title' is a file name of 1 to 165 characters, without '/', '\\', line breaks, control, invisible and text-direction characters.";
  /** The requests of a save that store something. */
  const stores = (): string[] =>
    requests().filter((name) => name === "artifacts" || name === "insert");

  beforeEach(() => {
    gateway.files.set("/workspace/out/report.csv", Buffer.from("a,b\n"));
  });

  it("saves the file where a result of the conversation goes, under the name of its path", async () => {
    expect(await save({ path: "/workspace/out/report.csv" })).toBe(
      JSON.stringify({
        fileId: "101",
        title: "report.csv",
        bytes: 4,
        where: "into folder 30",
      }),
    );
    expect(inserts).toEqual([{ folder: "30", title: "report.csv", type: "text/csv", bytes: 4 }]);
  });

  it("saves it under the title asked for", async () => {
    await save({ path: "/workspace/out/report.csv", title: " Sales 2026.csv " });
    expect(inserts[0]?.title).toBe("Sales 2026.csv");
  });

  it.each([
    ["x"],
    ["x".repeat(MAX_TITLE_CHARS)],
    ["\u0646\u0645\u06cc\u200c\u062e\u0648\u0627\u0647\u0645.csv"],
  ])(
    "saves it under a title of the shortest and the longest length taken, and one with a joiner: %j",
    async (title) => {
      await save({ path: "/workspace/out/report.csv", title });
      expect(inserts[0]?.title).toBe(title);
    },
  );

  it.each([
    [{}, "sandbox_save_file requires 'path', a path without '..'."],
    [{ path: "/workspace/a.txt", title: 5 }, "'title' is a file name: a string."],
    [{ path: "/workspace/a.txt", title: "a/b.txt" }, undefined],
    [{ path: "/workspace/a.txt", title: "a\\b.txt" }, undefined],
    [{ path: "/workspace/a.txt", title: "a\nb.txt" }, undefined],
    // DEL, a C1 control, a line separator, and the override that shows `fdp.html` as `lmth.pdf`.
    [{ path: "/workspace/a.txt", title: "a\u007fb.txt" }, undefined],
    [{ path: "/workspace/a.txt", title: "a\u0085b.txt" }, undefined],
    [{ path: "/workspace/a.txt", title: "a\u2028b.txt" }, undefined],
    [{ path: "/workspace/a.txt", title: "invoice\u202efdp.html" }, undefined],
    [{ path: "/workspace/a.txt", title: "a\u2066b.txt" }, undefined],
    // Characters that take no room: a zero-width space, a soft hyphen, a byte order mark, a tag.
    [{ path: "/workspace/a.txt", title: "Payroll\u200b.xlsx" }, undefined],
    [{ path: "/workspace/a.txt", title: "Pay\u00adroll.xlsx" }, undefined],
    [{ path: "/workspace/a.txt", title: "Pay\ufeffroll.xlsx" }, undefined],
    [{ path: "/workspace/a.txt", title: "Payroll\u{e0041}.xlsx" }, undefined],
    [{ path: "/workspace/a.txt", title: "  " }, undefined],
    [{ path: "/workspace/a.txt", title: ".." }, undefined],
    [{ path: "/workspace/a.txt", title: "." }, undefined],
    [{ path: "/workspace/a.txt", title: "x".repeat(MAX_TITLE_CHARS + 1) }, undefined],
    // A path that names no file has no name to save under.
    [{ path: "/" }, undefined],
  ])("refuses %j before a folder or a sandbox is asked for", async (args, message) => {
    expect(await save(args)).toBe(message ?? TITLE_REFUSAL);
    expect(upstream.seen).toEqual([]);
  });

  it("asks for no sandbox when there is no folder to save into", async () => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    // GET api/2.0/files/@my (FoldersController.GetMyFolder): a caller without the section.
    upstream.use(http.get("*/api/2.0/files/@my", () => docspaceError(404)));

    expect(await save({ path: "/workspace/out/report.csv" }, { entity: null })).toBe(
      "There is no folder the file could be saved into.",
    );
    expect(requests()).toEqual(["@my"]);
  });

  it("answers the portal's status and asks for no sandbox when the folder cannot be read", async () => {
    const warn = vi.spyOn(logger, "warn").mockImplementation(() => logger);
    upstream.use(http.get(FOLDER, () => docspaceError(500)));

    expect(await save({ path: "/workspace/out/report.csv" })).toBe(
      "The workspace could not be reached: it answered 500.",
    );
    expect(requests()).toEqual(["30"]);
    expect(warn).toHaveBeenCalled();
  });

  it("saves a file of the largest size taken", async () => {
    const piece = Buffer.alloc(1024 * 1024);
    gateway.fail.set("GET /workspace/most.bin", () =>
      chunked(...Array.from({ length: MAX_FILE_BYTES / piece.length }, () => piece)),
    );
    // The body is not read back here: the size is the one the result names.
    upstream.use(http.post(INSERT, () => HttpResponse.json(fileDto({ id: 101 }))));

    expect(await save({ path: "/workspace/most.bin" })).toContain(`"bytes":${MAX_FILE_BYTES}`);
  });

  it("saves nothing for a file that is not there, is empty or is over the limit of a saved file", async () => {
    gateway.files.set("/workspace/empty.csv", Buffer.alloc(0));
    // One byte over the limit, sent as the same megabyte again and again.
    const piece = Buffer.alloc(1024 * 1024);
    gateway.fail.set("GET /workspace/big.bin", () =>
      chunked(
        ...Array.from({ length: MAX_FILE_BYTES / piece.length }, () => piece),
        Buffer.alloc(1),
      ),
    );

    expect(await save({ path: "/workspace/none.txt" })).toBe(
      "/workspace/none.txt does not exist. No file was saved.",
    );
    expect(await save({ path: "/workspace/empty.csv" })).toBe(
      "/workspace/empty.csv is empty. No file was saved.",
    );
    expect(await save({ path: "/workspace/big.bin" })).toBe(
      `/workspace/big.bin is over ${MAX_FILE_BYTES} bytes. No file was saved.`,
    );
    expect(stores()).toEqual([]);
  });

  it("says nothing was saved only for a refusal of the workspace", async () => {
    const warn = vi.spyOn(logger, "warn").mockImplementation(() => logger);
    upstream.use(http.post(INSERT, () => docspaceError(403)));

    expect(await save({ path: "/workspace/out/report.csv" })).toBe(
      "The workspace refused to save report.csv: it answered 403. No file was saved.",
    );
    expect(warn).toHaveBeenCalledOnce();
  });

  it("names a refusal of the AI service by its status, in the answer and in the log", async () => {
    const warn = vi.spyOn(logger, "warn").mockImplementation(() => logger);
    upstream.use(http.post(ARTIFACTS, () => docspaceError(409, "MARKER of the body")));

    expect(await save({ path: "/workspace/out/report.csv" })).toBe(
      "The workspace refused to save report.csv: it answered 409. No file was saved.",
    );
    // The message of the error carries the upstream's address and body: neither is logged.
    expect(warn.mock.calls).toEqual([
      ["sandbox_save_file: report.csv was not saved: AI service 409 Conflict"],
    ]);
    expect(stores()).toEqual(["artifacts"]);
  });

  it.each([
    ["a 5xx", () => docspaceError(502)],
    ["an answer that is not a file", () => new HttpResponse("saved", { status: 200 })],
    ["a connection that drops", () => HttpResponse.error()],
  ])("says the file may have been saved after %s", async (_name, answer) => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    upstream.use(http.post(INSERT, answer));

    expect(await save({ path: "/workspace/out/report.csv" })).toBe(
      "The workspace's answer to saving report.csv could not be read: the file may or may not have been saved. Look for it before saving again.",
    );
  });

  it("answers a failure of the sandbox's file route as unreachable, with nothing saved", async () => {
    vi.spyOn(logger, "warn").mockImplementation(() => logger);
    gateway.fail.set("GET", () => new HttpResponse(null, { status: 500 }));

    expect(await save({ path: "/workspace/out/report.csv" })).toBe(
      "The sandbox cannot be reached right now. Try again later.",
    );
    expect(stores()).toEqual([]);
  });
});
