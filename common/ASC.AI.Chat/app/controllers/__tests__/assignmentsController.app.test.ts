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

import type { AssignmentsEngine } from "@onlyoffice/ai-chat/core";
import request from "supertest";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { engineClass, engineMock, mockReset } from "../../__tests__/helpers/engine.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { aiGate, useUpstream } from "../../__tests__/helpers/upstream.js";

// The query fallback of the single-argument assignment routes, through the real
// parsers: body-parser sets `req.body` to `{}` for a request without a body.

const engine = engineMock<AssignmentsEngine>();
vi.mock("@onlyoffice/ai-chat/core", async (original) => ({
  ...(await original<typeof import("@onlyoffice/ai-chat/core")>()),
  AssignmentsEngine: engineClass(() => engine),
}));

const upstream = useUpstream({ localApp: true });
// The access gate of every route here (assertAiAccess) answers: the caller may use AI.
beforeEach(() => {
  upstream.use(aiGate());
});
const { createApp } = await import("../../createApp.js");
const { API_PREFIX } = await import("../../routes.js");
const app = createApp();

beforeEach(() => {
  mockReset(engine);
});

const AUTH = { Authorization: "Bearer t" };

describe("assignments routes through the whole app", () => {
  it("unassigns the action named in the query of a request without a body, as the document says", async () => {
    engine.unassign.mockResolvedValue(undefined);

    const reply = await request(app)
      .delete(`${API_PREFIX}/assignments/unassign?actionType=Chat`)
      .set(AUTH);

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.unassign).toHaveBeenCalledWith("Chat");
    expect(responseViolations("aiAssignmentsUnassign", 200, reply.body)).toEqual([]);
  });

  it("unassigns the action sent as a JSON string body", async () => {
    engine.unassign.mockResolvedValue(undefined);

    const reply = await request(app)
      .delete(`${API_PREFIX}/assignments/unassign`)
      .set(AUTH)
      .set("Content-Type", "application/json")
      .send('"Chat"');

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.unassign).toHaveBeenCalledWith("Chat");
  });

  it("deletes a profile's assignments by the id in the query", async () => {
    engine.cascadeProfileDelete.mockResolvedValue(undefined);

    const reply = await request(app)
      .delete(`${API_PREFIX}/assignments/cascade-profile-delete?profileId=p1`)
      .set(AUTH);

    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ success: true });
    expect(engine.cascadeProfileDelete).toHaveBeenCalledWith("p1");
  });

  it("answers 400 to a bulk assign whose JSON body is null", async () => {
    const reply = await request(app)
      .put(`${API_PREFIX}/assignments/bulk-assign`)
      .set(AUTH)
      .set("Content-Type", "application/json")
      .send("null");

    expect(reply.status).toBe(400);
    expect(reply.body).toEqual({ error: "body must be an object mapping actionType to profileId" });
    expect(engine.bulkAssign).not.toHaveBeenCalled();
  });

  it("calls no upstream but the access gate for any of these requests", async () => {
    await request(app).delete(`${API_PREFIX}/assignments/unassign?actionType=Chat`).set(AUTH);

    expect(upstream.seen.map((r) => `${r.method} ${r.path}`)).toEqual([
      "GET /internal/ai/tool-prefs",
    ]);
  });
});
