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
import { describe, expect, it } from "vitest";

import { HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import {
  AGENT_TOOLS,
  callAgentTool,
  execReport,
  MAX_READ_BYTES,
  MAX_WRITE_CHARS,
  stepOf,
} from "../agentTools.js";
import { SESSION_ENV, toolContext } from "./context.js";
import { fakeGateway, refusedWith } from "./gateway.js";

const upstream = useUpstream();

describe("the tools of a plugin task", () => {
  it("offers a shell, files, the skill and the hand-over, none of them asking for approval", () => {
    expect(AGENT_TOOLS.map((tool) => tool.name)).toEqual([
      "bash",
      "read_file",
      "write_file",
      "read_skill",
      "deliver_plugin",
    ]);
    for (const tool of AGENT_TOOLS) {
      expect(tool).toMatchObject({
        serverType: "plugin-task",
        requireApproval: false,
        enabled: true,
      });
    }
  });

  it("answers an unknown tool by its name", async () => {
    expect(await callAgentTool(toolContext(), "rm_rf", {})).toBe('Unknown tool "rm_rf".');
  });
});

describe("stepOf", () => {
  it.each([
    ["bash", { command: "npm run build" }, "Running: npm run build"],
    [
      "bash",
      { command: "cat > a.ts <<'EOF'\nconst secret = 1;\nEOF" },
      "Running: cat > a.ts <<'EOF'",
    ],
    ["bash", { command: "x".repeat(200) }, `Running: ${"x".repeat(80)}`],
    ["bash", {}, "Running: "],
    ["read_file", { path: "/workspace/a.ts" }, "Reading /workspace/a.ts"],
    [
      "write_file",
      { path: "/workspace/a.ts", content: "const secret = 1;" },
      "Writing /workspace/a.ts",
    ],
    ["read_skill", { path: "references/build.md" }, "Reading the skill: references/build.md"],
    ["read_skill", {}, "Reading the skill: "],
    ["deliver_plugin", { project_dir: "/workspace/p" }, "Checking and packaging the plugin"],
    ["other", {}, "Calling other"],
    ["read_file", { path: `/${"p".repeat(300)}` }, `Reading /${"p".repeat(199)}`],
    ["x".repeat(300), {}, `Calling ${"x".repeat(200)}`],
  ])("describes %s %j", (name, args, line) => {
    expect(stepOf(name, args)).toBe(line);
  });
});

describe("execReport", () => {
  it("shows the exit code and the streams that have something", () => {
    expect(execReport({ code: 0, stdout: "", stderr: "", truncated: false })).toBe("exit code: 0");
    expect(execReport({ code: 2, stdout: "out", stderr: "err", truncated: true })).toBe(
      "exit code: 2\n[stdout]\nout\n[stderr]\nerr\n(output cut at 20000 characters per stream)",
    );
  });
});

describe("bash", () => {
  it("runs the command in the workspace with the session's environment", async () => {
    const gateway = fakeGateway(upstream);
    gateway.run = () => ({ stdout: "v24.0.0\n" });

    const result = await callAgentTool(toolContext(), "bash", { command: "node --version" });

    expect(result).toBe("exit code: 0\n[stdout]\nv24.0.0\n");
    expect(gateway.execs).toEqual([
      {
        id: "sb-1",
        command: "node --version",
        cwd: "/workspace",
        env: SESSION_ENV,
        timeout: "120s",
      },
    ]);
  });

  it("takes a timeout of 1 and of 900 seconds", async () => {
    const gateway = fakeGateway(upstream);
    await callAgentTool(toolContext(), "bash", { command: "a", timeout_seconds: 1 });
    await callAgentTool(toolContext(), "bash", { command: "b", timeout_seconds: 900 });
    expect(gateway.execs.map((call) => call.timeout)).toEqual(["1s", "900s"]);
  });

  it.each([[0], [-1], [901], [1.5], ["60"], [null]])(
    "refuses a timeout of %j without running anything",
    async (timeout) => {
      const gateway = fakeGateway(upstream);
      expect(
        await callAgentTool(toolContext(), "bash", { command: "a", timeout_seconds: timeout }),
      ).toBe("'timeout_seconds' is a whole number from 1 to 900.");
      expect(gateway.calls).toEqual([]);
    },
  );

  it.each([[undefined], [""], ["  \n"], [5]])("refuses the command %j", async (command) => {
    expect(await callAgentTool(toolContext(), "bash", { command })).toBe(
      "bash requires a non-empty 'command'.",
    );
  });

  it("answers a failed command as a result and a lost sandbox as a failure", async () => {
    const gateway = fakeGateway(upstream);
    gateway.run = () => ({ stderr: "boom", code: 1 });
    expect(await callAgentTool(toolContext(), "bash", { command: "false" })).toBe(
      "exit code: 1\n[stderr]\nboom",
    );

    gateway.run = () => HttpResponse.json({ code: "failed_precondition" }, { status: 400 });
    await expect(callAgentTool(toolContext(), "bash", { command: "true" })).rejects.toMatchObject({
      kind: "lost",
    });
  });
});

describe("read_file", () => {
  it("reads a file as text", async () => {
    const gateway = fakeGateway(upstream);
    gateway.files.set("/workspace/p/src/a.ts", Buffer.from("const a = 'é';"));
    expect(await callAgentTool(toolContext(), "read_file", { path: "/workspace/p/src/a.ts" })).toBe(
      "const a = 'é';",
    );
  });

  it("reads a file of exactly the limit and refuses one a byte larger", async () => {
    const gateway = fakeGateway(upstream);
    gateway.files.set("/at", Buffer.alloc(MAX_READ_BYTES, "a"));
    gateway.files.set("/over", Buffer.alloc(MAX_READ_BYTES + 1, "a"));
    expect(await callAgentTool(toolContext(), "read_file", { path: "/at" })).toHaveLength(
      MAX_READ_BYTES,
    );
    expect(await callAgentTool(toolContext(), "read_file", { path: "/over" })).toBe(
      "/over is over 262144 bytes: read a part of it with bash (head, sed, grep).",
    );
  });

  it("answers an empty file with a text, never with nothing", async () => {
    const gateway = fakeGateway(upstream);
    gateway.files.set("/workspace/p/.gitkeep", Buffer.alloc(0));
    // The fake answers an empty stream for it, as the gateway does.
    expect(await callAgentTool(toolContext(), "read_file", { path: "/workspace/p/.gitkeep" })).toBe(
      "(/workspace/p/.gitkeep is empty)",
    );
  });

  it("says that a file does not exist", async () => {
    fakeGateway(upstream);
    expect(await callAgentTool(toolContext(), "read_file", { path: "/workspace/nope" })).toBe(
      "/workspace/nope does not exist.",
    );
  });

  it.each([[undefined], ["relative/a.ts"], ["/workspace/../etc/passwd"], [7], [""]])(
    "refuses the path %j without asking the sandbox",
    async (target) => {
      const gateway = fakeGateway(upstream);
      expect(await callAgentTool(toolContext(), "read_file", { path: target })).toBe(
        "read_file requires 'path', an absolute path without '..'.",
      );
      expect(gateway.calls).toEqual([]);
    },
  );

  it("fails when the sandbox does not answer", async () => {
    const gateway = fakeGateway(upstream);
    gateway.fail.set("GET", () => HttpResponse.json({ error: "x" }, { status: 503 }));
    await expect(callAgentTool(toolContext(), "read_file", { path: "/a" })).rejects.toMatchObject({
      kind: "unavailable",
    });
  });
});

describe("write_file", () => {
  it("makes the directory and writes the file", async () => {
    const gateway = fakeGateway(upstream);

    const result = await callAgentTool(toolContext(), "write_file", {
      path: "/workspace/p/src/a.ts",
      content: "const a = 'é';",
    });

    expect(result).toBe("Wrote 15 bytes to /workspace/p/src/a.ts.");
    expect(gateway.calls).toEqual(["MakeDir /workspace/p/src", "PUT /workspace/p/src/a.ts"]);
    expect(String(gateway.files.get("/workspace/p/src/a.ts"))).toBe("const a = 'é';");
  });

  it("takes content of exactly the limit and refuses one character more", async () => {
    const gateway = fakeGateway(upstream);
    expect(
      await callAgentTool(toolContext(), "write_file", {
        path: "/at",
        content: "a".repeat(MAX_WRITE_CHARS),
      }),
    ).toBe(`Wrote ${MAX_WRITE_CHARS} bytes to /at.`);
    expect(
      await callAgentTool(toolContext(), "write_file", {
        path: "/over",
        content: "a".repeat(MAX_WRITE_CHARS + 1),
      }),
    ).toBe("'content' is over 1048576 characters: write the file in parts with bash.");
    expect(gateway.files.has("/over")).toBe(false);
  });

  it.each([
    [{ content: "x" }],
    [{ path: "a.ts", content: "x" }],
    [{ path: "/a/../b", content: "x" }],
    [{ path: "/a.ts" }],
    [{ path: "/a.ts", content: 5 }],
  ])("refuses %j without asking the sandbox", async (args) => {
    const gateway = fakeGateway(upstream);
    expect(await callAgentTool(toolContext(), "write_file", args)).toBe(
      "write_file requires 'path', an absolute path without '..', and 'content', a string.",
    );
    expect(gateway.calls).toEqual([]);
  });

  it("fails when the directory cannot be made", async () => {
    const gateway = fakeGateway(upstream);
    gateway.fail.set("MakeDir", refusedWith("not_found", 404));
    await expect(
      callAgentTool(toolContext(), "write_file", { path: "/a/b", content: "x" }),
    ).rejects.toMatchObject({ kind: "lost" });
  });
});

describe("read_skill", () => {
  it("reads a text of the skill from the service's own disk", async () => {
    const dir = await fs.mkdtemp(path.join(os.tmpdir(), "agent-tools-test-"));
    try {
      await fs.mkdir(path.join(dir, "references"));
      await fs.writeFile(path.join(dir, "references", "build.md"), "how to build");
      const context = toolContext({ dir });

      expect(await callAgentTool(context, "read_skill", { path: "references/build.md" })).toBe(
        "how to build",
      );
      expect(await callAgentTool(context, "read_skill", { path: "../x" })).toContain(
        "is not a file of the skill",
      );
    } finally {
      await fs.rm(dir, { recursive: true, force: true });
    }
  });

  it.each([[undefined], [""], [3]])("refuses the path %j", async (target) => {
    expect(await callAgentTool(toolContext(), "read_skill", { path: target })).toBe(
      "read_skill requires 'path', e.g. references/build.md.",
    );
  });
});
