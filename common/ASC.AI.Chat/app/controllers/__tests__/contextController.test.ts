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

import type { ContextEngine } from "@onlyoffice/ai-chat/core";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import type { Reply } from "../../__tests__/helpers/http.js";
import { call } from "./promptsControllerData.helpers.js";

const engine = engineMock<ContextEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  ContextEngine: engineClass(engine),
}));
vi.mock("../../storage/index.js", () => ({ storage: {} }));

const { contextController } = await import("../contextController.js");
// After the mocks: these modules load the library, whose engine class is replaced above.
const { responseViolations } = await import("../../__tests__/helpers/schema.js");
const { DocspaceApiHttpError } = await import("../../storage/docspaceFilesApi.js");

const published = (operationId: string, reply: Reply): string[] =>
  responseViolations(operationId, reply.status, reply.body);

const folders = [
  {
    cloud: "docspace",
    rooms: [{ id: "5", name: "Instructions", path: "Rooms/Instructions", kind: "room" as const }],
  },
];
const skills = [{ id: "5678", name: "contract-review", description: "How to review a contract." }];

beforeEach(() => {
  mockReset(engine);
});

describe.each([
  ["getContextFolders", "aiContextGetContextFolders"],
  ["getKnowledgeFolders", "aiContextGetKnowledgeFolders"],
] as const)("contextController.%s", (method, operationId) => {
  it("answers the folders the engine lists", async () => {
    engine[method].mockResolvedValue(folders);

    const reply = await call(contextController[method], { method: "GET" });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(folders);
    expect(published(operationId, reply)).toEqual([]);
  });

  it("relays the status of the Files API without its URL", async () => {
    engine[method].mockRejectedValue(
      new DocspaceApiHttpError(403, "Forbidden", "http://proxy.internal/api"),
    );

    const reply = await call(contextController[method], { method: "GET" });

    expect(reply.status).toBe(403);
    expect(reply.body).toEqual({ error: "Forbidden" });
    expect(published(operationId, reply)).toEqual([]);
  });
});

describe("contextController.getRoomSkills", () => {
  it.each([
    ["with a cloud", { cloud: "docspace", roomId: "5" }, "docspace"],
    ["without a cloud", { roomId: "5" }, ""],
    ["with a repeated cloud", { cloud: ["a", "b"], roomId: "5" }, ""],
  ])("lists the skills of the room %s", async (_what, query, cloud) => {
    engine.getRoomSkills.mockResolvedValue(skills);

    const reply = await call(contextController.getRoomSkills, { method: "GET", query });

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual(skills);
    expect(engine.getRoomSkills).toHaveBeenCalledWith(cloud, "5");
    expect(published("aiContextGetRoomSkills", reply)).toEqual([]);
  });

  it.each([[{}], [{ roomId: "" }], [{ roomId: ["5", "6"] }]])(
    "answers 400 for the query %j",
    async (query) => {
      const reply = await call(contextController.getRoomSkills, { method: "GET", query });

      expect(reply.status).toBe(400);
      expect(reply.body).toEqual({ error: "roomId required" });
      expect(engine.getRoomSkills).not.toHaveBeenCalled();
      expect(published("aiContextGetRoomSkills", reply)).toEqual([]);
    },
  );

  it("relays a 404 of the Files API for a room that does not exist", async () => {
    engine.getRoomSkills.mockRejectedValue(
      new DocspaceApiHttpError(404, "Not Found", "http://proxy.internal"),
    );

    const reply = await call(contextController.getRoomSkills, {
      method: "GET",
      query: { roomId: "9" },
    });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "Not Found" });
    expect(published("aiContextGetRoomSkills", reply)).toEqual([]);
  });
});

describe("contextController.getRoomSkill", () => {
  it.each([
    ["with a cloud", { cloud: "docspace", roomId: "5", skillId: "5678" }, "docspace"],
    ["without a cloud", { roomId: "5", skillId: "5678" }, ""],
  ])("answers the Markdown of the skill %s", async (_what, query, cloud) => {
    engine.getRoomSkill.mockResolvedValue("---\nname: contract-review\n---\nBody");

    const reply = await call(contextController.getRoomSkill, { method: "GET", query });

    expect(reply.status).toBe(200);
    expect(reply.body).toBe("---\nname: contract-review\n---\nBody");
    expect(engine.getRoomSkill).toHaveBeenCalledWith(cloud, "5", "5678");
    expect(published("aiContextGetRoomSkill", reply)).toEqual([]);
  });

  it.each([
    [{}],
    [{ roomId: "5" }],
    [{ skillId: "5678" }],
    [{ roomId: "", skillId: "5678" }],
    [{ roomId: "5", skillId: "" }],
    [{ roomId: "5", skillId: ["1", "2"] }],
  ])("answers 400 for the query %j", async (query) => {
    const reply = await call(contextController.getRoomSkill, { method: "GET", query });

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "roomId and skillId required" });
    expect(engine.getRoomSkill).not.toHaveBeenCalled();
    expect(published("aiContextGetRoomSkill", reply)).toEqual([]);
  });

  it("answers 500 without details when the read fails", async () => {
    engine.getRoomSkill.mockRejectedValue(new Error("read failed at http://proxy.internal"));

    const reply = await call(contextController.getRoomSkill, {
      method: "GET",
      query: { roomId: "5", skillId: "5678" },
    });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal server error" });
    expect(published("aiContextGetRoomSkill", reply)).toEqual([]);
  });
});
