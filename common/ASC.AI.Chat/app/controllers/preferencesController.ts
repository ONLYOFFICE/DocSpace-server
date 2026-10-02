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
  isToolPermissionMode,
  PreferencesEngine,
  TOOL_PERMISSION_MODES,
} from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { asString } from "../narrow.js";
import { isReasoningLevel, REASONING_LEVELS } from "../storage/reasoningDepth.js";

const engine = new PreferencesEngine({ storage });

// The scope a preference is written to. Preferences are keyed on a folder
// (an agent room; any other accessible folder folds to the portal-wide
// scope downstream), and folder ids are integers on the .NET side — so
// anything else names no scope at all. A thread id in particular used to
// fold to the portal-wide preference and answer `{ success: true }` for a
// write that changed nothing the caller could observe (Bug 84026); it is
// refused with 400 instead. An absent value is the portal-wide scope. Reads
// stay lenient: an unknown scope reads as the portal-wide fallback.
function writeScope(value: unknown): string | undefined {
  if (value === undefined || value === null || value === "") {
    return undefined;
  }
  if (typeof value === "number" && Number.isInteger(value) && value > 0) {
    return String(value);
  }
  if (typeof value === "string" && /^\d+$/.test(value)) {
    return value;
  }
  throw Object.assign(new Error("entityId must be a room id (a positive integer)"), {
    status: 400,
    expose: true,
  });
}

export const preferencesController = {
  getDeepMode: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const value = await engine.getDeepMode(entityId);
    res.json(value);
  }),

  setDeepMode: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["value", "entityId"] as const);
    // Require a real boolean. `Boolean(args.value)` mis-handled non-booleans:
    // the string "false" coerced to `true` (Bug 82813), and an absent value
    // coerced to `false`, silently overwriting the stored setting on an empty
    // request (Bug 82814). Reject anything that is not a boolean with a 400.
    if (typeof args.value !== "boolean") {
      res.status(400).json({ error: "value is required and must be a boolean" });
      return;
    }
    const entityId = writeScope(args.entityId);
    await engine.setDeepMode(args.value, entityId);
    res.json({ success: true });
  }),

  clearDeepMode: asyncHandler(async (req, res) => {
    const { entityId } = unpackPositional(req.body, ["entityId"] as const);
    await engine.clearDeepMode(writeScope(entityId));
    res.json({ success: true });
  }),

  isDeepModeSet: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const value = await engine.isDeepModeSet(entityId);
    res.json(value);
  }),

  // Extended-thinking depth. Deep mode above stays the widget's fallback when
  // these two are unavailable; both are views of the ONE `depth` value the C#
  // storage keeps (see `storage/preferencesStorage.ts`). The engine's
  // reasoning-level methods are built for a host with two values — its
  // `setReasoningLevel` writes the toggle first (a read plus a write here)
  // and the depth after, so a concurrent round could observe the interim
  // `medium`, and its `getReasoningLevel` reads twice. With a single value
  // the storage answers both in one call with identical semantics: a
  // missing row is `off` (the configured deep-mode default is off).
  getReasoningLevel: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const value = (await storage.preferences.readReasoningLevel?.(entityId)) ?? "off";
    res.json(value);
  }),

  setReasoningLevel: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["value", "entityId"] as const);
    // Same discipline as `setDeepMode`: only a real level is accepted, so an
    // absent or mistyped value can never overwrite the stored depth.
    if (!isReasoningLevel(args.value)) {
      res.status(400).json({
        error: `value is required and must be one of: ${REASONING_LEVELS.join(", ")}`,
      });
      return;
    }
    const entityId = writeScope(args.entityId);
    await storage.preferences.upsertReasoningLevel?.(args.value, entityId);
    res.json({ success: true });
  }),

  // How a tool call is approved — the composer's "Permissions" row. Stored
  // next to the depth in the same C# row; the engine answers the default
  // (`ask`) for a scope with nothing stored.
  getToolPermissionMode: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const value = await engine.getToolPermissionMode(entityId);
    res.json(value);
  }),

  setToolPermissionMode: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["value", "entityId"] as const);
    // Same discipline as `setDeepMode`: only a real mode is accepted, so an
    // absent or mistyped value can never overwrite the stored mode.
    if (!isToolPermissionMode(args.value)) {
      res.status(400).json({
        error: `value is required and must be one of: ${TOOL_PERMISSION_MODES.join(", ")}`,
      });
      return;
    }
    const entityId = writeScope(args.entityId);
    await engine.setToolPermissionMode(args.value, entityId);
    res.json({ success: true });
  }),
};
