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
import { asString } from "../narrow.js";

const engine = new AssignmentsEngine({ storage });

const ACTION_TYPES = new Set<string>(Object.values(ActionType));

function asActionType(value: unknown): ActionType | undefined {
  if (typeof value !== "string" || !ACTION_TYPES.has(value)) {
    return undefined;
  }
  return value as ActionType;
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
    const result = await engine.resolveForAction(actionType, entityId);
    res.json(result);
  }),

  tryResolveForAction: asyncHandler(async (req, res) => {
    const actionType = asActionType(req.query["actionType"]);
    if (!actionType) {
      res.status(400).json({ error: "actionType required" });
      return;
    }
    const entityId = asString(req.query["entityId"]);
    const result = await engine.tryResolveForAction(actionType, entityId);
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
    const map: Partial<Record<ActionType, string>> = {};
    for (const [k, v] of Object.entries(req.body ?? {})) {
      const actionType = asActionType(k);
      if (actionType && typeof v === "string") {
        map[actionType] = v;
      }
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
