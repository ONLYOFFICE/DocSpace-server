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
import { asyncHandler, unpackPositional } from "./_helpers.js";
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

  update: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["id", "updates"] as const);
    const updates = (args.updates as Partial<UpdateBody>) ?? {};
    const result = await engine.update(args.id as string, updates);
    res.json(result);
  }),

  move: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["id", "folderId"] as const);
    const folderId = args.folderId === undefined ? null : (args.folderId as string | null);
    const result = await engine.move(args.id as string, folderId);
    res.json(result);
  }),

  delete: asyncHandler(async (req, res) => {
    const { id } = unpackPositional(req.body, ["id"] as const);
    const idStr = typeof id === "string" ? id : asString(req.query["id"]);
    if (!idStr) {
      res.status(400).json({ error: "id required" });
      return;
    }
    await engine.delete(idStr);
    res.json({ success: true });
  }),

  list: asyncHandler(async (req, res) => {
    // ApiProvider's fetcher drops both `null` and `undefined` folderId
    // values from the URL, so the backend cannot distinguish them.
    // Per spec, treat key-absent as `null` (root-level prompts).
    const folderIdRaw = req.query["folderId"];
    const folderId = folderIdRaw === undefined || folderIdRaw === ""
      ? null
      : asString(folderIdRaw) ?? null;
    const result = await engine.list(folderId);
    res.json(result);
  }),

  createFolder: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["name"] as const);
    const result = await engine.createFolder(args.name as string);
    res.json(result);
  }),

  renameFolder: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["id", "name"] as const);
    const result = await engine.renameFolder(args.id as string, args.name as string);
    res.json(result);
  }),

  deleteFolder: asyncHandler(async (req, res) => {
    const { id } = unpackPositional(req.body, ["id"] as const);
    const idStr = typeof id === "string" ? id : asString(req.query["id"]);
    if (!idStr) {
      res.status(400).json({ error: "id required" });
      return;
    }
    await engine.deleteFolder(idStr);
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

  importBundle: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["bundle", "options"] as const);
    const bundle = args.bundle as PromptBundle;
    const options = args.options as { mode?: "merge" | "replace" } | undefined;
    const result = await engine.importBundle(bundle, options);
    res.json(result);
  }),

  getById: asyncHandler(async (req, res) => {
    const id = asString(req.query["id"]);
    if (!id) {
      res.status(400).json({ error: "id required" });
      return;
    }
    const prompt = await engine.getById(id);
    res.json(prompt);
  }),

  getFolderById: asyncHandler(async (req, res) => {
    const id = asString(req.query["id"]);
    if (!id) {
      res.status(400).json({ error: "id required" });
      return;
    }
    const folder = await engine.getFolderById(id);
    res.json(folder);
  }),
};
