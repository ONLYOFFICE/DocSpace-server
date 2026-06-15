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

import { ActionType } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { docSpaceApi } from "../storage/httpClient.js";
import type { QueryValue } from "../storage/httpClient.js";
import { asyncHandler } from "./_helpers.js";
import { isObject, getString, getNumber, getObject } from "../narrow.js";
import type { JsonObject } from "../narrow.js";

// The agent's profile is stored as an assignment scoped to the agent's
// entry id. `Chat` is the action an agent room serves; fall back to
// `Default` if the engine package ever drops `Chat`.
const AGENT_ACTION_TYPE: ActionType = ActionType.Chat ?? ActionType.Default;

// `profileId` is a profile UUID (`Profile.id: string` in
// @onlyoffice/ai-chat; a `Guid` on the .NET side).
const UUID_PATTERN = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

function badRequest(message: string): never {
  throw Object.assign(new Error(message), { status: 400, expose: true });
}

// Agent ids are int folder ids on the .NET side (`RoomIdRequestDto<int>`).
// Reject anything non-integer up front with a clean 400 instead of letting
// the upstream model-binder fail opaquely.
function agentIdParam(value: string | undefined): string {
  if (!value || !/^\d+$/.test(value)) {
    badRequest("agent id must be a positive integer");
  }
  return value;
}

// Forward the caller's flat string query params to the .NET endpoint
// unchanged. Array/object query values (not used by the agent list API)
// are dropped rather than guessed at.
function forwardQuery(query: Record<string, unknown>): Record<string, QueryValue> {
  const out: Record<string, QueryValue> = {};
  for (const [key, value] of Object.entries(query)) {
    if (typeof value === "string") {
      out[key] = value;
    }
  }
  return out;
}

interface CreateAgentBody {
  profileId?: string;
  prompt?: string;
}

export const agentsController = {
  // POST agents — creates an AI agent. Creation is delegated to the .NET
  // endpoint `POST api/2.0/ai/agents` (AgentsController.CreateAgent). The
  // caller's body is forwarded as-is except that `profileId` and `prompt`
  // are stripped. The profile is then bound to the created agent via an
  // assignment (profileId + the agent's entry id); a binding failure is an
  // error for the caller even though the agent room already exists.
  createAgent: asyncHandler<CreateAgentBody>(async (req, res) => {
    const body: JsonObject = isObject(req.body) ? req.body : {};

    const profileId = getString(body, "profileId");
    if (profileId === undefined) {
      badRequest("profileId is required and must be a string");
    }
    if (!UUID_PATTERN.test(profileId)) {
      badRequest("profileId must be a UUID");
    }

    const prompt = getString(body, "prompt");
    if (prompt === undefined) {
      badRequest("prompt is required and must be a string");
    }

    const { profileId: _profileId, prompt: _prompt, ...rest } = body;
    // The agent's model comes from the assigned profile, so only the prompt
    // is stored on the room via a prompt-only `chatSettings` (accepted by
    // FileStorageService.ValidateChatSettingsAsync). `raw` keeps the
    // DocSpace `{ response, status, ... }` envelope so the result is
    // returned to the client in the same shape as a direct .NET call; the
    // agent id is read out of `response` for the assignment.
    const envelope = await docSpaceApi.post(
      "/ai/agents",
      { ...rest, chatSettings: { prompt } },
      { raw: true },
    );
    const created = isObject(envelope) ? getObject(envelope, "response") : undefined;
    const agentId = created ? getNumber(created, "id") : undefined;
    if (agentId === undefined) {
      throw new Error("agent created, but the AI service returned no agent id");
    }

    await storage.assignments.create(AGENT_ACTION_TYPE, profileId, String(agentId));

    res.json(envelope);
  }),

  // GET agents — lists AI agents. Query params are forwarded as-is to
  // `GET api/2.0/ai/agents` (AgentsController.GetAgents), which reads them
  // via [FromQuery]. Returns the upstream FolderContentDto.
  getAgents: asyncHandler(async (req, res) => {
    const content = await docSpaceApi.get("/ai/agents", {
      query: forwardQuery(req.query),
      raw: true,
    });
    res.json(content);
  }),

  // GET agents/news — returns the agents' new items
  // (`GET api/2.0/ai/agents/news`).
  getAgentsNews: asyncHandler(async (_req, res) => {
    const news = await docSpaceApi.get("/ai/agents/news", { raw: true });
    res.json(news);
  }),

  // GET agents/{id} — returns a single agent
  // (`GET api/2.0/ai/agents/{id}`).
  getAgentInfo: asyncHandler(async (req, res) => {
    const id = agentIdParam(req.params["id"]);
    const envelope = await docSpaceApi.get(`/ai/agents/${id}`, { raw: true });

    // Enrich the agent with its assigned profile so the edit dialog can
    // prefill the profile selector: the binding lives in the assignment
    // (scoped to the agent's entry id), not on the room itself. The prompt
    // is already on the room (`chatSettings.prompt`). A missing assignment
    // or a resolve failure simply leaves `profileId` absent.
    const response = isObject(envelope) ? getObject(envelope, "response") : undefined;
    if (response) {
      const profileId = await storage.assignments
        .readByType(AGENT_ACTION_TYPE, id)
        .catch(() => null);
      if (profileId) {
        response["profileId"] = profileId;
      }
    }

    res.json(envelope);
  }),

  // PUT agents/{id} — updates an agent. The body (UpdateRoomRequest) is
  // forwarded as-is to `PUT api/2.0/ai/agents/{id}`. `chatSettings` is the
  // caller's responsibility here: the upstream still requires a valid
  // providerId/modelId when chatSettings is present.
  updateAgent: asyncHandler(async (req, res) => {
    const id = agentIdParam(req.params["id"]);
    const body: JsonObject = isObject(req.body) ? req.body : {};

    // `profileId` is not part of the room contract: strip it from the
    // forwarded body and, if present, re-bind the agent's profile after the
    // room update. Everything else (title, tags, chatSettings.prompt, …) is
    // forwarded as-is.
    const profileId = getString(body, "profileId");
    if (profileId !== undefined && !UUID_PATTERN.test(profileId)) {
      badRequest("profileId must be a UUID");
    }
    const { profileId: _profileId, ...rest } = body;

    const agent = await docSpaceApi.put(`/ai/agents/${id}`, rest, { raw: true });

    if (profileId !== undefined) {
      const existing = await storage.assignments
        .readByType(AGENT_ACTION_TYPE, id)
        .catch(() => null);
      if (existing) {
        await storage.assignments.update(AGENT_ACTION_TYPE, profileId, id);
      } else {
        await storage.assignments.create(AGENT_ACTION_TYPE, profileId, id);
      }
    }

    res.json(agent);
  }),

  // DELETE agents/{id} — removes an agent. The body (DeleteRoomRequest,
  // e.g. `{ deleteAfter }`) is forwarded to `DELETE api/2.0/ai/agents/{id}`.
  // The per-agent profile assignment is intentionally left untouched: the
  // .NET assignment API exposes no per-entry delete, so cleanup of orphaned
  // assignment rows is out of scope here.
  deleteAgent: asyncHandler(async (req, res) => {
    const id = agentIdParam(req.params["id"]);
    const operation = await docSpaceApi.delete(`/ai/agents/${id}`, req.body ?? {}, { raw: true });
    res.json(operation);
  }),

  // PUT agents/agentquota — changes the quota for the given agents
  // (`PUT api/2.0/ai/agents/agentquota`, body `{ roomIds, quota }`).
  updateAgentsQuota: asyncHandler(async (req, res) => {
    const result = await docSpaceApi.put("/ai/agents/agentquota", req.body ?? {}, { raw: true });
    res.json(result);
  }),

  // PUT agents/resetquota — resets the quota for the given agents
  // (`PUT api/2.0/ai/agents/resetquota`, body `{ roomIds }`).
  resetAgentsQuota: asyncHandler(async (req, res) => {
    const result = await docSpaceApi.put("/ai/agents/resetquota", req.body ?? {}, { raw: true });
    res.json(result);
  }),
};
