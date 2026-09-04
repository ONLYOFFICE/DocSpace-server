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

import { AsyncLocalStorage } from "async_hooks";
import type { IncomingHttpHeaders } from "http";
import type { Request, Response, NextFunction } from "express";
import type { ForwardedHeaders, RequestContext } from "./types.js";
import type { DocspaceFolderInfo } from "./storage/docspaceFilesApi.js";
import type { ChatContextSnapshot } from "./storage/chatContextSnapshot.js";

const HOP_BY_HOP = new Set<string>([
  "host",
  "content-length",
  "content-type",
  "connection",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailer",
  "transfer-encoding",
  "upgrade",
  "expect",
  "accept-encoding",
  // A caller's `x-session-id` must never be forwarded to the provider. The
  // engine derives this header itself from the round's thread id
  // (`ActionArgs.threadId` -> `ProviderCredentials.sessionId`, attached by
  // `OnlyOfficeProvider`), and its own rule is that an explicit `x-session-id`
  // already present in `profile.headers` wins. Forwarded caller headers are
  // merged into exactly that set for `onlyoffice` profiles
  // (`profilesStorage.withOnlyofficeProviderOverrides`), so a client-supplied
  // value would silently displace the thread id and collapse every thread of
  // that caller into one upstream session. The thread id is the only
  // trustworthy source, so the caller's copy is dropped here.
  "x-session-id",
  // A caller's `Mcp-Session-Id` must never be forwarded upstream. The shared
  // docspace-mcp container binds the target portal + credentials to a session
  // at `initialize`, so relaying a client-supplied session id makes a request
  // attach to a foreign session and inherit its portal — a cross-tenant leak,
  // and the cause of tool calls reaching a stale portal. The MCP client threads
  // its own session id through its call arguments, so stripping this is safe.
  "mcp-session-id",
]);

const als = new AsyncLocalStorage<RequestContext>();

function pickForwardableHeaders(rawHeaders: IncomingHttpHeaders | undefined): ForwardedHeaders {
  const out: ForwardedHeaders = {};
  if (!rawHeaders) {
    return out;
  }
  for (const [name, value] of Object.entries(rawHeaders)) {
    if (value === undefined || value === null) {
      continue;
    }
    const lower = name.toLowerCase();
    if (HOP_BY_HOP.has(lower)) {
      continue;
    }
    out[lower] = Array.isArray(value) ? value.join(", ") : String(value);
  }
  return out;
}

export function requestContextMiddleware(req: Request, _res: Response, next: NextFunction): void {
  const ctx: RequestContext = {
    headers: pickForwardableHeaders(req.headers),
    folderInfoCache: new Map(),
  };
  als.run(ctx, () => next());
}

export function getForwardedHeaders(): ForwardedHeaders {
  return als.getStore()?.headers ?? {};
}

// Opt the current request into forwarding the client headers down to the
// provider (see RequestContext.forwardHeadersToProvider). Set by the
// chat-stream handlers; read when the provider profile is resolved.
export function markForwardHeadersToProvider(): void {
  const store = als.getStore();
  if (store) {
    store.forwardHeadersToProvider = true;
  }
}

export function shouldForwardHeadersToProvider(): boolean {
  return als.getStore()?.forwardHeadersToProvider === true;
}

// Remember the form id resolved from the current message's attachments so a
// same-request tool call (whose `ToolsAdapter.callTool` signature carries no
// attachment refs) still targets the right form. Request-scoped: dies with
// the request, never visible to other users or threads.
export function setResolvedFormId(formId: number): void {
  const store = als.getStore();
  if (store) {
    store.resolvedFormId = formId;
  }
}

export function getResolvedFormId(): number | undefined {
  return als.getStore()?.resolvedFormId;
}

// Names of the custom MCP servers resolved for the current round's scope.
// Set by the custom-tools resolver (app/tools/customTools.ts) whenever it
// runs; read by the engine's `systemServerTypes` callback, which is
// synchronous and therefore cannot resolve the registry itself. The send
// handlers prime the resolver before calling the engine so the value is
// present when the callback fires. Request-scoped: dies with the request.
export function setCustomServerNames(names: string[]): void {
  const store = als.getStore();
  if (store) {
    store.customServerNames = names;
  }
}

export function getCustomServerNames(): string[] {
  return als.getStore()?.customServerNames ?? [];
}

// Per-request folder-info memoization store (see RequestContext). Undefined
// outside a request context, where callers fall back to a direct fetch.
export function getFolderInfoCache():
  | Map<string, Promise<DocspaceFolderInfo | undefined>>
  | undefined {
  return als.getStore()?.folderInfoCache;
}

// The round's aggregate read (see RequestContext.chatContext). Set once by the
// send handlers after `GET internal/ai/chat-context`; read by every storage
// read method. Undefined outside a request context and on non-round routes.
export function setChatContextSnapshot(snapshot: ChatContextSnapshot | undefined): void {
  const store = als.getStore();
  if (store) {
    store.chatContext = snapshot;
  }
}

export function getChatContextSnapshot(): ChatContextSnapshot | undefined {
  return als.getStore()?.chatContext;
}

// Per-request count of GET requests that reached the AI service. With a
// primed snapshot a round should end at exactly one (the aggregate itself);
// anything above is a read the snapshot does not cover yet.
export function countUpstreamRead(): void {
  const store = als.getStore();
  if (store) {
    store.upstreamReads = (store.upstreamReads ?? 0) + 1;
  }
}

export function getUpstreamReadCount(): number {
  return als.getStore()?.upstreamReads ?? 0;
}

export function countUpstreamCall(method: string): void {
  const store = als.getStore();
  if (store) {
    const calls = (store.upstreamCalls ??= {});
    calls[method] = (calls[method] ?? 0) + 1;
  }
}

export function getUpstreamCalls(): Record<string, number> {
  return als.getStore()?.upstreamCalls ?? {};
}

export function countFilesApiRead(): void {
  const store = als.getStore();
  if (store) {
    store.filesApiReads = (store.filesApiReads ?? 0) + 1;
  }
}

export function getFilesApiReadCount(): number {
  return als.getStore()?.filesApiReads ?? 0;
}

export function noteChatContextMiss(label: string): void {
  const store = als.getStore();
  if (store) {
    (store.chatContextMisses ??= []).push(label);
  }
}

export function getChatContextMisses(): string[] {
  return als.getStore()?.chatContextMisses ?? [];
}
