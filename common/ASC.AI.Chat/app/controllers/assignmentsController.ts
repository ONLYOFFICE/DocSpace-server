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

import { AssignmentsEngine, ActionType } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { asActionType, asString, isObject } from "../narrow.js";
import { assertEntityAccessible } from "../storage/docspaceFilesApi.js";

const engine = new AssignmentsEngine({ storage });

// The engine's lenient lookup still throws a plain Error when the slot
// names a profile that can no longer be loaded - deleted without the
// assignment cascade, or one the caller cannot see. For the caller that is
// the same as an empty slot, not a server failure, so both resolve
// operations answer it as one (404 and an empty result respectively)
// instead of a 500.
async function resolveOrNull(
  actionType: ActionType,
  entityId: string | undefined,
): Promise<Awaited<ReturnType<AssignmentsEngine["tryResolveForAction"]>>> {
  try {
    return await engine.tryResolveForAction(actionType, entityId);
  } catch (err) {
    if (err instanceof Error && err.message.startsWith("Assigned profile not found")) {
      return null;
    }
    throw err;
  }
}

interface AssignBody {
  actionType?: string;
  profileId?: string;
}

interface UnassignBody {
  actionType?: string;
}

interface ProfileIdBody {
  profileId?: string;
}

interface BulkAssignBody {
  [key: string]: string | undefined;
}

export const assignmentsController = {
  resolveForAction: asyncHandler(async (req, res) => {
    const actionType = asActionType(req.query["actionType"]);
    if (!actionType) {
      res.status(400).json({ error: "actionType required" });
      return;
    }
    const entityId = asString(req.query["entityId"]);
    // A nonexistent room used to fold to the portal-wide lookup and answer
    // its profile with 200; get-all-assignments already 404s it (Bug 82832),
    // and so do both resolve operations now (Bug 84279). An accessible
    // non-agent room still degrades to the portal scope by design.
    await assertEntityAccessible(entityId);
    // The engine's throwing variant reports "nothing configured" with a
    // plain Error, which read as a 500 (Bugs 84278, 84450); the lenient
    // variant answers null for the same states (and `resolveOrNull` for an
    // assigned profile that is gone), and that is a 404 here — the status
    // threads/create gives the same condition.
    const result = await resolveOrNull(actionType, entityId);
    if (result === null) {
      res.status(404).json({ error: `no AI profile is available for ActionType.${actionType}` });
      return;
    }
    res.json(result);
  }),

  tryResolveForAction: asyncHandler(async (req, res) => {
    const actionType = asActionType(req.query["actionType"]);
    if (!actionType) {
      res.status(400).json({ error: "actionType required" });
      return;
    }
    const entityId = asString(req.query["entityId"]);
    await assertEntityAccessible(entityId);
    const result = await resolveOrNull(actionType, entityId);
    res.json(result);
  }),

  assign: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["actionType", "profileId"] as const);
    const actionType = asActionType(args.actionType);
    const profileId = typeof args.profileId === "string" ? args.profileId : undefined;
    if (!actionType || !profileId) {
      res.status(400).json({ error: "actionType and profileId required" });
      return;
    }
    const result = await engine.assign(actionType, profileId);
    res.json(result);
  }),

  unassign: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["actionType"] as const);
    const raw = args.actionType ?? asString(req.query["actionType"]);
    const actionType = asActionType(raw);
    if (!actionType) {
      res.status(400).json({ error: "actionType required" });
      return;
    }
    await engine.unassign(actionType);
    res.json({ success: true });
  }),

  bulkAssign: asyncHandler<BulkAssignBody>(async (req, res) => {
    // Validate the whole body up front: an unknown ActionType key or a
    // non-string value used to be silently dropped, so a caller that mistyped
    // an action or sent a bad profileId got a partial (or empty) assignment
    // under a 200 with no signal anything was wrong (Bug 82831). Reject with a
    // descriptive 400 instead of building a map from only the valid entries.
    const body = req.body;
    if (!isObject(body)) {
      res.status(400).json({
        error: "body must be an object mapping actionType to profileId",
      });
      return;
    }
    const map: Partial<Record<ActionType, string>> = {};
    for (const [k, v] of Object.entries(body)) {
      const actionType = asActionType(k);
      if (!actionType) {
        res.status(400).json({ error: `invalid actionType "${k}"` });
        return;
      }
      if (typeof v !== "string") {
        res.status(400).json({
          error: `profileId for actionType "${k}" must be a string`,
        });
        return;
      }
      map[actionType] = v;
    }
    const result = await engine.bulkAssign(map);
    res.json(result);
  }),

  getAssignment: asyncHandler(async (req, res) => {
    const actionType = asActionType(req.query["actionType"]);
    if (!actionType) {
      res.status(400).json({ error: "actionType required" });
      return;
    }
    const profileId = await engine.getAssignment(actionType);
    res.json(profileId);
  }),

  getAllAssignments: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    // Read-side gating (Bug 82832): a supplied entityId must reference a folder
    // the caller can access — a nonexistent/no-access id 404s. But a valid
    // *non-agent* folder is fine: it falls through and assignmentsStorage
    // degrades the read to the global scope (review #2), so the model
    // assignment still loads in ordinary rooms and My Documents. An
    // absent/empty entityId stays the global scope.
    await assertEntityAccessible(entityId);
    const result = await engine.getAllAssignments(entityId);
    res.json(result);
  }),

  cascadeProfileDelete: asyncHandler<ProfileIdBody>(async (req, res) => {
    const profileId = req.body?.profileId ?? asString(req.query["profileId"]);
    if (!profileId) {
      res.status(400).json({ error: "profileId required" });
      return;
    }
    await engine.cascadeProfileDelete(profileId);
    res.json({ success: true });
  }),
};
