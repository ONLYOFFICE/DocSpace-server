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

import { ActionType } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { resolveAgentEntityId } from "../storage/docspaceFilesApi.js";

// Mirrors agentsController: the agent's profile is stored as a Chat
// assignment scoped to the agent's entry id.
const AGENT_ACTION_TYPE: ActionType = ActionType.Chat ?? ActionType.Default;

/**
 * The profile assigned to the agent the given scope points at, or null when
 * the scope is absent, is not an agent room, or the agent has no resolvable
 * assignment (e.g. its profile was disabled).
 *
 * The agent's assigned profile is authoritative for every round and thread
 * in its scope: callers substitute it over any client-supplied profileId,
 * so a request cannot run an agent's chat on a different model
 * (Bug 82914 / Bug 82915).
 */
export async function agentAssignedProfileId(
  scope: string | undefined,
): Promise<string | null> {
  // Resolve agent-ness explicitly: readByType would silently fall back to
  // the GLOBAL assignment for a non-agent scope, which must not override
  // anything here.
  const agentId = await resolveAgentEntityId(scope);
  if (!agentId) {
    return null;
  }
  return storage.assignments
    .readByType(AGENT_ACTION_TYPE, agentId)
    .catch(() => null);
}
