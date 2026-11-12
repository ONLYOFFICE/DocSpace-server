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

const filesApiGet = vi.fn();
const resolveAiProjectEntityId = vi.fn();
const getMyDocumentsFolderId = vi.fn();
vi.mock("../docspaceFilesApi.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../docspaceFilesApi.js")>()),
  filesApiGet,
  resolveAiProjectEntityId,
  getMyDocumentsFolderId,
}));

const getRoomsSkills = vi.fn();
vi.mock("../roomSkills.js", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../roomSkills.js")>()),
  getRoomsSkills,
}));

// The project's own model as `GET files/rooms/42/model` answers it.
let projectModel: string | null = null;

const readByType = vi.fn();
const readById = vi.fn();
vi.mock("../index.js", () => ({
  storage: { assignments: { readByType }, profiles: { readById } },
}));

const { applyAiProjectChatContext } = await import("../aiProjectChatContext.js");
const { DocspaceApiHttpError } = await import("../docspaceFilesApi.js");

// The project as `GET api/2.0/files/rooms/{id}` answers it (the FolderDto fields the context reads).
const projectRoom = (overrides: Record<string, unknown> = {}) => ({
  id: 42,
  title: "Sales",
  access: 2,
  inRoom: true,
  ...overrides,
});

describe("applyAiProjectChatContext outside a project", () => {
  beforeEach(() => {
    resolveAiProjectEntityId.mockResolvedValue(undefined);
  });

  it("reads the skills of every picked DocSpace room again for the round", async () => {
    getRoomsSkills.mockResolvedValue(
      new Map([
        [
          "7",
          [
            { id: "f1", name: "tone", description: "Brand voice." },
            { id: "f2", name: "new", description: "Second." },
          ],
        ],
      ]),
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
          { id: "f2", name: "new", description: "Second." },
        ],
      },
      { cloud: "other", id: "x", skills: [{ id: "s", name: "keep", description: "" }] },
    ]);
    // One catalog read for every picked DocSpace room.
    expect(getRoomsSkills).toHaveBeenCalledWith(["7"]);
  });

  it("keeps the skills sent for a room the catalog leaves out", async () => {
    getRoomsSkills.mockResolvedValue(new Map());
    const sent = [{ id: "f1", name: "old", description: "" }];
    const body = { actionArgs: { contextRooms: [{ cloud: "docspace", id: "7", skills: sent }] } };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body.actionArgs.contextRooms[0]?.skills).toEqual(sent);
  });

  it("keeps the skills sent when the catalog cannot be read", async () => {
    getRoomsSkills.mockRejectedValue(new Error("500"));
    const sent = [{ id: "f1", name: "old", description: "" }];
    const body = { actionArgs: { contextRooms: [{ cloud: "docspace", id: "7", skills: sent }] } };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body.actionArgs.contextRooms[0]?.skills).toEqual(sent);
  });

  it("leaves a body without context rooms alone", async () => {
    const body = {
      entityId: "3",
      actionArgs: { knowledgeRooms: [{ cloud: "docspace", id: "9" }] },
    };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(getRoomsSkills).not.toHaveBeenCalled();
    expect(body.actionArgs).toEqual({ knowledgeRooms: [{ cloud: "docspace", id: "9" }] });
  });
});

// The project's sources as `GET api/2.0/files/rooms/{id}/sources` answers them.
const sources = {
  instructionsRooms: [{ id: 7, title: "Brand voice", logo: null }],
  knowledgeRooms: [{ id: 9, title: "Docs", logo: null }],
};

describe("applyAiProjectChatContext inside a project", () => {
  beforeEach(() => {
    resolveAiProjectEntityId.mockResolvedValue("42");
    filesApiGet.mockReset();
    filesApiGet.mockImplementation(async (path: string) => {
      if (path === "rooms/42") return projectRoom();
      if (path === "rooms/42/sources") return sources;
      if (path === "rooms/42/model") return { profileId: projectModel };
      if (path === "rooms/42/skills") {
        return {
          rooms: [
            {
              id: 7,
              title: "Brand voice",
              logo: null,
              skills: [{ id: "f1", name: "tone", description: "Tone." }],
            },
          ],
        };
      }
      throw new Error(`unexpected ${path}`);
    });
    projectModel = null;
    readByType.mockResolvedValue("p1");
    readById.mockResolvedValue({ id: "p1", capabilities: 1 });
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
      contextRooms: [
        { cloud: "docspace", id: "7", skills: [{ id: "f1", name: "tone", description: "Tone." }] },
      ],
      knowledgeRooms: [{ cloud: "docspace", id: "9" }],
    });
    // The project, its sources, its model and the skills all come from the Files API.
    expect(filesApiGet).toHaveBeenCalledWith("rooms/42");
    expect(filesApiGet).toHaveBeenCalledWith("rooms/42/sources");
    expect(filesApiGet).toHaveBeenCalledWith("rooms/42/model");
    expect(filesApiGet).toHaveBeenCalledWith("rooms/42/skills");
  });

  it("runs the round without the skills when the catalog cannot be read", async () => {
    filesApiGet.mockImplementation(async (path: string) => {
      if (path === "rooms/42") return projectRoom();
      if (path === "rooms/42/sources") return sources;
      if (path === "rooms/42/model") return { profileId: null };
      throw new Error("500");
    });
    const body: Record<string, unknown> = { entityId: "42", actionArgs: {} };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect((body["actionArgs"] as Record<string, unknown>)["contextRooms"]).toEqual([
      { cloud: "docspace", id: "7", skills: [] },
    ]);
  });

  it("keeps the folder the caller picked as the scope", async () => {
    const body: Record<string, unknown> = {
      entityId: "42",
      actionArgs: { folderContext: { id: "555", name: "Q4 drafts" } },
    };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body["contextEntityId"]).toBe("555");
    expect((body["actionArgs"] as Record<string, unknown>)["folderContext"]).toEqual({
      id: "555",
      name: "Q4 drafts",
    });
    expect(getMyDocumentsFolderId).not.toHaveBeenCalled();
  });

  it("scopes the round to the first of several picked context folders and keeps them all", async () => {
    const folderContexts = [{ id: "555" }, { id: "777", name: "Reports" }];
    const body: Record<string, unknown> = { entityId: "42", actionArgs: { folderContexts } };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body["contextEntityId"]).toBe("555");
    expect((body["actionArgs"] as Record<string, unknown>)["folderContexts"]).toEqual(folderContexts);
    expect(getMyDocumentsFolderId).not.toHaveBeenCalled();
  });

  it("runs without a folder context when My documents cannot be resolved", async () => {
    getMyDocumentsFolderId.mockResolvedValue(undefined);
    const body: Record<string, unknown> = { entityId: "42", contextEntityId: "7", actionArgs: {} };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body["contextEntityId"]).toBeUndefined();
    expect((body["actionArgs"] as Record<string, unknown>)["folderContext"]).toBeUndefined();
  });

  it("refuses an admin outside the project, and a portal whose Chat model is gone", async () => {
    filesApiGet.mockResolvedValueOnce(projectRoom({ access: 0, inRoom: false }));
    expect(await applyAiProjectChatContext({ entityId: "42" })).toMatchObject({ status: 403 });
    readById.mockResolvedValueOnce(undefined);
    expect(await applyAiProjectChatContext({ entityId: "42" })).toMatchObject({
      status: 409,
      body: { code: "projectModelUnavailable" },
    });
  });

  it("propagates a project the caller cannot read as the Files API's status", async () => {
    filesApiGet.mockImplementation(async (url: string) => {
      throw new DocspaceApiHttpError(403, "Forbidden", url);
    });
    await expect(applyAiProjectChatContext({ entityId: "42" })).rejects.toMatchObject({
      status: 403,
    });
  });

  it("refuses the round when the portal's Chat model cannot chat or none is assigned", async () => {
    readById.mockResolvedValueOnce({ id: "p1", capabilities: 2 });
    expect(await applyAiProjectChatContext({ entityId: "42" })).toMatchObject({ status: 409 });
    readByType.mockResolvedValueOnce(null);
    expect(await applyAiProjectChatContext({ entityId: "42" })).toMatchObject({ status: 409 });
  });

  it("runs on the project's own model ahead of the portal's", async () => {
    projectModel = "own";
    readById.mockImplementation(async (id: string) => ({ id, capabilities: 1 }));
    const body: Record<string, unknown> = { entityId: "42", profileId: "composer", actionArgs: {} };
    expect(await applyAiProjectChatContext(body)).toBeNull();
    expect(body["profileId"]).toBe("own");
    expect(readByType).not.toHaveBeenCalled();
  });

  it("refuses the round when the project's own model is gone, without falling back", async () => {
    projectModel = "own";
    readById.mockImplementation(async (id: string) =>
      id === "own" ? undefined : { id, capabilities: 1 },
    );
    expect(await applyAiProjectChatContext({ entityId: "42" })).toMatchObject({
      status: 409,
      body: { code: "projectModelUnavailable" },
    });
  });
});
