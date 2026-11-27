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

import request from "supertest";
import { describe, expect, it } from "vitest";
import { docspaceError, http, useUpstream } from "./helpers/upstream.js";
import { publishedOperations, responseViolations } from "./helpers/schema.js";
import { createApp } from "../createApp.js";

// The AI access gate before any work (ts-ai-failure-paths.md §3), over every published
// operation: the AI service refuses the caller (a Guest, or a portal with AI switched off), the
// request carries a body or a query the route would refuse, and the answer is 403 with no request
// made anywhere but to the AI service, whose 403 is the gate.

const upstream = useUpstream({ localApp: true });
const app = createApp();

const AUTH = { Authorization: "Bearer t" };
const JSON_TYPE = { "Content-Type": "application/json" };

// The routes the spec or the .NET suite lets judge their input before the gate, or leaves
// ungated; each names its source.
const NOT_GATED_FIRST: Readonly<Record<string, string>> = {
  // `editor-tools/call` refuses an empty tool name with 400 first (EditorToolAiDisabledTests).
  aiEditorToolsCall: "CONTRACT passthrough-13",
  // "validate before the AI switch (400 before 403)", openspec ai-agents "Exporting".
  aiExportTextToDocx: "openspec ai-agents, text-to-docx",
  // "usable by every member and while AI is off", openspec ai-agents "Per-user AI chat settings".
  aiSettingsGetUser: "openspec ai-agents, per-user settings",
  aiSettingsSetUser: "openspec ai-agents, per-user settings",
  // "readable by every signed-in member, Guests included, with AI on or off".
  aiSettingsGet: "openspec ai-agents, AI readiness",
  // "validation comes first on the write", openspec ai-agents "Gates on the tool permission mode routes".
  aiSettingsSetToolMode: "openspec ai-agents, tool permission mode",
  aiPreferencesSetToolPermissionMode: "openspec ai-agents, tool permission mode",
  // "id-format checks run before the role check" (ProfileAccessGuestTests).
  aiProfilesGetById: "ProfileAccessGuestTests",
  aiProfilesListModels: "ProfileAccessGuestTests",
  aiProfilesTestConnection: "ProfileAccessGuestTests",
  // Reads of Instructions and Knowledge rooms straight from the Files API, which refuses the AI
  // rooms section while AI is off (openspec ai-agents "Portal AI switch"); no AI-service gate.
  aiContextGetContextFolders: "Files API, openspec ai-agents Portal AI switch",
  aiContextGetKnowledgeFolders: "Files API, openspec ai-agents Portal AI switch",
  aiContextGetRoomSkills: "Files API, openspec ai-agents Portal AI switch",
  aiContextGetRoomSkill: "Files API, openspec ai-agents Portal AI switch",
};

const gated = publishedOperations().filter((op) => !(op.operationId in NOT_GATED_FIRST));

describe("the AI access gate", () => {
  it.each(gated.map((op) => [op.operationId, op.method, op.path] as const))(
    "answers 403 to %s for a caller without AI access, before its input is read",
    async (operationId, method, path) => {
      upstream.use(http.all("*/internal/ai/*", () => docspaceError(403)));
      // A path parameter, a repeated query parameter and a body of the wrong type: every check
      // of the input the route makes would refuse them.
      const url = `${path.replace(/\{[^}]+\}/g, "x")}?id=a&id=b&entityId=a&entityId=b`;
      const call = request(app)
        [method as "get" | "post" | "put" | "patch" | "delete"](url)
        .set(AUTH);
      const reply = method === "get" ? await call : await call.set(JSON_TYPE).send("7");

      expect(reply.status).toBe(403);
      expect(responseViolations(operationId, 403, reply.body)).toEqual([]);
      expect(upstream.seen.filter((r) => !r.path.startsWith("/internal/ai/"))).toEqual([]);
    },
  );

  // The same with an input the route accepts: one numeric `entityId`, `threadId` and `id`, in the
  // query and in the body. A route that reads the entity from the Files API ahead of the gate
  // passes the row above, where the repeated parameter is read as absent, and fails this one.
  it.each(gated.map((op) => [op.operationId, op.method, op.path] as const))(
    "answers 403 to %s with a well-formed entityId, with no request but to the AI service",
    async (operationId, method, path) => {
      upstream.use(http.all("*/internal/ai/*", () => docspaceError(403)));
      const url = `${path.replace(/\{[^}]+\}/g, "1")}?entityId=1&threadId=1&id=1`;
      const call = request(app)
        [method as "get" | "post" | "put" | "patch" | "delete"](url)
        .set(AUTH);
      const reply =
        method === "get"
          ? await call
          : await call.set(JSON_TYPE).send({ entityId: "1", threadId: "1", id: "1" });

      expect(reply.status).toBe(403);
      expect(responseViolations(operationId, 403, reply.body)).toEqual([]);
      expect(upstream.seen.filter((r) => !r.path.startsWith("/internal/ai/"))).toEqual([]);
    },
  );

  // The parsers of createApp run in front of the authentication gate and of the AI access gate
  // (ts-ai-express.md §2): what they refuse is answered for every caller, and no route is reached.
  describe("behind the body parser", () => {
    const op = gated.find((candidate) => candidate.method === "post");
    const path = op?.path.replace(/\{[^}]+\}/g, "x") ?? "";

    it("answers 400 to a body that does not parse, although the AI service would refuse the caller", async () => {
      upstream.use(http.all("*/internal/ai/*", () => docspaceError(403)));

      const reply = await request(app).post(path).set(AUTH).set(JSON_TYPE).send("{");

      expect(reply.status).toBe(400);
      expect(upstream.seen).toEqual([]);
    });

    it("answers 400 to a body that does not parse from a caller without credentials, not 401", async () => {
      const reply = await request(app).post(path).set(JSON_TYPE).send("{");

      expect(reply.status).toBe(400);
      expect(upstream.seen).toEqual([]);
    });

    it("answers 413 to a body over the parser's limit, although the AI service would refuse the caller", async () => {
      upstream.use(http.all("*/internal/ai/*", () => docspaceError(403)));
      const body = JSON.stringify({ text: "a".repeat(101 * 1024) });

      const reply = await request(app).post(path).set(AUTH).set(JSON_TYPE).send(body);

      expect(reply.status).toBe(413);
      expect(upstream.seen).toEqual([]);
    });

    it("answers 401 to a body that parses from a caller without credentials", async () => {
      const reply = await request(app).post(path).set(JSON_TYPE).send("{}");

      expect(reply.status).toBe(401);
      expect(upstream.seen).toEqual([]);
    });
  });

  it("leaves out only the routes the spec or the .NET suite lets judge their input first", () => {
    const published = new Set(publishedOperations().map((op) => op.operationId));
    expect(Object.keys(NOT_GATED_FIRST).filter((id) => !published.has(id))).toEqual([]);
  });
});
