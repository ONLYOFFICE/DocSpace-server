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

  list: asyncHandler(async (req, res) => {
    const entityId = asString(req.query["entityId"]);
    const threads = await engine.list(entityId);
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
      res.status(400).json({ error: "threadId required" });
      return;
    }
    const thread = await engine.getById(threadId);
    res.json(thread);
  }),

  getMessageById: asyncHandler(async (req, res) => {
    const messageId = asString(req.query["messageId"]);
    if (!messageId) {
      res.status(400).json({ error: "messageId required" });
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
