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
// request to the external agent, which creates office documents with
// ONLYOFFICE Document Builder in a sandbox of its own, and brings the result
// back as new files in the user's workspace. The workspace files and folders
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
  canTakeUpload,
  getAgentResultStorageId,
  getFolderInfo,
  getMyDocumentsFolderId,
  resolveAgentEntityId,
} from "../storage/docspaceFilesApi.js";
import { mediaTypeOf, uploadFile } from "../storage/docspaceFileTransfer.js";
import { DocspaceApiHttpError } from "../storage/docspaceFilesApi.js";
import {
  getForwardedHeaders,
  getRoundAttachmentIds,
  getRoundProfileId,
  getSourceMeta,
} from "../requestContext.js";
import { resolveCustomServers } from "./customTools.js";
import {
  agentServerWhitelist,
  getSystemServerConfig,
  sessionCookie,
  systemServerHeaders,
} from "./systemTools.js";
import { getMcpServers, PORTAL_MCP_SERVER_NAME } from "../../config/index.js";
import {
  DOCUMENT_AGENT_FILES_PATH,
  DOCUMENT_AGENT_FILES_SERVER,
  DOCUMENT_AGENT_TOKEN_HEADER,
  GET_FILE_LINK_TOOL,
  MAX_FILE_BYTES,
} from "./documentAgentFiles.js";
import { fileEditorUrl } from "./httpToolsAdapter.js";
import { agentAssignedProfileId } from "../controllers/agentProfile.js";
import { runDocumentTask, DocumentAgentError } from "./documentAgentClient.js";
import type { DocumentAgentSettings } from "./documentAgentClient.js";
import { getArray, getString, isObject } from "../narrow.js";
import { aiServiceBaseUrl } from "../storage/httpClient.js";
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
    "Delegate the creation of office documents (docx, xlsx, pptx, pdf) to the document agent, " +
    "which works autonomously with ONLYOFFICE Document Builder and saves the resulting files " +
    "into the workspace as new files. Use it when the user asks for a real file with content: " +
    "a report, a letter, a spreadsheet with data and charts, a presentation, a filled template. " +
    "The agent reads workspace content itself by id: the files attached to the current message " +
    "are passed on automatically; name other files in `fileIds` and folders in `folderIds`. " +
    "Existing files are never changed: an edit comes back as a new file. " +
    "The call returns the agent's summary and the saved files with " +
    "`url`; when you mention a file, link it exactly as `[<title>](<url>)`, copying `url` " +
    "verbatim. The task may take several minutes.",
  inputSchema: {
    type: "object",
    properties: {
      task: {
        type: "string",
        description:
          "The complete request for the agent, in the user's language: what to produce or " +
          "change, the content or data to use, the format, the file name. Include everything " +
          "the agent needs; it cannot ask questions.",
      },
      fileIds: {
        type: "array",
        items: { type: "string" },
        description:
          "Ids of workspace files the agent should read, in addition to the message's " +
          "attachments.",
      },
      folderIds: {
        type: "array",
        items: { type: "string" },
        description: "Ids of workspace folders or rooms whose files the agent should read.",
      },
    },
    required: ["task"],
  },
  serverType: DOCUMENT_AGENT_TYPE,
  requireApproval: true,
  enabled: true,
};

export const documentAgentTools: ToolsAdapter = {
  async getTools(): Promise<Record<string, TMCPItem[]>> {
    return getDocumentAgent() ? { [DOCUMENT_AGENT_TYPE]: [TOOL] } : {};
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
  const profile = await resolveProfile(entityId);
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

  const settings: DocumentAgentSettings = {
    model: modelSettings(profile, config),
    mcpServers,
    limits: {
      taskTimeoutSeconds: config.taskTimeoutSeconds,
      maxFileBytes: MAX_FILE_BYTES,
      ...(config.maxToolCalls === undefined ? {} : { maxToolCalls: config.maxToolCalls }),
    },
  };
  logger.info(
    `${DOCUMENT_AGENT_TOOL_NAME}: start entityId=${entityId ?? "-"} profile=${profile.id} ` +
      `model=${profile.modelId} files=[${inputFileIds.join(",")}] ` +
      `folders=[${folderIds.join(",")}] servers=[${Object.keys(mcpServers).join(",")}] ` +
      `target=${targetFolderId}`,
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

  const saved: { id: string; title: string; url: string }[] = [];
  const notSaved: string[] = [];
  for (const artifact of result.artifacts) {
    try {
      const file = await uploadFile(
        targetFolderId,
        artifact.name,
        artifact.bytes,
        artifact.mediaType === "application/octet-stream"
          ? mediaTypeOf(artifact.name)
          : artifact.mediaType,
      );
      saved.push({ id: file.id, title: file.title, url: fileEditorUrl(file.id) });
    } catch (err) {
      logger.warn(
        `${DOCUMENT_AGENT_TOOL_NAME}: ${artifact.name} not saved into ${targetFolderId}: ${
          err instanceof Error ? err.message : String(err)
        }`,
      );
      notSaved.push(artifact.name);
    }
  }

  if (result.state !== "completed") {
    return {
      error: `The document agent did not finish the task (${result.state}): ${result.text || "no details"}`,
      ...(saved.length > 0 ? { data: { files: saved } } : {}),
    };
  }
  return {
    data: {
      summary: result.text,
      files: saved,
      ...(notSaved.length > 0 ? { notSaved } : {}),
    },
  };
}

// The chat's own model: the round's profile when the handler recorded one,
// else the agent room's assignment, else the portal's chat default. Read raw,
// as the C# storage serves it: the in-process override would point an
// `onlyoffice` profile at this service's own address, which the agent, on
// another host, cannot reach (see modelSettings).
async function resolveProfile(entityId: string | undefined): Promise<Profile | undefined> {
  const roundProfileId = getRoundProfileId();
  if (roundProfileId) {
    const profile = await storage.profiles.readByIdRaw(roundProfileId);
    if (profile) {
      return profile;
    }
  }
  const profileId =
    (await agentAssignedProfileId(entityId)) ??
    (await storage.assignments.readByType(ActionType.Chat ?? ActionType.Default));
  return profileId ? storage.profiles.readByIdRaw(profileId) : undefined;
}

// The portal's own provider is reached through the portal API, where the
// gateway proxy lives at `/api/2.0/ai/gateway`, authenticated as the user.
// The C# side stores the portal's public root as the profile's `baseUrl`,
// which from inside the deployment may be a dev host with a self-signed
// certificate or a name that does not resolve; so, unless the host names
// the portal address for the agent, it gets the AI service's own address,
// the one this service uses for the same calls.
const ONLYOFFICE_PROVIDER = "onlyoffice";
const ONLYOFFICE_GATEWAY_PATH = "/api/2.0/ai/gateway";

// The profile as the agent's settings want it. A third-party provider goes
// as stored, key included. For the portal's own provider the key is a
// placeholder and the session is what authenticates: the caller's auth
// cookie goes along, that one cookie only (the portal API reads the session
// from it, the key stays a harmless bearer), or, for a caller that sent a
// header instead, that header in place of the key. The round's source goes
// along too, so the agent's requests are billed to the same entry as the
// chat's own.
function modelSettings(
  profile: Profile,
  config: DocumentAgentConfig,
): DocumentAgentSettings["model"] {
  const model: DocumentAgentSettings["model"] = {
    provider: profile.providerType,
    baseUrl: profile.baseUrl,
    modelId: profile.modelId,
    reasoning: reasoningOf(profile),
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
    model.baseUrl = `${config.portalUrl ?? aiServiceBaseUrl}${ONLYOFFICE_GATEWAY_PATH}`;
    const source = getSourceMeta();
    if (source) {
      model.source = source;
    }
    const cookie = sessionCookie();
    const authorization = getForwardedHeaders()["authorization"];
    if (cookie) {
      model.headers = { ...model.headers, cookie };
    } else if (authorization) {
      delete model.apiKey;
      model.headers = { ...model.headers, authorization };
    }
  }
  return model;
}

function reasoningOf(profile: Profile): DocumentAgentSettings["model"]["reasoning"] {
  const support = profile.reasoningSupport;
  const thinks = support
    ? support.thinks && (!support.canDisable || profile.reasoning === true)
    : profile.reasoning === true;
  if (!thinks) {
    return "off";
  }
  return support?.defaultDepth ?? "medium";
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
    "Workspace inputs, by id. To open or edit a file as it is, fetch it with " +
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
// host-configured system servers when the host shares them, under the same
// whitelist the chat applies to an agent room; and the custom servers of the
// round's scope, as the chat itself sees them. The system servers carry the
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

  const whitelist = config.shareSystemServers ? await agentServerWhitelist(entityId) : undefined;
  const headers = systemServerHeaders();
  for (const server of getMcpServers()) {
    const portal = server.name === PORTAL_MCP_SERVER_NAME;
    if (!portal && !config.shareSystemServers) {
      continue;
    }
    if (whitelist && !whitelist.has(server.name)) {
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
  const session = sessionCookie();
  const authorization = getForwardedHeaders()["authorization"];
  add(DOCUMENT_AGENT_FILES_SERVER, `${config.filesUrl}${DOCUMENT_AGENT_FILES_PATH}`, {
    [DOCUMENT_AGENT_TOKEN_HEADER]: config.token,
    ...(session ? { cookie: session } : authorization ? { authorization } : {}),
  });

  const custom = await resolveCustomServers(entityId);
  for (const [name, server] of Object.entries(custom)) {
    const headers = isObject(server.headers) ? stringHeaders(server.headers) : undefined;
    add(name, server.url, headers);
  }
  return out;
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

// Where the files go: an agent room's Result Storage, else the current
// folder when the user may create there, else My documents.
async function resolveUploadTarget(entityId: string | undefined): Promise<string | undefined> {
  if (entityId) {
    const agentId = await resolveAgentEntityId(entityId);
    if (agentId) {
      const resultStorageId = await getAgentResultStorageId(agentId);
      if (resultStorageId) {
        return resultStorageId;
      }
    } else if (canTakeUpload(await getFolderInfo(entityId))) {
      return entityId;
    }
  }
  return getMyDocumentsFolderId();
}
