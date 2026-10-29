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

import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("../../log.js", () => ({
  default: { info: vi.fn(), warn: vi.fn(), error: vi.fn(), debug: vi.fn() },
}));

const aiServiceGet = vi.fn();
vi.mock("../httpClient.js", () => ({ aiService: { get: aiServiceGet } }));

const resolveAiProjectEntityId = vi.fn();
const getMyDocumentsFolderId = vi.fn();
vi.mock("../docspaceFilesApi.js", () => ({ resolveAiProjectEntityId, getMyDocumentsFolderId }));

const getRoomSkills = vi.fn();
vi.mock("../roomSkills.js", () => ({ CONTEXT_CLOUD: "docspace", getRoomSkills }));

vi.mock("../aiProjectsMock/registry.js", () => ({ mockProjectTitle: () => undefined }));
vi.mock("../aiProjectsMock/store.js", () => ({}));

const { applyAiProjectChatContext } = await import("../aiProjectChatContext.js");

const projectContext = (overrides: Record<string, unknown> = {}) => ({
  projectId: 42,
  title: "Sales",
  myRole: 11,
  canChat: true,
  profileId: "p1",
  effectiveProfileId: "p1",
  profileAvailable: true,
  instructionsRoomIds: [7],
  knowledgeRoomIds: [9],
  unavailableRoomIds: [],
  ...overrides,
});

describe("applyAiProjectChatContext outside a project", () => {
  beforeEach(() => {
    resolveAiProjectEntityId.mockResolvedValue(undefined);
  });

  it("reads the skills of every picked DocSpace room again for the round", async () => {
    getRoomSkills.mockImplementation(async (id: string) =>
      id === "7"
        ? [{ id: "f1", name: "tone", description: "Brand voice." }, { id: "f2", name: "new", description: "" }]
        : [],
    );
    const body = {
      entityId: "3",
      actionArgs: {
        contextRooms: [
          { cloud: "docspace", id: "7", skills: [{ id: "f1", name: "old", description: "" }] },
          { cloud: "other", id: "x", skills: [{ id: "s", name: "keep", description: "" }] },
        ],
      },
    };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body.actionArgs.contextRooms).toEqual([
      {
        cloud: "docspace",
        id: "7",
        skills: [
          { id: "f1", name: "tone", description: "Brand voice." },
          { id: "f2", name: "new", description: "" },
        ],
      },
      { cloud: "other", id: "x", skills: [{ id: "s", name: "keep", description: "" }] },
    ]);
  });

  it("keeps the skills sent when a room cannot be read", async () => {
    getRoomSkills.mockRejectedValue(new Error("403"));
    const sent = [{ id: "f1", name: "old", description: "" }];
    const body = { actionArgs: { contextRooms: [{ cloud: "docspace", id: "7", skills: sent }] } };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body.actionArgs.contextRooms[0]?.skills).toEqual(sent);
  });

  it("leaves a body without context rooms alone", async () => {
    const body = { entityId: "3", actionArgs: { knowledgeRooms: [{ cloud: "docspace", id: "9" }] } };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(getRoomSkills).not.toHaveBeenCalled();
    expect(body.actionArgs).toEqual({ knowledgeRooms: [{ cloud: "docspace", id: "9" }] });
  });
});

describe("applyAiProjectChatContext inside a project", () => {
  beforeEach(() => {
    resolveAiProjectEntityId.mockResolvedValue("42");
    aiServiceGet.mockImplementation(async (url: string) => {
      if (url === "/projects/42/chat-context") return projectContext();
      if (url === "/projects/42/skills") {
        return { rooms: [{ id: 7, available: true, skills: [{ id: "f1", name: "tone", description: "" }] }] };
      }
      throw new Error(`unexpected ${url}`);
    });
    getMyDocumentsFolderId.mockResolvedValue("1001");
  });

  it("scopes a round without a picked folder to the caller's My documents", async () => {
    const body: Record<string, unknown> = {
      entityId: "42",
      contextEntityId: "7",
      profileId: "composer",
      actionArgs: { contextRooms: [{ cloud: "docspace", id: "5" }] },
    };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body["profileId"]).toBe("p1");
    expect(body["contextEntityId"]).toBe("1001");
    expect(body["actionArgs"]).toEqual({
      contextRooms: [{ cloud: "docspace", id: "7", skills: [{ id: "f1", name: "tone", description: "" }] }],
      knowledgeRooms: [{ cloud: "docspace", id: "9" }],
      folderContext: { id: "1001" },
    });
    expect(getRoomSkills).not.toHaveBeenCalled();
  });

  it("keeps the folder the caller picked as the scope", async () => {
    const body: Record<string, unknown> = {
      entityId: "42",
      actionArgs: { folderContext: { id: "555", name: "Q4 drafts" } },
    };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body["contextEntityId"]).toBe("555");
    expect((body["actionArgs"] as Record<string, unknown>)["folderContext"]).toEqual({ id: "555", name: "Q4 drafts" });
    expect(getMyDocumentsFolderId).not.toHaveBeenCalled();
  });

  it("runs without a folder context when My documents cannot be resolved", async () => {
    getMyDocumentsFolderId.mockResolvedValue(undefined);
    const body: Record<string, unknown> = { entityId: "42", contextEntityId: "7", actionArgs: {} };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body["contextEntityId"]).toBeUndefined();
    expect((body["actionArgs"] as Record<string, unknown>)["folderContext"]).toBeUndefined();
  });

  it("refuses a caller who may not chat, and a project whose model is gone", async () => {
    aiServiceGet.mockResolvedValueOnce(projectContext({ canChat: false }));
    expect(await applyAiProjectChatContext({ entityId: "42" })).toMatchObject({ status: 403 });
    aiServiceGet.mockResolvedValueOnce(projectContext({ profileAvailable: false }));
    expect(await applyAiProjectChatContext({ entityId: "42" })).toMatchObject({
      status: 409,
      body: { code: "projectModelUnavailable" },
    });
  });
});
