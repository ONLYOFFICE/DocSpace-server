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

import { PromptsEngine } from "@onlyoffice/ai-chat/core";
import type { CreatePromptInput, PromptBundle } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { asyncHandler } from "./_helpers.js";
import { asString } from "../narrow.js";

const engine = new PromptsEngine({ storage });

interface UpdateBody {
  id: string;
  name?: string;
  text?: string;
  folderId?: string | null;
}

interface MoveBody {
  id: string;
  folderId?: string | null;
}

interface IdBody {
  id?: string;
}

interface CreateFolderBody {
  name: string;
}

interface RenameFolderBody {
  id: string;
  name: string;
}

interface ImportBody {
  bundle: PromptBundle;
  mode?: "merge" | "replace";
}

export const promptsController = {
  create: asyncHandler<CreatePromptInput>(async (req, res) => {
    const result = await engine.create(req.body);
    res.json(result);
  }),

  update: asyncHandler<UpdateBody>(async (req, res) => {
    const { id, ...updates } = req.body;
    const result = await engine.update(id, updates);
    res.json(result);
  }),

  move: asyncHandler<MoveBody>(async (req, res) => {
    const { id, folderId } = req.body;
    const result = await engine.move(id, folderId ?? null);
    res.json(result);
  }),

  delete: asyncHandler<IdBody>(async (req, res) => {
    const id = req.body?.id ?? asString(req.query["id"]);
    if (!id) {
      res.status(400).json({ error: "id required" });
      return;
    }
    await engine.delete(id);
    res.json({ success: true });
  }),

  list: asyncHandler(async (req, res) => {
    if (!("folderId" in req.query)) {
      const result = await engine.list();
      res.json(result);
      return;
    }
    const folderIdRaw = req.query["folderId"];
    const folderId = folderIdRaw === "" || folderIdRaw === undefined
      ? null
      : asString(folderIdRaw) ?? null;
    const result = await engine.list(folderId);
    res.json(result);
  }),

  createFolder: asyncHandler<CreateFolderBody>(async (req, res) => {
    const result = await engine.createFolder(req.body.name);
    res.json(result);
  }),

  renameFolder: asyncHandler<RenameFolderBody>(async (req, res) => {
    const { id, name } = req.body;
    const result = await engine.renameFolder(id, name);
    res.json(result);
  }),

  deleteFolder: asyncHandler<IdBody>(async (req, res) => {
    const id = req.body?.id ?? asString(req.query["id"]);
    if (!id) {
      res.status(400).json({ error: "id required" });
      return;
    }
    await engine.deleteFolder(id);
    res.json({ success: true });
  }),

  listFolders: asyncHandler(async (_req, res) => {
    const folders = await engine.listFolders();
    res.json(folders);
  }),

  export: asyncHandler(async (_req, res) => {
    const bundle = await engine.export();
    res.json(bundle);
  }),

  importBundle: asyncHandler<ImportBody>(async (req, res) => {
    const { bundle, mode } = req.body;
    const result = await engine.importBundle(bundle, mode ? { mode } : undefined);
    res.json(result);
  }),

  getById: asyncHandler(async (req, res) => {
    const id = asString(req.query["id"]);
    if (!id) {
      res.json(null);
      return;
    }
    const prompt = await engine.getById(id);
    res.json(prompt);
  }),

  getFolderById: asyncHandler(async (req, res) => {
    const id = asString(req.query["id"]);
    if (!id) {
      res.json(null);
      return;
    }
    const folder = await engine.getFolderById(id);
    res.json(folder);
  }),
};
