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

import { ThreadsEngine } from "@onlyoffice/ai-chat/core";
import type { Profile, OpenOrCreateInput } from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";
import { storage } from "../storage/index.js";
import { asyncHandler } from "./_helpers.js";
import { asString, parseInt10 } from "../narrow.js";

const engine = new ThreadsEngine({ storage });

interface CreateBody {
  title: string;
  profileId?: string;
}

type ThreadMessageInput = Omit<ThreadMessageLike, "id" | "createdAt">;

interface AppendUserMessageBody {
  threadId: string;
  message: ThreadMessageInput;
  profileId?: string;
}

interface TouchBody {
  threadId: string;
  profileId?: string;
}

interface RenameBody {
  threadId: string;
  title: string;
}

interface ThreadIdBody {
  threadId?: string;
}

interface MessageIdBody {
  messageId?: string;
}

interface RegenerateTitleBody {
  threadId: string;
  profile: Profile;
}

interface UpdateMessageBody {
  messageId: string;
  message: ThreadMessageInput;
}

export const threadsController = {
  create: asyncHandler<CreateBody>(async (req, res) => {
    const thread = await engine.create(req.body);
    res.json(thread);
  }),

  openOrCreate: asyncHandler<OpenOrCreateInput>(async (req, res) => {
    const result = await engine.openOrCreate(req.body);
    res.json(result);
  }),

  appendUserMessage: asyncHandler<AppendUserMessageBody>(async (req, res) => {
    const { threadId, message, profileId } = req.body;
    const messageId = await engine.appendUserMessage(threadId, message, profileId);
    res.json({ messageId });
  }),

  touch: asyncHandler<TouchBody>(async (req, res) => {
    const { threadId, profileId } = req.body;
    await engine.touch(threadId, profileId);
    res.json({ success: true });
  }),

  rename: asyncHandler<RenameBody>(async (req, res) => {
    const { threadId, title } = req.body;
    await engine.rename(threadId, title);
    res.json({ success: true });
  }),

  delete: asyncHandler<ThreadIdBody>(async (req, res) => {
    const threadId = req.body?.threadId ?? asString(req.query["threadId"]);
    if (!threadId) {
      res.status(400).json({ error: "threadId required" });
      return;
    }
    await engine.delete(threadId);
    res.json({ success: true });
  }),

  clearMessages: asyncHandler<ThreadIdBody>(async (req, res) => {
    const threadId = req.body?.threadId ?? asString(req.query["threadId"]);
    if (!threadId) {
      res.status(400).json({ error: "threadId required" });
      return;
    }
    await engine.clearMessages(threadId);
    res.json({ success: true });
  }),

  regenerateTitle: asyncHandler<RegenerateTitleBody>(async (req, res) => {
    const { threadId, profile } = req.body;
    const title = await engine.regenerateTitle(threadId, profile);
    res.json({ title });
  }),

  list: asyncHandler(async (_req, res) => {
    const threads = await engine.list();
    res.json(threads);
  }),

  readMessages: asyncHandler(async (req, res) => {
    const threadId = asString(req.query["threadId"]);
    if (!threadId) {
      res.json([]);
      return;
    }
    const limit = parseInt10(req.query["limit"]);
    const startIndex = parseInt10(req.query["startIndex"]);
    const messages = await engine.readMessages(threadId, limit, startIndex);
    res.json(messages);
  }),

  getById: asyncHandler(async (req, res) => {
    const threadId = asString(req.query["threadId"]);
    if (!threadId) {
      res.json(null);
      return;
    }
    const thread = await engine.getById(threadId);
    res.json(thread);
  }),

  getMessageById: asyncHandler(async (req, res) => {
    const messageId = asString(req.query["messageId"]);
    if (!messageId) {
      res.json(null);
      return;
    }
    const message = await engine.getMessageById(messageId);
    res.json(message);
  }),

  updateMessage: asyncHandler<UpdateMessageBody>(async (req, res) => {
    const { messageId, message } = req.body;
    await engine.updateMessage(messageId, message);
    res.json({ success: true });
  }),

  deleteMessage: asyncHandler<MessageIdBody>(async (req, res) => {
    const messageId = req.body?.messageId ?? asString(req.query["messageId"]);
    if (!messageId) {
      res.status(400).json({ error: "messageId required" });
      return;
    }
    await engine.deleteMessage(messageId);
    res.json({ success: true });
  }),
};
