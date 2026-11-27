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
// source code, which remains licensed under the GNU AGPL version 3.
//
// SPDX-License-Identifier: AGPL-3.0-only

// The sandbox the chat's tools run in. A caller has one, shared by all their conversations: it
// is asked from the gateway for the caller's user id, which the gateway answers the same
// sandbox for while it runs, so nothing about it is stored anywhere and any instance of this
// service serves any call. Each conversation works in a directory of its own in it.
//
// A plugin task owns its sandbox for as long as it runs: a timer renews it and the task's
// `finally` deletes it. This one is kept alive by use alone: every tool call that reaches the
// gateway moves the expiry to its own length plus the idle lifetime (a call refused for its
// arguments reaches nobody), nothing renews it in between, and nothing here deletes
// it: a sandbox nobody calls is removed by the gateway when it expires, a deleted conversation
// leaves its directory to go with it.

import type { ChatSandboxConfig } from "../../config/index.js";
import { createSandbox, makeDir, renewSandbox } from "../pluginAgent/sandboxClient.js";
import { getSelfId } from "../storage/docspacePeopleApi.js";

// The image's writable directory (design.md of add-chat-sandbox, decision 7).
export const CHAT_SANDBOX_ROOT = "/workspace";

// A thread id goes into a path and into `cwd`: a plain name, nothing a path reads as more.
const THREAD_DIR_NAME = /^[A-Za-z0-9_-]{1,100}$/;

/** The directory of a conversation; undefined for a thread id that is not a plain name. */
export function threadDirOf(threadId: string): string | undefined {
  return THREAD_DIR_NAME.test(threadId) ? `${CHAT_SANDBOX_ROOT}/${threadId}` : undefined;
}

/**
 * The caller's running sandbox with `dir` in it, its expiry moved to `ttlSeconds` from now;
 * started when the caller has none. A sandbox started by this call is empty: `dir` is made
 * either way, so a call cannot tell, and neither can its result.
 */
export async function acquireUserSandbox(
  config: ChatSandboxConfig,
  dir: string,
  ttlSeconds: number,
  signal?: AbortSignal,
): Promise<string> {
  const userId = await getSelfId(signal);
  const id = await createSandbox(config, userId, ttlSeconds, signal);
  // `Create` answers a sandbox that already runs as it is, with the expiry it had: the lifetime
  // it is sent applies to a new one only (the gateway's session service).
  await renewSandbox(config, id, ttlSeconds, signal);
  await makeDir(config, id, dir, signal);
  return id;
}
