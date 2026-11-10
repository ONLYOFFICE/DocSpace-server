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

// The document agent as one tool of the chat: `document_agent` hands a
// request to the external agent, which creates and works on office documents
// with ONLYOFFICE Document Builder in a sandbox of its own, and brings the
// result back as new files in the user's workspace. The workspace files and folders
// the request is about (the message's attachments, the ids the model names)
// go as ids in the task's text; the agent reads their content itself through
// the portal's MCP server, and the originals stay as they are. The agent runs
// on the chat's own model profile and sees the custom MCP servers of the
// current scope, so the delegated task has the same tools and the same model
// the chat does.
//
// Like `formAnalysisSubAgent`, this is a ToolsAdapter with a single tool
// whose work happens elsewhere; unlike it, the work lives in another
// process, reached over A2A (see documentAgentClient).

import { ActionType } from "@onlyoffice/ai-chat/core";
import type {
  Profile,
  ProgressCallback,
  TMCPItem,
  ToolCallOptions,
  ToolsAdapter,
} from "@onlyoffice/ai-chat/core";
import { DOCUMENT_AGENT_TYPE, getDocumentAgent } from "../../config/index.js";
import type { DocumentAgentConfig } from "../../config/index.js";
import { storage } from "../storage/index.js";
import {
  type DocumentAgentSettings as TenantSettings,
  readDocumentAgentSettingsForTask,
  type ReasoningLevel,
} from "../storage/documentAgentSettingsStorage.js";
import { reasoningFor } from "./documentAgentReasoning.js";
import {
  canTakeUpload,
  getFolderInfo,
  getMyDocumentsFolderId,
  getFileInfo,
} from "../storage/docspaceFilesApi.js";
import { DocspaceApiHttpError } from "../storage/docspaceFilesApi.js";
import {
  getForwardedHeaders,
  getRoundAttachmentIds,
  getRoundProfileId,
  getSourceMeta,
} from "../requestContext.js";
import { resolveCustomServers } from "./customTools.js";
import { getSystemServerConfig, sessionCookie, systemServerHeaders } from "./systemTools.js";
import { getMcpServers, PORTAL_MCP_SERVER_NAME } from "../../config/index.js";
import {
  DOCUMENT_AGENT_FILES_PATH,
  DOCUMENT_AGENT_FILES_SERVER,
  DOCUMENT_AGENT_OUTPUTS_FOLDER_PARAM,
  DOCUMENT_AGENT_OUTPUTS_PATH,
  DOCUMENT_AGENT_TOKEN_HEADER,
  GET_FILE_LINK_TOOL,
  MAX_FILE_BYTES,
} from "./documentAgentFiles.js";
import { fileEditorUrl } from "./httpToolsAdapter.js";
import { runDocumentTask, DocumentAgentError } from "./documentAgentClient.js";
import type { DocumentAgentSettings } from "./documentAgentClient.js";
import { getArray, getString, isObject } from "../narrow.js";
import { aiServiceBaseUrl } from "../storage/httpClient.js";
import { onlyofficeGatewayPath } from "../storage/profilesStorage.js";
import type { DocspaceProfile } from "../storage/profilesStorage.js";
import logger from "../log.js";

export const DOCUMENT_AGENT_TOOL_NAME = "document_agent";

// Past the task timeout the agent reports a failure itself; the margin
// covers the sandbox teardown and the files on their way back.
const ABORT_MARGIN_MS = 60_000;

// Server names the agent accepts, and the two it keeps for itself.
const SERVER_NAME = /^[a-z0-9_-]+$/;
const RESERVED_SERVER_NAMES = new Set(["sandbox", "docbuilder"]);

const TOOL: TMCPItem = {
  name: DOCUMENT_AGENT_TOOL_NAME,
  description:
    "Hand a document task to the document agent, which works autonomously with ONLYOFFICE " +
    "Document Builder in a sandbox of its own and saves the resulting files into the workspace " +
    "as new files. Use it whenever the user wants a real file made or an existing file worked on: " +
    "a report, a letter, a spreadsheet with data and charts, a presentation, a filled template, " +
    "a translation, a redaction, a merge, a conversion, a fix of a file. " +
    "The agent opens the files itself: never read a file for it and never put a file's content " +
    "into `task`. The files attached to the current message go along automatically; every other " +
    "file the request is about (named, selected or open) goes by id in `fileIds`, a whole folder " +
    "in `folderIds`. Existing files are never changed: a result always comes back as a new file. " +
    "The call returns the agent's summary and the saved files with " +
    "`url`; when you mention a file, link it exactly as `[<title>](<url>)`, copying `url` " +
    "verbatim. The task may take several minutes.",
  inputSchema: {
    type: "object",
    properties: {
      task: {
        type: "string",
        description:
          "The user's request for the agent, in their own words and language, as close to " +
          "verbatim as possible. Add only what the user said elsewhere in the conversation and the " +
          "agent needs (the format, the file name, the language). Do not rephrase it into steps, " +
          "do not add your own method, and do not paste any file content: the agent cannot ask " +
          "questions, but it has its own procedures and reads the files itself.",
      },
      fileIds: {
        type: "array",
        items: { type: "string" },
        description:
          "Ids of every workspace file the request is about, besides the message's attachments: " +
          "the file the user named, selected or has open. A request about a file always carries " +
          "the file's id here, never its text in `task`.",
      },
      folderIds: {
        type: "array",
        items: { type: "string" },
        description:
          "Ids of workspace folders or rooms whose files the agent should read, only when the " +
          "request is about a whole folder.",
      },
    },
    required: ["task"],
  },
  serverType: DOCUMENT_AGENT_TYPE,
  requireApproval: true,
  enabled: true,
};

export const documentAgentTools: ToolsAdapter = {
  // Offered when the server has an agent and the portal has not switched it off.
  async getTools(): Promise<Record<string, TMCPItem[]>> {
    if (!getDocumentAgent()) {
      return {};
    }
    return (await tenantSettings()).enabled ? { [DOCUMENT_AGENT_TYPE]: [TOOL] } : {};
  },

  async callTool(
    toolName: string,
    args: Record<string, unknown>,
    entityId?: string,
    options?: ToolCallOptions,
  ): Promise<unknown> {
    if (toolName !== DOCUMENT_AGENT_TOOL_NAME) {
      return `Unknown tool "${toolName}"`;
    }
    const config = getDocumentAgent();
    if (!config) {
      return "The document agent is not configured on this server.";
    }
    const task = getString(isObject(args) ? args : {}, "task")?.trim();
    if (!task) {
      return `${DOCUMENT_AGENT_TOOL_NAME} requires a non-empty 'task'.`;
    }
    try {
      return JSON.stringify(
        await run(
          config,
          task,
          idsOf(args, "fileIds"),
          idsOf(args, "folderIds"),
          entityId,
          options?.onprogress,
        ),
      );
    } catch (err) {
      // Contract: a failure is a result, never a throw — a throw aborts the chat stream.
      const message = err instanceof Error ? err.message : String(err);
      logger.warn(`${DOCUMENT_AGENT_TOOL_NAME} failed: ${message}`);
      if (err instanceof DocspaceApiHttpError) {
        return `The workspace could not be reached: ${message}.`;
      }
      if (err instanceof DocumentAgentError) {
        return `The document agent is unavailable: ${message}.`;
      }
      if (err instanceof Error && err.name === "TimeoutError") {
        return "The document task timed out before the agent finished.";
      }
      return "The document task could not be completed due to an internal error.";
    }
  },
};

function idsOf(args: Record<string, unknown>, key: string): string[] {
  const raw = getArray(args, key) ?? [];
  const ids: string[] = [];
  for (const value of raw) {
    if (typeof value === "string" && value.trim() !== "") {
      ids.push(value.trim());
    } else if (typeof value === "number" && Number.isFinite(value)) {
      ids.push(String(value));
    }
  }
  return ids;
}

async function run(
  config: DocumentAgentConfig,
  task: string,
  fileIds: string[],
  folderIds: string[],
  entityId: string | undefined,
  onprogress: ProgressCallback | undefined,
): Promise<unknown> {
  const tenant = await tenantSettings();
  if (!tenant.enabled) {
    return { error: "The document agent is switched off on this portal." };
  }
  const profile = await resolveProfile(tenant.profileId);
  if (!profile) {
    return { error: "No model is configured for this chat, the document agent cannot run." };
  }
  const [inputFileIds, mcpServers, targetFolderId] = await Promise.all([
    inputFileIdsOf(fileIds),
    collectServers(config, entityId),
    resolveUploadTarget(entityId),
  ]);
  if (!targetFolderId) {
    return { error: "There is no folder the resulting files could be saved into." };
  }
  const logFolderId = tenant.taskLog ? await resolveLogTarget(tenant, targetFolderId) : undefined;

  // The result files come back through this service (see documentAgentFiles.ts
  // receive): the agent posts each one here, with the shared token and the
  // user's session, and it is inserted into the target folder as the user.
  const settings: DocumentAgentSettings = {
    model: modelSettings(profile, config, reasoningFor(profile, tenant.reasoning)),
    mcpServers,
    outputs: { url: outputsUrl(config, targetFolderId), headers: ownServerHeaders(config) },
    // The task's log goes the same way as its results, into the folder the
    // portal chose for logs (or the results' own), as task-log.md.
    ...(logFolderId === undefined
      ? {}
      : { log: { url: outputsUrl(config, logFolderId), headers: ownServerHeaders(config) } }),
    // The portal's limits, under the server's own: appsettings may still
    // pin a lower tool budget, and the outputs endpoint takes no more than
    // MAX_FILE_BYTES whatever the portal says.
    limits: {
      taskTimeoutSeconds: config.taskTimeoutSeconds,
      maxFileBytes: Math.min(tenant.maxFileBytes, MAX_FILE_BYTES),
      maxToolCalls: Math.min(tenant.maxToolCalls, config.maxToolCalls ?? Infinity),
    },
  };
  logger.info(
    `${DOCUMENT_AGENT_TOOL_NAME}: start entityId=${entityId ?? "-"} profile=${profile.id} ` +
      `model=${profile.modelId} files=[${inputFileIds.join(",")}] ` +
      `folders=[${folderIds.join(",")}] servers=[${Object.keys(mcpServers).join(",")}] ` +
      `target=${targetFolderId} log=${logFolderId ?? "-"}`,
  );

  // The steps shown live are the agent's own, in the language of the request:
  // each A2A status update of the task becomes an MCP-shaped progress
  // notification, its text the message and its ordinal the progress, which
  // MCP requires to increase. The agent gives no total.
  let step = 0;
  const result = await runDocumentTask(config, {
    task: withInputIds(task, inputFileIds, folderIds),
    settings,
    signal: AbortSignal.timeout(config.taskTimeoutSeconds * 1000 + ABORT_MARGIN_MS),
    onStatus: onprogress
      ? (text) => {
          step += 1;
          onprogress({ progress: step, message: text });
        }
      : undefined,
  });

  // Every artifact is a file the outputs endpoint took: its url is the editor
  // link the endpoint answered, resolved by the agent against the endpoint's
  // address, so only the file id in it is ours. The title is read back: a
  // same-named file was saved under a numbered one.
  const saved: { id: string; title: string; url: string }[] = [];
  let log: { id: string; title: string; url: string } | undefined;
  for (const artifact of result.artifacts) {
    const id = savedFileId(artifact.url);
    if (id === undefined) {
      logger.warn(`${DOCUMENT_AGENT_TOOL_NAME}: artifact ${artifact.name} names no file of ours`);
      continue;
    }
    const info = await getFileInfo(id).catch(() => undefined);
    const file = { id, title: info?.title ?? artifact.name, url: fileEditorUrl(id) };
    // The task's log is not a result: it goes back on its own, for the chat
    // to offer next to the files.
    if (artifact.kind === "log") {
      log = file;
    } else {
      saved.push(file);
    }
  }
  const extras = {
    ...(saved.length > 0 ? { files: saved } : {}),
    ...(log === undefined ? {} : { log }),
  };

  if (result.state !== "completed") {
    return {
      error: `The document agent did not finish the task (${result.state}): ${result.text || "no details"}`,
      ...(Object.keys(extras).length > 0 ? { data: extras } : {}),
    };
  }
  return {
    data: {
      summary: result.text,
      files: saved,
      ...(log === undefined ? {} : { log }),
    },
  };
}

/** The file id of an editor link as `fileEditorUrl` builds it, whatever host the agent put in front. */
function savedFileId(url: string): string | undefined {
  const parsed = URL.parse(url);
  if (parsed === null || !parsed.pathname.endsWith("/doceditor")) {
    return undefined;
  }
  const id = parsed.searchParams.get("fileId");
  return id ? id : undefined;
}

/** The outputs endpoint of this service with the folder the files go into. */
function outputsUrl(config: DocumentAgentConfig, folderId: string): string {
  const url = new URL(`${config.filesUrl}${DOCUMENT_AGENT_OUTPUTS_PATH}`);
  url.searchParams.set(DOCUMENT_AGENT_OUTPUTS_FOLDER_PARAM, folderId);
  return url.href;
}

// The portal's settings for the agent; while they cannot be read the
// agent is off, so a broken file never turns it back on.
function tenantSettings(): Promise<TenantSettings> {
  return readDocumentAgentSettingsForTask();
}

// Where the task's log goes: the folder the portal chose when the user may
// create there, else next to the results. A chosen folder the user cannot
// write to would only make the agent's delivery fail.
async function resolveLogTarget(tenant: TenantSettings, resultsFolderId: string): Promise<string> {
  const chosen = tenant.taskLogFolderId;
  if (chosen === null || chosen === resultsFolderId) {
    return resultsFolderId;
  }
  const info = await getFolderInfo(chosen).catch(() => undefined);
  if (canTakeUpload(info)) {
    return chosen;
  }
  logger.warn(
    `${DOCUMENT_AGENT_TOOL_NAME}: log folder ${chosen} is not writable for the user, logging next to the results`,
  );
  return resultsFolderId;
}

// The model the task runs on: the portal's pick for the agent when it has
// one that still exists, else the chat's own (the round's profile when the
// handler recorded one, else the portal's chat default). Read raw,
// as the C# storage serves it: the in-process override would point an
// `onlyoffice` profile at this service's own address, which the agent, on
// another host, cannot reach (see modelSettings).
async function resolveProfile(pickedProfileId: string | null): Promise<DocspaceProfile | undefined> {
  if (pickedProfileId) {
    const picked = await storage.profiles.readByIdRaw(pickedProfileId);
    if (picked) {
      return picked;
    }
    logger.warn(
      `${DOCUMENT_AGENT_TOOL_NAME}: the portal's agent profile ${pickedProfileId} is gone, using the chat's`,
    );
  }
  const roundProfileId = getRoundProfileId();
  if (roundProfileId) {
    const profile = await storage.profiles.readByIdRaw(roundProfileId);
    if (profile) {
      return profile;
    }
  }
  const profileId = await storage.assignments.readByType(ActionType.Chat ?? ActionType.Default);
  return profileId ? storage.profiles.readByIdRaw(profileId) : undefined;
}

// The portal's own provider is reached through the portal API, where the
// gateway proxy lives at `/api/2.0/ai/gateway` (its BYOK route for a profile
// on the portal's own provider key), authenticated as the user.
// The C# side stores the portal's public root as the profile's `baseUrl`,
// which from inside the deployment may be a dev host with a self-signed
// certificate or a name that does not resolve; so, unless the host names
// the portal address for the agent, it gets the AI service's own address,
// the one this service uses for the same calls.
const ONLYOFFICE_PROVIDER = "onlyoffice";

// The profile as the agent's settings want it. A third-party provider goes
// as stored, key included. For the portal's own provider the key is a
// placeholder and the session is what authenticates: the caller's auth
// cookie goes along, that one cookie only (the portal API reads the session
// from it, the key stays a harmless bearer), or, for a caller that sent a
// header instead, that header in place of the key. With it go the two
// forwarded headers that name the portal the caller came through (see
// tenantHeaders): the agent reaches the gateway by the AI service's own
// address, whose host names no tenant, so without them the session has no
// tenant to be checked against and the gateway answers 401. Nothing else of
// the caller's headers goes: the agent is an external service, and what it
// gets is the session and the tenant, no more. The round's source goes along
// too, so the agent's requests are billed to the same entry as the chat's
// own.
function modelSettings(
  profile: DocspaceProfile,
  config: DocumentAgentConfig,
  reasoning: ReasoningLevel,
): DocumentAgentSettings["model"] {
  const model: DocumentAgentSettings["model"] = {
    provider: profile.providerType,
    baseUrl: profile.baseUrl,
    modelId: profile.modelId,
    reasoning,
  };
  if (profile.key) {
    model.apiKey = profile.key;
  }
  if (profile.headers && Object.keys(profile.headers).length > 0) {
    model.headers = { ...profile.headers };
  }
  if (profile.useResponsesApi !== undefined) {
    model.responsesApi = profile.useResponsesApi;
  }
  if (profile.providerType === ONLYOFFICE_PROVIDER) {
    model.baseUrl = `${config.portalUrl ?? aiServiceBaseUrl}${onlyofficeGatewayPath(profile)}`;
    const source = getSourceMeta();
    if (source) {
      model.source = source;
    }
    const cookie = sessionCookie();
    const authorization = getForwardedHeaders()["authorization"];
    if (cookie) {
      model.headers = { ...model.headers, cookie, ...tenantHeaders() };
    } else if (authorization) {
      delete model.apiKey;
      model.headers = { ...model.headers, authorization, ...tenantHeaders() };
    }
  }
  return model;
}

// The headers the portal reads the tenant from when a request comes in by
// an address that is not the portal's own: the AI service resolves the
// tenant by host (alias plus base domain) and trusts the forwarded headers
// from the deployment's network, so the host the browser used and its
// scheme go along as `X-Forwarded-Host` and `X-Forwarded-Proto`, the way the
// router set them on the request this service is serving (the plain `Host`
// is the fallback, though the request context drops it as hop-by-hop). Only
// those two, and only when set: without a value there is nothing to
// forward, and the portal's own address (a local run, where the host is the
// tenant's alias) needs none.
function tenantHeaders(): Record<string, string> {
  const forwarded = getForwardedHeaders();
  const host = forwarded["x-forwarded-host"] || forwarded["host"];
  const proto = forwarded["x-forwarded-proto"];
  return {
    ...(host ? { "x-forwarded-host": host } : {}),
    ...(proto ? { "x-forwarded-proto": proto } : {}),
  };
}

// The ids the model named plus the files attached to the message, once each.
// Nothing is read here: the agent reads them through the portal's MCP server
// under the caller's session, so what the user may not open, it cannot read.
async function inputFileIdsOf(fileIds: string[]): Promise<string[]> {
  const ids = new Set<string>(fileIds);
  const refs = getRoundAttachmentIds();
  if (refs.length > 0) {
    const records = await storage.attachments.readManyByIds(refs);
    for (const record of records) {
      const entryId = record?.path?.split("/", 1)[0];
      if (entryId) {
        ids.add(entryId);
      }
    }
  }
  return [...ids];
}

// The task as the agent gets it: the model's request, then the workspace ids
// it is about, with how to reach them.
function withInputIds(task: string, fileIds: string[], folderIds: string[]): string {
  if (fileIds.length === 0 && folderIds.length === 0) {
    return task;
  }
  const lines = [
    "The request's input files, by workspace id. To open or edit a file as it is, fetch it with " +
      `${DOCUMENT_AGENT_FILES_SERVER}_${GET_FILE_LINK_TOOL}: it lands in your sandbox as a file. ` +
      "docspace_download_file_as_text gives a file's text only; docspace_get_folder_content lists " +
      "a folder. Never change the originals: produce every result as a new file under " +
      "/workspace/out.",
  ];
  if (fileIds.length > 0) {
    lines.push(`- file ids: ${fileIds.join(", ")}`);
  }
  if (folderIds.length > 0) {
    lines.push(`- folder ids: ${folderIds.join(", ")}`);
  }
  return `${task}\n\n${lines.join("\n")}`;
}

// The MCP servers the agent gets: the portal's own server always, as that is
// how the agent reads the workspace ids it is handed; the other
// host-configured system servers when the host shares them; and the portal's
// custom servers, as the chat itself sees them. The system servers carry the
// caller's session as their headers. The agent connects to every server it
// is given before it starts, so a server it cannot reach fails the task.
async function collectServers(
  config: DocumentAgentConfig,
  entityId: string | undefined,
): Promise<NonNullable<DocumentAgentSettings["mcpServers"]>> {
  const out: NonNullable<DocumentAgentSettings["mcpServers"]> = {};
  const add = (name: string, url: string, headers: Record<string, string> | undefined): void => {
    if (!SERVER_NAME.test(name) || RESERVED_SERVER_NAMES.has(name)) {
      logger.warn(
        `${DOCUMENT_AGENT_TOOL_NAME}: MCP server "${name}" skipped, the agent cannot take that name`,
      );
      return;
    }
    // First come first served: the system servers go in before the custom
    // ones, so a custom server cannot stand in for the portal's or ours.
    if (name in out) {
      logger.warn(
        `${DOCUMENT_AGENT_TOOL_NAME}: MCP server "${name}" skipped, a server of that name is already handed over`,
      );
      return;
    }
    out[name] = {
      url,
      ...(headers && Object.keys(headers).length > 0 ? { headers } : {}),
    };
  };

  const headers = systemServerHeaders();
  for (const server of getMcpServers()) {
    const portal = server.name === PORTAL_MCP_SERVER_NAME;
    if (!portal && !config.shareSystemServers) {
      continue;
    }
    const url = getSystemServerConfig(server.name)?.url ?? server.endpoint;
    add(server.name, url, headers);
  }
  if (!(PORTAL_MCP_SERVER_NAME in out)) {
    logger.warn(
      `${DOCUMENT_AGENT_TOOL_NAME}: the portal's MCP server "${PORTAL_MCP_SERVER_NAME}" is not ` +
        "configured in ai.mcp, the agent cannot read workspace files",
    );
  }

  // This service's own server (see documentAgentFiles.ts): the agent presents
  // the shared token and the user's session, the same session the portal
  // server gets, so a link is minted only for a file the user may read.
  add(
    DOCUMENT_AGENT_FILES_SERVER,
    `${config.filesUrl}${DOCUMENT_AGENT_FILES_PATH}`,
    ownServerHeaders(config),
  );

  const custom = await resolveCustomServers(entityId);
  for (const [name, server] of Object.entries(custom)) {
    const headers = isObject(server.headers) ? stringHeaders(server.headers) : undefined;
    add(name, server.url, headers);
  }
  return out;
}

// What the agent presents to this service's own routes: the shared token,
// the user's session and the tenant headers. The routes admit the agent by
// the token, then call the portal's Files API as the user with the headers
// the agent sent (see documentAgentFiles.ts), so the tenant must be named
// there as it is on the model's requests (see tenantHeaders).
function ownServerHeaders(config: DocumentAgentConfig): Record<string, string> {
  const session = sessionCookie();
  const authorization = getForwardedHeaders()["authorization"];
  return {
    [DOCUMENT_AGENT_TOKEN_HEADER]: config.token,
    ...(session ? { cookie: session } : authorization ? { authorization } : {}),
    ...tenantHeaders(),
  };
}

function stringHeaders(headers: Record<string, unknown>): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [key, value] of Object.entries(headers)) {
    if (typeof value === "string") {
      out[key] = value;
    }
  }
  return out;
}

// Where the files go: the current folder when the user may create there,
// else My documents.
async function resolveUploadTarget(entityId: string | undefined): Promise<string | undefined> {
  if (entityId && canTakeUpload(await getFolderInfo(entityId))) {
    return entityId;
  }
  return getMyDocumentsFolderId();
}
