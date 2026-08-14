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

import { SystemToolsSource } from "@onlyoffice/ai-chat/core";
import type {
  FetchLike,
  McpHttpServerConfig,
  ToolsAdapter,
} from "@onlyoffice/ai-chat/core";
import type { TMCPItem } from "@onlyoffice/ai-chat";
import {
  getMcpServers,
  mcpPortalBaseUrl,
  PORTAL_MCP_SERVER_NAME,
} from "../../config/index.js";
import { getForwardedHeaders } from "../requestContext.js";
import { withTimeout } from "../storage/httpClient.js";
import { resolveAgentEntityId } from "../storage/docspaceFilesApi.js";
import { storage } from "../storage/index.js";
import logger from "../log.js";

// docspace-mcp resolves the target portal from the `Referer` header — it
// mirrors the C# `HttpClientTransport`, which sends `Referer` = portal root
// plus `Authorization`. Derive the portal root from the forwarded proxy /
// client headers so a request without it doesn't get rejected (HTTP 400).
// docspace-mcp reaches the portal API at the URL carried in `Referer`
// (its internal-mode credential parser uses it as the API base). The MCP
// server runs in a container, so a loopback host derived from the browser
// origin (`localhost:8092` in dev) points at the container itself, not the
// portal — rewrite it to `host.docker.internal`, which resolves to the
// host from inside Docker. Real portal domains pass through untouched.
const LOOPBACK_HOSTS = new Set(["localhost", "127.0.0.1", "[::1]", "::1"]);
const CONTAINER_HOST_ALIAS = "host.docker.internal";

function rewriteLoopback(portalRoot: string): string {
  try {
    const url = new URL(portalRoot);
    if (LOOPBACK_HOSTS.has(url.hostname)) {
      url.hostname = CONTAINER_HOST_ALIAS;
      return url.href;
    }
  } catch {
    // not a URL — return as-is, the MCP server will reject it
  }
  return portalRoot;
}

function portalReferer(headers: Record<string, string>): string | undefined {
  // Explicit override (`AI__MCP_PORTAL_BASE_URL`) wins over derivation —
  // for setups where neither the client origin nor the loopback rewrite
  // yields an address the MCP container can reach.
  const override = mcpPortalBaseUrl();
  if (override) {
    return override.replace(/\/+$/, "") + "/";
  }
  const origin = headers["origin"];
  if (origin) {
    return rewriteLoopback(origin.replace(/\/+$/, "") + "/");
  }
  const xfHost = headers["x-forwarded-host"]?.split(",")[0]?.trim();
  if (xfHost) {
    const proto = headers["x-forwarded-proto"]?.split(",")[0]?.trim() || "https";
    return rewriteLoopback(`${proto}://${xfHost}/`);
  }
  const referer = headers["referer"];
  if (referer) {
    try {
      const parsed = new URL(referer);
      return rewriteLoopback(`${parsed.protocol}//${parsed.host}/`);
    } catch {
      // malformed referer — fall through
    }
  }
  return undefined;
}

// Pull a single cookie value out of a `Cookie` header.
function readCookie(cookieHeader: string | undefined, name: string): string | undefined {
  if (!cookieHeader) {
    return undefined;
  }
  for (const part of cookieHeader.split(";")) {
    const idx = part.indexOf("=");
    if (idx <= 0) {
      continue;
    }
    if (part.slice(0, idx).trim() === name) {
      return part.slice(idx + 1).trim();
    }
  }
  return undefined;
}

// docspace-mcp authenticates via the `Authorization` header, not the cookie.
// Mirror the C# `DocSpaceTransportBuilder`: use the raw `asc_auth_key` cookie
// value as the Authorization value, falling back to an incoming Authorization
// header. The browser sends the auth as a cookie, so without this the MCP
// server rejects the request ("Authorization header is required").
const AUTH_COOKIE = "asc_auth_key";

function authorizationValue(headers: Record<string, string>): string | undefined {
  const cookie = headers["cookie"];
  return (
    readCookie(cookie, AUTH_COOKIE) ??
    readCookie(cookie, "authorization") ??
    headers["authorization"]
  );
}

// Browser content-negotiation headers. Stripped from the forwarded set so
// they don't conflict with the `Accept` the library now sets on every MCP
// request (a duplicate / combined Accept makes the server answer 406).
const NEGOTIATION_HEADERS = ["accept", "accept-encoding", "accept-language"];

function stripNegotiation(
  headers: Record<string, string>,
): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [key, value] of Object.entries(headers)) {
    if (!NEGOTIATION_HEADERS.includes(key.toLowerCase())) {
      out[key] = value;
    }
  }
  return out;
}

// Forward the caller's DocSpace auth/context headers to the MCP server so
// it acts on behalf of the current user, plus an `Authorization` header
// derived from the auth cookie and a `Referer` pointing at the portal root.
// `getForwardedHeaders` reads the per-request AsyncLocalStorage context,
// populated for the duration of the request that drives the chat stream /
// `listSystemTools` call. Content negotiation belongs to the MCP client: it
// sets `Accept` / `Content-Type` / `Mcp-Session-Id` itself, so we drop the
// browser's negotiation headers from the forwarded set and let the
// library's headers win.
//
// Verbose by design: system tools have been silently absent, so every MCP
// round-trip is logged (method, url, status, errors) to pinpoint where the
// flow breaks — unreachable endpoint, auth, referer, or an empty tool list.
const forwardingFetch: FetchLike = async (url, init) => {
  const provided = (init?.headers as Record<string, string> | undefined) ?? {};
  const forwarded = stripNegotiation(getForwardedHeaders());
  const referer = portalReferer(forwarded);
  const authorization = authorizationValue(forwarded);
  const method = (init?.method ?? "GET").toUpperCase();
  logger.info(
    `MCP fetch -> ${method} ${url} (authorization=${Boolean(
      authorization,
    )}, referer=${referer ?? "-"}, headers=[${Object.keys(forwarded)
      .sort()
      .join(",")}])`,
  );
  // Bound the connect/header phase so an unreachable or hung MCP server
  // can't pin the request forever; the timer is cleared once the response
  // is in hand, leaving streamed bodies to flow under the MCP client's own
  // signal.
  const { signal, cancel } = withTimeout(init?.signal ?? undefined);
  try {
    const res = await fetch(url, {
      ...init,
      signal,
      headers: {
        ...forwarded,
        ...(referer ? { referer } : {}),
        ...(authorization ? { authorization } : {}),
        ...provided,
      },
    });
    cancel();
    if (res.ok) {
      logger.info(`MCP fetch <- ${res.status} ${res.statusText} for ${url}`);
    } else {
      // Read a clone so the MCP client can still consume the original body.
      let body = "";
      try {
        body = (await res.clone().text()).slice(0, 500);
      } catch {
        // ignore — body not readable
      }
      logger.warn(
        `MCP fetch <- ${res.status} ${res.statusText} for ${url} body=${body}`,
      );
    }
    return res;
  } catch (err) {
    cancel();
    logger.error(
      `MCP fetch failed for ${url}: ${
        err instanceof Error ? err.message : String(err)
      }`,
    );
    throw err;
  }
};

// Map each preconfigured MCP server onto a system-tools entry keyed by its
// name (the `serverType` used for per-tool prefs and approval grouping).
function buildServers(): Record<string, McpHttpServerConfig> {
  const servers: Record<string, McpHttpServerConfig> = {};
  for (const server of getMcpServers()) {
    servers[server.name] = { url: server.endpoint };
  }
  return servers;
}

const servers = buildServers();
const serverNames = Object.keys(servers);

/**
 * Canonical config of a configured system MCP server, by name. The tools
 * controller uses it to pin entity-scoped whitelist markers for system
 * servers to the real endpoint instead of trusting a client-provided
 * config — so a marker entry never shadows the system group with a broken
 * connection.
 */
export function getSystemServerConfig(
  name: string,
): McpHttpServerConfig | undefined {
  return servers[name];
}

if (serverNames.length > 0) {
  logger.info(`System MCP tools configured: ${serverNames.join(", ")}`);
} else {
  logger.info("No system MCP tools configured (ai.mcp empty)");
}

const source = new SystemToolsSource({
  servers,
  fetch: forwardingFetch,
});

// Agent rooms carry an MCP whitelist: only the servers enabled for the
// agent are served to its chat. The whitelist is the agent's per-entity
// custom-servers map in `/mcp-servers` (managed by the agent
// create/edit dialog through the new-ai `tools/*` routes): an entry whose
// name matches a system server acts as the "enabled" marker for it.
// Returns `undefined` when `entityId` is absent or not an agent room — no
// filtering, the full configured set stays available. Fails closed: if the
// whitelist cannot be resolved, an agent gets no system MCP tools rather
// than all of them.
async function agentServerWhitelist(
  entityId: string | undefined,
): Promise<Set<string> | undefined> {
  if (!entityId) {
    return undefined;
  }
  const agentId = await resolveAgentEntityId(entityId);
  if (!agentId) {
    return undefined;
  }
  // The portal's own MCP server is always enabled, agents included — it
  // never depends on the per-agent marker map (and the failure path below
  // keeps it too).
  try {
    const servers = await storage.mcpServers.readAll(agentId);
    return new Set([...Object.keys(servers), PORTAL_MCP_SERVER_NAME]);
  } catch (err) {
    logger.error(
      `systemTools: failed to resolve MCP whitelist for agent ${agentId}: ${
        err instanceof Error ? err.message : String(err)
      } — serving only the portal MCP server`,
    );
    return new Set([PORTAL_MCP_SERVER_NAME]);
  }
}

function filterByWhitelist<T>(
  grouped: Record<string, T>,
  whitelist: Set<string> | undefined,
): Record<string, T> {
  if (!whitelist) {
    return grouped;
  }
  const filtered: Record<string, T> = {};
  const dropped: string[] = [];
  for (const [serverType, items] of Object.entries(grouped)) {
    if (whitelist.has(serverType)) {
      filtered[serverType] = items;
    } else {
      dropped.push(serverType);
    }
  }
  if (dropped.length > 0) {
    logger.info(
      `systemTools: servers [${dropped.join(", ")}] are not enabled for this agent — filtered out`,
    );
  }
  return filtered;
}

// Read-only (GET) MCP operations run without an approval dialog.
//
// docspace-mcp marks its read-only operations with the MCP-standard
// `annotations.readOnlyHint: true`; the raw descriptor survives the
// `tools/list` cast, so the hint is readable here. System servers are
// host-configured and trusted, so the hint is honored for all of them:
// `requireApproval: false` auto-allows the call in every consumer of this
// catalog — the DocSpace chat engine (autoAllow on tool-call-pending),
// agents, and the editor plugin (via editor-tools/list). Mutating tools
// keep prompting through the systemServerTypes approval flow.
function withReadOnlyAutoAllow(
  grouped: Record<string, TMCPItem[]>,
): Record<string, TMCPItem[]> {
  const result: Record<string, TMCPItem[]> = {};
  for (const [type, items] of Object.entries(grouped)) {
    result[type] = items.map((tool) => {
      const annotations = (
        tool as { annotations?: { readOnlyHint?: unknown } }
      ).annotations;
      return annotations?.readOnlyHint === true
        ? { ...tool, requireApproval: false }
        : tool;
    });
  }
  return result;
}

// A failed MCP tool call is usually NOT a transport error: the MCP server
// answers 200 with `isError: true` inside the result payload (e.g. when the
// portal rejected its API call with a 401), so neither the fetch-level warn
// nor the callTool catch ever fires — on a warn-level stand such failures
// were completely invisible. Detect that shape and return the last line of
// the error text (the MCP server's own step trace ends with the actual
// failure, e.g. "GET …/files/rooms: 401 Unauthorized"; never user content).
// Returns undefined for successful or unrecognized results.
function describeToolErrorResult(result: unknown): string | undefined {
  let payload: unknown = result;
  if (typeof payload === "string") {
    try {
      payload = JSON.parse(payload);
    } catch {
      return undefined;
    }
  }
  if (!payload || typeof payload !== "object") {
    return undefined;
  }
  const { isError, content } = payload as {
    isError?: unknown;
    content?: unknown;
  };
  if (isError !== true) {
    return undefined;
  }
  let text = "";
  if (Array.isArray(content)) {
    const first = content.find(
      (part): part is { text: string } =>
        !!part &&
        typeof part === "object" &&
        typeof (part as { text?: unknown }).text === "string",
    );
    text = first?.text ?? "";
  }
  const lines = text
    .split("\n")
    .map((line) => line.trim())
    .filter((line) => line.length > 0);
  const last = lines[lines.length - 1] ?? "<no error text>";
  return last.slice(0, 300);
}

// Diagnostic wrapper around the source: logs enumeration and invocation so
// we can see whether the engine is even offered the system tools and
// whether calls reach the MCP server. Delegates everything to `source`;
// keeps `getServerTypes` so `AIEngineDeps.systemServerTypes` still works.
//
// Shared instance: wire the same object into `AIEngine` (execution, via
// `composeToolsAdapters`) and `ToolsEngine` (listing for permission cards).
export const systemToolsSource: ToolsAdapter & {
  getServerTypes(): string[];
} = {
  async getTools(entityId, config) {
    const serverNames = source.getServerTypes();
    logger.info(
      `systemTools.getTools(entityId=${entityId ?? "-"}, attachments=${
        config?.attachmentId.length ?? 0
      }) configured servers=[${serverNames.join(", ") || "<none>"}] — enumerating`,
    );
    const started = Date.now();
    let grouped: Awaited<ReturnType<typeof source.getTools>>;
    try {
      const whitelist = await agentServerWhitelist(entityId);
      grouped = withReadOnlyAutoAllow(
        filterByWhitelist(await source.getTools(), whitelist),
      );
    } catch (err) {
      logger.error(
        `systemTools.getTools failed after ${Date.now() - started}ms: ${err instanceof Error ? err.message : String(err)}`,
      );
      return {};
    }
    for (const [type, items] of Object.entries(grouped)) {
      const names = items.map((t) => t.name).join(", ") || "<none>";
      logger.info(
        `systemTools.getTools server=${type} -> ${items.length} tool(s) in ${Date.now() - started}ms: [${names}]`,
      );
    }
    if (Object.keys(grouped).length === 0) {
      logger.warn(`systemTools.getTools(entityId=${entityId ?? "-"}) -> no tools from any server`);
    }
    return grouped;
  },

  async callTool(toolName, args, entityId) {
    logger.info(
      `systemTools.callTool name=${toolName} args=[${Object.keys(args).join(
        ",",
      )}] entityId=${entityId ?? "-"}`,
    );
    try {
      // Enforce the agent whitelist on execution too — the tool list is
      // rebuilt per stream, but a call by name must not slip through for a
      // server the agent has disabled.
      const whitelist = await agentServerWhitelist(entityId);
      if (whitelist) {
        const grouped = await source.getTools();
        const owner = Object.entries(grouped).find(
          ([, items]) => items.some((t) => t.name === toolName),
        )?.[0];
        if (owner !== undefined && !whitelist.has(owner)) {
          logger.warn(
            `systemTools.callTool name=${toolName} denied: server "${owner}" is not enabled for this agent`,
          );
          return `Tool "${toolName}" is not available: its MCP server is not enabled for this agent.`;
        }
      }
      const result = await source.callTool(toolName, args);
      const failure = describeToolErrorResult(result);
      if (failure) {
        logger.warn(
          `systemTools.callTool name=${toolName} returned an error result: ${failure}`,
        );
      } else {
        logger.info(`systemTools.callTool name=${toolName} ok`);
      }
      return result;
    } catch (err) {
      logger.error(
        `systemTools.callTool name=${toolName} failed: ${
          err instanceof Error ? err.message : String(err)
        }`,
      );
      throw err;
    }
  },

  getServerTypes() {
    return source.getServerTypes();
  },
};
