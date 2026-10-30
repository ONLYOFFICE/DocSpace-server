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

// The AI projects routes when the mock is on (`AI_PROJECTS_MOCK=1`, see
// storage/aiProjectsMock): the same handlers as aiProjectsController, the
// same id and profile checks, answered in process from the caller's mock
// world. Two extra routes drive it: `GET /api/2.0/ai/projects/mock` tells the
// scenario, `POST /api/2.0/ai/projects/mock` `{ "scenario": "many" }` rebuilds
// every world from another one (`default`, `many`, `empty`, `one`).

import type { Response } from "express";
import { asyncHandler } from "./_helpers.js";
import type { TypedRequest } from "./_helpers.js";
import {
  assertProjectProfile,
  assertKnowledgeRoomsLinkable,
  bodyOf,
  forwardQuery,
  projectIdParam,
} from "./aiProjectsController.js";
import * as mock from "../storage/aiProjectsMock/store.js";
import type { MockReply } from "../storage/aiProjectsMock/store.js";

function send(res: Response, reply: MockReply): void {
  res.status(reply.status).json(reply.body);
}

const stringQuery = (req: TypedRequest) => forwardQuery(req.query) as Record<string, string>;

function handler(run: (req: TypedRequest) => Promise<MockReply>) {
  return asyncHandler(async (req, res) => {
    send(res, await run(req));
  });
}

export const aiProjectsMockController = {
  getMock: handler(async () => mock.describeMock()),
  resetMock: handler(async (req) => {
    const body = bodyOf(req.body);
    return mock.resetMock(typeof body["scenario"] === "string" ? body["scenario"] : undefined);
  }),

  getProjects: handler(async (req) => mock.listProjects(stringQuery(req))),
  createProject: handler(async (req) => {
    const body = bodyOf(req.body);
    await assertProjectProfile(body);
    await assertKnowledgeRoomsLinkable(body);
    return mock.createProject(body);
  }),
  getProject: handler(async (req) => mock.getProject(projectIdParam(req.params["id"]))),
  updateProject: handler(async (req) => {
    const body = bodyOf(req.body);
    await assertProjectProfile(body);
    await assertKnowledgeRoomsLinkable(body);
    return mock.updateProject(projectIdParam(req.params["id"]), body);
  }),
  deleteProject: handler(async (req) => mock.deleteProject(projectIdParam(req.params["id"]))),
  getSkills: handler(async (req) => mock.getSkills(projectIdParam(req.params["id"]))),
  getKnowledgeFiles: handler(async (req) =>
    mock.getKnowledge(projectIdParam(req.params["id"]), stringQuery(req)),
  ),
};
