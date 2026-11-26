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
import { useUpstream } from "../../__tests__/helpers/upstream.js";
import { HttpAssignmentsStorage } from "../assignmentsStorage.js";
import { HttpAttachmentsStorage } from "../attachmentsStorage.js";
import { getFileInfo, getFolderInfo } from "../docspaceFilesApi.js";
import { HttpMcpServersStorage } from "../mcpServersStorage.js";
import { HttpMessagesStorage } from "../messagesStorage.js";
import { HttpProfilesStorage } from "../profilesStorage.js";
import { HttpPromptFoldersStorage } from "../promptFoldersStorage.js";
import { HttpPromptsStorage } from "../promptsStorage.js";
import { HttpThreadsStorage } from "../threadsStorage.js";
import { filesApiGet, filesApiRequest, filesApiSend } from "../docspaceFilesApi.js";
import { aiService, aiServiceRootGet } from "../httpClient.js";
import { assertEntityPath } from "../upstreamShape.js";

// `GET <path>/` with an empty id reaches the list route of the same path, whose array a
// single-entity read cannot map: `assignments/bulk-assign` answered 502 for `{ Translation: "" }`
// where ASC.AI.Tests expects a per-item error (AssignmentBulkTests). No handler is registered,
// so a request that went out fails the test. `.` and `..` pass `encodeURIComponent` and are then
// removed by the URL parser as dot segments, so `threads/.` reached the same list route and
// `threads/..` the parent path.
useUpstream();

const NO_ENTITY = ["", ".", ".."];

const reads: [string, (id: string) => Promise<unknown>, unknown][] = [
  ["threads.readById", (id) => new HttpThreadsStorage().readById(id), null],
  ["messages.readById", (id) => new HttpMessagesStorage().readById(id), null],
  ["messages.readThreadId", (id) => new HttpMessagesStorage().readThreadId(id), null],
  ["attachments.readById", (id) => new HttpAttachmentsStorage().readById(id), null],
  ["prompts.readById", (id) => new HttpPromptsStorage().readById(id), null],
  ["promptFolders.readById", (id) => new HttpPromptFoldersStorage().readById(id), null],
  ["profiles.readById", (id) => new HttpProfilesStorage().readById(id), undefined],
  ["profiles.readByIdRaw", (id) => new HttpProfilesStorage().readByIdRaw(id), undefined],
  // `get-custom-server` answers 200 null for a name nobody registered (openspec ai-agents,
  // "Custom MCP servers"), and `.` is such a name.
  ["mcpServers.readByName", (id) => new HttpMcpServersStorage().readByName(id), null],
  ["assignments.readByType", (id) => new HttpAssignmentsStorage().readByType(id as never), null],
  ["files.getFolderInfo", (id) => getFolderInfo(id), undefined],
  ["files.getFileInfo", (id) => getFileInfo(id), undefined],
];

describe.each(NO_ENTITY)("a read by the id %j", (id) => {
  it.each(reads)("%s answers absent without calling out", async (_name, read, absent) => {
    await expect(read(id)).resolves.toBe(absent);
  });
});

// Every other call by id - a write, a delete, a read of a sub-resource - goes through one of
// the clients, which refuse the path before a request: `DELETE threads/.` would be sent as
// `DELETE threads/`, another route.
const clients: [string, (path: string) => Promise<unknown>][] = [
  ["aiService.get", (path) => aiService.get(`/${path}`)],
  ["aiService.post", (path) => aiService.post(`/${path}`, {})],
  ["aiService.put", (path) => aiService.put(`/${path}`, {})],
  ["aiService.delete", (path) => aiService.delete(`/${path}`)],
  ["aiServiceRootGet", (path) => aiServiceRootGet(`/${path}`)],
  ["filesApiGet", (path) => filesApiGet(path, { count: 1 })],
  ["filesApiSend", (path) => filesApiSend("PUT", path, {})],
  ["filesApiRequest", (path) => filesApiRequest("DELETE", path, { json: {} })],
];

describe.each(clients)("%s", (_name, call) => {
  it.each([
    ["an empty id at the end", "threads/"],
    ["an empty id in the middle", "threads//messages"],
    ["a dot at the end", "threads/."],
    ["a dot in the middle", "threads/./messages"],
    ["two dots at the end", "threads/.."],
    ["two dots in the middle", "rooms/../sources"],
    ["two dots before a query", "file/..?returnSingleOperation=true"],
  ])("answers 404 for a path with %s, without a request", async (_what, path) => {
    await expect(call(path)).rejects.toMatchObject({
      status: 404,
      expose: true,
      message: "Not Found",
    });
  });
});

describe("assertEntityPath", () => {
  it.each([
    "/threads",
    "threads/t-1/messages",
    "/config",
    "rooms/12/sources?x=..",
    "file/a.b",
    "file/...",
    "file/%2E",
    "@my?count=1",
  ])("lets %s through", (path) => {
    expect(() => assertEntityPath(path)).not.toThrow();
  });
});
