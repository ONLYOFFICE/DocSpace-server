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

import { describe, expect, it } from "vitest";
import { afterUpstreamTimeout } from "../../__tests__/helpers/time.js";
import { http, HttpResponse, stalled, useUpstream } from "../../__tests__/helpers/upstream.js";
import { HttpAssignmentsStorage } from "../assignmentsStorage.js";
import { HttpAttachmentsStorage } from "../attachmentsStorage.js";
import { HttpMcpServersStorage } from "../mcpServersStorage.js";
import { HttpMessagesStorage } from "../messagesStorage.js";
import { HttpPreferencesStorage } from "../preferencesStorage.js";
import { HttpPromptFoldersStorage } from "../promptFoldersStorage.js";
import { HttpPromptsStorage } from "../promptsStorage.js";
import { HttpThreadsStorage } from "../threadsStorage.js";
import { HttpToolPrefsStorage } from "../toolPrefsStorage.js";
import { UpstreamShapeError } from "../upstreamShape.js";
import { HttpWebSearchStorage } from "../webSearchStorage.js";

// Every call of the AI service that reads a 404 as "not there": a read that answers `null` for
// it, a delete that takes it for "already gone". An AI service that never answers is not a 404:
// the call rejects with the timeout, so the route answers a 5xx and the caller can retry
// (ts-ai-failure-paths.md §1, ts-ai-tests.md §6). What each does with a 500 is in its own file.
const upstream = useUpstream();

const calls: [string, () => Promise<unknown>][] = [
  ["assignments.readByType", () => new HttpAssignmentsStorage().readByType("Chat")],
  ["assignments.delete", () => new HttpAssignmentsStorage().delete("Chat")],
  ["attachments.readById", () => new HttpAttachmentsStorage().readById("a1")],
  ["attachments.delete", () => new HttpAttachmentsStorage().delete("a1")],
  ["mcpServers.readByName", () => new HttpMcpServersStorage().readByName("crm")],
  ["mcpServers.delete", () => new HttpMcpServersStorage().delete("crm")],
  ["messages.readById", () => new HttpMessagesStorage().readById("m1")],
  ["messages.readThreadId", () => new HttpMessagesStorage().readThreadId("m1")],
  ["messages.delete", () => new HttpMessagesStorage().delete("m1")],
  ["messages.deleteByThread", () => new HttpMessagesStorage().deleteByThread("t1")],
  [
    "preferences.readToolPermissionMode",
    () => new HttpPreferencesStorage().readToolPermissionMode(),
  ],
  ["preferences.readReasoningLevel", () => new HttpPreferencesStorage().readReasoningLevel()],
  ["preferences.deleteReasoningLevel", () => new HttpPreferencesStorage().deleteReasoningLevel()],
  ["promptFolders.readById", () => new HttpPromptFoldersStorage().readById("f1")],
  ["promptFolders.delete", () => new HttpPromptFoldersStorage().delete("f1")],
  ["prompts.readById", () => new HttpPromptsStorage().readById("p1")],
  ["prompts.delete", () => new HttpPromptsStorage().delete("p1")],
  ["threads.readById", () => new HttpThreadsStorage().readById("t1")],
  ["threads.delete", () => new HttpThreadsStorage().delete("t1")],
  ["webSearch.read", () => new HttpWebSearchStorage().read()],
  ["webSearch.delete", () => new HttpWebSearchStorage().delete()],
];

describe("a call that reads a 404 as absent, against an AI service that never answers", () => {
  it.each(calls)("%s rejects with the timeout", async (_name, call) => {
    upstream.use(http.all("*/internal/ai/*", stalled));

    const outcome = await afterUpstreamTimeout(call);

    expect(outcome).toEqual({ error: expect.any(Error) });
    expect(String("error" in outcome && outcome.error)).toContain("upstream timeout after");
    expect(upstream.seen).toHaveLength(1);
  });
});

// The envelope of a failure (`ErrorApiResponse`, common/ASC.Api.Core/Middleware/
// CommonApiResponse.cs: `status: 1` and `error`, no `response`) sent with a 200 by a hop in
// front of the AI service. It is not the empty envelope of an action that returned null: read
// as "nothing stored", it switched every disabled tool back on.
describe("the same calls, against the envelope of a failure sent with a 200", () => {
  it.each([
    ...calls,
    ["toolPrefs.readDisabled", () => new HttpToolPrefsStorage().readDisabled()] as const,
  ])("%s rejects with an answer of an unknown shape", async (_name, call) => {
    upstream.use(
      http.all("*/internal/ai/*", () =>
        HttpResponse.json({ error: { message: "boom" }, status: 1, statusCode: 500 }),
      ),
    );

    await expect(call()).rejects.toBeInstanceOf(UpstreamShapeError);
  });
});
