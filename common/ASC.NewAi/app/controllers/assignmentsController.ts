// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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

  getAllAssignments: asyncHandler(async (_req, res) => {
    const result = await engine.getAllAssignments();
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
