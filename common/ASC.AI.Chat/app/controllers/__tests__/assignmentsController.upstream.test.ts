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
import { invoke } from "../../__tests__/helpers/http.js";
import { responseViolations } from "../../__tests__/helpers/schema.js";
import { docspace, docspaceError, http, useUpstream } from "../../__tests__/helpers/upstream.js";
import { assignmentsController } from "../assignmentsController.js";

// The resolve operations with the real AssignmentsEngine and the real storage: only the AI
// service is stubbed. GET internal/ai/assignments/{actionType} (AssignmentsStorageController.cs),
// GET internal/ai/profiles/{id} (ProfileStorageController.cs).
const upstream = useUpstream();

const ASSIGNED = "6f1c2a3b-0000-4000-8000-000000000009";

function chatSlotNames(profileRead: () => Response): void {
  upstream.use(
    http.get("*/internal/ai/assignments/Chat", () => docspace(ASSIGNED)),
    http.get("*/internal/ai/profiles/:id", profileRead),
  );
}

describe("assignmentsController resolve operations over the real engine", () => {
  it("answers 404 from resolve-for-action when the assigned profile is gone", async () => {
    chatSlotNames(() => docspaceError(404));

    const reply = await invoke(assignmentsController.resolveForAction, {
      method: "GET",
      query: { actionType: "Chat" },
    });

    expect(reply.status).toBe(404);
    expect(reply.body).toEqual({ error: "no AI profile is available for ActionType.Chat" });
    expect(responseViolations("aiAssignmentsResolveForAction", 404, reply.body)).toEqual([]);
    expect(upstream.seen.map((r) => r.path)).toEqual([
      "/internal/ai/assignments/Chat",
      `/internal/ai/profiles/${ASSIGNED}`,
    ]);
  });

  it("answers null from try-resolve-for-action when the assigned profile is gone", async () => {
    chatSlotNames(() => docspaceError(404));

    const reply = await invoke(assignmentsController.tryResolveForAction, {
      method: "GET",
      query: { actionType: "Chat" },
    });

    expect(reply.status).toBe(200);
    expect(reply.body).toBeNull();
    expect(responseViolations("aiAssignmentsTryResolveForAction", 200, reply.body)).toEqual([]);
  });

  it("answers the assigned profile by its listed fields when it exists", async () => {
    // ProfileDto (products/ASC.AI/Server/Models/ResponseDto/ProfileDto.cs).
    const profile = {
      id: ASSIGNED,
      name: "GPT-4o",
      providerType: "openai",
      baseUrl: "https://api.openai.com/v1",
      modelId: "gpt-4o",
      createdAt: 1_700_000_000_000,
      key: "sk-MARKER",
    };
    chatSlotNames(() => docspace(profile));

    const reply = await invoke(assignmentsController.resolveForAction, {
      method: "GET",
      query: { actionType: "Chat" },
    });

    const { key: _key, ...shown } = profile;
    expect(reply.status).toBe(200);
    expect(reply.body).toEqual({ profileId: ASSIGNED, profile: shown });
    expect(responseViolations("aiAssignmentsResolveForAction", 200, reply.body)).toEqual([]);
  });

  it("answers the status of a failed profile read, not an empty slot", async () => {
    chatSlotNames(() => docspaceError(500));

    const reply = await invoke(assignmentsController.resolveForAction, {
      method: "GET",
      query: { actionType: "Chat" },
    });

    expect(reply.status).toBe(500);
    expect(reply.body).toEqual({ error: "Internal Server Error" });
    expect(responseViolations("aiAssignmentsResolveForAction", 500, reply.body)).toEqual([]);
  });
});
