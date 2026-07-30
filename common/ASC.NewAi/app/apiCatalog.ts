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

import {
  DEFAULT_AI_ROUTES,
  DEFAULT_ASSIGNMENTS_ROUTES,
  DEFAULT_ATTACHMENTS_ROUTES,
  DEFAULT_PREFERENCES_ROUTES,
  DEFAULT_PROFILES_ROUTES,
  DEFAULT_PROMPTS_ROUTES,
  DEFAULT_THREADS_ROUTES,
  DEFAULT_TOOLS_ROUTES,
  DEFAULT_WEB_SEARCH_ROUTES,
} from "@onlyoffice/ai-chat/core";
import type { EngineDoc, CustomRouteDoc } from "./openapi.js";

// Declarative catalog of the service's HTTP surface, decoupled from the
// Express app. `routes.ts` consumes it to register handlers; the build-time
// OpenAPI emitter (`scripts/generate-openapi.ts`) consumes the very same
// data to produce the document — so the two cannot drift. Kept free of app,
// config and controller imports so the emitter stays a pure, offline build
// step (no storage/appsettings needed).

// Engine groups backed by an `@onlyoffice/ai-chat` service. `name` is the
// controller key used in `routes.ts`; `tag`/`description` drive the docs.
export const ENGINE_DOCS: ReadonlyArray<EngineDoc> = [
  { name: "ai", tag: "AI", description: "Chat completions and tool-call approval.", routes: DEFAULT_AI_ROUTES },
  { name: "assignments", tag: "Assignments", description: "Profile-to-entity assignment resolution.", routes: DEFAULT_ASSIGNMENTS_ROUTES },
  { name: "attachments", tag: "Attachments", description: "Message file and image attachments.", routes: DEFAULT_ATTACHMENTS_ROUTES },
  { name: "preferences", tag: "Preferences", description: "Per-entity chat preferences (e.g. deep mode).", routes: DEFAULT_PREFERENCES_ROUTES },
  { name: "profiles", tag: "Profiles", description: "AI provider profiles and model discovery.", routes: DEFAULT_PROFILES_ROUTES },
  { name: "prompts", tag: "Prompts", description: "Saved prompts and prompt folders.", routes: DEFAULT_PROMPTS_ROUTES },
  { name: "threads", tag: "Threads", description: "Chat threads and their messages.", routes: DEFAULT_THREADS_ROUTES },
  { name: "tools", tag: "Tools", description: "MCP custom servers and system tool preferences.", routes: DEFAULT_TOOLS_ROUTES },
  { name: "webSearch", tag: "Web search", description: "Web-search provider configuration.", routes: DEFAULT_WEB_SEARCH_ROUTES },
];

// Routes registered explicitly in `routes.ts` that are not backed by an
// engine. Kept in sync by hand with those `router.<verb>(...)` calls.
// `operationId`s are lowerCamelCase and `newAi`-scoped so they never clash
// with the .NET AI service's ids (e.g. its own `getAgents`) once merged.
export const CUSTOM_ROUTE_DOCS: ReadonlyArray<CustomRouteDoc> = [
  { method: "POST", path: "/text-to-docx", tag: "Export", operationId: "newAiExportTextToDocx", summary: "Start markdown → docx export", hasBody: true },
  { method: "GET", path: "/agents", tag: "Agents", operationId: "newAiAgentsList", summary: "List agents" },
  { method: "POST", path: "/agents", tag: "Agents", operationId: "newAiAgentsCreate", summary: "Create an agent", hasBody: true },
  { method: "GET", path: "/agents/news", tag: "Agents", operationId: "newAiAgentsNews", summary: "List agent news items" },
  { method: "GET", path: "/agents/{id}", tag: "Agents", operationId: "newAiAgentsGet", summary: "Get an agent", pathParams: ["id"] },
  { method: "PUT", path: "/agents/{id}", tag: "Agents", operationId: "newAiAgentsUpdate", summary: "Update an agent", pathParams: ["id"], hasBody: true },
  { method: "DELETE", path: "/agents/{id}", tag: "Agents", operationId: "newAiAgentsDelete", summary: "Delete an agent", pathParams: ["id"], hasBody: true },
  { method: "PUT", path: "/agents/agentquota", tag: "Agents", operationId: "newAiAgentsUpdateQuota", summary: "Update agents' quota", hasBody: true },
  { method: "PUT", path: "/agents/resetquota", tag: "Agents", operationId: "newAiAgentsResetQuota", summary: "Reset agents' quota", hasBody: true },
  { method: "GET", path: "/config", tag: "Settings", operationId: "newAiSettingsGet", summary: "Get AI settings" },
  { method: "GET", path: "/config/vectorization", tag: "Settings", operationId: "newAiSettingsGetVectorization", summary: "Get vectorization settings" },
  { method: "PUT", path: "/config/vectorization", tag: "Settings", operationId: "newAiSettingsSetVectorization", summary: "Update vectorization settings", hasBody: true },
  { method: "GET", path: "/config/user", tag: "Settings", operationId: "newAiSettingsGetUser", summary: "Get user AI settings" },
  { method: "PUT", path: "/config/user", tag: "Settings", operationId: "newAiSettingsSetUser", summary: "Update user AI settings", hasBody: true },
  { method: "POST", path: "/vectorization/tasks", tag: "Vectorization", operationId: "newAiVectorizationStartTask", summary: "Start a vectorization task", hasBody: true },
];

// Base path the service is mounted under (the DocSpace nginx route). Shared
// by the router registration and the emitted document's absolute paths.
export const API_PREFIX = "/api/2.0/ai";
