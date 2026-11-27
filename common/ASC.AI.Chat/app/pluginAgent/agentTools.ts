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

// The tools the model of a plugin task works with: a shell and files in the task's sandbox, the
// texts of the skill, and the hand-over of the finished plugin. Everything a tool answers is
// text for the model; a failure of the sandbox itself (gone, unreachable, or a refusal this
// module has no reading for) ends the task instead.

import path from "path";
import type { TMCPItem } from "@onlyoffice/ai-chat/core";

import type { PluginAgentConfig } from "../../config/index.js";
import { getString } from "../narrow.js";
import {
  execInSandbox,
  makeDir,
  readSandboxFile,
  SandboxError,
  writeSandboxFile,
} from "./sandboxClient.js";
import type { ExecResult } from "./sandboxClient.js";
import { SANDBOX_SKILL_DIR, SANDBOX_WORKSPACE } from "./sandboxSession.js";
import type { SandboxSession } from "./sandboxSession.js";
import { readSkillFile } from "./skill.js";
import type { PluginSkill } from "./skill.js";

/** The group of the task's own tools: it never leaves this process, the chat does not see it. */
const SERVER_TYPE = "plugin-task";

export const DELIVER_TOOL = "deliver_plugin";

export const BASH_DEFAULT_TIMEOUT_SECONDS = 120;
export const BASH_MAX_TIMEOUT_SECONDS = 900;
/** What the model is shown of one command's stdout and of its stderr. */
export const MAX_OUTPUT_CHARS = 20_000;
export const MAX_READ_BYTES = 256 * 1024;
export const MAX_WRITE_CHARS = 1024 * 1024;
/** What a step line shows of a command, and of a path or a tool's name. */
const MAX_STEP_COMMAND_CHARS = 80;
const MAX_STEP_NAME_CHARS = 200;

function tool(name: string, description: string, properties: object, required: string[]): TMCPItem {
  return {
    name,
    description,
    inputSchema: { type: "object", properties, required },
    serverType: SERVER_TYPE,
    requireApproval: false,
    enabled: true,
  };
}

export const AGENT_TOOLS: TMCPItem[] = [
  tool(
    "bash",
    `Run a shell command in the sandbox, in ${SANDBOX_WORKSPACE}. Returns the exit code, stdout and stderr. ` +
      `The skill's scripts and templates are in ${SANDBOX_SKILL_DIR} ($SKILL of the skill's text). ` +
      "npm installs from a registry that is already configured; there is no other network access.",
    {
      command: { type: "string", description: "The command, run by sh -c" },
      timeout_seconds: {
        type: "integer",
        description: `Kill the command after this many seconds; ${BASH_DEFAULT_TIMEOUT_SECONDS} by default, ${BASH_MAX_TIMEOUT_SECONDS} at most`,
      },
    },
    ["command"],
  ),
  tool(
    "read_file",
    "Read a text file of the sandbox.",
    { path: { type: "string", description: "Absolute path inside the sandbox" } },
    ["path"],
  ),
  tool(
    "write_file",
    "Write a text file of the sandbox, replacing it; missing directories are created.",
    {
      path: { type: "string", description: "Absolute path inside the sandbox" },
      content: { type: "string", description: "The whole file" },
    },
    ["path", "content"],
  ),
  tool(
    "read_skill",
    "Read a text of the plugin skill: SKILL.md or a file under references/, as the skill links them.",
    {
      path: { type: "string", description: "Path relative to the skill, e.g. references/build.md" },
    },
    ["path"],
  ),
  tool(
    DELIVER_TOOL,
    "Hand the finished plugin over. Builds the project, runs its type check and the skill's validator; " +
      "when one fails, returns its output so that you can fix the project and call this again. " +
      "When all pass, the plugin package and its sources are saved and the task ends.",
    {
      project_dir: {
        type: "string",
        description: `Absolute path of the plugin project, a directory under ${SANDBOX_WORKSPACE}`,
      },
    },
    ["project_dir"],
  ),
];

export interface ToolContext {
  config: PluginAgentConfig;
  session: SandboxSession;
  skill: PluginSkill;
  signal: AbortSignal;
}

/**
 * One line for the user about a call: the path it is about, the start of the first line of a
 * command, a fixed line for the hand-over, or the name of a tool this module does not know.
 * Never a file's content or a command's output, and never more than a cut piece of what the
 * model wrote.
 */
export function stepOf(name: string, args: Record<string, unknown>): string {
  const target = (getString(args, "path") ?? "").slice(0, MAX_STEP_NAME_CHARS);
  switch (name) {
    case "bash": {
      const firstLine = (getString(args, "command") ?? "").split("\n")[0] ?? "";
      return `Running: ${firstLine.slice(0, MAX_STEP_COMMAND_CHARS)}`;
    }
    case "read_file":
      return `Reading ${target}`;
    case "write_file":
      return `Writing ${target}`;
    case "read_skill":
      return `Reading the skill: ${target}`;
    case DELIVER_TOOL:
      return "Checking and packaging the plugin";
    default:
      return `Calling ${name.slice(0, MAX_STEP_NAME_CHARS)}`;
  }
}

/** A command's result as the model reads it. */
export function execReport(result: ExecResult): string {
  return [
    `exit code: ${result.code}`,
    ...(result.stdout ? ["[stdout]", result.stdout] : []),
    ...(result.stderr ? ["[stderr]", result.stderr] : []),
    ...(result.truncated ? [`(output cut at ${MAX_OUTPUT_CHARS} characters per stream)`] : []),
  ].join("\n");
}

/** An absolute path of the sandbox without a `..` part, or undefined. */
export function sandboxPath(value: unknown): string | undefined {
  return typeof value === "string" && value.startsWith("/") && !value.split("/").includes("..")
    ? value
    : undefined;
}

async function bash(context: ToolContext, args: Record<string, unknown>): Promise<string> {
  const command = getString(args, "command");
  if (!command?.trim()) {
    return "bash requires a non-empty 'command'.";
  }
  const asked = args["timeout_seconds"];
  if (
    asked !== undefined &&
    !(
      typeof asked === "number" &&
      Number.isInteger(asked) &&
      asked >= 1 &&
      asked <= BASH_MAX_TIMEOUT_SECONDS
    )
  ) {
    return `'timeout_seconds' is a whole number from 1 to ${BASH_MAX_TIMEOUT_SECONDS}.`;
  }
  return execReport(
    await execInSandbox(context.config, context.session.id, command, {
      cwd: SANDBOX_WORKSPACE,
      env: context.session.env,
      timeoutSeconds: asked ?? BASH_DEFAULT_TIMEOUT_SECONDS,
      maxOutputChars: MAX_OUTPUT_CHARS,
      signal: context.signal,
    }),
  );
}

async function readFile(context: ToolContext, args: Record<string, unknown>): Promise<string> {
  const target = sandboxPath(args["path"]);
  if (!target) {
    return "read_file requires 'path', an absolute path without '..'.";
  }
  try {
    const bytes = await readSandboxFile(
      context.config,
      context.session.id,
      target,
      MAX_READ_BYTES,
      context.signal,
    );
    return bytes.length === 0 ? `(${target} is empty)` : Buffer.from(bytes).toString("utf8");
  } catch (err) {
    if (err instanceof SandboxError && err.kind === "absent") {
      return `${target} does not exist.`;
    }
    if (err instanceof SandboxError && err.kind === "tooLarge") {
      return `${target} is over ${MAX_READ_BYTES} bytes: read a part of it with bash (head, sed, grep).`;
    }
    throw err;
  }
}

async function writeFile(context: ToolContext, args: Record<string, unknown>): Promise<string> {
  const target = sandboxPath(args["path"]);
  const content = args["content"];
  if (!target || typeof content !== "string") {
    return "write_file requires 'path', an absolute path without '..', and 'content', a string.";
  }
  if (content.length > MAX_WRITE_CHARS) {
    return `'content' is over ${MAX_WRITE_CHARS} characters: write the file in parts with bash.`;
  }
  const bytes = Buffer.from(content, "utf8");
  await makeDir(context.config, context.session.id, path.posix.dirname(target), context.signal);
  await writeSandboxFile(context.config, context.session.id, target, bytes, context.signal);
  return `Wrote ${bytes.length} bytes to ${target}.`;
}

/** Runs one of the tools but `deliver_plugin`, which the task itself handles. */
export async function callAgentTool(
  context: ToolContext,
  name: string,
  args: Record<string, unknown>,
): Promise<string> {
  switch (name) {
    case "bash":
      return bash(context, args);
    case "read_file":
      return readFile(context, args);
    case "write_file":
      return writeFile(context, args);
    case "read_skill": {
      const requested = getString(args, "path");
      return requested
        ? readSkillFile(context.skill, requested)
        : "read_skill requires 'path', e.g. references/build.md.";
    }
    default:
      return `Unknown tool "${name}".`;
  }
}
