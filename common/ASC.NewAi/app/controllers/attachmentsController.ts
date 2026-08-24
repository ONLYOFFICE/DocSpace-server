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

import { AttachmentsEngine } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";
import logger from "../log.js";
import { isObject, parseInt10 } from "../narrow.js";

const engine = new AttachmentsEngine({ storage });

interface FileInput {
  path: string;
  content: string;
  type: number;
  title?: string;
}

interface ImageInput {
  name: string;
  base64: string;
  title?: string;
}

type ParseResult<T> = { ok: true; value: T } | { ok: false; error: string };

// Coerce a file `type` to a number. Accepts a real number or a numeric string
// like "1" (Bugs 82745, 82746); anything else (boolean, object, non-numeric
// string) yields `undefined` so the caller can reject it with a 400. The value
// is an open-ended ONLYOFFICE `c_oAscFileType` code (e.g. docx=65, pptx=129,
// xlsx=257, pdf=513, vsd=16385 — see the client's `getOnlyofficeFileType`),
// NOT the small `ASC.Web.Core` `FileType` category enum, so it is validated as
// a number only — a closed enum-membership check would reject legitimate
// attachments (see the Bug 82743 note).
function normalizeFileType(value: unknown): number | undefined {
  return parseInt10(value);
}

// Reject an image payload that is not valid base64 (Bug 82752). Accepts both a
// bare base64 string and a `data:<mime>;base64,<payload>` data URL (the shape
// the composer sends); validates the payload against the base64 alphabet and
// required padding.
function isValidBase64(value: string): boolean {
  const match = /^data:[^,]*;base64,(.*)$/s.exec(value);
  const data = (match ? (match[1] ?? "") : value).trim();
  if (data.length === 0 || data.length % 4 !== 0) {
    return false;
  }
  return /^[A-Za-z0-9+/]+={0,2}$/.test(data);
}

// Validate a single file-attachment input up front so a malformed shape returns
// a descriptive 400 instead of collapsing to a 500 inside the engine/storage
// (Bugs 82739, 82741, 82749). `type` is coerced to a number (Bugs 82745,
// 82746; see the Bug 82743 note) and an empty/absent `title` is accepted while
// a non-string `title` is rejected (Bugs 82740, 82748). `path` is the required
// host entryId that the AI backend resolves server-side, so it is validated as
// a string and passed through unchanged (see the Bug 82742 note).
function parseFileInput(raw: unknown): ParseResult<FileInput> {
  if (!isObject(raw)) {
    return { ok: false, error: "input is required and must be an object" };
  }
  if (typeof raw.path !== "string") {
    return { ok: false, error: "input.path is required and must be a string" };
  }
  if (typeof raw.content !== "string") {
    return { ok: false, error: "input.content is required and must be a string" };
  }
  const type = normalizeFileType(raw.type);
  if (type === undefined) {
    return {
      ok: false,
      error: "input.type is required and must be a number (ONLYOFFICE file type code)",
    };
  }
  if (raw.title !== undefined && typeof raw.title !== "string") {
    return { ok: false, error: "input.title must be a string when present" };
  }
  const value: FileInput = {
    path: raw.path,
    content: raw.content,
    type,
  };
  if (typeof raw.title === "string") {
    value.title = raw.title;
  }
  return { ok: true, value };
}

// Validate a single image-attachment input up front (Bugs 82751, 82752,
// 82753): `name` and `base64` must be present and non-empty, `base64` must be
// valid base64, and `title` (if present) must be a string.
function parseImageInput(raw: unknown): ParseResult<ImageInput> {
  if (!isObject(raw)) {
    return { ok: false, error: "input is required and must be an object" };
  }
  if (typeof raw.name !== "string" || raw.name.length === 0) {
    return {
      ok: false,
      error: "input.name is required and must be a non-empty string",
    };
  }
  if (typeof raw.base64 !== "string" || raw.base64.length === 0) {
    return {
      ok: false,
      error: "input.base64 is required and must be a non-empty string",
    };
  }
  if (!isValidBase64(raw.base64)) {
    return { ok: false, error: "input.base64 must be valid base64 data" };
  }
  if (raw.title !== undefined && typeof raw.title !== "string") {
    return { ok: false, error: "input.title must be a string when present" };
  }
  const value: ImageInput = { name: raw.name, base64: raw.base64 };
  if (typeof raw.title === "string") {
    value.title = raw.title;
  }
  return { ok: true, value };
}

export const attachmentsController = {
  saveFile: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["input", "entityId"] as const);
    // Validate the required `input` shape up front: without it, an undefined or
    // malformed `input` reaches the engine and throws a TypeError that collapses
    // to a generic 500 (Bugs 82739, 82741). Reject with a clean 400 instead.
    const parsed = parseFileInput(args.input);
    if (!parsed.ok) {
      res.status(400).json({ error: parsed.error });
      return;
    }
    const result = await engine.saveFile(parsed.value, args.entityId as string | undefined);
    res.json(result);
  }),

  saveFilesMany: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["inputs", "entityId"] as const);
    // Validate each element with the same rules as save-file; a bad element
    // must fail with a clean 400 rather than crash the engine (Bug 82754).
    // `inputs` itself is required: a missing/null value is a malformed
    // request, not an empty batch — only an explicit `[]` means "no files"
    // (Bug 82754 reopen).
    const list = args.inputs;
    if (!Array.isArray(list)) {
      res.status(400).json({ error: "inputs must be an array" });
      return;
    }
    const inputs: FileInput[] = [];
    for (let i = 0; i < list.length; i++) {
      const parsed = parseFileInput(list[i]);
      if (!parsed.ok) {
        res.status(400).json({ error: `inputs[${i}]: ${parsed.error}` });
        return;
      }
      inputs.push(parsed.value);
    }
    const result = await engine.saveFilesMany(inputs, args.entityId as string | undefined);
    // Tell the client which attached forms have analysable submission data and their field keys/types, by
    // adding the info onto each saved file (matched by DocSpace entry id) — keeps the array response shape.
    const entryIds = inputs
      .map((input) => input.path)
      .filter((path): path is string => typeof path === "string" && path.length > 0);

    // Best-effort enrichment: a form-analysis lookup failure must not fail the whole save.
    let analysis: Awaited<ReturnType<typeof storage.attachments.getFormAnalysis>> = [];
    try {
      analysis = await storage.attachments.getFormAnalysis(entryIds);
    } catch (err) {
      logger.warn(
        `saveFilesMany: form-analysis lookup failed: ${err instanceof Error ? err.message : String(err)}`,
      );
    }

    if (Array.isArray(result) && analysis.length > 0) {
      const byEntryId = new Map(analysis.map((item) => [item.entryId, item]));
      for (const file of result) {
        const path = (file as { path?: string })?.path;
        const entryId = typeof path === "string" ? path.split("/", 1)[0] : undefined;
        const info = entryId ? byEntryId.get(entryId) : undefined;
        if (info) {
          const target = file as unknown as Record<string, unknown>;
          target.canAnalyze = info.canAnalyze;
          target.formKeys = info.keys;
        }
      }
    }

    res.json(result);
  }),

  saveImage: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["input", "entityId"] as const);
    // Validate the image payload up front: a missing/invalid `base64` or `name`
    // reaches the engine and throws, collapsing to a 500 (Bugs 82751, 82752,
    // 82753). Reject with a clean 400 instead.
    const parsed = parseImageInput(args.input);
    if (!parsed.ok) {
      res.status(400).json({ error: parsed.error });
      return;
    }
    const result = await engine.saveImage(parsed.value, args.entityId as string | undefined);
    res.json(result);
  }),

  saveImagesMany: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["inputs", "entityId"] as const);
    // Validate each element with the same rules as save-image; an element
    // without a payload must fail with a clean 400 (Bug 82755).
    const rawInputs = args.inputs;
    if (rawInputs !== undefined && rawInputs !== null && !Array.isArray(rawInputs)) {
      res.status(400).json({ error: "inputs must be an array" });
      return;
    }
    const list = Array.isArray(rawInputs) ? rawInputs : [];
    const inputs: ImageInput[] = [];
    for (let i = 0; i < list.length; i++) {
      const parsed = parseImageInput(list[i]);
      if (!parsed.ok) {
        res.status(400).json({ error: `inputs[${i}]: ${parsed.error}` });
        return;
      }
      inputs.push(parsed.value);
    }
    const result = await engine.saveImagesMany(inputs, args.entityId as string | undefined);
    res.json(result);
  }),

  get: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["id"] as const);
    // An empty/absent id reaches the DocSpace storage as a bare route and
    // comes back as a 405; validate it here so the caller gets a clean 400
    // (Bug 82756).
    if (typeof args.id !== "string" || args.id.length === 0) {
      res.status(400).json({ error: "id is required" });
      return;
    }
    const result = await engine.get(args.id);
    res.json(result);
  }),

  getMany: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["ids"] as const);
    // An empty body leaves `ids` unset and the engine batch-scan throws,
    // collapsing to a 500; require a present, non-empty array (Bug 82763).
    if (!Array.isArray(args.ids) || args.ids.length === 0) {
      res.status(400).json({
        error: "ids is required and must be a non-empty array",
      });
      return;
    }
    const result = await engine.getMany(args.ids as string[]);
    res.json(result);
  }),

  delete: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["id"] as const);
    await engine.delete(args.id as string);
    res.json({ success: true });
  }),

  deleteMany: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["ids"] as const);
    await engine.deleteMany((args.ids as string[]) ?? []);
    res.json({ success: true });
  }),

  linkToMessage: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["ids", "messageId", "threadId"] as const);
    // Require every param up front: an empty body would otherwise reach storage
    // as undefined ids/messageId/threadId and fail late (Bug 82771).
    if (
      !Array.isArray(args.ids) ||
      args.ids.length === 0 ||
      typeof args.messageId !== "string" ||
      args.messageId.length === 0 ||
      typeof args.threadId !== "string" ||
      args.threadId.length === 0
    ) {
      res.status(400).json({
        error: "ids (non-empty array), messageId (string) and threadId (string) are required",
      });
      return;
    }
    await engine.linkToMessage(args.ids as string[], args.messageId, args.threadId);
    res.json({ success: true });
  }),
};
