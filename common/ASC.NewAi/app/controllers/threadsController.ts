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

import { ThreadsEngine, AssignmentsEngine, ActionType } from "@onlyoffice/ai-chat/core";
import type {
  Profile,
  OpenOrCreateInput,
  MessagesCursor,
} from "@onlyoffice/ai-chat/core";
import type { ThreadMessageLike } from "@assistant-ui/react";
import { storage } from "../storage/index.js";
import { asyncHandler, unpackPositional, attachmentLimitError } from "./_helpers.js";
import { asString, parseInt10, isObject, getString } from "../narrow.js";
import { assertEntityAccessible } from "../storage/docspaceFilesApi.js";
import { agentAssignedProfileId } from "./agentProfile.js";

// `cursor` arrives JSON-stringified in the query (see the route table in
// the library: DEFAULT_THREADS_ROUTES.readMessages). Malformed or alien
// values degrade to `undefined` — an unpaginated read — rather than 400,
// matching the storage contract's "may ignore pagination" latitude.
function parseMessagesCursor(raw: unknown): MessagesCursor | undefined {
  if (typeof raw !== "string" || raw.length === 0) {
    return undefined;
  }
  try {
    const parsed: unknown = JSON.parse(raw);
    if (isObject(parsed) && getString(parsed, "id") !== undefined) {
      return parsed as unknown as MessagesCursor;
    }
  } catch {
    // fall through — not JSON
  }
  return undefined;
}

const engine = new ThreadsEngine({ storage });

interface CreateBody {
  title: string;
  profileId?: string;
  entityId?: string;
}

const assignmentsEngine = new AssignmentsEngine({ storage });

// Gate for creating a thread (review #6, Bug 82719). Two requirements:
//  1. If an `entityId` is supplied it must reference a folder the caller can
//     access — a missing folder or a no-access response both surface as 404
//     (see `assertEntityAccessible`). An accessible NON-agent folder is
//     allowed BY DESIGN (Bug 82719 reopen decision): threads are either
//     global or agent-scoped, so `HttpThreadsStorage` folds a non-agent
//     entityId (e.g. the Trash root, an ordinary room — the main client
//     sends the current location here) to the global scope instead of
//     rejecting it. An absent entityId is the legitimate global scope.
//  2. A live profile must be resolvable — an explicit `profileId` that exists,
//     or the `Chat` assignment for the scope — otherwise there is no model to
//     run the thread against, so reject with 404.
export async function assertThreadCreatable(
  entityId: string | undefined,
  profileId: string | undefined,
): Promise<void> {
  await assertEntityAccessible(entityId);
  // A malformed profileId makes the C# lookup fail with a non-404 status;
  // treat that the same as "no such profile" and fall through to the
  // scope-assignment resolution instead of relaying an opaque error
  // (Bug 83045).
  if (
    profileId
    && (await storage.profiles.readById(profileId).catch(() => undefined))
  ) {
    return;
  }
  const resolved = await assignmentsEngine.tryResolveForAction(
    ActionType.Chat,
    entityId,
  );
  if (resolved?.profile) {
    return;
  }
  throw Object.assign(new Error("no AI profile is available for this thread"), {
    status: 404,
    expose: true,
  });
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
    // The agent's assigned profile is authoritative for threads created in
    // its room: substitute it over any caller-supplied profileId so a new
    // thread cannot start on a different model (Bug 82915).
    if (req.body && typeof req.body === "object") {
      const agentProfileId = await agentAssignedProfileId(req.body.entityId);
      if (agentProfileId) {
        req.body.profileId = agentProfileId;
      }
    }
    await assertThreadCreatable(req.body?.entityId, req.body?.profileId);
    const thread = await engine.create(req.body);
    res.json(thread);
  }),

  openOrCreate: asyncHandler<OpenOrCreateInput>(async (req, res) => {
    // Thread creation requires a profile and, when scoped, an accessible entity
    // (review #6, Bug 82826). openOrCreate carries the resolved `profile`
    // object directly, so a missing/non-object profile means there is no model
    // to run the thread against → 404 (this also avoids the engine TypeError
    // that previously collapsed to a 500). A supplied entityId must be
    // reachable; a non-agent one folds to the global scope downstream (see
    // `assertThreadCreatable` for the Bug 82719 design decision).
    const body = req.body;
    if (!isObject(body) || !isObject(body.profile)) {
      res.status(404).json({
        error: "an AI profile is required to open or create a thread",
      });
      return;
    }
    await assertEntityAccessible(getString(body, "entityId"));
    const result = await engine.openOrCreate(body);
    res.json(result);
  }),

  appendUserMessage: asyncHandler(async (req, res) => {
    const args = unpackPositional(req.body, ["threadId", "message", "profileId"] as const);
    // Enforce the composer's per-kind attachment cap server-side — the UI
    // cannot exceed it, so only a direct API call can (Bug 82894).
    const limitError = attachmentLimitError(args.message);
    if (limitError) {
      res.status(400).json({ error: limitError });
      return;
    }
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
    // Same title rules as threads/create: a missing/null/empty/whitespace
    // title is a 400, never a silent success — rename used to accept all of
    // those and blank the stored name, a state create can't produce
    // (Bug 83094).
    if (typeof args.threadId !== "string" || args.threadId.length === 0) {
      res.status(400).json({ error: "threadId required" });
      return;
    }
    if (typeof args.title !== "string" || args.title.trim().length === 0) {
      res.status(400).json({
        error: "title is required and must be a non-empty string",
      });
      return;
    }
    await engine.rename(args.threadId, args.title);
    res.json({ success: true });
  }),

  delete: asyncHandler(async (req, res) => {
    const { threadId } = unpackPositional(req.body, ["threadId"] as const);
    const idStr = typeof threadId === "string" ? threadId : asString(req.query["threadId"]);
    if (!idStr) {
      res.status(400).json({ error: "threadId required" });
      return;
    }
    // The storage layer deliberately swallows the C# 404 (idempotent delete
    // for engine cascades), so without this check deleting a nonexistent or
    // already-deleted thread reported success — unlike rename/clear-messages
    // on the same ids (Bug 83095). Verify existence at the HTTP boundary.
    if ((await storage.threads.readById(idStr)) === null) {
      res.status(404).json({ error: "thread not found" });
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
    // The engine dereferences `profile` (and needs a real threadId); a missing
    // profile makes it throw a TypeError → 500 (Bug 82828). Validate both up
    // front and return a clean 400.
    if (typeof args.threadId !== "string" || !isObject(args.profile)) {
      res.status(400).json({
        error: "threadId (string) and profile (object) are required",
      });
      return;
    }
    const title = await engine.regenerateTitle(args.threadId, args.profile as Profile);
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
    const count = parseInt10(req.query["count"]);
    const cursor = parseMessagesCursor(req.query["cursor"]);
    const messages = await engine.readMessages(threadId, count, cursor);
    res.json(messages);
  }),

  getById: asyncHandler(async (req, res) => {
    const threadId = asString(req.query["threadId"]);
    if (!threadId) {
      res.status(400).json({ error: "threadId required" });
      return;
    }
    const thread = await engine.getById(threadId);
    // Storage returns null for a missing thread; without this guard the handler
    // answers 200 with a null body for a nonexistent threadId (Bug 82718).
    if (thread === null) {
      res.status(404).json({ error: "thread not found" });
      return;
    }
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
