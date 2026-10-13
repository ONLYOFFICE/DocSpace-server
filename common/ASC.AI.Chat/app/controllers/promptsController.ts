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

import { PromptsEngine } from "@onlyoffice/ai-chat/core";
import type {
  CreatePromptInput,
  FolderMutationResult,
  ImportError,
  ImportMode,
  ImportResult,
  PromptBundle,
  PromptMutationResult,
} from "@onlyoffice/ai-chat/core";
import type { Response } from "express";
import { storage } from "../storage/index.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import { asString, isObject } from "../narrow.js";

const engine = new PromptsEngine({ storage });

// A refusal as the engine reports it: `{ success: false, error }` with the
// field named. The engine returns these instead of throwing, so the HTTP
// status is the controller's to pick (Bugs 84307-84311): `code` says why
// when the library sets it, and the message wording is the fallback for a
// library that predates the field.
type RefusalError = ImportError["error"];

function refusalStatus(error: RefusalError): number {
  const code = (error as { code?: string }).code;
  if (code === "not_found") return 404;
  if (code === "conflict") return 409;
  if (code !== undefined) return 400;
  if (/not found/i.test(error.message)) return 404;
  if (/already exists/i.test(error.message)) return 409;
  return 400;
}

function sendMutation(res: Response, result: PromptMutationResult | FolderMutationResult): void {
  res.status(result.success ? 200 : refusalStatus(result.error)).json(result);
}

// An import answers 409 when every refusal is a name conflict and 400 for
// anything else (a bad version, a dangling folder reference, an empty
// entry), so a client can offer "merge anyway" only where it applies.
function sendImport(res: Response, result: ImportResult): void {
  if (result.success) {
    res.json(result);
    return;
  }
  const statuses = result.errors.map((e) => refusalStatus(e.error));
  res.status(statuses.every((s) => s === 409) ? 409 : 400).json(result);
}

// Shape refusals raised here, before the engine: the same `{ success:
// false, error }` body the engine produces, so the widget's stores read
// them the same way. The engine assumes typed input and calls `.trim()`
// on `name` / `text`, which made a number or null a 500 (Bug 84429).
function shapeRefusal(res: Response, field: "name" | "text", message: string): void {
  res.status(400).json({ success: false, error: { field, message, code: "invalid" } });
}

function isOptionalString(v: unknown): v is string | undefined {
  return v === undefined || typeof v === "string";
}

function isFolderRef(v: unknown): v is string | null | undefined {
  return v === undefined || v === null || typeof v === "string";
}

// A bundle as `POST prompts/import-bundle` accepts it: the exact shape
// `GET prompts/export` produces. Anything else used to reach the engine,
// which dereferences `bundle.version` / `.folders` / `.prompts` unguarded
// and answered 500 (Bug 84423). Returns the refusals, empty when the
// bundle is well-formed.
function bundleShapeErrors(bundle: unknown): ImportError[] {
  const refuse = (kind: ImportError["kind"], ref: string, message: string): ImportError => ({
    kind,
    ref,
    error: { field: "name", message, code: "invalid" } as ImportError["error"],
  });
  if (!isObject(bundle)) {
    return [refuse("folder", "<bundle>", "bundle is required and must be an object")];
  }
  if (bundle["version"] !== 1) {
    return [refuse("folder", "<bundle>", `Unsupported bundle version: ${String(bundle["version"])}`)];
  }
  const errors: ImportError[] = [];
  const folders = bundle["folders"];
  const prompts = bundle["prompts"];
  if (!Array.isArray(folders)) {
    errors.push(refuse("folder", "<bundle>", "bundle.folders must be an array"));
  } else {
    folders.forEach((f, i) => {
      if (!isObject(f) || typeof f["id"] !== "string" || typeof f["name"] !== "string") {
        errors.push(refuse("folder", `folders[${i}]`, "a folder needs string id and name"));
      }
    });
  }
  if (!Array.isArray(prompts)) {
    errors.push(refuse("prompt", "<bundle>", "bundle.prompts must be an array"));
  } else {
    prompts.forEach((p, i) => {
      if (
        !isObject(p)
        || typeof p["id"] !== "string"
        || typeof p["name"] !== "string"
        || typeof p["text"] !== "string"
        || !isFolderRef(p["folderId"])
      ) {
        errors.push(
          refuse("prompt", `prompts[${i}]`, "a prompt needs string id, name and text, and a string or null folderId"),
        );
      }
    });
  }
  return errors;
}

const IMPORT_MODES: ReadonlySet<string> = new Set(["merge", "replace"]);

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
  create: asyncHandler(async (req, res) => {
    const body: unknown = req.body;
    if (!isObject(body)) {
      shapeRefusal(res, "name", "the request body must be an object");
      return;
    }
    if (typeof body["name"] !== "string") {
      shapeRefusal(res, "name", "name is required and must be a string");
      return;
    }
    if (typeof body["text"] !== "string") {
      shapeRefusal(res, "text", "text is required and must be a string");
      return;
    }
    if (!isFolderRef(body["folderId"])) {
      shapeRefusal(res, "name", "folderId must be a string or null");
      return;
    }
    const input: CreatePromptInput = {
      name: body["name"],
      text: body["text"],
      folderId: body["folderId"],
    };
    sendMutation(res, await engine.create(input));
  }),

  update: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["id", "updates"] as const);
    if (typeof args.id !== "string" || args.id.length === 0) {
      shapeRefusal(res, "name", "id is required and must be a string");
      return;
    }
    const updates: unknown = args.updates ?? {};
    if (!isObject(updates)) {
      shapeRefusal(res, "name", "updates must be an object");
      return;
    }
    if (!isOptionalString(updates["name"])) {
      shapeRefusal(res, "name", "name must be a string");
      return;
    }
    if (!isOptionalString(updates["text"])) {
      shapeRefusal(res, "text", "text must be a string");
      return;
    }
    if (!isFolderRef(updates["folderId"])) {
      shapeRefusal(res, "name", "folderId must be a string or null");
      return;
    }
    const patch: Partial<UpdateBody> = {};
    if (updates["name"] !== undefined) patch.name = updates["name"];
    if (updates["text"] !== undefined) patch.text = updates["text"];
    if (updates["folderId"] !== undefined) patch.folderId = updates["folderId"];
    sendMutation(res, await engine.update(args.id, patch));
  }),

  move: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["id", "folderId"] as const);
    if (typeof args.id !== "string" || args.id.length === 0) {
      shapeRefusal(res, "name", "id is required and must be a string");
      return;
    }
    if (!isFolderRef(args.folderId)) {
      shapeRefusal(res, "name", "folderId must be a string or null");
      return;
    }
    const folderId = args.folderId === undefined ? null : args.folderId;
    sendMutation(res, await engine.move(args.id, folderId));
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
    if (typeof args.name !== "string") {
      shapeRefusal(res, "name", "name is required and must be a string");
      return;
    }
    sendMutation(res, await engine.createFolder(args.name));
  }),

  renameFolder: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["id", "name"] as const);
    if (typeof args.id !== "string" || args.id.length === 0) {
      shapeRefusal(res, "name", "id is required and must be a string");
      return;
    }
    if (typeof args.name !== "string") {
      shapeRefusal(res, "name", "name is required and must be a string");
      return;
    }
    sendMutation(res, await engine.renameFolder(args.id, args.name));
  }),

  deleteFolder: asyncHandler(async (req, res) => {
    const { id } = unpackPositional(req.body, ["id"] as const);
    const idStr = typeof id === "string" ? id : asString(req.query["id"]);
    if (!idStr) {
      res.status(400).json({ error: "id required" });
      return;
    }
    // The storage delete swallows the C# 404, so deleting another user's
    // folder (scoped away by C#) or a nonexistent one reported success while
    // rename-folder on the same id answers "Folder not found" (Bug 83138).
    // Verify visibility at the HTTP boundary; both cases stay a uniform 404
    // so a foreign folder is indistinguishable from a missing one.
    if ((await storage.promptFolders.readById(idStr)) === null) {
      res.status(404).json({ error: `Folder not found: ${idStr}` });
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
    const shapeErrors = bundleShapeErrors(args.bundle);
    if (shapeErrors.length > 0) {
      res.status(400).json({ success: false, errors: shapeErrors });
      return;
    }
    const bundle = args.bundle as PromptBundle;
    const options: unknown = args.options;
    let mode: ImportMode | undefined;
    if (options !== undefined && options !== null) {
      const rawMode = isObject(options) ? options["mode"] : options;
      if (rawMode !== undefined && (typeof rawMode !== "string" || !IMPORT_MODES.has(rawMode))) {
        res.status(400).json({
          success: false,
          errors: [
            {
              kind: "folder",
              ref: "<options>",
              error: { field: "name", message: "options.mode must be merge or replace", code: "invalid" },
            },
          ],
        });
        return;
      }
      mode = rawMode as ImportMode | undefined;
    }
    sendImport(res, await engine.importBundle(bundle, mode === undefined ? undefined : { mode }));
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
