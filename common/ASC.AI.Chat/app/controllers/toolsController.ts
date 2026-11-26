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

import { ToolsEngine, isToolPrefsScope } from "@onlyoffice/ai-chat/core";
import type { McpServerConfig, ToolPrefsScope } from "@onlyoffice/ai-chat/core";
import {
  PORTAL_MCP_SERVER_NAME,
  DOCSPACE_INTEGRATION_SERVER_TYPE,
  DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
  WEB_SEARCH_TYPE,
  IMAGE_GENERATION_TYPE,
  DOCUMENT_AGENT_TYPE,
} from "../../config/index.js";
import { customToolsSource, resolveCustomServers } from "../tools/customTools.js";
import { storage } from "../storage/index.js";
import { systemToolsSource, getSystemServerConfig } from "../tools/systemTools.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { asString, isObject } from "../narrow.js";
import { assertEntityAccessible } from "../storage/docspaceFilesApi.js";
import { clientFault } from "../httpErrors.js";
import { optionalString } from "./requestFields.js";
import { assertAiAccess } from "./aiAccess.js";

const engine = new ToolsEngine({ storage, systemToolsSource });

// A custom MCP server name is used verbatim as a single URL path segment on
// the read / update / delete routes (`/mcp-servers/{name}` on the .NET AI
// service, and `/mcp-servers/${encodeURIComponent(name)}` on the way there).
// `create` takes the name in the request body, so a name that isn't a safe
// path segment registers fine but is then unreachable by name: a `/` becomes
// `%2F` (rejected / mis-routed by the .NET host) and a `.`/`..` dot-segment is
// normalised away, orphaning a stored-but-undeletable entry (Bug 82985).
// Reject such names at the source with a 400 instead. Printable punctuation
// and spaces survive URL-encoding and stay routable, so they are allowed.
const UNSAFE_NAME_CHARS = /[\u0000-\u001f\u007f/\\]/;

function assertRoutableServerName(rawName: unknown): string {
  if (typeof rawName !== "string" || rawName.trim().length === 0) {
    throw clientFault(400, "name is required");
  }
  if (rawName === "." || rawName === ".." || UNSAFE_NAME_CHARS.test(rawName)) {
    throw clientFault(
      400,
      'name must not be ".", "..", or contain a path separator or control character',
    );
  }
  return rawName;
}

// Resolve the config to store for an entry. Entries named after a
// configured system server are pinned to the canonical system config (see
// getSystemServerConfig in tools/systemTools.ts) so such an entry never
// shadows the system group with a broken connection. A write without a
// config re-saves the entry already registered under that name; anything
// else without a config is refused. `stored` is the registry read once by a
// caller that resolves many entries (the whole-map save).
async function resolveConfig(
  name: string,
  provided: unknown,
  stored?: Record<string, McpServerConfig>,
): Promise<McpServerConfig> {
  const system = getSystemServerConfig(name);
  if (system) {
    return system;
  }
  const portal = stored
    ? (stored[name] ?? null)
    : await storage.mcpServers.readByName(name, undefined);
  if (isObject(provided) && Object.keys(provided).length > 0) {
    return keepStoredHeaders(provided, portal);
  }
  if ((typeof provided === "string" || Array.isArray(provided)) && provided.length > 0) {
    return forTheEngineToRefuse(provided);
  }
  if (portal) {
    return portal;
  }
  throw clientFault(400, `No config provided and no portal-level server named "${name}"`);
}

// A config that is a string or a list goes to the engine as sent: the engine
// refuses it as a value on the `url` field, and the whole-map save reports it
// among the entries it refuses (spec ai-agents, "Server configurations and
// replace-all"; ASC.AI.Tests, McpServerReplaceAllTests). The library types the
// parameter as an object, which this value is not: the one place the
// controller hands the engine a value its type does not describe.
function forTheEngineToRefuse(config: unknown): McpServerConfig {
  return config as McpServerConfig;
}

// No read answers a server's headers (see withoutHeaders below), so a config
// that comes back from the editor has none. Writing it as is would erase the
// credentials on every save; a config with no `headers` key therefore keeps
// the stored ones. Only while it still points at the same `url`: carried to
// another address they would hand the credentials to whoever can edit the
// entry. An explicit `headers` (an empty object included) always wins, which
// is how they are replaced or removed.
function keepStoredHeaders(
  provided: McpServerConfig,
  stored: McpServerConfig | null,
): McpServerConfig {
  if ("headers" in provided || !stored || !isObject(stored["headers"])) {
    return provided;
  }
  if (typeof provided["url"] !== "string" || provided["url"] !== stored["url"]) {
    return provided;
  }
  return { ...provided, headers: stored["headers"] };
}

// The headers a server is called with carry its credentials. The servers run
// in this process (see tools/customTools.ts), so the browser never needs
// them and no read answers them.
function withoutHeaders(config: McpServerConfig): McpServerConfig {
  const { headers: _headers, ...shown } = config;
  return shown;
}

// System-server entries are pinned to the canonical internal endpoint
// (see resolveConfig above). Redact — never drop — the config on the way
// out: system servers run server-side only (see tools/systemTools.ts), so
// the browser must not receive a config it would try to start itself, and
// the internal endpoint must not leak. The name still round-trips: saving
// the whole map from the chat config editor re-pins the entry through
// resolveConfig.
function redactSystemServer(name: string, config: McpServerConfig): McpServerConfig {
  return getSystemServerConfig(name) ? {} : config;
}

const INVALID_SCOPE = Symbol("invalid tool-prefs scope");

// The optional `scope` of a disabled-list read or write (Bug 84381):
// `"user"` (the caller's own list, what an absent value means) or
// `"tenant"` (the list an administrator keeps for everyone, which the AI
// service folds into every user's list on read). Anything else is a 400
// rather than a silent fall-back to the user list, so a settings screen
// cannot believe it switched a tool off for everyone when it did not.
function parseToolPrefsScope(value: unknown): ToolPrefsScope | undefined | typeof INVALID_SCOPE {
  if (value === undefined || value === null || value === "") {
    return undefined;
  }
  return isToolPrefsScope(value) ? value : INVALID_SCOPE;
}

function invalidScopeMessage(value: unknown): string {
  return `unknown scope "${String(value)}"; valid values: tenant, user`;
}

// Body narrowing for the two prefs writes. The library coerces instead of
// checking: a string `toolNames` spreads into one entry per character, an
// absent one reads as "clear the list" and `Boolean("false")` is true
// (Bugs 84402, 84403, 84404, 84405). Each answers 400 with the field named,
// before anything is read or written.
function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

// The names, or the message of the 400 that refuses them.
function readToolNames(value: unknown): string[] | string {
  if (!Array.isArray(value)) {
    return "toolNames is required and must be an array of tool names";
  }
  const names = value.filter(isNonEmptyString);
  if (names.length !== value.length) {
    return "toolNames must contain non-empty strings only";
  }
  return names;
}

export const toolsController = {
  addCustomServer: asyncHandler(async (req, res) => {
    // The access gate before the body (ts-ai-failure-paths.md §3): a guest or a portal with AI
    // off gets 403 for any body the parser accepted, and no Files API read is made on their behalf.
    await assertAiAccess();
    const args = unpackPositional(req.body, ["name", "config", "entityId"] as const);
    const name = assertRoutableServerName(args.name);
    // A supplied entityId must at least be REACHABLE: a nonexistent or
    // deleted id otherwise silently mutates the portal-wide registry
    // (Bug 82975). An accessible folder is accepted and ignored — the
    // registry is one map per portal (Bug 82863: the widget sends the
    // current location here) — mirroring the threads/create decision
    // (Bug 82719).
    const entityId = optionalString(args.entityId, "entityId");
    await assertEntityAccessible(entityId);
    const result = await engine.addCustomServer(
      name,
      await resolveConfig(name, args.config),
      entityId,
    );
    res.json(result);
  }),

  updateCustomServer: asyncHandler(async (req, res) => {
    // The access gate before the body (ts-ai-failure-paths.md §3): a guest or a portal with AI
    // off gets 403 for any body the parser accepted, and no Files API read is made on their behalf.
    await assertAiAccess();
    const args = unpackPositional(req.body, ["name", "config", "entityId"] as const);
    const name = assertRoutableServerName(args.name);
    // Same accessibility gate as addCustomServer (Bug 82975).
    const entityId = optionalString(args.entityId, "entityId");
    await assertEntityAccessible(entityId);
    const result = await engine.updateCustomServer(
      name,
      await resolveConfig(name, args.config),
      entityId,
    );
    res.json(result);
  }),

  removeCustomServer: asyncHandler(async (req, res) => {
    // The access gate before the body (ts-ai-failure-paths.md §3): a guest or a portal with AI
    // off gets 403 for any body the parser accepted, and no Files API read is made on their behalf.
    await assertAiAccess();
    const args = unpackPositional(req.body, ["name", "entityId"] as const);
    const name = typeof args.name === "string" ? args.name : asString(req.query["name"]);
    if (!name) {
      res.status(400).json({ error: "name required" });
      return;
    }
    // Same accessibility gate as addCustomServer (Bug 82975), and an entityId of another type is a
    // 400. The registry is one per portal (mcpServersStorage.delete ignores the scope): whatever
    // entityId is sent, the delete removes the portal's server of that name.
    const entityId = optionalString(args.entityId, "entityId");
    await assertEntityAccessible(entityId);
    await engine.removeCustomServer(name, entityId);
    res.json({ success: true });
  }),

  getCustomServer: asyncHandler(async (req, res) => {
    await assertAiAccess();
    const name = asString(req.query["name"]);
    if (!name) {
      res.status(400).json({ error: "name required" });
      return;
    }
    const entityId = asString(req.query["entityId"]);
    const config = await engine.getCustomServer(name, entityId);
    if (config === null) {
      res.json(null);
      return;
    }
    res.json(withoutHeaders(redactSystemServer(name, config)));
  }),

  listCustomServers: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const servers = await engine.listCustomServers(entityId);
    const redacted: Record<string, McpServerConfig> = {};
    for (const [name, config] of Object.entries(servers)) {
      // The portal MCP server is not user-manageable: a legacy registry
      // entry named after it must not surface as a selectable entry (it is
      // always enabled server-side, see systemTools).
      if (name === PORTAL_MCP_SERVER_NAME) continue;
      // The config editor reads this map and writes it back whole; the
      // headers it does not get are kept by resolveConfig on that write.
      redacted[name] = withoutHeaders(redactSystemServer(name, config));
    }
    res.json(redacted);
  }),

  listSystemTools: asyncHandler(async (req, res) => {
    await assertAiAccess();
    const entityId = asString(req.query["entityId"]);
    // The catalog is the system groups plus the registered custom MCP
    // servers' live tools — before this merge no listing route could show a
    // registered server's tools at all (Bug 83163). Server-type keys are
    // unique across the two sources (customToolsSource skips system-server
    // markers), so a plain spread cannot clobber a group.
    const [tools, custom] = await Promise.all([
      engine.listSystemTools(entityId),
      // The richer enumeration also says why a registered server delivered
      // no tools — the client shows that message on the permission card
      // (the browser has no local access to a server-executed MCP server).
      customToolsSource.getToolsWithErrors(entityId),
    ]);
    // This engine's source exposes plain getTools only, so the system half
    // always comes back as the legacy grouped map.
    const listed: unknown = isObject(tools) && "groups" in tools ? tools["groups"] : tools;
    // Hide the portal MCP server from every management surface (the MCP
    // settings page's permission cards):
    // it is always enabled with all tools and cannot be configured. The
    // chat engine's tool context does not go through this listing, so the
    // tools themselves stay available everywhere.
    // A listing that is not a map is refused with 500. A system source that
    // fails does not come here: systemToolsSource.getTools logs the failure
    // and answers an empty map (DEFECT tools-11), so the route answers 200
    // with no system groups for it.
    if (!isObject(listed)) {
      throw new Error("the system tool listing is not an object");
    }
    const systemGroups: Record<string, unknown> = {};
    for (const [serverType, group] of Object.entries(listed)) {
      if (serverType !== PORTAL_MCP_SERVER_NAME) systemGroups[serverType] = group;
    }
    res.json({
      groups: { ...systemGroups, ...custom.groups },
      errors: custom.errors,
      // `groups` mixes system and registered custom servers, so name the
      // system ones explicitly — the settings page labels servers by this
      // list; without it every registered custom server got the portal
      // branding (Bug 83705).
      system: Object.keys(systemGroups),
    });
  }),

  replaceAllCustomServers: asyncHandler(async (req, res) => {
    // The access gate before the body (ts-ai-failure-paths.md §3): a guest or a portal with AI
    // off gets 403 for any body the parser accepted, and no Files API read is made on their behalf.
    await assertAiAccess();
    const args = unpackPositional(req.body, ["map", "entityId"] as const);
    // `map` is required. Without it the loop below yields an empty map and
    // replaceAll wipes every registered MCP server for the scope, silently
    // destroying the configuration on a malformed request (Bug 82864). Reject
    // a missing/invalid map with a 400 instead.
    if (!isObject(args.map)) {
      res.status(400).json({ error: "map is required and must be an object" });
      return;
    }
    // An unreachable entityId is refused (Bug 82975). The map replaced is the
    // portal's one registry whatever entityId names.
    const entityId = optionalString(args.entityId, "entityId");
    await assertEntityAccessible(entityId);
    const map = args.map;
    // The complete registry, or a 502: a stored server the read cannot make out is not "no
    // such server", and the map written below would lose its headers.
    const stored = await storage.mcpServers.readAllForWrite();
    const normalized: Record<string, McpServerConfig> = {};
    for (const [name, config] of Object.entries(map)) {
      assertRoutableServerName(name);
      normalized[name] = await resolveConfig(name, config, stored);
    }
    const result = await engine.replaceAllCustomServers(normalized, entityId);
    res.json(result);
  }),

  setDisabled: asyncHandler(async (req, res) => {
    // The access gate before the body (ts-ai-failure-paths.md §3): a guest or a portal with AI
    // off gets 403 for any body the parser accepted, and no Files API read is made on their behalf.
    await assertAiAccess();
    const args = unpackPositional(req.body, [
      "serverType",
      "toolNames",
      "entityId",
      "scope",
    ] as const);
    // Same accessibility gate as the server writes above (Bug 82975): a
    // bogus entityId must not silently write prefs into the global scope.
    const entityId = optionalString(args.entityId, "entityId");
    await assertEntityAccessible(entityId);
    const scope = parseToolPrefsScope(args.scope);
    if (scope === INVALID_SCOPE) {
      res.status(400).json({ error: invalidScopeMessage(args.scope) });
      return;
    }
    // An explicit empty list stays legal: it is the documented "switch
    // everything back on" and what the settings page sends for it.
    const toolNames = readToolNames(args.toolNames);
    if (typeof toolNames === "string") {
      res.status(400).json({ error: toolNames });
      return;
    }
    // The serverType must be a group key the round's tool filter actually
    // matches — an arbitrary string used to be stored verbatim and read back
    // "successfully" while never disabling anything (Bug 83013). Valid keys:
    // the host-configured system servers, the two DocSpace-integration
    // groups, web-search / image-generation, and the scope's registered
    // custom MCP servers.
    const serverType: unknown = args.serverType;
    const validTypes = new Set<string>([
      ...systemToolsSource.getServerTypes(),
      DOCSPACE_INTEGRATION_SERVER_TYPE,
      DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE,
      WEB_SEARCH_TYPE,
      IMAGE_GENERATION_TYPE,
      DOCUMENT_AGENT_TYPE,
      ...Object.keys(await resolveCustomServers(entityId, { strict: true })),
    ]);
    if (typeof serverType !== "string" || !validTypes.has(serverType)) {
      res.status(400).json({
        error:
          `unknown serverType "${String(serverType)}"; valid values: ` +
          [...validTypes].sort().join(", "),
      });
      return;
    }
    await engine.setDisabled(serverType, toolNames, entityId, scope);
    res.json({ success: true });
  }),

  getDisabled: asyncHandler(async (req, res) => {
    await assertAiAccess();
    const entityId = asString(req.query["entityId"]);
    const scope = parseToolPrefsScope(req.query["scope"]);
    if (scope === INVALID_SCOPE) {
      res.status(400).json({ error: invalidScopeMessage(req.query["scope"]) });
      return;
    }
    const map = await engine.getDisabled(entityId, scope);
    res.json(map);
  }),

  isToolDisabled: asyncHandler(async (req, res) => {
    await assertAiAccess();
    const serverType = asString(req.query["serverType"]);
    const toolName = asString(req.query["toolName"]);
    if (!serverType || !toolName) {
      res.status(400).json({ error: "serverType and toolName required" });
      return;
    }
    const entityId = asString(req.query["entityId"]);
    const value = await engine.isToolDisabled(serverType, toolName, entityId);
    res.json(value);
  }),

  setAllowAlways: asyncHandler(async (req, res) => {
    // The access gate before the body (ts-ai-failure-paths.md §3): a guest or a portal with AI
    // off gets 403 for any body the parser accepted, and no Files API read is made on their behalf.
    await assertAiAccess();
    const args = unpackPositional(req.body, [
      "serverType",
      "toolName",
      "value",
      "entityId",
    ] as const);
    // Same accessibility gate as the server writes above (Bug 82975).
    const entityId = optionalString(args.entityId, "entityId");
    await assertEntityAccessible(entityId);
    if (!isNonEmptyString(args.serverType)) {
      res.status(400).json({ error: "serverType is required and must be a non-empty string" });
      return;
    }
    if (!isNonEmptyString(args.toolName)) {
      res.status(400).json({ error: "toolName is required and must be a non-empty string" });
      return;
    }
    if (typeof args.value !== "boolean") {
      res.status(400).json({ error: "value is required and must be a boolean" });
      return;
    }
    await engine.setAllowAlways(args.serverType, args.toolName, args.value, entityId);
    res.json({ success: true });
  }),

  getAllowAlways: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const tokens = await engine.getAllowAlways(entityId);
    res.json(tokens);
  }),

  isAllowAlways: asyncHandler(async (req, res) => {
    await assertAiAccess();
    const serverType = asString(req.query["serverType"]);
    const toolName = asString(req.query["toolName"]);
    if (!serverType || !toolName) {
      res.status(400).json({ error: "serverType and toolName required" });
      return;
    }
    const entityId = asString(req.query["entityId"]);
    const value = await engine.isAllowAlways(serverType, toolName, entityId);
    res.json(value);
  }),
};
