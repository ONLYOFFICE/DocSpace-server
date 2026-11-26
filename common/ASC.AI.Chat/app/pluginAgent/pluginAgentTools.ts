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

// The plugin agent as two tools of the chat. `plugin_agent` hands a description of a DocSpace
// plugin to a sub-agent that runs inside this process: it builds the plugin in a sandbox by the
// plugin skill, and leaves the package and its sources as two new files of the workspace.
// `install_plugin` installs such a package into the portal.
//
// Both are offered to the owner and the DocSpace admins of a portal only, on an installation
// that names a sandbox: a plugin runs in the browser of every user of the portal, so building
// one and installing one are an administrator's acts.
//
// Like `document_agent` the call waits for its task and reports the task's steps; unlike it
// the work happens here, and the sandbox never holds a credential of the portal: the sources of
// a follow-up task are read here, the results are saved here, the installation is made here.

import { ActionType } from "@onlyoffice/ai-chat/core";
import type { Profile, TMCPItem, ToolCallOptions, ToolsAdapter } from "@onlyoffice/ai-chat/core";

import {
  getCompaction,
  getPluginAgent,
  PLUGIN_AGENT_TYPE,
  PLUGIN_INSTALL_TYPE,
} from "../../config/index.js";
import type { PluginAgentConfig } from "../../config/index.js";
import logger from "../log.js";
import { isObject } from "../narrow.js";
import {
  getRoundProfileId,
  getRoundSignal,
  getRoundThreadId,
  markForwardHeadersToProvider,
} from "../requestContext.js";
import { invalidateChatContext, readChatContext } from "../storage/chatContextSnapshot.js";
import { DocspaceApiHttpError, getFileInfo } from "../storage/docspaceFilesApi.js";
import { addWebPlugin, isPortalAdmin } from "../storage/docspacePeopleApi.js";
import { AiServiceHttpError } from "../storage/httpClient.js";
import { storage } from "../storage/index.js";
import { FileTooLargeError, readFileBytes } from "../storage/roomSkills.js";
import { resolveUploadTarget } from "../tools/documentAgent.js";
import { MAX_FILE_BYTES, saveResult } from "../tools/documentAgentFiles.js";
import { AGENT_TOOLS, callAgentTool, DELIVER_TOOL, stepOf } from "./agentTools.js";
import type { ToolContext } from "./agentTools.js";
import { deliverPlugin, projectDirOf } from "./deliver.js";
import type { DeliveredPlugin } from "./deliver.js";
import { ModelTurnError, runLoop } from "./loop.js";
import type { ToolOutcome } from "./loop.js";
import { execInSandbox, SandboxError, writeSandboxFile } from "./sandboxClient.js";
import {
  describe,
  openSandboxSession,
  SANDBOX_SKILL_DIR,
  SANDBOX_WORKSPACE,
} from "./sandboxSession.js";
import { loadPluginSkill, pluginSkillPresent } from "./skill.js";
import type { PluginSkill } from "./skill.js";
import { endTask, startTask } from "./tasks.js";

export const PLUGIN_AGENT_TOOL_NAME = "plugin_agent";
export const INSTALL_PLUGIN_TOOL_NAME = "install_plugin";

export const MAX_TASK_CHARS = 20_000;
/** Where the sources of an earlier task are unpacked for a follow-up one. */
const FOLLOW_UP_DIR = `${SANDBOX_WORKSPACE}/plugin`;
const FOLLOW_UP_ARCHIVE = `${SANDBOX_WORKSPACE}/.input.zip`;
const UNPACK_TIMEOUT_SECONDS = 120;
/** What the model's last words are shown up to, when it stopped without handing a plugin over. */
const MAX_LAST_WORDS_CHARS = 500;
/** A zip archive starts with the signature of its first entry, or of its end when it is empty. */
const ZIP_SIGNATURES = ["PK\u0003\u0004", "PK\u0005\u0006"];

const PLUGIN_AGENT_TOOL: TMCPItem = {
  name: PLUGIN_AGENT_TOOL_NAME,
  description:
    "Build a plugin for this ONLYOFFICE DocSpace portal. Hand over a description of what the plugin " +
    "must do in 'task': an agent writes it in a sandbox, builds and validates it, which may take " +
    "many minutes, and saves two new files in the workspace: the plugin package (.zip) and an archive " +
    "of its sources (-src.zip). Nothing is installed. To change a plugin built earlier, pass the id of " +
    "its sources archive in 'sourcesFileId' and describe the change in 'task'. The result names both " +
    `file ids; install the package with ${INSTALL_PLUGIN_TOOL_NAME} only when the user asks for it.`,
  inputSchema: {
    type: "object",
    properties: {
      task: {
        type: "string",
        description: `What the plugin must do, or what to change in it; up to ${MAX_TASK_CHARS} characters.`,
      },
      sourcesFileId: {
        type: "integer",
        description:
          "The file id of a '-src.zip' archive an earlier task saved, to continue from it.",
      },
    },
    required: ["task"],
  },
  serverType: PLUGIN_AGENT_TYPE,
  // Unlike `document_agent` it asks under the default permission mode too: a task takes many
  // minutes of a sandbox and of the model. The user may pre-approve it, and the mode that runs
  // every tool unasked runs it as well.
  requireApproval: true,
  enabled: true,
};

const INSTALL_PLUGIN_TOOL: TMCPItem = {
  name: INSTALL_PLUGIN_TOOL_NAME,
  description:
    "Install a plugin package into this portal: the .zip file a plugin task saved, by its file id. " +
    "The plugin then runs for every user of the portal, so call this only when the user asked to " +
    "install it. A package with the name of an installed plugin replaces that plugin.",
  inputSchema: {
    type: "object",
    properties: {
      fileId: { type: "integer", description: "The file id of the plugin package (.zip)." },
    },
    required: ["fileId"],
  },
  serverType: PLUGIN_INSTALL_TYPE,
  requireApproval: true,
  enabled: true,
};

/** A whole number above zero, as a file id is; anything else is not one. */
function positiveInteger(value: unknown): number | undefined {
  return typeof value === "number" && Number.isSafeInteger(value) && value > 0 ? value : undefined;
}

/**
 * Whether the caller is the owner or a DocSpace admin; `unknown` when the role could not be
 * read. Unknown is never treated as an admin: the check guards who may put code into every
 * user's browser, so it fails closed (ts-ai-failure-paths.md §1), and the failure is logged.
 */
async function callerRole(): Promise<"admin" | "other" | "unknown"> {
  try {
    return (await isPortalAdmin()) ? "admin" : "other";
  } catch (err) {
    logger.warn(`pluginAgent: the caller's role was not read, the tools are off: ${String(err)}`);
    return "unknown";
  }
}

/** Whether an upstream answered with a refusal: a 4xx says that the thing asked for was not done. */
function refusedStatus(err: unknown): number | undefined {
  const status =
    err instanceof DocspaceApiHttpError || err instanceof AiServiceHttpError
      ? err.status
      : undefined;
  return status !== undefined && status >= 400 && status < 500 ? status : undefined;
}

export const pluginAgentTools: ToolsAdapter = {
  // Asked on every chat round: what is cheap comes first, and nothing here fails the round.
  async getTools(): Promise<Record<string, TMCPItem[]>> {
    if (!getPluginAgent()) {
      return {};
    }
    try {
      if (!(await pluginSkillPresent())) {
        return {};
      }
    } catch (err) {
      // A disk that cannot be read is a broken installation of an optional feature.
      logger.warn(`pluginAgent: the plugin skill was not read, the tools are off: ${String(err)}`);
      return {};
    }
    if ((await callerRole()) !== "admin") {
      return {};
    }
    return {
      [PLUGIN_AGENT_TYPE]: [PLUGIN_AGENT_TOOL],
      [PLUGIN_INSTALL_TYPE]: [INSTALL_PLUGIN_TOOL],
    };
  },

  async callTool(
    toolName: string,
    args: Record<string, unknown>,
    entityId?: string,
    options?: ToolCallOptions,
  ): Promise<unknown> {
    if (toolName !== PLUGIN_AGENT_TOOL_NAME && toolName !== INSTALL_PLUGIN_TOOL_NAME) {
      return `Unknown tool "${toolName}"`;
    }
    const input = isObject(args) ? args : {};
    try {
      const config = getPluginAgent();
      if (!config) {
        return "The plugin agent is not configured on this server.";
      }
      // The tools are offered to an admin only; the role is read again where the work starts,
      // as an offer made in one round does not vouch for the call of another.
      const role = await callerRole();
      if (role !== "admin") {
        return role === "other"
          ? "Only the owner and the DocSpace admins of the portal may build or install plugins."
          : "The caller's role could not be read just now, so nothing was started. Try again.";
      }
      if (toolName === INSTALL_PLUGIN_TOOL_NAME) {
        return await installPlugin(input, entityId);
      }
      const skill = await loadPluginSkill();
      if (!skill) {
        return "The plugin agent is not configured on this server.";
      }
      return await buildPlugin(config, skill, input, entityId, options);
    } catch (err) {
      // Contract: a failure is a result, never a throw — a throw aborts the chat stream. What
      // arrives here failed before anything was saved or sent to the portal: the steps that
      // create something answer for themselves. For the log only: the message of an upstream
      // error names the internal address it came from.
      logger.warn(`${toolName} failed: ${String(err)}`);
      const nothing =
        toolName === PLUGIN_AGENT_TOOL_NAME ? "No file was saved." : "Nothing was installed.";
      if (err instanceof DocspaceApiHttpError || err instanceof AiServiceHttpError) {
        return `The workspace could not be reached: it answered ${err.status}. ${nothing}`;
      }
      return `The call could not be completed due to an internal error. ${nothing}`;
    }
  },
};

/** The bytes of a workspace zip archive under the caller's rights, or the reason it is not one, for the model. */
async function readArchive(fileId: number, what: string): Promise<Uint8Array | string> {
  const id = String(fileId);
  const notAnArchive = `File ${id} is not a zip archive: ${what}.`;
  try {
    const info = await getFileInfo(id);
    if (!info) {
      return `File ${id} does not exist or cannot be read by the current user.`;
    }
    // By its name first: a file that is not one is not downloaded to find that out.
    if (info.fileExst?.toLowerCase() !== ".zip") {
      return notAnArchive;
    }
    const bytes = await readFileBytes(id, MAX_FILE_BYTES);
    const signature = Buffer.from(bytes.subarray(0, 4)).toString("latin1");
    return ZIP_SIGNATURES.includes(signature) ? bytes : notAnArchive;
  } catch (err) {
    if (err instanceof FileTooLargeError) {
      return `File ${id} is over ${MAX_FILE_BYTES} bytes.`;
    }
    if (err instanceof DocspaceApiHttpError && (err.status === 403 || err.status === 404)) {
      return `File ${id} does not exist or cannot be read by the current user.`;
    }
    throw err;
  }
}

/**
 * The model a task runs on: the round's own, else the portal's chat model. Read the way an
 * in-process call needs it, with the address and the caller's session of the portal's own
 * provider put in (`readById`, after the flag that asks for the session); the raw read the
 * document agent takes for an agent on another host carries neither.
 */
async function resolveTaskProfile(): Promise<Profile | undefined> {
  markForwardHeadersToProvider();
  const roundProfileId = getRoundProfileId();
  const ofRound = roundProfileId ? await storage.profiles.readById(roundProfileId) : undefined;
  if (ofRound) {
    return ofRound;
  }
  const profileId =
    (await storage.assignments.readByType(ActionType.Chat)) ??
    (await storage.assignments.readByType(ActionType.Default));
  return profileId ? storage.profiles.readById(profileId) : undefined;
}

async function buildPlugin(
  config: PluginAgentConfig,
  skill: PluginSkill,
  args: Record<string, unknown>,
  entityId: string | undefined,
  options: ToolCallOptions | undefined,
): Promise<string> {
  // Measured as it is used: without the white space around it.
  const task = typeof args["task"] === "string" ? args["task"].trim() : "";
  if (task === "" || task.length > MAX_TASK_CHARS) {
    return `${PLUGIN_AGENT_TOOL_NAME} requires 'task', a text of 1 to ${MAX_TASK_CHARS} characters.`;
  }
  const sourcesFileId = positiveInteger(args["sourcesFileId"]);
  if (args["sourcesFileId"] !== undefined && sourcesFileId === undefined) {
    return "'sourcesFileId' is the id of a file: a whole number above zero.";
  }

  // Every check that can refuse comes before the sandbox exists (ts-ai-failure-paths.md §6),
  // the cheap ones before the download of the sources.
  const profile = await resolveTaskProfile();
  if (!profile) {
    return "No model is configured for this chat, the plugin agent cannot run.";
  }
  const folderId = await resolveUploadTarget(entityId);
  if (!folderId) {
    return "There is no folder the resulting files could be saved into.";
  }
  const running = startTask(config.maxConcurrentTasks);
  if (!running) {
    return "The plugin agent is busy with other tasks. Try again in a few minutes.";
  }

  const round = getRoundSignal();
  const timeout = AbortSignal.timeout(config.taskTimeoutSeconds * 1000);
  const starting = AbortSignal.any([timeout, ...(round ? [round] : [])]);
  let lastFailedCheck: string | undefined;
  try {
    const sources =
      sourcesFileId === undefined
        ? undefined
        : await readArchive(sourcesFileId, "pass the '-src.zip' file an earlier task saved");
    if (typeof sources === "string") {
      return sources;
    }
    const session = await openSandboxSession(config, running, skill, starting);
    try {
      const signal = AbortSignal.any([starting, session.lost]);
      const context: ToolContext = { config, session, skill, signal };
      if (sources) {
        const unpacked = await unpackSources(context, sources);
        if (unpacked !== undefined) {
          return unpacked;
        }
      }

      let step = 0;
      const end = await runLoop<string>({
        profile,
        systemPrompt: systemPromptOf(skill),
        task: sources ? `${task}\n\n${FOLLOW_UP_NOTE}` : task,
        tools: AGENT_TOOLS,
        maxToolCalls: config.maxToolCalls,
        compaction: getCompaction(),
        signal,
        callTool: async (name, toolArgs): Promise<ToolOutcome<string>> => {
          step += 1;
          options?.onprogress?.({ progress: step, message: stepOf(name, toolArgs) });
          if (name !== DELIVER_TOOL) {
            return { text: await callAgentTool(context, name, toolArgs) };
          }
          const projectDir = projectDirOf(toolArgs["project_dir"]);
          if (!projectDir) {
            return {
              text: `${DELIVER_TOOL} requires 'project_dir', the plugin project's directory under ${SANDBOX_WORKSPACE}: plain names without spaces.`,
            };
          }
          const outcome = await deliverPlugin(context, projectDir);
          if (!outcome.passed) {
            lastFailedCheck = outcome.check;
            return { text: outcome.report };
          }
          return { text: "", done: await saveResults(outcome.plugin, folderId) };
        },
      });

      if (end.kind === "done") {
        logger.info(
          `${PLUGIN_AGENT_TOOL_NAME}: task ${running.id} ended after ${end.toolCalls} tool calls`,
        );
        return end.value;
      }
      const failedCheck = lastFailedCheck ? ` The last failed check: ${lastFailedCheck}.` : "";
      return end.kind === "budget"
        ? `The plugin was not finished: the budget of ${config.maxToolCalls} tool calls was spent.${failedCheck} No file was saved.`
        : `The plugin was not finished: the agent stopped without handing it over.${failedCheck} No file was saved. Its last words: ${end.text.slice(0, MAX_LAST_WORDS_CHARS)}`;
    } finally {
      await session.dispose();
    }
  } catch (err) {
    if (timeout.aborted) {
      return `The plugin task was stopped at its time limit of ${config.taskTimeoutSeconds} seconds. No file was saved.`;
    }
    if (round?.aborted) {
      return "The plugin task was stopped with the reply. No file was saved.";
    }
    if (err instanceof ModelTurnError) {
      logger.warn(`${PLUGIN_AGENT_TOOL_NAME}: task ${running.id}: ${err.message}`);
      return "The plugin was not finished: the model's provider failed a request. No file was saved.";
    }
    if (err instanceof SandboxError) {
      logger.warn(`${PLUGIN_AGENT_TOOL_NAME}: ${describe(err)}`);
      return err.kind === "busy"
        ? "The plugin agent is busy: no sandbox is free. Try again in a few minutes."
        : "The plugin agent is unavailable: its sandbox cannot be reached. No file was saved.";
    }
    throw err;
  } finally {
    endTask(running);
  }
}

const FOLLOW_UP_NOTE =
  `The plugin's project from an earlier task is unpacked in ${FOLLOW_UP_DIR}, without its ` +
  "node_modules and without a build: run npm install there first, change that project, and hand it over.";

/** Puts the sources of an earlier task into the sandbox; answers the reason when they do not unpack. */
async function unpackSources(
  context: ToolContext,
  sources: Uint8Array,
): Promise<string | undefined> {
  await writeSandboxFile(
    context.config,
    context.session.id,
    FOLLOW_UP_ARCHIVE,
    sources,
    context.signal,
  );
  const result = await execInSandbox(
    context.config,
    context.session.id,
    `mkdir -p ${FOLLOW_UP_DIR} && unzip -q -o ${FOLLOW_UP_ARCHIVE} -d ${FOLLOW_UP_DIR} && rm -f ${FOLLOW_UP_ARCHIVE}`,
    {
      cwd: SANDBOX_WORKSPACE,
      env: context.session.env,
      timeoutSeconds: UNPACK_TIMEOUT_SECONDS,
      maxOutputChars: MAX_LAST_WORDS_CHARS,
      signal: context.signal,
    },
  );
  return result.code === 0
    ? undefined
    : "The sources archive could not be unpacked: it is damaged or not a plugin's sources. No task was run.";
}

/** The skill's own text, then what differs here from the machine the skill was written for. */
function systemPromptOf(skill: PluginSkill): string {
  return [
    skill.instructions,
    "",
    "# Where you work",
    "",
    "You are the build job of the skill above, running without a person: nobody answers a question.",
    `- You work in a sandbox. The project goes under ${SANDBOX_WORKSPACE}, one directory for the plugin.`,
    `- $SKILL/scripts and $SKILL/templates are in ${SANDBOX_SKILL_DIR}. The skill's texts (SKILL.md, references/) are not in the sandbox: read them with the read_skill tool, by the paths the skill links.`,
    "- The only network access is npm install, from a registry that is already configured. Links to documentation sites cannot be opened: work from the references and from the installed types.",
    "- There is no browser: skip the preview step of the build workflow.",
    `- Finish by calling ${DELIVER_TOOL} with the project's directory. It builds, type-checks and validates the project itself; when a check fails, fix the project and call it again.`,
    "- Do not ask for confirmation and do not wait: make reasonable assumptions and say them in your last message.",
  ].join("\n");
}

/**
 * Saves the package, then the sources, and answers what the task came to. Each save answers for
 * itself (ts-ai-failure-paths.md §6): a refusal says the file was not saved, any other failure
 * that it may have been, since the workspace may have taken the file before its answer was
 * lost. The package alone is a usable result, so a failure of the second save is said next to
 * the id of the first.
 */
async function saveResults(plugin: DeliveredPlugin, folderId: string): Promise<string> {
  const threadId = getRoundThreadId() ?? readChatContext("thread")?.requested.threadId;
  const packageTitle = `${plugin.name}.zip`;
  let saved: { id: string; title: string };
  try {
    saved = await saveResult(
      threadId,
      folderId,
      packageTitle,
      plugin.packageBytes,
      "application/zip",
    );
  } catch (err) {
    logger.warn(`${PLUGIN_AGENT_TOOL_NAME}: ${packageTitle} was not saved: ${String(err)}`);
    const refused = refusedStatus(err);
    return refused !== undefined
      ? `The plugin was built, but the workspace refused to save it: it answered ${refused}. No file was saved.`
      : `The plugin was built, but the workspace's answer to saving ${packageTitle} could not be read: the file may or may not have been saved. Look for it before building again.`;
  }
  let sources: { id: string; title: string } | undefined;
  let warning: string | undefined;
  try {
    const file = await saveResult(
      threadId,
      folderId,
      `${plugin.name}-src.zip`,
      plugin.sourcesBytes,
      "application/zip",
    );
    sources = { id: file.id, title: file.title };
  } catch (err) {
    logger.warn(
      `${PLUGIN_AGENT_TOOL_NAME}: the sources of ${plugin.name} were not saved: ${String(err)}`,
    );
    warning =
      refusedStatus(err) !== undefined
        ? "The package was saved, the archive of its sources was not: a follow-up task cannot continue from this one."
        : "The package was saved; whether the archive of its sources was saved is not known. A follow-up task needs that archive's file id.";
  }
  return JSON.stringify({
    name: plugin.name,
    version: plugin.version,
    package: { fileId: saved.id, title: saved.title, bytes: plugin.packageBytes.length },
    sources: sources ? { fileId: sources.id, title: sources.title } : null,
    dependencies: plugin.dependencies,
    devDependencies: plugin.devDependencies,
    installed: false,
    ...(warning ? { warning } : {}),
  });
}

const MAY_BE_INSTALLED =
  "Whether the plugin was installed is not known. Check the portal's plugin settings before trying again.";

/**
 * Installs a package. The engine pauses an `install_plugin` call for the user's approval under
 * the "ask" mode always and under "auto" unless the tool is on the always-allow list; under
 * "allow" it pauses nothing. Code that then runs in every user's browser is not installed on a
 * standing permission, so in the two cases the engine did not ask about this call it installs
 * nothing. A standing permission is taken back at once, so that the next call asks: the engine
 * writes it before it runs the very call it was given with, and the user who ticked "always"
 * on the dialog is asked once more instead of being sent to the settings.
 */
async function installPlugin(
  args: Record<string, unknown>,
  entityId: string | undefined,
): Promise<string> {
  const fileId = positiveInteger(args["fileId"]);
  if (fileId === undefined) {
    return `${INSTALL_PLUGIN_TOOL_NAME} requires 'fileId', the id of the plugin package: a whole number above zero.`;
  }
  // Nothing stored is the engine's default, "auto".
  const mode = (await storage.preferences.readToolPermissionMode()) ?? "auto";
  if (mode === "allow") {
    return 'Nothing was installed: installing a plugin needs the user\'s approval of the call. Ask the user to switch the tool permissions of the chat from "Allow" to "Auto" or "Ask" and to ask again.';
  }
  if (mode === "auto") {
    // The list holds `<group>_<tool>` tokens (the library's `allowAlwaysToken`).
    const token = `${PLUGIN_INSTALL_TYPE}_${INSTALL_PLUGIN_TOOL_NAME}`;
    // Read anew, not from what the round was started with: the write below puts the whole
    // list back, and a task may have run for many minutes since, in which the user changed it.
    invalidateChatContext("toolPrefs");
    const list = await storage.toolPrefs.readAllowAlways(entityId);
    if (list.includes(token)) {
      await storage.toolPrefs.updateAllowAlways(
        list.filter((entry) => entry !== token),
        entityId,
      );
      return `Nothing was installed: an installation is approved one call at a time, so the standing approval of ${INSTALL_PLUGIN_TOOL_NAME} was removed. Call ${INSTALL_PLUGIN_TOOL_NAME} again: the user will be asked.`;
    }
  }

  const bytes = await readArchive(fileId, "pass the plugin package a plugin task saved");
  if (typeof bytes === "string") {
    return `Nothing was installed. ${bytes}`;
  }
  let answer;
  try {
    answer = await addWebPlugin(`plugin-${fileId}.zip`, bytes);
  } catch (err) {
    // A 5xx, a timeout, an answer that cannot be read: the portal may have installed it.
    logger.warn(`${INSTALL_PLUGIN_TOOL_NAME}: file ${fileId}: ${String(err)}`);
    return `The portal's answer to the installation could not be read. ${MAY_BE_INSTALLED}`;
  }
  if (answer.installed) {
    logger.info(
      `${INSTALL_PLUGIN_TOOL_NAME}: file ${fileId} installed as ${answer.name} ${answer.version}`,
    );
    return JSON.stringify({
      installed: true,
      name: answer.name,
      version: answer.version,
      enabled: answer.enabled,
    });
  }
  const said = `the portal answered ${answer.status}${answer.message ? ` "${answer.message}"` : ""}`;
  // The portal answers 403 for a caller or an installation that may not upload, before anything
  // is installed, and also after the installation, when the plugin's domains do not fit into
  // the Content Security Policy header (WebPluginsController.AddWebPluginFromFile).
  return answer.status === 403
    ? `The installation was refused: ${said}. ${MAY_BE_INSTALLED}`
    : `Nothing was installed: ${said}.`;
}
