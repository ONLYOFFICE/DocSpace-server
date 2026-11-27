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
import { http, HttpResponse, useUpstream } from "../../__tests__/helpers/upstream.js";
import { HttpAssignmentsStorage } from "../assignmentsStorage.js";
import { HttpAttachmentsStorage } from "../attachmentsStorage.js";
import { HttpByokKeysStorage } from "../byokKeysStorage.js";
import { HttpFormAnalysisStorage } from "../formAnalysisStorage.js";
import { HttpStorageAdapter, storage } from "../index.js";
import { HttpMcpServersStorage } from "../mcpServersStorage.js";
import { HttpMessagesStorage } from "../messagesStorage.js";
import { HttpPreferencesStorage } from "../preferencesStorage.js";
import { HttpProfilesStorage } from "../profilesStorage.js";
import { HttpPromptFoldersStorage } from "../promptFoldersStorage.js";
import { HttpPromptsStorage } from "../promptsStorage.js";
import { HttpThreadsStorage } from "../threadsStorage.js";
import { HttpToolPrefsStorage } from "../toolPrefsStorage.js";
import { HttpWebSearchStorage } from "../webSearchStorage.js";

// GET api/2.0/files/rooms (FoldersController/VirtualRoomsController of products/ASC.Files) and
// GET api/2.0/files/rooms/skills (AiHubService, products/ASC.Files), answered in the
// `{ response }` envelope of the Files API.
const upstream = useUpstream();

function rooms(folders: { id: number; title: string }[]) {
  return HttpResponse.json({ response: { folders, total: folders.length } });
}

function skills(
  list: { id: number; skills: { id: string; name: string; description: string }[] }[],
) {
  return HttpResponse.json({
    response: { rooms: list.map((r) => ({ ...r, title: "", logo: null })) },
  });
}

describe("HttpStorageAdapter", () => {
  it("holds one storage of each kind", () => {
    expect(storage).toBeInstanceOf(HttpStorageAdapter);
    expect(storage.threads).toBeInstanceOf(HttpThreadsStorage);
    expect(storage.messages).toBeInstanceOf(HttpMessagesStorage);
    expect(storage.profiles).toBeInstanceOf(HttpProfilesStorage);
    expect(storage.prompts).toBeInstanceOf(HttpPromptsStorage);
    expect(storage.promptFolders).toBeInstanceOf(HttpPromptFoldersStorage);
    expect(storage.assignments).toBeInstanceOf(HttpAssignmentsStorage);
    expect(storage.preferences).toBeInstanceOf(HttpPreferencesStorage);
    expect(storage.mcpServers).toBeInstanceOf(HttpMcpServersStorage);
    expect(storage.toolPrefs).toBeInstanceOf(HttpToolPrefsStorage);
    expect(storage.webSearch).toBeInstanceOf(HttpWebSearchStorage);
    expect(storage.attachments).toBeInstanceOf(HttpAttachmentsStorage);
    expect(storage.formAnalysis).toBeInstanceOf(HttpFormAnalysisStorage);
    expect(storage.byokKeys).toBeInstanceOf(HttpByokKeysStorage);
  });

  it("init and close do nothing", async () => {
    const adapter = new HttpStorageAdapter();
    await expect(adapter.init()).resolves.toBeUndefined();
    await expect(adapter.close()).resolves.toBeUndefined();
    expect(upstream.seen).toEqual([]);
  });

  it("lists the Instructions rooms that hold a skill as the context folders", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms", () => rooms([{ id: 7, title: "Brand voice" }])),
      http.get("*/api/2.0/files/rooms/skills", () =>
        skills([{ id: 7, skills: [{ id: "f1", name: "tone", description: "Tone." }] }]),
      ),
    );
    await expect(storage.getContextFolders()).resolves.toEqual([
      { cloud: "docspace", rooms: [{ id: "7", name: "Brand voice" }] },
    ]);
  });

  it("lists the Knowledge rooms as the knowledge folders", async () => {
    upstream.use(http.get("*/api/2.0/files/rooms", () => rooms([{ id: 9, title: "Docs" }])));
    await expect(storage.getKnowledgeFolders()).resolves.toEqual([
      { cloud: "docspace", rooms: [{ id: "9", name: "Docs" }] },
    ]);
    expect(new URL(`http://x${upstream.seen[0]?.path}`).searchParams.get("type")).toBe("11");
  });

  it("reads a room's skills and a skill's body whatever cloud is named", async () => {
    upstream.use(
      http.get("*/api/2.0/files/rooms/skills", () =>
        skills([{ id: 7, skills: [{ id: "f1", name: "tone", description: "Tone." }] }]),
      ),
      http.get("*/api/2.0/files/file/f1/presigneduri", () =>
        HttpResponse.json({ response: "/storage/files/f1?k=1" }),
      ),
      http.get("*/storage/files/f1", () => new HttpResponse("# Tone")),
    );
    await expect(storage.getRoomSkills("elsewhere", "7")).resolves.toEqual([
      { id: "f1", name: "tone", description: "Tone." },
    ]);
    await expect(storage.getRoomSkill("elsewhere", "7", "f1")).resolves.toBe("# Tone");
  });
});
