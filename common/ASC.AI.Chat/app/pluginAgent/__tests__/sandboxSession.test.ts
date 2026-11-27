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

import fs from "fs/promises";
import os from "os";
import path from "path";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type { PluginAgentConfig } from "../../../config/index.js";
import { HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import logger from "../../log.js";
import { SandboxError } from "../sandboxClient.js";
import {
  describe as describeFailure,
  npmConfiguration,
  openSandboxSession,
} from "../sandboxSession.js";
import type { PluginSkill } from "../skill.js";
import { fakeGateway, GATEWAY_URL, refusedWith } from "../../__tests__/helpers/sandboxGateway.js";

const upstream = useUpstream();

const CONFIG: PluginAgentConfig = {
  sandboxUrl: GATEWAY_URL,
  sandboxToken: "gw-secret",
  taskTimeoutSeconds: 1800,
  maxToolCalls: 150,
  maxConcurrentTasks: 2,
  sandboxTtlSeconds: 600,
  serviceUrl: "http://chat:9837",
  registryUrl: "https://registry.npmjs.org",
  cacheDir: "/nowhere",
  cacheMaxBytes: 1,
};
const TASK = { id: "0b0b0b0b-0000-4000-8000-000000000001", registryToken: "task-token" };

let skill: PluginSkill;

beforeEach(async () => {
  const dir = await fs.mkdtemp(path.join(os.tmpdir(), "sandbox-session-test-"));
  await fs.mkdir(path.join(dir, "scripts", "validator"), { recursive: true });
  await fs.mkdir(path.join(dir, "templates"));
  await fs.writeFile(path.join(dir, "scripts", "generate-plugin.mjs"), "generate");
  await fs.writeFile(path.join(dir, "scripts", "validator", "rules.mjs"), "rules");
  await fs.writeFile(path.join(dir, "templates", "tsconfig"), "{}");
  skill = {
    dir,
    instructions: "",
    sandboxFiles: [
      "scripts/generate-plugin.mjs",
      "scripts/validator/rules.mjs",
      "templates/tsconfig",
    ],
  };
});

afterEach(async () => {
  vi.useRealTimers();
  await fs.rm(skill.dir, { recursive: true, force: true });
});

const open = () => openSandboxSession(CONFIG, TASK, skill, new AbortController().signal);

describe("npmConfiguration", () => {
  it("names this service as the registry and the task's token for the paths under it", () => {
    expect(npmConfiguration("http://chat:9837", "tok")).toBe(
      [
        "registry=http://chat:9837/plugin-agent/npm/",
        "//chat:9837/plugin-agent/npm/:_authToken=tok",
        "audit=false",
        "fund=false",
        "update-notifier=false",
        "omit-lockfile-registry-resolved=true",
        "",
      ].join("\n"),
    );
    expect(npmConfiguration("https://chat.example", "tok")).toContain(
      "//chat.example/plugin-agent/npm/:_authToken=tok",
    );
  });

  it("names no registry and no token when the installation names no address of this service", () => {
    expect(npmConfiguration(undefined, "tok")).toBe(
      ["audit=false", "fund=false", "update-notifier=false", ""].join("\n"),
    );
  });
});

describe("openSandboxSession", () => {
  it("creates the sandbox for the task, copies the skill and writes the npm configuration", async () => {
    const gateway = fakeGateway(upstream);

    const session = await open();

    expect(session.id).toBe("sb-1");
    expect(session.env).toEqual({ NPM_CONFIG_USERCONFIG: "/workspace/.npmrc", CI: "1" });
    expect(upstream.seen[0]?.body).toEqual({ sessionId: TASK.id, ttl: "600s" });
    expect(gateway.calls.slice(0, 4)).toEqual([
      "Create",
      "MakeDir /workspace/.skill/scripts",
      "MakeDir /workspace/.skill/scripts/validator",
      "MakeDir /workspace/.skill/templates",
    ]);
    expect(
      Object.fromEntries([...gateway.files].map(([file, bytes]) => [file, String(bytes)])),
    ).toEqual({
      "/workspace/.skill/scripts/generate-plugin.mjs": "generate",
      "/workspace/.skill/scripts/validator/rules.mjs": "rules",
      "/workspace/.skill/templates/tsconfig": "{}",
      "/workspace/.npmrc": npmConfiguration(CONFIG.serviceUrl, TASK.registryToken),
    });
    expect(gateway.calls.at(-1)).toBe("PUT /workspace/.npmrc");
    await session.dispose();
  });

  it("deletes the sandbox when it is disposed and stops renewing it", async () => {
    vi.useFakeTimers({ toFake: ["setInterval", "clearInterval"] });
    const gateway = fakeGateway(upstream);
    const session = await open();

    await vi.advanceTimersByTimeAsync(200_000);
    await vi.waitFor(() => expect(gateway.calls.filter((c) => c === "Renew")).toHaveLength(1));
    expect(upstream.seen.at(-1)?.body).toEqual({ id: "sb-1", ttl: "600s" });

    await session.dispose();
    expect(gateway.calls.at(-1)).toBe("Delete");

    await vi.advanceTimersByTimeAsync(1_000_000);
    expect(gateway.calls.filter((c) => c === "Renew")).toHaveLength(1);
  });

  it("renews three times within the lifetime", async () => {
    vi.useFakeTimers({ toFake: ["setInterval", "clearInterval"] });
    const gateway = fakeGateway(upstream);
    const session = await open();

    await vi.advanceTimersByTimeAsync(600_000);
    await vi.waitFor(() => expect(gateway.calls.filter((c) => c === "Renew")).toHaveLength(3));
    await session.dispose();
  });

  it("says that the sandbox is lost when a renewal finds it gone", async () => {
    vi.useFakeTimers({ toFake: ["setInterval", "clearInterval"] });
    const gateway = fakeGateway(upstream);
    const session = await open();
    gateway.fail.set("Renew", refusedWith("failed_precondition", 400));

    await vi.advanceTimersByTimeAsync(200_000);
    await vi.waitFor(() => expect(session.lost.aborted).toBe(true));

    expect(session.lost.reason).toBeInstanceOf(SandboxError);
    await session.dispose();
  });

  it("keeps the task when a renewal fails for another reason", async () => {
    vi.useFakeTimers({ toFake: ["setInterval", "clearInterval"] });
    const gateway = fakeGateway(upstream);
    const session = await open();
    gateway.fail.set("Renew", () => HttpResponse.error());

    await vi.advanceTimersByTimeAsync(200_000);
    await vi.waitFor(() =>
      expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(expect.stringContaining("not renewed")),
    );

    expect(session.lost.aborted).toBe(false);
    await session.dispose();
  });

  it("does not throw when the sandbox cannot be deleted", async () => {
    const gateway = fakeGateway(upstream);
    const session = await open();
    gateway.fail.set("Delete", refusedWith("internal"));

    await expect(session.dispose()).resolves.toBeUndefined();

    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      "pluginAgent: sandbox sb-1 not deleted: sandbox Delete: unavailable (500 internal)",
    );
  });

  it("passes on a refused Create and has nothing to delete", async () => {
    const gateway = fakeGateway(upstream);
    gateway.fail.set("Create", refusedWith("resource_exhausted", 429));

    await expect(open()).rejects.toMatchObject({ kind: "busy" });

    expect(gateway.calls).toEqual(["Create"]);
  });

  it("looks for the sandbox and deletes it when Create ended without an answer", async () => {
    const gateway = fakeGateway(upstream);
    const stopped = new AbortController();
    let creates = 0;
    gateway.fail.set("Create", () => {
      creates += 1;
      if (creates === 1) {
        // The reply is stopped while the VM starts; the gateway finishes the creation anyway.
        stopped.abort(new Error("reply stopped"));
        return new Promise<Response>(() => undefined);
      }
      return HttpResponse.json({ id: "sb-orphan" });
    });

    await expect(openSandboxSession(CONFIG, TASK, skill, stopped.signal)).rejects.toThrow(
      "reply stopped",
    );

    expect(gateway.calls).toEqual(["Create", "Create", "Delete"]);
    // The same session both times: that is what makes the second Create answer the first's sandbox.
    expect(upstream.seen[1]?.body).toEqual({ sessionId: TASK.id, ttl: "600s" });
    expect(upstream.seen[2]?.body).toEqual({ id: "sb-orphan" });
  });

  it("looks for the sandbox when Create says that it did not come up in time", async () => {
    const gateway = fakeGateway(upstream);
    let creates = 0;
    gateway.fail.set("Create", () => {
      creates += 1;
      return creates === 1
        ? HttpResponse.json({ code: "deadline_exceeded" }, { status: 504 })
        : HttpResponse.json({ id: "sb-late" });
    });

    await expect(open()).rejects.toMatchObject({ kind: "busy", code: "deadline_exceeded" });

    expect(gateway.calls).toEqual(["Create", "Create", "Delete"]);
  });

  it("says so in the log when the sandbox it looks for cannot be found either", async () => {
    const gateway = fakeGateway(upstream);
    gateway.fail.set("Create", () => HttpResponse.error());

    await expect(open()).rejects.toMatchObject({ kind: "unavailable" });

    expect(gateway.calls).toEqual(["Create", "Create"]);
    expect(vi.mocked(logger.warn)).toHaveBeenCalledWith(
      expect.stringContaining(`a sandbox of task ${TASK.id} may be left to its lifetime`),
    );
  });

  it("renews the sandbox while it is still being prepared", async () => {
    vi.useFakeTimers({ toFake: ["setInterval", "clearInterval"] });
    const gateway = fakeGateway(upstream);
    // The last step of the preparation takes longer than a third of the lifetime.
    gateway.fail.set("PUT /workspace/.npmrc", async () => {
      await vi.advanceTimersByTimeAsync(200_000);
      await vi.waitFor(() => expect(gateway.calls).toContain("Renew"));
      return new HttpResponse(null, { status: 204 });
    });

    const session = await open();

    expect(gateway.calls.indexOf("Renew")).toBeGreaterThan(gateway.calls.indexOf("Create"));
    await session.dispose();
  });

  it.each([
    ["a directory cannot be made", "MakeDir"],
    ["a file of the skill cannot be written", "PUT /workspace/.skill/templates/tsconfig"],
    ["the npm configuration cannot be written", "PUT /workspace/.npmrc"],
  ])("deletes the sandbox when %s", async (_name, step) => {
    const gateway = fakeGateway(upstream);
    gateway.fail.set(step, () => HttpResponse.json({ error: "x" }, { status: 503 }));

    await expect(open()).rejects.toMatchObject({ kind: "unavailable" });

    expect(gateway.calls.at(-1)).toBe("Delete");
  });

  it("deletes the sandbox when a file of the skill is gone from the disk", async () => {
    const gateway = fakeGateway(upstream);
    await fs.rm(path.join(skill.dir, "templates", "tsconfig"));

    await expect(open()).rejects.toMatchObject({ code: "ENOENT" });

    expect(gateway.calls.at(-1)).toBe("Delete");
  });
});

describe("describe", () => {
  it("adds what the gateway said to a sandbox error and leaves another error as its text", () => {
    expect(describeFailure(new SandboxError("lost", "Renew", "400 failed_precondition"))).toBe(
      "sandbox Renew: lost (400 failed_precondition)",
    );
    expect(describeFailure(new Error("boom"))).toBe("Error: boom");
  });
});
