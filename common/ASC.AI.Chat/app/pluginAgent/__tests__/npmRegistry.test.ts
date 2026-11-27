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

import { createHash } from "crypto";
import fs from "fs/promises";
import os from "os";
import path from "path";
import express from "express";
import request from "supertest";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { createApp } from "../../createApp.js";
import { http, HttpResponse, stalled, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import {
  forgetKeptArchives,
  keepArchive,
  MAX_REGISTRY_FETCHES,
  PLUGIN_AGENT_NPM_PATH,
  registerPluginAgentRegistry,
  registryTarget,
  rewriteArchiveLinks,
} from "../npmRegistry.js";
import { endTask, startTask } from "../tasks.js";
import type { RunningTask } from "../tasks.js";

const upstream = useUpstream({ localApp: true });

const REGISTRY = "http://registry.test";
const SERVICE = "http://chat:9837";
const MIB = 1024 * 1024;

let cacheDir = "";
let task: RunningTask;

const app = express();
registerPluginAgentRegistry(app);

/** `token`: the running task's own when left out, none at all when `null`. */
const get = (target: string, token?: string | null) => {
  const call = request(app).get(`${PLUGIN_AGENT_NPM_PATH}${target}`);
  return token === null ? call : call.set("Authorization", `Bearer ${token ?? task.registryToken}`);
};

const keptFile = (target: string): string =>
  path.join(cacheDir, `${createHash("sha256").update(target).digest("hex")}.tgz`);

beforeEach(async () => {
  cacheDir = await fs.mkdtemp(path.join(os.tmpdir(), "npm-registry-test-"));
  vi.stubEnv("AI__PLUGIN_AGENT__SANDBOX_URL", "http://gateway:9090");
  vi.stubEnv("AI__PLUGIN_AGENT__SANDBOX_TOKEN", "gw-secret");
  vi.stubEnv("AI__PLUGIN_AGENT__REGISTRY_URL", REGISTRY);
  vi.stubEnv("AI__PLUGIN_AGENT__SERVICE_URL", SERVICE);
  vi.stubEnv("AI__PLUGIN_AGENT__CACHE_DIR", cacheDir);
  forgetKeptArchives();
  task = startTask(1)!;
});

afterEach(async () => {
  endTask(task);
  vi.unstubAllEnvs();
  await fs.rm(cacheDir, { recursive: true, force: true });
});

describe("registryTarget", () => {
  it.each([
    ["/react", { kind: "document", path: "react" }],
    ["/@types%2fnode", { kind: "document", path: "@types%2fnode" }],
    ["/@types/node", { kind: "document", path: "@types%2fnode" }],
    ["/react/-/react-19.0.0.tgz", { kind: "archive", path: "react/-/react-19.0.0.tgz" }],
    [
      "/@types/node/-/node-22.1.0+build.1.tgz",
      { kind: "archive", path: "@types/node/-/node-22.1.0+build.1.tgz" },
    ],
    ["/JSONStream", { kind: "document", path: "JSONStream" }],
  ])("reads %s", (rawPath, expected) => {
    expect(registryTarget(rawPath)).toEqual(expected);
  });

  it.each([
    "/",
    "",
    "/..",
    "/.",
    "/%2e%2e",
    "/../etc/passwd",
    "/react/../lodash",
    "/react/-/../../x.tgz",
    "/react/-/..%2f..%2fx.tgz",
    "/react/-/react.zip",
    "/react/-/",
    "/react/-",
    "/react/x/react-1.0.0.tgz",
    "/react/-/react-1.0.0.tgz/more",
    "/@scope",
    "/@/name",
    "/@scope/..",
    "/-/npm/v1/security/advisories/bulk",
    "/react%",
    "/re act",
    "/react/",
  ])("answers nothing for %s", (rawPath) => {
    expect(registryTarget(rawPath)).toBeUndefined();
  });
});

describe("rewriteArchiveLinks", () => {
  it("points the links that lead to the registry at the route and leaves the others", () => {
    const document = {
      name: "x",
      versions: {
        "1.0.0": { dist: { tarball: `${REGISTRY}/x/-/x-1.0.0.tgz`, integrity: "sha512-a" } },
        "1.1.0": { dist: { tarball: "http://elsewhere.test/x/-/x-1.1.0.tgz" } },
        "1.2.0": { dist: { tarball: `${REGISTRY}.evil.test/x/-/x-1.2.0.tgz` } },
        "2.0.0": { dist: {} },
        "3.0.0": {},
        "4.0.0": "not an object",
      },
    };
    rewriteArchiveLinks(document, REGISTRY, `${SERVICE}/plugin-agent/npm`);
    expect(document.versions).toEqual({
      "1.0.0": {
        dist: { tarball: `${SERVICE}/plugin-agent/npm/x/-/x-1.0.0.tgz`, integrity: "sha512-a" },
      },
      "1.1.0": { dist: { tarball: "http://elsewhere.test/x/-/x-1.1.0.tgz" } },
      "1.2.0": { dist: { tarball: `${REGISTRY}.evil.test/x/-/x-1.2.0.tgz` } },
      "2.0.0": { dist: {} },
      "3.0.0": {},
      "4.0.0": "not an object",
    });
  });

  it.each([[null], ["text"], [{}], [{ versions: [] }], [{ versions: "x" }]])(
    "leaves %j as it is",
    (document) => {
      const before = JSON.stringify(document);
      rewriteArchiveLinks(document, REGISTRY, SERVICE);
      expect(JSON.stringify(document)).toBe(before);
    },
  );
});

describe("the package registry route", () => {
  it("serves a package document with its archive links pointing back at the route", async () => {
    upstream.use(
      http.get(`${REGISTRY}/react`, () =>
        HttpResponse.json(
          {
            name: "react",
            versions: { "19.0.0": { dist: { tarball: `${REGISTRY}/react/-/react-19.0.0.tgz` } } },
          },
          { headers: { "Content-Type": "application/vnd.npm.install-v1+json" } },
        ),
      ),
      http.get(`${REGISTRY}/react/-/react-19.0.0.tgz`, () => new HttpResponse("archive bytes")),
    );

    const document = await get("/react");

    expect(document.status).toBe(200);
    expect(document.headers["content-type"]).toContain("application/vnd.npm.install-v1+json");
    const link: string = JSON.parse(document.text).versions["19.0.0"].dist.tarball;
    expect(link).toBe(`${SERVICE}${PLUGIN_AGENT_NPM_PATH}/react/-/react-19.0.0.tgz`);
    expect(upstream.seen[0]?.headers["accept"]).toBe("application/vnd.npm.install-v1+json");
    // Nothing of the sandbox's request goes on to the registry.
    expect(upstream.seen[0]?.headers["authorization"]).toBeUndefined();

    const archive = await get(link.slice(SERVICE.length + PLUGIN_AGENT_NPM_PATH.length));
    expect(archive.status).toBe(200);
    expect(archive.headers["content-type"]).toContain("application/octet-stream");
    expect(Buffer.from(archive.body).toString()).toBe("archive bytes");
  });

  it("asks the registry for a scoped package the way the registry spells it", async () => {
    upstream.use(
      http.get(`${REGISTRY}/@types%2fnode`, () => HttpResponse.json({ name: "@types/node" })),
    );
    const res = await get("/@types%2fnode");
    expect(res.status).toBe(200);
    expect(res.body).toEqual({ name: "@types/node" });
  });

  it("answers a HEAD without a body", async () => {
    upstream.use(http.get(`${REGISTRY}/react`, () => HttpResponse.json({ name: "react" })));
    const res = await request(app)
      .head(`${PLUGIN_AGENT_NPM_PATH}/react`)
      .set("Authorization", `Bearer ${task.registryToken}`);
    expect(res.status).toBe(200);
    expect(res.text ?? "").toBe("");
  });

  it.each([
    ["no token", null],
    ["an empty token", ""],
    ["a token no task holds", "x".repeat(43)],
  ])("answers 401 for %s, without asking the registry", async (_name, token) => {
    const res = await get("/react", token);
    expect(res.status).toBe(401);
    expect(res.body).toEqual({ error: "Unauthorized" });
    expect(upstream.seen).toHaveLength(0);
  });

  it("answers 401 for a token that is not sent as a bearer", async () => {
    const res = await request(app)
      .get(`${PLUGIN_AGENT_NPM_PATH}/react`)
      .set("Authorization", `Basic ${task.registryToken}`);
    expect(res.status).toBe(401);
  });

  it("answers 401 for the token of a task that has ended", async () => {
    endTask(task);
    expect((await get("/react")).status).toBe(401);
    expect(upstream.seen).toHaveLength(0);
  });

  it("answers 401 for the token of a running task when the plugin agent is not configured", async () => {
    vi.stubEnv("AI__PLUGIN_AGENT__SANDBOX_URL", "");
    expect((await get("/react")).status).toBe(401);
  });

  it("answers 401 for the token of a running task when the installation names no address of this service", async () => {
    vi.stubEnv("AI__PLUGIN_AGENT__SERVICE_URL", "");
    expect((await get("/react")).status).toBe(401);
    expect(upstream.seen).toHaveLength(0);
  });

  it.each(["put", "post", "delete", "patch"] as const)(
    "answers 405 for %s and sends nothing to the registry",
    async (method) => {
      const res = await request(app)
        [method](`${PLUGIN_AGENT_NPM_PATH}/react`)
        .set("Authorization", `Bearer ${task.registryToken}`);
      expect(res.status).toBe(405);
      expect(res.headers["allow"]).toBe("GET, HEAD");
      expect(upstream.seen).toHaveLength(0);
    },
  );

  it("answers 404 for a path that names neither a package nor an archive", async () => {
    for (const target of ["/react/-/react.zip", "/-/npm/v1/security/advisories/bulk", "/%2e%2e"]) {
      expect((await get(target)).status).toBe(404);
    }
    expect(upstream.seen).toHaveLength(0);
  });

  it("answers 404 for a package and an archive the registry does not have", async () => {
    upstream.use(
      http.get(`${REGISTRY}/nope`, () =>
        HttpResponse.json({ error: "Not found" }, { status: 404 }),
      ),
      http.get(`${REGISTRY}/nope/-/nope-1.0.0.tgz`, () => new HttpResponse(null, { status: 404 })),
    );
    expect((await get("/nope")).status).toBe(404);
    expect((await get("/nope/-/nope-1.0.0.tgz")).status).toBe(404);
  });

  it.each([
    ["answers 500", () => HttpResponse.json({ error: "secret upstream text" }, { status: 500 })],
    ["answers 403", () => HttpResponse.json({ error: "secret upstream text" }, { status: 403 })],
    ["cannot be reached", () => HttpResponse.error()],
  ])("answers 502 when the registry %s", async (_name, resolver) => {
    upstream.use(
      http.get(`${REGISTRY}/react`, resolver),
      http.get(`${REGISTRY}/react/-/react-19.0.0.tgz`, resolver),
    );
    for (const target of ["/react", "/react/-/react-19.0.0.tgz"]) {
      const res = await get(target);
      expect(res.status).toBe(502);
      expect(res.text).toBe('{"error":"Bad Gateway"}');
    }
  });

  it("answers 502 when the registry never answers", async () => {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
    try {
      upstream.use(http.get(`${REGISTRY}/react`, stalled));
      const pending = get("/react").then((res) => res.status);
      await vi.waitFor(() => expect(upstream.seen).toHaveLength(1));
      await vi.advanceTimersByTimeAsync(120_000);
      expect(await pending).toBe(502);
    } finally {
      vi.useRealTimers();
    }
  });

  it("does not follow a redirect of the registry to another address", async () => {
    upstream.use(
      http.get(`${REGISTRY}/react`, () =>
        HttpResponse.redirect("http://169.254.169.254/latest/meta-data/", 302),
      ),
      http.get(`${REGISTRY}/react/-/react-19.0.0.tgz`, () =>
        HttpResponse.redirect("http://elsewhere.test/react-19.0.0.tgz", 302),
      ),
    );

    expect((await get("/react")).status).toBe(502);
    expect((await get("/react/-/react-19.0.0.tgz")).status).toBe(502);

    // Neither address was asked: msw would list a request nobody answers.
    expect(upstream.seen.map((r) => r.path)).toEqual(["/react", "/react/-/react-19.0.0.tgz"]);
    expect(await fs.readdir(cacheDir)).toEqual([]);
  });

  it("lets four requests be served at a time and the rest wait their turn", async () => {
    expect(MAX_REGISTRY_FETCHES).toBe(4);
    let inFlight = 0;
    let most = 0;
    const release: (() => void)[] = [];
    upstream.use(
      http.get(`${REGISTRY}/:name`, async ({ params }) => {
        inFlight += 1;
        most = Math.max(most, inFlight);
        await new Promise<void>((resolve) => release.push(resolve));
        inFlight -= 1;
        return HttpResponse.json({ name: params["name"] });
      }),
    );

    const answers = ["a", "b", "c", "d", "e", "f"].map((name) => get(`/${name}`).then((r) => r));
    await vi.waitFor(() => expect(release).toHaveLength(4));
    expect(upstream.seen).toHaveLength(4);
    while (release.length > 0 || inFlight > 0) {
      release.shift()?.();
      await new Promise((resolve) => setImmediate(resolve));
    }

    expect((await Promise.all(answers)).map((r) => r.status)).toEqual([
      200, 200, 200, 200, 200, 200,
    ]);
    expect(most).toBe(4);
  });

  it("serves a kept archive in the same turns, so that reads from the cache are bounded too", async () => {
    const target = "react/-/react-19.0.0.tgz";
    upstream.use(http.get(`${REGISTRY}/${target}`, () => new HttpResponse("archive bytes")));
    expect((await get(`/${target}`)).status).toBe(200);

    // Every read of the kept file waits until the test lets it go.
    const readFile = fs.readFile.bind(fs);
    const release: (() => void)[] = [];
    const reads = vi.spyOn(fs, "readFile").mockImplementation(async (...args) => {
      await new Promise<void>((resolve) => release.push(resolve));
      return readFile(...(args as Parameters<typeof fs.readFile>));
    });

    const answers = Array.from({ length: 6 }, () => get(`/${target}`).then((r) => r));
    await vi.waitFor(() => expect(release).toHaveLength(4));
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(reads).toHaveBeenCalledTimes(4);

    while (reads.mock.calls.length < 6 || release.length > 0) {
      release.shift()?.();
      await new Promise((resolve) => setImmediate(resolve));
    }
    expect((await Promise.all(answers)).map((r) => r.status)).toEqual([
      200, 200, 200, 200, 200, 200,
    ]);
  });

  it("ends the fetch when the sandbox hangs up, and gives the place back", async () => {
    let upstreamSignal: AbortSignal | undefined;
    upstream.use(
      http.get(`${REGISTRY}/slow`, ({ request }) => {
        upstreamSignal = request.signal;
        return stalled();
      }),
    );
    const server = app.listen(0, "127.0.0.1");
    await new Promise((resolve) => server.once("listening", resolve));
    try {
      const { port } = server.address() as { port: number };
      const client = new AbortController();
      const pending = fetch(`http://127.0.0.1:${port}${PLUGIN_AGENT_NPM_PATH}/slow`, {
        headers: { Authorization: `Bearer ${task.registryToken}` },
        signal: client.signal,
      }).catch((err: unknown) => err);
      await vi.waitFor(() => expect(upstreamSignal).toBeDefined());

      client.abort();
      await pending;

      await vi.waitFor(() => expect(upstreamSignal?.aborted).toBe(true));
      // The place is free again: four more requests are all served.
      upstream.use(http.get(`${REGISTRY}/:name`, ({ params }) => HttpResponse.json(params)));
      const next = await Promise.all(["a", "b", "c", "d"].map((name) => get(`/${name}`)));
      expect(next.map((r) => r.status)).toEqual([200, 200, 200, 200]);
    } finally {
      await new Promise((resolve) => server.close(resolve));
    }
  });

  it("gives its place back when a fetch fails", async () => {
    upstream.use(http.get(`${REGISTRY}/:name`, () => HttpResponse.error()));
    for (let i = 0; i < 6; i += 1) {
      expect((await get(`/pkg${i}`)).status).toBe(502);
    }
    upstream.use(http.get(`${REGISTRY}/ok`, () => HttpResponse.json({ name: "ok" })));
    expect((await get("/ok")).status).toBe(200);
  });

  it("answers 502 for a document that is not JSON", async () => {
    upstream.use(http.get(`${REGISTRY}/react`, () => new HttpResponse("<html>")));
    expect((await get("/react")).status).toBe(502);
  });

  it("gives a document without a content type the type of bytes", async () => {
    upstream.use(
      http.get(
        `${REGISTRY}/react`,
        () =>
          new HttpResponse(Buffer.from('{"name":"react"}'), { headers: { "Content-Type": "" } }),
      ),
    );
    const res = await get("/react");
    expect(res.status).toBe(200);
    expect(res.headers["content-type"]).toContain("application/octet-stream");
  });
});

describe("the route in the application", () => {
  it("is served outside the API prefix, with the task's token as its only gate", async () => {
    upstream.use(http.get(`${REGISTRY}/react`, () => HttpResponse.json({ name: "react" })));
    const application = createApp();

    const admitted = await request(application)
      .get(`${PLUGIN_AGENT_NPM_PATH}/react`)
      .set("Authorization", `Bearer ${task.registryToken}`);
    const refused = await request(application).get(`${PLUGIN_AGENT_NPM_PATH}/react`);

    expect(admitted.status).toBe(200);
    expect(admitted.body).toEqual({ name: "react" });
    expect(refused.status).toBe(401);
    expect(refused.body).toEqual({ error: "Unauthorized" });
  });
});

describe("kept archives", () => {
  const TARGET = "react/-/react-19.0.0.tgz";

  it("serves an archive fetched before while the registry does not answer", async () => {
    let calls = 0;
    upstream.use(
      http.get(`${REGISTRY}/${TARGET}`, () => {
        calls += 1;
        return calls === 1 ? new HttpResponse("archive bytes") : HttpResponse.error();
      }),
    );

    expect((await get(`/${TARGET}`)).status).toBe(200);
    const again = await get(`/${TARGET}`);

    expect(again.status).toBe(200);
    expect(Buffer.from(again.body).toString()).toBe("archive bytes");
    expect(calls).toBe(1);
    expect(await fs.readdir(cacheDir)).toEqual([path.basename(keptFile(TARGET))]);
  });

  it("keeps an archive of a scoped package inside the directory", async () => {
    const target = "@types/node/-/node-22.1.0.tgz";
    upstream.use(http.get(`${REGISTRY}/${target}`, () => new HttpResponse("x")));

    expect((await get(`/${target}`)).status).toBe(200);

    const kept = await fs.readdir(cacheDir);
    expect(kept).toHaveLength(1);
    expect(kept[0]).toMatch(/^[0-9a-f]{64}\.tgz$/);
  });

  it("still serves the archive when it cannot be kept", async () => {
    // A file where the directory should be: nothing can be written under it.
    await fs.rm(cacheDir, { recursive: true });
    await fs.writeFile(cacheDir, "in the way");
    upstream.use(http.get(`${REGISTRY}/${TARGET}`, () => new HttpResponse("archive bytes")));

    const res = await get(`/${TARGET}`);

    expect(res.status).toBe(200);
    expect(Buffer.from(res.body).toString()).toBe("archive bytes");
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      expect.stringContaining("an archive was not kept"),
    );
  });

  const age = async (file: string, secondsAgo: number): Promise<void> => {
    const at = new Date(Date.now() - secondsAgo * 1000);
    await fs.utimes(file, at, at);
  };
  /** A file of the cache by a short name: the hash of that name, as the route names an archive. */
  const file = (name: string): string => keptFile(name);
  const keep = (name: string, size: number, cacheMaxBytes: number): Promise<void> =>
    keepArchive({ cacheDir, cacheMaxBytes }, file(name), Buffer.alloc(size));
  const kept = async (...names: string[]): Promise<boolean[]> => {
    const present = new Set(await fs.readdir(cacheDir));
    return names.map((name) => present.has(path.basename(file(name))));
  };

  it("drops the archives read longest ago once the directory is over its limit", async () => {
    await keep("a", 40, 100);
    await keep("b", 40, 100);
    await age(file("a"), 10);
    await age(file("b"), 20);

    await keep("c", 40, 100);

    expect(await kept("a", "b", "c")).toEqual([true, false, true]);
  });

  it("goes down to nine tenths of the limit, so that the next write does not evict again", async () => {
    for (const [name, seconds] of [
      ["a", 50],
      ["b", 40],
      ["c", 30],
      ["d", 20],
    ] as const) {
      await keep(name, 25, 100);
      await age(file(name), seconds);
    }
    expect(await kept("a", "b", "c", "d")).toEqual([true, true, true, true]);

    // 105 bytes: one eviction of `a` would leave 80, under the 90 that is aimed for.
    await keep("e", 5, 100);
    expect(await kept("a", "b", "c", "d", "e")).toEqual([false, true, true, true, true]);

    // 85 now: a small write stays under the limit and evicts nothing.
    await keep("f", 10, 100);
    expect(await kept("b", "c", "d", "e", "f")).toEqual([true, true, true, true, true]);
  });

  it("goes on counting from what it knows the directory to hold", async () => {
    await keep("a", 40, 100);
    await keep("b", 40, 100);
    await keep("c", 20, 100);
    expect(await kept("a", "b", "c")).toEqual([true, true, true]);

    await age(file("a"), 30);
    await age(file("b"), 20);
    await age(file("c"), 10);
    await keep("d", 50, 100);

    expect(await kept("a", "b", "c", "d")).toEqual([false, false, true, true]);
  });

  it("counts a half-written archive the process left behind and drops it", async () => {
    const leftover = `${file("x")}.3f2b8f0e-1c1d-4b7a-9d55-0a1b2c3d4e5f.partial`;
    await fs.writeFile(leftover, Buffer.alloc(80));
    await age(leftover, 3600);

    await keep("a", 30, 100);

    expect(await fs.readdir(cacheDir)).toEqual([path.basename(file("a"))]);
  });

  it("leaves a half-written archive of the last minutes alone: another request may be writing it", async () => {
    const inFlight = `${file("x")}.3f2b8f0e-1c1d-4b7a-9d55-0a1b2c3d4e5f.partial`;
    await fs.writeFile(inFlight, Buffer.alloc(80));
    await age(inFlight, 60);

    await keep("a", 30, 100);

    expect((await fs.readdir(cacheDir)).sort()).toEqual(
      [path.basename(inFlight), path.basename(file("a"))].sort(),
    );
  });

  it("neither counts nor deletes what is not its own in the directory", async () => {
    await fs.writeFile(path.join(cacheDir, "somebody-elses.tgz"), Buffer.alloc(500));
    await fs.mkdir(path.join(cacheDir, "lost+found"));
    await age(path.join(cacheDir, "somebody-elses.tgz"), 600);
    await keep("a", 40, 100);
    await age(file("a"), 60);

    await keep("b", 70, 100);

    expect((await fs.readdir(cacheDir)).sort()).toEqual(
      ["lost+found", "somebody-elses.tgz", path.basename(file("b"))].sort(),
    );
  });

  it("skips an entry it cannot delete and goes on with the next", async () => {
    // A directory under an archive's name: counted as an entry, not removable as a file.
    await fs.mkdir(file("stuck"));
    await age(file("stuck"), 600);
    await keep("a", 60, 100);
    await age(file("a"), 60);

    await keep("b", 60, 100);

    expect(await kept("stuck", "a", "b")).toEqual([true, false, true]);
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      expect.stringContaining("a file of the cache was not removed"),
    );
  });

  it("leaves no half file behind when an archive cannot be put in its place", async () => {
    // A directory where the archive should go: the rename fails after the bytes were written.
    await fs.mkdir(file("a"));

    await keep("a", 30, 100);

    expect(await fs.readdir(cacheDir)).toEqual([path.basename(file("a"))]);
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      expect.stringContaining("an archive was not kept"),
    );
  });

  it("keeps the archive it is serving even when that alone is over the limit", async () => {
    await keep("a", 10, 100);
    await age(file("a"), 60);

    await keep("big", 150, 100);

    expect(await kept("a", "big")).toEqual([false, true]);
  });

  it("answers 502 for an archive over the size it is ready to hold", async () => {
    const chunk = Buffer.alloc(MIB);
    let sent = 0;
    upstream.use(
      http.get(
        `${REGISTRY}/a/-/a-1.0.0.tgz`,
        () =>
          new HttpResponse(
            new ReadableStream<Uint8Array>({
              pull(controller) {
                sent += 1;
                if (sent > 129) {
                  controller.close();
                } else {
                  controller.enqueue(chunk);
                }
              },
            }),
          ),
      ),
    );
    expect((await get("/a/-/a-1.0.0.tgz")).status).toBe(502);
    expect(await fs.readdir(cacheDir)).toEqual([]);
  });
});
