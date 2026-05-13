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
import { asyncHandler } from "./_helpers.js";

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

interface SaveFileBody {
  input: FileInput;
  entityId?: string;
}

interface SaveFilesManyBody {
  inputs: FileInput[];
  entityId?: string;
}

interface SaveImageBody {
  input: ImageInput;
  entityId?: string;
}

interface SaveImagesManyBody {
  inputs: ImageInput[];
  entityId?: string;
}

interface IdBody {
  id: string;
}

interface IdsBody {
  ids: string[];
}

interface LinkToMessageBody {
  ids: string[];
  messageId: string;
  threadId: string;
}

export const attachmentsController = {
  saveFile: asyncHandler<SaveFileBody>(async (req, res) => {
    const { input, entityId } = req.body;
    const result = await engine.saveFile(input, entityId);
    res.json(result);
  }),

  saveFilesMany: asyncHandler<SaveFilesManyBody>(async (req, res) => {
    const { inputs, entityId } = req.body;
    const result = await engine.saveFilesMany(inputs ?? [], entityId);
    res.json(result);
  }),

  saveImage: asyncHandler<SaveImageBody>(async (req, res) => {
    const { input, entityId } = req.body;
    const result = await engine.saveImage(input, entityId);
    res.json(result);
  }),

  saveImagesMany: asyncHandler<SaveImagesManyBody>(async (req, res) => {
    const { inputs, entityId } = req.body;
    const result = await engine.saveImagesMany(inputs ?? [], entityId);
    res.json(result);
  }),

  get: asyncHandler<IdBody>(async (req, res) => {
    const result = await engine.get(req.body.id);
    res.json(result);
  }),

  getMany: asyncHandler<IdsBody>(async (req, res) => {
    const result = await engine.getMany(req.body.ids ?? []);
    res.json(result);
  }),

  delete: asyncHandler<IdBody>(async (req, res) => {
    await engine.delete(req.body.id);
    res.json({ success: true });
  }),

  deleteMany: asyncHandler<IdsBody>(async (req, res) => {
    await engine.deleteMany(req.body.ids ?? []);
    res.json({ success: true });
  }),

  linkToMessage: asyncHandler<LinkToMessageBody>(async (req, res) => {
    const { ids, messageId, threadId } = req.body;
    await engine.linkToMessage(ids ?? [], messageId, threadId);
    res.json({ success: true });
  }),
};
