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

import nconf from "nconf";
import path from "path";
import fs from "fs";
import { fileURLToPath } from "url";
import type {
  AppConfig,
  CompactionSetting,
  DocumentAgentSetting,
  McpServerSetting,
  RootConfig,
} from "../app/types.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

nconf.argv().env().file("config", path.join(__dirname, "config.json"));

const nodeEnv: string | undefined = nconf.get("NODE_ENV");
console.log("NODE_ENV: " + nodeEnv);

if (
  nodeEnv &&
  nodeEnv !== "development" &&
  fs.existsSync(path.join(__dirname, nodeEnv + ".json"))
) {
  nconf.file("config", path.join(__dirname, nodeEnv + ".json"));
}

getAndSaveAppsettings();

export default nconf;

export function getAppConfig(): AppConfig {
  const app: AppConfig = nconf.get("app");
  return app;
}

export function getRootConfig(): RootConfig {
  const root: RootConfig = nconf.get();
  return root;
}

// Per-entry endpoint override injected by the Aspire AppHost as
// `AI__MCP__<i>__ENDPOINT` (e.g. `http://onlyoffice-docspace-mcp:8000/mcp`),
// overriding the stale appsettings value. The `__` form is deliberate: a
// `:`-keyed var (the .NET convention) gets nested by nconf and would turn
// the `ai.mcp` array into an object. Read straight from the environment.
function mcpEndpointOverride(index: number): string | undefined {
  return process.env[`AI__MCP__${index}__ENDPOINT`];
}

// Portal base URL for the docspace-mcp hop, injected by the Aspire AppHost
// as `AI__MCP_PORTAL_BASE_URL` (same `__` form as the endpoint override
// above). docspace-mcp in internal mode reaches the portal API at the URL
// carried in the forwarded `Referer` header; deriving that from the client
// headers breaks in dev, where the browser origin is `localhost:8092` — a
// loopback the MCP *container* cannot reach. When set, this address (the
// proxy as seen from containers, e.g. `http://host.docker.internal:8092/`)
// replaces the derived referer. Unset in production installs, where the
// client-derived portal domain is reachable and tenant-correct.
export function mcpPortalBaseUrl(): string | undefined {
  return process.env["AI__MCP_PORTAL_BASE_URL"] || undefined;
}

// `core:cors` from the shared appsettings, as `BaseStartup` reads it for the
// .NET services. Those also take it from the environment as `core__cors`;
// nconf is not given a `__` separator here -- see mcpEndpointOverride for
// why -- so that form is read straight from the environment and wins over
// the file, as it does for .NET.
export function coreCors(): string | undefined {
  const fromEnv = process.env["core__cors"];
  if (fromEnv !== undefined) {
    return fromEnv;
  }
  const fromFile: unknown = nconf.get("core:cors");
  return typeof fromFile === "string" ? fromFile : undefined;
}

// The base URL of a service this one calls: the environment variable when it is set and not
// empty, else the settings entry when it is not empty, else the default; trailing slashes are
// stripped, as every caller appends a path that starts with one. A settings entry that is not a
// string is no setting: it used to stop the service at load.
function baseUrl(envName: string, fromSettings: unknown, fallback: string): string {
  const value =
    process.env[envName] || (typeof fromSettings === "string" ? fromSettings : "") || fallback;
  return value.replace(/\/+$/, "");
}

/** The portal's proxy, the Files and People APIs behind it: `API_HOST`, then `app.proxy.url`. */
export function getProxyUrl(): string {
  const app: AppConfig | undefined = nconf.get("app");
  return baseUrl("API_HOST", app?.proxy?.url, "http://localhost:8092");
}

/** The AI service: `AI_SERVICE_URL`, then `app.aiService.url`. */
export function getAiServiceUrl(): string {
  const app: AppConfig | undefined = nconf.get("app");
  return baseUrl("AI_SERVICE_URL", app?.aiService?.url, "http://localhost:5157");
}

// `AI_CHAT_CONTEXT=off` switches the chat-context read of a round off for the whole process
// (the per-request header of chatContext.ts does the same for one request). Only the exact
// value `off`: any other value, `OFF` included, leaves the read on.
export function chatContextDisabled(): boolean {
  return process.env["AI_CHAT_CONTEXT"] === "off";
}

// SSRF egress policy for user-supplied provider / web-search `baseUrl`s.
// By default loopback and RFC1918 private ranges are rejected before any
// outbound call (see `assertSafeBaseUrl` in app/security.ts), mirroring the
// C# `UrlValidator` default blacklist. On-prem installs that run a model
// server on an internal address — a local Ollama at 127.0.0.1, an inference
// box on 10.x — opt those ranges back in by setting
// `AI__ALLOW_PRIVATE_BASE_URL=true`. Cloud-metadata / link-local
// (169.254.0.0/16, fe80::/10) and the unspecified 0.0.0.0/8 stay blocked
// regardless — they are never a legitimate endpoint.
export function allowPrivateBaseUrl(): boolean {
  const value = process.env["AI__ALLOW_PRIVATE_BASE_URL"];
  return value === "true" || value === "1";
}

// The largest image an attachment is inlined from, in bytes: `ai:maxImageSize`
// of appsettings, the size the Files service lets a chat take an image up to.
const DEFAULT_MAX_IMAGE_SIZE = 20 * 1024 * 1024;
const MAX_IMAGE_SIZE_LIMIT = 1024 * 1024 * 1024;

export function getMaxImageSize(): number {
  return intWithin(nconf.get("ai:maxImageSize"), 1, MAX_IMAGE_SIZE_LIMIT) ?? DEFAULT_MAX_IMAGE_SIZE;
}

// Hard ceiling for a single round-trip to the AI service, the Files API or an MCP server, so
// that a hung upstream cannot pin a socket and the caller's request. Generous by default, as a
// tool call proxied through the .NET service can be slow. It bounds request/response calls
// only: a chat stream is bounded by the client's abort signal.
const DEFAULT_UPSTREAM_TIMEOUT_MS = 300_000;

/** `AI_CHAT_UPSTREAM_TIMEOUT_MS`: a second to an hour, in milliseconds. */
export function getUpstreamTimeoutMs(): number {
  return (
    intWithin(process.env["AI_CHAT_UPSTREAM_TIMEOUT_MS"], 1_000, 3_600_000) ??
    DEFAULT_UPSTREAM_TIMEOUT_MS
  );
}

// How often a streaming reply is saved while its round is in progress. The library's default
// (150 ms) is tuned for a local adapter; here every write is an HTTP hop.
const DEFAULT_PERSIST_INTERVAL_MS = 10_000;

/** `AI_CHAT_PERSIST_INTERVAL_MS`: 50 ms to ten minutes. */
export function getPersistIntervalMs(): number {
  return (
    intWithin(process.env["AI_CHAT_PERSIST_INTERVAL_MS"], 50, 600_000) ??
    DEFAULT_PERSIST_INTERVAL_MS
  );
}

/** How app/telemetry/sdk.ts starts OpenTelemetry, as {@link getTelemetrySettings} reads it. */
export interface TelemetrySettings {
  /** `openTelemetry:enable` is `true` or the string `"true"`; any other value is off. */
  enabled: boolean;
  /** The OTLP collector; undefined when it is absent, empty or not a string. */
  otlpEndpoint: string | undefined;
  /** `OTEL_EXPORTER_OTLP_PROTOCOL` trimmed; `http/protobuf` when absent, blank or not a string. */
  otlpProtocol: string;
  /** `openTelemetry:ServiceName`, then `OTEL_SERVICE_NAME`, then `ASC.AI.Chat`. */
  serviceName: string;
}

// The OpenTelemetry settings mirror the .NET contract: `openTelemetry` of the shared appsettings
// and the `OTEL_*` variables the Aspire AppHost injects. All of them go through nconf, so the
// environment comes first and the settings files after it, the `OTEL_*` names at the root of
// the settings tree. A value of the wrong type is no setting, so it cannot stop the host.
export function getTelemetrySettings(): TelemetrySettings {
  const enable: unknown = nconf.get("openTelemetry:enable");
  return {
    enabled: enable === true || enable === "true",
    otlpEndpoint: nonEmptyText(nconf.get("OTEL_EXPORTER_OTLP_ENDPOINT")),
    otlpProtocol: nonEmptyText(nconf.get("OTEL_EXPORTER_OTLP_PROTOCOL"))?.trim() || "http/protobuf",
    // Matches the .NET services' fallback style (an assembly-like name).
    serviceName:
      nonEmptyText(nconf.get("openTelemetry:ServiceName")) ??
      nonEmptyText(nconf.get("OTEL_SERVICE_NAME")) ??
      "ASC.AI.Chat",
  };
}

/** A string of at least one character, as given; anything else is undefined. */
function nonEmptyText(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

const DEFAULT_METRIC_EXPORT_INTERVAL_MS = 15_000;

/** `OTEL_METRIC_EXPORT_INTERVAL`: a second to an hour, in milliseconds. */
export function getMetricExportIntervalMs(): number {
  return (
    intWithin(process.env["OTEL_METRIC_EXPORT_INTERVAL"], 1_000, 3_600_000) ??
    DEFAULT_METRIC_EXPORT_INTERVAL_MS
  );
}

// The portal's own MCP server (docspace-mcp, the `ai.mcp` entry named
// below). It is always enabled everywhere — global chat, agents, the
// editor plugin — and its tools cannot be disabled: the agent whitelist
// always includes it (systemTools) and its tool-prefs "disabled" entries
// are ignored (toolPrefsStorage). The client hides it from the MCP
// management surfaces accordingly.
export const PORTAL_MCP_SERVER_NAME = "docspace";

// Group keys of the DocSpace integration tools (`httpToolsAdapter`). Defined
// here — not in the adapter — so the storage layer can alias the two groups
// in tool prefs without importing the adapter (which imports storage back).
// The engine gates approval per serverType, so approval-required tools are
// emitted under the dedicated `-approval` group; both groups are one logical
// source, and tool prefs treat them as one namespace (Bug 83013).
export const DOCSPACE_INTEGRATION_SERVER_TYPE = "docspace-integration";
export const DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE = "docspace-integration-approval";

// Group keys of the library's built-in tool sources (mirrors of the
// non-exported WEB_SEARCH_TYPE / IMAGE_GENERATION_TYPE constants in
// @onlyoffice/ai-chat core). Used to validate tool-pref serverTypes.
export const WEB_SEARCH_TYPE = "web-search";
export const IMAGE_GENERATION_TYPE = "image-generation";

// Group key of the document agent's single delegating tool
// (`document_agent`). Listed in `systemServerTypes`, so the engine asks the
// user before a task that may run for many minutes is started.
export const DOCUMENT_AGENT_TYPE = "document-agent";

export interface DocumentAgentConfig {
  url: string;
  token: string;
  /** The whole task, sandbox start to final answer; the agent's own ceiling still applies. */
  taskTimeoutSeconds: number;
  /** Tool calls the agent may make for one task; undefined leaves the agent's default. */
  maxToolCalls: number | undefined;
  /**
   * Whether the host-configured MCP servers of `ai.mcp` other than the portal's own are handed
   * to the agent with the caller's session. The portal's own server always goes: it is how the
   * agent reads the workspace. Their endpoints must be reachable from the agent: it refuses a
   * task whose server it cannot connect to.
   */
  shareSystemServers: boolean;
  /**
   * Where the agent reaches the portal API for the portal's own model provider (`onlyoffice`
   * profiles): the gateway proxy lives under it at `/api/2.0/ai/gateway`. Undefined means the
   * address this service itself uses, the AI service's own, which an agent on the same network
   * reaches; an agent elsewhere needs the public portal address here.
   */
  portalUrl: string | undefined;
  /**
   * Where the agent reaches this service, for the file-link MCP server handed to it (see
   * `documentAgentFiles.ts`). `http://localhost:<port>` by default, which an agent on the same
   * host reaches; an agent elsewhere needs this service's address on their shared network.
   */
  filesUrl: string;
}

const DOCUMENT_AGENT_DEFAULT_TIMEOUT_SECONDS = 1800;

// The document agent from the shared `appsettings.json` (`ai.documentAgent`),
// each field overridable from the environment in the same `__` form as the
// MCP endpoints (`AI__DOCUMENT_AGENT__URL`, `AI__DOCUMENT_AGENT__TOKEN`,
// `AI__DOCUMENT_AGENT__TASK_TIMEOUT_SECONDS`, `AI__DOCUMENT_AGENT__MAX_TOOL_CALLS`,
// `AI__DOCUMENT_AGENT__SHARE_SYSTEM_SERVERS`, `AI__DOCUMENT_AGENT__PORTAL_URL`,
// `AI__DOCUMENT_AGENT__FILES_URL`).
// Undefined without a URL and a token: the tool is then simply not offered.
// An environment variable that is set wins even when empty, so the Docker
// install's empty DOCUMENT_AGENT_URL / DOCUMENT_AGENT_TOKEN switch the agent
// off whatever the appsettings name.
export function getDocumentAgent(): DocumentAgentConfig | undefined {
  const ai: RootConfig["ai"] = nconf.get("ai");
  const setting: DocumentAgentSetting | undefined =
    ai?.documentAgent && typeof ai.documentAgent === "object" ? ai.documentAgent : undefined;
  const url = envOrSetting("AI__DOCUMENT_AGENT__URL", setting?.url);
  const token = envOrSetting("AI__DOCUMENT_AGENT__TOKEN", setting?.token);
  if (!url || !token) {
    return undefined;
  }
  const timeout =
    intWithin(
      process.env["AI__DOCUMENT_AGENT__TASK_TIMEOUT_SECONDS"],
      1,
      MAX_TASK_TIMEOUT_SECONDS,
    ) ??
    intWithin(setting?.taskTimeoutSeconds, 1, MAX_TASK_TIMEOUT_SECONDS) ??
    DOCUMENT_AGENT_DEFAULT_TIMEOUT_SECONDS;
  const maxToolCalls =
    intWithin(process.env["AI__DOCUMENT_AGENT__MAX_TOOL_CALLS"], 1, MAX_TOOL_CALLS_LIMIT) ??
    intWithin(setting?.maxToolCalls, 1, MAX_TOOL_CALLS_LIMIT);
  const share = process.env["AI__DOCUMENT_AGENT__SHARE_SYSTEM_SERVERS"];
  const shareSystemServers =
    share !== undefined && share !== ""
      ? share === "true" || share === "1"
      : setting?.shareSystemServers === true;
  const portalUrl = firstText(process.env["AI__DOCUMENT_AGENT__PORTAL_URL"], setting?.portalUrl);
  const filesUrl = firstText(process.env["AI__DOCUMENT_AGENT__FILES_URL"], setting?.filesUrl);
  return {
    url: url.replace(/\/+$/, ""),
    token,
    taskTimeoutSeconds: timeout,
    maxToolCalls,
    shareSystemServers,
    portalUrl: portalUrl === "" ? undefined : portalUrl.replace(/\/+$/, ""),
    filesUrl: (filesUrl === "" ? `http://localhost:${getAppConfig().port}` : filesUrl).replace(
      /\/+$/,
      "",
    ),
  };
}

/** The first value that is a non-blank string, trimmed; "" when there is none. */
function firstText(...values: unknown[]): string {
  for (const value of values) {
    if (typeof value === "string" && value.trim() !== "") {
      return value.trim();
    }
  }
  return "";
}

/** The environment variable when it is set at all (even empty), else the setting; trimmed. */
function envOrSetting(name: string, setting: unknown): string {
  const env = process.env[name];
  const value = env !== undefined ? env : setting;
  return typeof value === "string" ? value.trim() : "";
}

// The upper bounds of the numbers below: a value above one is a slip of the keyboard, and the
// default serves better than a day-long timeout or a context window no model has.
const MAX_TASK_TIMEOUT_SECONDS = 24 * 60 * 60;
const MAX_TOOL_CALLS_LIMIT = 10_000;
const MAX_KEEP_RECENT = 10_000;
const MAX_CONTEXT_WINDOW = 100_000_000;

/**
 * A number given as a number or as its text. Blank text is no number: `Number("")` is 0, so an
 * environment variable exported empty read as a setting of 0 and hid the file's value.
 */
function numberOf(value: unknown): number {
  if (typeof value === "number") {
    return value;
  }
  return typeof value === "string" && value.trim() !== "" ? Number(value) : NaN;
}

/** A whole number within `[min, max]`, given as a number or as its text; anything else is not a setting. */
function intWithin(value: unknown, min: number, max: number): number | undefined {
  const n = numberOf(value);
  return Number.isInteger(n) && n >= min && n <= max ? n : undefined;
}

/** The library's `CompactionOptions`, as the host resolves them. */
export interface CompactionConfig {
  enabled: boolean;
  threshold?: number;
  keepRecent?: number;
  defaultContextWindow?: number;
}

// Automatic history compaction from the shared `appsettings.json`
// (`ai.compaction`), each field overridable from the environment
// (`AI__COMPACTION__ENABLED`, `AI__COMPACTION__THRESHOLD`,
// `AI__COMPACTION__KEEP_RECENT`, `AI__COMPACTION__DEFAULT_CONTEXT_WINDOW`).
// On unless switched off (`false` / `0`); a knob that is absent or
// malformed leaves the library's own default. Read at request time so a changed setting needs
// no restart of the engine object.
export function getCompaction(): CompactionConfig {
  const ai: RootConfig["ai"] = nconf.get("ai");
  const setting: CompactionSetting | undefined =
    ai?.compaction && typeof ai.compaction === "object" ? ai.compaction : undefined;
  const enabled = flag(process.env["AI__COMPACTION__ENABLED"]) ?? setting?.enabled !== false;
  const threshold =
    unitFraction(process.env["AI__COMPACTION__THRESHOLD"]) ?? unitFraction(setting?.threshold);
  const keepRecent =
    intWithin(process.env["AI__COMPACTION__KEEP_RECENT"], 0, MAX_KEEP_RECENT) ??
    intWithin(setting?.keepRecent, 0, MAX_KEEP_RECENT);
  const defaultContextWindow =
    intWithin(process.env["AI__COMPACTION__DEFAULT_CONTEXT_WINDOW"], 1, MAX_CONTEXT_WINDOW) ??
    intWithin(setting?.defaultContextWindow, 1, MAX_CONTEXT_WINDOW);
  return {
    enabled,
    ...(threshold !== undefined ? { threshold } : {}),
    ...(keepRecent !== undefined ? { keepRecent } : {}),
    ...(defaultContextWindow !== undefined ? { defaultContextWindow } : {}),
  };
}

/** `true` / `1` or `false` / `0`, any case; anything else is not a setting. */
function flag(value: string | undefined): boolean | undefined {
  const v = value?.trim().toLowerCase();
  if (v === "true" || v === "1") return true;
  if (v === "false" || v === "0") return false;
  return undefined;
}

/** A number in (0, 1], from a number or a numeric string. */
function unitFraction(value: unknown): number | undefined {
  const n = numberOf(value);
  return Number.isFinite(n) && n > 0 && n <= 1 ? n : undefined;
}

// Group key for the form-analysis sub-agent's single delegating tool
// (`analyze_form`). Kept out of `systemServerTypes` so the tool runs silently.
export const FORM_ANALYSIS_TYPE = "form-analysis";

// Host-preconfigured MCP servers from the shared `appsettings.json`
// (`ai.mcp`), with the Aspire endpoint override applied per entry.
// Malformed entries (missing id / name / endpoint) are dropped so a bad
// config can't crash startup.
export function getMcpServers(): McpServerSetting[] {
  const ai: RootConfig["ai"] = nconf.get("ai");
  const mcp = ai?.mcp;
  if (!Array.isArray(mcp)) {
    if (mcp != null) {
      console.warn(
        "ai.mcp is not an array — ignoring (a ':'-keyed env var may have nested over it; use AI__MCP__<i>__ENDPOINT)",
      );
    }
    return [];
  }
  const servers: McpServerSetting[] = [];
  mcp.forEach((s, index) => {
    if (typeof s?.id !== "string" || typeof s?.name !== "string" || s.name.length === 0) {
      return;
    }
    const endpoint = mcpEndpointOverride(index) ?? s.endpoint;
    if (typeof endpoint !== "string" || endpoint.length === 0) {
      return;
    }
    servers.push({ id: s.id, name: s.name, endpoint });
  });
  return servers;
}

function getAndSaveAppsettings(): void {
  const app: AppConfig = nconf.get("app");
  let appsettings = app.appsettings;
  if (!path.isAbsolute(appsettings)) {
    appsettings = path.join(__dirname, appsettings);
  }
  const env = app.environment;
  console.log("environment: " + env);

  nconf.file("appsettingsWithEnv", path.join(appsettings, "appsettings." + env + ".json"));
  nconf.file("appsettings", path.join(appsettings, "appsettings.json"));
  nconf.file("appsettingsServices", path.join(appsettings, "appsettings.services.json"));
}
