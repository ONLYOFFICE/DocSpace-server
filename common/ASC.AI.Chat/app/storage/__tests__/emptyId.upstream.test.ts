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
import { HttpAttachmentsStorage } from "../attachmentsStorage.js";
import { getFileInfo, getFolderInfo } from "../docspaceFilesApi.js";
import { HttpMessagesStorage } from "../messagesStorage.js";
import { HttpProfilesStorage } from "../profilesStorage.js";
import { HttpPromptFoldersStorage } from "../promptFoldersStorage.js";
import { HttpPromptsStorage } from "../promptsStorage.js";
import { HttpThreadsStorage } from "../threadsStorage.js";

// `GET <path>/` with an empty id reaches the list route of the same path, whose array a
// single-entity read cannot map: `assignments/bulk-assign` answered 502 for `{ Translation: "" }`
// where ASC.AI.Tests expects a per-item error (AssignmentBulkTests). No handler is registered,
// so a request that went out fails the test.
useUpstream();

const reads: [string, () => Promise<unknown>, unknown][] = [
  ["threads.readById", () => new HttpThreadsStorage().readById(""), null],
  ["messages.readById", () => new HttpMessagesStorage().readById(""), null],
  ["messages.readThreadId", () => new HttpMessagesStorage().readThreadId(""), null],
  ["attachments.readById", () => new HttpAttachmentsStorage().readById(""), null],
  ["prompts.readById", () => new HttpPromptsStorage().readById(""), null],
  ["promptFolders.readById", () => new HttpPromptFoldersStorage().readById(""), null],
  ["profiles.readById", () => new HttpProfilesStorage().readById(""), undefined],
  ["profiles.readByIdRaw", () => new HttpProfilesStorage().readByIdRaw(""), undefined],
  ["files.getFolderInfo", () => getFolderInfo(""), undefined],
  ["files.getFileInfo", () => getFileInfo(""), undefined],
];

describe("a read by an empty id", () => {
  it.each(reads)("%s answers absent without calling out", async (_name, read, absent) => {
    await expect(read()).resolves.toBe(absent);
  });
});
