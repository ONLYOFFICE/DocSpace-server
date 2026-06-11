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
import { asyncHandler } from "./_helpers.js";
import { isObject, getString, getNumber } from "../narrow.js";
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
    // `chatSettings` is NOT forwarded for now: FileStorageService rejects
    // chatSettings without a valid providerId/modelId (ArgumentException
    // "ProviderId"), and the agent's model comes from the assigned profile
    // instead. Restore `chatSettings: { prompt }` in the payload below once
    // the .NET side accepts a prompt-only chatSettings.
    void prompt;
    const agent = await docSpaceApi.post("/ai/agents", {
      ...rest,
    });

    const agentId = isObject(agent) ? getNumber(agent, "id") : undefined;
    if (agentId === undefined) {
      throw new Error("agent created, but the AI service returned no agent id");
    }

    await storage.assignments.create(AGENT_ACTION_TYPE, profileId, String(agentId));

    res.json(agent);
  }),
};
