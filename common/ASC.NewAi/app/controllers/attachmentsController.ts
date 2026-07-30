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

export const attachmentsController = {
  saveFile: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["input", "entityId"] as const);
    const result = await engine.saveFile(
      args.input as FileInput,
      args.entityId as string | undefined,
    );
    res.json(result);
  }),

  saveFilesMany: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["inputs", "entityId"] as const);
    const inputs = (args.inputs as FileInput[]) ?? [];
    const result = await engine.saveFilesMany(
      inputs,
      args.entityId as string | undefined,
    );
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
      logger.warn(`saveFilesMany: form-analysis lookup failed: ${err instanceof Error ? err.message : String(err)}`);
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
    const result = await engine.saveImage(
      args.input as ImageInput,
      args.entityId as string | undefined,
    );
    res.json(result);
  }),

  saveImagesMany: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["inputs", "entityId"] as const);
    const result = await engine.saveImagesMany(
      (args.inputs as ImageInput[]) ?? [],
      args.entityId as string | undefined,
    );
    res.json(result);
  }),

  get: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["id"] as const);
    const result = await engine.get(args.id as string);
    res.json(result);
  }),

  getMany: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["ids"] as const);
    const result = await engine.getMany((args.ids as string[]) ?? []);
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
    await engine.linkToMessage(
      (args.ids as string[]) ?? [],
      args.messageId as string,
      args.threadId as string,
    );
    res.json({ success: true });
  }),
};
