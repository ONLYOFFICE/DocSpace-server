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
import { asyncHandler, unpackPositional } from "./_helpers.js";
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

  appendUserMessage: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["threadId", "message", "profileId"] as const);
    const messageId = await engine.appendUserMessage(
      args.threadId as string,
      args.message as ThreadMessageInput,
      args.profileId as string | undefined,
    );
    res.json({ messageId });
  }),

  touch: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["threadId", "profileId"] as const);
    await engine.touch(args.threadId as string, args.profileId as string | undefined);
    res.json({ success: true });
  }),

  rename: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["threadId", "title"] as const);
    await engine.rename(args.threadId as string, args.title as string);
    res.json({ success: true });
  }),

  delete: asyncHandler(async (req, res) => {
    const { threadId } = unpackPositional(req.body, ["threadId"] as const);
    const idStr = typeof threadId === "string" ? threadId : asString(req.query["threadId"]);
    if (!idStr) {
      res.status(400).json({ error: "threadId required" });
      return;
    }
    await engine.delete(idStr);
    res.json({ success: true });
  }),

  clearMessages: asyncHandler(async (req, res) => {
    const { threadId } = unpackPositional(req.body, ["threadId"] as const);
    const idStr = typeof threadId === "string" ? threadId : asString(req.query["threadId"]);
    if (!idStr) {
      res.status(400).json({ error: "threadId required" });
      return;
    }
    await engine.clearMessages(idStr);
    res.json({ success: true });
  }),

  regenerateTitle: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["threadId", "profile"] as const);
    const title = await engine.regenerateTitle(args.threadId as string, args.profile as Profile);
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

  updateMessage: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["messageId", "message"] as const);
    await engine.updateMessage(args.messageId as string, args.message as ThreadMessageInput);
    res.json({ success: true });
  }),

  deleteMessage: asyncHandler(async (req, res) => {
    const { messageId } = unpackPositional(req.body, ["messageId"] as const);
    const idStr = typeof messageId === "string" ? messageId : asString(req.query["messageId"]);
    if (!idStr) {
      res.status(400).json({ error: "messageId required" });
      return;
    }
    await engine.deleteMessage(idStr);
    res.json({ success: true });
  }),
};
