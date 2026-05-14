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

import { AttachmentsEngine } from "@onlyoffice/ai-chat/core";
import { storage } from "../storage/index.js";
import { asyncHandler, unpackPositional } from "./_helpers.js";

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
    const result = await engine.saveFilesMany(
      (args.inputs as FileInput[]) ?? [],
      args.entityId as string | undefined,
    );
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
