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

// The sandbox as tools of the chat itself: the chat's model runs commands and keeps files in the
// caller's sandbox, in a directory of the conversation, and saves a file of it into the
// workspace. Unlike `plugin_agent` and `document_agent` nothing is delegated: there is no second
// model and no task, each call is one command or one file.
//
// Offered to every caller of the chat on an installation that names a sandbox for it
// (`ai.sandbox`). The sandbox never holds a credential of the portal: a file reaches the
// workspace only through `sandbox_save_file`, which uploads it here under the caller's session.

import path from "path";
import type { TMCPItem, ToolCallOptions, ToolsAdapter } from "@onlyoffice/ai-chat/core";

import {
  CHAT_SANDBOX_MAX_COMMAND_SECONDS,
  CHAT_SANDBOX_TYPE,
  getChatSandbox,
} from "../../config/index.js";
import type { ChatSandboxConfig } from "../../config/index.js";
import logger from "../log.js";
import { getString, isObject } from "../narrow.js";
import {
  execReport,
  MAX_OUTPUT_CHARS,
  MAX_READ_BYTES,
  MAX_WRITE_CHARS,
  sandboxPath,
} from "../pluginAgent/agentTools.js";
import {
  execInSandbox,
  makeDir,
  readSandboxFile,
  SandboxError,
  writeSandboxFile,
} from "../pluginAgent/sandboxClient.js";
import { getRoundSignal, getRoundThreadId } from "../requestContext.js";
import { readChatContext } from "../storage/chatContextSnapshot.js";
import { DocspaceApiHttpError } from "../storage/docspaceFilesApi.js";
import { mediaTypeOf } from "../storage/docspaceFileTransfer.js";
import { AiServiceHttpError } from "../storage/httpClient.js";
import { writeWasRefused } from "../storage/writeOutcome.js";
import { resolveUploadTarget } from "../tools/documentAgent.js";
import { MAX_FILE_BYTES, saveResult } from "../tools/documentAgentFiles.js";
import { acquireUserSandbox, CHAT_SANDBOX_ROOT, threadDirOf } from "./userSandbox.js";

export const SANDBOX_EXEC_TOOL = "sandbox_exec";
export const SANDBOX_READ_TOOL = "sandbox_read_file";
export const SANDBOX_WRITE_TOOL = "sandbox_write_file";
export const SANDBOX_SAVE_TOOL = "sandbox_save_file";

export const EXEC_DEFAULT_TIMEOUT_SECONDS = 60;
export const EXEC_MAX_TIMEOUT_SECONDS = CHAT_SANDBOX_MAX_COMMAND_SECONDS;
// The length `skills/createSkill.ts` takes for a name. The Files service cuts a longer title at
// 170 characters instead of refusing it, and the model would then name a file that is not there.
export const MAX_TITLE_CHARS = 165;

const TOOL_NAMES = new Set([
  SANDBOX_EXEC_TOOL,
  SANDBOX_READ_TOOL,
  SANDBOX_WRITE_TOOL,
  SANDBOX_SAVE_TOOL,
]);

function tool(name: string, description: string, properties: object, required: string[]): TMCPItem {
  return {
    name,
    description,
    inputSchema: { type: "object", properties, required },
    serverType: CHAT_SANDBOX_TYPE,
    // The group is listed in `systemServerTypes`, so the "ask" mode pauses a call. No approval
    // of its own: a command runs in a machine that holds no credential of the portal.
    requireApproval: false,
    enabled: true,
  };
}

function toolsOf(config: ChatSandboxConfig): TMCPItem[] {
  // Rounded down: after a command that ran to its timeout, the idle lifetime is what is left.
  const idleMinutes = Math.floor(config.idleTtlSeconds / 60);
  const idleTime = idleMinutes === 1 ? "a minute" : `${idleMinutes} minutes`;
  const file = {
    type: "string",
    description:
      "Absolute path inside the sandbox, or one relative to the conversation's directory",
  };
  return [
    tool(
      SANDBOX_EXEC_TOOL,
      "Run a shell command in the user's sandbox: an isolated Linux machine that holds no " +
        "credential of the portal and no file but those these tools wrote into it. The command " +
        `runs in the directory of this conversation, ${CHAT_SANDBOX_ROOT}/<conversation id> ` +
        "(print it with pwd); the user's other conversations have theirs beside it. Returns the " +
        "exit code, stdout and stderr. The sandbox and its files stay for the next calls for " +
        `about ${idleTime} after the last one has ended; some time after that a call starts an ` +
        "empty one without saying so, and files written earlier are gone. It is made for Python " +
        "scripts: expect python3 with numpy, pandas, scipy, matplotlib, openpyxl, python-docx, " +
        "python-pptx, pypdf, reportlab and Pillow, and no network, so nothing can be installed; " +
        "an installation may differ, so check before relying on a tool or on the network. Save " +
        `a file the user should get with ${SANDBOX_SAVE_TOOL}.`,
      {
        command: { type: "string", description: "The command, run by sh -c" },
        timeout_seconds: {
          type: "integer",
          description: `Kill the command after this many seconds; ${EXEC_DEFAULT_TIMEOUT_SECONDS} by default, ${EXEC_MAX_TIMEOUT_SECONDS} at most`,
        },
      },
      ["command"],
    ),
    tool(SANDBOX_READ_TOOL, "Read a text file of the sandbox.", { path: file }, ["path"]),
    tool(
      SANDBOX_WRITE_TOOL,
      "Write a text file of the sandbox, replacing it; missing directories are created.",
      { path: file, content: { type: "string", description: "The whole file" } },
      ["path", "content"],
    ),
    tool(
      SANDBOX_SAVE_TOOL,
      "Save a file of the sandbox as a new file of the workspace, so that the user gets it: " +
        "among the artifacts of a conversation of an AI project, else into the current folder " +
        "when the user may create there, else into My documents. A file of the same name is " +
        "not replaced. Returns the new file's id and where it was saved.",
      {
        path: file,
        title: {
          type: "string",
          description:
            "The file's name in the workspace, with its extension; the name in 'path' by default",
        },
      },
      ["path"],
    ),
  ];
}

// The message of an AI service error ends in the upstream's body, which a log line does not
// carry (ts-ai-style.md, State and logging): such an error is named by its status.
const forLog = (err: unknown): string =>
  err instanceof AiServiceHttpError ? `AI service ${err.status} ${err.statusText}` : String(err);

// What a title may not hold. A path separator, since the title is a name. A control character or
// a line separator, which the workspace would show as a break. A character that is not
// displayed: Unicode's Default_Ignorable_Code_Point (the marks that turn the direction of the
// text, with which `invoice<U+202E>fdp.html` is listed as a PDF; the zero-width space, the soft
// hyphen, the fillers, the variation selectors and the tags, with which a second `Payroll.xlsx`
// lies beside the first) and the three interlinear annotation marks, which that property leaves
// out. The model picks the title, and a text it read may have told it which. The joiners U+200C
// and U+200D are the exception: Persian and Indic names are written with them. A look-alike
// letter of another script is not caught by any of this.
const NOT_OF_A_NAME =
  /[/\\\p{Cc}\p{Zl}\p{Zp}\uFFF9-\uFFFB]|(?![\u200C\u200D])\p{Default_Ignorable_Code_Point}/u;

/** A file name this service saves under: none of the characters above, not over its length. */
export function titleOf(value: string): string | undefined {
  const title = value.trim();
  return title !== "" &&
    title !== "." &&
    title !== ".." &&
    title.length <= MAX_TITLE_CHARS &&
    !NOT_OF_A_NAME.test(title)
    ? title
    : undefined;
}

interface Call {
  config: ChatSandboxConfig;
  threadId: string;
  /** The directory of the conversation in the sandbox. */
  dir: string;
  signal: AbortSignal | undefined;
}

/** A path of the sandbox: as given when absolute, else under the conversation's directory. */
function pathOf(call: Call, value: unknown): string | undefined {
  return typeof value === "string" && value !== "" && !value.startsWith("/")
    ? sandboxPath(`${call.dir}/${value}`)
    : sandboxPath(value);
}

async function exec(call: Call, args: Record<string, unknown>): Promise<string> {
  const command = getString(args, "command");
  if (!command?.trim()) {
    return `${SANDBOX_EXEC_TOOL} requires a non-empty 'command'.`;
  }
  const asked = args["timeout_seconds"];
  if (
    asked !== undefined &&
    !(
      typeof asked === "number" &&
      Number.isInteger(asked) &&
      asked >= 1 &&
      asked <= EXEC_MAX_TIMEOUT_SECONDS
    )
  ) {
    return `'timeout_seconds' is a whole number from 1 to ${EXEC_MAX_TIMEOUT_SECONDS}.`;
  }
  const timeoutSeconds = asked ?? EXEC_DEFAULT_TIMEOUT_SECONDS;
  const id = await acquireUserSandbox(call.config, call.dir, call.signal);
  const result = await execInSandbox(call.config, id, command, {
    cwd: call.dir,
    timeoutSeconds,
    maxOutputChars: MAX_OUTPUT_CHARS,
    signal: call.signal,
  });
  return execReport(result);
}

async function readFile(call: Call, args: Record<string, unknown>): Promise<string> {
  const target = pathOf(call, args["path"]);
  if (!target) {
    return `${SANDBOX_READ_TOOL} requires 'path', a path without '..'.`;
  }
  const id = await acquireUserSandbox(call.config, call.dir, call.signal);
  try {
    const bytes = await readSandboxFile(call.config, id, target, MAX_READ_BYTES, call.signal);
    return bytes.length === 0 ? `(${target} is empty)` : Buffer.from(bytes).toString("utf8");
  } catch (err) {
    if (err instanceof SandboxError && err.kind === "absent") {
      return `${target} does not exist.`;
    }
    if (err instanceof SandboxError && err.kind === "tooLarge") {
      return `${target} is over ${MAX_READ_BYTES} bytes: read a part of it with ${SANDBOX_EXEC_TOOL} (head, sed, grep).`;
    }
    throw err;
  }
}

async function writeFile(call: Call, args: Record<string, unknown>): Promise<string> {
  const target = pathOf(call, args["path"]);
  const content = args["content"];
  if (!target || typeof content !== "string") {
    return `${SANDBOX_WRITE_TOOL} requires 'path', a path without '..', and 'content', a string.`;
  }
  if (content.length > MAX_WRITE_CHARS) {
    return `'content' is over ${MAX_WRITE_CHARS} characters: write the file in parts with ${SANDBOX_EXEC_TOOL}.`;
  }
  const id = await acquireUserSandbox(call.config, call.dir, call.signal);
  const bytes = Buffer.from(content, "utf8");
  const parent = path.posix.dirname(target);
  // The conversation's directory is made with the sandbox.
  if (parent !== call.dir) {
    await makeDir(call.config, id, parent, call.signal);
  }
  await writeSandboxFile(call.config, id, target, bytes, call.signal);
  return `Wrote ${bytes.length} bytes to ${target}.`;
}

async function saveFile(
  call: Call,
  args: Record<string, unknown>,
  entityId: string | undefined,
): Promise<string> {
  const target = pathOf(call, args["path"]);
  if (!target) {
    return `${SANDBOX_SAVE_TOOL} requires 'path', a path without '..'.`;
  }
  const asked = args["title"];
  const title = titleOf(typeof asked === "string" ? asked : path.posix.basename(target));
  if (asked !== undefined && typeof asked !== "string") {
    return "'title' is a file name: a string.";
  }
  if (!title) {
    return `'title' is a file name of 1 to ${MAX_TITLE_CHARS} characters, without '/', '\\', line breaks, control characters and characters that are not displayed (zero-width and text-direction marks and the like).`;
  }
  // The checks of the arguments and of the folder come before the sandbox is asked
  // (ts-ai-failure-paths.md §6); what the file itself is refused for shows only once it is read.
  const folderId = await resolveUploadTarget(entityId);
  if (!folderId) {
    return "There is no folder the file could be saved into.";
  }
  const id = await acquireUserSandbox(call.config, call.dir, call.signal);
  let bytes: Uint8Array;
  try {
    bytes = await readSandboxFile(call.config, id, target, MAX_FILE_BYTES, call.signal);
  } catch (err) {
    if (err instanceof SandboxError && err.kind === "absent") {
      return `${target} does not exist. No file was saved.`;
    }
    if (err instanceof SandboxError && err.kind === "tooLarge") {
      return `${target} is over ${MAX_FILE_BYTES} bytes. No file was saved.`;
    }
    throw err;
  }
  if (bytes.length === 0) {
    // The artifacts of a project conversation refuse an empty file with the status that also
    // says "this conversation takes none", and the file would land in a folder instead.
    return `${target} is empty. No file was saved.`;
  }
  try {
    const saved = await saveResult(call.threadId, folderId, title, bytes, mediaTypeOf(title));
    return JSON.stringify({
      fileId: saved.id,
      title: saved.title,
      bytes: bytes.length,
      where: saved.where,
    });
  } catch (err) {
    // The save answers for itself (ts-ai-failure-paths.md §6): only a refusal says that
    // nothing was made.
    logger.warn(`${SANDBOX_SAVE_TOOL}: ${title} was not saved: ${forLog(err)}`);
    if (
      (err instanceof DocspaceApiHttpError || err instanceof AiServiceHttpError) &&
      writeWasRefused(err)
    ) {
      return `The workspace refused to save ${title}: it answered ${err.status}. No file was saved.`;
    }
    return `The workspace's answer to saving ${title} could not be read: the file may or may not have been saved. Look for it before saving again.`;
  }
}

export const sandboxTools: ToolsAdapter = {
  // Asked on every chat round: the configuration alone decides, and nothing is called.
  getTools(): Promise<Record<string, TMCPItem[]>> {
    const config = getChatSandbox();
    const groups: Record<string, TMCPItem[]> = {};
    if (config) {
      groups[CHAT_SANDBOX_TYPE] = toolsOf(config);
    }
    return Promise.resolve(groups);
  },

  async callTool(
    toolName: string,
    args: Record<string, unknown>,
    entityId?: string,
    _options?: ToolCallOptions,
  ): Promise<unknown> {
    if (!TOOL_NAMES.has(toolName)) {
      return `Unknown tool "${toolName}"`;
    }
    const config = getChatSandbox();
    if (!config) {
      return "The sandbox is not configured on this server.";
    }
    const threadId = getRoundThreadId() ?? readChatContext("thread")?.requested.threadId;
    if (!threadId) {
      return "A sandbox belongs to a conversation, and this call is made outside one.";
    }
    const dir = threadDirOf(threadId);
    if (!dir) {
      return "This conversation cannot have a directory in the sandbox.";
    }
    const input = isObject(args) ? args : {};
    const round = getRoundSignal();
    const call: Call = { config, threadId, dir, signal: round };
    try {
      switch (toolName) {
        case SANDBOX_EXEC_TOOL:
          return await exec(call, input);
        case SANDBOX_READ_TOOL:
          return await readFile(call, input);
        case SANDBOX_WRITE_TOOL:
          return await writeFile(call, input);
        default:
          return await saveFile(call, input, entityId);
      }
    } catch (err) {
      // Contract: a failure is a result, never a throw — a throw aborts the chat stream. A save
      // into the workspace answers for itself above, so nothing was saved by what arrives here.
      if (round?.aborted) {
        logger.info(`${toolName} was stopped with the reply: ${forLog(err)}`);
        return "The call was stopped with the reply.";
      }
      if (err instanceof SandboxError) {
        logger.warn(`${toolName}: ${err.message} (${err.detail})`);
        if (err.kind === "busy") {
          return "No sandbox is free right now. Try again in a few minutes.";
        }
        if (err.kind === "tooLarge") {
          return "The command's output could not be read: one piece of it was too large. Write the output to a file and read a part of it.";
        }
        // A file that is not there is answered where it is read. `absent` here is a write the
        // gateway answered 404: the sandbox itself is unknown to it.
        return err.kind === "lost" || err.kind === "absent"
          ? "The sandbox is gone. Call the tool again: a new, empty sandbox will be started."
          : "The sandbox cannot be reached right now. Try again later.";
      }
      // For the log only: the message of an upstream error names the internal address.
      logger.warn(`${toolName} failed: ${forLog(err)}`);
      // Only the portal is asked outside the save: who the caller is, and the folder to save into.
      if (err instanceof DocspaceApiHttpError) {
        return `The workspace could not be reached: it answered ${err.status}.`;
      }
      return "The call could not be completed due to an internal error.";
    }
  },
};
