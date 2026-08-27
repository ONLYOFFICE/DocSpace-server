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
import type { AppConfig, McpServerSetting, RootConfig } from "../app/types.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

nconf.argv()
    .env()
    .file("config", path.join(__dirname, "config.json"));

const nodeEnv: string | undefined = nconf.get("NODE_ENV");
console.log("NODE_ENV: " + nodeEnv);

if (nodeEnv && nodeEnv !== "development" && fs.existsSync(path.join(__dirname, nodeEnv + ".json"))) {
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
export const DOCSPACE_INTEGRATION_APPROVAL_SERVER_TYPE =
  "docspace-integration-approval";

// Group keys of the library's built-in tool sources (mirrors of the
// non-exported WEB_SEARCH_TYPE / IMAGE_GENERATION_TYPE constants in
// @onlyoffice/ai-chat core). Used to validate tool-pref serverTypes.
export const WEB_SEARCH_TYPE = "web-search";
export const IMAGE_GENERATION_TYPE = "image-generation";

// Host-preconfigured MCP servers from the shared `appsettings.json`
// (`ai.mcp`), with the Aspire endpoint override applied per entry.
// Malformed entries (missing id / name / endpoint) are dropped so a bad
// config can't crash startup.
export function getMcpServers(): McpServerSetting[] {
    const ai: RootConfig["ai"] = nconf.get("ai");
    const mcp = ai?.mcp;
    if (!Array.isArray(mcp)) {
        if (mcp != null) {
            console.warn("ai.mcp is not an array — ignoring (a ':'-keyed env var may have nested over it; use AI__MCP__<i>__ENDPOINT)");
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
