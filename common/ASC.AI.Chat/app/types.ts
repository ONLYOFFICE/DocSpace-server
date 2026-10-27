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

import type { DocspaceFolderInfo, DocspaceFileInfo, SourceMeta } from "./storage/docspaceFilesApi.js";
import type { ChatContextSnapshot } from "./storage/chatContextSnapshot.js";

export type ForwardedHeaders = Record<string, string>;

export interface RequestContext {
  headers: ForwardedHeaders;
  // When set, the forwarded client headers are merged into the resolved
  // provider profile's `headers` (onlyoffice provider only) so they reach the
  // upstream provider with each chat-stream request.
  forwardHeadersToProvider?: boolean;
  // Form entry id resolved from the current message's attachment refs by
  // `httpToolsAdapter.toContext`. Request-scoped on purpose: `callTool`
  // (per the `ToolsAdapter` interface) doesn't receive attachment refs, but
  // every in-stream tool call runs within the same HTTP request as the
  // `getTools` that resolved them.
  resolvedFormId?: number;
  // The attachment id resolvedFormId came from — the analyze-intent key.
  resolvedAttachmentId?: string;
  // Names of the custom MCP servers resolved for this round's scope — see
  // setCustomServerNames in requestContext.ts.
  customServerNames?: string[];
  // The knowledge rooms the composer connected for this round
  // (`actionArgs.knowledgeRooms[].id`); the .NET knowledge search tool is
  // scoped to them — see setKnowledgeRoomIds in requestContext.ts.
  knowledgeRoomIds?: number[];
  // Entry ids of forms attached for analysis (per-file analyzeOnly); read by createMany.
  analyzeEntryIds?: string[];
  // In-flight `GET /files/folder/{id}` reads, keyed by folder id. A round
  // reads the same folder for its instruction and its entity metadata; the
  // DTO cannot change within one request, so the second reader joins the
  // first fetch (see `getFolderInfoOnce`).
  folderInfoCache?: Map<string, Promise<DocspaceFolderInfo | undefined>>;
  // In-flight `GET /files/file/{id}` reads, keyed by file id — the file-side
  // twin of `folderInfoCache` (see `getFileInfoOnce`). Kept apart because
  // files and folders have independent id spaces.
  fileInfoCache?: Map<string, Promise<DocspaceFileInfo | undefined>>;
  // The entry this round is attributed to, resolved by `primeSourceMeta`
  // before the engine runs and handed to the library through the engines'
  // `resolveSource` dep, which carries it into every request `metadata`.
  // Absent when the round names no resolvable entry.
  sourceMeta?: SourceMeta;
  // The chat round's aggregate read (`GET internal/ai/chat-context`), primed
  // by the send handlers before the engine runs. Storage read methods serve
  // from it instead of issuing their own per-entity requests — see
  // `storage/chatContextSnapshot.ts`. Absent on every other route.
  chatContext?: ChatContextSnapshot;
  // Diagnostics for the aggregate: how many GETs still reached the AI
  // service during this request, and which reads bypassed the snapshot.
  upstreamReads?: number;
  // Every request to the AI service in this request, keyed by HTTP method,
  // and every folder read against the Files API — the full round-trip bill.
  upstreamCalls?: Record<string, number>;
  filesApiReads?: number;
  chatContextMisses?: string[];
  // The profile the current chat round runs on, as the send handlers
  // finalized it (thread prefill and agent substitution applied). Read by
  // the document agent tool, whose `callTool` carries no profile.
  roundProfileId?: string;
  // Attachment ref ids of the current user message, captured before the
  // engine runs so an in-round tool call can hand the attached files on.
  roundAttachmentIds?: string[];
}

export interface AppConfig {
  hostname?: string;
  name: string;
  port: number;
  appsettings: string;
  environment: string;
  logName?: string;
  proxy?: { url?: string };
  aiService?: { url?: string };
}

// A host-preconfigured MCP server from `appsettings.json` (`ai.mcp`).
// Wired into the engine as a server-side "system tools" source.
export interface McpServerSetting {
  id: string;
  name: string;
  endpoint: string;
}

export interface AwsCloudWatchConfig {
  accessKeyId?: string;
  secretAccessKey?: string;
  region?: string;
  logGroupName?: string;
  logStreamName?: string;
}

// The external document agent (`ai.documentAgent` in the shared
// `appsettings.json`): an A2A server that creates and edits office documents
// in a sandbox of its own. Absent or without a URL and token, the tool that
// delegates to it is not offered.
export interface DocumentAgentSetting {
  url?: string;
  token?: string;
  taskTimeoutSeconds?: number;
  maxToolCalls?: number;
  shareSystemServers?: boolean;
  /** The portal API as the agent reaches it, for the portal's own model provider; the AI service's address by default. */
  portalUrl?: string;
  /** This service as the agent reaches it, for the file-link server it hands the agent; `http://localhost:<port>` by default. */
  filesUrl?: string;
}

export interface RootConfig {
  app: AppConfig;
  aws?: { cloudWatch?: AwsCloudWatchConfig };
  logConsole?: boolean;
  logPath?: string;
  logLevel?: string;
  // Merged from the shared DocSpace `appsettings.json` `ai` section.
  ai?: { mcp?: McpServerSetting[]; documentAgent?: DocumentAgentSetting };
}

export interface ThreadDto {
  id: string;
  title: string;
  lastEditDate: number;
  profileId?: string | null;
}

export interface MessageDto {
  id: string;
  contents: string;
  timestamp: number;
}

export interface ProfileDto {
  id: string;
  name: string;
  providerType: string;
  baseUrl: string;
  key?: string | null;
  modelId: string;
  reasoning?: unknown;
  capabilities?: unknown;
  createdAt: number;
}

export interface McpServerDto {
  name: string;
  config: string;
}
