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

import { ContextEngine } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { asyncHandler } from "./_helpers.js";
import { asString } from "../narrow.js";

// The widget's `context` engine: the rooms a host offers as chat context and
// the skills inside them. The client connects the room the user is in when
// the chat opens; the picker lists every other room with a `.ai` folder the
// caller can read. All three reads are backed by the DocSpace Files API
// (see `storage/roomSkills.ts`).
const engine = new ContextEngine({ storage });

export const contextController = {
  getContextFolders: asyncHandler(async (_req, res) => {
    res.json(await engine.getContextFolders());
  }),

  getRoomSkills: asyncHandler(async (req, res) => {
    const cloud = asString(req.query["cloud"]) ?? "";
    const roomId = asString(req.query["roomId"]);
    if (!roomId) {
      res.status(400).json({ error: "roomId required" });
      return;
    }
    res.json(await engine.getRoomSkills(cloud, roomId));
  }),

  getRoomSkill: asyncHandler(async (req, res) => {
    const cloud = asString(req.query["cloud"]) ?? "";
    const roomId = asString(req.query["roomId"]);
    const skillId = asString(req.query["skillId"]);
    if (!roomId || !skillId) {
      res.status(400).json({ error: "roomId and skillId required" });
      return;
    }
    res.json(await engine.getRoomSkill(cloud, roomId, skillId));
  }),
};
